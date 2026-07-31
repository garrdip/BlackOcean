using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using ProjectD;
using DG.Tweening;


public class EndTurnButton : ButtonBase
{
    public GameObject endTurnBaseLight;
    public GameObject endTurnBaseLight2;
    public GameObject endTurnBaseLight3;
    public GameObject endTurnSBase;
    public GameObject endTurnS1;
    public GameObject endTurnS2;

    void Start()
    {
        GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        endTurnS1.GetComponent<RectTransform>().DOKill();
        endTurnS2.GetComponent<RectTransform>().DOKill();
    }

    public void OnPointerClick()
    {
        if(!IsEndTurnAllowedPhase()) return; // 내 행동 차례가 아닐 때는 반응하지 않음

        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "stage_ready");
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        PlayerInterface playerInterface = PlayerRegistry.Local;
        playerInterface.endTurnActive = !playerInterface.endTurnActive;
        playerInterface.OnEndTurnStateChanged(playerInterface.endTurnActive,  playerInterface.endTurnActive);
    }

    // 턴 종료를 누를 수 있는 페이즈인지 (M_TurnManager.CheckAllPlayersEndTurn이 실제로 처리하는 두 페이즈와 동일)
    // 전투 중에는 PLAYER_ACTIVE에서만 허용한다. 드로우/몬스터 페이즈에 미리 눌러두면 내 차례가 오자마자
    // 턴이 그대로 끝나버리기 때문. 비전투 방(NONE_BATTLE_SCENE)은 기존대로 진행용으로 사용한다.
    private bool IsEndTurnAllowedPhase()
    {
        if(M_TurnManager.instance == null) return false;
        BattleTurn phase = M_TurnManager.instance.phase;
        return phase == BattleTurn.PLAYER_ACTIVE || phase == BattleTurn.NONE_BATTLE_SCENE;
    }

    public void OnPointerEnter()
    {
        endTurnBaseLight.SetActive(true);
    }

    public void OnPointerExit()
    {
        endTurnBaseLight.SetActive(false);
    }

    public void SetEndTurnButtonActiveState(bool isActive)
    {
        endTurnBaseLight.SetActive(isActive);
        endTurnBaseLight2.SetActive(isActive);
        endTurnBaseLight3.SetActive(isActive);
        endTurnSBase.SetActive(isActive);
        endTurnS1.SetActive(isActive);
        endTurnS2.SetActive(isActive);
        if(isActive){
            SetEndTurnCircleRotateInfinite();
        }else{
            endTurnS1.GetComponent<RectTransform>().DOKill();
            endTurnS2.GetComponent<RectTransform>().DOKill();
        }
    }

    private void SetEndTurnCircleRotateInfinite()
    {
        if(!DOTween.IsTweening(endTurnS1.GetComponent<RectTransform>()) && !DOTween.IsTweening(endTurnS2.GetComponent<RectTransform>())){
            endTurnS1.GetComponent<RectTransform>().DORotate(new Vector3(0, 0, 360), 4.5f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetRelative(true)
                .SetLoops(-1, LoopType.Yoyo);
            endTurnS2.GetComponent<RectTransform>().DORotate(new Vector3(0, 0, -360), 4.5f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetRelative(true)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }
}
