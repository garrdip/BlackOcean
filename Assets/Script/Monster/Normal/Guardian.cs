using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class Guardian : SpawnedMonster
{
    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "단일딜후붕괴" :
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.5f);
                GeneralAttack();
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget)){
                    M_EffectManager.instance.RpcEffectNormalMonsterMagicAttack(tar.transform.position, SFX_TYPE.Elite_Devourer, 0);
                }
                yield return new WaitForSeconds(0.5f);
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget)){
                    M_EffectManager.instance.RpcEffectNormalMonsterBang(tar.transform.position, SFX_TYPE.Elite_Devourer, 2);
                    tar.GainBuff(BuffType.BOONGGUI,1,true,false,true,false,parent,null);
                }  
                yield return new WaitForSeconds(0.833f);
                ReturnToIdleAnimation();
                break;
            case "광역붕괴" :
                DoAnimation("Buff0");
                yield return new WaitForSeconds(0.867f);
                foreach(TargetObject tar in M_TurnManager.instance.spawnedPlayerList){
                    M_EffectManager.instance.RpcEffectNormalMonsterBang(tar.transform.position, SFX_TYPE.Elite_Devourer, 2);
                    tar.GainBuff(BuffType.BOONGGUI,1,true,false,true,false,parent,null);
                }
                yield return new WaitForSeconds(0.8f);
                ReturnToIdleAnimation();
                break;
            case "공격후흡혈" :
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.5f);
                GeneralAttack();
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
