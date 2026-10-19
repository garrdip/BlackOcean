# 아이템 시스템 — 구현 형식 (Phase 0, 2026-10-19)

> 기준 문서: `RPG_CONVERSION_DESIGN.md` 「보조 스탯」「속성」「약점 공격」「무기」「방어구」「장비 옵션」「아이템」「거점」「멀티플레이어 변경점」 절 + `ToDo_RPG_ITEM.md` 결정 Q1~Q45.
> 이 문서는 **CSV 헤더·enum·규칙의 확정 형식**이다. 수치는 전부 "초안"이며 `ToDo_RPG_ITEM.md` 9-4 밸런스 검토에서 바뀐다. 코드·CSV 파일 변경은 Phase 1 이후에 이 형식대로 한다 (Phase 0은 파일을 바꾸지 않는다).
> 표기: `[초안]` = 밸런스 테스트로 바뀔 수치. `[Phase N]` = 그 형식을 실제로 만드는 Phase.

---

## 1. 속성 `AttackAttribute` [Phase 4]

```
enum AttackAttribute { NONE, SLASH, STRIKE, PIERCE, ICHI, SIMSANG, RESONANCE }
BattleActions.IsMagic(a) = a is ICHI | SIMSANG | RESONANCE   // 마법방어로 경감
```
- 물리: 참격 SLASH · 타격 STRIKE · 관통 PIERCE / 마법: 이치 ICHI · 심상 SIMSANG · 공명 RESONANCE. 기존 `MAGIC`은 `ICHI`로 이관한다.
- 로컬라이즈 키 `attr.<enum>` (예: `attr.ICHI` = 이치). 표기는 `BattleActions.AttributeName`이 키를 읽는다.
- `MAGIC → ICHI` 이관 목록 (Phase 4-1에서 실행):
    | 파일 | 행 / 컬럼 | 현재 | 이관 |
    |---|---|---|---|
    | `SkillDB.csv` | HS1 화염구 `Attribute` | MAGIC | ICHI |
    | `SkillDB.csv` | HS3 마력 폭풍 `Attribute` | MAGIC | ICHI |
    | `MonsterStatDB.csv` | WacherA `Weakness`, `AttackAttribute` | MAGIC, MAGIC | ICHI, ICHI |
    | `MonsterStatDB.csv` | WacherB `Weakness`, `AttackAttribute` | MAGIC, MAGIC | ICHI, ICHI |
    | `MonsterStatDB.csv` | Guardian `Weakness` | PIERCE\|MAGIC | PIERCE\|ICHI |
    | `MonsterStatDB.csv` | 치유사 `AttackAttribute` | MAGIC | ICHI |
    | `MonsterStatDB.csv` | Happy, Saddy `AttackAttribute` | MAGIC | ICHI (엘리트 — 심상으로 바꿀지는 몬스터 기획에서) |
    | `MonsterStatDB.csv` | E3, E4 `Weakness` | MAGIC | ICHI |
    | `MonsterStatDB.csv` | Boss_Momos `AttackAttribute` | MAGIC | ICHI |
    | `MonsterStatDB.csv` | Boss_Apates `Weakness` | SLASH\|MAGIC | SLASH\|ICHI |
    | `CharacterStatDB.csv` | `Weakness`, `Resist`, `BasicAttackAttribute`, `AttackStat` 컬럼 | — | **컬럼 삭제** (Q17, Q25 — 기본 공격은 무기가 정한다) |
    | `EquipDB.csv` | 무기 행 `Attribute` | (신설) | 무기마다 지정 (§4) |
- **이관 완료 (2026-10-19, Phase 4-1)** — 위 표대로 실행. `IsMagic` = ICHI·SIMSANG·RESONANCE.

## 2. 보조 스탯·방어 스탯 컬럼 [Phase 1·2]

### 2-1. `CharacterStatDB.csv`
- 추가: `BaseEvade, BaseAccuracy, BaseCrit` (레벨 성장 없음 — 기획 「보조 스탯」)
    | Character | BaseEvade | BaseAccuracy | BaseCrit |
    |---|---|---|---|
    | GEORK | 5 | 95 | 5 |
    | HONGDANHYANG | 5 | 95 | 5 |
    | ERIS | 10 | 95 | 10 |
- 삭제: `Weakness`, `Resist` (Q17), `BasicAttackAttribute`, `AttackStat` (Q25)
- 유지: `BaseHP~GrowCtrl`, `Resource`, `BaseResource`

### 2-2. `MonsterStatDB.csv`
새 헤더: `Monster_Name, Weakness, AttackAttribute, Agility, Exp, Evade, Accuracy, CritRate, Defense, MagicDefense, HazardAtk, HazardDef, HazardMdef, HazardHp, DropTable, Description`
- 추가: `Evade, Accuracy, CritRate` (컬럼이 없거나 비면 5 / 95 / 0 — Q4), `Defense, MagicDefense` (Q14), `HazardMdef` (위험도 1당 마법방어 +), `DropTable` (§9 테이블 ID, 비면 등급별 기본)
- 의미 변경: `HazardDef` = 위험도 1당 **방어력 스탯** + (Q44. 구 "실드 획득량 +")
- 삭제: `TPShield` (로드만 되고 참조 없음 — Phase 2-1에서 로더와 함께 제거)
- 치명 보정: 몬스터 기본 치명 0이라도 약점 공격이면 `WEAKNESS_CRIT_BONUS`(10)%로 치명이 난다 (Q28, 의도)

