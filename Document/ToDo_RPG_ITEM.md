# 아이템 개편 구현 워크플로우 (무기·방어구·악세사리·물약)

> 기준 문서: `RPG_CONVERSION_DESIGN.md` — 「보조 스탯」「치명타」「판정 순서」「무기」「방어구」「장비 옵션」「아이템」 절 (2026-09-22 개정판)
> 현재 구현 기준: `DB/EquipData.cs`(플랫 스탯 7종) · `DB/ConsumableData.cs`(HP/자원 물약 2종) · `Player/GamePlayer.Equipment.cs`(SyncList<string> 인벤토리, OnGUI 임시 창) · `Mangers/RewardService.cs`(드랍) · `SaveManager/GameSaveService.cs`(프로필 저장)
> 원칙: **매 Phase가 끝나면 게임이 실행 가능한 상태**를 유지한다. 판정(명중·회피·치명)은 장비 옵션보다 먼저 넣는다 — 옵션 대부분이 판정 수치를 조절하기 때문이다. 멀티 검증은 ParrelSync 2클라이언트 기준.
> 진행 방식: 항목별 [ ] 체크 → 에디터 플레이 테스트 → 한글 커밋. 구현 확정 사항은 `RPG_CONVERSION_DESIGN.md`의 해당 절에 "구현 (날짜)" 줄로 되돌려 적는다.

---

## 확정 필요 (착수 전 기획 결정)

구현 순서에 영향을 주는 미결 사항. 결정되지 않은 항목은 아래 **가정**대로 진행하고, 기획서에 가정을 명시한다.

| # | 항목 | 가정 (결정 전 기본값) |
|---|---|---|
| D1 | 장비 '스킬레벨' 옵션 | **폐기**. 크리스털 원칙(스킬 레벨은 크리스털로만)과 충돌. `EquipSkillLevelBonus`·`SkillLevel` 컬럼·AC3 숙련의 부적 제거 |
| D2 | 등급 명칭 | 기획 노멀/고급/레어/전설 ↔ 현 `ItemGrade {NORMAL, RARE, UNIQUE, LEGEND}`. **장비 전용 `EquipGrade {NORMAL, ADVANCED, RARE, LEGEND}` 신설**, `ItemGrade`는 구 아이템/아티팩트용으로 유지 |
| D3 | 옵션 수치 | 옵션은 **드랍 시 범위 내 랜덤 굴림** (콤보 카드 배율과 같은 방식). 같은 이름 장비라도 옵션이 다르므로 장비는 **인스턴스**가 된다 |
| D4 | 몬스터 보조 스탯 | 몬스터도 회피 5% / 명중 95% / 치명 0%를 기본으로 가지며 `MonsterStatDB` 컬럼으로 개별 조정 (컬럼 없으면 기본값). 몬스터 치명은 기본 0 — 켤지는 테스트 |
| D5 | 물약 사용 대상 | 전투 중 물약은 **자신에게만** (현 구조 유지). 아군 대상 물약은 후속 |
| D6 | 극약의 정의 | 자신의 HP를 1로 만든다. 에리스는 즉시 광기, 게오르크는 잃은 HP만큼 "맞을 때 분노" 공식으로 분노 획득. 전투 중에만 사용 가능 |
| D7 | 메르크리우스 제작 | 재료 시스템이 없으므로 1차는 **골드 구매(노멀·고급 고정 목록) + 옵션 재감정(골드로 랜덤 옵션 재굴림)**. 재료 기반 제작은 후속 |
| D8 | 포지션 패널티 제거 옵션 | 전열의 방어 하락·후열의 공격 하락만 없애고 **보너스(전열 공격↑·후열 방어↑)는 유지** |
| D9 | 이동 시 턴 소모 제거 옵션 | 이동을 `FreeAction`처럼 턴당 1회 무료로 (철귀 이동과 같은 `tpUsedFreeSkillNo` 계열 처리). ※기획서 "중열: 위치 변환 코스트 삭제"도 현재 미구현 — 같은 경로로 함께 처리 |
| D10 | 콤보 추가 데미지 옵션 | 연계(파티 TP) 미구현 상태. 스탯 합산·표시까지만 만들고 **적용 지점은 연계 구현 시** 연결 |

