using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopUpWindow : MonoBehaviour
{
    public RectTransform verticalBar;
    public RectTransform backGround;
    public Transform mark;
    public TextMeshProUGUI description;

    public void SetPopUpWinwdowText(string info)
    {
        if(!CardData.instance.infomationDB.ContainsKey(info))return;
        description.text = "<b><color=yellow>  " + CardData.instance.infomationDB[info].name + "</b></color>" + '\n' + CardData.instance.infomationDB[info].description;
        float preferredHeight = description.preferredHeight + 0.1f;
        verticalBar.sizeDelta = new Vector2(0.04f,preferredHeight);
        backGround.sizeDelta = new Vector2(3f,preferredHeight);
        mark.localPosition = new Vector3(-1.36f,preferredHeight/2,0);
        GetComponent<RectTransform>().sizeDelta = new Vector2(3f,preferredHeight);
    }
    public void SetPopUpWinwdowText(Infomation info)
    {
        if(!CardData.instance.infomationDB.ContainsKey(info.info))return;
        description.text = "<b>  " + CardData.instance.colorList[info.colorCode] + CardData.instance.infomationDB[info.info].name + "</b></color>" + '\n' + CardData.instance.infomationDB[info.info].description;
        float preferredHeight = description.preferredHeight + 0.1f;
        verticalBar.sizeDelta = new Vector2(0.04f,preferredHeight);
        backGround.sizeDelta = new Vector2(3f,preferredHeight);
        mark.localPosition = new Vector3(-1.36f,preferredHeight/2,0);
        GetComponent<RectTransform>().sizeDelta = new Vector2(3f,preferredHeight);
    }
}