### 2-3. 공용 스탯 조회 (`TargetObject`, 플레이어·몬스터 공통) [Phase 1-2]
```
GetEvade / GetAccuracy / GetCritRate / GetCritDamage / GetDefense / GetMagicDefense / GetPhysicalAttack / GetMagicAttack
```
- 플레이어 = `GamePlayer.Total*` + `STAT_*` 버프. 몬스터 = `MonsterStatDB` + 위험도 보정 + `STAT_*` 버프.
- `GamePlayer.Total*` 정의 (Q25, Q43):
    | 프로퍼티 | 식 |
    |---|---|
    | `TotalStrength` | 힘 + `STRENGTH` 옵션 합 (무기 공격력 제외) |
    | `TotalIntelligence` | 지능 + `INTELLIGENCE` 옵션 합 |
    | `TotalAgility` | (민첩 + `AGILITY` 옵션 합) × (힘 요구치 미달이면 `ARMOR_STR_PENALTY_AGI_PERCENT`/100) |
    | `TotalControl` | 제어 + `CONTROL` 옵션 합 |
    | `TotalDefense` / `TotalMagicDefense` | 방어·마방 + 장비 기본 옵션 합 + `DEFENSE` 옵션 합 |
    | `TotalPhysicalAttack` | `TotalStrength` + Σ 무기 `PhysicalAttack` |
    | `TotalMagicAttack` | `TotalIntelligence` + Σ 무기 `MagicAttack` |
    | `GetAttackPower(scalesWithInt, attribute)` | (scalesWithInt ? `TotalIntelligence` : `TotalStrength`) + (`IsMagic(attribute)` ? Σ 무기 `MagicAttack` : Σ 무기 `PhysicalAttack`) — **실제 피해 계산은 전부 이 함수** (Q46). 평타는 무기의 `ScaleStat`·`Attribute`, 스킬은 SkillDB의 `ScaleStat`·`Attribute`로 부른다 (J1 확정). `NONE`은 scalesWithInt 쪽. `TotalPhysicalAttack/TotalMagicAttack`은 스탯창 표시용 |
    | `TotalMaxHP` | `MaxHP`(고유치: 기본 + 성장 + HP 노드) + `MAX_HP` 옵션 합 |
    | `TotalMaxResource` | `maxResource` + (자원이 MP면 `MAX_MP`, RAGE면 `MAX_RAGE` 옵션 합) |
    | `TotalEvade / TotalAccuracy / TotalCritRate` | 기본치 + `EVADE / ACCURACY / CRIT_RATE` 옵션 합 (%) |
    | `TotalCritDamage` | `CRIT_DAMAGE_PERCENT` + `CRIT_DAMAGE` 옵션 합 (%) |
- 세이브에는 고유치(`MaxHP`, `maxResource`, 힘·지능…)만 저장한다. 장비 효과는 매번 계산 (Q43 — 구 `ApplyEquipMaxDeltas` 폐기).

### 2-4. 스탯 버프 타입 (`BuffType` 추가, Q15·Q45) [Phase 1-12]
```
STAT_ATTACK, STAT_STRENGTH, STAT_DEFENSE, STAT_MAGIC_DEFENSE, STAT_CRIT_RATE, STAT_EVADE, STAT_ACCURACY
```
- 양수 = 상승, 음수 = 하락. `STAT_ATTACK`(공격력 가산)과 `STAT_STRENGTH`(힘 스탯 가산 — 힘 요구치 판정에 들어감)는 다르다.
- 지속: n턴, 대상의 자기 턴 종료 시 1 차감, 자기 턴 중 부여면 그 턴은 세지 않음 (Q33·Q37). 예외: 방어 행동·철귀 방어력 버프는 **다음 자기 턴 시작에 만료** (`expireOnOwnTurnStart`, Q40·Q41).
- 중첩: **출처(`sourceId` — 스킬 번호 · `DEFEND` · `HS0` · 물약 번호)가 다르면 합산**, 같은 출처는 갱신 (Q45). 탐색 키 = (대상, 종류, 출처).
- `BuffDB.csv` 행 + 로컬라이즈 `buff.STAT_<X>.name` (예: `buff.STAT_DEFENSE.name` = 방어력 변화). `ICHI_ATTACK/ICHI_DEFENSE`는 기존 스킬 호환용으로만 남기고 버프 개편에서 정리한다.

## 3. BalanceDB 키 [Phase 1~7]

