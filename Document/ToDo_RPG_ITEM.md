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
- **2026-10-19 검토 답변(`ToDo_RPG_ITEM_REVIEW.md` D1~D16) 반영 — Q25~Q39 추가**
    - 힘과 무기 공격력 분리 (Q25) — `TotalPhysicalAttack/TotalMagicAttack` 신설, 기본 공격 속성은 무기 행 `Attribute`
    - 명중·회피 **단일 판정** (Q26), MISS 시 부가 효과 생략·광역 대상별 판정·반격/반사도 판정 경유 (Q27)
    - 회복·실드 부여 치명 (Q29), 플레이어 약점 TP 반복 체감 (Q30), 분노 = 최종 피해 기준 (Q32)
    - **실드는 소진까지 지속** — 자기 턴 시작 리셋 폐지 (Q37, 방어 액션은 F4 확인 대기). 지속 버프는 자기 턴 종료 차감 3턴 (기존 `ApplyTimedDebuffTo`)
    - **골드를 플레이어(접속자) 단위 공유로 전환** (Q35 — 신설 7-10)
    - 드랍은 싱글 파티 1회 / 멀티 플레이어마다 굴림 (Q39, A7 교체)
    - `ResolveHit`을 공격 측 계산으로 한정하고 방어 측은 `DamageToPlayer/Monster(isCritical)`가 담당 (R5), `IsMagic` 헬퍼를 Phase 1로 (R6), `saveVersion` (R11)
- **2026-10-19 (2차) 검토 문서 F1~F14 답변 반영 — Q40~Q44 추가, A2·A3·A4·A6·A8·A9 확정**
    - 기본 공격 계수는 무기 속성으로 선택 (F1), 최대치는 `TotalMaxHP/TotalMaxResource` 계산 프로퍼티 (F2), 드랍 요구 레벨 필터 폐지 (F3), 멀티 드랍은 캐릭터마다 (F6)
    - **방어 행동은 실드가 아니라 물리 방어력 1턴 버프** (Q40). **실드는 덮어쓰기**(합산 아님)이며 소진까지 지속, 방어 행동·철귀의 실드 생성 폐지 (Q41)
    - 힘 요구치 판정에 힘 버프 포함 — 버프로 요구치를 넘으면 패널티 해제 (Q42)
    - 기획서 「자원」「기본 공격 속성」「보조 스탯」「치명타」「전투」 절을 이 결정에 맞춰 수정 (F7)
- **2026-10-19 (3차) G1~G4 답변 반영** — 방어 행동 = 물리 방어력 +(10 + 레벨), 다음 자기 턴이 올 때까지 (턴 시작 만료 — 1-15에 `expireOnOwnTurnStart` 추가). 철귀 턴 종료 효과 = 방어력 +10 같은 방식. 실드 덮어쓰기는 항상 교체. 보상 골드는 드랍과 같은 횟수. H1 답변 — **다른 출처의 `STAT_*` 버프는 합산** (Q45, 1-12 출처별 중첩). **미결 없음**
- **2026-10-19 (4차) Phase 0 검토·I1 답변 반영 — Q46 추가** — 에리스 평타 = **힘 + 핵 마법공격력, 결과는 공명(마법)**. 일반화: 무기 행에 `ScaleStat`(STR/INT) 컬럼, 공격력 = 계수 스탯 + 속성에 맞는 쪽의 무기 공격력 (`GamePlayer.GetAttackPower`). **스킬도 같은 함수를 쓴다 (J1 확정 — 적용)**. Phase 0 문서 검토에서 구 세이브 변환 매핑 누락(구 AR2 → AR3 · 구 HE2 → HE3 · PO3 자원별 분기 — R13)과 초기 중갑 세트 `RequireStr`이 원래 기본 힘 8을 넘던 문제(R15) 수정

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
| Q25 (10-19) | 힘·무기 공격력 분리 / 무기 속성 | **힘과 무기 공격력은 다른 개념.** 최종 물리공격력 = 힘 + 무기 `PhysicalAttack`, 최종 마법공격력 = 지능 + 무기 `MagicAttack` (`TotalPhysicalAttack/TotalMagicAttack`). `TotalStrength/TotalIntelligence`는 스탯(기본 + 옵션)만. **무기 행마다 `Attribute` 컬럼** — 기본 공격 속성은 캐릭터가 아니라 무기 속성을 따른다 (`CharacterStatDB.BasicAttackAttribute` 폐기). 기본 공격의 계수는 F1(무기 속성으로 선택)에서 출발해 **Q46으로 확정** — 계수 스탯은 무기 행 `ScaleStat`, 무기 공격력은 속성에 맞는 쪽 | 0-4, 3-6, 4-5 |
| Q26 (10-19) | 명중·회피 공식 | **단일 판정.** MISS 확률 = max(0, 대상 회피 + (100 − 공격자 명중)). 명중 105% vs 회피 5% = 0% MISS, 기본(95/5) = 10% MISS. 명중 실패를 따로 굴리지 않는다 | 1-6 |
| Q27 (10-19) | 판정을 거치는 범위 | MISS면 그 타격의 **부가 효과(디버프·TP 감소 등)도 생략**. 광역 공격은 **대상마다 개별 판정**. 반격(GS13)·반사 옵션은 **2차 판정(명중·치명·r)을 거친다**. 개화꽃 같은 고정 피해는 판정·방어 없음 | 1-3, 1-6, 5-6 |
| Q28 (10-19) | 몬스터 치명 | 몬스터 기본 치명 0 + 약점 보정 10%는 **의도**. 몬스터에게도 약점을 보완하는 세팅이 추후 요구될 예정. 치명 배수는 플레이어와 공용 | 1-7, 4-6 |
| Q29 (10-19) | 실드·회복·실드 부여 치명 | 실드는 **방어 공식 무시**로 먼저 차감하고 남은 피해에만 방어 공식 (현재 플레이어 규칙 유지 — 실드 100에 200 피해 = 실드 100 소모 + 100은 방어 적용). **실드 부여(수호 축복·만개 등)와 회복 모두 시전자 치명률로 치명 판정**. 배수는 `CRIT_DAMAGE_PERCENT` 공용 (F8 확정) | 1-10 |
| Q30 (10-19) | 플레이어 약점 TP 반복 체감 | **적용.** 몬스터가 같은 플레이어를 연속 약점 공격해도 `WEAKNESS_TP_REPEAT_PERCENT`로 체감 — 연속 약점 피해로 불합리한 상황이 나와선 안 된다 | 1-8, 4-6 |
| Q31 (10-19) | 몬스터 → 플레이어 방어 저하 | 방어력과 실드는 다른 개념. WacherA 등의 방어 저하는 **`STAT_DEFENSE` 음수 디버프 3턴** (실드 획득량 감소 방식 폐기) | 2-4, 1-12 |
| Q32 (10-19) | 분노 생성 기준 | **모든 계산이 끝난 최종 피해(HP에 실제로 들어간 피해) 기준.** 실드가 흡수한 분량으로는 분노가 생기지 않는다. 때릴 때·맞을 때 동일. ※ 기획서 「자원」 절의 "맞을 때 = 방어력 제외 데미지"를 고쳐야 한다 **[F7]** | 1-3, 5-3 |
| Q33 (10-19) | 버프 물약 지속 턴 | 아이템 사용도 턴을 소비하므로 **사용 턴을 제외하고 n턴** (실질 n+1턴) — 현재 `ApplyTimedDebuffTo`의 `skipFirstTick` 규칙 그대로 | 6-2 |
| Q34 (10-19) | 드랍 제어 요소 | 드랍은 **몬스터별 테이블 + 위험도(`MinHazard`) + 멀티 배율**로만 제어한다. 요구 레벨 필터(현재 `level + 2`)는 **폐지 (F3 확정)** — 고레벨 장비가 떨어져도 착용은 요구 레벨로 막힌다 | 7-3, 7-4 |
| Q35 (10-19) | 골드 단위 | **골드는 플레이어(접속자, `PlayerInterface`) 단위 공유.** 싱글 = 3캐릭터 공유, 멀티 = 플레이어별 골드(한 플레이어가 2캐릭터를 쓰면 2캐릭터 공유). 싱글 → 멀티로 캐릭터를 들고 올 때 골드는 **전액** (F5 확정). 보상 골드 지급 횟수는 **드랍과 같은 규칙 — 싱글 1회, 멀티는 캐릭터마다** (G4 확정). 경험치는 지금처럼 캐릭터마다 전액 | 7-8, 7-10(신설), 9-1 |
| Q36 (10-19) | 초기 세트 | 초기 방어구 세트는 **힘 요구치 패널티를 받지 않도록** `RequireStr`을 기본 힘(원래 밸런스 — 게오르크 10 기준, 테스트용 5배 상향값 아님) 이하로 둔다. 초기 세트 성능은 최하 | 3-10, 4-3 |
| Q37 (10-19) | 버프·실드 지속 규칙 | 버프·디버프는 **3턴, 자기 턴 종료 시 1씩 차감** (기존 `ApplyTimedDebuffTo`). **실드는 전부 소진될 때까지 지속** — 자기 턴 시작 리셋(`next.target.defense = 0`) 폐지. 실드 스킬은 대부분 삭제 예정. 방어 액션은 Q40, 실드 덮어쓰기는 Q41 | 1-14(신설), 2-3, 5-9 |
| Q38 (10-19) | 소모품 비전투 경로 / 받는 회복량 | 받는 회복량 증가는 장비 옵션(상시)이므로 **비전투 회복(물약·캠프·류진솔)에도 적용**. 소모품 효과 인자는 `GamePlayer` 기준으로 두고 전투 중에만 TargetObject를 보강한다 (R1) | 0-9, 5-7, 6-2, 6-3 |
| Q39 (10-19) | 드랍 판정 단위 | **싱글 = 파티 1회 굴림, 멀티 = 플레이어마다 굴림 × `ITEM_DROP_MULTIPLAYER_PERCENT`.** A7 교체. 멀티는 **캐릭터(`GamePlayer`)마다** — 한 플레이어가 캐릭터 2개를 쓰면 2번 굴린다 (F6 확정). 싱글만 파티 1회 | 7-3 |
| Q40 (10-19 2차) | 방어 행동 | **방어 행동은 실드를 만들지 않는다.** 물리 방어력을 올리는 `STAT_DEFENSE` 버프 (마법방어는 오르지 않음). **지속 = 다음 자기 턴이 올 때까지**(자기 턴 시작에 만료 — 지금 실드가 리셋되던 자리). **증가량 = 10 + 캐릭터 레벨 (임시 — 밸런스 검토 후 변경 예정, G1)**, BalanceDB `DEFEND_DEFENSE_UP_BASE`(10)·`DEFEND_DEFENSE_UP_PER_LEVEL`(1). `DEFEND_BASE_VALUE` 폐기 | 1-15(신설), 0-11 |
| Q41 (10-19 2차) | 실드 덮어쓰기·실드 생성원 정리 | 실드를 만드는 것은 **스킬·장비 옵션뿐**이며 전부 소진까지 지속. **새 실드를 부여하면 기존 실드를 지우고 교체한다** (합산 아님 — 실드 100에 10을 부여하면 10). 방어 행동(Q40)과 **철귀 턴 종료 실드(HS0_DEFENSE)는 실드를 만들지 않는다** — 철귀의 턴 종료 효과는 **방어력 +`HS0_DEFENSE`(10) 임시 버프 — 방어 행동과 같은 방식·지속**(그 아군의 다음 턴이 올 때까지, G2 확정). 공격력 +3은 그대로. 덮어쓰기는 **항상 교체**(더 작은 값으로도, G3 확정). 허물 강화(ENHANCESKIN, 회복→실드 가산)는 쓰는 곳이 없어 버프 개편 삭제 대상 | 1-10, 1-14, 5-9 |
| Q42 (10-19 2차) | 힘 요구치 비교값 | 요구치는 방어구 행마다 `RequireStr`, 못 넘는 부위가 하나라도 있으면 민첩 ½ 1회 (A4 ①③). 비교하는 힘은 **힘 버프를 포함**한 현재 힘(`TotalStrength` + `STAT_STRENGTH` 버프. 공격력 버프 `STAT_ATTACK/ICHI_ATTACK`·무기 공격력은 제외) — **버프로 요구치를 넘으면 그동안 패널티가 사라진다** (A4 ②④ 교체). TP 충전이 매 틱 `TotalAgility`를 읽으므로 동적 반영 | 4-5, 0-13 |
| Q43 (10-19 2차) | 최대치 합산 방식 | **계산 프로퍼티 (F2 — 제안 B).** `MaxHP/maxResource`는 캐릭터 고유치(성장·스킬트리 노드 포함)만 저장하고, `TotalMaxHP = MaxHP + MAX_HP 옵션 합`, `TotalMaxResource = maxResource + (자원 종류가 맞는 MAX_MP/MAX_RAGE 옵션 합)`을 매번 계산한다. `ApplyEquipMaxDeltas` 폐기. 구 세이브는 로드 때 1회 구 EquipDB 표로 델타를 뺀다 | 3-8, 3-11, 5-8, 9-1 |
| Q44 (10-19 2차) | 위험도 방어 보정 (A3) | `MonsterStatDB.HazardDef` = **위험도 1당 방어력 스탯 +**, `HazardMdef` 컬럼 추가 (F9 확정). 예: 방패병 방어 10 · HazardDef 0.5 · 위험도 6 → 13 | 2-1 |
| Q45 (10-19 3차) | 버프 중첩 | **출처가 다른 `STAT_*` 버프는 합산한다** (H1 — 방어 행동 + 철귀 방어력이 둘 다 적용. 철귀를 능동적으로 쓰게 하기 위함). 같은 출처를 다시 쓰면 갱신. 기존 "같은 종류는 강한 쪽만" 규칙(`ApplyTimedDebuffTo`)은 `STAT_*`에는 쓰지 않고, `ICHI_*`는 버프 개편에서 같은 원칙으로 | 1-12, 1-15, 6-2 |
| Q46 (10-19 4차) | 에리스 평타 계수 (I1) | **힘 + 핵 마법공격력을 쓰고, 결과는 마법(공명) 공격.** 일반화: 무기 행에 **`ScaleStat`(STR/INT)** 컬럼을 두고(SkillDB와 같은 이름·값), **공격력 = `ScaleStat` 스탯 + (속성이 마법이면 Σ무기 `MagicAttack`, 물리면 Σ무기 `PhysicalAttack`)** — `GamePlayer.GetAttackPower(bool scalesWithInt, AttackAttribute attribute)` 한 함수. 평타는 무기의 `ScaleStat`·`Attribute`로 부른다 (핵 = RESONANCE·STR → 힘 + 핵 마법공격력, 마법방어로 경감 / 대검 = SLASH·STR → 힘 + 물리공격력 / 지팡이 = ICHI·INT → 지능 + 마법공격력). **스킬도 SkillDB의 `ScaleStat`·`Attribute`로 같은 함수를 쓴다 (J1 확정)** — 에리스 공명 스킬 = 힘 + 핵 마법공격력, 화염구 = 지능 + 지팡이 마법공격력, 연속 베기 = 힘 + 대검 물리공격력. 속성 `NONE`(치유의 빛·수호 축복 등)은 `ScaleStat` 쪽 — INT면 마법공격력, STR이면 물리공격력. `TotalPhysicalAttack/TotalMagicAttack`은 스탯창 표시용으로 남긴다. 핵 행의 공격력은 `MagicAttack` 쪽에 둔다 (EW1 0/3, EW2 0/6) | 0-4, 3-6 |

