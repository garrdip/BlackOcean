using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Mirror;
using ProjectD;
using DG.Tweening;
using TMPro;
using Spine.Unity;
using Spine.Unity.Examples;
using System.Linq;

// TargetObject partial — 캐릭터 전용 로직 (에리스 변신 / 홍단향 철귀).
// 게오르크 고행 카드·영웅 변신(위대한 자)과 에리스 파괴의 권능(카드 배수)은 카드 시스템 제거로 삭제됨 (2026-09-01) — RPG의 고행길은 SkillData.Geork GS1
public partial class TargetObject
{

    IEnumerator HongDanHyangEyeFlicker()
    {
        while(true)
        {
            yield return new WaitForSeconds(Random.Range(2f,5f));
            anim.state.SetAnimation(1,"Eye",false);
        }
    }


    // ---- 에리스 변신 매커니즘 (RPG_CONVERSION_DESIGN, 2026-09-01) ----
    // HP 비율로 상태가 정해진다: 최대 HP의 ERIS_MAD_HP_PERCENT(10%) 이하 → 광기(MAD, 가하는 피해 +50%),
    // ERIS_ANGER_HP_PERCENT(40%) 이하 → 1차 변신(ANGER, +20%), 그 위 → NORMAL. 회복으로 비율이 오르면 되돌아간다.
    // HP가 바뀌는 모든 경로(SetPlayerHP)에서 서버가 갱신하고, 피해 배수는 BattleActions.ScaleByErisMode가 적용한다.
    // 애니메이션: 1차 변신 "Change1" → 완료 시 "ChIdle", 광기 "Change2" → "VIdle", 하향은 "RChange2"(광기→1차) / "RChange1"(1차→기본).
    // 변신 모션이 끝나면 OnAnimationComplete(TargetObject.cs)가 현재 erisMode 프리픽스(GetErisMode) + "Idle"을 재생하므로 여기서는 모션만 건다
    public bool erisReviveUsed = false; // 꿈을 본 인형 — 치사 피해를 HP 1로 버티는 부활은 한 전투에 한 번 (TargetObject는 전투마다 새로 스폰되므로 자연 리셋)
    public int erisDestroyUses = 0;     // '권능 : 파괴'(ES7) 이번 전투 사용 횟수 — 사용마다 계수 +30%p (SkillData.Eris), 전투마다 자연 리셋
    public float pendingHitDelay = 0f;  // 공격 모션 시작 후 첫 타격까지의 지연(초) — M_TurnManager.PlayPlayerActionAnimation이 설정(BalanceDB {캐릭터}_{모션}_HIT_DELAY_MS 우선, 없으면 {캐릭터}_ATTACK_HIT_DELAY_MS — 에리스 1.0 / 게오르크 Attack0 0.6·Attack1 0.5), BattleActions.AttackTarget이 소비

    [Server]
    public void UpdateErisMode()
    {
        if(player == null || player.character != Character.ERIS || playerMaxHP <= 0) return;
        int percent = playerHP * 100 / playerMaxHP;
        ErisMode next = ErisMode.NORMAL;
        if(playerHP > 0 && percent <= BalanceData.Get("ERIS_MAD_HP_PERCENT", 10)) next = ErisMode.MAD;
        else if(playerHP > 0 && percent <= BalanceData.Get("ERIS_ANGER_HP_PERCENT", 40)) next = ErisMode.ANGER;
        if(next == erisMode) return;
        ErisMode previous = erisMode;
        erisMode = next; // 모션보다 먼저 확정 — 완료 시 Idle 프리픽스와 피해 배율이 새 상태를 본다
        // 전이별 모션 (기획 확정 2026-09-01) — 완료 후 Idle은 OnAnimationComplete가 상태 프리픽스로 재생. 트랙 1의 광기 추가 모션 잔여는 OnChangedErisMode가 비운다
        //   기본→1차 Change0 / 1차→광기 Change1 / 기본→광기 Change2
        //   광기→기본 RChange2 / 광기→1차 RChange1 / 1차→기본 RChange0
        M_TurnManager.instance.StartAnimation(this, 0, GetErisTransitionMotion(previous, next), false);
    }