---

## Phase 0 — 데이터 구조 설계 (코드 변경 없음, 반나절)

**목표**: 인스턴스 장비·옵션·물약 데이터 형식을 확정해 이후 Phase가 CSV만 채우면 되게 한다.

- [ ] 0-1. `EquipGrade` enum 확정 (D2) — 등급별 옵션: NORMAL 0개(악세사리 1) / ADVANCED 1~2개·수치 낮음 / RARE 1~2개·수치 중간 / LEGEND 수치 높은 2개 + 전설 옵션 1.
      등급이 옵션 **개수와 수치 구간**을 함께 정하므로 `EquipOptionDB`의 Min/Max는 등급별 배율(`EQUIP_OPTION_VALUE_PERCENT_ADVANCED/RARE/LEGEND`)로 나눈다
- [ ] 0-2. `EquipOptionType` enum 초안 (`Common/ProjectD.cs`) — 기획서 옵션 목록 전체를 한 enum으로:
      일반 `STRENGTH, INTELLIGENCE, AGILITY, CONTROL, MAX_HP, MAX_MP, DEFENSE, MAGIC_DEFENSE, SHIELD`
      전설 `LIFE_STEAL, SKILL_DAMAGE, COMBO_DAMAGE, CRIT_DAMAGE, REFLECT_FLAT, REFLECT_PERCENT, HEAL_RECEIVED, CRIT_RATE, ACCURACY, EVADE, FREE_MOVE, NO_ROW_PENALTY, MAX_RAGE`
- [ ] 0-3. `EquipOptionDB.csv` 컬럼 설계: `OptionType, Slots('|' 구분 — WEAPON|ARMOR|HELMET|BOOTS|ACCESSORY), IsLegend(0/1), MinValue, MaxValue, IsPercent(0/1), Weight, NameKey`
      슬롯별 허용 옵션은 기획서 「장비 옵션」 표를 그대로 옮긴다 (무기 추가옵션 = 민첩·제어·HP·MP, 갑옷 = HP·MP·보호막, 투구 = 힘·지능·HP·MP, 신발 = 민첩·HP·MP·제어, 악세사리 = 전 일반 옵션 + 보호막)
- [ ] 0-4. `EquipDB.csv` 개편안: 기본 옵션만 남긴다 — `EquipNo, Name, Slot, Character, RequireLevel, PhysicalAttack, MagicAttack, Defense, MagicDefense, Price, DropWeight, Description`.
      `Grade/Agility/MaxHP/MaxResource/SkillLevel` 컬럼 삭제 (등급·추가 옵션은 인스턴스가 가짐). 무기 공격력을 물리/마법으로 분리 (기획 "공격력(물리공격력, 마법공격력)")
- [ ] 0-5. 장비 인스턴스 형식 확정: `EquipInstance { string instanceId; string equipNo; EquipGrade grade; int[] optionTypes; int[] optionValues; }`
      Mirror `SyncList<EquipInstance>` — 필드가 기본형·배열뿐이라 Weaver 자동 직렬화 가능. `List<>` 필드는 쓰지 않는다
- [ ] 0-6. `ConsumableDB.csv` 개편안: `PotionNo, Name, Type, Value, Duration, BattleOnly(0/1), Price, DropWeight, Description`
      `ConsumableType`: `HEAL_HP, RESTORE_MP, RESTORE_RAGE, BUFF_CRIT_RATE, BUFF_EVADE, BUFF_ATTACK, POISON_TO_ONE` (구 `RESTORE_RESOURCE`는 MP/분노 분리로 폐기)
