using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;

public class CardRemovePopUp : SingletonD<CardRemovePopUp>
{
    public List<GameObject> removableCards = new List<GameObject>();
    public List<GameObject> removePreviewCards = new List<GameObject>();
    public GridLayoutGroup gridLayoutGroup;
    
    public GameObject cardRemovePreview;

    public CanvasGroup canvasGroup; 
    public Button buttonCardRemoveOk;
    public Button buttonCardRemoveCancel;


    protected override void Awake()
    {
        PopUpUIManager.instance.onCardRemovePopUpShow += OnCardRemovePopUpShow;
        PopUpUIManager.instance.onCardRemovePopUpHide += OnCardRemovePopUpHide;
    }

    void Start()
    {
        buttonCardRemoveOk.onClick.AddListener(() => HandleClickCardRemoveOk());
        buttonCardRemoveCancel.onClick.AddListener(() => HandleClickCardRemoveCancel());
    }

    public void HandleCardEnhancePreviewOpen()
    {
        cardRemovePreview.SetActive(true);
        cardRemovePreview.GetComponent<CanvasGroup>().DOFade(1f, 0.5f);
        gridLayoutGroup.gameObject.SetActive(false);
    }

    public void HandleCardRemovePreviewHide()
    {
        cardRemovePreview.GetComponent<CanvasGroup>().DOFade(0f, 0.5f).OnComplete(() => {
            cardRemovePreview.SetActive(false);
        });
        gridLayoutGroup.gameObject.SetActive(true);
        foreach(GameObject card in removePreviewCards){
            Destroy(card);
        }
        removePreviewCards.Clear();
        foreach(GameObject card in removableCards){
            card.transform.localScale = Vector3.one;
        }
    }

    private void HandleClickCardRemoveOk()
    {
        HandleCardRemovePreviewHide();
        PopUpUIManager.instance.HandleCardRemovePopUp(false);
        // TODO : 선택한 카드 deck SyncList에서 찾아서 해당 카드 데이터 제거
    }

    private void HandleClickCardRemoveCancel()
    {
        HandleCardRemovePreviewHide();
    }

    public void CreateRemovePreviewCard(Card card)
    {
        GameObject removeCardObject = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab, new Vector3(0f, 2f, 0f), Quaternion.identity);
        removeCardObject.transform.SetParent(cardRemovePreview.transform);
        removeCardObject.transform.localScale = Vector3.one;
        CardOnDeck removeCard = removeCardObject.GetComponent<CardOnDeck>();
        removeCard.card = card;
        removeCard.isRemovePreviewCard = true;
        removePreviewCards.Add(removeCardObject);
    }
    
    // CardRemovePopUp 활성화 콜백
    public void OnCardRemovePopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        PopUpUIManager.instance.HandleMercuriusPopUp(false); // 카드 제거 팝업 활성화 될때 상점 팝업은 비활성화
        // 현재 플레이어의 deck 데이터로 카드 오브젝트 생성
        foreach(Card card in NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().deck){
            GameObject cardObject = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab, Vector3.zero, Quaternion.identity);
            CardOnDeck cardOnDeck = cardObject.GetComponent<CardOnDeck>();
            cardOnDeck.transform.SetParent(gridLayoutGroup.transform);
            cardOnDeck.transform.localScale = Vector3.one;
            cardOnDeck.card = card;
            removableCards.Add(cardObject);
        }
    }

    // CardRemovePopUp 비활성화 콜백
    public void OnCardRemovePopUpHide()
    {
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            foreach(GameObject cardObject in removableCards){
                Destroy(cardObject);
            }
            removableCards.Clear();
            gameObject.SetActive(false);
        });
        PopUpUIManager.instance.HandleMercuriusPopUp(true); // 카드 제거 팝업 비활성화 될때 상점 팝업은 활성화
    }
}
