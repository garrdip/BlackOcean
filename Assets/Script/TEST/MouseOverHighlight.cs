using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
public class MouseOverHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TextMeshProUGUI textMeshPro;
    public Material defaultMaterial;
    public Material outlineMaterial;


    void Start()
    {
        // TextMeshProUGUI 컴포넌트를 가져옵니다.
        textMeshPro = GetComponent<TextMeshProUGUI>();

        // 텍스트의 모든 글자에 대한 아웃라인을 비활성화합니다.
        textMeshPro.fontSharedMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        textMeshPro.fontMaterial = outlineMaterial;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        textMeshPro.fontMaterial = defaultMaterial;
    }

}