- [ ] 0-7. `MonsterStatDB.csv` 추가 컬럼 `Evade, Accuracy, CritRate` (없으면 5/95/0), `CharacterStatDB.csv` 추가 컬럼 `BaseEvade, BaseAccuracy, BaseCrit` (게오르크·홍단향 5/95/5, 에리스 10/95/10)
- [ ] 0-8. BalanceDB 키 목록 확정: `CRIT_DAMAGE_PERCENT(150)`, `CRIT_DEFENSE_PERCENT(50)`, `CRIT_DEFENSE_FLAT_REDUCE(10)`, `EQUIP_OPTION_VALUE_PERCENT_ADVANCED/RARE/LEGEND`, `EQUIP_GRADE_WEIGHT_NORMAL/ADVANCED/RARE/LEGEND`, `EQUIP_LEGEND_MIN_HAZARD`, `EQUIP_ELITE_GRADE_BONUS`, `EQUIP_BOSS_GRADE_BONUS`, `EQUIP_REROLL_COST_BASE`, `EQUIP_REROLL_COST_PER_GRADE`, `POTION_BUFF_TURNS`

**완료 기준**: 위 CSV 헤더와 enum이 기획서 「장비 옵션」「아이템」 절에 "구현 형식" 줄로 적혀 있다.

---

## Phase 1 — 보조 스탯·판정 순서 (명중 → 회피 → 치명)

**목표**: 장비와 무관하게 기본치만으로 MISS와 치명타가 전투에서 발생한다.

- [ ] 1-1. 스탯 로드 — `CharacterStatData.Entry`에 `baseEvade/baseAccuracy/baseCrit`, `MonsterData`에 `evade/accuracy/critRate` (0-7 컬럼, `CsvTable` 옵션 컬럼 처리로 미존재 시 기본값)
- [ ] 1-2. `GamePlayer` 합산 프로퍼티 추가 — `TotalEvade / TotalAccuracy / TotalCritRate / TotalCritDamage` (지금은 기본치 + 버프, Phase 3에서 장비 가산). 보조 스탯은 레벨 성장 없음 (기획)
- [ ] 1-3. `BattleActions.RollHit(attacker, defender)` — 명중 판정: 유효 회피 = max(0, 회피 − max(0, 명중 − 100)); 명중 실패 또는 회피 성공이면 MISS. 서버에서만 굴린다
- [ ] 1-4. `BattleActions.RollCrit(attacker)` + 치명 피해: 피해 × `CRIT_DAMAGE_PERCENT`, 방어력은 `CRIT_DEFENSE_PERCENT`(50%) 적용 후 `CRIT_DEFENSE_FLAT_REDUCE`(10) 차감(0 하한) — `ApplyDefenseFormula(damage, defenseStat, defensePercent, flatReduce)` 시그니처 확장
- [ ] 1-5. 판정 삽입 지점
      - 플레이어 → 몬스터: `BattleActions.AttackTarget` 진입부 (약점 판정 전). MISS면 피해·분노·약점·피격 모션 모두 생략
      - 몬스터 → 플레이어: `SpawnedMonster.GeneralAttack` → `TargetObject.DamageToPlayer` 호출 전. `DamageToPlayer`에 `isCritical` 파라미터 추가 (방어 50% 적용)
      - 스킬 코루틴(`SkillData.*`)은 `AttackTarget`을 경유하므로 자동 적용. `StaticDamage`(고정 피해)는 판정 없음
      - 회복 치명: `TargetObject.HealPlayer(value, healer)` — 시전자 치명률로 굴려 `CRIT_DAMAGE_PERCENT` 배율. 물약 회복도 포함
- [ ] 1-6. 표시 — `M_EffectManager.DisPlayeDamage` 확장: MISS 텍스트(피격 모션·셰이크 없음), 치명타 숫자 강조(색·크기). `RpcDisplayZeroDamage`와 같은 패턴으로 `RpcDisplayMiss`
- [ ] 1-7. 버프 훅 — 물약(Phase 4)과 장비(Phase 3)가 쓸 `BuffType.POTION_CRIT / POTION_EVADE` 추가, `GetBuffValue`로 1-2 합산에 가산. 공격력 물약은 기존 `ICHI_ATTACK` 재사용
- [ ] 1-8. 디버그 — `GamePlayer.DebugStats`에 보조 스탯 4종 표시·조정, 치명/회피 100% 치트로 판정 검증

