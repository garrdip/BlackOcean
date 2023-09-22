using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class NamePlate : MonoBehaviour
{
    public GameObject hpBarFiller;
    public TextMeshProUGUI hpText;
    public GameObject shield;
    public TextMeshProUGUI shieldValue;

    public void SetHPValue(int value,int max,int order)
    {
        hpBarFiller.transform.localPosition = new Vector3((3.2f*value/max)-3.2f,0,0);
        hpBarFiller.GetComponent<SpriteRenderer>().sortingOrder = -order+21;
        transform.GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = -order + 20;

        hpText.text = value + "/" + max;
    }

    public void SetShieldValue(int value,bool isGain, bool isEnemy)
    {
        if(value == 0)
        {
            shield.transform.localPosition = isEnemy? new Vector3(-1.9f,0.075f) : new Vector3(2,0.075f);
            shield.GetComponent<SpriteRenderer>().color = new Color(1,1,1,0);
            shield.SetActive(false);
        }
        else
        {
            if(isGain)
            {
                shield.SetActive(true);
                Sequence sequence;
                sequence = DOTween.Sequence()
                    .Join(shield.GetComponent<SpriteRenderer>().DOColor(new Color(1,1,1,1),0.5f))
                    .Join(shield.transform.DOLocalMove((isEnemy)?new Vector3(-0.9f,0.075f) : new Vector3(1,0.075f),0.5f));
            }
            else
            {

            }
            shieldValue.text = value.ToString();
        }
    }
}
