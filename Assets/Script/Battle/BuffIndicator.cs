using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuffIndicator : MonoBehaviour
{
    public Buff buff;
    public GameObject buffInfo;
    public Canvas canvas;
    public Image buffIcon;
    public TextMeshProUGUI textBuffName;
    public TextMeshProUGUI textBuffDescription;


    void Start()
    {
        buffIcon.sprite = BuffData.instance.buffIcons[buff.type];
        textBuffName.text = BuffData.instance.buffDB[buff.type].name;
        textBuffDescription.text = BuffData.instance.buffDB[buff.type].description;
    }

    void OnMouseEnter()
    {
        canvas.sortingLayerName = "PopUp";
        buffInfo.SetActive(true);
    }

    void OnMouseExit()
    {
        canvas.sortingLayerName = "BackLayer";
        buffInfo.gameObject.SetActive(false);
    }
}