### 결정에서 파생된 가정 (10-19 2차: 전부 확정 — A2는 Q41, A3는 Q44, A4는 Q42로 교체)
| # | 항목 | 가정 |
|---|---|---|
| A1 | 몬스터 방어 행동의 임시 처리 (Q14) | **확정 (10-19, Q37)** — 정식 리워크 전까지 실드 행동 7종은 "자신/아군 `STAT_DEFENSE` +ActionValue, **3턴(자기 턴 종료 차감)**" 스탯 버프로 **임시 전환**해 게임이 돌아가게 둔다. 예고 아이콘은 DEFENSE 그대로 |
| A2 | 플레이어 실드 (Q14) | **Q41로 교체 (10-19 2차)** — 몬스터 실드 삭제에 더해, 플레이어의 **방어 행동·철귀도 실드를 만들지 않는다**. 실드는 스킬(수호 축복·만개)·보호막 옵션만 만들고 소진까지 지속, 새 실드는 덮어쓰기 |
| A3 | 몬스터 위험도 방어 보정 (Q14) | **확정 (10-19 2차, Q44)** — `MonsterStatDB` `HazardDef`의 의미를 "실드 획득량 +"에서 **"방어력 스탯 +"(위험도 1당)** 로 바꾼다. 마법방어용 `HazardMdef` 컬럼 추가 |
| A4 | 힘 요구치 판정 (Q16) | **Q42로 교체 (10-19 2차)** — 요구치는 **장비 행마다** `RequireStr` (중갑 높음 / 경갑 낮음 / 로브 0). 착용한 방어구 중 하나라도 요구치를 못 넘으면 민첩 1/2 — **패널티는 1회만**. 비교 대상은 **힘 버프를 포함한 현재 힘**(공격력 버프·무기 공격력 제외) — 버프로 요구치를 넘으면 패널티가 풀린다 |
| A5 | 초기 장비 (Q16) | **확정 (09-29)** — 방어구가 한 부위라도 없으면 모든 속성이 약점이므로, 새 캐릭터에게 **같은 종류 방어구 3부위 세트**를 지급한다 (게오르크 중갑 / 에리스 경갑 / 홍단향 로브). 방어구가 빠진 구 세이브는 로드 시 1회 기본 세트를 보충한다 |
| A6 | 약점 상쇄 스택 (Q23) | **확정 (10-19 2차, F12)** — 서로 다른 속성의 약점 상쇄는 각각 적용되고, 같은 속성끼리는 중복 효과가 없다. "모든 속성 약점" 상태에도 적용된다 (상쇄한 속성만 빠짐). 상쇄 속성은 드랍 시 6속성 중 랜덤 (CSV 가중치) |
| A7 | 드랍 판정 단위 (Q24) | **Q39로 교체 (10-19)** — 처치한 몬스터마다 그 몬스터의 드랍 테이블을 **싱글은 파티 1회, 멀티는 캐릭터(`GamePlayer`)마다** 굴린다 (F6 확정 — 캐릭터 2개면 2번). 멀티 배율은 그 위에 곱한다 |
| A8 | 메르크리우스 상점 (Q20) | 판매 목록은 CSV(`ShopDB`)로 두고 1차는 NORMAL 등급 기본 장비(옵션 굴림 없이 NORMAL 인스턴스)를 판다. 물약 판매 여부는 같은 CSV에 행을 넣어 조절한다. 판매가(플레이어 → 상점)는 `EquipDB.Price × 등급별 판매율`(EquipGradeDB). 장착 중인 장비는 팔 수 없다. 인벤토리 상한 없음. 골드는 **플레이어 공유 골드(Q35)** 에서 내고 받는다. **확정 (10-19 2차, F13)** — 메르크리우스는 추후 별도 업데이트 예정이므로 1차는 이 규칙으로 |
| A9 | `SkillCast` 도입 범위 (Q10) | **확정 (10-19 2차, F14)** — 스킬 효과 위임을 `ExecuteSkill(SkillCast cast)`로 바꾸고 `SkillData.*` 전체를 한 커밋으로 기계적으로 이관한다. 배율은 1.0이므로 동작 변화는 없어야 한다. 퍼센트·지속 턴처럼 곱하기 애매한 수치는 기획 「파티 TP·연계 — 후속 검토」가 정해질 때까지 배율을 적용하지 않는 수치로 표시만 해 둔다 |

---

## Phase 0 — 데이터 구조 설계 (코드 변경 없음)

**목표**: 판정·몬스터 방어·인스턴스 장비·옵션·방어구·소모품·드랍 데이터 형식을 확정해, 이후 Phase가 CSV만 채우면 되게 한다.
※ 이 Phase는 **설계와 기획서 기록만** 한다. CSV 파일의 헤더를 실제로 바꾸면 로더(`EquipData`·`ConsumableData`·`MonsterData`)가 깨지므로 파일 변경은 Phase 3·6·7에서 로더와 함께 한다.

- [x] 0-1. `EquipGrade` enum + **`EquipGradeDB.csv`** (Q3): `Grade, OptionCountMin, OptionCountMax, LegendOptionCount, AccessoryOptionCountMin, AccessoryOptionCountMax, DropWeight, EliteBonusWeight, BossBonusWeight, MinHazard, SellPercent, ColorHex, NameKey`
      기획 기본값: NORMAL 0개(악세사리 1개) / ADVANCED 1~2개 / RARE 1~2개 / LEGEND 일반 2개 + 전설 1개. 수치 높낮이는 0-3의 등급별 범위가 정한다
- [x] 0-2. `EquipOptionType` enum 초안 (`Common/ProjectD.cs`)
      - 일반: `STRENGTH, INTELLIGENCE, AGILITY, CONTROL, MAX_HP, MAX_MP, DEFENSE, SHIELD`
      - 전설 14종: `LIFE_STEAL, SKILL_DAMAGE, COMBO_DAMAGE, CRIT_DAMAGE, REFLECT_FLAT, REFLECT_PERCENT, HEAL_RECEIVED, WEAKNESS_NULLIFY, CRIT_RATE, ACCURACY, EVADE, FREE_MOVE, NO_ROW_PENALTY, MAX_RAGE`
- [x] 0-3. **`EquipOptionDB.csv`** — 옵션 × 등급마다 한 행 (Q3): `OptionType, Grade, Slots('|' — WEAPON|ARMOR|HELMET|BOOTS|ACCESSORY), MinValue, MaxValue, IsPercent(0/1), ValueKind(NUMBER|ATTRIBUTE|FLAG), Weight, NameKey`
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
- [x] 0-4. `EquipDB.csv` 개편안 — 기본 옵션만 남긴다: `EquipNo, Name, Slot, WeaponType, ArmorType, Attribute, ScaleStat, RequireLevel, RequireStr, PhysicalAttack, MagicAttack, Defense, MagicDefense, Price, DropWeight, Description` (`ScaleStat`은 10-19 4차 Q46에서 추가)
      - 삭제: `Character`(착용 제한 없음), `Grade`(인스턴스가 가짐), `Agility/MaxHP/MaxResource`(추가 옵션으로 이관), `SkillLevel`(Q1)
      - 무기: **모든 무기 행이 `PhysicalAttack`·`MagicAttack`을 둘 다 가진다** (Q22 — 값은 무기마다 CSV로 조절, 0 허용). **`Attribute`(무기 행만, Q25)** = 기본 공격 속성 — `CharacterStatDB.BasicAttackAttribute`는 폐기. **`ScaleStat`(무기 행만, STR/INT — Q46)** = 평타 계수 스탯 (대검·장검·핵 STR / 지팡이·책·수정구 INT) — `CharacterStatDB.AttackStat`은 폐기. 평타 공격력 = `ScaleStat` 스탯 + 속성에 맞는 쪽의 무기 공격력 (핵 = 힘 + 마법공격력). 무기 공격력은 힘·지능 스탯에 더하지 않는다 (3-6)
      - 갑옷·투구·신발: `ArmorType` + `RequireStr` + 방어력·마법방어. 악세사리: 기본 옵션 없음 (노멀도 추가 옵션 1개)
- [x] 0-5. `WeaponType { NONE, GREATSWORD, LONGSWORD, STAFF, BOOK, ORB, CORE }` (대검·장검·지팡이·책·수정구·핵) / `ArmorType { NONE, HEAVY, LIGHT, ROBE }` (중갑·경갑·로브)
- [x] 0-6. **`ArmorTypeDB.csv`**: `ArmorType, Weaknesses('|'), NameKey` — HEAVY = STRIKE|RESONANCE, LIGHT = PIERCE|SIMSANG, ROBE = SLASH|ICHI.
      세트 규칙(기획서 「방어구」)은 코드 규칙으로 두고 기획서 절에 "구현 형식"으로 적는다: 3부위(갑옷·투구·신발) 중 미착용 부위가 있으면 전 속성 / 착용 종류 1개 = 그 종류 약점 / 2개 = 두 종류 약점 합집합 / 3개 = 전 속성
- [x] 0-7. `AttackAttribute` 개편안 (Q15): `{ NONE, SLASH, STRIKE, PIERCE, ICHI, SIMSANG, RESONANCE }` + `IsMagic()` = ICHI·SIMSANG·RESONANCE. 기존 CSV의 `MAGIC` 값을 `ICHI`로 바꾸는 이관 목록 작성 (SkillDB `Attribute`, MonsterStatDB `Weakness/AttackAttribute`, EquipDB 무기 `Attribute` — `CharacterStatDB.BasicAttackAttribute`는 Q25로 삭제)
- [x] 0-8. 장비 인스턴스 형식: `EquipInstance { string instanceId; string equipNo; EquipGrade grade; int[] optionTypes; int[] optionValues; }`
      `optionValues`는 `ValueKind`에 따라 수치 / `AttackAttribute` 정수 / 1(플래그)을 담는다. Mirror `SyncList<EquipInstance>` — 필드가 기본형·배열뿐이라 Weaver 자동 직렬화가 된다 (`List<>` 필드 금지)