**완료 기준**: 에리스 회피 10%로 몬스터 공격이 가끔 MISS, 치명타 시 배율·방어 50%가 로그로 확인된다. 홍단향 회복 스킬에 치명 회복 발생.

---

## Phase 2 — 장비 인스턴스·옵션 DB

**목표**: 장비가 등급과 랜덤 옵션을 가진 인스턴스로 생성·보유·저장된다. 아직 옵션 효과는 스탯 합산에만 반영 (일반 옵션).

- [ ] 2-1. `EquipGrade`, `EquipOptionType` enum 추가 (`Common/ProjectD.cs`) — 0-1, 0-2
- [ ] 2-2. `EquipOptionDB.csv` + `DB/EquipOptionData.cs` (BalanceData 패턴). `GetRollable(slot, isLegend)` — 슬롯 허용·전설 여부 필터
- [ ] 2-3. `EquipDB.csv` 개편 (0-4) + `EquipData.Def` 필드 정리. 기존 16행을 새 컬럼으로 이관 — AC3 숙련의 부적은 다른 악세사리로 교체 (D1)
- [ ] 2-4. `EquipInstance` 구조체 + `EquipGenerator`(서버 전용 static) — `Create(equipNo, grade)`: 등급별 옵션 수만큼 `EquipOptionDB`에서 Weight 가중 무중복 추출, Min~Max 굴림. 전설은 마지막 1개를 IsLegend 풀에서. 악세사리 노멀은 옵션 1개
- [ ] 2-5. `GamePlayer.Equipment.cs` 전환 — `equippedItems / inventoryEquips`를 `SyncList<EquipInstance>`로. `CmdEquip/CmdUnequip`은 `instanceId` 기준. `SumEquip`이 기본 옵션(EquipDB) + 인스턴스 옵션(optionTypes)을 함께 합산
      - `Total*`에 옵션 가산: 힘·지능·민첩·제어·방어·마방. `MAX_HP/MAX_MP/MAX_RAGE`는 `ApplyEquipMaxDeltas`로 착탈 시 델타 (기존 방식). `TotalControl` 신설 → `GainRageByDamage`·MP 회복이 `control` 대신 사용
      - 무기 공격력 물리/마법 분리 — `EquipAttackBonusFor`가 `physicalAttack`/`magicAttack`을 캐릭터 `AttackStat`에 맞춰 선택
- [ ] 2-6. D1 반영 — `EquipSkillLevelBonus` 삭제, `GamePlayer.SkillTree.GetSkillLevel`에서 장비 가산 제거
- [ ] 2-7. 초기 지급 `ServerGrantInitialGear` — 기본 무기를 NORMAL 인스턴스로 생성
- [ ] 2-8. 세이브 — `GameSaveService.ProfileData`의 `equippedItems/inventoryEquips`를 `List<EquipInstanceSave>`로 (instanceId·equipNo·grade·옵션 배열). **구 세이브 호환**: 문자열 항목이면 NORMAL 옵션 0개 인스턴스로 변환
- [ ] 2-9. 임시 OnGUI 창(`DrawEquipmentGUI`) — 등급 색·옵션 목록 표시 (정식 UI는 Phase 6). 디버그 치트: 등급 지정 장비 생성 버튼

**완료 기준**: 치트로 만든 전설 장비가 옵션 3개를 갖고, 착용 시 스탯창 수치가 오르며, 저장·이어하기 후 옵션이 그대로다. 멀티 클라이언트가 호스트 캐릭터의 장비 옵션을 같은 값으로 본다.

---

## Phase 3 — 옵션 효과 배선 (전설 옵션)

**목표**: 기획서 전설 옵션 12종이 실제 전투에 작동한다. 각 옵션은 `GamePlayer.EquipOptionSum(type)` 한 함수로 조회하고, 적용 지점만 다르다.

