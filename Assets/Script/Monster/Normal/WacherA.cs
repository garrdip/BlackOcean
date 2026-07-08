using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class WacherA : SpawnedMonster
{

    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "쇠락부여" :
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.5f);
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget)){
                    RpcStartSkillEffect(0, "Eff2_Bang", tar.transform.position, SFX_TYPE.Elite_Watcher, 0, "Effect");
                    tar.GainBuff(BuffType.SOIRAK,2,true,false,true,false,parent,null);
                }
                yield return new WaitForSeconds(0.833f);
                ReturnToIdleAnimation();
                break;
            case "붕괴부여" :
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.5f);
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget)){
                    RpcStartSkillEffect(0, "Eff2_Bang", tar.transform.position, SFX_TYPE.Elite_Watcher, 1, "Effect");
                    tar.GainBuff(BuffType.BOONGGUI,2,true,false,true,false,parent,null);
                }
                yield return new WaitForSeconds(0.833f);
                ReturnToIdleAnimation();
                break;
            case "힘감소" :
                DoAnimation("Buff0");
                yield return new WaitForSeconds(0.5f);
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget)){
                    RpcStartSkillEffect(0, "Eff2_Bang", tar.transform.position, SFX_TYPE.Elite_Watcher, 2, "Effect");
                    tar.GainBuff(BuffType.ICHI_ATTACK,-2,true,false,false,false,parent,null);
                }
                yield return new WaitForSeconds(0.833f);
                ReturnToIdleAnimation();
                break;
            case "방어감소" :
                DoAnimation("Buff0");
                yield return new WaitForSeconds(0.5f);
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget)){
                    RpcStartSkillEffect(0, "Eff2_Bang", tar.transform.position, SFX_TYPE.Elite_Watcher, 3, "Effect");
                    tar.GainBuff(BuffType.ICHI_DEFENSE,-2,true,false,false,false,parent,null);
                }
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

    [Server]
    public override IEnumerator OnHitAnimation()
    {
        return PlayHitAnimationSequence("Defense0", 1f);
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
