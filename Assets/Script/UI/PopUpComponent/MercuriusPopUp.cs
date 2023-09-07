using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MercuriusPopUp : SingletonD<MercuriusPopUp>, IPointerClickHandler
{
    public CanvasGroup canvasGroup;
    public GridLayoutGroup gridLayoutGroup;
    public GameObject frame;
    public TextMeshProUGUI textCardEnhancePrice;
    public TextMeshProUGUI textCardRemovePrice;
    public bool isMouseOnFrame = false;
    public List<Card> storeCards = new List<Card>();
    public List<GameObject> storeCardObjectList = new List<GameObject>();


    protected override void Awake()
    {
        PopUpUIManager.instance.onMercuriusPopUpShow += OnMercuriusPopUpShow;
        PopUpUIManager.instance.onMercuriusPopUpHide += OnMercuriusPopUpHide;
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(!isMouseOnFrame){
            PopUpUIManager.instance.HandleMercuriusPopUp(false);
        }
    }

    // 상점 카드 정보 리스트 요소 추가
    private void AddDeckList(List<Card> cards, GridLayoutGroup gridLayoutGroup)
    {
        ClearDeckList();
        foreach(Card card in cards){
            // 상점 카드 슬롯(최상단 부모 오브젝트)
            GameObject cardShopSlot = Instantiate(PopUpUIManager.instance.CardShopSlot);
            cardShopSlot.transform.SetParent(gridLayoutGroup.transform);
            cardShopSlot.transform.localScale = new Vector3(1, 1, 1);

            // 상점 카드
            GameObject cardOnDeck = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab);
            cardOnDeck.transform.SetParent(cardShopSlot.transform);
            cardOnDeck.transform.localScale = new Vector3(1, 1, 1);
            cardOnDeck.GetComponent<CardOnDeck>().card = card;

            // 상점 카드 가격 아이콘 + 텍스트
            GameObject cardShopPrice = Instantiate(PopUpUIManager.instance.CardShopPrice);
            cardShopPrice.transform.SetParent(cardShopSlot.transform);
            cardShopPrice.transform.localScale = new Vector3(1, 1, 1);

            TextMeshProUGUI textPrice = cardShopPrice.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            textPrice.text = "100";

            storeCardObjectList.Add(cardShopSlot);
        }
    }

    // 상점 카드 정보 리스트 요소 제거
    private void ClearDeckList()
    {
        for(int i=storeCardObjectList.Count-1; i >=0; i--){
            Destroy(storeCardObjectList[i]);
            storeCardObjectList.RemoveAt(i);
        }
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // MercuriusPopUp 활성화 콜백
    public void OnMercuriusPopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        GameUIManager.instance.GameUI.gameObject.SetActive(false);
        AddDeckList(storeCards, gridLayoutGroup);
        textCardEnhancePrice.text = "100";
        textCardRemovePrice.text = "100";
    }

    // MercuriusPopUp 비활성화 콜백
    public void OnMercuriusPopUpHide()
    {
        GameUIManager.instance.GameUI.gameObject.SetActive(true);
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            ClearDeckList();
            gameObject.SetActive(false);
        });
    }
}
