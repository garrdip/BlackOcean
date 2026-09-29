# 아이템 개편 구현 워크플로우 (판정·몬스터 방어력·무기·방어구·악세사리·소모품)

> 기준 문서: `RPG_CONVERSION_DESIGN.md` — 「보조 스탯」「일반 데미지」「치명타」「속성」「약점 공격」「판정 순서」「무기」「방어구」「장비 옵션」「아이템」「거점」「멀티플레이어 변경점」 절 (**2026-09-28 개정판 + 09-29 방어구 규칙 보완**)
> 현재 구현 기준: `DB/EquipData.cs`(플랫 스탯 16행) · `DB/ConsumableData.cs`(물약 3행) · `Player/GamePlayer.Equipment.cs`(SyncList<string> 인벤토리, OnGUI 임시 창) · `Battle/BattleActions.cs`(피해 계산) · `Battle/TargetObject.Damage.cs`(피격) · `Mangers/RewardService.cs`(드랍) · `SaveManager/GameSaveService.cs`(프로필 저장)
> 원칙: **매 Phase가 끝나면 게임이 실행 가능한 상태**를 유지한다. 판정(명중·회피·치명)은 장비 옵션보다 먼저 넣는다 — 옵션 대부분이 판정 수치를 조절하기 때문이다. **수치·목록은 전부 CSV(BalanceDB 포함)로 관리하고 코드에 리터럴을 두지 않는다.** 멀티 검증은 ParrelSync 2클라이언트 기준으로 한다.
> 진행 방식: 항목별 [ ] 체크 → 에디터 플레이 테스트 → 한글 커밋. 구현 확정 사항은 `RPG_CONVERSION_DESIGN.md`의 해당 절에 "구현 (날짜)" 줄로 되돌려 적는다.

### 갱신 이력
- 2026-09-22 초안 (보조 스탯·장비 옵션 등급·물약)
- 2026-09-29 기획 09-28 개정 반영 — 판정(랜덤 보정·치명 TP·약점 공식), 무기 착용 제한 제거·무기 종류, 속성 6종·방어구 약점, 약점 상쇄 옵션, 메르크리우스 역할 변경
- **2026-09-29 확정 결정(Q1~Q24) 반영**
    - Phase 2 **몬스터 방어력 전환** 신설 (Q14 — 몬스터 실드 폐기, 방어력 스탯으로 대체, 치명 방어 경감을 몬스터에도 적용)
    - Phase 1에 **전투 계산 파이프라인 일원화**(`HitContext`·`SkillCast`) 추가 — 플레이어·몬스터 공식 공통화(Q18), 콤보 배율 슬롯 선구현(Q10)
    - 약점 TP 반복 체감 **유지**, 감소율은 BalanceDB (Q12)
    - 옵션 수치는 **등급별 범위를 CSV에 직접 기록** (Q3) — 등급별 배율 키 폐기
    - 무기는 **물리·마법 공격력을 모두 가짐** (Q22), 계수 불일치 경고 폐기
    - 방어구 종류가 **갑옷·투구·신발 모두**에 붙고, 세트 혼합·미착용·힘 요구치 규칙 추가 (Q16, 기획서 「방어구」)
    - 약점 상쇄 옵션은 **속성 1가지**를 지정 (Q23)
    - **몬스터별 드랍 테이블** (Q24), 메르크리우스 **상점(기본템 구매·판매)** (Q20), 악세사리 가챠는 **후순위** (Q19)
    - 소모품을 **효과 바인딩 구조**로 재설계 — 몬스터 대상 사용·극약 등 신규 효과를 CSV 행 + 메서드로 추가 (Q5, Q6)
    - 신규 버프는 `ICHI_*`에 의존하지 않는다 — 버프 개편 예정 (Q15)
    - 구 유물 시스템 삭제를 범위 안으로 (Q2, Q21)
    - Phase 번호 재정렬 (신 Phase 2 삽입, 이후 한 칸씩 밀림)

---

## 현재 구현 현황 (2026-09-29 조사)

이 문서의 Phase 0~9는 **아직 착수 전**이다.

이미 있어서 재사용하는 것:
| 항목 | 위치 | 비고 |
|---|---|---|
| 장비 슬롯 6칸 (무기·갑옷·투구·신발·악세사리×2) | `EquipSlot`(ProjectD), `GamePlayer.Equipment.CmdEquip` | 같은 슬롯은 자동 교체, 전투 중 착탈 불가 |
| 합산 스탯 | `GamePlayer.Total*` (힘·지능·민첩·방어·마방), `ApplyEquipMaxDeltas`(MaxHP/자원 최대치) | 전투 코드가 모두 `Total*`을 쓴다 |
| 제어 스탯 | `GamePlayer.control` — 성장·스킬트리 CTRL 노드·세이브·분노 변환제어·MP 회복 | 장비 합산(`TotalControl`)만 없음 |
| 지속 턴 버프 | `M_TurnManager.ApplyTimedDebuffTo` / `TickTimedDebuffs` | 지속·감소 로직은 재사용. 단 `ICHI_ATTACK/ICHI_DEFENSE` 등 버프 타입 자체는 버프 개편 때 삭제 예정 (Q15) |
| 물약 사용 경로 | 전투 `TpAction.ITEM`(TpBattle ITEM 분기) / 비전투 `CmdUsePotionOnMap` | 두 곳에 switch가 중복됨. 비전투 HP 회복은 `HealPlayer`를 거치지 않음 |
| 전투 승리 드랍 | `RewardService.DistributeBattleRewards` — 장비 30%(`EQUIP_DROP_MIN_HAZARD` 이상)/물약 40%, 균등 뽑기 | 싱글은 GamePlayer 3명 각각 굴림. 몬스터별 차이 없음 |
| 처치 집계 | `M_TurnManager.battleExpPool` (몬스터 사망 시 경험치 적립) | 드랍 테이블용 처치 목록을 같은 자리에 추가 |
| 약점 판정 (몬스터 피격) | `BattleActions.AttackTarget` → `WEAKNESS_DAMAGE_PERCENT`(120) + `M_TurnManager.ApplyTpBreakTo` | 반복 시 절반 체감(`tpWeaknessHits`)은 유지, 공식은 Phase 1에서 교체 |
| 방어 공식 | `BattleActions.ApplyDefenseFormula` — 플레이어 피격에만 적용 | 몬스터 피격에는 방어 공식이 없다 (실드만) |

기획과 어긋나는 현재 코드 (Phase에서 정리):
- `EquipDB.csv` 헤더 `EquipNo,Name,Slot,Character,RequireLevel,Grade,Attack,Agility,Defense,MagicDefense,MaxHP,MaxResource,SkillLevel,Price,Description`
    - `Character` 컬럼과 `CmdEquip`의 캐릭터 전용 검증은 기획 "캐릭터별 착용 제한 없음"과 충돌한다
    - `Grade`는 구 유물용 `ItemGrade`로 파싱만 하고 어디서도 읽지 않는다
    - `SkillLevel`(AC3 숙련의 부적)은 크리스털 원칙과 충돌한다
    - 신발 BO1/BO2에는 방어·마방이 0이다 (기획상 신발 기본 옵션은 방어력·마법방어)
    - 무기 GW2/HW2에 방어·마방·자원이 붙어 있다 (기획상 무기 기본 옵션은 공격력뿐)
- `ConsumableType { HEAL_HP, RESTORE_RESOURCE }` — PO3 'MP 물약'이 분노·MP 공용이다. 에리스도 쓸 수 있다
- `maxResource`에 장비 `MaxResource`가 자원 종류와 무관하게 더해진다. 그래서 마력 장비를 끼면 게오르크의 분노 최대치가 오른다
- `AttackAttribute { NONE, SLASH, STRIKE, PIERCE, MAGIC, RESONANCE }` — 이치·심상이 없다. 마법방어는 `MAGIC`일 때만 적용되므로 공명 공격은 물리 방어로 경감된다 (`TargetObject.DamageToPlayer`)
- `CharacterStatDB` `Weakness/Resist` — 로드만 하고 피해 계산에서 쓰지 않는다
- 피해 계산에 랜덤 요소가 전혀 없다. 플레이어 → 몬스터(`BattleActions.AttackTarget`)와 몬스터 → 플레이어(`SpawnedMonster.GeneralAttack` → `DamageToPlayer`)가 **서로 다른 경로**라 공식이 대칭이 아니다
- 몬스터 실드: `GainDefense`를 쓰는 몬스터 7종 — `Soldier_Shield`, `WacherB`, `Guardian`, `SpearManB`, `GiantSoldier`(SinglePattern 실드 10/15/20 리터럴), `Happy`, `Saddy`(엘리트). 에리스 부서지세요(ES6)·얼마나 버틸까요(ES8)는 몬스터 실드를 깎는 방식으로 구현되어 있다 (`SkillData.Eris.cs`). `MonsterStatDB` `HazardDef`는 실드 획득량 보정이다
- 스킬 효과 메서드(`SkillData.*`)가 피해·실드·회복·버프 수치를 각자 계산한다. 공통 배율을 끼울 자리가 없다 (콤보 배율 선구현 대상)
- 구 유물(`ItemDB`/`ArtifactDB`/`GamePlayerItem`/`ItemMethods`/`ArtifactMethods`) — 획득 경로가 0건이다. `BattleInitialize`가 빈 목록만 순회한다

---

## 결정 사항 (2026-09-29 확정)

D = 초안의 가정, Q = 확정 답변. 확정된 내용이 워크플로우에 반영되어 있다.

