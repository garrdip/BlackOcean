using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BuffIndicatorController : MonoBehaviour
{
    public GameObject buffPrefab;
    public List<Sprite> buffIcons;

    List<(GameObject,Buff)> indicatedBuffs = new List<(GameObject,Buff)>();

    public void SetBuff(Buff buff)
    {
        if(indicatedBuffs.Exists(x => x.Item2.type == buff.type))
        {
            if(!buff.isInfinity && buff.value == 0)
            {
                GameObject removableBuff = indicatedBuffs.Find(x => x.Item2.type == buff.type).Item1;
                indicatedBuffs.RemoveAt(indicatedBuffs.FindIndex(x => x.Item2.type == buff.type));
                Destroy(removableBuff);
            }
            else
                indicatedBuffs.Find(x => x.Item2.type == buff.type).Item1.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = buff.value.ToString();
        }
        else
        {
            GameObject newBuff = Instantiate(buffPrefab);
            newBuff.transform.SetParent(transform);
            if(buff.type == ProjectD.BuffType.IRONDEMON)
                newBuff.GetComponent<SpriteRenderer>().sprite = buffIcons[1];
            else
                newBuff.GetComponent<SpriteRenderer>().sprite = buffIcons[0];
            newBuff.GetComponent<BuffIndicator>().buff = buff;
            if(!buff.isInfinity)
            {
                newBuff.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = buff.value.ToString();
            }
            else
            {
                newBuff.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
            }

            indicatedBuffs.Add((newBuff,buff));
        }
    }

}
