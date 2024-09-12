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
            case "힘증가" :
                parent.GainBuff(BuffType.ICHI_ATTACK,nextAction.actionValue,false,false,false,false,parent.GetComponent<TargetObject>(),null);
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
            case "두번찍기" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACKX2,true,nextTarget,(nextAction.actionValue + parent.GetComponent<TargetObject>().GetBuffValue(BuffType.ICHI_ATTACK)).ToString()  + " X 2");
                break;
            case "힘증가" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,nextTarget,nextAction.actionValue.ToString());
                break;
        }
    }
}
