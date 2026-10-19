using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class SpearManB : SpawnedMonster
{
    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "광공격후버프" :
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.5f);
                GeneralAttack();
                foreach(TargetObject tar in M_TurnManager.instance.spawnedMonsterList){
                    RpcStartSkillEffect(0, "Eff0_Sting", tar.transform.position, SFX_TYPE.Normal_Spear, 3, "Effect");
                    tar.GainBuff(BuffType.ICHI_ATTACK,2,false,false,false,false,parent);
                }
                yield return new WaitForSeconds(0.833f);
                ReturnToIdleAnimation();
                break;
            case "광방어후버프" :
                DoAnimation("Buff0");
                yield return new WaitForSeconds(0.867f);
                foreach(TargetObject tar in M_TurnManager.instance.spawnedMonsterList)
                {
                    RpcStartSkillEffect(0, "Eff05_Shield", tar.transform.position, SFX_TYPE.Normal_Axe, 6, "Effect");
                    GainDefenseBuff(tar, nextAction.actionValue);                               // 방어력 +ActionValue 3턴 (Phase 2 임시 전환 — 구 실드)
                    tar.GainBuff(BuffType.STAT_DEFENSE,2,false,false,false,false,parent);     // 전투 동안 누적 방어력 +2 (구 ICHI_DEFENSE 가산 → STAT_DEFENSE 합산, 2-4)
                }
                yield return new WaitForSeconds(0.8f);
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
        return PlayHitAnimationSequence("Defense0", 0.833f);
    }

    public override void OnChangedNextTarget(ActionTarget oldVal, ActionTarget newVal)
    {
        switch(nextAction.actionName){ // MonsterDB 행동명과 일치시킴 (Devourer에서 복사된 잘못된 케이스였음 — 예고가 표시되지 않던 버그)
            case "광공격후버프" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACK,true,nextTarget,CurrentAttackDamage(nextAction.actionValue).ToString());
                break;
            case "광방어후버프" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,true,ActionTarget.WHOLE_ALLY,nextAction.actionValue.ToString());
                break;
        }
    }
}
