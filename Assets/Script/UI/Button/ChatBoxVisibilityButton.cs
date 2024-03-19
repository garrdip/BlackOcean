using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ChatBoxVisibilityButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public GameObject buttonIcon;
    public GameObject buttonIconLight;

    void OnDestroy()
    {
        buttonIcon.GetComponent<RectTransform>().DOKill();
        buttonIconLight.GetComponent<RectTransform>().DOKill();
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        buttonIconLight.SetActive(true);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        buttonIconLight.SetActive(false);
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        M_MessageManager.instance.ChangeChatBoxVisibileState();
        if(M_MessageManager.instance.isChatBoxVisible){
            buttonIcon.GetComponent<RectTransform>().DORotate(new Vector3(0f, 0f, 180f), 0.5f);
            buttonIconLight.GetComponent<RectTransform>().DORotate(new Vector3(0f, 0f, 180f), 0.5f);
        }else{
            buttonIcon.GetComponent<RectTransform>().DORotate(new Vector3(0f, 0f, 0f), 0.5f);
            buttonIconLight.GetComponent<RectTransform>().DORotate(new Vector3(0f, 0f, 0f), 0.5f);
        }
    }
}
