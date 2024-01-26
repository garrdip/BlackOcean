using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using ProjectD;
using Mirror;
using Spine.Unity;
using TMPro;

public class NPC_Mercurius : SpawnedMonster
{
    public MercuriusPopUp mercuriusPopUp;
    public List<GameObject> shopCardObjectList = new List<GameObject>();
    private SkeletonAnimation toddAnim;
    private SkeletonAnimation backAnim;
    private SkeletonAnimation minion0Anim;
    private SkeletonAnimation minion1Anim;
    private SkeletonAnimation minion2Anim;
    private SkeletonAnimation minion3Anim;
    
    [SyncVar]
    public bool isOrigin = false; // 원본 오브젝트인지 구분값(타겟오브젝트들은 원본과 클론이 존재해서 둘중 하나만 호출되어야 함)
    public readonly SyncDictionary<GamePlayer, List<Card>> shopCardDictionary = new  SyncDictionary<GamePlayer, List<Card>>(); // 각 플레이어별 상점 카드 페어 데이터


    void Awake()
    {
        mercuriusPopUp = PopUpUIManager.instance.mercuriusPopUp.GetComponent<MercuriusPopUp>();
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.onClientDisconnected += OnClientDisconnected;
        toddAnim = GetComponent<SkeletonAnimation>();
        minion0Anim = transform.GetChild(1).GetComponent<SkeletonAnimation>();
        minion1Anim = transform.GetChild(2).GetComponent<SkeletonAnimation>();
        minion2Anim = transform.GetChild(3).GetComponent<SkeletonAnimation>();
        minion3Anim = transform.GetChild(4).GetComponent<SkeletonAnimation>();
        StartCoroutine(ToddAnimationBlend());
    }

    // NPC_Mercurius 각 클라이언트에 생성될 때 현재 플레이어의 카드 데이터에서 6개의 랜덤 카드데이터를 추출하여 팝업에 6개의 상점카드 세팅
    public override void OnStartClient()
    {
        if(isOrigin && mercuriusPopUp != null){
            InitShopCardByCharacter();
        }
    }

    // 클라이언트 연결 해제 이벤트 수신시 상점 카드 정보 갱신
    private void OnClientDisconnected(GamePlayer gamePlayer)
    {
        RemoveShopCard();
        InitShopCardByCharacter();
    }

    // NPC_Mercurius 파괴될 때 리스트 비움
    void OnDestroy()
    {
        if(mercuriusPopUp != null){
            for(int i= shopCardObjectList.Count-1; i >=0; i--){
                Destroy(shopCardObjectList[i]);
                shopCardObjectList.RemoveAt(i);
            }
        }
    }

    // 현재 로컬 플레이어의 캐릭터에 설정된 상점카드 데이터로 상점카드 오브젝트 생성
    private void InitShopCardByCharacter()
    {
        PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
        for(int i=0; i<playerInterface.ownedPlayers.Count; i++){
            GamePlayer gamePlayer = playerInterface.ownedPlayers[i];
            if(shopCardDictionary.TryGetValue(gamePlayer, out List<Card> shopCards)){
                CreateShopCard(shopCards, gamePlayer, i);
            }
        }
    }

    // 상점카드 오브젝트 생성
    private void CreateShopCard(List<Card> shopCards, GamePlayer cardOwner, int index)
    {
        foreach(Card card in shopCards){                    
            // 상점 카드 슬롯(최상단 부모 오브젝트)
            GameObject cardShopSlot = Instantiate(PopUpUIManager.instance.CardShopSlot,Vector3.zero, Quaternion.identity);
            cardShopSlot.transform.SetParent(mercuriusPopUp.grids[index].transform);
            cardShopSlot.transform.localScale = new Vector3(1, 1, 1);

            // 상점 카드
            GameObject cardOnDeckObject = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab, Vector3.zero, Quaternion.identity);
            cardOnDeckObject.transform.SetParent(cardShopSlot.transform);
            cardOnDeckObject.transform.localScale = new Vector3(1, 1, 1);
            CardOnDeck cardOnDeck = cardOnDeckObject.GetComponent<CardOnDeck>();
            cardOnDeck.card = card;
            cardOnDeck.cardOwner = cardOwner;

            // 상점 카드 가격 아이콘 + 텍스트
            GameObject cardShopPrice = Instantiate(PopUpUIManager.instance.CardShopPrice, Vector3.zero, Quaternion.identity);
            cardShopPrice.transform.SetParent(cardShopSlot.transform);
            cardShopPrice.transform.localScale = new Vector3(1, 1, 1);

            TextMeshProUGUI textPrice = cardShopPrice.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            textPrice.text = "100";

            shopCardObjectList.Add(cardShopSlot);
        }
    }

    // 상점카드 오브젝트 제거
    private void RemoveShopCard()
    {
        for(int i = shopCardObjectList.Count - 1; i >= 0; i--){
            Destroy(shopCardObjectList[i]);
            shopCardObjectList.RemoveAt(i);
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

    public override void OnChanedNextAction(MonsterAction oldVal, MonsterAction newVal)
    {
        
    }

    private IEnumerator ToddAnimationBlend()
    {
        WaitForSeconds loopTime = new WaitForSeconds(0.01f);
        int[] eachTimer = new int[5];
        for(int i = 0 ; i < 5 ; i++)
            eachTimer[i] = Random.Range(400,900);
        while(true)
        {
            for(int i = 0 ;i < 5 ; i ++)
            {
                eachTimer[i]--;
                if(eachTimer[i] <= 0)
                {
                    eachTimer[i] = Random.Range(600,1200);
                    switch(i)
                    {
                        case 0 : StartCoroutine(ToddActAnimation(toddAnim,3.3f));
                            break;
                        case 1 : StartCoroutine(ToddActAnimation(minion0Anim,2.66f));
                            break;
                        case 2 : StartCoroutine(ToddActAnimation(minion1Anim,3.33f));
                            break;
                        case 3 : StartCoroutine(ToddActAnimation(minion2Anim,2.66f));
                            break;
                        case 4 : StartCoroutine(ToddActAnimation(minion3Anim,4f));
                            break;
                    }
                }
            }
            yield return loopTime;
        }
    }

    private IEnumerator ToddActAnimation(SkeletonAnimation anim, float actTime)
    {
        anim.state.SetAnimation(0,"Act",false);
        yield return new WaitForSeconds(actTime);
        anim.state.SetAnimation(0,"Idle",true);
    }
}
