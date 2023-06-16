using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;


public class PopUpUI : SingletonD<PopUpUI>
{
    [Header("게임 오브젝트")]
    public GameObject DeckListPopUp; // 덱 목록 팝업
    public GameObject DeckRemovePopUp; // 덱 제거 팝업
    public GameObject CardOnHandRemovePopUp; // 패 제거 팝업
    public GameObject LayoutCardOnHandForRemove;
    public GameObject PrefareDeck;
    public GameObject TrashDeck;

    [Header("UI에 사용되는 카드 프리팹")]
    public GameObject CardOnDeckPrefab;


    [Header("UI 컴포넌트")]
    public GridLayoutGroup deckListPopUpGrid;
    public GridLayoutGroup deckRemovePopUpGrid;
    public Button buttonRemoveCardOnHandOk;
    public Button buttonReturnGame;

    [Header("댁 리스트")]
    public List<GameObject> deckList;

    [Header("팝업 제어 변수")]
    private int originSiblingIndex = 0;
    private bool isOpenPrefareDeckPopUp = false;
    private bool isOpenTrashDeckPopUp = false;
    private bool isOpenCardOnHandRemovePopUp = false;



    // PrefareDeck 정보 팝업
    public void HandleShowPrefareDeck()
    {
        isOpenPrefareDeckPopUp = !isOpenPrefareDeckPopUp; // 버튼 클릭으로도 팝업 열기 및 닫기
        originSiblingIndex = DeckUI.instance.buttonPrefareDeck.transform.GetSiblingIndex(); // 원래의 오브젝트 스택 순서 인덱스값
        DeckListPopUp.gameObject.SetActive(isOpenPrefareDeckPopUp);
        if(isOpenPrefareDeckPopUp){
            // 팝업창 열리면 버튼 오브젝트의 부모를 팝업으로 바꾼뒤 가장 마지막 순서로 추가하여 팝업창 위에 버튼 그려지도록 변경
            DeckUI.instance.buttonPrefareDeck.transform.SetParent(DeckListPopUp.transform);
            DeckUI.instance.buttonPrefareDeck.transform.SetAsLastSibling();
            M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHand");
        }else{
            HandleReturnGame();
        }

        // 로컬 플레이어의 PrefareDeck 조회
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            AddDeckList(gamePlayerDeck.prefareDeck, deckListPopUpGrid);
        }
        // TODO : 관전하려는 플레이어의 PrefareDeck 조회
    }

    // TrashDeck 정보 팝업
    public void HandleShowTrashDeck()
    {
        isOpenTrashDeckPopUp = !isOpenTrashDeckPopUp; // 버튼 클릭으로도 팝업 열기 및 닫기
        originSiblingIndex = DeckUI.instance.buttonTrashDeck.transform.GetSiblingIndex(); // 원래의 오브젝트 스택 순서 인덱스값
        DeckListPopUp.gameObject.SetActive(isOpenTrashDeckPopUp);
        if(isOpenTrashDeckPopUp){
            // 팝업창 열리면 버튼 오브젝트의 부모를 팝업으로 바꾼뒤 가장 마지막 순서로 추가하여 팝업창 위에 버튼 그려지도록 변경
            DeckUI.instance.buttonTrashDeck.transform.SetParent(DeckListPopUp.transform);
            DeckUI.instance.buttonTrashDeck.transform.SetAsLastSibling();
            M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHand");
        }else{
            HandleReturnGame();
        }

        // 로컬 플레이어의 Trash Deck 조회
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            AddDeckList(gamePlayerDeck.trashDeck, deckListPopUpGrid);
        }
        // TODO : 관전하려는 플레이어의 TrashDeck 조회
    }

    // CardOnHand 제거 팝업 호출
    public void HandleOpenCardOnHandRemovePopUp()
    {
        isOpenCardOnHandRemovePopUp = !isOpenCardOnHandRemovePopUp;
        CardOnHandRemovePopUp.gameObject.SetActive(isOpenCardOnHandRemovePopUp);
        if(isOpenCardOnHandRemovePopUp){
            Button buttonPrefareDeck =  DeckUI.instance.buttonPrefareDeck;
            buttonPrefareDeck.transform.SetParent(CardOnHandRemovePopUp.transform);
            buttonPrefareDeck.transform.SetAsLastSibling();

            Button buttonTrashDeck = DeckUI.instance.buttonTrashDeck;
            buttonTrashDeck.transform.SetParent(CardOnHandRemovePopUp.transform);
            buttonTrashDeck.transform.SetAsLastSibling();
            M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHandOverPopUp");
        }else{
            HandleCardOnHandRemoveOk();
        }
    }

    // CardOnHand 제거 팝업 확인 버튼 클릭
    public void HandleCardOnHandRemoveOk()
    {
        isOpenCardOnHandRemovePopUp = false;
        CardOnHandRemovePopUp.gameObject.SetActive(false);
        DeckUI.instance.buttonPrefareDeck.transform.SetParent(PrefareDeck.transform);
        DeckUI.instance.buttonTrashDeck.transform.SetParent(TrashDeck.transform);
        M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHand");
    }

    // 덱 제거 팝업 호출
    public void HandleOpenDeckRemovePopUp(SyncList<Card> deck)
    {
        DeckRemovePopUp.SetActive(true);
        AddDeckList(deck, deckRemovePopUpGrid);
    }

    // 덱 제거 팝업 확인 버튼
    public void HandleDeckRemoveOk()
    {
        // TODO : 선택된 카드를 덱 목록에서 제거
    }

    // 팝업 닫고 게임으로 돌아가기
    public void HandleReturnGame()
    {
        isOpenPrefareDeckPopUp = false;
        isOpenTrashDeckPopUp = false;
        if(isOpenCardOnHandRemovePopUp){
            DeckUI.instance.buttonPrefareDeck.transform.SetParent(CardOnHandRemovePopUp.transform);
            DeckUI.instance.buttonTrashDeck.transform.SetParent(CardOnHandRemovePopUp.transform);
            M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHandOverPopUp");
        }else{
            DeckUI.instance.buttonPrefareDeck.transform.SetParent(PrefareDeck.transform);
            DeckUI.instance.buttonTrashDeck.transform.SetParent(TrashDeck.transform);
            M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHand");
        }
        DeckUI.instance.buttonPrefareDeck.transform.SetSiblingIndex(originSiblingIndex);
        DeckUI.instance.buttonTrashDeck.transform.SetSiblingIndex(originSiblingIndex);
        DeckListPopUp.gameObject.SetActive(false);
        ClearDeckList();
    }

    // Deck정보 리스트 요소 추가
    private void AddDeckList(SyncList<Card> cards, GridLayoutGroup gridLayoutGroup)
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
}
