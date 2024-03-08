using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class SpearManA : SpawnedMonster
{
    public override IEnumerator DoAction()
    {
        Debug.Log(nextAction.actionName);
        switch(nextAction.actionName){
            case "준비상태돌입" :
                DoAnimation("Buff0");
                yield return new WaitForSeconds(0.5f);
                yield return new WaitForSeconds(0.833f);
                ReturnToIdleAnimation();
                break;
            case "강공격" :
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.867f);
                GeneralAttack();
                yield return new WaitForSeconds(0.8f);
                ReturnToIdleAnimation();
                break;
            case "APDO" :
                break;
        }
        yield return new WaitForSeconds(1f);
        isActive = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("위치 조정!");
        parent.nextActionIndicator.GetComponent<Transform>().position += new Vector3(0,3,0);
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
        yield return new WaitForSeconds(0.833f);
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

    public override void OnChangedNextTarget(ActionTarget oldVal, ActionTarget newVal)
    {
        switch(nextAction.actionName){
            case "단일딜후붕괴" or "공격후흡혈":
                parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACK,true,nextTarget,nextAction.actionValue.ToString());
                break;
            case "광역붕괴" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,nextTarget,nextAction.actionValue.ToString());
                break;
        }
    }
}