    static string GetErisTransitionMotion(ErisMode from, ErisMode to)
    {
        switch(from, to)
        {
            case (ErisMode.NORMAL, ErisMode.ANGER): return "Change0";
            case (ErisMode.ANGER, ErisMode.MAD): return "Change1";
            case (ErisMode.NORMAL, ErisMode.MAD): return "Change2";
            case (ErisMode.MAD, ErisMode.NORMAL): return "RChange2";
            case (ErisMode.MAD, ErisMode.ANGER): return "RChange1";
            case (ErisMode.ANGER, ErisMode.NORMAL): return "RChange0";
            default: return (to == ErisMode.MAD ? "V" : to == ErisMode.ANGER ? "Ch" : "") + "Idle"; // 도달 불가 조합 — 해당 상태 대기로 폴백
        }
    }

    // 치사 피해 시 에리스 부활 판정 — 한 전투에 한 번 HP 1로 살아남는다 (호출부는 이 결과로 HP를 1 또는 0으로 확정). 광기 진입은 SetPlayerHP → UpdateErisMode가 처리
    bool TryErisRevive()
    {
        if(player == null || player.character != Character.ERIS || erisReviveUsed) return false;
        erisReviveUsed = true;
        return true;
    }

    // 에리스 변신에 따른 가하는 피해 배율 % (BattleActions가 사용) — NORMAL 100 / ANGER +20 / MAD +50
    public int ErisAttackPercent()
    {
        if(player == null || player.character != Character.ERIS) return 100;
        switch(erisMode)
        {
            case ErisMode.MAD: return 100 + BalanceData.Get("ERIS_MAD_ATTACK_PERCENT", 50);
            case ErisMode.ANGER: return 100 + BalanceData.Get("ERIS_ANGER_ATTACK_PERCENT", 20);
            default: return 100;
        }
    }


    IEnumerator ErisAdditionalMadAnimation()
    {
        WaitForSeconds loopTime = new WaitForSeconds(0.1f);
        float haedTimer = Random.Range(1f,2f);
        float lbTimer = Random.Range(1f,2f);
        float ltTimer = Random.Range(1f,2f);
        float rTimer = Random.Range(1f,2f);
        Spine.TrackEntry track = null;
        while(erisMode == ErisMode.MAD)
        {
            if(haedTimer <= 0f)
            {
                haedTimer = Random.Range(1f,2f);
                track =  anim.state.SetAnimation(1,"VAniHead",false);
                track.MixBlend = Spine.MixBlend.Add;
                track.Alpha = 1f;
            }
            if(lbTimer <= 0f)
            {

                lbTimer = Random.Range(1f,2f);
                if(Random.Range(0,2) == 0)
                    track =  anim.state.SetAnimation(1,"VAniLBArm0",false);
                else
                    track =  anim.state.SetAnimation(1,"VAniLBArm1",false);
                track.MixBlend = Spine.MixBlend.Add;
                track.Alpha = 1f;

            }
            if(ltTimer <= 0f)
            {

                ltTimer = Random.Range(1f,2f);
                if(Random.Range(0,2) == 0)
                    track =  anim.state.SetAnimation(1,"VAniLTArm0",false);
                else
                    track =  anim.state.SetAnimation(1,"VAniLTArm1",false);
                track.MixBlend = Spine.MixBlend.Add;
                track.Alpha = 1f;
            }
            if(rTimer <= 0f)
            {
                rTimer = Random.Range(1f,2f);
                if(Random.Range(0,2) == 0)
                    track =  anim.state.SetAnimation(1,"VAniRArm0",false);
                else
                    track =  anim.state.SetAnimation(1,"VAniRArm1",false);
                track.MixBlend = Spine.MixBlend.Add;
                track.Alpha = 1f;
            }
            haedTimer -= 0.1f;
            lbTimer -= 0.1f;
            ltTimer -= 0.1f;
            rTimer -= 0.1f;
            yield return loopTime;
        }
    }


    public void OnIronDemonAnimationComplete(Spine.TrackEntry trackEntry)
    {
        if(trackEntry.Animation.Name == "Defense")
            ironDemon.GetComponent<SkeletonAnimation>().state.SetAnimation(0,"Idle",true);
    }


    public void ApllyIronDemonAnimationCallbackFunction()
    {
        OnChangedIronDemonLocation(this,this);
    }


}