| 키 | 초안 | 뜻 | Phase |
|---|---|---|---|
| `DAMAGE_RANDOM_PERCENT` | 10 | 타격마다 −r~+r 정수 굴림, 피해 × (100 + r)/100 (Q11) | 1 |
| `CRIT_DAMAGE_PERCENT` | 150 | 치명 배수 — 피해·회복·실드 부여 공용 (Q29) | 1 |
| `CRIT_DEFENSE_PERCENT` | 50 | 치명 시 대상 방어력 적용 비율 | 1 |
| `CRIT_DEFENSE_FLAT_REDUCE` | 10 | 치명 시 방어력 추가 차감 (0 하한) | 1 |
| `CRIT_TP_DAMAGE` | 5 | 치명 시 대상 TP 감소 (약점 TP와 합산, 체감 없음 — Q13) | 1 |
| `WEAKNESS_DAMAGE_PERCENT` | 120 | (기존) 약점 피해 배율 | — |
| `WEAKNESS_TP_FIXED` | 15 | 약점 TP 고정계수 | 1 |
| `WEAKNESS_TP_RATIO` | 5 | 약점 TP 비율계수 — TP 감소 = 고정 + 비율 × r/10 | 1 |
| `WEAKNESS_CRIT_BONUS` | 10 | 약점 공격 시 치명률 가산 %p | 1 |
| `WEAKNESS_TP_REPEAT_PERCENT` | 50 | 같은 대상 반복 약점 피격 시 TP 감소 체감률 (플레이어 대상 포함 — Q30) | 1 |
| `WEAKNESS_TP_MIN` | 5 | 약점 TP 감소 하한 (구 `TP_BREAK_MIN` 개명, 0 = 하한 없음) | 1 |
| `DEFEND_DEFENSE_UP_BASE` | 10 | 방어 행동 물리 방어력 가산 기본 (Q40, 임시) | 1 |
| `DEFEND_DEFENSE_UP_PER_LEVEL` | 1 | 방어 행동 레벨당 가산 (Q40, 임시) | 1 |
| `HS0_DEFENSE` | 10 | (기존 키, 의미 변경) 철귀 턴 종료 **방어력 버프량** (Q41) | 1 |
| `ARMOR_STR_PENALTY_AGI_PERCENT` | 50 | 힘 요구치 미달 시 민첩 비율 (Q16) | 4 |
| `ITEM_DROP_MULTIPLAYER_PERCENT` | 150 | 멀티 드랍 확률 배율 (Q24) | 7 |
| 폐기 | | `TP_BREAK_BASE`, `TP_BREAK_MIN`(개명), `DEFEND_BASE_VALUE`, `EQUIP_DROP_PERCENT`, `POTION_DROP_PERCENT`, `EQUIP_DROP_MIN_HAZARD`, `SKILL_LEVEL_BONUS_PERCENT`(크리스털 작업에서) | 1·7 |
- 등급 가중치·전설 위험도 하한·옵션 배율 키는 만들지 않는다 — `EquipGradeDB`/`EquipOptionDB`가 가진다 (Q3).

## 4. 장비 기본 데이터 `EquipDB.csv` [Phase 3]

새 헤더: `EquipNo, Name, Slot, WeaponType, ArmorType, Attribute, ScaleStat, RequireLevel, RequireStr, PhysicalAttack, MagicAttack, Defense, MagicDefense, Price, DropWeight, Description`

```
enum EquipSlot  { WEAPON, ARMOR, HELMET, BOOTS, ACCESSORY }          // 기존
enum WeaponType { NONE, GREATSWORD, LONGSWORD, STAFF, BOOK, ORB, CORE } // 대검·장검·지팡이·책·수정구·핵
enum ArmorType  { NONE, HEAVY, LIGHT, ROBE }                          // 중갑·경갑·로브
```
- 삭제 컬럼: `Character`(착용 제한 없음 — Q22), `Grade`(인스턴스가 가짐), `Agility/MaxHP/MaxResource`(추가 옵션으로 이관), `SkillLevel`(Q1), `Attack`(→ `PhysicalAttack/MagicAttack`)
- 무기 행: `WeaponType` + `Attribute`(기본 공격 속성, Q25) + `ScaleStat`(평타 계수 스탯 STR/INT — SkillDB와 같은 값, Q46) + `PhysicalAttack`·`MagicAttack` 둘 다 (0 허용). `ArmorType=NONE`, `RequireStr=0`, 방어 0.
    - 기본 공격 = `GetAttackPower(ScaleStat == INT, Attribute)` = **`ScaleStat` 스탯 + 속성에 맞는 쪽의 무기 공격력** (Q46). 대검·장검 = 힘 + 물리공격력, 지팡이·책·수정구 = 지능 + 마법공격력, **핵 = 힘 + 마법공격력(공명 — 마법방어로 경감)**. 무기가 없으면 STR·NONE(힘만). 스킬은 SkillDB `ScaleStat`·`Attribute`로 같은 함수 (J1 확정 — 에리스 공명 스킬 = 힘 + 핵 마법공격력).
    - 힘 + 무기 물리공격력 = 최종 물리공격력. 무기 공격력은 힘 스탯·힘 요구치에 들어가지 않는다.
