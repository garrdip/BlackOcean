using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class WacherB : SpawnedMonster
{


    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "힘증가" :
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.5f);
                nextTargetObject.GainBuff(BuffType.ICHI_ATTACK,4,false,false,false,false,parent,null);
                RpcStartSkillEffect(0, "Eff04_Buff", parent.transform.position, SFX_TYPE.Normal_Axe, 5, "Effect");
                yield return new WaitForSeconds(0.833f);
                ReturnToIdleAnimation();
                break;
            case "방어" :
                DoAnimation("Buff0");
                yield return new WaitForSeconds(0.5f);
                parent.GainDefense(15);
                RpcStartSkillEffect(1, "Eff05_Shield", parent.transform.position, SFX_TYPE.Normal_Axe, 6, "Effect"); ;
                yield return new WaitForSeconds(0.833f);
                ReturnToIdleAnimation();
                break;
            case "광역힘증가" :
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.5f);
                foreach(TargetObject tar in M_TurnManager.instance.spawnedMonsterList){
                    RpcStartSkillEffect(0, "Eff04_Buff", tar.transform.position, SFX_TYPE.Normal_Axe, 5, "Effect");
                    tar.GainBuff(BuffType.ICHI_ATTACK,2,false,false,false,false,parent,null);
                }
                yield return new WaitForSeconds(0.833f);
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
        parent.nextActionIndicator.GetComponent<Transform>().position += new Vector3(0,3,0);
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

    public override void OnChangedNextTarget(ActionTarget oldVal, ActionTarget newVal)
    {
        switch(nextAction.actionName){
            case "힘증가" or "광역힘증가" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACK,false,nextTarget,"");
                break;
            case "방어" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,nextTarget,nextAction.actionValue.ToString());
                break;
        }
    }
}
