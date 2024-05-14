using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class Soldier_Spear : SpawnedMonster
{

    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "찌르기" :
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.4f);
                GeneralAttack();
                yield return new WaitForSeconds(0.4f);
                ReturnToIdleAnimation();
                break;
            case "방어" :
                parent.GainDefense(nextAction.actionValue);
                DoAnimation("Buff0");
                yield return new WaitForSeconds(1.7f);
                ReturnToIdleAnimation();
                break;
            case "APDO" :
                break;
        }
        yield return new WaitForSeconds(1f);
        isActive = false;
    }
    
    [ClientRpc]
    public void DoAnimation(string actionName)
    {
        parent.anim.state.SetAnimation(1,actionName,false);
        switch(actionName){
            case "Attack0":
                // 공격 효과음
                AudioClip attackSound= M_SoundManager.instance.sfxClips[SFX_TYPE.Normal_Spear].Find((audioClip) => audioClip.name.Equals("monster_nor_spear_1_1"));
                M_SoundManager.instance.PlaySFX(attackSound, attackSound.length);
                break;
            case "Buff0":
                // 버프 효과음
                AudioClip buffSound = M_SoundManager.instance.sfxClips[SFX_TYPE.Normal_Axe].Find((audioClip) => audioClip.name.Equals("monster_nor_axe_3"));
                M_SoundManager.instance.PlaySFX(buffSound, buffSound.length);
                break;
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
        parent.anim.state.SetAnimation(1,"Defence0",false);
    }

    [ClientRpc]
    public override void ReturnToIdleAnimation()
    {
        parent.anim.state.SetAnimation(1,"Idle",true);
    }

    public override void OnChangedNextTarget(ActionTarget oldVal, ActionTarget newVal)
    {
        switch(nextAction.actionName){
            case "찌르기" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACK,true,nextTarget,nextAction.actionValue.ToString());
                break;
            case "방어" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,nextTarget,nextAction.actionValue.ToString());
                break;
        }
    }

    public override void OnBreakedShield()
    {
        OnBreakedShieldRpc();
    }

    [ClientRpc]
    public void OnBreakedShieldRpc()
    {
        // 실드 파괴음
        AudioClip buffSound = M_SoundManager.instance.sfxClips[SFX_TYPE.Normal_Axe].Find((audioClip) => audioClip.name.Equals("monster_nor_axe_4_3"));
        M_SoundManager.instance.PlaySFX(buffSound, buffSound.length);
    }
}
