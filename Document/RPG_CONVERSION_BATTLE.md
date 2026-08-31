## 전투 규칙
모든 상수값은 BalanceDB.csv에서 관리함.

## TP 획득량
- 오프셋(base) 50 + 캐릭터(몬스터)의 민첩 만큼 TP 획득
- 오프셋 수치는 공개하지 않고, balanceDB에 저장해서 불러오며 사용할 것.

## 데미지 공식 (물리,마법)
데미지 절대감소 상수 = 10
데미지 상대감소 상수 = 100
데미지 절대 감소 : 방어력/(데미지 절대감소 상수) 소숫자리 버림 수치
최종 데미지 = (데미지 - 데미지 절대감소)/(1 + 방어력/(데미지 상대감소 상수))

## 분노 
- 분노의 최대치 100 (이값을 넘기지 않음)
**생성공식**
변환제어 = 제어 + 50(변환제어 상수)
- 때릴때 : (데미지/몬스터의 최대HP:이값은 1을 넘기지 않음)*(변환제어)
- 맞을때 : (데미지/게오르크의 최대HP)*(변환제어)

## MP 회복 공식 (홍단향)
변환제어 = 제어 + 50(변환제어 상수)
매턴 : 제어/2
전투 종료후 : 제어

## 구현 (2026-08-26)
- BalanceDB 키: TP_GAIN_BASE(50) / DMG_FLAT_REDUCE_DIVISOR(10) / DMG_RELATIVE_REDUCE_DIVISOR(100) / RAGE_CONTROL_OFFSET(50) / MP_REGEN_CONTROL_DIVISOR(2).
  구 상수(RAGE_GAIN_ON_DEAL/ON_TAKEN, MP_REGEN_PER_TURN)는 제거.
- TP: M_TurnManager.TpBattle.GetUnitTpGain = TP_GAIN_BASE + 민첩 (게이지 충전·선충전 공용).
- 데미지: BattleActions.ApplyDefenseFormula — TP 전투에서 플레이어 피격 시 대열 보정 후 적용. 공격 속성 MAGIC이면 마법방어, 그 외는 방어력
  (몬스터 공격 속성 = MonsterStatDB AttackAttribute, DamageToPlayer에 전달). 고정피해(StaticDamage)는 기존대로 공식 미적용.
- 분노: BattleActions.GainRageByDamage — 때릴때 (최종 데미지/몬스터 최대HP, 1 상한) x 변환제어, 맞을때 (방어력 적용 전 데미지/자신 최대HP) x 변환제어.
  최대 100은 maxResource(CharacterStatDB BaseResource) 클램프. 고행길 등 고정 획득(GainRage)은 별도 유지.
- MP: 자기 턴 시작 제어/2, 전투 종료(TpBattleLoop 종료) 시 제어 전량 회복.
- 대열: playerOrder[2]=전열(몬스터와 가까움)/[1]=중열/[0]=후열 — 몬스터 타겟 규칙(ActionTarget.FRONT→playerOrder[2])과 동일 기준. 대열 보정은 방어 공식 이전에 적용.
- 방어 액션: 실드 = 방어력 스탯 + DEFEND_BASE_VALUE(5). **실드 소모는 방어력 공식 미적용** — 대열 보정된 원 데미지 그대로 깎이고(공격 9·전열이면 실드 10 소모), 실드를 뚫고 남은 피해에만 방어력 감소 공식이 적용된다.

## 버프 디버프의 스택이 깍이는 시점.
- 시전자의 턴이 돌아올때.


## 몬스터 모으기(충전) — 2026-08-31
- `SpawnedMonster.chargeMultiplier`(SyncVar) — '모으기' 행동이 `ChargeNextAttack(배율)`로 세팅, 다음 공격의 `GeneralAttack`이 `CurrentAttackDamage = (위험도 보정 공격력 + 힘 버프) x 배율`로 적용, 행동 종료 시 `OnActionFinished()`가 소모 (턴 매니저가 DoAction 완료 뒤 호출 — TP/구 경로 공통).
- "다음 턴 무조건 공격"은 MonsterDB의 **시퀀스 행동**으로 보장한다: 한 행에 `모으기,배율,, 공격,값,대상`처럼 행동을 이어 쓰면 `GetNextAction`이 순서대로 실행한다.
- 적용: `Soldier_Axe` 20% 행동 `힘증가(힘 버프 +2)` → `모으기(x2) → 두번찍기 8 FRONT`. 인디케이터는 모으기 직후 두번찍기 예고에 배율 반영된 피해(예: 16 X 2)를 표시.
- 적용: `Soldier_Spear` 50% 행동 `방어(실드 +5)` → `모으기(x2) → 찌르기 10 RANDOM_MIDDLE_BACK` (예고 20).

## 몬스터 수치는 MonsterDB ActionValue가 원본 — 2026-08-31 점검
- 공격 피해: 모든 몬스터가 `SpawnedMonster.GeneralAttack()` → `CurrentAttackDamage(nextAction.actionValue)` = `(ActionValue + round(위험도 x HazardAtk) + 힘 버프) x 모으기 배율`. 최종 피해는 `TargetObject.DamageToPlayer`가 대열 보정(전열 120%/후열 80%) → 실드 → 방어력 공식을 적용하므로 표기값과 다를 수 있음(의도).
- 버프/디버프/실드 수치도 ActionValue로 통일: WacherA(쇠락/붕괴/힘감소/방어감소), WacherB(힘증가/방어/광역힘증가), Devourer 광역붕괴, Guardian 광역방어, Happy 방어, Saddy 힘버프/방어. 기존엔 리터럴(DB와 같은 값)이 박혀 있어 CSV를 바꿔도 반영되지 않았다.
- 부가 효과(주 행동에 딸린 붕괴 1, 버프 +2 등)는 DB에 컬럼이 없어 리터럴 유지: Devourer/Happy 공격후붕괴의 붕괴 1, SpearManB 버프 +2, GiantSoldier SinglePattern의 실드 10/15/20.
- 행동명 불일치 수정: Guardian(DB 단일공격/광역방어인데 코드는 Devourer 케이스 → 무행동), SpearManA/B 예고 케이스, Happy/Saddy(스텁 → 구현). 미구현: Boss_Apates/Boss_Geras(DoAction 없음 — DB SingleAction/Enrage 미대응), Assassin/Executioner/Elder(base, DB 행 없음), E3/E4/치유사(DB 행만 존재).

## 몬스터 행동 예고 수치 숨김 — 2026-08-31
- 플레이어는 몬스터의 다음 행동 **종류(아이콘)와 대상(열)**만 알 수 있고 피해량은 알 수 없어야 한다. `NextActionIndicator.ShowActionValue = false`로 숫자 텍스트를 숨김 (몬스터 스크립트가 넘기는 value는 무시 — 호출부 미수정, 디버그 시 true로 되돌릴 수 있음).
