using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;
using TMPro;
using Steamworks;

public class DeckUI : SingletonD<DeckUI>
{
    [Header("게임 오브젝트")]
    public GameObject DeckListPanel;
    public GameObject GameUI;
    public GameObject CardOnHandsPanel;
    public GameObject PrefareDeck;
    public GameObject TrashDeck;
    public GameObject DeckListPopUp;
    public GameObject DeckRemovePopUp;
    public GameObject CardOnDeckPrefab;
    public GameObject GameBackGround;

    [Header("UI 요소")]
    public Button buttonEndTurn;
    public Button buttonPrefareDeck;
    public Button buttonTrashDeck;
    public Button buttonReturnGame;
    public GridLayoutGroup gridLayoutGroup;
    public Text textPrefareDeckCount;
    public Text textTrashDeckCount;
    public Button buttonRemoveConfirm;
    public Button testButton;

    [Header("댁 리스트")]
    public List<GameObject> deckList;

    [Header("플레이어 리스트(플레이어 정보 및 턴 정보)")]
    public List<GameObject> playerOrderList;

    private int originSiblingIndex = 0;
    private bool isOpenPrefareDeckPopUp = false;
    private bool isOpenTrashDeckPopUp = false;
    private bool isOpenDeckRemovePopUp = false;


    // 턴 넘김
    public void HandleEndTurn()
    {
        NetworkClient.localPlayer.GetComponent<GamePlayer>().endTurnActive = !NetworkClient.localPlayer.GetComponent<GamePlayer>().endTurnActive;
    }

