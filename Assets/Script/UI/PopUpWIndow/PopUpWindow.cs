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

    public void SetPopUpWinwdowText(Infomation info)
    {
        description.text = CardData.instance.infomationDB[info.info].name + '\n' + CardData.instance.infomationDB[info.info].description;
        float preferredHeight = description.preferredHeight;
        verticalBar.sizeDelta = new Vector2(0.04f,preferredHeight);
        backGround.sizeDelta = new Vector2(3f,preferredHeight);
        mark.localPosition = new Vector3(-1.36f,preferredHeight/2,0);
        GetComponent<RectTransform>().sizeDelta = new Vector2(3f,preferredHeight);
    }
}