- 방어구 행(ARMOR/HELMET/BOOTS): `ArmorType` + `RequireStr` + `Defense`·`MagicDefense`. `WeaponType=NONE`, `Attribute=NONE`, 공격 0.
- 악세사리 행: 기본 옵션 없음 (전 수치 0). 노멀도 추가 옵션 1개 (§5).
- `DropWeight`: 풀 지정자(`ANY_*`) 드랍·상점 가중치. 0이면 풀에서 제외(퀘스트·전용 드랍).
- 요구 레벨: 착용 조건에만 쓴다. 드랍 레벨 필터는 없다 (F3).
- 행 초안 `[초안]` — 무기 종류 6종 × 1 이상, 방어구 3종 × 3부위, 악세사리 4종. 구 16행은 아래로 이관하고 AC3 숙련의 부적은 삭제. **세이브 변환기 매핑: AC3 → AC4, 구 AR2(철 갑옷 Lv4) → AR3, 구 HE2(현자의 관) → HE3** — 새 AR2·HE2는 초기 세트용 다른 장비라 번호가 겹친다 (R13):
    | EquipNo | Name | Slot | WeaponType | ArmorType | Attribute | Scale | ReqLv | ReqStr | PAtk | MAtk | Def | Mdef | Price | DropW | 비고 |
    |---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
    | GW1 | 낡은 대검 | WEAPON | GREATSWORD | NONE | SLASH | STR | 1 | 0 | 3 | 0 | 0 | 0 | 50 | 10 | 게오르크 초기 (구 GW1) |
    | GW2 | 대장군의 검 | WEAPON | GREATSWORD | NONE | SLASH | STR | 5 | 0 | 7 | 0 | 0 | 0 | 200 | 5 | 구 GW2 (방어 1 → 옵션으로) |
    | LW1 | 낡은 장검 | WEAPON | LONGSWORD | NONE | PIERCE | STR | 1 | 0 | 2 | 1 | 0 | 0 | 50 | 10 | 신규 |
    | HW1 | 낡은 지팡이 | WEAPON | STAFF | NONE | ICHI | INT | 1 | 0 | 0 | 3 | 0 | 0 | 50 | 10 | 홍단향 초기 (구 HW1) |
    | HW2 | 개화 지팡이 | WEAPON | STAFF | NONE | ICHI | INT | 5 | 0 | 0 | 6 | 0 | 0 | 200 | 5 | 구 HW2 (MP 5·마방 1 → 옵션으로) |
    | BW1 | 낡은 책 | WEAPON | BOOK | NONE | SIMSANG | INT | 1 | 0 | 0 | 2 | 0 | 0 | 50 | 10 | 신규 |
    | OW1 | 흐린 수정구 | WEAPON | ORB | NONE | SIMSANG | INT | 1 | 0 | 1 | 2 | 0 | 0 | 50 | 10 | 신규 |
    | EW1 | 흠집 난 코어 | WEAPON | CORE | NONE | RESONANCE | STR | 1 | 0 | 0 | 3 | 0 | 0 | 50 | 10 | 에리스 초기 (구 EW1). 공격력은 `MagicAttack` 쪽 (Q46) |
    | EW2 | 공명 코어 | WEAPON | CORE | NONE | RESONANCE | STR | 5 | 0 | 0 | 6 | 0 | 0 | 200 | 5 | 구 EW2 (민첩 2 → 옵션으로) |
    | AR1 | 가죽 갑옷 | ARMOR | NONE | LIGHT | NONE | - | 1 | 6 | 0 | 0 | 2 | 1 | 60 | 10 | 구 AR1 (HP 5 → 옵션으로). 에리스 초기 |
    | AR2 | 철 갑옷 | ARMOR | NONE | HEAVY | NONE | - | 1 | 8 | 0 | 0 | 4 | 1 | 60 | 10 | 게오르크 초기 (구 AR2는 AR3으로) |
    | AR3 | 강철 갑옷 | ARMOR | NONE | HEAVY | NONE | - | 4 | 14 | 0 | 0 | 6 | 2 | 220 | 5 | 구 AR2 (민첩 −1·HP 10 폐기) |
    | AR4 | 낡은 로브 | ARMOR | NONE | ROBE | NONE | - | 1 | 0 | 0 | 0 | 1 | 3 | 60 | 10 | 홍단향 초기 |
    | HE1 | 가죽 투구 | HELMET | NONE | LIGHT | NONE | - | 1 | 6 | 0 | 0 | 1 | 1 | 40 | 10 | 구 HE1. 에리스 초기 |
    | HE2 | 철 투구 | HELMET | NONE | HEAVY | NONE | - | 1 | 8 | 0 | 0 | 2 | 0 | 40 | 10 | 게오르크 초기 |
    | HE3 | 현자의 관 | HELMET | NONE | ROBE | NONE | - | 4 | 0 | 0 | 0 | 1 | 4 | 200 | 5 | 구 HE2 (MP 10 → 옵션으로) |
    | HE4 | 낡은 두건 | HELMET | NONE | ROBE | NONE | - | 1 | 0 | 0 | 0 | 0 | 2 | 40 | 10 | 홍단향 초기 |
    | BO1 | 여행자 신발 | BOOTS | NONE | LIGHT | NONE | - | 1 | 6 | 0 | 0 | 1 | 1 | 40 | 10 | 구 BO1 (민첩 1 → 옵션으로). 에리스 초기 |
    | BO2 | 질풍의 신발 | BOOTS | NONE | LIGHT | NONE | - | 4 | 8 | 0 | 0 | 2 | 1 | 220 | 5 | 구 BO2 (민첩 3 → 옵션으로) |
    | BO3 | 철 장화 | BOOTS | NONE | HEAVY | NONE | - | 1 | 8 | 0 | 0 | 2 | 0 | 40 | 10 | 게오르크 초기 |
    | BO4 | 천 신발 | BOOTS | NONE | ROBE | NONE | - | 1 | 0 | 0 | 0 | 0 | 1 | 40 | 10 | 홍단향 초기 |
    | AC1 | 생명의 반지 | ACCESSORY | NONE | NONE | NONE | - | 2 | 0 | 0 | 0 | 0 | 0 | 120 | 10 | 구 AC1 (HP 15 → 옵션으로. 노멀은 옵션 1개) |
    | AC2 | 마력의 목걸이 | ACCESSORY | NONE | NONE | NONE | - | 2 | 0 | 0 | 0 | 0 | 0 | 120 | 10 | 구 AC2 (자원 10 → 옵션으로) |
    | AC4 | 수호의 팔찌 | ACCESSORY | NONE | NONE | NONE | - | 3 | 0 | 0 | 0 | 0 | 0 | 150 | 10 | 구 AC4 (방어 2·마방 2 → 옵션으로). AC3 대체 매핑 대상 |
    | AC5 | 철귀의 부적 | ACCESSORY | NONE | NONE | NONE | - | 6 | 0 | 0 | 0 | 0 | 0 | 400 | 3 | 구 AC3 자리 (스킬레벨 폐기 — 옵션으로만) |
    - `RequireStr` 초안은 **원래 밸런스의 기본 힘**(게오르크 **8** — `CharacterStatDB` 주석 "기본 8/성장 3이 원래 값", 현재 CSV 10은 테스트 상향 / 에리스 7 / 홍단향 4) 기준이다 (Q36, R15). 초기 세트(게오르크 AR2·HE2·BO3 요구치 8 / 에리스 AR1·HE1·BO1 요구치 6 / 홍단향 AR4·HE4·BO4 요구치 0)는 요구치 ≤ 원래 기본 힘이라 원복 후에도 패널티가 없다. 테스트 상향을 원복하기 전까지는 게오르크가 더 많은 요구치를 넘는다.

