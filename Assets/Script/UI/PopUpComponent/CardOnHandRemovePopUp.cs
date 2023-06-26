using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CardOnHandRemovePopUp : SingletonD<CardOnHandRemovePopUp>
{
    public CanvasGroup canvasGroup;


    protected override void Awake()
    {
        PopUpUIManager.instance.onChangeCardOnHandRemovePopUpShow += OnChangeCardOnHandRemovePopUpShow;
        PopUpUIManager.instance.onChangeCardOnHandRemovePopUpHide += OnChangeCardOnHandRemovePopUpHide;
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    // CardOnHand 제거 팝업 확인 버튼 클릭
    public void HandleCardOnHandRemoveOk()
    {
        PopUpUIManager.instance.HandleHideCardOnHandRemovePopUp();
        GameUIManager.instance.buttonPrefareDeck.transform.SetParent(GameUIManager.instance.PrefareDeck.transform);
        GameUIManager.instance.buttonTrashDeck.transform.SetParent(GameUIManager.instance.TrashDeck.transform);
        M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHand");
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // CardOnHandRemovePop 활성화 콜백
    public void OnChangeCardOnHandRemovePopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        Button buttonPrefareDeck =  GameUIManager.instance.buttonPrefareDeck;
        buttonPrefareDeck.transform.SetParent(PopUpUIManager.instance.cardOnHandRemovePopUp.transform);
        buttonPrefareDeck.transform.SetAsLastSibling();

        Button buttonTrashDeck = GameUIManager.instance.buttonTrashDeck;
        buttonTrashDeck.transform.SetParent(PopUpUIManager.instance.cardOnHandRemovePopUp.transform);
        buttonTrashDeck.transform.SetAsLastSibling();
        M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHandOverPopUp");
    }

     // CardOnHandRemovePop 비활성화 콜백
    public void OnChangeCardOnHandRemovePopUpHide()
    {
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
        });
    }
}