- [x] 0-9. **소모품 구조** (Q5, Q6) — `ConsumableDB.csv`: `PotionNo, Name, Effect, ValidTarget, Value, Duration, BattleOnly(0/1), UsableResource(ANY|MP|RAGE|HP), Price, DropWeight, Description`
      - `Effect` = 효과 메서드 이름. 스킬(`SkillData`)과 같은 **리플렉션 바인딩** — `ConsumableEffects.<Effect>(ConsumableDef def, TargetObject user, TargetObject target)`
      - 같은 효과의 변형(HP 물약 30/60 등)은 **CSV 행만** 추가, 새 효과는 **CSV 행 + 같은 이름의 메서드** 추가
      - `ValidTarget`은 스킬과 같은 enum을 재사용한다. 스킬이 `NONE`을 "자신·대상 불필요"로 쓰므로 **소모품도 `NONE` = 자신**으로 통일한다 (`SELF`를 따로 추가하지 않는다 — R10). 1차는 `NONE`만 허용, 추후 `MEMBER/ENEMY`를 열면 대상 선택 UI만 붙이면 되게 한다
      - 효과 메서드의 사용자·대상 인자는 **`GamePlayer` 기준**이다 (Q38, R1). 거점·미로에는 TargetObject가 없다. 전투 중에는 `M_TurnManager.GetCurrentPlayerTargetObject`로 TargetObject를 찾아 전투 전용 효과(스탯 버프·치명 회복)를 보강한다. 몬스터 대상(추후)은 TargetObject만 있으므로 "유닛" 추상(`GamePlayer` 또는 `TargetObject`를 감싸는 `IBattleUnit`)을 두는 것을 검토
      - 1차 효과: `HEAL_HP, RESTORE_MP, RESTORE_RAGE, BUFF_ATTACK, BUFF_CRIT_RATE, BUFF_EVADE`. 극약(`POISON_TO_ONE`)은 후순위 (Q6)
      - `ConsumableType` enum은 폐기한다 (`Effect` 문자열 바인딩으로 대체)
- [x] 0-10. 보조 스탯·방어 스탯 컬럼
      - `CharacterStatDB.csv`: `BaseEvade, BaseAccuracy, BaseCrit` 추가 (게오르크·홍단향 5/95/5, 에리스 10/95/10), `Weakness/Resist` 삭제 (Q17), `BasicAttackAttribute`·`AttackStat` 삭제 (Q25·Q46 — 무기 행의 `Attribute`·`ScaleStat`이 기본 공격의 속성과 계수를 정한다)
      - `MonsterStatDB.csv`: `Evade, Accuracy, CritRate`(없으면 5/95/0, Q4), **`Defense, MagicDefense`**(Q14), `HazardMdef` 추가, `HazardDef` 의미 변경 (A3), `DropTable` 추가 (0-12). 미사용 `TPShield` 컬럼은 삭제 여부를 확인한다 (`Monster.tpShield` 로드만 되고 참조 없음)
- [x] 0-11. BalanceDB 키 목록 확정 (플레이어·몬스터 공용 — Q18)
      - 판정: `DAMAGE_RANDOM_PERCENT`(10), `CRIT_DAMAGE_PERCENT`(150 — 피해·회복·실드 부여 공용, Q29), `CRIT_DEFENSE_PERCENT`(50), `CRIT_DEFENSE_FLAT_REDUCE`(10), `CRIT_TP_DAMAGE`(5)
      - 방어 행동 (Q40): `DEFEND_DEFENSE_UP_BASE`(10)·`DEFEND_DEFENSE_UP_PER_LEVEL`(1) — 방어 행동 시 물리 방어력 +(기본 + 레벨 × 레벨당), 임시 수치. `DEFEND_BASE_VALUE` 폐기. 철귀: `HS0_DEFENSE`(10)의 의미를 실드량 → 방어력 버프량으로 (Q41)
      - 약점: `WEAKNESS_DAMAGE_PERCENT`(120, 기존), `WEAKNESS_TP_FIXED`(15), `WEAKNESS_TP_RATIO`(5), `WEAKNESS_CRIT_BONUS`(10), **`WEAKNESS_TP_REPEAT_PERCENT`**(50 — 반복 체감률, Q12. 플레이어 대상에도 적용 — Q30), `WEAKNESS_TP_MIN`(구 `TP_BREAK_MIN` 개명, 0이면 하한 없음)
      - 방어구: `ARMOR_STR_PENALTY_AGI_PERCENT`(50 — 힘 부족 시 민첩 비율, Q16)
      - 드랍: `ITEM_DROP_MULTIPLAYER_PERCENT`(150)
      - 폐기: `TP_BREAK_BASE`, `TP_BREAK_MIN`(개명), `EQUIP_DROP_PERCENT`·`POTION_DROP_PERCENT`·`EQUIP_DROP_MIN_HAZARD`(드랍 테이블로 대체, Q24), `SKILL_LEVEL_BONUS_PERCENT`(Q1 — 크리스털 작업과 함께 정리), `DEFEND_BASE_VALUE`(Q40)
      - 등급 가중치·전설 위험도 하한·옵션 배율 키는 **만들지 않는다** — `EquipGradeDB`/`EquipOptionDB`가 가진다 (Q3)
- [x] 0-12. **드랍 테이블** (Q24) — `DropTableDB.csv`: `TableId, Kind(EQUIP|POTION), ItemNo, Chance, MinHazard, Description`
      - `ItemNo`는 특정 `EquipNo/PotionNo` 또는 풀 지정자(`ANY_EQUIP`, `ANY_WEAPON`, `ANY_ARMOR`, `ANY_ACCESSORY`, `ANY_POTION` — `DropWeight` 가중 추출)
      - `Chance`는 %이며 100을 넘을 수 있다. 판정: 100 이하 부분은 확정, 넘는 몫은 추가 1회 판정 확률 (예: 150 → 1개 확정 + 50%로 1개 더)
      - `MonsterStatDB.DropTable`로 몬스터마다 테이블을 지정한다 (없으면 `DEFAULT_NORMAL/ELITE/BOSS` 테이블)
- [x] 0-13. **스탯 버프 타입** (Q15) — 이 워크플로우가 새로 쓰는 버프는 `ICHI_*`가 아니라 스탯 이름 기반으로 정의한다: `STAT_ATTACK, STAT_STRENGTH, STAT_DEFENSE, STAT_MAGIC_DEFENSE, STAT_CRIT_RATE, STAT_EVADE, STAT_ACCURACY` (양수 = 상승, 음수 = 하락). `STAT_STRENGTH`(힘 버프)는 `STAT_ATTACK`(공격력 버프)과 다르다 — 힘 요구치 판정(Q42)에는 힘 버프만 들어간다.
      버프 개편 작업과 이름·구조를 맞춘다 — 개편이 먼저 끝나면 그 타입을 쓴다. `BuffDB.csv`·`buff.<enum>.name` 로컬라이즈 행 포함
- [x] 0-14. `ShopDB.csv` (Q20, A8): `ShopId, Kind(EQUIP|POTION), ItemNo, Price(비우면 DB Price), Description` — 1차 `ShopId=MERCURIUS`

**완료 기준**: 위 CSV 헤더·enum·세트 규칙이 기획서 「장비 옵션」「아이템」「속성」「방어구」「약점 공격」 절에 "구현 형식" 줄로 적혀 있다.
**완료 (2026-10-19)** — 상세 형식은 `RPG_CONVERSION_ITEMS.md` §1~§11, 기획서 9개 절(「멀티플레이어 변경점」「거점」「보조 스탯」「속성」「약점 공격」「무기」「방어구」「장비 옵션」「아이템」)에 "구현 형식 (2026-10-19, Phase 0)" 줄 추가. 수치는 전부 초안(9-4). 에리스 평타 계수는 I1 답변으로 확정 (Q46 — 무기 행 `ScaleStat` 컬럼 추가, ITEMS §4-1). **10-19 4차 Phase 0 문서 검토**: 구 세이브 변환 매핑(구 AR2 → AR3 · 구 HE2 → HE3 · PO3 자원별 분기 — R13)과 초기 중갑 세트 `RequireStr` 10 → 8(원래 기본 힘 — R15)을 수정. 스킬의 무기 공격력 선택(R14)은 J1 답변으로 확정(스킬도 `GetAttackPower`) — 3-6에서 구현. **미결 없음.**

---

## Phase 1 — 전투 계산 파이프라인·판정 (명중 → 회피 → 치명, 랜덤 보정, 약점 개정)

**목표**: 플레이어 → 몬스터와 몬스터 → 플레이어가 **같은 함수·같은 공식**으로 계산된다. 기본치만으로 MISS·치명타·데미지 편차가 발생하고, 약점 공격이 09-28 공식대로 TP를 깎는다. 모든 스킬 수치가 콤보 배율 슬롯을 거친다 (배율 1.0 — 동작 변화 없음).

- [x] 1-1. 보조 스탯 로드 — `CharacterStatData.Entry`에 `baseEvade/baseAccuracy/baseCrit`, `MonsterData`에 `evade/accuracy/critRate` (0-10 컬럼, 컬럼이 없으면 기본값)
- [x] 1-2. **공용 스탯 조회** — `TargetObject`(플레이어·몬스터 공통)에 `GetEvade / GetAccuracy / GetCritRate / GetCritDamage / GetDefense / GetMagicDefense / GetPhysicalAttack / GetMagicAttack`을 둔다. 플레이어는 `GamePlayer.Total*` + 스탯 버프, 몬스터는 `MonsterStatDB` + 위험도 보정 + 스탯 버프. 보조 스탯은 레벨 성장이 없다 (기획).
      `BattleActions.IsMagic(AttackAttribute)` 헬퍼도 여기서 만든다 (R6 — Phase 2가 쓴다). Phase 4 전까지는 `MAGIC`·`RESONANCE`를 마법으로 취급하고, 4-1에서 `ICHI·SIMSANG·RESONANCE`로 바꾼다
- [x] 1-3. **`HitContext` + `BattleActions.ResolveHit(ctx)`** (Q10, Q18, R5) — 한 번의 타격을 표현하는 컨텍스트: 공격자·대상·기본 피해·속성·스킬 여부·r·결과 플래그(miss/crit/weakness)·`powerMultiplier`(콤보 배율 슬롯, 기본 1.0)·`bonusPercent`(스킬 추가 데미지 등).
      **ResolveHit은 공격 측 계산까지만 한다**: 명중·회피 단일 판정(1-6) → (MISS면 종료 — 부가 효과도 생략, Q27) → r 굴림 → 약점 판정(피해 120%) → 치명 판정(약점 보정 포함) → 배율 적용 → **대상 종류별 피격 함수 호출** → 약점 TP + 치명 TP → 결과 표시.
      **방어 측은 피격 함수가 담당한다**: `DamageToPlayer(damage, attribute, isCritical)` = 대열 보정 → 보호 분담 → 실드(방어 공식 무시, Q29) → 남은 피해에 방어 공식(치명이면 50% − 10) → HP 적용 → 분노(최종 피해 기준, Q32). `DamageToMonster(damage, from, attribute, isCritical)` = 방어 공식(Phase 2) → HP 적용. 두 함수 모두 **HP에 실제로 들어간 피해(int)를 반환**한다 (분노·생명력 흡수·반사의 기준값).
      - 보호 분담(`RedirectToGuardian`)은 원 대상에서 판정이 끝난 피해를 나누기만 한다 — 보호자에게 약점·치명을 다시 판정하지 않는다
      - 반격(GS13)·반사 옵션(5-6)은 **별도의 `HitContext`로 `ResolveHit`을 다시 탄다** (명중·치명·r 2차 판정, Q27). 개화꽃 등 고정 피해(`StaticDamage*`)는 판정·랜덤·방어 없음 — 기존 경로 유지
      호출부 이관:
      - 플레이어 → 몬스터: `BattleActions.AttackTarget` 내부를 `ResolveHit`로 교체 (기사도 배율은 ctx 가산)
      - 몬스터 → 플레이어: `SpawnedMonster.GeneralAttack`이 `DamageToPlayer`를 직접 부르지 않고 `ResolveHit`을 거친다
- [x] 1-4. **`SkillCast` 도입** (Q10, A9) — 스킬 위임을 `ExecuteSkill(SkillCast cast)`로 바꾼다 (`cast.def / user / targets / powerMultiplier / comboOrder`). `SkillData.Geork/Eris/DanHyang` 전 메서드를 한 커밋으로 기계적으로 이관한다.
      피해·실드·회복·버프 수치는 모두 `cast.Scale(value)`를 거친다. 공격은 `cast`에서 만든 `HitContext`로 `ResolveHit` 호출. 퍼센트·지속 턴 등 배율을 곱하지 않을 수치는 주석으로 표시.
      ※ 착수 전에 워킹 트리의 미커밋 변경(SkillDB `FreeAction` 컬럼·`SkillDef.freeAction`·`tpUsedFreeSkillNo` 개명)을 먼저 커밋한다 (R7)
