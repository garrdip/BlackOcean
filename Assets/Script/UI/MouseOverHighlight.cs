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
        textMeshPro = GetComponent<TextMeshProUGUI>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        textMeshPro.fontMaterial = outlineMaterial;
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseover"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        textMeshPro.fontMaterial = defaultMaterial;
    }

}