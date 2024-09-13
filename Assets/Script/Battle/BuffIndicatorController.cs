using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using TMPro;

public class BuffIndicatorController : MonoBehaviour
{
    public GameObject buffPrefab;
   
    List<GameObject> indicatedBuffs = new List<GameObject>();

    public void SetBuff(Buff buff, int index, TargetObject tar)
    {
        if(index < indicatedBuffs.Count) // 신규 버프 여부 확인
        {
            Debug.Log("기존 버프 변경");    
            indicatedBuffs[index].transform.GetChild(0).GetComponent<TextMeshPro>().text = buff.value.ToString();
            SetMonsterFlowerPowder(buff, tar, false);
        }
        else
        {
            Debug.Log("신규 버프 등록");
            GameObject newBuff = Instantiate(buffPrefab);
            newBuff.transform.SetParent(transform);
            newBuff.GetComponent<SpriteRenderer>().sprite = BuffData.instance.buffIcons[buff.type];
            newBuff.GetComponent<BuffIndicator>().buff = buff;

            if(!buff.isInfinity)
            {
                newBuff.transform.GetChild(0).GetComponent<TextMeshPro>().text = buff.value.ToString();
            }
            else
            {
                newBuff.transform.GetChild(0).GetComponent<TextMeshPro>().text = "";
            }
            indicatedBuffs.Add(newBuff);
            SetMonsterFlowerPowder(buff, tar, true);
        }
    }

    public void RemoveBuff(int index, Buff buff, TargetObject tar)
    {
        if(buff.type == BuffType.FLOWERPOWDER && tar.objectType == ObjectType.ENEMY){
            NamePlate namePlate = tar.monsterNamePlate.GetComponent<NamePlate>();
            namePlate.SetHpBarByNoneFlowerPowderState(tar.monster.HP, tar.monster.MAXHP); // 몬스터인 경우 꽃가루 버프가 제거될 때 Hp Bar 상태 원래 상태로 변경
        }
        Destroy(indicatedBuffs[index]);
        indicatedBuffs.RemoveAt(index);
    }

    private void SetMonsterFlowerPowder(Buff buff, TargetObject tar, bool isNewBuff)
    {
        if(buff.type == BuffType.FLOWERPOWDER && tar.objectType == ObjectType.ENEMY){
            NamePlate namePlate = tar.monsterNamePlate.GetComponent<NamePlate>();
            namePlate.SetHpBarByFlowerPowderState(buff.value, tar.monster.HP, tar.monster.MAXHP, isNewBuff); // 몬스터인 경우 꽃가루 버프가 추가될 때 Hp Bar 처리
        }
    }
}
