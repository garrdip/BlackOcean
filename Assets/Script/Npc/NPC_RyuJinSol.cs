using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;
using Spine.Unity;
using Spine;

public class NPC_RyuJinSol : SpawnedMonster
{
    [Header("스켈레톤 애니매이션 컴포넌트")]
    public SkeletonAnimation skeletonAnimation;
    public List<string> animationNames = new List<string>();
    private TrackEntry trackEntry;
    public Coroutine eyeBlikCoroutine;


    void Awake()
    {
        AddEventTrigger();
    }

    void Start()
    {
        GetComponent<SkeletonRendererCustomMaterials>().enabled = false;
        GetAnimationNames(skeletonAnimation);
        PlayRandomAnimation();
        eyeBlikCoroutine = StartCoroutine(StartEyeBlinkAnimation());
    }

    void OnDestroy()
    {
        trackEntry.Complete -= OnAnimationComplete;
        StopCoroutine(eyeBlikCoroutine);
    }

    public void OnClickRyuJinSol(PointerEventData pointerEventData)
    {
        // 클릭 이벤트 처리
    }

    public void OnPointerEnterRyuJinSol(PointerEventData eventData)
    {
        GetComponent<SkeletonRendererCustomMaterials>().enabled = true;
    }

    public void OnPointerExitRyuJinSol(PointerEventData eventData)
    {
       GetComponent<SkeletonRendererCustomMaterials>().enabled = false;
    }

    // 3초 마다 눈 깜빡임 애니매이션 재생
    private IEnumerator StartEyeBlinkAnimation()
    {
        while(true)
        {
            yield return new WaitForSeconds(3f);
            skeletonAnimation.state.SetAnimation(1, "Eye", false);
        }
    }

    private int GetRandomIndex()
    {
        int randomValue = Random.Range(0, 100);
        if (randomValue < 65) return 2; // Idle
        if (randomValue < 85) return 3; // Mouth
        if (randomValue < 95) return 1; // Eye
        else return 0; // Act
    }
  
    // 애니매이션 리스트들중 하나 랜덤으로 재생 : 재생 완료 콜백에서 재귀호출하는 방식으로 무한 재생
    private void PlayRandomAnimation()
    {
        string randomAnimation = animationNames[GetRandomIndex()];
        trackEntry = skeletonAnimation.state.SetAnimation(0, randomAnimation, false);
        trackEntry.Complete += OnAnimationComplete;
    }

    // 애니매이션 완료 콜백
    private void OnAnimationComplete(TrackEntry trackEntry)
    {
        PlayRandomAnimation();
    }

    // 스파인 스켈레톤 애니매이션 목록 조회
    private List<string> GetAnimationNames(SkeletonAnimation skeletonAnimation)
    {
        SkeletonData skeletonData = skeletonAnimation.Skeleton.Data;
        foreach(Spine.Animation animation in skeletonData.Animations){
            animationNames.Add(animation.Name);
        }
        return animationNames;
    }

    private void AddEventTrigger()
    {
        EventTrigger eventTrigger = gameObject.AddComponent<EventTrigger>();

        // PointerClick 이벤트 추가
        EventTrigger.Entry pointerClickEntry = new EventTrigger.Entry();
        pointerClickEntry.eventID = EventTriggerType.PointerClick;
        pointerClickEntry.callback.AddListener((data) => { OnClickRyuJinSol((PointerEventData)data); });
        eventTrigger.triggers.Add(pointerClickEntry);

        // PointerEnter 이벤트 추가
        EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry();
        pointerEnterEntry.eventID = EventTriggerType.PointerEnter;
        pointerEnterEntry.callback.AddListener((data) => { OnPointerEnterRyuJinSol((PointerEventData)data); });
        eventTrigger.triggers.Add(pointerEnterEntry);

        // PointerExit 이벤트 추가
        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry();
        pointerExitEntry.eventID = EventTriggerType.PointerExit;
        pointerExitEntry.callback.AddListener((data) => { OnPointerExitRyuJinSol((PointerEventData)data); });
        eventTrigger.triggers.Add(pointerExitEntry);
    }
}
