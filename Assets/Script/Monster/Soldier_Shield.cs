using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class Soldier_Shield : SpawnedMonster
{
    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "혼자방어" :
                parent.GainDefense(nextAction.actionValue);
                DoAnimation("Buff0");
                yield return new WaitForSeconds(1.7f);
                ReturnToIdleAnimation();
                break;
            case "팀방어" :
                foreach(TargetObject tar in M_TurnManager.instance.spawnedMonsterList)
                    tar.GainDefense(nextAction.actionValue);
                DoAnimation("Buff0");
                yield return new WaitForSeconds(1.7f);
                ReturnToIdleAnimation();
                break;
            case "APDO" :
                break;
        }
        isActive = false;
    }
    
    [ClientRpc]
    public void DoAnimation(string actionName)
    {
        parent.anim.state.SetAnimation(1,actionName,false);
        switch(actionName){
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
            case "혼자방어" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,nextTarget,nextAction.actionValue.ToString());
                break;
            case "팀방어" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,true,ActionTarget.WHOLE,nextAction.actionValue.ToString());
                break;
        }
    }
}
