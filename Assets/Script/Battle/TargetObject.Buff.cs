using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;

// TargetObject partial — 버프 획득/조회/방어도 처리 및 버프 SyncList 콜백
public partial class TargetObject
{

    // ----------------------------------------------           Buff 관련 함수          ---------------------------------------------------//

    // 붕괴 쇠락 등은 공유 // 꽃가루 뭐시기는 개인
    public int GainBuff(BuffType buffType, int value, bool isDebuff, bool isInfinity, bool isDecrease, bool isSeparate, TargetObject tar)
    {
        int retVal = 0;
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
        // 위험도 가중치 — 몬스터의 방어 획득량 보정 (몬스터별 MonsterStatDB HazardDef x 위험도, 위험도 시스템)
        if(objectType == ObjectType.ENEMY && monster != null)
            value = monster.ScaledDefense(value);
        // 방어력 저하 디버프(ICHI_DEFENSE 음수 — 에리스 '부서지세요'/'얼마나 버틸까요', 감시자의 방어 저하)는 획득량에서 차감.
        // 양수(방어 상승 버프)는 기존처럼 호출부가 직접 더하므로(창병 등) 여기서는 음수만 반영해 이중 계산을 피한다
        int defenseDown = Mathf.Min(0, GetBuffValue(BuffType.ICHI_DEFENSE));
        value = Mathf.Max(0, value + defenseDown);
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


    // ---------------------------------------------------------SynclList Callback ,Syncvar Hook -----------------------------------------------------------//

    public void OnChangedBuff(SyncList<Buff>.Operation op, int index, Buff oldBuff, Buff newBuff)
    {
        switch (op)
        {
            case SyncList<Buff>.Operation.OP_ADD:
                buffIndicator.SetBuff(newBuff, index, this);
                break;
            case SyncList<Buff>.Operation.OP_INSERT:
                buffIndicator.SetBuff(newBuff, index, this);
                break;
            case SyncList<Buff>.Operation.OP_REMOVEAT:
                buffIndicator.RemoveBuff(index, oldBuff, this);
                break;
            case SyncList<Buff>.Operation.OP_SET:
                buffIndicator.SetBuff(newBuff, index, this);
                break;
        }
    }
}