| # | 항목 | 확정 결정 | 반영 |
|---|---|---|---|
| D1/Q1 | 장비 '스킬레벨' 옵션 | **폐기**. 스킬 레벨은 오로지 크리스털로만 올린다. `EquipSkillLevelBonus`·`SkillLevel` 컬럼 제거, AC3 숙련의 부적은 다른 악세사리로 교체 | 3-9 |
| D2/Q2 | 등급 | 장비 전용 `EquipGrade {NORMAL, ADVANCED, RARE, LEGEND}` 신설. 유물은 더 이상 쓰지 않으므로 `ItemGrade`와 함께 폐기 | 0-1, 3-1 |
| D3/Q3 | 옵션 수치 | 드랍 시 범위 안에서 랜덤 굴림 → 장비는 인스턴스. **옵션 수치 범위는 CSV에 등급별로 따로 지정**한다 (등급마다 랜덤 범위가 다름). 등급별 옵션 개수도 CSV | 0-1, 0-3 |
| D4/Q4 | 몬스터 보조 스탯 | 몬스터도 회피·명중·치명을 가진다 (`MonsterStatDB` 컬럼, 없으면 5/95/0) | 0-10, 1-1 |
| D5/Q5 | 소모품 대상 | 1차는 자신에게만. **추후 몬스터에게도 사용할 수 있도록** 대상(`ValidTarget`)을 데이터로 두고, 효과가 플레이어를 가정하지 않는 구조로 만든다 | 0-9, 6-1~6-3 |
| D6/Q6 | 극약 | **추후 구현**. 소모품 효과를 "CSV 행 + 같은 이름의 효과 메서드"로 추가할 수 있게 구조화한다 | 6-2, 후순위 |
| ~~D7~~ | ~~메르크리우스 제작~~ | 폐기 (09-28) → Q19, Q20 | — |
| D8/Q8 | 포지션 패널티 제거 옵션 | 보너스는 유지하고 패널티만 제거. 다른 옵션보다 기대값이 높은 것은 의도 | 5-10 |
| D9/Q9 | 이동 시 턴 소모 제거 옵션 | 턴당 1회 무료 (철귀 이동과 별도 플래그). 기획 "중열: 위치 변환 코스트 삭제"도 같은 경로 | 5-11 |
| D10/Q10 | 콤보 추가 데미지 | 연계는 **콤보 카드 구현 후** 구현하는 후순위. 다만 전투 시스템은 **콤보 카드를 받을 수 있게 미리 구현**한다 — 모든 스킬 수치가 거치는 배율 슬롯(`SkillCast.powerMultiplier`)과 공격 컨텍스트(`HitContext`)를 Phase 1에서 만든다 | 1-3, 1-4, 5-5 |
| D11/Q11 | 랜덤보정값 | 공격 1타마다 정수 −10 ~ +10을 균등으로 굴린다. **다단 공격은 타수마다 굴림**. 같은 r을 약점 TP 공식에 쓴다. 고정 피해·회복·실드에는 적용하지 않는다. 플레이어·몬스터 공통 | 1-5 |
| D12/Q12 | 약점 TP | TP 감소 = 약점TP고정계수(15) + 약점TP비율계수(5) × r / 10. **같은 대상 반복 체감은 유지**하고, 감소율을 BalanceDB 상수로 둔다 (`WEAKNESS_TP_REPEAT_PERCENT` — 50이면 반복마다 50%로 감소. 값은 테스트 후 결정) | 1-8 |
| D13/Q13 | 치명타 TP | 치명이면 대상 TP −5 (치명타TP계수). 약점 TP와 **합산** — 약점 + 치명이 TP를 크게 깎는 것은 의도 | 1-9 |
| D14/Q14 | 치명 시 방어 경감 / 몬스터 실드 | **몬스터 실드는 삭제하고 방어력 스탯으로 대체**한다. 실드를 쓰는 몬스터는 전부 리워크 예정. 치명 방어 경감(50% 적용 후 −10)은 **플레이어·몬스터 모두**에 적용 | Phase 2, 1-7 |
| D15/Q15 | 속성 enum / 버프 | `MAGIC` → `ICHI`(이치), `SIMSANG`(심상) 추가, `RESONANCE`(공명) 유지. 버프는 전부 개편 예정이고 `ICHI_ATTACK` 같은 버프는 삭제될 예정 → **이 워크플로우의 신규 버프는 `ICHI_*`를 쓰지 않는다** | 0-7, 0-13, 1-12 |
| D16/Q16 | 방어구 종류 | **갑옷·투구·신발 모두**에 방어구 종류가 붙는다. 약점 규칙은 기획서 「방어구」 참조 — 한 부위라도 미착용이면 모든 속성 약점, 2종 혼합이면 두 종류 약점 모두, 3종 혼합이면 모든 속성 약점. 중갑·경갑·로브 순으로 힘 요구치가 있고, 부족하면 민첩 1/2 | 0-5, 0-6, Phase 4 |
| D17/Q17 | 캐릭터 고유 약점·내성 | **폐기**. 장비 변경으로 약점을 바꾸는 것이 전략 | 4-4 |
| D18/Q18 | 플레이어 약점 피격 | 플레이어와 몬스터의 치명 공식·약점 TP 피해 모두 **같은 공식** (BalanceDB 키도 공용) | 1-3, 4-6 |
| D19/Q19 | 메르크리우스 악세사리 가챠 | **후순위**로 미룬다 | 후순위 |
| D20/Q20 | 장비 처분 | 인벤토리에 누적되며 **메르크리우스에게 판매** 가능. 메르크리우스는 **낮은 등급의 기본템을 판매**한다 (상점) | 7-7~7-9 |
| D21/Q21 | 구 유물 시스템 | **폐기** (삭제) | 3-1 |
| D22/Q22 | 무기 공격력 | 모든 무기가 **물리공격력과 마법공격력을 모두** 가지며 CSV로 관리한다. 계수 불일치 툴팁 경고는 **띄우지 않는다** | 0-4, 3-6 |
| D23/Q23 | 약점 상쇄 옵션 | 옵션마다 **속성 1가지**를 지정해 그 속성의 약점만 무효화한다 (예: "약점 상쇄 : 공명") | 0-3, 5-12 |
| D24/Q24 | 드랍 | **몬스터별 아이템 드랍 테이블**을 만든다. 멀티 1.5배 적용 후 100%를 넘는 몫은 추가 1회 판정 확률로 처리 | 0-12, 7-1~7-4 |
| D25 | 최대 MP | **가정대로 진행 (09-29 확인)** — MP 옵션은 MP 캐릭터(홍단향)에게만 유효, 분노 최대치는 전설 옵션 `MAX_RAGE`로만 오른다. 최대 MP 레벨 성장(`GrowMP`)은 밸런스 테스트 후 결정 | 3-8 |

### 결정에서 파생된 가정 (A5 확정, 나머지는 확인 필요)
| # | 항목 | 가정 |
|---|---|---|
| A1 | 몬스터 방어 행동의 임시 처리 (Q14) | 정식 리워크 전까지 실드 행동 7종은 "자신/아군 방어력 +ActionValue, 다음 자기 턴까지" 스탯 버프로 **임시 전환**해 게임이 돌아가게 둔다. 예고 아이콘은 DEFENSE 그대로 |
| A2 | 플레이어 실드 (Q14) | 실드 삭제는 **몬스터만**이다. 플레이어의 방어 행동·철귀 실드·홍단향 수호 축복·보호막 옵션은 유지 |
| A3 | 몬스터 위험도 방어 보정 (Q14) | `MonsterStatDB` `HazardDef`의 의미를 "실드 획득량 +"에서 **"방어력 스탯 +"(위험도 1당)** 로 바꾼다. 마법방어용 `HazardMdef` 컬럼 추가 |
| A4 | 힘 요구치 판정 (Q16) | 요구치는 **장비 행마다** `RequireStr` 컬럼으로 둔다 (중갑 높음 / 경갑 낮음 / 로브 0). 착용한 방어구 중 하나라도 요구치를 못 넘으면 민첩 1/2 — **패널티는 1회만** (부위 수만큼 중첩하지 않음). 비교 대상은 버프를 뺀 힘(기본 + 장비). 전투 중 버프로 요구치가 오르내려 TP 속도가 흔들리지 않게 하기 위함 |
| A5 | 초기 장비 (Q16) | **확정 (09-29)** — 방어구가 한 부위라도 없으면 모든 속성이 약점이므로, 새 캐릭터에게 **같은 종류 방어구 3부위 세트**를 지급한다 (게오르크 중갑 / 에리스 경갑 / 홍단향 로브). 방어구가 빠진 구 세이브는 로드 시 1회 기본 세트를 보충한다 |
| A6 | 약점 상쇄 스택 (Q23) | 서로 다른 속성의 약점 상쇄는 각각 적용되고, 같은 속성끼리는 중복 효과가 없다. "모든 속성 약점" 상태에도 적용된다 (상쇄한 속성만 빠짐). 상쇄 속성은 드랍 시 6속성 중 랜덤 (CSV 가중치) |
| A7 | 드랍 판정 단위 (Q24) | 처치한 몬스터마다 그 몬스터의 드랍 테이블을 **GamePlayer마다** 굴린다 (현재 구조 유지 — 싱글은 3명, 멀티는 인원수만큼). 확률은 테이블 수치로 조정 |
| A8 | 메르크리우스 상점 (Q20) | 판매 목록은 CSV(`ShopDB`)로 두고 1차는 NORMAL 등급 기본 장비(옵션 굴림 없이 NORMAL 인스턴스)를 판다. 물약 판매 여부는 같은 CSV에 행을 넣어 조절한다. 판매가(플레이어 → 상점)는 `EquipDB.Price × 등급별 판매율`(EquipGradeDB). 장착 중인 장비는 팔 수 없다. 골드는 현재 선택 캐릭터가 내고 받는다 |
| A9 | `SkillCast` 도입 범위 (Q10) | 스킬 효과 위임을 `ExecuteSkill(SkillCast cast)`로 바꾸고 `SkillData.*` 전체를 한 커밋으로 기계적으로 이관한다. 배율은 1.0이므로 동작 변화는 없어야 한다. 퍼센트·지속 턴처럼 곱하기 애매한 수치는 기획 「파티 TP·연계 — 후속 검토」가 정해질 때까지 배율을 적용하지 않는 수치로 표시만 해 둔다 |

