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
        // 아이콘/설명 미등록 버프(SUHOJA 등)로 인한 KeyNotFoundException 방지 — 아이콘은 기본값 유지, 이름은 enum명
        if (BuffData.instance.buffIcons.TryGetValue(buff.type, out Sprite icon)) buffIcon.sprite = icon;
        if (BuffData.instance.buffDB.TryGetValue(buff.type, out BuffInformation info))
        {
            textBuffName.text = info.name;
            textBuffDescription.text = info.description;
        }
        else
        {
            textBuffName.text = buff.type.ToString();
            textBuffDescription.text = "";
        }
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
