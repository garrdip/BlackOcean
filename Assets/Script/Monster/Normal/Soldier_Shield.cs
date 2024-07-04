using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class Soldier_Shield : SpawnedMonster
{
    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "혼자방어" :
                DoAnimation("Buff0");
                RpcStartSkillEffect(0, "Eff05_Shield", parent.transform.position, SFX_TYPE.Normal_Axe, 6, "Effect");
                parent.GainDefense(nextAction.actionValue);
                yield return new WaitForSeconds(1.7f);
                ReturnToIdleAnimation();
                break;
            case "팀방어" :
                DoAnimation("Buff0");
                foreach(TargetObject tar in M_TurnManager.instance.spawnedMonsterList){
                    RpcStartSkillEffect(0, "Eff05_Shield", tar.transform.position, SFX_TYPE.Normal_Axe, 6, "Effect");
                    tar.GainDefense(nextAction.actionValue);
                }
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
            case "혼자방어" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,nextTarget,nextAction.actionValue.ToString());
                break;
            case "팀방어" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,true,ActionTarget.WHOLE,nextAction.actionValue.ToString());
                break;
        }
    }
}
