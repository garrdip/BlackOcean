using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class Boss_Momos : SpawnedMonster
{
    public override IEnumerator DoAction()
    {
        DoAnimation("Attact0");
        yield return new WaitForSeconds(0.4f);
        GeneralAttack();
        yield return new WaitForSeconds(0.4f);
        ReturnToIdleAnimation();

        yield return new WaitForSeconds(1f);
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
        yield return new WaitForSeconds(0.667f);
        ReturnToIdleAnimation();
    }

    [ClientRpc]
    public void OnHitAnimationRPC()
    {
        parent.anim.state.SetAnimation(1,"Defense0",false);
    }

    [ClientRpc]
    public override void ReturnToIdleAnimation()
    {
        parent.anim.state.SetAnimation(1,"Idle",true);
    }

    public override void OnChanedNextAction(MonsterAction oldVal, MonsterAction newVal)
    {
        switch(nextAction.actionName){
            case "찌르기" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACK,true,newVal.actionTarget,newVal.actionValue.ToString());
                break;
            case "방어" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,newVal.actionTarget,newVal.actionValue.ToString());
                break;
        }
    }
}
