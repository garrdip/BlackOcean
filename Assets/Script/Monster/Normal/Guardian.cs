using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

// 가디언 — MonsterDB: 단일공격(20, FRONT) / 광역방어(10, WHOLE_ALLY) / 상시 버프 SUHOJA.
// 2026-08-31: Devourer에서 복사된 케이스(단일딜후붕괴/광역붕괴/공격후흡혈)가 DB 행동명과 달라 아무 행동도 하지 않던 것을 DB 기준으로 재작성. 수치는 모두 ActionValue.
public class Guardian : SpawnedMonster
{
    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "단일공격" :
                DoAnimation("Attack0");
                yield return new WaitForSeconds(0.5f);
                GeneralAttack(); // 피해 = ActionValue(위험도 보정) + 힘 버프
                foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget)){
                    RpcStartSkillEffect(0, "Eff3_MagicAttack", tar.transform.position, SFX_TYPE.Elite_Devourer, 0, "Effect");
                }
                yield return new WaitForSeconds(0.833f);
                ReturnToIdleAnimation();
                break;
            case "광역방어" :
                DoAnimation("Buff0");
                yield return new WaitForSeconds(0.867f);
                foreach(TargetObject tar in M_TurnManager.instance.spawnedMonsterList){
                    RpcStartSkillEffect(1, "Eff2_Bang", tar.transform.position, SFX_TYPE.Elite_Devourer, 2, "Effect");
                    tar.GainDefense(ScaledDefense(nextAction.actionValue)); // 실드 = ActionValue(위험도 보정)
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
        parent.nextActionIndicator.GetComponent<Transform>().position += new Vector3(0,3,0); // 키가 커서 예고 표시를 위로
    }

    [Server]
    public override IEnumerator OnHitAnimation()
    {
        return PlayHitAnimationSequence("Defense0", 0.833f);
    }

    public override void OnChangedNextTarget(ActionTarget oldVal, ActionTarget newVal)
    {
        switch(nextAction.actionName){
            case "단일공격" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACK,true,nextTarget,CurrentAttackDamage(nextAction.actionValue).ToString());
                break;
            case "광역방어" :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,true,ActionTarget.WHOLE_ALLY,ScaledDefense(nextAction.actionValue).ToString());
                break;
        }
    }
}