---

## Phase 0 — 데이터 구조 설계 (코드 변경 없음)

**목표**: 판정·몬스터 방어·인스턴스 장비·옵션·방어구·소모품·드랍 데이터 형식을 확정해, 이후 Phase가 CSV만 채우면 되게 한다.

- [ ] 0-1. `EquipGrade` enum + **`EquipGradeDB.csv`** (Q3): `Grade, OptionCountMin, OptionCountMax, LegendOptionCount, AccessoryOptionCountMin, AccessoryOptionCountMax, DropWeight, EliteBonusWeight, BossBonusWeight, MinHazard, SellPercent, ColorHex, NameKey`
      기획 기본값: NORMAL 0개(악세사리 1개) / ADVANCED 1~2개 / RARE 1~2개 / LEGEND 일반 2개 + 전설 1개. 수치 높낮이는 0-3의 등급별 범위가 정한다
- [ ] 0-2. `EquipOptionType` enum 초안 (`Common/ProjectD.cs`)
      - 일반: `STRENGTH, INTELLIGENCE, AGILITY, CONTROL, MAX_HP, MAX_MP, DEFENSE, SHIELD`
      - 전설 14종: `LIFE_STEAL, SKILL_DAMAGE, COMBO_DAMAGE, CRIT_DAMAGE, REFLECT_FLAT, REFLECT_PERCENT, HEAL_RECEIVED, WEAKNESS_NULLIFY, CRIT_RATE, ACCURACY, EVADE, FREE_MOVE, NO_ROW_PENALTY, MAX_RAGE`
- [ ] 0-3. **`EquipOptionDB.csv`** — 옵션 × 등급마다 한 행 (Q3): `OptionType, Grade, Slots('|' — WEAPON|ARMOR|HELMET|BOOTS|ACCESSORY), MinValue, MaxValue, IsPercent(0/1), ValueKind(NUMBER|ATTRIBUTE|FLAG), Weight, NameKey`
      - 전설 옵션은 `Grade=LEGEND` 행만 둔다. 한 옵션이 등급마다 다른 범위를 가질 수 있다 (예: 민첩 ADVANCED 1~3 / RARE 3~5 / LEGEND 5~8)
      - `WEAKNESS_NULLIFY`는 `ValueKind=ATTRIBUTE` — 굴린 값이 `AttackAttribute`(Q23). 후보 속성과 가중치는 `AttributePool` 컬럼('|' 구분) 또는 별도 행으로 둔다
      - `FREE_MOVE`/`NO_ROW_PENALTY`는 `ValueKind=FLAG` (값 없음)
      - 슬롯별 허용 옵션은 기획서 「장비 옵션」 표를 그대로 옮긴다
      | 슬롯 | 추가 옵션 | 전설 옵션 |
      |---|---|---|
      | 무기 | 민첩·제어·HP·MP | 생명력 흡수·스킬 추가 데미지·콤보 추가 데미지·치명타 데미지 |
      | 갑옷 | HP·MP·보호막 | 반사(고정)·반사(비례)·받는 회복량 증가·약점 상쇄 |
      | 투구 | 힘·지능·HP·MP | 치명타 확률·명중률 |
      | 신발 | 민첩·HP·MP·제어 | 회피율·이동 시 턴 소모 제거·포지션 패널티 제거 |
      | 악세사리 | 방어력·민첩·힘·지능·제어·HP·MP·보호막 | 분노 최대치·포지션 패널티 제거·이동 턴 소모 제거·생명력 흡수·스킬 추가 데미지·콤보 추가 데미지·반사(고정)·반사(비례)·받는 회복량·치명타 확률·회피율·치명타 데미지·약점 상쇄 |
- [ ] 0-4. `EquipDB.csv` 개편안 — 기본 옵션만 남긴다: `EquipNo, Name, Slot, WeaponType, ArmorType, RequireLevel, RequireStr, PhysicalAttack, MagicAttack, Defense, MagicDefense, Price, DropWeight, Description`
      - 삭제: `Character`(착용 제한 없음), `Grade`(인스턴스가 가짐), `Agility/MaxHP/MaxResource`(추가 옵션으로 이관), `SkillLevel`(Q1)
      - 무기: **모든 무기 행이 `PhysicalAttack`·`MagicAttack`을 둘 다 가진다** (Q22 — 값은 무기마다 CSV로 조절, 0 허용)
      - 갑옷·투구·신발: `ArmorType` + `RequireStr` + 방어력·마법방어. 악세사리: 기본 옵션 없음 (노멀도 추가 옵션 1개)
- [ ] 0-5. `WeaponType { NONE, GREATSWORD, LONGSWORD, STAFF, BOOK, ORB, CORE }` (대검·장검·지팡이·책·수정구·핵) / `ArmorType { NONE, HEAVY, LIGHT, ROBE }` (중갑·경갑·로브)
- [ ] 0-6. **`ArmorTypeDB.csv`**: `ArmorType, Weaknesses('|'), NameKey` — HEAVY = STRIKE|RESONANCE, LIGHT = PIERCE|SIMSANG, ROBE = SLASH|ICHI.
      세트 규칙(기획서 「방어구」)은 코드 규칙으로 두고 기획서 절에 "구현 형식"으로 적는다: 3부위(갑옷·투구·신발) 중 미착용 부위가 있으면 전 속성 / 착용 종류 1개 = 그 종류 약점 / 2개 = 두 종류 약점 합집합 / 3개 = 전 속성
- [ ] 0-7. `AttackAttribute` 개편안 (Q15): `{ NONE, SLASH, STRIKE, PIERCE, ICHI, SIMSANG, RESONANCE }` + `IsMagic()` = ICHI·SIMSANG·RESONANCE. 기존 CSV의 `MAGIC` 값을 `ICHI`로 바꾸는 이관 목록 작성 (SkillDB `Attribute`, MonsterStatDB `Weakness/AttackAttribute`, CharacterStatDB `BasicAttackAttribute`)
- [ ] 0-8. 장비 인스턴스 형식: `EquipInstance { string instanceId; string equipNo; EquipGrade grade; int[] optionTypes; int[] optionValues; }`
      `optionValues`는 `ValueKind`에 따라 수치 / `AttackAttribute` 정수 / 1(플래그)을 담는다. Mirror `SyncList<EquipInstance>` — 필드가 기본형·배열뿐이라 Weaver 자동 직렬화가 된다 (`List<>` 필드 금지)
- [ ] 0-9. **소모품 구조** (Q5, Q6) — `ConsumableDB.csv`: `PotionNo, Name, Effect, ValidTarget, Value, Duration, BattleOnly(0/1), UsableResource(ANY|MP|RAGE|HP), Price, DropWeight, Description`
      - `Effect` = 효과 메서드 이름. 스킬(`SkillData`)과 같은 **리플렉션 바인딩** — `ConsumableEffects.<Effect>(ConsumableDef def, TargetObject user, TargetObject target)`
      - 같은 효과의 변형(HP 물약 30/60 등)은 **CSV 행만** 추가, 새 효과는 **CSV 행 + 같은 이름의 메서드** 추가
      - `ValidTarget`은 스킬과 같은 enum을 재사용한다. 1차는 `SELF`만 허용 (enum에 없으면 추가), 추후 `MEMBER/ENEMY`를 열면 대상 선택 UI만 붙이면 되게 한다
      - 1차 효과: `HEAL_HP, RESTORE_MP, RESTORE_RAGE, BUFF_ATTACK, BUFF_CRIT_RATE, BUFF_EVADE`. 극약(`POISON_TO_ONE`)은 후순위 (Q6)
      - `ConsumableType` enum은 폐기한다 (`Effect` 문자열 바인딩으로 대체)
- [ ] 0-10. 보조 스탯·방어 스탯 컬럼
      - `CharacterStatDB.csv`: `BaseEvade, BaseAccuracy, BaseCrit` 추가 (게오르크·홍단향 5/95/5, 에리스 10/95/10), `Weakness/Resist` 삭제 (Q17)
      - `MonsterStatDB.csv`: `Evade, Accuracy, CritRate`(없으면 5/95/0, Q4), **`Defense, MagicDefense`**(Q14), `HazardMdef` 추가, `HazardDef` 의미 변경 (A3), `DropTable` 추가 (0-12). 미사용 `TPShield` 컬럼은 삭제 여부를 확인한다 (`Monster.tpShield` 로드만 되고 참조 없음)
