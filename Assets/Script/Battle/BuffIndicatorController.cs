using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using TMPro;

public class BuffIndicatorController : MonoBehaviour
{
    public GameObject buffPrefab;
   
    List<GameObject> indicatedBuffs = new List<GameObject>();

    public void SetBuff(Buff buff,int index)
    {
        if(index < indicatedBuffs.Count) // 신규 버프 여부 확인
        {
            indicatedBuffs[index].transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = buff.value.ToString();
        }
        else
        {
            GameObject newBuff = Instantiate(buffPrefab);
            newBuff.transform.SetParent(transform);
            newBuff.GetComponent<SpriteRenderer>().sprite = CardData.instance.buffIcons[buff.type];
            newBuff.GetComponent<BuffIndicator>().buff = buff;

            if(!buff.isInfinity)
            {
                newBuff.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = buff.value.ToString();
            }
            else
            {
                newBuff.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
            }
            newBuff.transform.GetChild(0).GetComponent<Canvas>().sortingLayerName = "BackLayer";
            newBuff.transform.GetChild(0).GetComponent<Canvas>().sortingOrder = 10;
            indicatedBuffs.Add(newBuff);
        }
    }

    public void RemoveBuff(int index)
    {
        Destroy(indicatedBuffs[index]);
        indicatedBuffs.RemoveAt(index);
    }
}
