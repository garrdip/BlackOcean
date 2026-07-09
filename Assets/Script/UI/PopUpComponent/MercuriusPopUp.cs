using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using TMPro;

public class MercuriusPopUp : SingletonD<MercuriusPopUp>, IPointerClickHandler
{
    public CanvasGroup canvasGroup;
    public GameObject frameLayout;
    public bool isMouseOnFrame = false;
    public List<GameObject> shopCardObjectList = new List<GameObject>(); // 상점 카드 오브젝트 리스트
    public List<GridLayoutGroup> grids = new List<GridLayoutGroup>();
    public TabLayout tabLayout;
    public Button buttonClose;
    public GameObject buttonCloseLight;
    public CanvasGroup cardInfoCanvasGroup;
    public CardOnDeck hoveredCardOnDeck;


    protected override void Awake()
    {
        PopUpUIManager.instance.onMercuriusPopUpShow += OnMercuriusPopUpShow;
        PopUpUIManager.instance.onMercuriusPopUpHide += OnMercuriusPopUpHide; 
    }

    void Start()
    {
        buttonClose.onClick.AddListener(() => PopUpUIManager.instance.HandleMercuriusPopUp(false));
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    // mercuriusPop의 PointerClick 이벤트
    public void OnPointerClick(PointerEventData eventData)
    {
        if(!tabLayout.isMouseOnFrame){
            PopUpUIManager.instance.HandleMercuriusPopUp(false);
        }
    }

    // 마우스 오버된 상점카드 정보 활성화
    public void ShowHoverdCardInfo(Card card)
    {
        Card hoverdCard = new Card(card.baseCard);
        hoveredCardOnDeck.card = hoverdCard;
        hoveredCardOnDeck.initCardData(hoverdCard);
        hoveredCardOnDeck.InitCardTemplateByCharacter(hoverdCard);
        cardInfoCanvasGroup.DOFade(1f, 0.3f);  
    }

    // 마우스 오버된 상점카드 정보 비활성화
    public void HideHoverdCardInfo()
    {
        hoveredCardOnDeck.card = null;
        cardInfoCanvasGroup.DOFade(0f, 0.3f);  
    }

    // 각 플레이어들의 상점 카드 오브젝트 생성
    public void CreateShopCards()
    {
        for(int i=0; i<M_TurnManager.instance.playerOrder.Count; i++){
            uint netId = M_TurnManager.instance.playerOrder[i];
            if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayerDeck gamePlayerDeck = networkIdentity.GetComponent<GamePlayerDeck>();
                // 각 플레이어들의 shopCards synclist 데이터로 상점 카드 오브젝트 생성
                foreach(Card card in gamePlayerDeck.shopCards){
                    // 상점 카드 슬롯(최상단 부모 오브젝트)
                    GameObject cardShopSlot = Instantiate(PopUpUIManager.instance.CardShopSlot,Vector3.zero, Quaternion.identity);
                    cardShopSlot.transform.SetParent(grids[i].transform);
                    cardShopSlot.transform.localScale = Vector3.one;
                    cardShopSlot.transform.localPosition = Vector3.zero;

                    // 상점 카드
                    GameObject cardOnDeckObject = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab, Vector3.zero, Quaternion.identity);
                    cardOnDeckObject.transform.SetParent(cardShopSlot.transform);
                    cardOnDeckObject.transform.localScale = Vector3.one;
                    cardOnDeckObject.transform.localPosition = Vector3.zero;
                    CardOnDeck cardOnDeck = cardOnDeckObject.GetComponent<CardOnDeck>();
                    cardOnDeck.card = card;
                    cardOnDeck.cardOwner =  networkIdentity.GetComponent<GamePlayer>();

                    // 상점 카드 가격 아이콘 + 텍스트
                    GameObject cardShopPrice = Instantiate(PopUpUIManager.instance.CardShopPrice, Vector3.zero, Quaternion.identity);
                    cardShopPrice.transform.SetParent(cardShopSlot.transform);
                    cardShopPrice.transform.localScale = Vector3.one;
                    cardShopPrice.transform.localPosition = new Vector3(0f, 30f, 0f);

                    TextMeshProUGUI textPrice = cardShopPrice.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                    textPrice.text = card.cardPrice.ToString();

                    shopCardObjectList.Add(cardShopSlot);
                }
            }
        }
    }

    // 상점 카드 오브젝트 제거
    public void RemoveShopCards()
    {
        for(int i = shopCardObjectList.Count - 1; i >= 0; i--){
            Destroy(shopCardObjectList[i]);
            shopCardObjectList.RemoveAt(i);
        }
    }

    // -------------------------------------------------------------------  이벤트 트리거 함수 -------------------------------------------------------------------------- //

    public void OnPointerEnterCloseButton()
    {
        buttonCloseLight.SetActive(true);
    }

    public void OnPointerExitCloseButton()
    {
        buttonCloseLight.SetActive(false);
    }


    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // MercuriusPopUp 활성화 콜백
    public void OnMercuriusPopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        CreateShopCards();
        tabLayout.ShowTab(PlayerRegistry.Local.currentGamePlayer.GetComponent<GamePlayer>().selectOrder);
    }

    // MercuriusPopUp 비활성화 콜백
    public void OnMercuriusPopUpHide()
    {
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
            isMouseOnFrame = false;
        });
        RemoveShopCards();
    }
}