- [ ] 0-11. BalanceDB 키 목록 확정 (플레이어·몬스터 공용 — Q18)
      - 판정: `DAMAGE_RANDOM_PERCENT`(10), `CRIT_DAMAGE_PERCENT`(150), `CRIT_DEFENSE_PERCENT`(50), `CRIT_DEFENSE_FLAT_REDUCE`(10), `CRIT_TP_DAMAGE`(5)
      - 약점: `WEAKNESS_DAMAGE_PERCENT`(120, 기존), `WEAKNESS_TP_FIXED`(15), `WEAKNESS_TP_RATIO`(5), `WEAKNESS_CRIT_BONUS`(10), **`WEAKNESS_TP_REPEAT_PERCENT`**(50 — 반복 체감률, Q12), `WEAKNESS_TP_MIN`(구 `TP_BREAK_MIN` 개명, 0이면 하한 없음)
      - 방어구: `ARMOR_STR_PENALTY_AGI_PERCENT`(50 — 힘 부족 시 민첩 비율, Q16)
      - 드랍: `ITEM_DROP_MULTIPLAYER_PERCENT`(150)
      - 폐기: `TP_BREAK_BASE`, `TP_BREAK_MIN`(개명), `EQUIP_DROP_PERCENT`·`POTION_DROP_PERCENT`·`EQUIP_DROP_MIN_HAZARD`(드랍 테이블로 대체, Q24), `SKILL_LEVEL_BONUS_PERCENT`(Q1 — 크리스털 작업과 함께 정리)
      - 등급 가중치·전설 위험도 하한·옵션 배율 키는 **만들지 않는다** — `EquipGradeDB`/`EquipOptionDB`가 가진다 (Q3)
- [ ] 0-12. **드랍 테이블** (Q24) — `DropTableDB.csv`: `TableId, Kind(EQUIP|POTION), ItemNo, Chance, MinHazard, Description`
      - `ItemNo`는 특정 `EquipNo/PotionNo` 또는 풀 지정자(`ANY_EQUIP`, `ANY_WEAPON`, `ANY_ARMOR`, `ANY_ACCESSORY`, `ANY_POTION` — `DropWeight` 가중 추출)
      - `Chance`는 %이며 100을 넘을 수 있다. 판정: 100 이하 부분은 확정, 넘는 몫은 추가 1회 판정 확률 (예: 150 → 1개 확정 + 50%로 1개 더)
      - `MonsterStatDB.DropTable`로 몬스터마다 테이블을 지정한다 (없으면 `DEFAULT_NORMAL/ELITE/BOSS` 테이블)
- [ ] 0-13. **스탯 버프 타입** (Q15) — 이 워크플로우가 새로 쓰는 버프는 `ICHI_*`가 아니라 스탯 이름 기반으로 정의한다: `STAT_ATTACK, STAT_DEFENSE, STAT_MAGIC_DEFENSE, STAT_CRIT_RATE, STAT_EVADE, STAT_ACCURACY` (양수 = 상승, 음수 = 하락).
      버프 개편 작업과 이름·구조를 맞춘다 — 개편이 먼저 끝나면 그 타입을 쓴다. `BuffDB.csv`·`buff.<enum>.name` 로컬라이즈 행 포함
- [ ] 0-14. `ShopDB.csv` (Q20, A8): `ShopId, Kind(EQUIP|POTION), ItemNo, Price(비우면 DB Price), Description` — 1차 `ShopId=MERCURIUS`

**완료 기준**: 위 CSV 헤더·enum·세트 규칙이 기획서 「장비 옵션」「아이템」「속성」「방어구」「약점 공격」 절에 "구현 형식" 줄로 적혀 있다.

---

## Phase 1 — 전투 계산 파이프라인·판정 (명중 → 회피 → 치명, 랜덤 보정, 약점 개정)

**목표**: 플레이어 → 몬스터와 몬스터 → 플레이어가 **같은 함수·같은 공식**으로 계산된다. 기본치만으로 MISS·치명타·데미지 편차가 발생하고, 약점 공격이 09-28 공식대로 TP를 깎는다. 모든 스킬 수치가 콤보 배율 슬롯을 거친다 (배율 1.0 — 동작 변화 없음).

- [ ] 1-1. 보조 스탯 로드 — `CharacterStatData.Entry`에 `baseEvade/baseAccuracy/baseCrit`, `MonsterData`에 `evade/accuracy/critRate` (0-10 컬럼, 컬럼이 없으면 기본값)
- [ ] 1-2. **공용 스탯 조회** — `TargetObject`(플레이어·몬스터 공통)에 `GetEvade / GetAccuracy / GetCritRate / GetCritDamage / GetDefense / GetMagicDefense`를 둔다. 플레이어는 `GamePlayer.Total*` + 스탯 버프, 몬스터는 `MonsterStatDB` + 위험도 보정 + 스탯 버프. 보조 스탯은 레벨 성장이 없다 (기획)
- [ ] 1-3. **`HitContext` + `BattleActions.ResolveHit(ctx)`** (Q10, Q18) — 한 번의 타격을 표현하는 컨텍스트: 공격자·대상·기본 피해·속성·스킬 여부·r·결과 플래그(miss/crit/weakness)·`powerMultiplier`(콤보 배율 슬롯, 기본 1.0)·`bonusPercent`(스킬 추가 데미지 등).
      처리 순서 (양방향 공통): 명중 → 회피 → (MISS면 종료) → r 굴림 → 약점 판정(피해 120%) → 치명 판정(약점 보정 포함) → 배율 적용 → 대상 방어 공식(치명이면 방어 50% − 10) → 피해 적용 → 약점 TP + 치명 TP → 결과 표시.
      호출부 이관:
      - 플레이어 → 몬스터: `BattleActions.AttackTarget` 내부를 `ResolveHit`로 교체 (기사도 배율은 ctx 가산)
      - 몬스터 → 플레이어: `SpawnedMonster.GeneralAttack`이 `DamageToPlayer`를 직접 부르지 않고 `ResolveHit`을 거친다. `DamageToPlayer`는 대열 보정·보호 분담·실드·방어 공식만 담당 (방어 공식 인자로 `isCritical`)
      - `StaticDamage`(고정 피해)는 판정·랜덤·방어 없음 — 기존 경로 유지
- [ ] 1-4. **`SkillCast` 도입** (Q10, A9) — 스킬 위임을 `ExecuteSkill(SkillCast cast)`로 바꾼다 (`cast.def / user / targets / powerMultiplier / comboOrder`). `SkillData.Geork/Eris/DanHyang` 전 메서드를 한 커밋으로 기계적으로 이관한다.
      피해·실드·회복·버프 수치는 모두 `cast.Scale(value)`를 거친다. 공격은 `cast`에서 만든 `HitContext`로 `ResolveHit` 호출. 퍼센트·지속 턴 등 배율을 곱하지 않을 수치는 주석으로 표시
- [ ] 1-5. 일반 데미지 랜덤 보정 (Q11) — `ResolveHit` 안에서 타격마다 −`DAMAGE_RANDOM_PERCENT` ~ +`DAMAGE_RANDOM_PERCENT` 정수를 서버에서 굴려 피해 × (100 + r)/100. 다단 공격(연속 베기·풍차 베기·찢어 줄게요 반복 등)은 `ResolveHit`을 타수만큼 호출하므로 자동으로 타마다 굴린다. 몬스터 예고 수치는 이미 숨겨져 있다 (`NextActionIndicator.ShowActionValue = false`)
- [ ] 1-6. 명중·회피 — 유효 회피 = max(0, 회피 − max(0, 명중 − 100)). 명중 실패 또는 회피 성공이면 MISS: 피해·분노·약점 TP·치명 TP·피격 모션을 모두 생략한다 (기획 "약점 공격 Miss 시 TP 피해 없음")
- [ ] 1-7. 치명 — 치명률 + (약점이면 `WEAKNESS_CRIT_BONUS`). 피해 × 치명 배수. 방어 공식은 `ApplyDefenseFormula(damage, defenseStat, defensePercent, flatReduce)`로 시그니처를 확장해 치명이면 50% 적용 후 −10 (0 하한). **몬스터 대상에도 적용** (Q14 — 몬스터 방어력은 Phase 2 전까지 0이라 효과 없음, 경로만 연결)
- [ ] 1-8. 약점 TP (Q12) — `ApplyTpBreakTo`를 `ApplyWeaknessTpDamage(target, r)`로 교체: TP 감소 = (`WEAKNESS_TP_FIXED` + `WEAKNESS_TP_RATIO` × r / 10) × (`WEAKNESS_TP_REPEAT_PERCENT`/100)^(이번 전투에서 그 대상이 약점 피격된 횟수), 하한 `WEAKNESS_TP_MIN`. `tpWeaknessHits` 집계는 유지. **플레이어 `TpUnit`도 처리** (Phase 4의 플레이어 약점 피격용)
- [ ] 1-9. 치명 TP (Q13) — 치명이면 `DamageTpTo(target, CRIT_TP_DAMAGE)`. 약점 TP와 합산, 반복 체감 없음
- [ ] 1-10. 회복 치명 — `TargetObject.HealPlayer(value, healer)`: 시전자 치명률로 굴려 치명 배수 적용 (판정 순서 "회복 : 치명"). 소모품 회복도 이 경로. 몬스터 회복도 같은 함수로 부를 수 있게 이름을 `Heal(value, healer)`로 일반화하는 것을 검토 (Q5 대비)
- [ ] 1-11. 표시 — `M_EffectManager.DisPlayeDamage` 확장: MISS 텍스트(피격 모션·셰이크 없음), 치명타 숫자 강조(색·크기). `RpcDisplayZeroDamage` 패턴으로 `RpcDisplayMiss` (RPC 해시 충돌 확인)
- [ ] 1-12. 스탯 버프 훅 (0-13, Q15) — 1-2 조회 함수가 `STAT_*` 버프를 가산한다. 이후 물약(Phase 6)·홍단향 마법 트리(치명타 증가·회피율 증가·스쿤다)·몬스터 방어 행동 임시 전환(Phase 2)이 이 훅을 쓴다. `ICHI_*`는 기존 스킬 호환용으로만 남긴다
- [ ] 1-13. 디버그 — `GamePlayer.DebugStats`에 보조 스탯 4종 표시·조정, 치명/회피 100% 치트, 랜덤 보정 0 고정 치트, 몬스터 스탯 확인

