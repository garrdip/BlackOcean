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
                parent.GainDefense(nextAction.actionValue);
                DoAnimation("2Buff");
                yield return new WaitForSeconds(1.7f);
                ReturnToIdleAnimation();
                break;
            case "팀방어" :
                foreach(TargetObject tar in M_TurnManager.instance.spawnedMonsterList)
                    tar.GainDefense(nextAction.actionValue);
                DoAnimation("2Buff");
                yield return new WaitForSeconds(1.7f);
                ReturnToIdleAnimation();
                break;
        }
        isActive = false;
    }
    
    [ClientRpc]
    public void DoAnimation(string actionName)
    {
        parent.anim.state.SetAnimation(1,actionName,false);
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
        parent.anim.state.SetAnimation(1,"2Defence",false);
    }

    [ClientRpc]
    public override void ReturnToIdleAnimation()
    {
        parent.anim.state.SetAnimation(1,"2Idle",true);
    }

    public override void OnChanedNextAction(MonsterAction oldVal, MonsterAction newVal)
    {
        switch(nextAction.actionName){
            case "혼자방어" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,newVal.actionTarget,newVal.actionValue.ToString());
                break;
            case "팀방어" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,true,ActionTarget.WHOLE,newVal.actionValue.ToString());
                break;
        }
    }

}
