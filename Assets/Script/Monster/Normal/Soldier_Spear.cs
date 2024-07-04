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
            case "방어" :
                DoAnimation("Buff0");
                RpcStartSkillEffect(1, "Eff05_Shield", parent.transform.position, SFX_TYPE.Normal_Axe, 6, "Effect");
                parent.GainDefense(nextAction.actionValue);
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
        OnHitAnimationRPC();
        yield return new WaitForSeconds(0.633f);
        ReturnToIdleAnimation();
    }

    [ClientRpc]
    public void OnHitAnimationRPC()
    {
        parent.anim.state.SetAnimation(1,"Defence0",false);
    }

    public override void OnChangedNextTarget(ActionTarget oldVal, ActionTarget newVal)
    {
        switch(nextAction.actionName){
            case "찌르기" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACK,true,nextTarget,nextAction.actionValue.ToString());
                break;
            case "방어" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,nextTarget,nextAction.actionValue.ToString());
                break;
        }
    }
}
