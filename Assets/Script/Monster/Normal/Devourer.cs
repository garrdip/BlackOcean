using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class Devourer : SpawnedMonster
{
    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "단일딜후붕괴" :
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.5f);
                GeneralAttack();
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget)){
                    RpcStartSkillEffect(0, "Eff3_MagicAttack", tar.transform.position, SFX_TYPE.Elite_Devourer, 0, "Effect");
                    RpcStartSkillParticle(0, tar.transform.position + new Vector3(0f, 3.5f, 0f));
                }
                yield return new WaitForSeconds(0.5f);
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget)){
                    RpcStartSkillEffect(1, "Eff2_Bang", tar.transform.position, SFX_TYPE.Elite_Devourer, 1, "Effect");
                    tar.GainBuff(BuffType.BOONGGUI,1,true,false,true,false,parent,null);
                }  
                yield return new WaitForSeconds(0.833f);
                ReturnToIdleAnimation();
                break;
            case "광역붕괴" :
                DoAnimation("Buff0");
                yield return new WaitForSeconds(0.867f);
                foreach(TargetObject tar in M_TurnManager.instance.spawnedPlayerList){
                    RpcStartSkillEffect(1, "Eff2_Bang", tar.transform.position, SFX_TYPE.Elite_Devourer, 1, "Effect");
                    tar.GainBuff(BuffType.BOONGGUI,1,true,false,true,false,parent,null);
                }
                yield return new WaitForSeconds(0.8f);
                ReturnToIdleAnimation();
                break;
            case "공격후흡혈" :
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.5f);
                GeneralAttack();
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget)){
                    RpcStartSkillEffect(0, "Eff3_MagicAttack", tar.transform.position, SFX_TYPE.Elite_Devourer, 2, "Effect");
                    RpcStartSkillParticle(0, tar.transform.position + new Vector3(0f, 3.5f, 0f));
                }
                if(HP + nextAction.actionValue + parent.GetBuffValue(BuffType.ICHI_ATTACK) > MAXHP)
                    HP = MAXHP;
                else
                    HP += nextAction.actionValue + parent.GetBuffValue(BuffType.ICHI_ATTACK);
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
        Debug.Log("위치 조정!");
        parent.nextActionIndicator.GetComponent<Transform>().position += new Vector3(0,3,0);
    }

    [Server]
    public override IEnumerator OnHitAnimation()
    {
        return PlayHitAnimationSequence("Defense0", 0.833f);
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
