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
    public string selectCardGuid;

    public GameObject cardRemovePreview;

    public CanvasGroup canvasGroup; 
    public GridLayoutGroup gridLayoutGroup;
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

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    // 카드 제거 승인
    private void HandleClickCardRemoveOk()
    {
        MercuriusPopUp mercuriusPopUp = PopUpUIManager.instance.mercuriusPopUp.GetComponent<MercuriusPopUp>();
        GamePlayerDeck gamePlayerDeck = mercuriusPopUp.tabLayout.GetSelectedGamePlayerDeck();
        if(gamePlayerDeck != null){
            gamePlayerDeck.CmdRemoveDeck(selectCardGuid); // 카드 제거 커맨드 전송
            StartCardRemoveAnimation();
        }
    }

    // 카드 제거 애니매이션
    private void StartCardRemoveAnimation()
    {
        CardOnDeck cardOnDeck = removePreviewCards[0].GetComponent<CardOnDeck>();
        buttonCardRemoveOk.gameObject.SetActive(false);
        buttonCardRemoveCancel.gameObject.SetActive(false);
        StartCoroutine(
            cardOnDeck.CardOnDeckDissolve(() => {
                buttonCardRemoveOk.gameObject.SetActive(true);
                buttonCardRemoveCancel.gameObject.SetActive(true);
                HandleCardRemovePreviewHide();
            })
        );
    }

    // 카드 제거 취소
    private void HandleClickCardRemoveCancel()
    {
        HandleCardRemovePreviewHide();
    }

    // 카드 제거 프리뷰창 활성화
    public void HandleCardRemovePreviewOpen()
    {
        cardRemovePreview.SetActive(true);
        gridLayoutGroup.gameObject.SetActive(false);
    }

    // 카드 제거 프리뷰창 비활성화
    public void HandleCardRemovePreviewHide()
    {
        cardRemovePreview.SetActive(false);
        gridLayoutGroup.gameObject.SetActive(true);
        foreach(GameObject card in removePreviewCards){
            Destroy(card);
        }
        removePreviewCards.Clear();
        selectCardGuid = string.Empty;
    }

    // 카드 제거 프리뷰에 사용될 카드 오브젝트 생성
    public void CreateRemovePreviewCard(Card card)
    {
        GameObject removeCardObject = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab, new Vector3(0f, 2f, 0f), Quaternion.identity);
        removeCardObject.transform.SetParent(cardRemovePreview.transform);
        removeCardObject.transform.localScale = Vector3.one;
        CardOnDeck removeCard = removeCardObject.GetComponent<CardOnDeck>();
        removeCard.card = card.CardDeepCopy(false);
        removeCard.isRemovePreviewCard = true;
        removePreviewCards.Add(removeCardObject);
    }

    // 플레이어의 deck 데이터로 카드 오브젝트 생성
    public void CreateRemoveableCards(SyncList<Card> deck)
    {
        foreach(Card card in deck){
            GameObject cardObject = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab, Vector3.zero, Quaternion.identity);
            CardOnDeck cardOnDeck = cardObject.GetComponent<CardOnDeck>();
            cardOnDeck.transform.SetParent(gridLayoutGroup.transform);
            cardOnDeck.transform.localScale = Vector3.one;
            cardOnDeck.card = card.CardDeepCopy(false);
            removableCards.Add(cardObject);
        }
    }

    // CardRemovePopUp의 카드 오브젝트 제거
    public void ClearRemoveableCards()
    {
        foreach(GameObject cardObject in removableCards){
            Destroy(cardObject);
        }
        removableCards.Clear();
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //
    
    // CardRemovePopUp 활성화 콜백
    public void OnCardRemovePopUpShow()
    {
        MercuriusPopUp mercuriusPopUp = PopUpUIManager.instance.mercuriusPopUp.GetComponent<MercuriusPopUp>();
        GamePlayerDeck gamePlayerDeck = mercuriusPopUp.tabLayout.GetSelectedGamePlayerDeck();
        if(gamePlayerDeck != null){
            CreateRemoveableCards(gamePlayerDeck.deck);
        }
        canvasGroup.DOFade(1.0f, 0.5f);
        PopUpUIManager.instance.HandleMercuriusPopUp(false);
    }

    // CardRemovePopUp 비활성화 콜백
    public void OnCardRemovePopUpHide()
    {
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            ClearRemoveableCards();
            gameObject.SetActive(false);
        });
        PopUpUIManager.instance.HandleMercuriusPopUp(true);
    }
}