- [x] 1-5. 일반 데미지 랜덤 보정 (Q11) — `ResolveHit` 안에서 타격마다 −`DAMAGE_RANDOM_PERCENT` ~ +`DAMAGE_RANDOM_PERCENT` 정수를 서버에서 굴려 피해 × (100 + r)/100. 다단 공격(연속 베기·풍차 베기·찢어 줄게요 반복 등)은 `ResolveHit`을 타수만큼 호출하므로 자동으로 타마다 굴린다. 몬스터 예고 수치는 이미 숨겨져 있다 (`NextActionIndicator.ShowActionValue = false`)
- [x] 1-6. 명중·회피 (Q26, Q27) — **단일 판정**: MISS 확률 = max(0, 대상 회피 + (100 − 공격자 명중)). 명중 105% vs 회피 5%면 0%, 기본(95/5)이면 10%. MISS면 피해·분노·약점 TP·치명 TP·피격 모션·**그 타격에 딸린 부가 효과(디버프·TP 감소)** 를 모두 생략한다 (기획 "약점 공격 Miss 시 TP 피해 없음"). 광역 공격은 **대상마다 개별 판정**
- [x] 1-7. 치명 — 치명률 + (약점이면 `WEAKNESS_CRIT_BONUS`). 피해 × 치명 배수. 방어 공식은 `ApplyDefenseFormula(damage, defenseStat, defensePercent, flatReduce)`로 시그니처를 확장해 치명이면 50% 적용 후 −10 (0 하한). **몬스터 대상에도 적용** (Q14 — 몬스터 방어력은 Phase 2 전까지 0이라 효과 없음, 경로만 연결). 몬스터 기본 치명 0 + 약점 보정 10%는 의도 (Q28)
- [x] 1-8. 약점 TP (Q12, Q30) — `ApplyTpBreakTo`를 `ApplyWeaknessTpDamage(target, r)`로 교체: TP 감소 = (`WEAKNESS_TP_FIXED` + `WEAKNESS_TP_RATIO` × r / 10) × (`WEAKNESS_TP_REPEAT_PERCENT`/100)^(이번 전투에서 그 대상이 약점 피격된 횟수), 하한 `WEAKNESS_TP_MIN`. `tpWeaknessHits` 집계는 유지. **플레이어 `TpUnit`도 같은 규칙(반복 체감 포함)으로 처리** (Phase 4의 플레이어 약점 피격용)
- [x] 1-9. 치명 TP (Q13) — 치명이면 `DamageTpTo(target, CRIT_TP_DAMAGE)`. 약점 TP와 합산, 반복 체감 없음
- [x] 1-10. 회복·실드 부여 치명 (Q29, Q41) — `TargetObject.Heal(value, healer)` / `GainShield(value, giver)`: 시전자 치명률로 굴려 `CRIT_DAMAGE_PERCENT` 배수 적용 (판정 순서 "회복 : 치명"). **`GainShield`는 기존 실드를 지우고 새 값으로 교체한다** (합산 아님 — 100에 10을 부여하면 10). 수호 축복·만개·보호막 옵션·HP 물약이 이 경로. 기존 `GainDefense`(가산)는 제거. 몬스터 회복도 같은 함수를 쓸 수 있게 TargetObject 공통으로 둔다 (Q5 대비).
      **비전투 회복은 TargetObject가 없다** (R1) — `GamePlayer.ServerHeal(value, healerGamePlayer)`를 두고, 전투 중이면 TargetObject의 `Heal`로 위임, 비전투면 `HP` 직접 가산. 치명·받는 회복량 옵션(5-7)은 양쪽에 적용된다 (Q38)
- [x] 1-11. 표시 — `M_EffectManager.DisPlayeDamage` 확장: MISS 텍스트(피격 모션·셰이크 없음), 치명타 숫자 강조(색·크기). `RpcDisplayZeroDamage` 패턴으로 `RpcDisplayMiss` (RPC 해시 충돌 확인)
- [x] 1-12. 스탯 버프 훅 (0-13, Q15, Q37) — 1-2 조회 함수가 `STAT_*` 버프를 가산한다. 지속 규칙은 기존 `ApplyTimedDebuffTo` 그대로 — **n턴, 대상의 자기 턴 종료 시 1 차감, 자기 턴 중 부여면 그 턴은 세지 않음**(사용 턴 제외 n턴, Q33).
      **중첩 규칙 (Q45)**: `STAT_*` 버프는 **출처(스킬·행동·물약 종류)별로 따로 관리해 합산**한다 — 같은 출처를 다시 쓰면 그 출처의 수치·지속만 갱신, 다른 출처는 더한다 (예: 방어 행동 + 철귀 방어력 = 둘 다 적용, 치명타 물약 + 홍단향 치명타 증가 = 합산). `TpTimedDebuff`에 `sourceId`(스킬 번호·`DEFEND`·`HS0`·물약 번호)를 두고 (target, type, sourceId)로 찾는다. 기존 `ICHI_*`의 "같은 종류는 강한 쪽만" 규칙은 버프 개편 때 같은 원칙으로 바꾼다 이후 물약(Phase 6)·홍단향 마법 트리(치명타 증가·회피율 증가·스쿤다)·몬스터 방어 행동 임시 전환(Phase 2)·몬스터의 플레이어 방어 저하(Q31)가 이 훅을 쓴다. `ICHI_*`는 기존 스킬 호환용으로만 남긴다
- [x] 1-13. 디버그 — `GamePlayer.DebugStats`에 보조 스탯 4종 표시·조정, 치명/회피 100% 치트, 랜덤 보정 0 고정 치트, 몬스터 스탯 확인
- [x] 1-14. **실드 지속 규칙 변경** (Q37, Q41) — 실드는 전부 소진될 때까지 지속한다. TP 루프의 자기 턴 시작 리셋(`next.target.defense = 0`)을 제거한다 (예외 없음 — 방어 행동이 더 이상 실드를 만들지 않으므로). 실드를 만드는 것은 수호 축복(HS4)·만개(HU1)·보호막 옵션(5-9)뿐이며 모두 1-10 `GainShield`(덮어쓰기)를 쓴다. 허물 강화(ENHANCESKIN)의 회복→실드 가산 분기는 쓰는 곳이 없으므로 `Heal`로 옮기면서 제거한다 (버프 개편 삭제 대상)
- [x] 1-15. **방어 행동·철귀 전환** (Q40, Q41) — `TpAction.DEFEND`를 "실드 획득"에서 **"물리 방어력 임시 버프"** 로 바꾼다. 증가량 = `DEFEND_DEFENSE_UP_BASE`(10) + 레벨 × `DEFEND_DEFENSE_UP_PER_LEVEL`(1) (임시 수치, 9-4에서 조정). 마법방어는 오르지 않는다. `DEFEND_BASE_VALUE`·`ICHI_DEFENSE` 가산 경로 삭제.
      **만료 시점은 "다음 자기 턴이 올 때까지"(턴 시작 만료)** — 기존 `ApplyTimedDebuffTo`는 턴 **종료** 차감이라 그대로 쓸 수 없다. `TpTimedDebuff`에 `expireOnOwnTurnStart` 플래그를 추가하고, TP 루프의 턴 실행 직전(지금 `defense = 0` 리셋이 있던 자리)에서 그 플래그가 켜진 버프를 상쇄 제거한다. 다른 지속 버프(3턴, 턴 종료 차감)는 영향 없음.
      철귀 턴 종료 효과(`ApplyPlayerTurnEndEffects`)의 `GainDefense(HS0_DEFENSE)`도 같은 방식의 **`STAT_DEFENSE` +10 임시 버프**(그 아군의 다음 턴 시작에 만료)로 바꾼다. 공격력 +3(`HS0_ATTACK`)은 기존 지속 그대로. 방어 행동과 철귀 버프가 같은 아군에 겹치면 **합산한다** (Q45 — 다른 출처의 버프는 합산, 철귀를 능동적으로 쓰게 하기 위함). 1-12의 출처별 중첩 규칙을 따른다. SkillDB HS0 설명문·파티 TP 기획(철귀 버프 부여 시 +1)의 "실드" 표현 정리

**완료 기준**
- 에리스 회피 10%로 몬스터 공격이 가끔 MISS 난다 (몬스터 명중 95 → 15%). 몬스터도 플레이어 공격을 가끔 회피한다 (기본 10%)
- 같은 공격의 피해가 ±10% 안에서 흔들리고, 다단 공격은 타마다 다르다. MISS 난 타격은 디버프도 걸리지 않는다
- 치명타 시 배율·방어 50% −10·TP −5가 양방향 로그로 확인된다
- 약점 TP 감소가 10~20에서 매번 달라지고, 같은 대상 반복 시 `WEAKNESS_TP_REPEAT_PERCENT`만큼 줄어든다
- 홍단향 회복 스킬·수호 축복에 치명 회복/치명 실드가 발생한다. 거점에서 HP 물약을 써도 치명 회복이 난다
- 수호 축복 실드가 자기 턴이 돌아와도 사라지지 않는다. 실드 100인 아군에게 수호 축복 10을 걸면 실드가 10이 된다
- 방어 행동을 하면 실드가 생기지 않고 방어력이 (10 + 레벨)만큼 오른 채로 다음 자기 턴이 올 때까지 유지되며, 자기 턴이 시작되면 사라진다. 마법 공격에는 효과가 없다. 철귀가 붙은 아군도 턴 종료에 같은 방어력 버프 10을 받는다
- 게오르크 분노가 실드가 흡수한 피해로는 오르지 않고, 방어 적용 후 HP에 들어간 피해로만 오른다
- `SkillCast` 이관 전후로 전 스킬의 결과가 같다 (배율 1.0 회귀 확인)

**구현 (2026-10-19)** — 컴파일·정적 검증 완료(방어 공식 치명 분기, BalanceDB 신규 키, CharacterStatDB 보조 스탯, SkillDB 30행 `SkillCast` 바인딩). **인게임 플레이 확인은 아직** — 위 완료 기준을 1스테이지 전투로 확인해야 한다.
- 신규: `Battle/HitContext.cs`, `Battle/TargetObject.Stats.cs`(Get* 조회·`Heal`·`GainShield`·`RpcDisplayMiss`), `Player/GamePlayer.Combat.cs`(`TotalEvade/Accuracy/CritRate/CritDamage`·`ServerHeal`)
- `BattleActions.ResolveHit`(명중·r·약점·치명·배율 → 피격 함수 → 약점 TP·치명 TP → 분노) — `AttackTarget`(플레이어)·`SpawnedMonster.ResolveAttack`(몬스터)·`CounterAttack`(2차 판정) 공용. `LastHitLanded`로 MISS 시 부가 효과 생략 (GS8·GS18·ES6)
- `DamageToPlayer/DamageToMonster(…, attribute, isCritical)`가 int(HP 피해) 반환. 몬스터 실드(`defense`)·`GainDefense`는 Phase 2까지 유지
- `ApplyTimedStatBuff(target, type, value, turns, from, sourceId, expireOnOwnTurnStart)` — 출처별 합산. 방어 행동 `"DEFEND"`·철귀 `"HS0"`은 턴 시작 만료. 구 `ApplyTimedDebuffTo`(ICHI_* 등)는 `sourceId == null`로 분리 유지
- 방어 행동 모션은 캐릭터별 이름이 달라 미재생(0.3초 대기만). 치명 표시는 `critDamageDisplay` SyncVar(HP 훅 전 선언) 패턴, MISS는 `RpcDisplayMiss` → `M_EffectManager.DisplayMiss`. 로컬라이즈 `ui.msg.miss`·`buff.STAT_*`는 ko(BuffDB)·en만 — 나머지 로케일은 8-1
- 디버그 치트(치명 100% / 회피 100% / 랜덤 0)는 `BattleActions` 정적 플래그 — 호스트에서만 효과

---

## Phase 2 — 몬스터 방어력 전환 (Q14 신설)

**목표**: 몬스터가 실드 대신 방어력·마법방어 스탯을 가지고, 플레이어의 공격이 몬스터 방어 공식으로 경감된다. 실드를 쓰던 몬스터 7종은 임시 행동으로 돌아가고, 정식 리워크를 기다린다.

