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
                DoAnimation("Buff0");
                RpcStartSkillEffect(0, "Eff05_Shield", parent.transform.position, SFX_TYPE.Normal_Axe, 6, "Effect");
                GainDefenseBuff(parent, nextAction.actionValue); // 방어력 +ActionValue 3턴 (Phase 2 임시 전환 — 구 실드)
                yield return new WaitForSeconds(1.7f);
                ReturnToIdleAnimation();
                break;
            case "팀방어" :
                DoAnimation("Buff0");
                foreach(TargetObject tar in M_TurnManager.instance.spawnedMonsterList){
                    RpcStartSkillEffect(0, "Eff05_Shield", tar.transform.position, SFX_TYPE.Normal_Axe, 6, "Effect");
                    GainDefenseBuff(tar, nextAction.actionValue);
                }
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
        return PlayHitAnimationSequence("Defence0", 0.633f);
    }

    public override void OnChangedNextTarget(ActionTarget oldVal, ActionTarget newVal)
    {
        switch(nextAction.actionName){
            case "혼자방어" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,nextTarget,nextAction.actionValue.ToString());
                break;
            case "팀방어" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,true,ActionTarget.WHOLE_ALLY,nextAction.actionValue.ToString());
                break;
        }
    }
}
