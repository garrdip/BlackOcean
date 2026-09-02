using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

// 해피 (엘리트, Twins 그룹) — MonsterDB: 공격후붕괴(50, RANDOM_SINGLE) / 방어(50).
// 2026-08-31: 비어 있던 스텁(WacherA 케이스명만 복사)을 DB 행동명 기준으로 구현. 수치는 모두 ActionValue.
// 스파인 애니메이션: Attack0(1.333s) / Attack1(1.667s) / Beff0(1.667s) / Defense0(0.667s) / Idle
public class Happy : SpawnedMonster
{
    public override MonsterGrade monsterGrade { get { return MonsterGrade.ELITE; } }

    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "공격후붕괴" :
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.6f);
                GeneralAttack(); // 피해 = ActionValue(위험도 보정) + 힘 버프
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget)){
                    if(tar == null || tar.playerHP == 0) continue;
                    tar.GainBuff(BuffType.BOONGGUI,1,true,false,true,false,parent); // 부가 효과 붕괴 1 (DB에 별도 컬럼 없음 — 고정)
                }
                yield return new WaitForSeconds(0.733f);
                ReturnToIdleAnimation();
                break;
            case "방어" :
                DoAnimation("Beff0");
                yield return new WaitForSeconds(0.8f);
                parent.GainDefense(ScaledDefense(nextAction.actionValue)); // 실드 = ActionValue(위험도 보정)
                yield return new WaitForSeconds(0.867f);
                ReturnToIdleAnimation();
                break;
            case "APDO" :
                break;
        }
        yield return new WaitForSeconds(0.5f);
        isActive = false;
    }

    [Server]
    public override IEnumerator OnHitAnimation()
    {
        return PlayHitAnimationSequence("Defense0", 0.667f);
    }

    public override void OnChangedNextTarget(ActionTarget oldVal, ActionTarget newVal)
    {
        switch(nextAction.actionName){
            case "공격후붕괴" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACKANDDEBUFF,true,nextTarget,CurrentAttackDamage(nextAction.actionValue).ToString());
                break;
            case "방어" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,nextTarget,ScaledDefense(nextAction.actionValue).ToString());
                break;
        }
    }
}