- [x] 2-1. 로드 — `MonsterData`에 `defense/magicDefense/hazardMdefBonus` 추가. `hazardDefBonus`는 방어력 스탯 보정으로 의미 변경 (A3 확정, Q44). `SpawnedMonster.ScaledDefense`를 "위험도 보정 방어력 스탯"으로 재정의하고, 1-2의 `GetDefense/GetMagicDefense`가 쓴다
- [x] 2-2. 몬스터 피격 방어 공식 — `DamageToMonster(damage, from, attribute, isCritical)`가 `ApplyDefenseFormula`(1-2 `IsMagic`이면 마법방어)를 적용한다 (1-3 역할 분담). `DamageToMonster`·`StaticDamageToMonster`·`THEREISNOJABI` 경로의 실드(`defense`) 분기를 모두 제거한다 (몬스터의 `defense` SyncVar는 쓰지 않음). `StaticDamageToMonster`는 방어 무시 유지
- [x] 2-3. 몬스터 실드 행동 임시 전환 (A1 확정, Q37) — `Soldier_Shield`, `WacherB`, `Guardian`, `SpearManB`, `GiantSoldier`(SinglePattern 리터럴 10/15/20도 MonsterDB로), `Happy`, `Saddy`의 `GainDefense` 호출을 `ApplyTimedDebuffTo(STAT_DEFENSE, +ActionValue, 3턴)`으로 바꾼다 (자기 턴 종료 차감 — 1-12 규칙). 예고 아이콘은 DEFENSE 유지. `TargetObject.GainDefense`의 몬스터 분기(`ScaledDefense` 보정)는 제거
- [x] 2-4. 스킬·몬스터 행동 이관 (Q31) — 에리스 부서지세요(ES6)·얼마나 버틸까요(ES8): "현재 실드 즉시 삭감 + 방어 획득량 감소 디버프(`ICHI_DEFENSE` 음수)" → **몬스터 방어력 디버프**(`STAT_DEFENSE` 음수, 수치·지속은 기존 BalanceDB `ERIS_BREAK_*`/`ERIS_ENDURE_*`). SkillDB 설명문과 `RPG_CONVERSION_SKILLS.md` 구현 줄을 고친다.
      전수 점검 목록 (R8):
      - `WacherA` 방어 저하(플레이어 대상 `ICHI_DEFENSE` 음수 → 실드 획득량 감소) → `STAT_DEFENSE` 음수 **3턴** (Q31 — 방어력과 실드는 다른 개념)
      - `SpearManB` 방어 행동의 `ICHI_DEFENSE` 가산 → `STAT_DEFENSE` 버프 합산으로
      - `TargetObject.GainDefense`의 `ICHI_DEFENSE` 음수 차감 — 플레이어 실드 획득량 감소 경로는 Q31로 쓰이지 않게 되므로 제거 (`GainDefense` 자체는 1-10 `GainShield`로 대체)
      - 게오르크 빈틈없는 자세(GS14)·찌르고 막기(GS15)·고통 수집(GS16)의 `ICHI_DEFENSE` 양수 → `STAT_DEFENSE` 양수로 옮기면 1-2 `GetDefense`가 자동 가산 (버프 개편과 겹치면 개편 쪽에서)
      - `ICHI_ATTACK`(Devourer·Saddy·SpearManB·WacherB·기도·철귀·쇠락)은 **버프 개편 범위 — 이 Phase에서 건드리지 않는다**
- [x] 2-5. 수치 초안 — `MonsterStatDB`에 몬스터별 `Defense/MagicDefense`를 채운다 (방패병·거인병·수호자는 높게, 감시자·치유사는 마법방어 위주). 기존 `WacherB` 등의 실드 수치를 참고해 1스테이지 전투 길이가 크게 변하지 않게 맞춘다
- [x] 2-6. 표시 — 몬스터 이름표/디버그에 방어력·마법방어 표시 (실드 게이지는 몬스터에게 더 이상 뜨지 않음)

**완료 기준**
- 방패병을 공격하면 방어 공식으로 경감된 피해가 들어가고, 치명타는 방어를 50% − 10으로 계산한다
- 몬스터에게 실드가 생기지 않는다. 방어 행동을 한 몬스터는 3턴(자기 턴 종료 차감) 동안 방어력이 오른다
- 부서지세요가 몬스터 방어력을 낮춰 이후 피해가 늘어난다
- 감시자A의 방어 저하를 받은 플레이어는 3턴 동안 방어 공식의 방어력이 낮아진다 (실드량은 영향 없음)
- 플레이어의 방어 행동(방어력 버프)·수호 축복 실드는 그대로 작동한다 (Q40·Q41)

**구현 (2026-10-19)** — 컴파일 확인. **인게임 플레이 확인은 아직** (Phase 1과 함께 1스테이지 전투로 확인).
- `MonsterStatDB.csv` 헤더 교체: `TPShield` 삭제, `Evade/Accuracy/CritRate/Defense/MagicDefense/HazardMdef` 추가 (DropTable은 Phase 7). 방어 수치는 초안 — 방패병 15/5, 수호자 20/8, 거인병 18/4, 감시자 3~5/10~12, 보스 20~30/15~25
- `SpawnedMonster.ScaledDefense()/ScaledMagicDefense()` = 스탯 + Hazard × 위험도 (구 `ScaledDefense(int)` 폐기). 원격 클라이언트는 `Data`(monsterName → MonsterDB)로 조회. `TargetObject.GetDefense/GetMagicDefense`가 몬스터 분기로 사용, 0 하한
- `DamageToMonster`·`StaticDamageToMonster`에서 실드(`defense`) 분기 삭제, `TargetObject.GainDefense` 삭제 (유물 `ItemMethods`는 `GainShield`로 임시 연결 — 3-1에서 삭제)
- 실드 몬스터 7종 → `SpawnedMonster.GainDefenseBuff(target, amount)` = `STAT_DEFENSE` +amount 3턴 (출처 `{몬스터명}_DEFEND`). 거인병 10/15/20은 BalanceDB `GIANTSOLDIER_DEFENSE_UP_BASE/PER_STEP`. 창잡이B의 누적 +2는 `STAT_DEFENSE` 영구 버프
- 2-4: 부서지세요·얼마나 버틸까요 → `STAT_DEFENSE` 음수(출처 = 스킬 번호), 감시자A 방어감소 → `STAT_DEFENSE` 음수 3턴, 빈틈없는 자세·찌르고 막기·고통 수집 → `STAT_DEFENSE` 양수(출처 = 스킬 번호). 플레이어 `GetDefense`의 `ICHI_DEFENSE` 가산 제거. `ICHI_ATTACK`은 미변경
- 2-6: 전투 OnGUI TP 라벨에 몬스터 `방 x / 마방 y` 표시. 몬스터 실드 게이지는 더 이상 변하지 않는다 (`OnBreakedShield` 경로는 남아 있으나 호출되지 않음)

※ 몬스터 행동 정식 리워크(실드 몬스터 7종의 새 패턴)는 몬스터 기획이 나온 뒤 별도 작업이다. 이 Phase는 게임이 돌아가는 임시 상태까지다.

---

## Phase 3 — 장비 인스턴스·옵션 DB·무기 개편

**목표**: 장비가 등급과 랜덤 옵션을 가진 인스턴스로 생성·보유·저장된다. 누구나 어떤 무기든 착용할 수 있다. 아직 옵션 효과는 스탯 합산에만 반영한다 (일반 옵션).

- [x] 3-1. **구 유물 시스템 삭제** (Q2, Q21) — 순서: `GamePlayer.prefab`에서 `GamePlayerItem` 컴포넌트를 MCP로 먼저 제거 → `GamePlayerItem.cs`·`Item/Item.cs·ItemMethods.cs·ArtifactMethods.cs`·`DB/ItemData.cs`(씬 오브젝트 확인)·`ItemDB/ArtifactDB.csv`·`M_TurnManager.teamArtifacts/AddTeamArtifact`·`BattleInitialize`의 유물 발동·`ItemEffectTime/ItemType/ItemGrade` enum 삭제. `RewardListItem`의 `Reward_Type.Item` 분기는 지우지 않고 "장비 표시 전용"으로 비워 두었다가 7-5에서 채운다 (R12). 콘솔에서 Missing Script·RPC 해시 확인
- [x] 3-2. `EquipGrade`, `EquipOptionType`, `WeaponType`, `ArmorType` enum 추가 (`Common/ProjectD.cs`) — 0-1, 0-2, 0-5
- [x] 3-3. `EquipGradeDB.csv` + `DB/EquipGradeData.cs`, `EquipOptionDB.csv` + `DB/EquipOptionData.cs` (BalanceData 패턴). `GetRollable(slot, grade, isLegend)` — 슬롯·등급·전설 여부로 필터
- [x] 3-4. `EquipDB.csv` 개편 (0-4) + `EquipData.Def` 정리. 기존 16행을 새 컬럼으로 이관한다. 무기 종류 6종마다 최소 1행, 방어구는 3종류 × 3부위 = 최소 9행 (Phase 4에서 `ArmorType`·`RequireStr` 채움). AC3 숙련의 부적은 다른 악세사리로 교체 (Q1)
- [x] 3-5. 착용 제한 제거 — `CmdEquip`의 캐릭터 전용 검증, `EquipData.GetUsableBy`, OnGUI 착용 가능 표시, `ServerAddRandomEquip` 드랍 풀의 캐릭터 조건 제거
- [x] 3-6. 무기 공격력·속성 (Q22, Q25) — **힘과 무기 공격력은 다른 개념.** `TotalStrength/TotalIntelligence`는 스탯(기본 + `STRENGTH/INTELLIGENCE` 옵션)만 합산하고, 무기 공격력은 `TotalPhysicalAttack = TotalStrength + Σ 무기 PhysicalAttack`, `TotalMagicAttack = TotalIntelligence + Σ 무기 MagicAttack`으로 따로 둔다 (캐릭터와 무관, 모든 무기가 두 값을 모두 가짐). `EquipAttackBonusFor`는 삭제.
      공격력 조회는 한 함수 **`GamePlayer.GetAttackPower(bool scalesWithInt, AttackAttribute attribute)` = (scalesWithInt ? `TotalIntelligence` : `TotalStrength`) + (`IsMagic(attribute)` ? Σ무기 `MagicAttack` : Σ무기 `PhysicalAttack`)** (Q46. 속성 `NONE`은 scalesWithInt 쪽 — INT면 마법공격력, STR이면 물리공격력. **스킬에도 적용 — J1 확정**). `BasicAttackDamage`는 **장착 무기의 `ScaleStat`·`Attribute`**(무기가 없으면 STR·NONE)로, `BattleActions.SkillDamage`는 스킬의 `scalesWithInt`·`attribute`로 이 함수를 부른다. 예: 홍단향이 대검(참격·STR)을 들면 평타는 힘 + 대검 물리공격력의 참격, 화염구는 지능 + 대검 마법공격력의 이치. **에리스 핵(공명·STR)은 평타·공명 스킬 모두 힘 + 핵 마법공격력**이고 마법방어로 경감된다 (I1). `TotalPhysicalAttack/TotalMagicAttack`은 스탯창 표시용. TpBattle `TpAction.ATTACK` 분기의 `stat.basicAttack` 참조를 무기 속성으로 교체. `PlayerOrder`·`DebugStats` 표시도 공격력/스탯을 구분
- [x] 3-7. `EquipInstance` 구조체 + `EquipGenerator`(서버 전용 static) — `Create(equipNo, grade)`: `EquipGradeDB`의 옵션 개수를 굴리고, 해당 등급 행에서 Weight 가중 무중복 추출 후 Min~Max를 굴린다. 전설은 마지막 1개를 전설 옵션 풀에서. `ValueKind=ATTRIBUTE`는 속성을 굴린다 (A6). 악세사리는 악세사리 옵션 개수 컬럼 사용
- [x] 3-8. `GamePlayer.Equipment.cs` 전환 — `equippedItems / inventoryEquips`를 `SyncList<EquipInstance>`로. `CmdEquip/CmdUnequip`은 `instanceId` 기준. `SumEquip`이 기본 옵션(EquipDB)과 인스턴스 옵션을 함께 합산하고 `EquipOptionSum(type)` / `EquipOptionAttributes(type)`(속성형 옵션 목록) 조회 함수를 둔다
      - `Total*`에 옵션 가산: 힘·지능·민첩·제어·방어
      - `TotalControl` 신설 — `BattleActions.GainRageByDamage`와 TpBattle MP 회복(턴 시작·전투 종료)이 `control` 대신 사용
      - `MAX_HP/MAX_MP/MAX_RAGE`는 **계산 프로퍼티** (Q43 — `ApplyEquipMaxDeltas` 델타 방식 폐기): `MaxHP/maxResource`는 캐릭터 고유치(성장·노드 포함)만 저장하고 `TotalMaxHP = MaxHP + Σ MAX_HP`, `TotalMaxResource = maxResource + Σ(자원 종류가 맞는 MAX_MP/MAX_RAGE)` 프로퍼티를 신설. `TargetObject.playerMaxHP`·`PlayerOrder`·`DebugStats`·류진솔 회복·캠프 회복·`AddMaxHP`의 HP 클램프가 `Total*`을 쓴다. 착탈 시 현재 HP/자원은 `Total*`로 클램프만 한다. **`MAX_MP`는 자원이 MP인 캐릭터에게만** (D25)
