using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

// 새디 (엘리트, Twins 그룹) — MonsterDB: 두번공격(20, RANDOM_SINGLE) / 힘버프(5) / 방어(100).
// 2026-08-31: 비어 있던 스텁(WacherA 케이스명만 복사)을 DB 행동명 기준으로 구현. 수치는 모두 ActionValue.
// 스파인 애니메이션: Attack0(1.333s) / Attack1(1.667s) / Beff0(1.667s) / Defense0(0.667s) / Idle
public class Saddy : SpawnedMonster
{
    public override MonsterGrade monsterGrade { get { return MonsterGrade.ELITE; } }

    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "두번공격" :
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.6f);
                GeneralAttack(); // 1타 — 피해 = ActionValue(위험도 보정) + 힘 버프
                yield return new WaitForSeconds(0.733f);
                DoAnimation("Attack1");
                yield return new WaitForSeconds(0.7f);
                GeneralAttack(); // 2타
                yield return new WaitForSeconds(0.967f);
                ReturnToIdleAnimation();
                break;
            case "힘버프" :
                DoAnimation("Beff0");
                yield return new WaitForSeconds(0.8f);
                parent.GainBuff(BuffType.ICHI_ATTACK,nextAction.actionValue,false,false,false,false,parent); // 힘 +ActionValue
                yield return new WaitForSeconds(0.867f);
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
            case "두번공격" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACKX2,true,nextTarget,CurrentAttackDamage(nextAction.actionValue).ToString() + " X 2");
                break;
            case "힘버프" :
            case "방어" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,nextTarget,nextAction.actionValue.ToString());
                break;
        }
    }
}
