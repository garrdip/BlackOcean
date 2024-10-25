using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ProjectD;
using DG.Tweening;
using Mirror;
using TMPro;


public class DeckSelectPopUp : SingletonD<DeckSelectPopUp>
{
    [Header("댁 리스트")]
    public List<GameObject> deckList = new List<GameObject>();
    public List<GameObject> selectCardObjectList = new List<GameObject>();
    public List<Card> selectCards = new List<Card>();
    public string requestCardNumber; // 어떤 카드의 요청으로 팝업이 호출됬는지 확인을 위한 변수

    [Header("UI 컴포넌트")]
    public CanvasGroup canvasGroup;
    public GridLayoutGroup deckListPopUpGrid;
    public TextMeshProUGUI textTitle;
    public TextMeshProUGUI textExplanation;
    public Button buttonSelectSubmit;

    
    protected override void Awake()
    {
        PopUpUIManager.instance.onDeckSelectPopUpShow += OnDeckSelectPopUpShow;
        PopUpUIManager.instance.onDeckSelectPopUpHide += OnDeckSelectPopUpHide;
    }

    void Start()
    {
        GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
        textExplanation.text = $"카드를 <color=green>{gamePlayerDeck.maxSelectableCardCount}</color> 장 선택하세요.\n( 마우스 왼쪽 버튼 클릭 시 선택, 오른쪽 버튼 클릭 시 해제 됩니다. )";
        buttonSelectSubmit.onClick.AddListener(OnClickDeckSelectSubmit);
    }

    private void AddDeckList(SyncList<Card> selectCards, GridLayoutGroup gridLayoutGroup)
    {
        ClearDeckList();
        foreach(Card card in selectCards){
            GameObject cardObject = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab);
            cardObject.transform.SetParent(gridLayoutGroup.transform);
            cardObject.transform.localScale = Vector3.one;
            CardOnDeck cardOnDeck =cardObject.GetComponent<CardOnDeck>();
            cardOnDeck.card = card;
            deckList.Add(cardObject);
        }
    }

    private void ClearDeckList()
    {
        for(int i=deckList.Count-1; i >=0; i--){
            Destroy(deckList[i]);
            deckList.RemoveAt(i);
        }
        for(int i=selectCardObjectList.Count-1; i >=0; i--){
            Destroy(selectCardObjectList[i]);
            selectCardObjectList.RemoveAt(i);
        }
        selectCards.Clear();
    }

    public void ChangeCardOnDeckSelectState(GameObject selectCardObject, bool isSelect)
    {
        GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
        CardOnDeck cardOnDeck = selectCardObject.GetComponent<CardOnDeck>();
        if(isSelect){
            if(selectCardObjectList.Count < gamePlayerDeck.maxSelectableCardCount){
                if(!IsDuplicateSelected(cardOnDeck.card.guid)){
                    selectCardObjectList.Add(selectCardObject);
                    selectCards.Add(cardOnDeck.card);
                    cardOnDeck.cardBackground.color = Color.red;
                }
            }else{
                M_MessageManager.instance
                    .MakeToast()
                    .Position(ToastPosition.Top)
                    .FadeInTime(0.5f)
                    .FadeOutTime(0.5f)
                    .MessageBoxColor(Color.red)
                    .TextColor(Color.red)
                    .Text(Const.ERR_NO_MORE_SELECTABLE_CARD)
                    .Show();
            } 
        }else{
            selectCardObjectList.Remove(selectCardObject);
            selectCards.Remove(cardOnDeck.card);
            cardOnDeck.cardBackground.color = Color.white;
        }
    }

    // 중복 선택 체크
    private bool IsDuplicateSelected(string guid)
    {
        foreach(Card card in selectCards){
            if(card.guid.Equals(guid)){
                return true;
            }
        }
        return false;
    }

    // DeckSelectPopUp 확인 버튼 클릭
    public void OnClickDeckSelectSubmit()
    {
        GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>(); 
        gamePlayerDeck.CmdCheckRequestCard(requestCardNumber, selectCards);
        PopUpUIManager.instance.HandleHideDeckSelectPopUp();
    }
    
    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    public void OnDeckSelectPopUpShow(DeckListType type, string cardNumber)
    {
        requestCardNumber = cardNumber;
        canvasGroup.DOFade(1.0f, 0.5f);
        GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
        switch(type){
            case DeckListType.PREFARE_DECK:
                textTitle.text = Const.PREFARE_DECK;
                // 로컬 플레이어의 PrefareDeck 조회
                if(NetworkClient.connection != null && NetworkClient.active){
                    AddDeckList(gamePlayerDeck.prefareDeck, deckListPopUpGrid);
                }
                break;
            case DeckListType.TRASH_DECK:
                textTitle.text = Const.TRASH_DECK;
                // 로컬 플레이어의 TrashDeck 조회
                if(NetworkClient.connection != null && NetworkClient.active){
                    AddDeckList(gamePlayerDeck.trashDeck, deckListPopUpGrid);
                }
                break;
            case DeckListType.FORGOTTEN_DECK:
                textTitle.text = Const.FORGOTTEN_DECK;
                // 로컬 플레이어의 ForgottenDeck 조회
                if(NetworkClient.connection != null && NetworkClient.active){
                    AddDeckList(gamePlayerDeck.forgottenDeck, deckListPopUpGrid);
                }
                break;
        }
    }

    public void OnDeckSelectPopUpHide()
    {
        GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
        gamePlayerDeck.CmdResetMaxCardSelectableCount(); // 카드 선택 가능 갯수 초기화
        ClearDeckList();
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
        });
    }
}
