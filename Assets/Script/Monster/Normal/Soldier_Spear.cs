using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class Soldier_Spear : SpawnedMonster
{

    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "찌르기" :
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.4f);
                GeneralAttack();
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget))
                {
                    RpcStartSkillEffect(0, "Eff0_Sting", tar.transform.position, SFX_TYPE.Normal_Spear, 1, "Effect");
                }
                yield return new WaitForSeconds(0.4f);
                ReturnToIdleAnimation();
                break;
            case "모으기" : // 다음 턴 찌르기(MonsterDB 시퀀스로 보장)의 피해 x ActionValue(2) — 구 '방어'(실드 획득) 대체
                ChargeNextAttack(nextAction.actionValue);
                DoAnimation("Buff0");
                RpcStartSkillEffect(1, "Eff05_Shield", parent.transform.position, SFX_TYPE.Normal_Axe, 6, "Effect");
                yield return new WaitForSeconds(1.7f);
                ReturnToIdleAnimation();
                break;
            case "APDO" :
                break;
        }
        yield return new WaitForSeconds(1f);
        isActive = false;
    }

    [Server]
    public override IEnumerator OnHitAnimation()
    {
        return PlayHitAnimationSequence("Defence0", 0.633f);
    }

    public override void OnChangedNextTarget(ActionTarget oldVal, ActionTarget newVal)
    {
        switch(nextAction.actionName){
            case "찌르기" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACK,true,nextTarget,CurrentAttackDamage(nextAction.actionValue).ToString()); // 모으기 직후면 배율 반영된 값
                break;
            case "모으기" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,nextTarget,"x" + nextAction.actionValue.ToString());
                break;
        }
    }
}
