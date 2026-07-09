using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ProjectD;
using Mirror;
using Spine.Unity;


public class NPC_Mercurius : SpawnedMonster
{
    public MercuriusPopUp mercuriusPopUp;
    public List<GameObject> shopCardObjectList = new List<GameObject>();
    private IEnumerator minionVoiceCoroutine;
    private SkeletonAnimation toddAnim;
    private SkeletonAnimation backAnim;
    private SkeletonAnimation todBlueAnim;
    private SkeletonAnimation todGreenAnim;
    private SkeletonAnimation todRedAnim;
    private SkeletonAnimation todYellowAnim;
    public ExpandableButtonGroup expandableButtonGroup;
    private bool isOpenExpandableButtons = false;
    public Button buttonCardShop;
    public Button buttonCardEnhance;
    public Button buttonCardRemove;


    void Awake()
    {
        mercuriusPopUp = PopUpUIManager.instance.mercuriusPopUp.GetComponent<MercuriusPopUp>();
        toddAnim = transform.GetChild(0).GetComponent<SkeletonAnimation>();
        todBlueAnim = transform.GetChild(1).GetComponent<SkeletonAnimation>();
        todGreenAnim = transform.GetChild(2).GetComponent<SkeletonAnimation>();
        todRedAnim = transform.GetChild(3).GetComponent<SkeletonAnimation>();
        todYellowAnim = transform.GetChild(4).GetComponent<SkeletonAnimation>();
        StartCoroutine(ToddAnimationBlend());
        AddEventTrigger();
        minionVoiceCoroutine = PlayMinionsVoice();
        buttonCardShop.onClick.AddListener(() => OnClickCardShopButton());
        buttonCardEnhance.onClick.AddListener(() => OnClickCardEnhanceButton());
        buttonCardRemove.onClick.AddListener(() => OnClickCardRemoveButton());
    }

    public override void Start()
    {
        base.Start();
        for(int i=0; i<6; i++){
            transform.GetChild(i).GetComponent<SkeletonRendererCustomMaterials>().enabled = false;
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        // OnStopServer 시점에 각 플레이어들의 shopCard 리스트 데이터 제거
        foreach(uint netId in M_TurnManager.instance.playerOrder){
            if(NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayerDeck gamePlayerDeck = networkIdentity.GetComponent<GamePlayerDeck>();
                gamePlayerDeck.shopCards.Clear();
            }
        }
    }

    public override void OnStartClient()
    {
        if(mercuriusPopUp != null){
            // Todd -> 플레이어 -> 미니언즈 순서로 음성 재생
            PlayToddVoice(() => {
                PlayCharacterVoiceOnCardShop();
            });
            StartCoroutine(minionVoiceCoroutine); // 미니언 음성 재생
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        StopCoroutine(minionVoiceCoroutine); // 미니언 음성 재생 중지
    }

    // Todd 초기 음성 재생
    private void PlayToddVoice(System.Action callback = null)
    {
        List<AudioClip> clips = M_SoundManager.instance.GetVoiceClips(VOICE_TYPE.Todd).FindAll((audioClip) => audioClip.name.Contains("thoth")); // Todd 음성 리스트 추출
        AudioClip firstVoice = clips[0];
        AudioClip secondVoice = clips[1];
        M_SoundManager.instance.PlayVoice(firstVoice, firstVoice.length, false, () => {
            M_SoundManager.instance.PlayVoice(secondVoice, secondVoice.length, false, () =>{
                if(callback != null){
                    callback();
                }
            });
        });
    }

    // 카드 상인에 대한 캐릭터들 상호작용 음성 재생
    private void PlayCharacterVoiceOnCardShop()
    {
        if(NetworkClient.localPlayer != null){
            AudioClip meetCardNpcVoice = null;
            Character character = PlayerRegistry.Local.currentGamePlayer.character;
            switch(character){
                case Character.HONGDANHYANG:
                    List<AudioClip> danhyangVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.HongDanHyang, 77, 3);
                    meetCardNpcVoice = danhyangVoices[Random.Range(0, danhyangVoices.Count)];
                    break;
                case Character.GEORK:
                    List<AudioClip> georkVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Geork, 89, 3);
                    meetCardNpcVoice = georkVoices[Random.Range(0, georkVoices.Count)];
                    break;
                case Character.ERIS:
                    List<AudioClip> erisVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Eris, 135, 3);
                    meetCardNpcVoice = erisVoices[Random.Range(0, erisVoices.Count)];
                    break;
            }
            M_SoundManager.instance.PlayVoice(meetCardNpcVoice, meetCardNpcVoice.length);
        }
    }

    // NPC_Mercurius 생성 10초후에 5초마다 미니언 랜덤 음성 재생
    private IEnumerator PlayMinionsVoice()
    {
        yield return new WaitForSeconds(10f);
        List<AudioClip> clips = M_SoundManager.instance.GetVoiceClips(VOICE_TYPE.Todd).FindAll((audioClip) => audioClip.name.Contains("minons"));
        while(gameObject.activeSelf){
            int randomIndex = Random.Range(0, clips.Count);
            AudioClip clipToPlay = clips[randomIndex];
            M_SoundManager.instance.PlayVoice(clipToPlay, clipToPlay.length);
            yield return new WaitForSeconds(5f);
        }
    }

    public void OnPointerEnterMercurius(PointerEventData eventData)
    {
        for(int i=0; i<6; i++){
            transform.GetChild(i).GetComponent<SkeletonRendererCustomMaterials>().enabled = true;
        }
    }

    public void OnPointerExitMercurius(PointerEventData eventData)
    {
        for(int i=0; i<6; i++){
            transform.GetChild(i).GetComponent<SkeletonRendererCustomMaterials>().enabled = false;
        }
    }

    // NPC Mercurius 클릭 이벤트
    public void OnClickMercurius(PointerEventData pointerEventData)
    {
        if(M_TurnManager.instance.phase == BattleTurn.NONE_BATTLE_SCENE){
            isOpenExpandableButtons = !isOpenExpandableButtons;
            if(isOpenExpandableButtons){
                expandableButtonGroup.OpenExpandableButtonGroup();
            }else{
                expandableButtonGroup.HideExpandableButtonGroup();
            }
        }
    }

    public void OnClickCardShopButton()
    {
        PopUpUIManager.instance.HandleMercuriusPopUp(true);
    }

    public void OnClickCardEnhanceButton()
    {
        PopUpUIManager.instance.HandleCardEnhancePopUp(true);
    }

    public void OnClickCardRemoveButton()
    {
        PopUpUIManager.instance.HandleCardRemovePopUp(true);
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
                        case 1 : StartCoroutine(ToddActAnimation(todBlueAnim,2.66f));
                            break;
                        case 2 : StartCoroutine(ToddActAnimation(todGreenAnim,3.33f));
                            break;
                        case 3 : StartCoroutine(ToddActAnimation(todRedAnim,2.66f));
                            break;
                        case 4 : StartCoroutine(ToddActAnimation(todYellowAnim,4f));
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
