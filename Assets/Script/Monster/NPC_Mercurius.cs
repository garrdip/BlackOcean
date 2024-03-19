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

    [Header("Materials")]
    public Material todBackOriginMaterial;
    public Material todBackOutlineMaterial;
    public Material todblueOriginMaterial;
    public Material todblueOutlineMaterial;
    public Material todGreenOriginMaterial;
    public Material todGreenOutlineMaterial;
    public Material todRedOriginMaterial;
    public Material todRedOutlineMaterial;
    public Material todYellowOriginMaterial;
    public Material todYellowOutlineMaterial;
    public Material todOriginMaterial;
    public Material todOutlineMaterial;


    
    [SyncVar]
    public bool isOrigin = false; // 원본 오브젝트인지 구분값(타겟오브젝트들은 원본과 클론이 존재해서 둘중 하나만 호출되어야 함)
    public readonly SyncDictionary<GamePlayer, List<Card>> shopCardDictionary = new  SyncDictionary<GamePlayer, List<Card>>(); // 각 플레이어별 상점 카드 페어 데이터


    void Awake()
    {
        mercuriusPopUp = PopUpUIManager.instance.mercuriusPopUp.GetComponent<MercuriusPopUp>();
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.onClientDisconnected += OnClientDisconnected;
        minion0Anim = transform.GetChild(1).GetComponent<SkeletonAnimation>();
        minion1Anim = transform.GetChild(2).GetComponent<SkeletonAnimation>();
        minion2Anim = transform.GetChild(3).GetComponent<SkeletonAnimation>();
        minion3Anim = transform.GetChild(4).GetComponent<SkeletonAnimation>();
        toddAnim = transform.GetChild(5).GetComponent<SkeletonAnimation>();
        StartCoroutine(ToddAnimationBlend());
        AddEventTrigger();
    }

    // NPC_Mercurius 각 클라이언트에 생성될 때 현재 플레이어의 카드 데이터에서 6개의 랜덤 카드데이터를 추출하여 팝업에 6개의 상점카드 세팅
    public override void OnStartClient()
    {
        if(isOrigin && mercuriusPopUp != null){
            InitShopCardByCharacter();
            PlayToddVoice();
            StartCoroutine(PlayMinionsVoice());
        }
    }

    // Todd 초기 음성 재생
    private void PlayToddVoice()
    {
        List<AudioClip> clips = M_SoundManager.instance.voiceClips[VOICE_TYPE.Todd].FindAll((audioClip) => audioClip.name.Contains("thoth")); // Todd 음성 리스트 추출
        AudioClip firstVoice = clips[0];
        AudioClip secondVoice = clips[1];
        M_SoundManager.instance.PlayVoice(firstVoice, firstVoice.length, false, () => {
            M_SoundManager.instance.PlayVoice(secondVoice, secondVoice.length);
        });
    }

    // 5초마다 미니언 랜덤 음성 재생
    IEnumerator PlayMinionsVoice()
    {
        List<AudioClip> clips = M_SoundManager.instance.voiceClips[VOICE_TYPE.Todd].FindAll((audioClip) => audioClip.name.Contains("minons"));
        while(gameObject.activeSelf){
            int randomIndex = Random.Range(0, clips.Count);
            AudioClip clipToPlay = clips[randomIndex];
            M_SoundManager.instance.PlayVoice(clipToPlay, clipToPlay.length);
            yield return new WaitForSeconds(5f);
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
        StopCoroutine(PlayMinionsVoice());
    }

    public void OnPointerEnterMercurius(PointerEventData eventData)
    {
        transform.GetChild(0).GetComponent<MeshRenderer>().material = todBackOutlineMaterial;
        transform.GetChild(1).GetComponent<MeshRenderer>().material = todblueOutlineMaterial;
        transform.GetChild(2).GetComponent<MeshRenderer>().material = todGreenOutlineMaterial;
        transform.GetChild(3).GetComponent<MeshRenderer>().material = todRedOutlineMaterial;
        transform.GetChild(4).GetComponent<MeshRenderer>().material = todYellowOutlineMaterial;
        transform.GetChild(5).GetComponent<MeshRenderer>().material = todOutlineMaterial;
    }

    public void OnPointerExitMercurius(PointerEventData eventData)
    {
        transform.GetChild(0).GetComponent<MeshRenderer>().material = todBackOriginMaterial;
        transform.GetChild(1).GetComponent<MeshRenderer>().material = todblueOriginMaterial;
        transform.GetChild(2).GetComponent<MeshRenderer>().material = todGreenOriginMaterial;
        transform.GetChild(3).GetComponent<MeshRenderer>().material = todRedOriginMaterial;
        transform.GetChild(4).GetComponent<MeshRenderer>().material = todYellowOriginMaterial;
        transform.GetChild(5).GetComponent<MeshRenderer>().material = todOriginMaterial;
    }

    // NPC Mercurius 클릭 이벤트
    public void OnClickMercurius(PointerEventData pointerEventData)
    {
        if(M_TurnManager.instance.phase == BattleTurn.NONE_BATTLE_SCENE){
            PopUpUIManager.instance.HandleMercuriusPopUp(true);
        }
    }

    // EventTrigger를 이용한 동적 클릭 이벤트 할당
    private void AddEventTrigger()
    {
        EventTrigger eventTrigger = gameObject.AddComponent<EventTrigger>();

        // PointerClick 이벤트 추가
        EventTrigger.Entry pointerClickEntry = new EventTrigger.Entry();
        pointerClickEntry.eventID = EventTriggerType.PointerClick;
        pointerClickEntry.callback.AddListener((data) => { OnClickMercurius((PointerEventData)data); });
        eventTrigger.triggers.Add(pointerClickEntry);

        // PointerEnter 이벤트 추가
        EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry();
        pointerEnterEntry.eventID = EventTriggerType.PointerEnter;
        pointerEnterEntry.callback.AddListener((data) => { OnPointerEnterMercurius((PointerEventData)data); });
        eventTrigger.triggers.Add(pointerEnterEntry);

        // PointerExit 이벤트 추가
        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry();
        pointerExitEntry.eventID = EventTriggerType.PointerExit;
        pointerExitEntry.callback.AddListener((data) => { OnPointerExitMercurius((PointerEventData)data); });
        eventTrigger.triggers.Add(pointerExitEntry);
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
            cardShopSlot.transform.localScale = Vector3.one;
            cardShopSlot.transform.localPosition = Vector3.zero;

            // 상점 카드
            GameObject cardOnDeckObject = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab, Vector3.zero, Quaternion.identity);
            cardOnDeckObject.transform.SetParent(cardShopSlot.transform);
            cardOnDeckObject.transform.localScale = Vector3.one;
            cardOnDeckObject.transform.localPosition = Vector3.zero;
            CardOnDeck cardOnDeck = cardOnDeckObject.GetComponent<CardOnDeck>();
            cardOnDeck.card = card;
            cardOnDeck.cardOwner = cardOwner;

            // 상점 카드 가격 아이콘 + 텍스트
            GameObject cardShopPrice = Instantiate(PopUpUIManager.instance.CardShopPrice, Vector3.zero, Quaternion.identity);
            cardShopPrice.transform.SetParent(cardShopSlot.transform);
            cardShopPrice.transform.localScale = Vector3.one;
            cardShopPrice.transform.localPosition = new Vector3(0f, 30f, 0f);

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
