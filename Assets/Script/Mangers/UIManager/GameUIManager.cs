using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Mirror;

// 게임 씬 공용 UI — 화면 전환(페이드/블록 트랜지션)과 페이즈 텍스트.
// 카드 전투 UI(패 패널/이치/덱 버튼/카드 큐/턴 종료 버튼)는 카드 시스템 제거로 삭제됨 (2026-09-01).
public class GameUIManager : SingletonD<GameUIManager>
{
    [Header("게임 오브젝트")]
    public GameObject RootGameObject;

    [Header("카메라 사이즈값")]
    public static float battelSceneCameraSize = 10.8f; // 거점/전투 화면 카메라 크기값

    [Header("화면 전환 UI")]
    public Image screenTransition;
    public Image screenFade;
    public enum ScreenTransitionMode {
        Fade,
        Transition
    }
    public ScreenTransitionMode screenTransitionMode; // 스크린 전환 모드(페이드 인 아웃, 블록트랜지션 인 아웃)

    [Header("전투 정보 UI")]
    public TextMeshProUGUI textCurrentPhase; // MapInfo/CurrentPhaseBG/TextCurrentPhase — 페이즈 표시. TP 전투 턴 순서 텍스트(M_TurnManager.TpBattle)가 스타일을 복제한다


    void Start()
    {
        // DDOL 정리 목록에 등록 — 메뉴씬으로 돌아갈 때 파괴되도록 한다.
        // 등록하지 않으면 게임오버 후 재시작해도 이전 판의 GameUIManager가 살아남고,
        // SingletonD가 새 GameScene의 정상 인스턴스를 중복으로 보고 파괴해 버린다.
        // 그러면 screenFade/screenTransition 등 참조가 전부 파괴된 상태가 되어 화면 전환 트윈이
        // 시작조차 못 하고, 완료 콜백이 호출되지 않아 맵→전투 전환이 멈춘다.
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        if(networkRoomManager != null && !networkRoomManager.persistentManagers.ContainsKey(gameObject.name)){
            networkRoomManager.persistentManagers.Add(gameObject.name, gameObject);
        }

        ConfigScreenChangeMode(screenTransitionMode);
        screenTransition.material =  new Material(screenTransition.material); // 머티리얼 인스턴스 복사본을 생성하여 이미지의 머티리얼값에 할당(원본대신 복사본을 사용해 프로퍼티값 변경)
    }

    // 화면 전환 모드에 따라 사용할 오브젝트 활성화 설정
    public void ConfigScreenChangeMode(ScreenTransitionMode screenTransitionMode)
    {
        switch(screenTransitionMode){
            case ScreenTransitionMode.Fade:
                screenFade.gameObject.SetActive(true);
                screenTransition.gameObject.SetActive(false);
                break;
            case ScreenTransitionMode.Transition:
                screenFade.gameObject.SetActive(false);
                screenTransition.gameObject.SetActive(true);
                break;
        }
    }

    // 화면 전환 모드에 따라 스크린 IN 시퀀스 수행
    public void DoScreenChangeIn(System.Action callback = null)
    {
        switch(screenTransitionMode){
            case ScreenTransitionMode.Fade:
                DoScreenFadeIn(() => callback());
                break;
            case ScreenTransitionMode.Transition:
                DoScreenTransitionIn(() => callback());
                break;
        }
    }

    // 화면 전환 모드에 따라 스크린 OUT 시퀀스 수행
    public void DoScreenChangeOut()
    {
         switch(screenTransitionMode){
            case ScreenTransitionMode.Fade:
                DoScreenFadeOut();
                break;
            case ScreenTransitionMode.Transition:
                DoScreenTransitionOut();
                break;
        }
    }

    // 스크린 Fade In 시퀀스
    private void DoScreenFadeIn(System.Action callback = null)
    {
        screenFade.DOFade(1f, 1.0f).OnComplete(() => {
            if(callback != null){
                callback();
            }
        });
    }

    // 스크린 Fade Out 시퀀스
    private void DoScreenFadeOut()
    {
        screenFade.DOFade(0f, 1.0f);
    }

    // 스크린 Block Transition In 시퀀스
    private void DoScreenTransitionIn(System.Action callback = null)
    {
        StartCoroutine(TransitionInCoroutine(() => {
            if(callback != null){
                callback();
            }
        }));
    }

    // 스크린 Block Transition Out 시퀀스
    private void DoScreenTransitionOut()
    {
        StartCoroutine(TransitionOutCoroutine());
    }

    private IEnumerator TransitionInCoroutine(System.Action callback = null)
    {
        screenTransition.enabled = true;
        float duration = 1.0f; // TransitionIn의 지속 시간
        float elapsedTime = 0f;

        float initialScroll = 2.5f; // 진행상태 프로퍼티값의 초기값
        float finalScroll = 0f;     // 진행상태 프로퍼티값의 최종값

        while (elapsedTime < duration){
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Transition_In 구간 : 0에서 1사이의 t값이 0 ~ 1 구간에서는, 프로퍼티값의 초기값 -> 0 변경
            float currentScroll = Mathf.Lerp(initialScroll, finalScroll, t);

            screenTransition.material.SetFloat("_Progress", currentScroll);

            yield return null;
        }
        screenTransition.material.SetFloat("_Progress", finalScroll);
        if(callback != null){
            callback();
        }
    }

    private IEnumerator TransitionOutCoroutine()
    {
        screenTransition.enabled = true;
        float duration = 1.0f; // TransitionOut의 지속 시간
        float elapsedTime = 0f;

        float initialScroll = 0f;     // TransitionIn에서 최종적으로 설정된 값
        float finalScroll = 2.5f; // TransitionOut에서 되돌아갈 초기값

        while (elapsedTime < duration){
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Transition_Out 구간 : 0에서 1사이의 t값이 0 ~ 1 구간에서는, 0 -> 프로퍼티값의 초기값 변경
            float currentScroll = Mathf.Lerp(initialScroll, finalScroll, t);

            screenTransition.material.SetFloat("_Progress", currentScroll);

            yield return null;
        }
        screenTransition.material.SetFloat("_Progress", finalScroll);
        screenTransition.enabled = false;
    }
}
