using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class GiantSoldier : SpawnedMonster
{
    [SyncVar]
    int currentLevel = 0;

    public override void OnBreakedShield()
    {
        if(isServer)
            currentLevel = 0;
        OnBreakedShieldRPC();
    }

    [ClientRpc]
    void OnBreakedShieldRPC()
    {
        OnChangedNextTarget(nextTarget,nextTarget);
    }

    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "SinglePattern" :
                switch(currentLevel)
                {
                    case 0 :
                        DoAnimation("Buff0");
                        yield return new WaitForSeconds(0.5f);
                        M_EffectManager.instance.RpcEffectNormalMonsterShield(parent.transform.position, SFX_TYPE.Normal_Axe, 6);
                        parent.GainDefense(10);
                        yield return new WaitForSeconds(0.5f);
                        currentLevel++;
                        break;
                    case 1 :
                        DoAnimation("Buff0");
                        yield return new WaitForSeconds(0.5f);
                        M_EffectManager.instance.RpcEffectNormalMonsterShield(parent.transform.position, SFX_TYPE.Normal_Axe, 6);
                        parent.GainDefense(15);
                        yield return new WaitForSeconds(0.5f);
                        currentLevel++;
                        break;
                    case 2 :
                        DoAnimation("Buff0");
                        yield return new WaitForSeconds(0.5f);
                        M_EffectManager.instance.RpcEffectNormalMonsterShield(parent.transform.position, SFX_TYPE.Normal_Axe, 6);
                        parent.GainDefense(20);
                        yield return new WaitForSeconds(0.5f);
                        currentLevel++;
                        break;
                    case 3 :
                        DoAnimation("Attact0");
                        yield return new WaitForSeconds(1f);
                        GeneralAttack();
                        yield return new WaitForSeconds(0.667f);
                        currentLevel = 0;
                        break;
                }
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
        parent.nextActionIndicator.GetComponent<Transform>().position += new Vector3(0,1,0);
    }

    [ClientRpc]
    public void DoAnimation(string actionName)
    {
        parent.anim.state.SetAnimation(1,actionName,false);
        // 거병은 예외적으로 공격 SFX 호출을 애니매이션과 동일한 시점에 재생(추후 조정 필요)
        if(actionName.Equals("Attact0")){
            AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.Elite_GiantSoldier][2];
            M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        }
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
        parent.anim.state.SetAnimation(1,"Defense0",false);
    }

    [ClientRpc]
    public override void ReturnToIdleAnimation()
    {
        parent.anim.state.SetAnimation(1,"Idle",true);
    }

    public override void OnChangedNextTarget(ActionTarget oldVal, ActionTarget newVal)
    {
        Debug.Log("Changed Next Target");
        switch(currentLevel)
        {
            case 0 : 
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,nextTarget,"10");
                break;
            case 1 : 
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,nextTarget,"15");
                break;
            case 2 : 
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,nextTarget,"20");
                break;
            case 3 :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACK,true,ActionTarget.FRONT,"30");
                break;
        }
    }
}
