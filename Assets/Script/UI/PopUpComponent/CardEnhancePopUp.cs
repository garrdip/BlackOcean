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

    public void HandleCardEnhancePreviewOpen()
    {
        cardEnhancePreview.SetActive(true);
        cardEnhancePreview.GetComponent<CanvasGroup>().DOFade(1f, 0.5f);
        gridLayoutGroup.gameObject.SetActive(false);
    }

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
    }

    private void HandleClickCardEnhnaceOk()
    {
        HandleCardEnhancePreviewHide();
        PopUpUIManager.instance.HandleCardEnhancePopUp(false);
        // TODO : 선택한 카드 deck SyncList에서 찾아서 해당 카드 강화데이터로 변경
    }

    private void HandleClickCardEnhnaceCancel()
    {
        HandleCardEnhancePreviewHide();
    }

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
        afterCard.card = new Card(CardData.instance.cards.Find(c => c.cardNumber.Equals(card.baseCard.cardNumber + "_E")));
        afterCard.isEnhancedPreviewCard = true;
        
        enhancePreivewCards.Add(previousCardObject);
        enhancePreivewCards.Add(afterCardObject);
    }

    // CardEnhancePopUp 활성화 콜백
    public void OnCardEnhancePopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        PopUpUIManager.instance.HandleMercuriusPopUp(false); // 카드 강화 팝업 활성화 될때 상점 팝업은 비활성화
        // 현재 플레이어의 deck 데이터로 카드 오브젝트 생성
        foreach(Card card in NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().deck){
            GameObject cardObject = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab, Vector3.zero, Quaternion.identity);
            CardOnDeck cardOnDeck = cardObject.GetComponent<CardOnDeck>();
            cardOnDeck.transform.SetParent(gridLayoutGroup.transform);
            cardOnDeck.transform.localScale = Vector3.one;
            cardOnDeck.card = card;
            enhanceableCards.Add(cardObject);
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