### 4-1. 에리스 기본 공격의 계수 — 확정 (I1 → Q46, 2026-10-19 4차)
답변: **힘 + 마법공격력을 쓰고, 결과는 마법(공명) 공격.** 그래서 무기 행에 `ScaleStat`(STR/INT)을 두어 "계수 스탯"과 "속성(어느 방어로 막히나·어느 무기 공격력을 더하나)"을 분리했다. 핵 = `RESONANCE` + `STR` → 힘 + 핵 `MagicAttack`, 마법방어로 경감. 위 표의 EW1·EW2는 `MagicAttack` 쪽에 값을 둔다.
- **스킬도 같은 규칙 (J1 확정, 10-19 4차)** — 스킬 피해·회복 = SkillDB `ScaleStat` 스탯 + (스킬 `Attribute`가 마법이면 무기 마법공격력, 물리면 물리공격력). 에리스 공명 스킬 ES5~ES10 = 힘 + 핵 마법공격력, 화염구 = 지능 + 지팡이 마법공격력, 연속 베기 = 힘 + 대검 물리공격력. 속성 `NONE` 스킬(치유의 빛·수호 축복 등 회복·실드)은 `ScaleStat` 쪽 무기 공격력 (지능이면 마법공격력).

## 5. 장비 등급·옵션 [Phase 3]

### 5-1. `EquipGrade` + `EquipGradeDB.csv`
```
enum EquipGrade { NORMAL, ADVANCED, RARE, LEGEND }   // 구 ItemGrade는 유물과 함께 폐기
```
헤더: `Grade, OptionCountMin, OptionCountMax, LegendOptionCount, AccessoryOptionCountMin, AccessoryOptionCountMax, DropWeight, EliteBonusWeight, BossBonusWeight, MinHazard, SellPercent, ColorHex, NameKey`

| Grade | OptMin | OptMax | Legend | AccMin | AccMax | DropW | EliteBonus | BossBonus | MinHazard | Sell% | Color | NameKey | `[초안]` |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| NORMAL | 0 | 0 | 0 | 1 | 1 | 60 | 20 | 0 | 0 | 30 | #FFFFFF | equip.grade.NORMAL | |
| ADVANCED | 1 | 2 | 0 | 1 | 2 | 30 | 40 | 30 | 0 | 40 | #4CAF50 | equip.grade.ADVANCED | 수치 낮음 |
| RARE | 1 | 2 | 0 | 1 | 2 | 9 | 30 | 50 | 3 | 50 | #2196F3 | equip.grade.RARE | 수치 중간 |
| LEGEND | 2 | 2 | 1 | 2 | 2 | 1 | 10 | 20 | 8 | 60 | #FF9800 | equip.grade.LEGEND | 일반 2 + 전설 1 |
- 등급 굴림 = `DropWeight` + (엘리트 처치면 `EliteBonusWeight`, 보스면 `BossBonusWeight`) 가중 추출, 이번 전투 유효 위험도 < `MinHazard`인 등급은 제외.
- `SellPercent` = 플레이어가 상점에 팔 때 `EquipDB.Price` 대비 %. `ColorHex` = 이름 색. `NameKey` = 로컬라이즈 키.

### 5-2. `EquipOptionType`
```
enum EquipOptionType {
    // 일반 (추가 옵션)
    STRENGTH, INTELLIGENCE, AGILITY, CONTROL, MAX_HP, MAX_MP, DEFENSE, SHIELD,
    // 전설
    LIFE_STEAL, SKILL_DAMAGE, COMBO_DAMAGE, CRIT_DAMAGE, REFLECT_FLAT, REFLECT_PERCENT, HEAL_RECEIVED,
    WEAKNESS_NULLIFY, CRIT_RATE, ACCURACY, EVADE, FREE_MOVE, NO_ROW_PENALTY, MAX_RAGE
}
```