| 옵션 | 적용 지점 | 항목 |
|---|---|---|
| 치명타 확률 / 명중률 / 회피율 | `TotalCritRate / TotalAccuracy / TotalEvade` 합산 (Phase 1) | [ ] 3-1 |
| 치명타 데미지 | `TotalCritDamage` → `RollCrit` 배율 가산 | [ ] 3-2 |
| 생명력 흡수 | `BattleActions.AttackTarget` — `DamageToMonster`가 돌려준 실제 피해 × % → `HealPlayer` (반환값 필요: `DamageToMonster`가 int 반환하도록) | [ ] 3-3 |
| 스킬 추가 데미지 | `BattleActions.SkillDamage` 최종 배율 (+%) — 기본 공격 제외 | [ ] 3-4 |
| 콤보 추가 데미지 | 합산·표시만. 적용은 연계 구현 시 (D10) | [ ] 3-5 |
| 반사 데미지 (고정 / 비례) | `TargetObject.DamageToPlayer` — HP 피해 발생 후 `CounterAttack`과 같은 경로로 `tpActingUnit` 몬스터에 고정치 + 받은 피해 × %. 반격(GS13)과 합산 | [ ] 3-6 |
| 받는 회복량 증가 | `TargetObject.HealPlayer` — 대상의 옵션 합으로 배율 (치명 회복 배율과 곱) | [ ] 3-7 |
| 분노 최대치 증가 | `ApplyEquipMaxDeltas` — 자원이 RAGE인 캐릭터만 (MP 캐릭터가 착용하면 무효, 툴팁에 표시) | [ ] 3-8 |
| 보호막 (악세사리) | 전투 시작(`BATTLE_INITIALIZE`, 아이템 `STARTBATTLE` 발동 지점) 시 `GainDefense(값)` | [ ] 3-9 |
| 포지션 패널티 제거 | `BattleActions.RowAttackPercent`(후열 80 → 100) / `RowIncomingDamagePercent`(전열 120 → 100) — 보너스는 유지 (D8) | [ ] 3-10 |
| 이동 시 턴 소모 제거 | `M_TurnManager.ExecutePlayerTpTurn` `TpAction.MOVE` — 옵션 보유 시 `freeAction = true`, 턴당 1회. 중열 무료 이동(기획)도 같은 분기 (D9) | [ ] 3-11 |

- [ ] 3-12. 옵션 스택 규칙 — 같은 옵션이 여러 장비에 붙으면 합산 (플래그형 FREE_MOVE/NO_ROW_PENALTY는 OR). 퍼센트 옵션 상한은 BalanceDB(`EVADE_CAP` 등)로 두되 1차는 무제한
- [ ] 3-13. 검증용 치트 — 특정 옵션 하나만 붙은 전설 장비 생성 (옵션 타입 지정)

**완료 기준**: 옵션 12종 각각을 치트 장비로 붙여 전투 로그에서 효과가 확인된다. 옵션 없는 장비의 전투 결과는 Phase 1과 동일 (회귀 없음).

---

## Phase 4 — 물약 개편

**목표**: 기획서 「아이템」 절의 물약 7종(HP·MP·치명타·회피·공격력·분노·극약)이 전투와 맵에서 작동한다.

- [ ] 4-1. `ConsumableType` 개편 (0-6) + `ConsumableDB.csv` 7행 — `RESTORE_RESOURCE` 폐기, MP 물약은 MP 캐릭터에게만, 분노의 물약은 게오르크에게만 유효 (다른 캐릭터는 사용 버튼 비활성)
- [ ] 4-2. `ConsumableData.Def`에 `duration, battleOnly, dropWeight` 추가
- [ ] 4-3. 사용 로직 통합 — `GamePlayer.ServerApplyPotion(def, TargetObject target)` 한 곳으로 (현재 `CmdUsePotionOnMap`과 `TpBattle.cs` ITEM 분기에 중복된 switch를 합친다)
      - `BUFF_*`: `GainBuff(BuffType.POTION_CRIT / POTION_EVADE / ICHI_ATTACK, value, 지속 = duration)`, 전투 전용, `TickTimedDebuffs`로 자기 턴 종료마다 감소
      - `POISON_TO_ONE`(극약): `SetPlayerHP(1)` + 게오르크면 잃은 HP 기준 `GainRageByDamage`, 에리스는 `UpdateErisMode`가 자동 광기 (D6). 전투 전용, HP 1이면 사용 불가
      - `HEAL_HP`: `HealPlayer` 경유로 치명 회복·받는 회복량 옵션 적용
