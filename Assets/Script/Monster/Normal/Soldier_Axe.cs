using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class Soldier_Axe : SpawnedMonster
{
    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "두번찍기" :          
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.4f);
                GeneralAttack();
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget))
                {
                    RpcStartSkillEffect(0, "Eff1_Cut", tar.transform.position, SFX_TYPE.Normal_Axe, 0, "Effect");
                }
                yield return new WaitForSeconds(0.4f);
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.4f);
                GeneralAttack();
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget))
                {
                    RpcStartSkillEffect(0, "Eff1_Cut", tar.transform.position, SFX_TYPE.Normal_Axe, 0, "Effect");
                }
                yield return new WaitForSeconds(0.4f);
                ReturnToIdleAnimation();
                break;
            case "모으기" : // 다음 턴 두번찍기(MonsterDB 시퀀스로 보장)의 피해 x ActionValue(2) — 구 '힘증가'(힘 버프) 대체
                ChargeNextAttack(nextAction.actionValue);
                DoAnimation("Buff0");
                RpcStartSkillEffect(1, "Eff04_Buff", parent.transform.position, SFX_TYPE.Normal_Axe, 5, "Effect");
                RpcStartSkillParticle(0, parent.transform.position + new Vector3(0f, 2.5f, 0f));
                yield return new WaitForSeconds(1.7f);
                ReturnToIdleAnimation();
                break;
            case "APDO" :
                break;
        }
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
            case "두번찍기" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACKX2,true,nextTarget,CurrentAttackDamage(nextAction.actionValue).ToString() + " X 2"); // 모으기 직후면 배율 반영된 값
                break;
            case "모으기" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,nextTarget,"x" + nextAction.actionValue.ToString());
                break;
        }
    }
}
