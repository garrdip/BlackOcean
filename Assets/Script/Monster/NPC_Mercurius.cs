using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using ProjectD;
using Mirror;
using TMPro;

public class NPC_Mercurius : SpawnedMonster
{
    public MercuriusPopUp mercuriusPopUp;
    
    [SyncVar]
    public bool isOrigin = false; // 원본 오브젝트인지 구분값(타겟오브젝트들은 원본과 클론이 존재해서 둘중 하나만 호출되어야 함)

    public List<Card> shopCards = new List<Card>();
    public List<GameObject> shopCardObjectList = new List<GameObject>();

    void Awake()
    {
        mercuriusPopUp = PopUpUIManager.instance.mercuriusPopUp.GetComponent<MercuriusPopUp>();
    }

    // NPC_Mercurius 각 클라이언트에 생성될 때 현재 플레이어의 카드 데이터에서 6개의 랜덤 카드데이터를 추출하여 팝업에 6개의 상점카드 세팅
    public override void OnStartClient()
    {
        if(isOrigin && mercuriusPopUp != null){
            InitShopCard();
        }
    }

    // NPC_Mercurius 파괴될 때 리스트 비움
    void OnDestroy()
    {
        if(mercuriusPopUp != null){
            shopCards.Clear();
            for(int i= shopCardObjectList.Count-1; i >=0; i--){
                Destroy(shopCardObjectList[i]);
                shopCardObjectList.RemoveAt(i);
            }
        }
    }

    // 카드 매니저에서 6장의 카드를 추출하여 상점 카드 생성
    private void InitShopCard()
    {
        foreach(Card card in  M_CardManager.instance.ExtractRandomCards(6)){                    
            // 상점 카드 슬롯(최상단 부모 오브젝트)
            GameObject cardShopSlot = Instantiate(PopUpUIManager.instance.CardShopSlot);
            cardShopSlot.transform.SetParent(mercuriusPopUp.gridLayoutGroup.transform);
            cardShopSlot.transform.localScale = new Vector3(1, 1, 1);

            // 상점 카드
            GameObject cardOnDeck = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab);
            cardOnDeck.transform.SetParent(cardShopSlot.transform);
            cardOnDeck.transform.localScale = new Vector3(1, 1, 1);
            cardOnDeck.GetComponent<CardOnDeck>().card = card;
            if(cardOnDeck.GetComponent<CardOnDeck>().isSoldOut){
                cardOnDeck.GetComponent<CardOnDeck>().canvasGroup.alpha = 0.5f;
            }

            // 상점 카드 가격 아이콘 + 텍스트
            GameObject cardShopPrice = Instantiate(PopUpUIManager.instance.CardShopPrice);
            cardShopPrice.transform.SetParent(cardShopSlot.transform);
            cardShopPrice.transform.localScale = new Vector3(1, 1, 1);

            TextMeshProUGUI textPrice = cardShopPrice.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            textPrice.text = "100";

            shopCards.Add(card);
            shopCardObjectList.Add(cardShopSlot);
        }
    }

    void OnMouseDown()
    {
        if(!EventSystem.current.IsPointerOverGameObject()){ // NPC_Mercurius가 UI에 가려져 있을 경우(팝업이 활성화 된 경우) 클릭 이벤트 방지
            if(M_TurnManager.instance.phase == BattleTurn.NONE_BATTLE_SCENE){
                PopUpUIManager.instance.HandleMercuriusPopUp(true);
            }
        }
    }
}