### 5-3. `EquipOptionDB.csv` — 옵션 × 등급마다 한 행 (Q3)
헤더: `OptionType, Grade, Slots, MinValue, MaxValue, IsPercent, ValueKind, Weight, AttributePool, NameKey`
- `Slots`: `|` 구분 (WEAPON|ARMOR|HELMET|BOOTS|ACCESSORY). `ValueKind`: NUMBER / ATTRIBUTE(값 = `AttackAttribute` 정수) / FLAG(값 1). `AttributePool`: ATTRIBUTE형만, `속성:가중치|…`.
- 일반 옵션은 NORMAL(악세사리 전용)·ADVANCED·RARE·LEGEND 행, 전설 옵션은 LEGEND 행만. 등급마다 범위가 다르다.
- 슬롯별 허용 옵션 (기획 「장비 옵션」 그대로):
    | 슬롯 | 추가 옵션 | 전설 옵션 |
    |---|---|---|
    | WEAPON | AGILITY·CONTROL·MAX_HP·MAX_MP | LIFE_STEAL·SKILL_DAMAGE·COMBO_DAMAGE·CRIT_DAMAGE |
    | ARMOR | MAX_HP·MAX_MP·SHIELD | REFLECT_FLAT·REFLECT_PERCENT·HEAL_RECEIVED·WEAKNESS_NULLIFY |
    | HELMET | STRENGTH·INTELLIGENCE·MAX_HP·MAX_MP | CRIT_RATE·ACCURACY |
    | BOOTS | AGILITY·MAX_HP·MAX_MP·CONTROL | EVADE·FREE_MOVE·NO_ROW_PENALTY |
    | ACCESSORY | DEFENSE·AGILITY·STRENGTH·INTELLIGENCE·CONTROL·MAX_HP·MAX_MP·SHIELD | MAX_RAGE·NO_ROW_PENALTY·FREE_MOVE·LIFE_STEAL·SKILL_DAMAGE·COMBO_DAMAGE·REFLECT_FLAT·REFLECT_PERCENT·HEAL_RECEIVED·CRIT_RATE·EVADE·CRIT_DAMAGE·WEAKNESS_NULLIFY |
- 수치 범위 `[초안]` (Min~Max. NORMAL 행은 악세사리 전용):
    | OptionType | NORMAL | ADVANCED | RARE | LEGEND | 단위 | 적용 지점 (ToDo) |
    |---|---|---|---|---|---|---|
    | STRENGTH / INTELLIGENCE / AGILITY / DEFENSE | 1~1 | 1~2 | 2~4 | 4~6 | 수치 | Total* (3-8) |
    | CONTROL | 2~2 | 2~4 | 4~8 | 8~12 | 수치 | TotalControl (3-8) |
    | MAX_HP | 5~5 | 5~10 | 10~20 | 20~30 | 수치 | TotalMaxHP (3-8) |
    | MAX_MP | 2~2 | 2~4 | 4~8 | 8~12 | 수치 | TotalMaxResource (MP 캐릭터만, D25) |
    | SHIELD | 5~5 | 5~10 | 10~20 | 20~30 | 수치 | 전투 시작 실드, 합산 후 1회 (5-9) |
    | LIFE_STEAL | — | — | — | 5~10 | % | HP 피해 × % 회복 (5-3) |
    | SKILL_DAMAGE | — | — | — | 10~20 | % | 스킬 타격 피해 증가 (5-4) |
    | COMBO_DAMAGE | — | — | — | 10~20 | % | 연계 시에만 (5-5, 후순위) |
    | CRIT_DAMAGE | — | — | — | 20~40 | %p | 치명 배수 가산 (5-2) |
    | REFLECT_FLAT | — | — | — | 3~6 | 수치 | 피격 시 반사, 2차 판정 (5-6) |
    | REFLECT_PERCENT | — | — | — | 10~20 | % | 받은 피해 × % 반사 (5-6) |
    | HEAL_RECEIVED | — | — | — | 10~20 | % | 받는 회복량 (비전투 포함, 5-7) |
    | WEAKNESS_NULLIFY | — | — | — | ATTRIBUTE | 속성 | 약점 집합에서 제거 (5-12). Pool = SLASH:1\|STRIKE:1\|PIERCE:1\|ICHI:1\|SIMSANG:1\|RESONANCE:1 |
    | CRIT_RATE / ACCURACY / EVADE | — | — | — | 5~10 | %p | 보조 스탯 가산 (5-1) |
    | FREE_MOVE / NO_ROW_PENALTY | — | — | — | FLAG | — | 5-11 / 5-10 |
    | MAX_RAGE | — | — | — | 10~20 | 수치 | TotalMaxResource (RAGE 캐릭터만, 5-8) |
- 스택 규칙 (5-13): 수치형 합산, FLAG는 OR, ATTRIBUTE는 합집합(같은 속성 중복 무효 — A6). 상한은 1차 없음.
- 생성 (`EquipGenerator.Create(equipNo, grade)`): 등급 행의 옵션 개수를 굴림 → 슬롯·등급이 맞는 일반 옵션 행에서 `Weight` 가중 **무중복** 추출 → 각 `Min~Max` 균등 굴림 → LEGEND면 전설 옵션 풀에서 `LegendOptionCount`개 추가. 악세사리는 `AccessoryOptionCount*` 사용.