- [x] 3-9. Q1 반영 — `EquipSkillLevelBonus` 삭제, `GamePlayer.SkillTree.GetSkillLevel`에서 장비 가산 제거, `BattleActions.SkillDamage` 주석 수정
- [x] 3-10. 초기 지급 `ServerGrantInitialGear` (Q36) — 무기 + 방어구 3부위 세트(A5)를 NORMAL 인스턴스로 생성·장착. 캐릭터별 기본 무기는 무기 종류 기준으로 다시 지정 (게오르크 대검 / 홍단향 지팡이 / 에리스 핵). 초기 세트의 `RequireStr`은 **캐릭터 기본 힘(원래 밸런스) 이하**로 두어 패널티가 없게 하고, 성능은 최하로. 게오르크의 원래 기본 힘은 CharacterStatDB 주석대로 **8**(현재 CSV 10은 테스트 상향)이므로 초기 중갑 세트(AR2·HE2·BO3)는 `RequireStr` 8 (R15)
- [x] 3-11. 세이브 — `GameSaveService.ProfileData`의 `equippedItems/inventoryEquips`를 `List<EquipInstanceSave>`로 (instanceId·equipNo·grade·옵션 배열). **`RpgSaveData.saveVersion` 추가** (R11) — 구 세이브(필드 없음 = 0)는 로드 시 1회 변환하고 현재 버전으로 저장한다.
      **구 세이브 호환 (버전 0 → 1)**: 문자열 항목 → NORMAL 옵션 0개 인스턴스, 삭제·번호가 바뀐 행 → 대체 장비 매핑 (**AC3 → AC4, 구 AR2 철 갑옷 → AR3 강철 갑옷, 구 HE2 현자의 관 → HE3** — 새 AR2·HE2는 다른 장비다, R13), 방어구 부위가 비어 있으면 기본 세트 보충 (A5), 구 EquipDB의 `MaxHP/MaxResource` 델타를 저장된 최대치에서 1회 차감 (Q43 — 장착 중이던 구 장비의 값만. 구 EquipDB 16행의 `MaxHP/MaxResource`는 변환기에 상수표로 남긴다)
- [x] 3-12. 임시 OnGUI 창(`DrawEquipmentGUI`) — 등급 색·무기/방어구 종류·옵션 목록 표시 (정식 UI는 Phase 8). 디버그 치트: 등급·옵션 지정 장비 생성

**완료 기준**
- 치트로 만든 전설 장비가 옵션 3개를 갖고, 착용하면 스탯창 수치가 오른다. 같은 옵션이라도 등급에 따라 수치 범위가 다르다
- 저장·이어하기 후에도 옵션이 그대로다
- 홍단향이 대검을 착용할 수 있고, 대검의 물리·마법 공격력이 각각 `TotalPhysicalAttack/TotalMagicAttack`에 더해진다. 힘·지능 스탯 자체는 변하지 않는다. 기본 공격 속성이 대검의 속성으로 바뀐다
- 에리스의 평타와 공명 스킬이 힘 + 핵 마법공격력으로 계산되고 마법방어로 경감된다. 핵을 벗으면 힘만으로 친다. 홍단향 화염구는 지능 + 지팡이 마법공격력 (Q46)
- 게오르크가 MP 옵션 장비를 껴도 분노 최대치는 100 그대로다
- 멀티 클라이언트가 호스트 캐릭터의 장비 옵션을 같은 값으로 본다
- 유물 관련 코드·데이터가 남아 있지 않고 콘솔에 Missing Script가 없다

**구현 (2026-10-19)** — 컴파일·정적 검증 완료 (EquipDB 25행 로드, `EquipGenerator` 전설 = 일반 2 + 전설 1, 유물 타입 제거). **인게임 플레이 확인은 아직** (착탈·저장·이어하기·멀티 동기화).
- 유물 삭제: `GamePlayer.prefab`의 `GamePlayerItem` 컴포넌트와 `MenuScene`의 `ItemData` 오브젝트를 에디터 스크립트로 제거 후 `Item/*`·`GamePlayerItem.cs`·`ItemData.cs`·`ItemDB/ArtifactDB.csv` 삭제, `M_TurnManager.teamArtifacts/AddTeamArtifact`·`BattleInitialize` 유물 발동·`ItemEffectTime/ItemType/ItemGrade`·`ItemEventHanddler` 삭제. `Reward_Type.Item` 분기는 유지(7-5). ※ 추적 중인 복구 씬 `Assets/_Recovery/0.unity`에는 `ItemData` 참조(Missing Script)가 남아 있다 — 복구용 씬이라 손대지 않음
- 신규: `DB/EquipInstance.cs`(struct + `EquipInstanceSave`), `DB/EquipGradeData.cs`(`RollGrade(처치 등급, 위험도)`), `DB/EquipOptionData.cs`(`GetRollable`, 표시 `Format`), `DB/EquipGenerator.cs`, `EquipGradeDB.csv`, `EquipOptionDB.csv`(옵션 × 등급 46행, 약점 상쇄 풀은 4-1에서 ICHI/SIMSANG으로 교체), `EquipDB.csv`(0-4 헤더 + `ScaleStat`, ITEMS §4 초안 26행)
- `GamePlayer.Equipment.cs` 재작성: `SyncList<EquipInstance>`, `Total*` + `EquipOptionSum/HasEquipFlag/EquipOptionAttributes`, `TotalPhysicalAttack/TotalMagicAttack`, **`GetAttackPower(scalesWithInt, attribute)`**(Q46·J1), `WeaponDef/BasicAttackAttribute/BasicAttackScalesWithInt`, `TotalMaxHP/TotalMaxResource`(Q43 — `ApplyEquipMaxDeltas` 폐기, 착탈 시 `ClampToTotals`), `TotalControl`, `CmdEquip/CmdUnequip(instanceId)`, `ServerGrantInitialGear`(초기 세트 4부위), `ServerFillMissingArmor`, 치트 `CmdDebugCreateEquip`
- `BattleActions.BasicAttackDamage/SkillDamage`·치유의 빛·수호 축복·만개·반격·TP 루프 ATTACK·`PlayerOrder`가 무기 기준(`GetAttackPower`·`BasicAttackAttribute`)으로. `CharacterStatDB`의 `AttackStat/BasicAttackAttribute`는 코드에서 더 이상 읽지 않음 (컬럼 삭제는 4-1·4-4)
- `MaxHP/maxResource` 소비처(`BattleSpawner`·류진솔/캠프 회복·`AddMaxHP` 클램프·MP 회복·분노 클램프·OnGUI)를 `TotalMaxHP/TotalMaxResource`로. 분노·MP 회복은 `TotalControl`
- 세이브: `RpgSaveData.saveVersion`(현재 1), `ProfileData.equippedInstances/inventoryInstances`. 버전 0 로드 시 `ApplyProfile`에서 1회 변환 — 구 델타 차감표(`LegacyMaxDelta`), 번호 매핑(`MapLegacyEquipNo`: AC3→AC4·AR2→AR3·HE2→HE3), 방어구 보충. PO3·골드 변환은 6-4·7-10

---

## Phase 4 — 속성 분류·방어구 약점·힘 요구치

**목표**: 속성이 물리 3종(참격·타격·관통)과 마법 3종(이치·심상·공명)으로 나뉘고, 마법 속성 공격은 모두 마법방어로 경감된다. 입은 방어구 세트가 플레이어의 약점을 정하고, 몬스터가 약점을 치면 몬스터와 같은 공식으로 피해 120%·TP 감소·치명 보정이 들어간다. 힘이 부족한 방어구는 민첩을 반으로 깎는다.

- [x] 4-1. `AttackAttribute` 개편 (0-7, Q15) — `MAGIC` → `ICHI`, `SIMSANG` 추가. CSV 이관: SkillDB `Attribute`(HS1 화염구·HS3 마력 폭풍 등), MonsterStatDB `Weakness/AttackAttribute`(감시자·정예·보스 등 `MAGIC` 행 전부), EquipDB 무기 `Attribute`(Q25 — 지팡이·책·수정구는 ICHI/SIMSANG, 핵은 RESONANCE 등 무기마다 지정). `BattleActions.AttributeName` 표기와 로컬라이즈 키 `attr.<enum>` 추가
- [x] 4-2. `BattleActions.IsMagic(AttackAttribute)` 갱신 — 1-2에서 만든 헬퍼의 판정을 `ICHI·SIMSANG·RESONANCE`로 바꾼다. 방어 스탯 선택이 `IsMagic`을 쓰는지 양쪽(`DamageToPlayer`, `DamageToMonster`) 확인. 공명 공격(Boss_Geras 등)이 마법방어로 경감되는지 확인
- [x] 4-3. `ArmorTypeDB.csv` + 로더 (0-6). `EquipDB` 갑옷·투구·신발 행에 `ArmorType`·`RequireStr`을 채운다 (중갑 높음 / 경갑 낮음 / 로브 0)
- [x] 4-4. `GamePlayer.GetWeaknesses()` (Q16) — 갑옷·투구·신발 3부위의 `ArmorType`을 모아 세트 규칙(0-6)으로 약점 집합을 만든다: 미착용 부위 있음 → 전 속성 / 1종 → 그 종류 약점 / 2종 → 합집합 / 3종 → 전 속성. 그다음 약점 상쇄 옵션의 속성을 뺀다 (Phase 5-12). 착탈 시 갱신(SyncVar 또는 계산 프로퍼티).
      `CharacterStatDB` `Weakness/Resist` 컬럼과 `CharacterStatData.weakness/resist` 삭제 (Q17)
- [x] 4-5. 힘 요구치 패널티 (Q42) — 착용한 방어구 중 `RequireStr` > **현재 힘(`TotalStrength` = 기본 힘 + `STRENGTH` 옵션 + `STAT_STRENGTH` 버프. 무기 공격력·공격력 버프 제외)** 인 부위가 하나라도 있으면 `TotalAgility`에 `ARMOR_STR_PENALTY_AGI_PERCENT`(50%) 적용 (1회). 힘 버프로 요구치를 넘으면 그동안 패널티가 풀린다 — 비전투에서는 `GamePlayer`만 보고, 전투 중에는 `TargetObject`의 `STAT_STRENGTH` 버프를 더해 계산하는 조회 함수(`CurrentStrengthForRequirement`)를 둔다. TP 충전(`GetUnitTpGain`)이 매 틱 `TotalAgility`를 읽으므로 자동 반영 — 확인만
- [x] 4-6. 플레이어 약점 피격 (Q18, Q30) — `ResolveHit`의 약점 판정이 대상이 플레이어면 `GetWeaknesses()`를 본다. 피해 120%·`ApplyWeaknessTpDamage`(플레이어 TP, 반복 체감 포함)·몬스터 치명 +`WEAKNESS_CRIT_BONUS` 모두 몬스터 피격과 같은 코드·같은 BalanceDB 키
- [x] 4-7. 표시 — 전투 OnGUI 약점 힌트에 플레이어 현재 약점(세트 상태 포함), 스탯창에 힘 부족 패널티 표시. 몬스터 행동 예고에 공격 속성 표시(구 워크플로우 2C-3)를 함께 진행 권장 — 약점 회피를 위한 장비·대열 선택의 근거가 된다 (`NextActionIndicator` 확장, 데이터는 `MonsterStatDB.AttackAttribute`)

**완료 기준**
- 중갑 3부위 착용 캐릭터가 공명 공격을 맞으면 마법방어로 경감되고, 피해 120%·TP 감소가 들어간다. 참격 공격에는 약점이 없다
- 투구를 벗으면 모든 속성이 약점이 된다. 중갑 2 + 로브 1이면 타격·공명·참격·이치가 약점이다
- 힘이 부족한 캐릭터가 중갑을 입으면 민첩이 반으로 줄고 TP 충전이 느려진다

**구현 (2026-10-19)** — 컴파일 확인. **인게임 플레이 확인은 아직.**
- `AttackAttribute { NONE, SLASH, STRIKE, PIERCE, ICHI, SIMSANG, RESONANCE }` — CSV 이관: SkillDB HS1·HS3, MonsterStatDB 10행(ITEMS §1 표 그대로), EquipDB(지팡이 ICHI, 책·수정구 SIMSANG), EquipOptionDB 약점 상쇄 풀. 프리팹의 직렬화 값은 NPC `attackAttribute: 0`(NONE)뿐이라 enum 순서 변경 영향 없음
- `CharacterStatDB` `Weakness/Resist/AttackStat/BasicAttackAttribute` 컬럼·필드 삭제 (Description 앞에 Base* 보조 스탯 컬럼 재배치)
- `BattleActions.IsMagic` = 이치·심상·공명. `AttributeName`은 로컬라이즈 `attr.<enum>`(ko·en), `AttributeListName` 추가
- `ArmorTypeDB.csv` + `DB/ArmorTypeData.cs`(`GetWeaknesses(type)`, `AllAttributes`). `GamePlayer.GetWeaknesses()` 세트 규칙 + 약점 상쇄 차감, `IsWeakTo`가 플레이어 대상에 사용 → 피해 120%·약점 TP(반복 체감)·치명 +10%p가 몬스터와 같은 코드
- 힘 요구치: `CurrentStrengthForRequirement()`(= TotalStrength + 전투 중 STAT_STRENGTH 버프), `IsStrengthRequirementUnmet`, `TotalAgility`에 `ARMOR_STR_PENALTY_AGI_PERCENT`(50) 1회 적용 — TP 충전(`GetUnitTpGain`)이 매 틱 읽는다
- 표시: 전투 상태줄에 행동 캐릭터 약점·힘 부족, 몬스터 TP 라벨에 공격 속성, 장비 창 헤더·스탯 창에 약점/요구치
- 기존 `MAGIC` 속성이 `ICHI`로 이관되어 몬스터 약점 판정이 이전과 같다 (회귀 없음)

