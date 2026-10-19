using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

// 거인병 — MonsterDB: SinglePattern(30, FRONT). 방어 x3(방어력 +10/+15/+20 3턴 — BalanceDB GIANTSOLDIER_DEFENSE_UP_BASE/PER_STEP) 뒤 강공격의 4단계 순환.
// 아이템 개편 Phase 2: 실드 → 방어력 버프 임시 전환 (실드 파괴 시 단계 리셋 규칙은 실드와 함께 폐지)
public class GiantSoldier : SpawnedMonster
{
    [SyncVar]
    int currentLevel = 0;

    // 단계별 방어력 상승량 — 10/15/20 (구 실드 리터럴을 BalanceDB로)
    static int DefenseStep(int level) => BalanceData.Get("GIANTSOLDIER_DEFENSE_UP_BASE", 10) + level * BalanceData.Get("GIANTSOLDIER_DEFENSE_UP_PER_STEP", 5);

    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "SinglePattern" :
                switch(currentLevel)
                {
                    case 0 :
                    case 1 :
                    case 2 :
                        DoAnimation("Buff0");
                        yield return new WaitForSeconds(0.5f);
                        RpcStartSkillEffect(0, "Eff05_Shield", parent.transform.position, SFX_TYPE.Normal_Axe, 6, "Effect");
                        GainDefenseBuff(parent, DefenseStep(currentLevel)); // 방어력 +10/15/20 3턴 (Phase 2 임시 전환 — 구 실드)
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
            AudioClip audioClip = M_SoundManager.instance.GetSFXClipAt(SFX_TYPE.Elite_GiantSoldier, 2);
            M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        }
    }

    [Server]
    public override IEnumerator OnHitAnimation()
    {
        return PlayHitAnimationSequence("Defense0", 0.633f);
    }

    public override void OnChangedNextTarget(ActionTarget oldVal, ActionTarget newVal)
    {
        switch(currentLevel)
        {
            case 0 :
            case 1 :
            case 2 :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.DEFENSE,false,nextTarget,DefenseStep(currentLevel).ToString());
                break;
            case 3 :
                parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACK,true,ActionTarget.FRONT,ScaledAttack(nextAction.actionValue).ToString());
                break;
        }
    }
}
