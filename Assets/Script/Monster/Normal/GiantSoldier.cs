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
                        RpcStartSkillEffect(0, "Eff05_Shield", parent.transform.position, SFX_TYPE.Normal_Axe, 6, "Effect");
                        parent.GainDefense(10);
                        yield return new WaitForSeconds(0.5f);
                        currentLevel++;
                        ReturnToIdleAnimation();
                        break;
                    case 1 :
                        DoAnimation("Buff0");
                        yield return new WaitForSeconds(0.5f);
                        RpcStartSkillEffect(0, "Eff05_Shield", parent.transform.position, SFX_TYPE.Normal_Axe, 6, "Effect");
                        parent.GainDefense(15);
                        yield return new WaitForSeconds(0.5f);
                        currentLevel++;
                        ReturnToIdleAnimation();
                        break;
                    case 2 :
                        DoAnimation("Buff0");
                        yield return new WaitForSeconds(0.5f);
                        RpcStartSkillEffect(0, "Eff05_Shield", parent.transform.position, SFX_TYPE.Normal_Axe, 6, "Effect");
                        parent.GainDefense(20);
                        yield return new WaitForSeconds(0.5f);
                        currentLevel++;
                        ReturnToIdleAnimation();
                        break;
                    case 3 :
                        DoAnimation("Attact0");
                        yield return new WaitForSeconds(0.7f);
                        RpcGroundAttackEffect(0);
                        GeneralAttack();
                        yield return new WaitForSeconds(0.667f);
                        currentLevel = 0;
                        ReturnToIdleAnimation();
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
    public void RpcGroundAttackEffect(int index)
    {
        Camera.main.GetComponent<Shake>().Shaking();
        Vector3 position = parent.transform.position + new Vector3(-2.75f, 0f, 0f);
        ParticleSystem mainParticleSystem = Instantiate(effectParticles[index], position, Quaternion.identity);
        mainParticleSystem.GetComponent<ParticleSystemRenderer>().sortingOrder = GetComponent<MeshRenderer>().sortingOrder - 1;
        ParticleSystem sidParticle = mainParticleSystem.transform.GetChild(0).GetComponent<ParticleSystem>();
        sidParticle.GetComponent<ParticleSystemRenderer>().sortingOrder = GetComponent<MeshRenderer>().sortingOrder - 1;
        ParticleSystem stoneParticle = mainParticleSystem.transform.GetChild(2).GetComponent<ParticleSystem>();
        stoneParticle.GetComponent<ParticleSystemRenderer>().sortingOrder = GetComponent<MeshRenderer>().sortingOrder - 2;
    }

    [ClientRpc]
    public override void DoAnimation(string actionName)
    {
        base.DoAnimation(actionName);
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