---

## Phase 5 — 옵션 효과 배선 (전설 옵션 14종 + 보호막)

**목표**: 기획서 전설 옵션 14종과 보호막 옵션이 실제 전투에 작동한다. 각 옵션은 `GamePlayer.EquipOptionSum(type)`(속성형은 `EquipOptionAttributes`)으로 조회하고, 적용 지점만 다르다.

| 옵션 | 적용 지점 | 항목 |
|---|---|---|
| 치명타 확률 / 명중률 / 회피율 | 1-2 공용 스탯 조회에 가산 | [x] 5-1 |
| 치명타 데미지 | 1-2 `GetCritDamage`에 가산 | [x] 5-2 |
| 생명력 흡수 | `ResolveHit` — HP에 실제로 들어간 피해(1-3 피격 함수 반환값) × % → 공격자 `Heal` (치명 회복 판정 포함) | [x] 5-3 |
| 스킬 추가 데미지 | `HitContext.bonusPercent` (스킬 타격만, 기본 공격 제외) | [x] 5-4 |
| 콤보 추가 데미지 | `HitContext`에 콤보 보너스로 합산해 두고, **콤보 배율(`powerMultiplier`)이 1.0을 넘을 때만** 적용 — 연계 구현 전에는 효과 없음 (Q10). 스탯창 표시 | [x] 5-5 |
| 반사 데미지 (고정 / 비례) | `TargetObject.DamageToPlayer` — HP 피해 후 `tpActingUnit` 몬스터에 고정치 + 받은 피해 × %를 **별도 `HitContext`로 `ResolveHit`** (명중·치명·r 2차 판정, Q27). 반격(GS13)도 같은 경로로 옮기고 합산 | [x] 5-6 |
| 받는 회복량 증가 | `Heal`/`GamePlayer.ServerHeal` — 대상 옵션 합으로 배율 (치명 회복 배율과 곱). **비전투 회복(물약·캠프·류진솔)에도 적용** (Q38) | [x] 5-7 |
| 분노 최대치 증가 | `TotalMaxResource` (3-8, Q43) — 자원이 RAGE인 캐릭터만 | [x] 5-8 |
| 보호막 (갑옷·악세사리 추가 옵션) | 전투 시작(`BattleInitialize`) 시 플레이어 `GainShield(옵션 합)` — 보호막 옵션이 여럿이면 합산한 값으로 1회 부여(5-13). 실드는 소진까지 지속하므로(Q37, 1-14) 첫 자기 턴에 사라지지 않지만, 전투 중 수호 축복 등 새 실드가 들어오면 덮어쓰인다 (Q41) | [x] 5-9 |
| 포지션 패널티 제거 | `BattleActions.RowAttackPercent`(후열 80 → 100) / `RowIncomingDamagePercent`(전열 120 → 100) — 보너스 유지 (Q8) | [x] 5-10 |
| 이동 시 턴 소모 제거 | `M_TurnManager.ExecutePlayerTpTurn` `TpAction.MOVE` — 옵션 보유 시 `freeAction = true`, 턴당 1회. 턴 미소모 스킬 플래그 `tpUsedFreeSkillNo`(철귀 이동)와 별도로 `tpUsedFreeMove`(bool SyncVar)를 신설. 중열 무료 이동(기획)도 같은 분기 (Q9) | [x] 5-11 |
| 약점 상쇄 (속성 지정) | `GetWeaknesses()`에서 옵션이 지정한 속성을 뺀다 (Q23, A6) | [x] 5-12 |

- [x] 5-13. 옵션 스택 규칙 — 수치형은 합산, 플래그형(`FREE_MOVE/NO_ROW_PENALTY`)은 OR, 속성형(`WEAKNESS_NULLIFY`)은 속성 합집합. 퍼센트 옵션 상한은 BalanceDB(`EVADE_CAP` 등)로 두되 1차는 무제한
- [x] 5-14. 검증용 치트 — 특정 옵션 하나만 붙은 전설 장비 생성 (옵션 타입·속성 지정)

**완료 기준**: 옵션 14종 + 보호막 각각을 치트 장비로 붙여 전투 로그에서 효과를 확인한다 (콤보 추가 데미지는 스탯 표시만). 옵션 없는 장비의 전투 결과는 Phase 4와 같다 (회귀 없음).

**구현 (2026-10-19)** — 컴파일 확인. **인게임 확인은 아직** (장비 창의 "옵션 1개짜리 전설 악세사리" 치트로 옵션마다 확인).
- 5-1·5-2: `GamePlayer.TotalEvade/TotalAccuracy/TotalCritRate/TotalCritDamage`에 옵션 가산. 5-7: `HealReceivedPercent` — `TargetObject.Heal`·`ServerHeal`(비전투) 양쪽
- 5-3·5-4·5-5: `ResolveHit`에서 생명력 흡수(HP 피해 × % → 공격자 `Heal`), 스킬 추가 데미지(`isSkill`), 콤보 추가 데미지(`powerMultiplier > 1`일 때만)
- 5-6: `CounterAttack(hpDamageTaken)` = 반격 + 반사 고정 + 받은 피해 × %를 한 `HitContext`(2차 판정)로
- 5-9: TP 루프 시작에서 `GainShield(SHIELD 옵션 합)` 1회. 5-10: `RowAttackPercent`(후열)·`RowIncomingDamagePercent`(전열)에서 `NO_ROW_PENALTY`면 100
- 5-11: `tpUsedFreeMove` SyncVar, MOVE 분기에서 `FREE_MOVE` 옵션 **또는 중열 출발**(기획 "중열: 위치 변환 코스트 삭제", Q9)이면 턴 미소모 (턴당 1회). OnGUI 이동 버튼에 `*` 표시
- 5-14: `CmdDebugCreateOptionEquip(optionType, value)` — 옵션 하나만 붙은 전설 악세사리 (OnGUI 22개 버튼). 스탯 창에 옵션 요약 줄

---

## Phase 6 — 소모품 구조화·물약

**목표**: 소모품이 "CSV 행 + 효과 메서드"로 추가되는 구조가 되고, 1차 물약 6종(HP·MP·분노·공격력·치명타·회피)이 전투와 비전투(거점·미로)에서 작동한다. 대상은 1차에 자신뿐이지만, 효과 코드는 몬스터 대상에도 그대로 쓸 수 있다.

- [x] 6-1. `ConsumableData` 개편 (0-9) — `Def`에 `effect, validTarget, duration, battleOnly, usableResource, dropWeight`. `ConsumableType` enum 삭제. 로드 시 `Effect` 이름으로 `ConsumableEffects`의 정적 메서드를 리플렉션 바인딩하고, 없으면 에러 로그 (SkillData와 같은 방식)
- [x] 6-2. **`ConsumableEffects`** (Q5, Q6, Q38, R1) — `IEnumerator <Effect>(ConsumableDef def, GamePlayer user, GamePlayer target, TargetObject targetUnit)`. `targetUnit`은 전투 중에만 채워지고(비전투는 null), 추후 몬스터 대상은 `target = null, targetUnit = 몬스터`로 넘긴다. 효과는 대상이 플레이어인지 몬스터인지 가정하지 않고 1-2·1-10의 공용 함수(`GamePlayer.ServerHeal` / `TargetObject.Heal`, 스탯 버프, `DamageTpTo` 등)만 쓴다. 1차 효과:
      - `HEAL_HP`: `ServerHeal` 경유 → 치명 회복·받는 회복량 옵션이 전투·비전투 모두 적용 (Q38)
      - `RESTORE_MP` / `RESTORE_RAGE`: 자원 종류가 맞는 대상만 (`BattleActions.GainRage` 등)
      - `BUFF_ATTACK / BUFF_CRIT_RATE / BUFF_EVADE`: `STAT_ATTACK / STAT_CRIT_RATE / STAT_EVADE` 지속 버프 (0-13 — `ICHI_ATTACK` 미사용, Q15). 전투 전용(`targetUnit` 필요). 지속은 **사용 턴을 제외한 n턴** (Q33 — `ApplyTimedDebuffTo`의 `skipFirstTick` 그대로)
- [x] 6-3. 사용 로직 통합 — `GamePlayer.ServerUseConsumable(potionNo, GamePlayer target)` 한 곳으로 모은다. 검증: 보유·`BattleOnly`·`ValidTarget`(1차 `NONE` = 자신만)·`UsableResource`. 전투 중이면 `M_TurnManager.GetCurrentPlayerTargetObject(target)`로 `targetUnit`을 구해 넘긴다. 전투 ITEM 분기(TpBattle)와 `CmdUsePotionOnMap`의 중복 switch를 이 함수 호출로 교체. 비전투 HP 회복도 같은 규칙
- [x] 6-4. `ConsumableDB.csv` 1차 행 — HP 물약·상급 HP 물약·MP 물약·분노의 물약·공격력 물약·치명타 물약·회피율 물약. PO3(구 `RESTORE_RESOURCE`, MP·분노 공용)는 `RESTORE_MP`로 이관. 구 세이브의 PO3은 소지 캐릭터의 자원 종류에 따라 **MP → PO3 / RAGE → PO4 / HP(에리스) → PO1**로 변환 (R13 — 그대로 두면 게오르크·에리스가 쓸 수 없는 물약이 남는다)
- [x] 6-5. 비전투 사용 제한 — `BattleOnly=1`은 거부 (토스트 `ui.msg.potion_battle_only`)
- [x] 6-6. 초기 지급 — HP 물약 2개 유지, 게오르크에게 분노의 물약 1개 시험 지급 (테스트 후 결정)
- [x] 6-7. 전투 UI(OnGUI 아이템 목록) — 사용할 수 없는 물약은 비활성 + 사유 표시 (자원 불일치·비전투 전용 등)

**완료 기준**
- 치명타 물약을 쓰면 사용 턴 다음부터 n턴 동안 치명률 상승이 스탯창에 보이고, 만료되면 원복된다
- 비전투에서는 버프 물약이 사용되지 않는다. 에리스는 MP 물약을 쓸 수 없다
- 거점에서 HP 물약을 쓰면 받는 회복량 옵션·치명 회복이 적용된다
- 새 효과를 추가하는 절차가 "CSV 행 + `ConsumableEffects` 메서드 1개"뿐임을 확인한다 (테스트용 더미 효과로 검증 후 삭제)

**구현 (2026-10-19)** — 컴파일·바인딩 확인. **인게임 확인은 아직.**
- `ConsumableData` 재작성 (0-9 헤더, `ExecuteConsumable` 리플렉션 바인딩, `CanUse(def, user, inBattle, out reasonKey)`·`ReasonText`·`PickWeighted`), `ConsumableType` enum 삭제. 신규 `DB/ConsumableEffects.cs` — `HEAL_HP`(ServerHeal)·`RESTORE_MP`·`RESTORE_RAGE`·`BUFF_ATTACK/BUFF_CRIT_RATE/BUFF_EVADE`(`ApplyTimedStatBuff`, 출처 = 물약 번호)
- `GamePlayer.ServerUseConsumable(potionNo, target)` 단일 경로 (보유·BattleOnly·UsableResource·NONE = 자신). TpBattle ITEM 분기는 이것을 yield, `CmdUsePotionOnMap`은 코루틴 시작. 거부는 `TargetPotionRefused` 토스트 (`ui.msg.potion_battle_only` / `ui.msg.potion_resource_mismatch`)
- `ConsumableDB.csv` PO1~PO7 (ITEMS §7 초안). 초기 지급 PO1×2 + 게오르크 PO4×1. 세이브 버전 0의 PO3 → 자원별 PO3/PO4/PO1 (`MapLegacyPotion`)
- OnGUI(전투 아이템 목록·장비 창 소모품)에서 사용 불가 물약 비활성 + 사유

---

## Phase 7 — 몬스터별 드랍 테이블·메르크리우스 상점

**목표**: 몬스터마다 다른 드랍 테이블로 장비·물약이 떨어지고, 등급은 `EquipGradeDB`로 굴린다. 멀티에서는 드랍 확률이 1.5배다. 메르크리우스에서 기본 장비를 사고, 인벤토리 장비를 판다. 골드는 플레이어(접속자) 단위로 공유한다.

