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

