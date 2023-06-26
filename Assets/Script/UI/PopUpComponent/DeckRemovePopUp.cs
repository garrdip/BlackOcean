using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DeckRemovePopUp : SingletonD<DeckRemovePopUp>
{
    public CanvasGroup canvasGroup;


    protected override void Awake()
    {
        PopUpUIManager.instance.onChangeDeckRemovePopUpShow += OnChangeDeckRemovePopUpShow;
        PopUpUIManager.instance.onChangeDeckRemovePopUpHide += OnChangeDeckRemovePopUpHide;
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // DeckRemovePopUp 활성화 콜백
    public void OnChangeDeckRemovePopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
    }

    // DeckRemovePopUp 비활성화 콜백
    public void OnChangeDeckRemovePopUpHide()
    {
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
        });
    } 
}