**완료 기준**
- 에리스 회피 10%로 몬스터 공격이 가끔 MISS 난다. 몬스터도 플레이어 공격을 가끔 회피한다
- 같은 공격의 피해가 ±10% 안에서 흔들리고, 다단 공격은 타마다 다르다
- 치명타 시 배율·방어 50% −10·TP −5가 양방향 로그로 확인된다
- 약점 TP 감소가 10~20에서 매번 달라지고, 같은 대상 반복 시 `WEAKNESS_TP_REPEAT_PERCENT`만큼 줄어든다
- 홍단향 회복 스킬에 치명 회복이 발생한다
- `SkillCast` 이관 전후로 전 스킬의 결과가 같다 (배율 1.0 회귀 확인)

---

## Phase 2 — 몬스터 방어력 전환 (Q14 신설)

**목표**: 몬스터가 실드 대신 방어력·마법방어 스탯을 가지고, 플레이어의 공격이 몬스터 방어 공식으로 경감된다. 실드를 쓰던 몬스터 7종은 임시 행동으로 돌아가고, 정식 리워크를 기다린다.

- [ ] 2-1. 로드 — `MonsterData`에 `defense/magicDefense/hazardMdefBonus` 추가. `hazardDefBonus`는 방어력 스탯 보정으로 의미 변경 (A3). `SpawnedMonster.ScaledDefense`를 "위험도 보정 방어력 스탯"으로 재정의하고, 1-2의 `GetDefense/GetMagicDefense`가 쓴다
- [ ] 2-2. 몬스터 피격 방어 공식 — `ResolveHit`이 몬스터 대상에도 `ApplyDefenseFormula`(속성이 `IsMagic`이면 마법방어)를 적용한다. `TargetObject.DamageToMonster`의 실드 분기를 제거한다 (몬스터의 `defense` SyncVar는 쓰지 않음). `StaticDamageToMonster`는 방어 무시 유지
- [ ] 2-3. 몬스터 실드 행동 임시 전환 (A1) — `Soldier_Shield`, `WacherB`, `Guardian`, `SpearManB`, `GiantSoldier`(SinglePattern 리터럴 10/15/20도 MonsterDB로), `Happy`, `Saddy`의 `GainDefense` 호출을 `STAT_DEFENSE` 버프(ActionValue, 다음 자기 턴까지)로 바꾼다. 예고 아이콘은 DEFENSE 유지. `TargetObject.GainDefense`의 몬스터 분기(`ScaledDefense` 보정)는 제거
- [ ] 2-4. 스킬 이관 — 에리스 부서지세요(ES6)·얼마나 버틸까요(ES8): "현재 실드 즉시 삭감 + 방어 획득량 감소 디버프(`ICHI_DEFENSE` 음수)" → **몬스터 방어력 디버프**(`STAT_DEFENSE` 음수, 수치·지속은 기존 BalanceDB `ERIS_BREAK_*`/`ERIS_ENDURE_*`). SkillDB 설명문과 `RPG_CONVERSION_SKILLS.md` 구현 줄을 고친다. 그 밖에 몬스터 실드·`ICHI_DEFENSE`를 전제한 스킬·몬스터 행동(SpearManB의 `ICHI_DEFENSE` 가산 등)을 전수 점검
- [ ] 2-5. 수치 초안 — `MonsterStatDB`에 몬스터별 `Defense/MagicDefense`를 채운다 (방패병·거인병·수호자는 높게, 감시자·치유사는 마법방어 위주). 기존 `WacherB` 등의 실드 수치를 참고해 1스테이지 전투 길이가 크게 변하지 않게 맞춘다
- [ ] 2-6. 표시 — 몬스터 이름표/디버그에 방어력·마법방어 표시 (실드 게이지는 몬스터에게 더 이상 뜨지 않음)

**완료 기준**
- 방패병을 공격하면 방어 공식으로 경감된 피해가 들어가고, 치명타는 방어를 50% − 10으로 계산한다
- 몬스터에게 실드가 생기지 않는다. 방어 행동을 한 몬스터는 다음 자기 턴까지 방어력이 오른다
- 부서지세요가 몬스터 방어력을 낮춰 이후 피해가 늘어난다
- 플레이어의 방어 행동·철귀 실드는 그대로 작동한다 (A2)

※ 몬스터 행동 정식 리워크(실드 몬스터 7종의 새 패턴)는 몬스터 기획이 나온 뒤 별도 작업이다. 이 Phase는 게임이 돌아가는 임시 상태까지다.

---

## Phase 3 — 장비 인스턴스·옵션 DB·무기 개편

**목표**: 장비가 등급과 랜덤 옵션을 가진 인스턴스로 생성·보유·저장된다. 누구나 어떤 무기든 착용할 수 있다. 아직 옵션 효과는 스탯 합산에만 반영한다 (일반 옵션).

- [ ] 3-1. **구 유물 시스템 삭제** (Q2, Q21) — 순서: `GamePlayer.prefab`에서 `GamePlayerItem` 컴포넌트를 MCP로 먼저 제거 → `GamePlayerItem.cs`·`Item/Item.cs·ItemMethods.cs·ArtifactMethods.cs`·`DB/ItemData.cs`(씬 오브젝트 확인)·`ItemDB/ArtifactDB.csv`·`M_TurnManager.teamArtifacts/AddTeamArtifact`·`BattleInitialize`의 유물 발동·`ItemEffectTime/ItemType/ItemGrade` enum·`RewardListItem`의 `Reward_Type.Item` 분기 삭제. 콘솔에서 Missing Script·RPC 해시 확인
- [ ] 3-2. `EquipGrade`, `EquipOptionType`, `WeaponType`, `ArmorType` enum 추가 (`Common/ProjectD.cs`) — 0-1, 0-2, 0-5
- [ ] 3-3. `EquipGradeDB.csv` + `DB/EquipGradeData.cs`, `EquipOptionDB.csv` + `DB/EquipOptionData.cs` (BalanceData 패턴). `GetRollable(slot, grade, isLegend)` — 슬롯·등급·전설 여부로 필터
- [ ] 3-4. `EquipDB.csv` 개편 (0-4) + `EquipData.Def` 정리. 기존 16행을 새 컬럼으로 이관한다. 무기 종류 6종마다 최소 1행, 방어구는 3종류 × 3부위 = 최소 9행 (Phase 4에서 `ArmorType`·`RequireStr` 채움). AC3 숙련의 부적은 다른 악세사리로 교체 (Q1)
- [ ] 3-5. 착용 제한 제거 — `CmdEquip`의 캐릭터 전용 검증, `EquipData.GetUsableBy`, OnGUI 착용 가능 표시, `ServerAddRandomEquip` 드랍 풀의 캐릭터 조건 제거
- [ ] 3-6. 무기 공격력 (Q22) — `TotalStrength = strength + Σ physicalAttack`, `TotalIntelligence = intelligence + Σ magicAttack` (캐릭터와 무관, 모든 무기가 두 값을 모두 가짐). `EquipAttackBonusFor`는 삭제. `BattleActions`는 `Total*`을 쓰므로 회귀만 확인
- [ ] 3-7. `EquipInstance` 구조체 + `EquipGenerator`(서버 전용 static) — `Create(equipNo, grade)`: `EquipGradeDB`의 옵션 개수를 굴리고, 해당 등급 행에서 Weight 가중 무중복 추출 후 Min~Max를 굴린다. 전설은 마지막 1개를 전설 옵션 풀에서. `ValueKind=ATTRIBUTE`는 속성을 굴린다 (A6). 악세사리는 악세사리 옵션 개수 컬럼 사용
- [ ] 3-8. `GamePlayer.Equipment.cs` 전환 — `equippedItems / inventoryEquips`를 `SyncList<EquipInstance>`로. `CmdEquip/CmdUnequip`은 `instanceId` 기준. `SumEquip`이 기본 옵션(EquipDB)과 인스턴스 옵션을 함께 합산하고 `EquipOptionSum(type)` / `EquipOptionAttributes(type)`(속성형 옵션 목록) 조회 함수를 둔다
      - `Total*`에 옵션 가산: 힘·지능·민첩·제어·방어
      - `TotalControl` 신설 — `BattleActions.GainRageByDamage`와 TpBattle MP 회복(턴 시작·전투 종료)이 `control` 대신 사용
      - `MAX_HP/MAX_MP`는 `ApplyEquipMaxDeltas`로 착탈 시 델타 반영. **`MAX_MP`는 자원이 MP인 캐릭터에게만** (D25)
- [ ] 3-9. Q1 반영 — `EquipSkillLevelBonus` 삭제, `GamePlayer.SkillTree.GetSkillLevel`에서 장비 가산 제거, `BattleActions.SkillDamage` 주석 수정
- [ ] 3-10. 초기 지급 `ServerGrantInitialGear` — 무기 + 방어구 3부위 세트(A5)를 NORMAL 인스턴스로 생성·장착. 캐릭터별 기본 무기는 무기 종류 기준으로 다시 지정 (게오르크 대검 / 홍단향 지팡이 / 에리스 핵)
- [ ] 3-11. 세이브 — `GameSaveService.ProfileData`의 `equippedItems/inventoryEquips`를 `List<EquipInstanceSave>`로 (instanceId·equipNo·grade·옵션 배열).
      **구 세이브 호환**: 문자열 항목 → NORMAL 옵션 0개 인스턴스, 삭제된 행(AC3 등) → 대체 장비 매핑, 방어구 부위가 비어 있으면 기본 세트 보충 (A5)