- [ ] 4-4. 맵 사용 제한 — `battleOnly=1`은 `CmdUsePotionOnMap`에서 거부(토스트 `ui.msg.potion_battle_only`)
- [ ] 4-5. 초기 지급 조정 — `ServerGrantInitialGear` HP 물약 2개 유지, 게오르크에게 분노의 물약 1개 시험 지급 (테스트 후 결정)
- [ ] 4-6. 전투 UI(OnGUI 아이템 목록) — 캐릭터에 무효한 물약 비활성 + 사유 표시

**완료 기준**: 치명타 물약 사용 후 n턴 동안 치명률 상승이 스탯창에 보이고, 극약 사용 시 에리스가 즉시 광기 변신한다. 맵에서 버프 물약은 사용되지 않는다.

---

## Phase 5 — 드랍·제작 (메르크리우스)

**목표**: 장비가 등급을 굴려 드랍되고, 위험도 하한선이 전설 드랍을 가른다. 메르크리우스 상단에서 골드로 구매·재감정한다.

- [ ] 5-1. 등급 굴림 `EquipGenerator.RollGrade(monsterGrade, hazard)` — BalanceDB 가중치 + 엘리트/보스 보너스, `hazard < EQUIP_LEGEND_MIN_HAZARD`면 LEGEND 제외 (기존 `EQUIP_DROP_MIN_HAZARD`는 드랍 자체의 하한으로 유지)
- [ ] 5-2. `RewardService.DistributeBattleRewards` — `ServerAddRandomEquip`이 `DropWeight`·요구 레벨 필터 후 5-1 등급으로 인스턴스 생성. 멀티 드랍 1.5배(`ITEM_DROP_MULTIPLAYER_PERCENT` 신설, 기획 「멀티플레이어 변경점」)
- [ ] 5-3. 물약 드랍 — `ServerAddRandomConsumable`이 `DropWeight` 가중 추출 (극약·버프 물약은 낮게)
- [ ] 5-4. 보상 목록 표시 — `BattleResultPopUp`/`Reward` 항목에 드랍 장비 이름·등급 색 (현재 로그만)
- [ ] 5-5. 메르크리우스 UI — `NPC_Mercurius`의 숨긴 버튼을 "상단" 하나로 살리고 `ItemShopPopUp`(현재 그림자꾼이 열던 구 카드 상점 팝업)을 장비 상점으로 재배선: 탭 1 구매(노멀·고급 고정 목록, `EquipDB.Price`), 탭 2 재감정(보유 장비 선택 → 골드 → 옵션 재굴림, 등급 유지), 탭 3 판매(등급별 가격)
      ※ `NPC_ShadowMan`의 `buttonItemShop` 연결은 스킬 초기화 UI로 바뀔 때까지 그대로 둔다
- [ ] 5-6. `CmdBuyEquip / CmdRerollEquip / CmdSellEquip` — 서버 검증(골드·거점 상태·소유), 비용은 BalanceDB (D7)

**완료 기준**: 위험도 0에서 전설이 나오지 않고 하한 이상에서 나온다. 엘리트 처치 후 고급 이상 비율이 눈에 띄게 오른다. 재감정으로 옵션이 바뀌되 등급은 유지된다.

---

## Phase 6 — UI·로컬라이즈

**목표**: OnGUI 임시 창을 정식 인벤토리·장비 팝업으로 교체하고, 옵션·물약 문자열을 로컬라이즈한다.

