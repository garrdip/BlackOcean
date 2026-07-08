using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Mirror;
using ProjectD;
using DG.Tweening;
using TMPro;
using Spine.Unity;
using Spine.Unity.Examples;
using System.Linq;

// TargetObject partial — 버프 획득/조회/방어도 처리 및 버프 SyncList 콜백
public partial class TargetObject
{

    // ----------------------------------------------           Buff 관련 함수          ---------------------------------------------------//

    // 붕괴 쇠락 등은 공유 // 꽃가루 뭐시기는 개인
    public int GainBuff(BuffType buffType, int value, bool isDebuff, bool isInfinity, bool isDecrease, bool isSeparate, TargetObject tar, Card card)
    {
        int retVal = 0;
        if(objectType == ObjectType.PLAYER && tar != this && CardData.instance.CheckCardCharacteristic(card,CardCharacteristic.GOOWON)) value *= 2; // 이곳에 구원 등록
        if(tar.HasBuff(BuffType.THEREISNOJABI) && buffType == BuffType.APDO)
        {
            int toalStack = GetBuffValue(buffType,tar) + value;
            StaticDamageToMonster(toalStack * tar.GetBuffValue(BuffType.THEREISNOJABI));
            if(HasBuff(buffType,tar))buffs.Remove(buffs.Find(buff => buff.type == buffType && buff.user == tar.netId));
            return 0;
        }

        if((buffs.Find(buff => buff.type == buffType && buff.user == tar.netId) == null && isSeparate )|| (buffs.Find(buff => buff.type == buffType) == null && !isSeparate )|| (isInfinity && value <= 0)) // 버프 신규 등록
        {
            if(value == 0 && !isInfinity)return 0;
            
            Buff newBuff = new Buff(buffType,value,isDebuff,isInfinity,isDecrease,isSeparate,tar);
            buffs.Add(newBuff);
            for(int i = 0 ;i < buffs.Count ; i++)
                Debug.Log(buffs[i].type);
            retVal =  buffs.FindIndex(buff => buff == newBuff);
        }
        else // 버프가 있을경우 중첩 상승
        {
            Buff modItem;
            int indexOfOldItem;
            if(isSeparate) 
            {
                modItem = new Buff(buffs.Find(buff => buff.type == buffType && buff.user == tar.netId));
                indexOfOldItem = buffs.FindIndex(buff => buff.type == buffType && buff.user == tar.netId);
            }
            else
            {
                modItem = new Buff(buffs.Find(buff => buff.type == buffType));
                indexOfOldItem = buffs.FindIndex(buff => buff.type == buffType);
            }
            
            modItem.value += value;
            if(modItem.type == BuffType.APDO && modItem.value >= currentApDoRequirement) // 압도 처리
            {
                monster.APDO();
                modItem.value -= currentApDoRequirement;
                currentApDoRequirement += 4;
            }
            if(modItem.value == 0)
                buffs.RemoveAt(indexOfOldItem);
            else
                buffs[indexOfOldItem] = modItem;
            retVal = indexOfOldItem;
        }
        return retVal;
    }


    public int GetBuffValue(BuffType buffType, TargetObject tar)
    {
        if(tar == null)
        {
            if(buffs.Find(buff => buff.type == buffType) == null) return 0;
            else return buffs.Find(buff => buff.type == buffType).value;
        }
        else
        {
            if(buffs.Find(buff => buff.type == buffType && buff.user == tar.netId) == null) return 0;
            else return buffs.Find(buff => buff.type == buffType && buff.user == tar.netId).value;
        }
    }


    public int GetBuffValue(BuffType buffType)
    {
        int retVal = 0;
        foreach(Buff buff in buffs)
        {
            if(buff.type  == buffType)
                retVal += buff.value;
        }
        return retVal;
    }


    public int GetBuffValueByIndex(int index)
    {
        return buffs[index].value;
    }


    public void GainBuffByIndex(int index, int value)
    {
        Buff newBuff = new Buff(buffs[index]);
        newBuff.value += value;
        buffs[index] = newBuff;
    }


    public void GainDefense(int value)
    {
        defense += value;
    }


    public bool HasBuff(BuffType buffType)
    {
        return buffs.FindIndex(buff => buff.type == buffType) != -1;
    }


    public bool HasBuff(BuffType buffType, TargetObject user)
    {
        return buffs.FindIndex(buff => buff.type == buffType && buff.user == user.netId) != -1;
    }


    private void ReArrangeBuffEffectIndex(int index)
    {
        buffTrunBeginEffect.Remove(index);
        buffCardDrowEffect.Remove(index);
        buffCardUseEffect.Remove(index);
        buffTurnEndEffect.Remove(index);
        List<int> keyList = new List<int>();
        keyList = buffTrunBeginEffect.Keys.ToList();
        foreach(int itemKey in keyList)
        {
            if(itemKey > index)
            {
                buffTrunBeginEffect.Add(itemKey-1,buffTrunBeginEffect[itemKey]);
                buffTrunBeginEffect.Remove(itemKey);
            }
        }
        keyList = buffCardDrowEffect.Keys.ToList();
        foreach(int itemKey in keyList)
        {
            if(itemKey > index)
            {
                buffCardDrowEffect.Add(itemKey-1,buffCardDrowEffect[itemKey]);
                buffCardDrowEffect.Remove(itemKey);
            }
        }
        keyList = buffCardUseEffect.Keys.ToList();
        foreach(int itemKey in keyList)
        {
            if(itemKey > index)
            {
                buffCardUseEffect.Add(itemKey-1,buffCardUseEffect[itemKey]);
                buffCardUseEffect.Remove(itemKey);
            }
        }
        keyList = buffTurnEndEffect.Keys.ToList();
        foreach(int itemKey in keyList)
        {
            if(itemKey > index)
            {
                buffTurnEndEffect.Add(itemKey-1,buffTurnEndEffect[itemKey]);
                buffTurnEndEffect.Remove(itemKey);
            }
        }
    }


    // ---------------------------------------------------------SynclList Callback ,Syncvar Hook -----------------------------------------------------------//

    public void OnChangedBuff(SyncList<Buff>.Operation op, int index, Buff oldBuff, Buff newBuff)
    {
        if(newBuff != null)
            if((newBuff.type == BuffType.ICHI_ATTACK || newBuff.type == BuffType.ICHI_DEFENSE) && objectType == ObjectType.PLAYER)
                foreach(CardOnHand cardOnHand in player.GetComponent<GamePlayerDeck>().cardOnHands)
                    cardOnHand.CardInfoChangedEvent?.Invoke();
        switch (op)
        {
            case SyncList<Buff>.Operation.OP_ADD:
                buffIndicator.SetBuff(newBuff, index, this);
                break;
            case SyncList<Buff>.Operation.OP_INSERT:
                buffIndicator.SetBuff(newBuff, index, this);
                break;
            case SyncList<Buff>.Operation.OP_REMOVEAT:
                ReArrangeBuffEffectIndex(index);
                buffIndicator.RemoveBuff(index, oldBuff, this);
                buffTrunBeginEffect.Remove(index);
                break;
            case SyncList<Buff>.Operation.OP_SET:
                buffIndicator.SetBuff(newBuff, index, this);
                break;
            case SyncList<Buff>.Operation.OP_CLEAR:
                break;
        }
    }
}