### 5-4. 장비 인스턴스 (Mirror 동기화·세이브)
```
struct EquipInstance { string instanceId; string equipNo; EquipGrade grade; int[] optionTypes; int[] optionValues; }
// optionValues: NUMBER = 수치, ATTRIBUTE = (int)AttackAttribute, FLAG = 1
// SyncList<EquipInstance> — 기본형·배열 필드만 (List<> 금지, Weaver 자동 직렬화)
class EquipInstanceSave { string instanceId; string equipNo; int grade; int[] optionTypes; int[] optionValues; }  // JSON
```
- `instanceId` = 서버 GUID 문자열. 착탈·판매는 `instanceId` 기준. 같은 슬롯은 자동 교체, 악세사리 2칸, 전투 중 착탈 불가 (기존).
- 세이브 `RpgSaveData.saveVersion` (없음 = 0). 0 → 1 변환: 문자열 장비 → NORMAL 옵션 0개 인스턴스, 대체 장비 매핑(**AC3 → AC4, 구 AR2 → AR3, 구 HE2 → HE3** — R13), 구 EquipDB `MaxHP/MaxResource` 델타를 저장된 최대치에서 차감(변환기 상수표), 방어구 빈 부위 기본 세트 보충, 구 PO3(자원 공용) → 소지 캐릭터 자원별 **PO3(MP) / PO4(RAGE) / PO1(HP)** (R13), 캐릭터별 골드 → 플레이어 골드 합산.

## 6. 방어구 종류·약점 [Phase 4]

### 6-1. `ArmorTypeDB.csv`
헤더: `ArmorType, Weaknesses, NameKey`
| ArmorType | Weaknesses | NameKey |
|---|---|---|
| HEAVY | STRIKE\|RESONANCE | equip.armortype.HEAVY (중갑) |
| LIGHT | PIERCE\|SIMSANG | equip.armortype.LIGHT (경갑) |
| ROBE | SLASH\|ICHI | equip.armortype.ROBE (로브) |

### 6-2. 세트 규칙 — `GamePlayer.GetWeaknesses()` (코드 규칙, Q16)
```
types = { 갑옷.ArmorType, 투구.ArmorType, 신발.ArmorType }  (미착용 부위 = 없음)
if 미착용 부위 있음        → 전 속성 6종
else if distinct(types)=1 → 그 종류의 Weaknesses
else if distinct(types)=2 → 두 종류의 Weaknesses 합집합
else (3종)                 → 전 속성 6종
result −= 착용 장비의 WEAKNESS_NULLIFY 속성들 (Q23·A6 — "전 속성" 상태에도 적용)
```
- 캐릭터 고유 약점·내성은 없다 (Q17). 몬스터 약점 판정과 같은 코드·같은 BalanceDB 키 (Q18).
- 새 캐릭터는 같은 종류 3부위를 받는다 (A5 — §4 초기 세트).

### 6-3. 힘 요구치 (Q42)
```
요구치 미달 = 착용 방어구 중 RequireStr > 현재 힘 인 부위가 하나라도 있음
현재 힘 = TotalStrength(기본 + STRENGTH 옵션) + STAT_STRENGTH 버프 (공격력 버프·무기 공격력 제외)
미달이면 TotalAgility × ARMOR_STR_PENALTY_AGI_PERCENT/100 — 부위 수와 무관하게 1회
```
- 전투 중 힘 버프로 요구치를 넘으면 그동안 패널티가 풀린다 (TP 충전이 매 틱 `TotalAgility`를 읽는다).

## 7. 소모품 `ConsumableDB.csv` [Phase 6]

헤더: `PotionNo, Name, Effect, ValidTarget, Value, Duration, BattleOnly, UsableResource, Price, DropWeight, Description`
- `Effect` = `ConsumableEffects`의 정적 메서드 이름 (SkillData와 같은 리플렉션 바인딩, 없으면 에러 로그). `ConsumableType` enum 폐기.
- `ValidTarget`: 스킬과 같은 enum. **`NONE` = 자신** (1차는 NONE만 허용, 추후 MEMBER/ENEMY). `BattleOnly` 0/1. `UsableResource`: ANY | MP | RAGE | HP (자원 종류가 맞는 캐릭터만).
- 효과 시그니처 (Q38 — 비전투에는 TargetObject가 없다):
    ```
    static IEnumerator <Effect>(ConsumableData.Def def, GamePlayer user, GamePlayer target, TargetObject targetUnit)
    // targetUnit: 전투 중에만 채워짐(비전투 null). 추후 몬스터 대상 = target null + targetUnit 몬스터
    ```
- 사용 경로: `GamePlayer.ServerUseConsumable(potionNo, GamePlayer target)` 한 곳 (보유·BattleOnly·ValidTarget·UsableResource 검증) — 전투 ITEM 액션과 비전투 사용이 모두 이것을 부른다.
- 1차 효과 메서드: `HEAL_HP`(`ServerHeal` 경유 — 치명 회복·받는 회복량 적용), `RESTORE_MP`, `RESTORE_RAGE`, `BUFF_ATTACK`, `BUFF_CRIT_RATE`, `BUFF_EVADE`(STAT_* 버프, 전투 전용, 사용 턴 제외 `Duration`턴). 후순위: `POISON_TO_ONE`(극약).
- 1차 행 `[초안]`:
    | PotionNo | Name | Effect | ValidTarget | Value | Duration | BattleOnly | UsableResource | Price | DropW |
    |---|---|---|---|---|---|---|---|---|---|
    | PO1 | HP 물약 | HEAL_HP | NONE | 30 | 0 | 0 | ANY | 30 | 10 |
    | PO2 | 상급 HP 물약 | HEAL_HP | NONE | 60 | 0 | 0 | ANY | 80 | 4 |
    | PO3 | MP 물약 | RESTORE_MP | NONE | 15 | 0 | 0 | MP | 40 | 6 |
    | PO4 | 분노의 물약 | RESTORE_RAGE | NONE | 30 | 0 | 1 | RAGE | 40 | 6 |
    | PO5 | 공격력 물약 | BUFF_ATTACK | NONE | 5 | 3 | 1 | ANY | 60 | 4 |
    | PO6 | 치명타 물약 | BUFF_CRIT_RATE | NONE | 15 | 3 | 1 | ANY | 60 | 4 |
    | PO7 | 회피율 물약 | BUFF_EVADE | NONE | 15 | 3 | 1 | ANY | 60 | 4 |
