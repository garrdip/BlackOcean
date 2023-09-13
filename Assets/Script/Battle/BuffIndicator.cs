using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BuffIndicator : MonoBehaviour
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
            newBuff.GetComponent<SpriteRenderer>().sprite = buffIcons[0];
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
