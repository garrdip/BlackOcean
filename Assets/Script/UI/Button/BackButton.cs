using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class BackButton : MonoBehaviour
{
    public GameObject backButtonArrow;
    public GameObject backButtonArrowLight;
    public GameObject backButtonTail;
    public GameObject backButtonTailLight;

    private float moveTime;
    private float movePositionX;
    public Vector3 originPosition;

    void Awake()
    {
        moveTime = 0.3f;
        movePositionX = -29f;
    }
    void Start()
    {
        originPosition = backButtonTail.GetComponent<RectTransform>().localPosition;
    }

    void OnDisable()
    {
        backButtonTail.GetComponent<RectTransform>().DOKill();
        backButtonTailLight.GetComponent<RectTransform>().DOKill();
        backButtonTail.GetComponent<RectTransform>().localPosition = originPosition;
        backButtonTailLight.GetComponent<RectTransform>().localPosition = originPosition;
    }

    void OnDestroy()
    {
        backButtonTail.GetComponent<RectTransform>().DOKill();
        backButtonTailLight.GetComponent<RectTransform>().DOKill();
    }


    // ---------------------------------------------------- 이벤트 트리거에 등록된 함수들 ----------------------------------------------------//
    
    public void OnPointerEnterBackButton()
    {
        backButtonArrowLight.SetActive(true);
        backButtonTailLight.SetActive(true);
        backButtonTail.GetComponent<RectTransform>().DOLocalMoveX(movePositionX, moveTime);
        backButtonTailLight.GetComponent<RectTransform>().DOLocalMoveX(movePositionX, moveTime);
    }

    public void OnPointerExitBackButton()
    {
        backButtonArrowLight.SetActive(false);
        backButtonTailLight.SetActive(false);
        backButtonTail.GetComponent<RectTransform>().DOLocalMoveX(0f, moveTime);
        backButtonTailLight.GetComponent<RectTransform>().DOLocalMoveX(0f, moveTime);
    }

    public void OnPointerClickBackButtonOnRoom()
    {
        RoomUI.instance.HandleBackToMainScene();
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    public void OnPointerClickBackButtonOnDeckListPopUp()
    {
        PopUpUIManager.instance.HandleHideDeckListPopUp();
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    public void OnPointerClickBackButtonOnCardEnhancePopUp()
    {
        PopUpUIManager.instance.HandleCardEnhancePopUp(false);
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    public void OnPointerClickBackButtonOnCardRemovePopUp()
    {
        PopUpUIManager.instance.HandleCardRemovePopUp(false);
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    public void OnPointerClickBackButtonOnCampPopUp()
    {
        PopUpUIManager.instance.HandleCampPopUpHide();
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }
}