    // PrefareDeck 정보 팝업
    public void HandleShowPrefareDeck()
    {
        isOpenPrefareDeckPopUp = !isOpenPrefareDeckPopUp; // 버튼 클릭으로도 팝업 열기 및 닫기
        originSiblingIndex = buttonPrefareDeck.transform.GetSiblingIndex(); // 원래의 오브젝트 스택 순서 인덱스값
        DeckListPopUp.gameObject.SetActive(isOpenPrefareDeckPopUp);
        if(isOpenPrefareDeckPopUp){
            // 팝업창 열리면 버튼 오브젝트의 부모를 팝업으로 바꾼뒤 가장 마지막 순서로 추가하여 팝업창 위에 버튼 그려지도록 변경
            buttonPrefareDeck.transform.SetParent(DeckListPopUp.transform);
            buttonPrefareDeck.transform.SetAsLastSibling();
            ChangeCardOnHandSortingLayerByName("CardOnHand");
        }else{
            HandleReturnGame();
        }

        // 로컬 플레이어의 PrefareDeck 조회
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            AddDeckList(gamePlayerDeck.prefareDeck);
        }
        // TODO : 관전하려는 플레이어의 PrefareDeck 조회
    }

    // TrashDeck 정보 팝업
    public void HandleShowTrashDeck()
    {
        isOpenTrashDeckPopUp = !isOpenTrashDeckPopUp; // 버튼 클릭으로도 팝업 열기 및 닫기
        originSiblingIndex = buttonTrashDeck.transform.GetSiblingIndex(); // 원래의 오브젝트 스택 순서 인덱스값
        DeckListPopUp.gameObject.SetActive(isOpenTrashDeckPopUp);
        if(isOpenTrashDeckPopUp){
            // 팝업창 열리면 버튼 오브젝트의 부모를 팝업으로 바꾼뒤 가장 마지막 순서로 추가하여 팝업창 위에 버튼 그려지도록 변경
            buttonTrashDeck.transform.SetParent(DeckListPopUp.transform);
            buttonTrashDeck.transform.SetAsLastSibling();
            ChangeCardOnHandSortingLayerByName("CardOnHand");
        }else{
            HandleReturnGame();
        }

        // 로컬 플레이어의 Trash Deck 조회
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            AddDeckList(gamePlayerDeck.trashDeck);
        }
        // TODO : 관전하려는 플레이어의 TrashDeck 조회
    }

    // 댁 제거 팝업 호출 (TEST)
    public void HandleTest()
    {
        isOpenDeckRemovePopUp = !isOpenDeckRemovePopUp;
        DeckRemovePopUp.gameObject.SetActive(isOpenDeckRemovePopUp);
        if(isOpenDeckRemovePopUp){
            buttonPrefareDeck.transform.SetParent(DeckRemovePopUp.transform);
            buttonPrefareDeck.transform.SetAsLastSibling();
            buttonTrashDeck.transform.SetParent(DeckRemovePopUp.transform);
            buttonTrashDeck.transform.SetAsLastSibling();
            ChangeCardOnHandSortingLayerByName("CardOnHandOverPopUp");
        }else{
            HandleRemoveConfirm();
        }
    }

    // 댁 제거 팝업 확인 버튼 클릭
    public void HandleRemoveConfirm()
    {
        isOpenDeckRemovePopUp = false;
        DeckRemovePopUp.gameObject.SetActive(false);
        ChangeCardOnHandSortingLayerByName("CardOnHand");
        buttonPrefareDeck.transform.SetParent(PrefareDeck.transform);
        buttonTrashDeck.transform.SetParent(TrashDeck.transform);
    }

    // CardOnHand 오브젝트들의 sortingLayer 변경
    private void ChangeCardOnHandSortingLayerByName(string layerName)
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands){
                cardOnHand.GetComponent<SpriteRenderer>().sortingLayerName = layerName;
                cardOnHand.cardOnHandCanvas.sortingLayerName = layerName;
            }
        }
    }

    // 팝업 닫고 게임으로 돌아가기
    public void HandleReturnGame()
    {
        isOpenPrefareDeckPopUp = false;
        isOpenTrashDeckPopUp = false;
        if(isOpenDeckRemovePopUp){
            buttonPrefareDeck.transform.SetParent(DeckRemovePopUp.transform);
            buttonTrashDeck.transform.SetParent(DeckRemovePopUp.transform);
            ChangeCardOnHandSortingLayerByName("CardOnHandOverPopUp");
        }else{
            buttonPrefareDeck.transform.SetParent(PrefareDeck.transform);
            buttonTrashDeck.transform.SetParent(TrashDeck.transform);
            ChangeCardOnHandSortingLayerByName("CardOnHand");
        }
        buttonPrefareDeck.transform.SetSiblingIndex(originSiblingIndex);
        buttonTrashDeck.transform.SetSiblingIndex(originSiblingIndex);
        DeckListPopUp.gameObject.SetActive(false);
        ClearDeckList();
    }

    // Deck정보 리스트 요소 추가
    private void AddDeckList(SyncList<Card> cards)
    {
        ClearDeckList();
        foreach(Card card in cards){
            GameObject cardOnDeck = Instantiate(CardOnDeckPrefab);
            cardOnDeck.transform.SetParent(gridLayoutGroup.transform);
            cardOnDeck.transform.localScale = new Vector3(1, 1, 1);
            cardOnDeck.GetComponent<CardOnDeck>().card = card;
            deckList.Add(cardOnDeck);
        }
    }

    // Deck정보 리스트 요소 제거
    private void ClearDeckList()
    {
        for(int i=deckList.Count-1; i >=0; i--){
            Destroy(deckList[i]);
            deckList.RemoveAt(i);
        }
    }

    // 댁 카운트 텍스트 컴포넌트들의 크기 변경 애니매이션(댁 카운트 변경 시 크기 커졌다 작아지는 애니매이션)
    public void DeckCountTextScaleAnimation(Text textComponent, int count)
    {
        Vector3 chagenScale = new Vector3(2f, 2f, 2f);
        Vector3 originScale = new Vector3(1f, 1f, 1f);
        textComponent.text = count.ToString();
        textComponent.transform.DOScale(chagenScale, 0.1f).SetEase(Ease.OutQuad)
        .OnComplete(() =>
        {
            textComponent.transform.DOScale(originScale, 0.1f).SetEase(Ease.InQuad);
        });
    }
}