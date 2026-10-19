using ProjectD;

/// <summary>
/// 한 번의 타격(피해를 주는 한 판정)을 표현하는 컨텍스트 — 아이템 개편 Phase 1 (ToDo_RPG_ITEM 1-3).
/// BattleActions.ResolveHit이 공격 측 계산(명중·회피 → 랜덤 보정 → 약점 → 치명 → 배율)을 채우고,
/// 대상 종류별 피격 함수(TargetObject.DamageToPlayer / DamageToMonster)가 방어 측을 처리한 뒤 실제 HP 피해를 dealt에 돌려준다.
/// 플레이어 → 몬스터, 몬스터 → 플레이어, 반격·반사(2차 판정)가 모두 같은 구조를 쓴다.
/// </summary>
public class HitContext
{
    // ---- 입력 (호출부가 채움) ----
    public TargetObject attacker;            // 공격자 (플레이어 또는 몬스터 유닛)
    public TargetObject target;              // 대상
    public int baseDamage;                   // 공격 측 기본 피해 — 스탯 x 계수 x 대열 보정 x 변신 배율까지 적용된 값 (BasicAttackDamage / SkillDamage / 몬스터 ActionValue)
    public AttackAttribute attribute = AttackAttribute.NONE; // 공격 속성 (약점 판정·방어 스탯 선택)
    public bool isSkill;                     // 스킬 타격 여부 (스킬 추가 데미지 옵션 등 — 기본 공격 제외 효과의 기준)
    public float powerMultiplier = 1f;       // 콤보 배율 슬롯 (후속 연계 기획 — 지금은 항상 1.0, 동작 변화 없음)
    public int bonusPercent;                 // 추가 피해 % (기사도, 스킬 추가 데미지 옵션 등 — 합산 후 1회 적용)
    public bool isSecondary;                 // 반격·반사 등 2차 판정 — 재귀(반격의 반격)를 막는다

    // ---- 결과 (ResolveHit이 채움) ----
    public bool miss;                        // 명중·회피 단일 판정 실패 — 피해·부가 효과 전부 생략
    public bool critical;                    // 치명타
    public bool weakness;                    // 약점 속성 적중
    public int randomPercent;                // 랜덤 보정 r (−DAMAGE_RANDOM_PERCENT ~ +DAMAGE_RANDOM_PERCENT)
    public int finalDamage;                  // 공격 측 계산이 끝난 피해 (방어 측 적용 전)
    public int dealt;                        // HP에 실제로 들어간 피해 (실드 흡수·방어 경감 제외) — 분노·생명력 흡수·반사의 기준값

    /// <summary>판정이 끝나 피해 적용까지 갔는지 (MISS가 아니면 true — 부가 효과를 붙여도 되는지의 기준)</summary>
    public bool Landed => !miss;
}