- [ ] 3-12. 임시 OnGUI 창(`DrawEquipmentGUI`) — 등급 색·무기/방어구 종류·옵션 목록 표시 (정식 UI는 Phase 8). 디버그 치트: 등급·옵션 지정 장비 생성

**완료 기준**
- 치트로 만든 전설 장비가 옵션 3개를 갖고, 착용하면 스탯창 수치가 오른다. 같은 옵션이라도 등급에 따라 수치 범위가 다르다
- 저장·이어하기 후에도 옵션이 그대로다
- 홍단향이 대검을 착용할 수 있다 (대검의 마법공격력만큼 지능 합산이 오름)
- 게오르크가 MP 옵션 장비를 껴도 분노 최대치는 100 그대로다
- 멀티 클라이언트가 호스트 캐릭터의 장비 옵션을 같은 값으로 본다
- 유물 관련 코드·데이터가 남아 있지 않고 콘솔에 Missing Script가 없다

---

## Phase 4 — 속성 분류·방어구 약점·힘 요구치

**목표**: 속성이 물리 3종(참격·타격·관통)과 마법 3종(이치·심상·공명)으로 나뉘고, 마법 속성 공격은 모두 마법방어로 경감된다. 입은 방어구 세트가 플레이어의 약점을 정하고, 몬스터가 약점을 치면 몬스터와 같은 공식으로 피해 120%·TP 감소·치명 보정이 들어간다. 힘이 부족한 방어구는 민첩을 반으로 깎는다.

- [ ] 4-1. `AttackAttribute` 개편 (0-7, Q15) — `MAGIC` → `ICHI`, `SIMSANG` 추가. CSV 이관: SkillDB `Attribute`(HS1 화염구·HS3 마력 폭풍 등), MonsterStatDB `Weakness/AttackAttribute`(감시자·정예·보스 등 `MAGIC` 행 전부), CharacterStatDB `BasicAttackAttribute`. `BattleActions.AttributeName` 표기와 로컬라이즈 키 `attr.<enum>` 추가
- [ ] 4-2. `BattleActions.IsMagic(AttackAttribute)` — 방어 스탯 선택(`attribute == MAGIC`)을 `IsMagic`으로 교체 (플레이어 피격 `DamageToPlayer`, 몬스터 피격 `ResolveHit` 양쪽). 공명 공격(Boss_Geras 등)이 마법방어로 경감되는지 확인
- [ ] 4-3. `ArmorTypeDB.csv` + 로더 (0-6). `EquipDB` 갑옷·투구·신발 행에 `ArmorType`·`RequireStr`을 채운다 (중갑 높음 / 경갑 낮음 / 로브 0)
- [ ] 4-4. `GamePlayer.GetWeaknesses()` (Q16) — 갑옷·투구·신발 3부위의 `ArmorType`을 모아 세트 규칙(0-6)으로 약점 집합을 만든다: 미착용 부위 있음 → 전 속성 / 1종 → 그 종류 약점 / 2종 → 합집합 / 3종 → 전 속성. 그다음 약점 상쇄 옵션의 속성을 뺀다 (Phase 5-12). 착탈 시 갱신(SyncVar 또는 계산 프로퍼티).
      `CharacterStatDB` `Weakness/Resist` 컬럼과 `CharacterStatData.weakness/resist` 삭제 (Q17)
- [ ] 4-5. 힘 요구치 패널티 (A4) — 착용한 방어구 중 `RequireStr` > (기본 힘 + 장비 힘)인 부위가 하나라도 있으면 `TotalAgility`에 `ARMOR_STR_PENALTY_AGI_PERCENT`(50%) 적용 (1회). TP 충전(`GetUnitTpGain`)이 `TotalAgility`를 쓰는지 확인하고, 아니면 교체
- [ ] 4-6. 플레이어 약점 피격 (Q18) — `ResolveHit`의 약점 판정이 대상이 플레이어면 `GetWeaknesses()`를 본다. 피해 120%·`ApplyWeaknessTpDamage`(플레이어 TP)·몬스터 치명 +`WEAKNESS_CRIT_BONUS` 모두 몬스터 피격과 같은 코드·같은 BalanceDB 키
- [ ] 4-7. 표시 — 전투 OnGUI 약점 힌트에 플레이어 현재 약점(세트 상태 포함), 스탯창에 힘 부족 패널티 표시. 몬스터 행동 예고에 공격 속성 표시(구 워크플로우 2C-3)를 함께 진행 권장 — 약점 회피를 위한 장비·대열 선택의 근거가 된다 (`NextActionIndicator` 확장, 데이터는 `MonsterStatDB.AttackAttribute`)

**완료 기준**
- 중갑 3부위 착용 캐릭터가 공명 공격을 맞으면 마법방어로 경감되고, 피해 120%·TP 감소가 들어간다. 참격 공격에는 약점이 없다
- 투구를 벗으면 모든 속성이 약점이 된다. 중갑 2 + 로브 1이면 타격·공명·참격·이치가 약점이다
- 힘이 부족한 캐릭터가 중갑을 입으면 민첩이 반으로 줄고 TP 충전이 느려진다
- 기존 `MAGIC` 속성이 `ICHI`로 이관되어 몬스터 약점 판정이 이전과 같다 (회귀 없음)

---

## Phase 5 — 옵션 효과 배선 (전설 옵션 14종 + 보호막)

**목표**: 기획서 전설 옵션 14종과 보호막 옵션이 실제 전투에 작동한다. 각 옵션은 `GamePlayer.EquipOptionSum(type)`(속성형은 `EquipOptionAttributes`)으로 조회하고, 적용 지점만 다르다.

| 옵션 | 적용 지점 | 항목 |
|---|---|---|
| 치명타 확률 / 명중률 / 회피율 | 1-2 공용 스탯 조회에 가산 | [ ] 5-1 |
| 치명타 데미지 | 1-2 `GetCritDamage`에 가산 | [ ] 5-2 |
| 생명력 흡수 | `ResolveHit` — HP에 실제로 들어간 피해 × % → 공격자 `HealPlayer`. `DamageToMonster`가 실제 피해(int)를 반환하도록 변경 | [ ] 5-3 |
| 스킬 추가 데미지 | `HitContext.bonusPercent` (스킬 타격만, 기본 공격 제외) | [ ] 5-4 |
| 콤보 추가 데미지 | `HitContext`에 콤보 보너스로 합산해 두고, **콤보 배율(`powerMultiplier`)이 1.0을 넘을 때만** 적용 — 연계 구현 전에는 효과 없음 (Q10). 스탯창 표시 | [ ] 5-5 |
| 반사 데미지 (고정 / 비례) | `TargetObject.DamageToPlayer` — HP 피해 후 `CounterAttack`과 같은 경로로 `tpActingUnit` 몬스터에 고정치 + 받은 피해 × %. 반격(GS13)과 합산 | [ ] 5-6 |
| 받는 회복량 증가 | `HealPlayer` — 대상 옵션 합으로 배율 (치명 회복 배율과 곱) | [ ] 5-7 |
| 분노 최대치 증가 | `ApplyEquipMaxDeltas` — 자원이 RAGE인 캐릭터만 | [ ] 5-8 |
| 보호막 (갑옷·악세사리 추가 옵션) | 전투 시작(`BattleInitialize`) 시 플레이어 `GainDefense(값)` (A2 — 플레이어 실드는 유지) | [ ] 5-9 |
| 포지션 패널티 제거 | `BattleActions.RowAttackPercent`(후열 80 → 100) / `RowIncomingDamagePercent`(전열 120 → 100) — 보너스 유지 (Q8) | [ ] 5-10 |
| 이동 시 턴 소모 제거 | `M_TurnManager.ExecutePlayerTpTurn` `TpAction.MOVE` — 옵션 보유 시 `freeAction = true`, 턴당 1회 (`tpUsedFreeMove` 신설). 중열 무료 이동(기획)도 같은 분기 (Q9) | [ ] 5-11 |
| 약점 상쇄 (속성 지정) | `GetWeaknesses()`에서 옵션이 지정한 속성을 뺀다 (Q23, A6) | [ ] 5-12 |

- [ ] 5-13. 옵션 스택 규칙 — 수치형은 합산, 플래그형(`FREE_MOVE/NO_ROW_PENALTY`)은 OR, 속성형(`WEAKNESS_NULLIFY`)은 속성 합집합. 퍼센트 옵션 상한은 BalanceDB(`EVADE_CAP` 등)로 두되 1차는 무제한
- [ ] 5-14. 검증용 치트 — 특정 옵션 하나만 붙은 전설 장비 생성 (옵션 타입·속성 지정)

**완료 기준**: 옵션 14종 + 보호막 각각을 치트 장비로 붙여 전투 로그에서 효과를 확인한다 (콤보 추가 데미지는 스탯 표시만). 옵션 없는 장비의 전투 결과는 Phase 4와 같다 (회귀 없음).

---

## Phase 6 — 소모품 구조화·물약

**목표**: 소모품이 "CSV 행 + 효과 메서드"로 추가되는 구조가 되고, 1차 물약 6종(HP·MP·분노·공격력·치명타·회피)이 전투와 비전투(거점·미로)에서 작동한다. 대상은 1차에 자신뿐이지만, 효과 코드는 몬스터 대상에도 그대로 쓸 수 있다.

