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
    private IEnumerator minionVoiceCoroutine;
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


    void Awake()
    {
        mercuriusPopUp = PopUpUIManager.instance.mercuriusPopUp.GetComponent<MercuriusPopUp>();
        minion0Anim = transform.GetChild(1).GetComponent<SkeletonAnimation>();
        minion1Anim = transform.GetChild(2).GetComponent<SkeletonAnimation>();
        minion2Anim = transform.GetChild(3).GetComponent<SkeletonAnimation>();
        minion3Anim = transform.GetChild(4).GetComponent<SkeletonAnimation>();
        toddAnim = transform.GetChild(5).GetComponent<SkeletonAnimation>();
        StartCoroutine(ToddAnimationBlend());
        AddEventTrigger();
        minionVoiceCoroutine = PlayMinionsVoice();
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
            StartCoroutine(minionVoiceCoroutine); // 미니언 음성 재생
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        StopCoroutine(minionVoiceCoroutine); // 미니언 음성 재생 중지
    }

    // NPC_Mercurius 생성 10초후에 5초마다 미니언 랜덤 음성 재생
    private IEnumerator PlayMinionsVoice()
    {
        yield return new WaitForSeconds(10f);
        List<AudioClip> clips = M_SoundManager.instance.voiceClips[VOICE_TYPE.Todd].FindAll((audioClip) => audioClip.name.Contains("minons"));
        while(gameObject.activeSelf){
            int randomIndex = Random.Range(0, clips.Count);
            AudioClip clipToPlay = clips[randomIndex];
            M_SoundManager.instance.PlayVoice(clipToPlay, clipToPlay.length);
            yield return new WaitForSeconds(5f);
        }
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