- 로컬라이즈 `potion.<PotionNo>.name/.desc`. 초기 지급: PO1 × 2 (+ 게오르크 PO4 × 1 시험).

## 8. 골드·드랍 단위 (Q35·Q39·G4)
- 골드는 **플레이어(`PlayerInterface`) 단위 공유** — 싱글 3캐릭터 공유, 멀티는 플레이어별(2캐릭터면 2캐릭터 공유). 세이브 `PlayerGoldData { steamId, gold }`. 싱글 → 멀티 캐릭터 이관 시 전액.
- 전투 보상 골드·드랍 굴림 횟수: **싱글 = 파티 1회, 멀티 = 캐릭터(GamePlayer)마다**. 멀티 드랍 확률 × `ITEM_DROP_MULTIPLAYER_PERCENT`/100. 경험치는 캐릭터마다 전액 (기존).

## 9. 드랍 테이블 `DropTableDB.csv` [Phase 7]

헤더: `TableId, Kind, ItemNo, Chance, MinHazard, Description`
- `Kind`: EQUIP | POTION. `ItemNo`: 특정 `EquipNo/PotionNo` 또는 풀 지정자 `ANY_EQUIP / ANY_WEAPON / ANY_ARMOR(갑옷·투구·신발) / ANY_ACCESSORY / ANY_POTION` (`DropWeight` 가중 추출, 레벨 필터 없음).
- `Chance` %: 100 이하 확정, 넘는 몫은 추가 1회 판정 (150 → 1개 확정 + 50%). 이번 전투 유효 위험도 < `MinHazard`면 건너뜀. 장비 등급은 §5-1로 굴린다.
- 몬스터 지정: `MonsterStatDB.DropTable`. 비면 `DEFAULT_NORMAL / DEFAULT_ELITE / DEFAULT_BOSS`.
- 초안 `[초안]`:
    | TableId | Kind | ItemNo | Chance | MinHazard | 비고 |
    |---|---|---|---|---|---|
    | DEFAULT_NORMAL | POTION | ANY_POTION | 40 | 0 | |
    | DEFAULT_NORMAL | EQUIP | ANY_EQUIP | 15 | 0 | |
    | DEFAULT_ELITE | EQUIP | ANY_EQUIP | 100 | 0 | 확정 1 |
    | DEFAULT_ELITE | POTION | ANY_POTION | 60 | 0 | |
    | DEFAULT_BOSS | EQUIP | ANY_EQUIP | 150 | 0 | 1 + 50% |
    | DEFAULT_BOSS | EQUIP | ANY_ACCESSORY | 50 | 5 | |
    | SOLDIER_SHIELD | EQUIP | ANY_ARMOR | 25 | 0 | 방패병 — 방어구 |
    | SOLDIER_SHIELD | POTION | ANY_POTION | 40 | 0 | |
    | WACHER | EQUIP | HW1 | 10 | 0 | 감시자 — 지팡이 |
    | WACHER | EQUIP | ANY_EQUIP | 10 | 0 | |
    | WACHER | POTION | PO3 | 30 | 0 | MP 물약 |

## 10. 상점 `ShopDB.csv` [Phase 7]
헤더: `ShopId, Kind, ItemNo, Price, Description`
- `ShopId` 1차 `MERCURIUS`. `Kind` EQUIP | POTION. `Price` 비우면 DB `Price`. 구매 장비는 NORMAL 인스턴스(악세사리는 옵션 1개 굴림). 판매가 = `EquipDB.Price × SellPercent`(§5-1). 장착 중인 장비는 못 판다. 인벤토리 상한 없음. 골드는 §8 플레이어 공유 골드.
- 초안: `MERCURIUS` — GW1, LW1, HW1, BW1, OW1, EW1, AR1, AR2, AR4, HE1, HE2, HE4, BO1, BO3, BO4, PO1, PO3.

## 11. 로컬라이즈 키 요약 [Phase 8]
`equip.<EquipNo>.name/.desc`, `equip.option.<EquipOptionType>.name/.format`("{0}%", 약점 상쇄 "약점 상쇄 : {attr}"), `equip.grade.<EquipGrade>`, `equip.weapontype.<WeaponType>`, `equip.armortype.<ArmorType>`, `attr.<AttackAttribute>`, `potion.<PotionNo>.name/.desc`, `buff.STAT_*.name`, `ui.msg.miss`, `ui.msg.potion_battle_only`, `ui.shop.*`. 전 로케일 CSV(`Resources/Language/*.csv`)에 등록 (`Document/LOCALIZATION.md`).