- [ ] 6-1. 로컬라이즈 키 — `equip.option.<EquipOptionType>.name` / `.format`("{0}%" 등), `equip.grade.<EquipGrade>`, `potion.<PotionNo>.name/.desc`, `ui.msg.miss`, `ui.msg.potion_battle_only`. 전 로케일 CSV 등록 (`Document/LOCALIZATION.md`)
- [ ] 6-2. 장비 툴팁 컴포넌트 — 이름·등급·슬롯·기본 옵션·추가 옵션(전설 옵션 별색)·요구 레벨·무효 옵션 경고(분노 최대치를 MP 캐릭터가 착용 등)
- [ ] 6-3. 인벤토리 팝업(`UI/PopUpComponent/InventoryPopUp`) — 슬롯 6칸(무기·갑옷·투구·신발·악세사리×2) + 보유 장비 그리드 + 소모품 탭. `PopUpUIManager` 등록, 스킬트리 팝업과 상호 배타. 비교 표시(장착 중 vs 선택)
- [ ] 6-4. 전투 아이템 액션 — OnGUI 목록을 소모품 버튼 바로 (물약 아이콘·수량·비활성 사유)
- [ ] 6-5. 스탯창(`DebugStats`) 정리 — 보조 스탯 4종·제어를 정식 스탯 표시로 승격, 장비 기여분을 괄호로
- [ ] 6-6. `DrawEquipmentGUI` OnGUI 제거

**완료 기준**: OnGUI 없이 장비 착탈·물약 사용·툴팁 확인이 가능하고, 로케일을 바꾸면 옵션 이름이 바뀐다.

---

## Phase 7 — 세이브·멀티·밸런스 검증

- [ ] 7-1. 구 세이브 3슬롯 로드 회귀 (문자열 장비 → NORMAL 인스턴스 변환, 구 `RESTORE_RESOURCE` 물약 → MP/분노 물약 매핑 또는 폐기)
- [ ] 7-2. ParrelSync 2클라이언트 — 클라이언트 캐릭터의 드랍·착탈·재감정·물약 사용이 호스트 검증을 거쳐 반영되는지, `SyncList<EquipInstance>` 초기 동기화(늦게 들어온 클라이언트)
- [ ] 7-3. RPC 해시 충돌 점검 — 새 `Cmd*/Rpc*` 추가 후 콘솔 "have the same hash" 확인
- [ ] 7-4. 밸런스 초안 — 옵션 Min/Max, 등급 가중치, 전설 위험도 하한, 재감정 비용을 1스테이지 플레이로 조정. 결과를 `RPG_CONVERSION_DESIGN.md`에 기록
- [ ] 7-5. 기획서 갱신 — 「장비 옵션」「아이템」 절에 구현 줄 추가, D1~D10 결정 반영, `RPG_CONVERSION_ITEMS.md`(현재 빈 문서)에 옵션 표·물약 표·드랍 표를 옮긴다

---

## 폐기·정리 목록

- `EquipDB.csv` `Grade/Agility/MaxHP/MaxResource/SkillLevel` 컬럼, `EquipData.Def.skillLevel`, `GamePlayer.EquipSkillLevelBonus` (D1, Phase 2)
- `ConsumableType.RESTORE_RESOURCE` (Phase 4)
- `SyncList<string>` 장비 인벤토리와 문자열 세이브 형식 (Phase 2, 변환기만 남김)
- `GamePlayer.Equipment.cs`의 OnGUI 창 (Phase 6)
- 카드 시대 `ItemDB.csv`/`ArtifactDB.csv`/`GamePlayerItem`(이치 문장 등)은 이번 개편 범위 밖 — 아티팩트 개편 시 별도 처리

## 의존 관계

```
Phase 0 ─┬─ Phase 1 (판정) ──┐
         └─ Phase 2 (인스턴스) ─┴─ Phase 3 (옵션 효과) ─┬─ Phase 5 (드랍·제작) ─┐
                                 Phase 4 (물약) ────────┘                       ├─ Phase 6 (UI) ─ Phase 7
                                                                                 ┘
```
Phase 1과 2는 서로 독립이라 병행 가능. Phase 4는 Phase 1의 버프 훅(1-7)만 있으면 착수 가능.