- [ ] 6-1. `ConsumableData` 개편 (0-9) — `Def`에 `effect, validTarget, duration, battleOnly, usableResource, dropWeight`. `ConsumableType` enum 삭제. 로드 시 `Effect` 이름으로 `ConsumableEffects`의 정적 메서드를 리플렉션 바인딩하고, 없으면 에러 로그 (SkillData와 같은 방식)
- [ ] 6-2. **`ConsumableEffects`** (Q5, Q6) — `IEnumerator <Effect>(ConsumableDef def, TargetObject user, TargetObject target)`. 효과는 `target`이 플레이어인지 몬스터인지 가정하지 않고 1-2·1-10의 공용 함수(`Heal`, 스탯 버프, `DamageTpTo` 등)만 쓴다. 1차 효과:
      - `HEAL_HP`: `Heal` 경유 → 치명 회복·받는 회복량 옵션 적용
      - `RESTORE_MP` / `RESTORE_RAGE`: 자원 종류가 맞는 대상만 (`BattleActions.GainRage` 등)
      - `BUFF_ATTACK / BUFF_CRIT_RATE / BUFF_EVADE`: `STAT_ATTACK / STAT_CRIT_RATE / STAT_EVADE` 지속 버프 (0-13 — `ICHI_ATTACK` 미사용, Q15). 전투 전용
- [ ] 6-3. 사용 로직 통합 — `GamePlayer.ServerUseConsumable(potionNo, TargetObject target)` 한 곳으로 모은다. 검증: 보유·`BattleOnly`·`ValidTarget`(1차 SELF만)·`UsableResource`. 전투 ITEM 분기(TpBattle)와 `CmdUsePotionOnMap`의 중복 switch를 이 함수 호출로 교체. 비전투 HP 회복도 같은 규칙
- [ ] 6-4. `ConsumableDB.csv` 1차 행 — HP 물약·상급 HP 물약·MP 물약·분노의 물약·공격력 물약·치명타 물약·회피율 물약. PO3(구 `RESTORE_RESOURCE`)는 `RESTORE_MP`로 이관
- [ ] 6-5. 비전투 사용 제한 — `BattleOnly=1`은 거부 (토스트 `ui.msg.potion_battle_only`)
- [ ] 6-6. 초기 지급 — HP 물약 2개 유지, 게오르크에게 분노의 물약 1개 시험 지급 (테스트 후 결정)
- [ ] 6-7. 전투 UI(OnGUI 아이템 목록) — 사용할 수 없는 물약은 비활성 + 사유 표시 (자원 불일치·비전투 전용 등)

**완료 기준**
- 치명타 물약을 쓰면 n턴 동안 치명률 상승이 스탯창에 보이고, 만료되면 원복된다
- 비전투에서는 버프 물약이 사용되지 않는다. 에리스는 MP 물약을 쓸 수 없다
- 새 효과를 추가하는 절차가 "CSV 행 + `ConsumableEffects` 메서드 1개"뿐임을 확인한다 (테스트용 더미 효과로 검증 후 삭제)

---

## Phase 7 — 몬스터별 드랍 테이블·메르크리우스 상점

**목표**: 몬스터마다 다른 드랍 테이블로 장비·물약이 떨어지고, 등급은 `EquipGradeDB`로 굴린다. 멀티에서는 드랍 확률이 1.5배다. 메르크리우스에서 기본 장비를 사고, 인벤토리 장비를 판다.

- [ ] 7-1. 처치 기록 — `M_TurnManager`에 이번 전투에서 처치한 몬스터 목록(`battleKilledMonsters`)을 `battleExpPool` 적립 지점에 함께 기록. 전투 시작 시 초기화 (`M_TurnManager.Spawner`)
- [ ] 7-2. `DropTableDB.csv` + `DB/DropTableData.cs` (0-12), `MonsterStatDB.DropTable` 로드
- [ ] 7-3. 드랍 판정 — `RewardService.DistributeBattleRewards`가 처치 몬스터마다, GamePlayer마다(A7) 테이블 항목을 굴린다. 확률 = `Chance` × (멀티면 `ITEM_DROP_MULTIPLAYER_PERCENT`/100). 100 이하 확정분 + 넘는 몫은 추가 1회 판정 (Q24). `MinHazard` 미만 항목은 건너뜀. 기존 `EQUIP_DROP_PERCENT/POTION_DROP_PERCENT/EQUIP_DROP_MIN_HAZARD` 경로 삭제
- [ ] 7-4. 등급 굴림 `EquipGenerator.RollGrade(monsterGrade, hazard)` — `EquipGradeDB`의 `DropWeight` + 엘리트/보스 보너스 가중치, `MinHazard` 미만 등급 제외 (위험도 하한 보상). 풀 지정자는 `DropWeight` 가중 추출 후 요구 레벨 필터
- [ ] 7-5. 보상 목록 표시 — `Reward_Type`에 `Equip/Potion` 추가(이미 지급된 항목은 수령 버튼 없는 표시 전용), `BattleResultPopUp`/`RewardListItem`에 이름·등급 색 표시 (현재는 `Debug.Log`만)
- [ ] 7-6. 드랍 테이블 초안 — 일반 몬스터(물약 위주 + 낮은 확률 장비), 엘리트(장비 확정 1 + 등급 보너스), 보스(장비 150% 등). 방패병은 방어구, 감시자는 지팡이류처럼 몬스터 특색을 준다
- [ ] 7-7. `ShopDB.csv` + 로더 (0-14, A8)
- [ ] 7-8. `M_HubManager.CmdShopBuy(gamePlayerNetId, shopId, itemNo)` / `CmdShopSell(gamePlayerNetId, instanceId)` — `CmdReduceHazard` 패턴의 서버 검증(거점 상태·골드·소유·장착 여부). 구매는 NORMAL 인스턴스 생성(옵션 굴림 — 악세사리 노멀은 옵션 1개), 판매가 = `Price × SellPercent`. 거래 후 자동 저장
- [ ] 7-9. 메르크리우스 UI — `NPC_Mercurius`에서 숨긴 구 카드 버튼 중 하나를 "상점"으로 살린다 (나머지는 후순위 콤보카드 가챠·악세사리 가챠 자리로 남겨 둠). 클래스 주석 '기획: 장비 제작'을 기획대로 고친다. 상점 팝업은 런타임 구성(`SaveSlotPanel` 패턴)으로 구매 탭 / 판매 탭 (현재 선택 캐릭터 기준 골드·인벤토리).
      ※ `NPC_ShadowMan.buttonItemShop` → `ItemShopPopUp`(빈 팝업)은 그림자꾼 스킬 초기화·크리스털 가챠 UI로 바뀔 때까지 그대로 둔다

**완료 기준**
- 방패병과 감시자의 드랍 경향이 다르다. 테이블 확률 150% 항목은 싱글에서 1개 확정 + 50%로 1개 더 나온다
- 멀티에서 같은 몬스터의 드랍 확률이 1.5배다
- 위험도가 낮으면 전설이 나오지 않는다. 엘리트 처치 후 고급 이상 비율이 오른다
- 메르크리우스에서 기본 장비를 사면 골드가 줄고 NORMAL 장비가 들어온다. 인벤토리 장비를 팔면 등급에 따라 골드를 받는다. 장착 중인 장비는 팔 수 없다

---

## Phase 8 — UI·로컬라이즈

**목표**: OnGUI 임시 창을 정식 인벤토리·장비·상점 팝업으로 교체하고, 옵션·소모품·속성·방어구 문자열을 로컬라이즈한다.

- [ ] 8-1. 로컬라이즈 키 — 전 로케일 CSV에 등록 (`Document/LOCALIZATION.md`)
      - `equip.<EquipNo>.name/.desc`, `equip.option.<EquipOptionType>.name` / `.format`("{0}%", 약점 상쇄는 "약점 상쇄 : {attr}"), `equip.grade.<EquipGrade>`
      - `equip.weapontype.<WeaponType>`, `equip.armortype.<ArmorType>`, `attr.<AttackAttribute>`
      - `potion.<PotionNo>.name/.desc`, `buff.STAT_*.name`, `ui.msg.miss`, `ui.msg.potion_battle_only`, `ui.shop.*`
      - 현재 장비·물약 이름은 CSV 한국어를 그대로 표시한다
- [ ] 8-2. 장비 툴팁 컴포넌트 — 이름·등급·슬롯·무기 종류·방어구 종류와 힘 요구치·기본 옵션(물리/마법 공격력)·추가 옵션(전설 옵션 별색)·요구 레벨. 표시:
      - 방어구 약점과 세트 상태 (예: "중갑 — 타격·공명 약점", "세트 혼합 — 약점 합산")
      - 힘 요구치 미달 경고 ("힘 부족 — 민첩 ½")
      - 자원 불일치로 무효인 옵션 (MP 옵션을 비MP 캐릭터가 착용 등)
      - 무기 공격 계수 불일치 경고는 **띄우지 않는다** (Q22)
