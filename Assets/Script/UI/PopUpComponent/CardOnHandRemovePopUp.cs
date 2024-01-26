using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Mirror;

public class CardOnHandRemovePopUp : SingletonD<CardOnHandRemovePopUp>
{
    public CanvasGroup canvasGroup;
    public GridLayoutGroup gridLayoutGroup;
    public List<GameObject> removeCardSlots = new List<GameObject>();


    protected override void Awake()
    {
        PopUpUIManager.instance.onChangeCardOnHandRemovePopUpShow += OnChangeCardOnHandRemovePopUpShow;
        PopUpUIManager.instance.onChangeCardOnHandRemovePopUpHide += OnChangeCardOnHandRemovePopUpHide;
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    // 패 제거 팝업 확인 버튼 클릭
    public void HandleCardOnHandRemoveOk()
    {
        PopUpUIManager.instance.HandleHideCardOnHandRemovePopUp();
        GameUIManager.instance.buttonPrefareDeck.transform.SetParent(GameUIManager.instance.PrefareDeck.transform);
        GameUIManager.instance.buttonTrashDeck.transform.SetParent(GameUIManager.instance.TrashDeck.transform);
        M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHand");
        
        PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
        GamePlayerDeck gamePlayerDeck = playerInterface.currentGamePlayer.GetComponent<GamePlayerDeck>();
        int choosedCardCount = 0;
        for(int i=0; i<gamePlayerDeck.choosedCardOnHands.Length; i++){
            if(gamePlayerDeck.choosedCardOnHands[i] != null){
                choosedCardCount++;
                CardOnHand cardOnHand = gamePlayerDeck.choosedCardOnHands[i];
                float duration = 0.5f;
                cardOnHand.transform.DOScale(new Vector3(0.02f, 0.02f, 0f), duration);
                cardOnHand.transform.DORotate(new Vector3(0f, 0f, -90f), duration);
                cardOnHand.transform.DOMove(GameUIManager.instance.ForgottenDeck.transform.position, duration).SetEase(Ease.InOutCirc)
                .OnComplete(() => {
                    gamePlayerDeck.CmdDestroyCardOnHandToForgotten(cardOnHand);
                    cardOnHand.transform.DOKill();
                });
            }
        }
        CardData.instance.H26_CallBack(choosedCardCount); // 패 제거 팝업에서 선택한 카드 갯수 넘겨줌
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
        ResetCycleIndex();
        if(PopUpUIManager.instance.deckListPopUp.activeSelf){
            Button buttonPrefareDeck =  GameUIManager.instance.buttonPrefareDeck;
            buttonPrefareDeck.transform.SetParent(PopUpUIManager.instance.deckListPopUp.transform);
            buttonPrefareDeck.transform.SetAsLastSibling();

            Button buttonTrashDeck = GameUIManager.instance.buttonTrashDeck;
            buttonTrashDeck.transform.SetParent(PopUpUIManager.instance.deckListPopUp.transform);
            buttonTrashDeck.transform.SetAsLastSibling();
        }else{
            Button buttonPrefareDeck =  GameUIManager.instance.buttonPrefareDeck;
            buttonPrefareDeck.transform.SetParent(GameUIManager.instance.PrefareDeck.transform);
            buttonPrefareDeck.transform.SetAsLastSibling();

            Button buttonTrashDeck = GameUIManager.instance.buttonTrashDeck;
            buttonTrashDeck.transform.SetParent(GameUIManager.instance.TrashDeck.transform);
            buttonTrashDeck.transform.SetAsLastSibling();
        }
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
            M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHand");
        });
    }

    // 현재 플레이어의 패 제거 팝업에 사용되는 순환용 인덱스 0 으로 초기화
    private void ResetCycleIndex()
    {
        PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
        GamePlayerDeck gamePlayerDeck = playerInterface.currentGamePlayer.GetComponent<GamePlayerDeck>();
        gamePlayerDeck.currentIndex = 0;
    }
}
