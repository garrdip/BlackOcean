using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;
using Spine.Unity;

public class WacherA : SpawnedMonster
{

    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "쇠락부여" :
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.5f);
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget))
                    tar.GainBuff(BuffType.SOIRAK,2,true,false,true,false,parent,null);
                yield return new WaitForSeconds(0.833f);
                ReturnToIdleAnimation();
                break;
            case "붕괴부여" :
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.5f);
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget))
                    tar.GainBuff(BuffType.BOONGGUI,2,true,false,true,false,parent,null);
                yield return new WaitForSeconds(0.833f);
                ReturnToIdleAnimation();
                break;
            case "힘감소" :
                DoAnimation("Buff0");
                yield return new WaitForSeconds(0.5f);
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget))
                    tar.GainBuff(BuffType.ICHI_ATTACK,-2,true,false,false,false,parent,null);
                yield return new WaitForSeconds(0.833f);
                ReturnToIdleAnimation();
                break;
            case "방어감소" :
                DoAnimation("Buff0");
                yield return new WaitForSeconds(0.5f);
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget))
                    tar.GainBuff(BuffType.ICHI_DEFENSE,-2,true,false,false,false,parent,null);
                yield return new WaitForSeconds(0.833f);
                ReturnToIdleAnimation();
                break;
            case "APDO" :
                break;
        }
        yield return new WaitForSeconds(0.5f);
        isActive = false;
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
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
        yield return new WaitForSeconds(1f);
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
            case "쇠락부여" or "붕괴부여" or "힘감소" or "방어감소" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACKANDDEBUFF,false,nextTarget,"");
                break;
        }
    }
}