- [ ] 8-3. 인벤토리 팝업(`UI/PopUpComponent/InventoryPopUp`) — 슬롯 6칸 + 보유 장비 그리드 + 소모품 탭 + 현재 약점 요약. `PopUpUIManager` 등록, 스킬트리 팝업과 상호 배타. 비교 표시(장착 중 vs 선택)
- [ ] 8-4. 메르크리우스 상점 팝업 정식화 (7-9의 런타임 팝업 교체)
- [ ] 8-5. 전투 아이템 액션 — OnGUI 목록을 소모품 버튼 바로 교체 (아이콘·수량·비활성 사유). 대상 선택(ValidTarget)은 후순위지만 버튼 구조는 대상 선택 단계를 끼울 수 있게 둔다 (Q5)
- [ ] 8-6. 스탯창(`DebugStats`) 정리 — 보조 스탯 4종·제어·현재 약점·힘 부족 패널티를 정식 표시로, 장비 기여분은 괄호로
- [ ] 8-7. `DrawEquipmentGUI` OnGUI 제거

**완료 기준**: OnGUI 없이 장비 착탈·물약 사용·상점 거래·툴팁 확인이 가능하다. 로케일을 바꾸면 옵션·속성·방어구 종류 이름이 바뀐다.

---

## Phase 9 — 세이브·멀티·밸런스 검증

- [ ] 9-1. 구 세이브 3슬롯 로드 회귀 — 문자열 장비 → NORMAL 인스턴스, AC3 등 삭제 장비 대체 매핑, 방어구 빈 부위 기본 세트 보충(A5), PO3 → `RESTORE_MP`
- [ ] 9-2. ParrelSync 2클라이언트 — 클라이언트 캐릭터의 드랍·착탈·상점 거래·소모품 사용이 호스트 검증을 거쳐 반영되는지. `SyncList<EquipInstance>` 초기 동기화(늦게 들어온 클라이언트), MISS·치명 표시 RPC, 몬스터 방어 버프 표시
- [ ] 9-3. RPC 해시 충돌 점검 — 새 `Cmd*/Rpc*`(`RpcDisplayMiss`, `CmdShopBuy/CmdShopSell`, `CmdEquip` 시그니처 변경 등) 추가 후 콘솔 "have the same hash" 확인
- [ ] 9-4. 밸런스 초안 — 1스테이지 플레이로 조정하고 결과를 `RPG_CONVERSION_DESIGN.md`에 기록
      - 판정: 랜덤 보정 폭, 약점 TP 계수·**반복 체감률**(Q12), 치명 TP
      - 몬스터: 방어력·마법방어·위험도 보정 (Phase 2 이후 전투 길이)
      - 장비: 등급별 옵션 범위·개수, 등급 가중치, 전설 위험도 하한, 힘 요구치, 드랍 테이블 확률, 상점 가격·판매율
- [ ] 9-5. 기획서 갱신
      - 「보조 스탯」「치명타」「약점 공격」「속성」「방어구」「장비 옵션」「아이템」「거점」 절에 구현 줄 추가, 결정 사항(Q1~Q24, A1~A9) 반영
      - 「약점 공격 (임시)」의 '임시' 표기 정리 여부를 기획과 확인
      - `RPG_CONVERSION_ITEMS.md`(현재 '### 아이템' 한 줄)에 옵션 표·소모품 표·드랍 테이블 요약·방어구 약점 표를 옮긴다
      - `RPG_CONVERSION_WORKFLOW.md` Phase 4를 "1차 완료 + 개편은 ToDo_RPG_ITEM 참조"로 바꾼다

---

## 후순위 (구조만 준비, 구현은 나중)

| 항목 | 근거 | 준비된 구조 |
|---|---|---|
| 메르크리우스 **악세사리 가챠** | Q19 | `EquipGenerator.Create/RollGrade`, 상점 커맨드 패턴(7-8), 메르크리우스 버튼 자리(7-9). 가챠 전용 가중치는 `EquipGradeDB`에 컬럼 추가로 |
| **극약** (HP 1만 남김) 및 신규 소모품 효과 | Q6 | `ConsumableEffects`에 메서드 1개 + CSV 행. 게오르크 분노(맞을 때 공식)·에리스 광기(`UpdateErisMode`)는 기존 경로로 동작 |
| 소모품 **아군·몬스터 대상** 사용 | Q5 | `ValidTarget` 컬럼, `target` 인자, 효과가 대상 종류를 가정하지 않음(6-2). 대상 선택 UI만 추가 |
| **콤보 추가 데미지** 실제 적용 | Q10 | `SkillCast.powerMultiplier`·`HitContext` 콤보 보너스(1-3, 1-4, 5-5). 연계·콤보 카드 구현 시 배율만 넣으면 됨 |
| 몬스터 **정식 리워크** (실드 몬스터 7종) | Q14 | Phase 2 임시 방어 버프 상태에서 몬스터 기획 후 교체 |

## 폐기·정리 목록

- 구 유물 시스템 전체 — `GamePlayerItem`, `Item/*`, `DB/ItemData.cs`, `ItemDB/ArtifactDB.csv`, `teamArtifacts`, `ItemEffectTime/ItemType/ItemGrade` (Phase 3-1)
- `EquipDB.csv` `Character/Grade/Agility/MaxHP/MaxResource/SkillLevel` 컬럼, `Attack` 단일 컬럼, `EquipData.Def.character/grade/skillLevel`, `EquipData.GetUsableBy`, `GamePlayer.EquipSkillLevelBonus`, `EquipAttackBonusFor` (Phase 3)
- `CmdEquip` 캐릭터 전용 검증 (Phase 3)
- `SyncList<string>` 장비 인벤토리와 문자열 세이브 형식 (Phase 3 — 변환기만 남김)
- 몬스터 실드: `DamageToMonster` 실드 분기, `GainDefense` 몬스터 분기, 몬스터 7종의 실드 행동, 에리스 ES6/ES8의 실드 삭감 (Phase 2)
- `AttackAttribute.MAGIC` (→ `ICHI`), `CharacterStatDB` `Weakness/Resist` 컬럼과 `CharacterStatData.weakness/resist` (Phase 4)
- `ApplyTpBreakTo`(→ `ApplyWeaknessTpDamage`), BalanceDB `TP_BREAK_BASE`(`TP_BREAK_MIN`은 `WEAKNESS_TP_MIN`으로 개명) (Phase 1)
- BalanceDB `EQUIP_DROP_PERCENT`·`POTION_DROP_PERCENT`·`EQUIP_DROP_MIN_HAZARD` (Phase 7 — 드랍 테이블로 대체)
- `ConsumableType` enum (Phase 6 — `Effect` 바인딩으로 대체)
- `GamePlayer.Equipment.cs`의 OnGUI 창, TpBattle 전투 아이템 OnGUI 목록 (Phase 8)
- ToDo 구 D7·구 5-5/5-6 (메르크리우스 장비 구매·재감정 탭) — 09-28 개정으로 폐기, 상점(구매·판매)은 Q20으로 재정의

## 이 문서의 범위 밖 (다른 워크플로우에서 처리)

- **버프 전면 개편** (Q15) — `ICHI_*` 등 기존 버프 타입 삭제와 스킬 이관. 이 문서는 새 코드가 `ICHI_*`에 의존하지 않게 `STAT_*` 버프(0-13)만 추가한다. 두 작업이 동시에 진행되면 버프 타입 이름을 먼저 합의한다
- **전투 시작 TP 랜덤** (기획 「전투 시작 TP 랜덤」) — `M_TurnManager.TpBattle` 선충전 교체. `RPG_CONVERSION_WORKFLOW.md`에서 다룬다
- **파티 TP·연계·콤보 카드**, 메르크리우스 **콤보카드 가챠** — 이 문서의 1-3·1-4가 배율 슬롯을 미리 만든다 (Q10)
- **스킬 강화 크리스털**, 그림자꾼 **스킬 초기화·크리스털 가챠** — Q1(장비 스킬레벨 폐기)이 크리스털 원칙의 전제. SkillTreeDB `SKILL_LEVEL` 노드와 `SKILL_LEVEL_BONUS_PERCENT` 정리는 크리스털 작업에서
- 홍단향 마법 트리(치명타 증가·회피율 증가·스쿤다) — 1-12 스탯 버프 훅을 공유
- 몬스터 행동 정식 리워크 (Q14) — 몬스터 기획 후

## 의존 관계

```
Phase 0 ─┬─ Phase 1 (파이프라인·판정) ─┬─ Phase 2 (몬스터 방어력) ─────────────────┐
         │                             ├─ Phase 4 (속성·방어구 약점·힘 요구치) ─┬─ Phase 5 (옵션 효과) ─ Phase 7 (드랍·상점) ─┬─ Phase 8 (UI) ─ Phase 9
         ├─ Phase 3 (인스턴스·무기·유물 삭제) ─┘                                  │                                            │
         └─ Phase 6 (소모품, 1-2·1-10·1-12 필요) ───────────────────────────────┴────────────────────────────────────────────┘
```
- Phase 1과 3은 서로 독립이라 병행할 수 있다. 단 1-4 `SkillCast` 이관은 스킬 파일 전체를 건드리므로 다른 스킬 작업과 충돌하지 않는 시점에 한 번에 한다
- Phase 2는 Phase 1(공용 스탯 조회·`ResolveHit`·스탯 버프 훅)만 있으면 된다. 장비와 무관하다
- Phase 4는 Phase 1(약점 TP·양방향 `ResolveHit`)과 Phase 3(`EquipDB` `ArmorType`·`RequireStr`)이 모두 필요하다
- Phase 6은 Phase 1의 공용 회복·스탯 버프(1-2, 1-10, 1-12)만 있으면 착수할 수 있다
- Phase 7은 Phase 3(인스턴스·등급 DB)이 필수, 상점(7-7~7-9)은 Phase 5와 무관하게 먼저 해도 된다
- 약점 상쇄(5-12)는 Phase 4, 콤보 추가 데미지(5-5)의 실제 효과는 연계 구현에 의존한다
