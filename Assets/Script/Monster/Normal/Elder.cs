using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;


// 치유사(Elder) — 잃은 체력이 가장 많은 아군 몬스터(자신 포함) 하나를 회복시키는 단일 패턴.
// 회복량은 MonsterDB의 단일회복 ActionValue를 그대로 사용한다 (밸런스는 CSV에서 조정).
public class Elder : SpawnedMonster
{
    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "단일회복" :
                TargetObject healTarget = FindMostWoundedAlly();
                DoAnimation("Buff0");
                yield return new WaitForSeconds(0.5f);
                if(healTarget != null){
                    // HP 세터가 서버 전용 + MAXHP 클램프를 처리한다
                    healTarget.monster.HP += nextAction.actionValue;
                    // 이펙트는 프리팹에 등록된 에셋이 있을 때만 재생 (프리팹 구성 전에도 동작하도록)
                    if(effectDataAssets.Count > 0)
                        RpcStartSkillEffect(0, "Eff04_Buff", healTarget.transform.position, SFX_TYPE.Normal_Axe, 5, "Effect");
                    if(effectParticles.Count > 0)
                        RpcStartSkillParticle(0, healTarget.transform.position + new Vector3(0f, 2.5f, 0f));
                }
                yield return new WaitForSeconds(0.833f);
                ReturnToIdleAnimation();
                break;
            case "APDO" :
                break;
        }
        yield return new WaitForSeconds(1f);
        isActive = false;
    }

    // 잃은 체력(MAXHP - HP)이 가장 많은 살아있는 아군 몬스터. 전원이 만피면 자기 자신을 반환한다
    TargetObject FindMostWoundedAlly()
    {
        TargetObject best = parent;
        int bestMissing = 0;
        foreach(TargetObject tar in M_TurnManager.instance.spawnedMonsterList){
            if(tar == null || tar.monster == null || tar.monster.HP <= 0) continue;
            int missing = tar.monster.MAXHP - tar.monster.HP;
            if(missing > bestMissing){
                bestMissing = missing;
                best = tar;
            }
        }
        return best;
    }

    [Server]
    public override IEnumerator OnHitAnimation()
    {
        return PlayHitAnimationSequence("Defense0", 0.633f);
    }

    public override void OnChangedNextTarget(ActionTarget oldVal, ActionTarget newVal)
    {
        // 전용 회복 아이콘이 없어 아군 지원 계열 표기 관례(WacherB 방어)에 맞춰 DEFENSE 아이콘 + 회복량 표시
        parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,nextTarget,nextAction.actionValue.ToString());
    }
}
