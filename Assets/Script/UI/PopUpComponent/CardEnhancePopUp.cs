using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;


public class CardEnhancePopUp : SingletonD<CardEnhancePopUp>
{
    public List<GameObject> enhanceableCards = new List<GameObject>(); // 강화 가능 카드 오브젝트 리스트
    public List<GameObject> enhancePreivewCards = new List<GameObject>(); // 강화 프리뷰창 카드 오브젝트 리스트
    public string selectCardGuid;

    public Image[] enhanceProgressArrows; // 강화 진행 상태 표시 화살표 이미지 배열
    private Coroutine enhanceProgressCoroutine;
    private Coroutine enhanceOutlineCoroutine;
    public Material cardEnhancedOutline; // 강화 카드 머티리얼

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
        gridLayoutGroup.gameObject.SetActive(false);
    }

    // 카드 강화 프리뷰창 비활성화
    public void HandleCardEnhancePreviewHide()
    {
        cardEnhancePreview.SetActive(false);
        gridLayoutGroup.gameObject.SetActive(true);
        foreach(GameObject card in enhancePreivewCards){
            Destroy(card);
        }
        enhancePreivewCards.Clear();
        selectCardGuid = string.Empty;
        ResetEnhanceProgress();
    }

    // 카드 강화 승인
    private void HandleClickCardEnhnaceOk()
    {
        buttonEnhanceOk.gameObject.SetActive(false);
        buttonEnhanceCancel.gameObject.SetActive(false);
        ClearAllEnhanceableCards();
        enhanceProgressCoroutine = StartCoroutine(EnhanceProgress(() => {
            enhanceOutlineCoroutine = StartCoroutine(EnhancedOutline(() => {
                SyncList<Card> deck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().deck;
                int index = deck.FindIndex(c => c.guid.Equals(selectCardGuid));
                if(index != -1){
                    deck[index].isEnhanced = true;
                    buttonEnhanceOk.gameObject.SetActive(true);
                    buttonEnhanceCancel.gameObject.SetActive(true);
                    HandleCardEnhancePreviewHide();
                    CreateEnhanceableCards();
                }
            }));
        }));
    }

    // 카드 강화 취소
    private void HandleClickCardEnhnaceCancel()
    {
        HandleCardEnhancePreviewHide();
    }

    // 강화된 카드 외곽선 머티리얼 변경
    private IEnumerator EnhancedOutline(System.Action callback = null)
    {
        Material enhancedMaterial = new Material(cardEnhancedOutline);
        CardOnDeck enhancedCardOnDeck = enhancePreivewCards[1].GetComponent<CardOnDeck>();
        enhancedCardOnDeck.cardBackground.material = enhancedMaterial;
        float duration = 1.5f;
        float timer = 0f;
        while (timer < duration)
        {
            float outlineRatio = timer / duration;
            enhancedMaterial.SetFloat("_OutlineWidth2", outlineRatio);
            timer += Time.deltaTime;
            yield return null;
        }
        if(callback != null){
            callback();
        }
    }

    // 강화 진행 표시 코루틴 시작 및 화살표 색상 녹색으로 순차 변경
    private IEnumerator EnhanceProgress(System.Action callback = null)
    {
        for(int i=0; i<enhanceProgressArrows.Length; i++){
            enhanceProgressArrows[i].color = Color.green;
            yield return new WaitForSeconds(0.15f);
        }
        if(callback != null){
            callback();
        }
    }

    // 강화 진행 표시 코루틴 중지 및 화살표 색상 초기화
    private void ResetEnhanceProgress()
    {
        if(enhanceProgressCoroutine != null){
            StopCoroutine(enhanceProgressCoroutine);
        }
        if(enhanceOutlineCoroutine != null){
            StopCoroutine(enhanceOutlineCoroutine);
        }
        for(int i=0; i<enhanceProgressArrows.Length; i++){
            enhanceProgressArrows[i].color = Color.white;
        }
    }

    // 카드 강화 프리뷰에 사용될 카드 오브젝트 생성
    public void CreateEnhancePreviewCard(Card card)
    {
        // 강화 이전 카드 프리뷰 오브젝트
        GameObject previousCardObject = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab, previousCardPosition.transform.position, Quaternion.identity);
        previousCardObject.transform.SetParent(cardEnhancePreview.transform);
        previousCardObject.transform.localScale = Vector3.one;
        CardOnDeck previousCard = previousCardObject.GetComponent<CardOnDeck>();
        previousCard.card = card.CardDeepCopy(false);
        previousCard.isEnhancedPreviewCard = true;
       
        // 강화 이후 카드 프리뷰 오브젝트
        GameObject afterCardObject = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab, afterCardPosition.transform.position, Quaternion.identity);
        afterCardObject.transform.SetParent(cardEnhancePreview.transform);
        afterCardObject.transform.localScale = Vector3.one;
        CardOnDeck afterCard = afterCardObject.GetComponent<CardOnDeck>();
        afterCard.card = card.CardDeepCopy(false);
        afterCard.card.isEnhanced = true; // 강화카드
        afterCard.isEnhancedPreviewCard = true;
        
        enhancePreivewCards.Add(previousCardObject);
        enhancePreivewCards.Add(afterCardObject);
    }

    // 현재 플레이어의 deck 데이터로 카드 오브젝트 생성(이미 강화된 카드는 제외)
    private void CreateEnhanceableCards()
    {
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

    // CardEnhnacePopUp의 카드 오브젝트 제거
    private void ClearAllEnhanceableCards()
    {
        foreach(GameObject cardObject in enhanceableCards){
            Destroy(cardObject);
        }
        enhanceableCards.Clear();
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // CardEnhancePopUp 활성화 콜백
    public void OnCardEnhancePopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        CreateEnhanceableCards();
        PopUpUIManager.instance.HandleMercuriusPopUp(false); // 카드 강화 팝업 활성화 될때 상점 팝업은 비활성화
    }

    // CardEnhancePopUp 비활성화 콜백
    public void OnCardEnhancePopUpHide()
    {
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            ClearAllEnhanceableCards();
            gameObject.SetActive(false);
        });
        PopUpUIManager.instance.HandleMercuriusPopUp(true); // 카드 강화 팝업 비활성화 될때 상점 팝업은 활성화
    }
}
