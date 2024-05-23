using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;


public class CardEnhancePopUp : SingletonD<CardEnhancePopUp>
{
    public List<GameObject> enhanceableCards = new List<GameObject>();
    public List<GameObject> enhancePreivewCards = new List<GameObject>();
    public string selectCardGuid;

    public GameObject cardEnhancePreview;
    public GameObject previousCardPosition;
    public GameObject afterCardPosition;

    public CanvasGroup canvasGroup;
    public GridLayoutGroup gridLayoutGroup;
    public Button buttonEnhanceOk;
    public Button buttonEnhanceCancel;


    protected override void Awake()
    {
        PopUpUIManager.instance.onCardEnhancePopUpShow += OnCardEnhancePopUpShow;
        PopUpUIManager.instance.onCardEnhancePopUpHide += OnCardEnhancePopUpHide;
    }
    
    void Start()
    {
        buttonEnhanceOk.onClick.AddListener(() => HandleClickCardEnhnaceOk());
        buttonEnhanceCancel.onClick.AddListener(() => HandleClickCardEnhnaceCancel());
    }

    // 카드 강화 프리뷰창 활성화
    public void HandleCardEnhancePreviewOpen()
    {
        cardEnhancePreview.SetActive(true);
        cardEnhancePreview.GetComponent<CanvasGroup>().DOFade(1f, 0.5f);
        gridLayoutGroup.gameObject.SetActive(false);
    }

    // 카드 강화 프리뷰창 비활성화
    public void HandleCardEnhancePreviewHide()
    {
        cardEnhancePreview.GetComponent<CanvasGroup>().DOFade(0f, 0.5f).OnComplete(() => {
            cardEnhancePreview.SetActive(false);
        });
        gridLayoutGroup.gameObject.SetActive(true);
        foreach(GameObject card in enhancePreivewCards){
            Destroy(card);
        }
        enhancePreivewCards.Clear();
        foreach(GameObject card in enhanceableCards){
            card.transform.localScale = Vector3.one;
        }
        selectCardGuid = string.Empty;
    }

    // 카드 강화 승인
    private void HandleClickCardEnhnaceOk()
    {
        SyncList<Card> deck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().deck;
        int index = deck.FindIndex(c => c.guid.Equals(selectCardGuid));
        if(index != -1){
            HandleCardEnhancePreviewHide();
            PopUpUIManager.instance.HandleCardEnhancePopUp(false);
            deck[index].isEnhanced = true;
        }
    }

    // 카드 강화 취소
    private void HandleClickCardEnhnaceCancel()
    {
        HandleCardEnhancePreviewHide();
    }

    // 카드 강화 프리뷰에 사용될 카드 오브젝트 생성
    public void CreateEnhancePreviewCard(Card card)
    {
        // 강화 이전 카드 프리뷰 오브젝트
        GameObject previousCardObject = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab, previousCardPosition.transform.position, Quaternion.identity);
        previousCardObject.transform.SetParent(cardEnhancePreview.transform);
        previousCardObject.transform.localScale = Vector3.one;
        CardOnDeck previousCard = previousCardObject.GetComponent<CardOnDeck>();
        previousCard.card = card;
        previousCard.isEnhancedPreviewCard = true;
       
        // 강화 이후 카드 프리뷰 오브젝트
        GameObject afterCardObject = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab, afterCardPosition.transform.position, Quaternion.identity);
        afterCardObject.transform.SetParent(cardEnhancePreview.transform);
        afterCardObject.transform.localScale = Vector3.one;
        CardOnDeck afterCard = afterCardObject.GetComponent<CardOnDeck>();
        afterCard.card = new Card(CardData.instance.cards.Find(c => c.cardNumber.Equals(card.baseCard.cardNumber + "_E"))); // 카드 DB에서 해당카드 강화버전 조회
        afterCard.isEnhancedPreviewCard = true;
        
        enhancePreivewCards.Add(previousCardObject);
        enhancePreivewCards.Add(afterCardObject);
    }

    // CardEnhancePopUp 활성화 콜백
    public void OnCardEnhancePopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        PopUpUIManager.instance.HandleMercuriusPopUp(false); // 카드 강화 팝업 활성화 될때 상점 팝업은 비활성화
        // 현재 플레이어의 deck 데이터로 카드 오브젝트 생성(이미 강화된 카드는 제외)
        foreach(Card card in NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().deck){
            if(!card.isEnhanced){
                GameObject cardObject = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab, Vector3.zero, Quaternion.identity);
                CardOnDeck cardOnDeck = cardObject.GetComponent<CardOnDeck>();
                cardOnDeck.transform.SetParent(gridLayoutGroup.transform);
                cardOnDeck.transform.localScale = Vector3.one;
                cardOnDeck.card = card;
                enhanceableCards.Add(cardObject);
            }
        }
    }

    // CardEnhancePopUp 비활성화 콜백
    public void OnCardEnhancePopUpHide()
    {
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            foreach(GameObject cardObject in enhanceableCards){
                Destroy(cardObject);
            }
            enhanceableCards.Clear();
            gameObject.SetActive(false);
        });
        PopUpUIManager.instance.HandleMercuriusPopUp(true); // 카드 강화 팝업 비활성화 될때 상점 팝업은 활성화
    }
}
