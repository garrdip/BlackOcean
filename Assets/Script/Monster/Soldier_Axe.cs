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
                DoAnimation("1Attack");
                yield return new WaitForSeconds(0.4f);
                GeneralAttack();
                yield return new WaitForSeconds(0.4f);
                DoAnimation("1Attack");
                yield return new WaitForSeconds(0.4f);
                GeneralAttack();
                yield return new WaitForSeconds(0.4f);
                ReturnToIdleAnimation();
                break;
            case "힘증가" :
                parent.GainBuff(BuffType.ICHI_ATTACK,nextAction.actionValue,false,false,false,null);
                parent.clone.GainBuff(BuffType.ICHI_ATTACK,nextAction.actionValue,false,false,false,null);
                DoAnimation("1Buff");
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
        Invoke("OnHitAnimationPlayer",0.3f);
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
        parent.anim.state.SetAnimation(1,"1Defence",false);
    }
    
    [ClientRpc]
    public override void ReturnToIdleAnimation()
    {
        parent.anim.state.SetAnimation(1,"1Idle",true);
    }
}
