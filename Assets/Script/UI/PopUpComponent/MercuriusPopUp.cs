using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class MercuriusPopUp : SingletonD<MercuriusPopUp>
{
    public CanvasGroup canvasGroup;
    public Button backBtn;


    protected override void Awake()
    {
        PopUpUIManager.instance.onMercuriusPopUpShow += OnMercuriusPopUpShow;
        PopUpUIManager.instance.onMercuriusPopUpHide += OnMercuriusPopUpHide;
        backBtn.onClick.AddListener(() => OnMercuriusPopUpHide());
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // DeckRemovePopUp 활성화 콜백
    public void OnMercuriusPopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
    }

    // DeckRemovePopUp 비활성화 콜백
    public void OnMercuriusPopUpHide()
    {
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
        });
    } 
}