- [x] 7-1. 처치 기록 — `M_TurnManager`에 이번 전투에서 처치한 몬스터 목록(`battleKilledMonsters`)을 `battleExpPool` 적립 지점에 함께 기록. 전투 시작 시 초기화 (`M_TurnManager.Spawner`)
- [x] 7-2. `DropTableDB.csv` + `DB/DropTableData.cs` (0-12), `MonsterStatDB.DropTable` 로드
- [x] 7-3. 드랍 판정 (Q39, Q34) — `RewardService.DistributeBattleRewards`가 처치 몬스터마다 테이블 항목을 굴린다. **굴림 단위: 싱글 = 파티 1회, 멀티 = 캐릭터(`GamePlayer`)마다** (Q39, F6 — 캐릭터 2개를 쓰는 플레이어는 2번 굴린다). 확률 = `Chance` × (멀티면 `ITEM_DROP_MULTIPLAYER_PERCENT`/100). 100 이하 확정분 + 넘는 몫은 추가 1회 판정 (Q24). `MinHazard` 미만 항목은 건너뜀. 드랍 장비의 수령 캐릭터: 싱글은 선택 중인 캐릭터(`currentGamePlayer`), 멀티는 굴린 캐릭터의 인벤토리 (착용 제한이 없으므로 누구 인벤토리든 쓸 수 있다). 기존 `EQUIP_DROP_PERCENT/POTION_DROP_PERCENT/EQUIP_DROP_MIN_HAZARD` 경로 삭제
- [x] 7-4. 등급 굴림 `EquipGenerator.RollGrade(monsterGrade, hazard)` — `EquipGradeDB`의 `DropWeight` + 엘리트/보스 보너스 가중치, `MinHazard` 미만 등급 제외 (위험도 하한 보상). 풀 지정자는 `DropWeight` 가중 추출. 요구 레벨 필터(현 `level + 2`)는 **폐지** (Q34, F3 — 테이블·위험도로만 제어)
- [x] 7-5. 보상 목록 표시 — `Reward_Type`에 `Equip/Potion` 추가(이미 지급된 항목은 수령 버튼 없는 표시 전용), `BattleResultPopUp`/`RewardListItem`에 이름·등급 색 표시 (현재는 `Debug.Log`만)
- [x] 7-6. 드랍 테이블 초안 — 일반 몬스터(물약 위주 + 낮은 확률 장비), 엘리트(장비 확정 1 + 등급 보너스), 보스(장비 150% 등). 방패병은 방어구, 감시자는 지팡이류처럼 몬스터 특색을 준다
- [x] 7-7. `ShopDB.csv` + 로더 (0-14, A8)
- [x] 7-8. `M_HubManager.CmdShopBuy(gamePlayerNetId, shopId, itemNo)` / `CmdShopSell(gamePlayerNetId, instanceId)` — `CmdReduceHazard` 패턴의 서버 검증(거점 상태·골드·소유·장착 여부). 구매는 NORMAL 인스턴스 생성(옵션 굴림 — 악세사리 노멀은 옵션 1개), 판매가 = `Price × SellPercent`. 골드는 7-10의 **플레이어 공유 골드**에서 내고 받는다 (Q35). 거래 후 자동 저장
- [x] 7-10. **골드 공유 단위 전환** (Q35) — 골드를 `GamePlayer.gold`(캐릭터별)에서 **`PlayerInterface` 단위 공유**로 옮긴다. 싱글 = 3캐릭터 공유, 멀티 = 플레이어별(한 플레이어가 2캐릭터를 쓰면 2캐릭터 공유).
      - `PlayerInterface.gold` SyncVar(훅으로 UI 갱신) 신설. `GamePlayer.gold`는 `objectOwner.gold`로 위임하는 프로퍼티로 남겨 호출부를 단계적으로 옮긴다
      - `RewardService.DistributeBattleRewards`: 골드 보상 지급 횟수는 **드랍과 같은 규칙 — 싱글 1회, 멀티는 캐릭터마다** (G4 확정. 현재는 GamePlayer마다 전액이라 싱글이 3배 받는다). 보상 목록의 Gold 항목도 같은 횟수. 경험치는 지금처럼 캐릭터마다 전액
      - 류진솔 회복(`HealPartyFull`)·소피아 위험도(`ReduceHazard`)·상점(7-8)이 플레이어 골드를 쓴다
      - 세이브: `ProfileData.gold`를 플레이어 단위(`PlayerGoldData { steamId, gold }`)로 옮긴다. 구 세이브(버전 0)는 같은 SteamID 프로필들의 골드 합으로 변환. 싱글 세이브의 캐릭터를 멀티로 들고 올 때 골드는 **전액** (F5)
      - `PlayerOrder` 파티 배너·거점 UI의 골드 표시를 플레이어 골드로
- [x] 7-9. 메르크리우스 UI — `NPC_Mercurius`에서 숨긴 구 카드 버튼 중 하나를 "상점"으로 살린다 (나머지는 후순위 콤보카드 가챠·악세사리 가챠 자리로 남겨 둠). 클래스 주석 '기획: 장비 제작'을 기획대로 고친다. 상점 팝업은 런타임 구성(`SaveSlotPanel` 패턴)으로 구매 탭 / 판매 탭 (현재 선택 캐릭터 기준 골드·인벤토리).
      ※ `NPC_ShadowMan.buttonItemShop` → `ItemShopPopUp`(빈 팝업)은 그림자꾼 스킬 초기화·크리스털 가챠 UI로 바뀔 때까지 그대로 둔다

**완료 기준**
- 방패병과 감시자의 드랍 경향이 다르다. 테이블 확률 150% 항목은 싱글에서 파티 전체로 1개 확정 + 50%로 1개 더 나온다
- 멀티 2인에서 같은 몬스터는 캐릭터마다 굴리고 확률이 1.5배다 (150% 항목 → 캐릭터마다 2개 확정 + 25%)
- 위험도가 낮으면 전설이 나오지 않는다. 엘리트 처치 후 고급 이상 비율이 오른다
- 싱글에서 전투 승리 골드가 한 번만 들어오고, 세 캐릭터 누구를 선택해도 같은 골드가 보인다. 멀티에서는 플레이어마다 골드가 따로다
- 메르크리우스에서 기본 장비를 사면 골드가 줄고 NORMAL 장비가 들어온다. 인벤토리 장비를 팔면 등급에 따라 골드를 받는다. 장착 중인 장비는 팔 수 없다

**구현 (2026-10-19)** — 컴파일·정적 검증 완료 (드랍 굴림 100회: DEFAULT_BOSS 장비 153개 ≈ 150%, 멀티 ×1.5·위험도 5에서 305개; 상점 17항목; GW2 희귀 판매가 100). **인게임 확인은 아직.**
- 7-1: `M_TurnManager.battleKilledMonsters`(이름·등급·테이블) — 사망 처리에서 기록, 전투 진입 시 초기화, `ConsumeKilledMonsters`
- 7-2: `DropTableDB.csv` + `DB/DropTableData.cs`(`TableFor`, `Roll(tableId, hazard, 배율%)` — 100 초과분 추가 1회 판정, 풀 지정자 `ANY_*` DropWeight 추출). `MonsterStatDB.DropTable` 컬럼(방패병 SOLDIER_SHIELD, 감시자 WACHER)
- 7-3·7-4·7-6: `RewardService.DistributeBattleRewards` 재작성 — 싱글은 `currentGamePlayer` 1회 / 멀티는 캐릭터마다, `ITEM_DROP_MULTIPLAYER_PERCENT`(150), 등급 `EquipGradeData.RollGrade(처치 등급, 위험도)`. `EQUIP_DROP_PERCENT/POTION_DROP_PERCENT/EQUIP_DROP_MIN_HAZARD` 삭제. 골드 보상도 같은 횟수(G4), 경험치는 캐릭터마다 전액
- 7-5: `Reward_Type.Equip/Potion` + `rewardName/rewardColorHex`, `RewardListItem`이 런타임 이름 라벨(등급 색) 생성 — 클릭 = 목록에서 제거(이미 지급됨)
- 7-7·7-8: `ShopDB.csv` + `DB/ShopData.cs`(`SellPrice` = Price × SellPercent), `M_HubManager.CmdShopBuy/CmdShopSell` (거점·골드·인벤토리 검증, 거래 후 저장, 토스트)
- 7-9: `UI/PopUpComponent/ShopPanel.cs` 런타임 팝업(구매/판매 탭, 인벤토리·골드 변화 구독). `NPC_Mercurius`의 `buttonCardShop`을 "상점"으로 (`ui.NPC_Mercurius.ButtonShop_Label`)
- 7-10: `PlayerInterface.gold` SyncVar(훅 → 소유 캐릭터 `NotifyGoldChanged` → `PlayerOrder`), `GamePlayer.gold`는 위임 프로퍼티, `EnsureGoldInitialized`(세이브 `PlayerGoldData` 또는 초기 골드 — 싱글 파티는 100 공유). 세이브 `RpgSaveData.playerGolds`, 버전 0은 SteamID별 프로필 골드 합(`TryGetPlayerGold`). 싱글 → 멀티 이관은 같은 SteamID라 전액(F5)

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

- [ ] 9-1. 구 세이브 3슬롯 로드 회귀 (`saveVersion` 0 → 현재) — 문자열 장비 → NORMAL 인스턴스, 대체 장비 매핑(AC3 → AC4 · 구 AR2 → AR3 · 구 HE2 → HE3, R13), 방어구 빈 부위 기본 세트 보충(A5), 최대치 델타 차감(3-11, F2), 구 PO3 → 자원 종류별 PO3/PO4/PO1(6-4), 캐릭터별 골드 → 플레이어 골드 합산(7-10). 변환 후 저장하면 현재 버전으로 기록되는지
- [ ] 9-2. ParrelSync 2클라이언트 — 클라이언트 캐릭터의 드랍·착탈·상점 거래·소모품 사용이 호스트 검증을 거쳐 반영되는지. `SyncList<EquipInstance>` 초기 동기화(늦게 들어온 클라이언트), MISS·치명 표시 RPC, 몬스터 방어 버프 표시
- [ ] 9-3. RPC 해시 충돌 점검 — 새 `Cmd*/Rpc*`(`RpcDisplayMiss`, `CmdShopBuy/CmdShopSell`, `CmdEquip` 시그니처 변경 등) 추가 후 콘솔 "have the same hash" 확인
- [ ] 9-4. 밸런스 초안 — 1스테이지 플레이로 조정하고 결과를 `RPG_CONVERSION_DESIGN.md`에 기록
      - 판정: 랜덤 보정 폭, 약점 TP 계수·**반복 체감률**(Q12), 치명 TP
      - 몬스터: 방어력·마법방어·위험도 보정 (Phase 2 이후 전투 길이)
      - 장비: 등급별 옵션 범위·개수, 등급 가중치, 전설 위험도 하한, 힘 요구치, 드랍 테이블 확률, 상점 가격·판매율
- [ ] 9-5. 기획서 갱신
      - 「보조 스탯」「치명타」「약점 공격」「속성」「방어구」「장비 옵션」「아이템」「거점」 절에 구현 줄 추가, 결정 사항(Q1~Q39, A1~A9) 반영
      - (10-19 2차에 기획서 선반영 완료) 「자원」 최종 피해 기준(Q32), 「기본 공격 속성」 무기 속성(Q25), 「보조 스탯」 단일 판정 공식(Q26), 「치명타」 회복·실드 치명(Q29), 「전투」 방어 행동·실드 규칙(Q40·Q41). 구현 후에는 "구현 (날짜)" 줄만 덧붙인다
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
- TP 루프의 자기 턴 시작 실드 리셋 `next.target.defense = 0`, 방어 행동의 실드 획득·`DEFEND_BASE_VALUE`, 철귀 턴 종료 실드, `TargetObject.GainDefense`(→ `GainShield` 덮어쓰기), 허물 강화의 회복→실드 분기 (Phase 1-14·1-15)
- `CharacterStatDB` `BasicAttackAttribute/AttackStat`(→ 무기 `Attribute`), `GamePlayer.EquipAttackBonusFor`(→ `TotalPhysicalAttack/TotalMagicAttack`), `ApplyEquipMaxDeltas`(→ `TotalMaxHP/TotalMaxResource`) (Phase 3)
- `GamePlayer.gold` 캐릭터별 골드 → `PlayerInterface.gold` 공유 골드, `RewardService`의 GamePlayer마다 골드 지급 (Phase 7-10)
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
- Phase 1과 3은 서로 독립이라 병행할 수 있다. 단 1-4 `SkillCast` 이관은 스킬 파일 전체를 건드리므로 다른 스킬 작업과 충돌하지 않는 시점에 한 번에 한다 (미커밋 FreeAction 변경을 먼저 커밋)
- Phase 2는 Phase 1(공용 스탯 조회·`IsMagic` 헬퍼·`ResolveHit`·스탯 버프 훅)만 있으면 된다. 장비와 무관하다
- 7-10 골드 공유 전환은 Phase 3과 무관하고 세이브(3-11 `saveVersion`)만 공유하므로 먼저 해도 된다
- Phase 4는 Phase 1(약점 TP·양방향 `ResolveHit`)과 Phase 3(`EquipDB` `ArmorType`·`RequireStr`)이 모두 필요하다
- Phase 6은 Phase 1의 공용 회복·스탯 버프(1-2, 1-10, 1-12)만 있으면 착수할 수 있다
- Phase 7은 Phase 3(인스턴스·등급 DB)이 필수, 상점(7-7~7-9)은 Phase 5와 무관하게 먼저 해도 된다
- 약점 상쇄(5-12)는 Phase 4, 콤보 추가 데미지(5-5)의 실제 효과는 연계 구현에 의존한다
