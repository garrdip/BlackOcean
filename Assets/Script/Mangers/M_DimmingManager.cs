using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using DG.Tweening;
using Mirror;
using Spine.Unity.Examples;


// 화면 딤/페이드 담당.
// - 딤(Dimming/Clear): 카드 타겟팅 등에서 배경을 어둡게 하고 대상만 앞 레이어로 올리는 부분 딤 (월드 스프라이트)
// - 페이드(FadeOut/FadeIn): 화면 전환용 전체 암전 — 최상위 ScreenCanvas의 screenFade 이미지를 사용해 UI까지 전부 가린다.
//   서버가 RpcFadeOut → (검은 화면 뒤에서 루트 전환/오브젝트 스폰) → RpcFadeIn 순서로 호출하면 과도현상 없이 전환된다 (M_TurnManager.Spawner 참조)
public class M_DimmingManager : NetworkSingletonD<M_DimmingManager>
{
    public const float FadeDuration = 0.8f; // 페이드 아웃/인 각각의 시간(초) — 서버 대기 시간 계산에도 사용

    SpriteRenderer dim;

    protected override void Start()
    {
        DontDestroyOnLoad(gameObject);
        dim = GetComponent<SpriteRenderer>();
    }

    // ------------------------------------------------------------ 화면 페이드 (전환용) ------------------------------------------------------------ //

    [ClientRpc]
    public void RpcFadeOut()
    {
        FadeOut();
    }

    [ClientRpc]
    public void RpcFadeIn()
    {
        FadeIn();
    }

    // 화면을 검게 (진행 중인 페이드 트윈은 끊고 이어서 — 겹침으로 인한 깜빡임 방지)
    public void FadeOut(System.Action onComplete = null)
    {
        Image fade = GetScreenFade();
        if(fade == null){ onComplete?.Invoke(); return; }
        fade.DOKill();
        fade.gameObject.SetActive(true);
        fade.raycastTarget = true; // 전환 중 클릭 차단
        fade.DOFade(1f, FadeDuration).SetEase(Ease.InOutSine).OnComplete(() => onComplete?.Invoke());
    }

    // 검은 화면에서 원래대로
    public void FadeIn(System.Action onComplete = null)
    {
        Image fade = GetScreenFade();
        if(fade == null){ onComplete?.Invoke(); return; }
        fade.DOKill();
        fade.gameObject.SetActive(true);
        fade.DOFade(0f, FadeDuration).SetEase(Ease.InOutSine).OnComplete(() => {
            fade.raycastTarget = false;
            onComplete?.Invoke();
        });
    }

    // 즉시 검게/투명하게 (연출 없이 상태만 맞출 때)
    public void SetFadeImmediate(bool black)
    {
        Image fade = GetScreenFade();
        if(fade == null) return;
        fade.DOKill();
        fade.gameObject.SetActive(true);
        Color color = fade.color;
        color.a = black ? 1f : 0f;
        fade.color = color;
        fade.raycastTarget = black;
    }

    Image GetScreenFade()
    {
        return GameUIManager.instance != null ? GameUIManager.instance.screenFade : null;
    }

    // ------------------------------------------------------------ 부분 딤 (타겟 강조) ------------------------------------------------------------- //

    [ClientRpc]
    public void StartDimming(List<TargetObject> targets)
    {
        DOTween.Kill(dim);
        Dimming();
        foreach(TargetObject tar in targets)
        {
            if(tar != null)
            {
                SetTargetObjectLayer(tar, "FrontLayer");
            }
        }
    }

    [ClientRpc]
    public void StopDimming(List<TargetObject> targets)
    {
        Clear();
        foreach(TargetObject tar in targets)
        {
            if(tar != null)
            {
                SetTargetObjectLayer(tar, "BackLayer");
            }
        }
    }

    public void Dimming()
    {
        dim.DOFade(0.6f,0.4f);
    }

    public void Clear()
    {
        dim.DOFade(0f,0.4f);
    }

    public void SetTargetObjectLayer(TargetObject tar, string layerName)
    {
        tar.targetObjectUI.GetComponent<SortingGroup>().sortingLayerName = layerName;
        if(tar.player != null){
            tar.avatar.GetComponent<MeshRenderer>().sortingLayerName = layerName;
            if(tar.ironDemon != null){
                tar.ironDemon.GetComponent<MeshRenderer>().sortingLayerName = layerName;
                tar.ironDemon.GetComponent<SkeletonRenderTexture>().quad.GetComponent<MeshRenderer>().sortingLayerName = layerName; // SkeletonRenderTexture의 정렬값 따로 조정
            }
            tar.playerHpCanvas.sortingLayerName = layerName;
            tar.playerNameCanvas.sortingLayerName = layerName;
            tar.playerShieldCanvas.sortingLayerName = layerName;
        }else{
            tar.monster.GetComponent<MeshRenderer>().sortingLayerName = layerName;
            tar.monsterHpCanvas.sortingLayerName = layerName;
            tar.monsterNameCanvas.sortingLayerName = layerName;
            tar.monsterShieldCanvas.sortingLayerName = layerName;
            tar.nextActionCanvas.sortingLayerName = layerName;
        }
    }
}
