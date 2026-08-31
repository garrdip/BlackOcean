# RPG 전환 구현 워크플로우

> 기준 문서: `RPG_CONVERSION_DESIGN.md`(기획), `RPG_CONVERSION_PLAN.md`(코드베이스 조사·전환 전략)
> 원칙: **매 단계가 끝날 때마다 게임이 실행 가능한 상태**를 유지한다. 전투처럼 큰 개조는 캐릭터 1명 수직 슬라이스로 먼저 검증한다. 멀티 검증은 ParrelSync 2클라이언트 기준.

---

## Phase 0 — 월드맵 지형 기반 → ❌ 폐기 → **거점(Hub) 전환 ✅ (2026-08-25)**

> **맵 타일 시스템 전면 제거**: 기획서(`RPG_CONVERSION_DESIGN.md` "맵" 절)의 Darkest Dungeon 방식
> — "거점에서 할일하고, 스테이지 진입" — 에 맞춰 2D 육각형 맵·3D 구체 맵을 모두 삭제하고
> GameScene을 **거점 화면**으로 바꿨다. 거점에는 NPC 4종(류진솔·소피아·메르크리우스·그림자꾼)만
> 화면 가로 전체에 넓게 상주하고(파티 아바타 없음 — 기획 확정), 스테이지 진입 시 같은 화면(BattleScene 루트)에서 전투로 전환된다.
> 류진솔/소피아 치유는 아바타 클릭 대상 선택 대신 자신의 캐릭터 즉시 회복(거점 방문당 1회)으로 변경. 골드 전달은 대상 선택 불가로 사실상 비활성 (6-2 NPC 개편 대상).
>
> - **화면 루트 2층 구조**: 거점 루트 `Hub`(배경 + 집 앵커 `House_RyuJinSol/Sophia/Mercurius/ShadowMan`) ↔ 전투 루트 `Game`.
>   옛 맵의 MapScene/BattleScene 토글과 같은 구조로 `M_HubManager.SetHubViewActive(bool)`이 거점→전투→거점 전환 시 루트를 바꾼다.
>   NPC는 스폰 후 자기 집 앵커 아래로 들어간다(`AttachNpcToHouse`) — 추후 "집에 들어가서 수행" 방식(집 클릭 → 실내)은 이 앵커 기준으로 확장
> - 신규 `Mangers/M_HubManager.cs` (씬 오브젝트 `M_HubManager`, 구 `M_MapManager` 자리):
>   `EnterHub()` 거점 진입(NPC 스폰, `NONE_BATTLE_SCENE`), `StartStageBattle(roomType, hazard)` 전투 진입,
>   집 앵커 `houses`(인스펙터, NPC 스폰 위치), `hubCameraSize`, 임시 OnGUI 스테이지 진입 버튼(호스트, 우하단 — 정식 스테이지 선택 UI 전까지)
> - 흐름: 로딩 `HUB_SCENE` → `EnterHub` → (임시 버튼) `StartStageBattle` → 전투 → 보상 → `NoneBattleEnd`(자동 저장) → `EnterHub`
> - `M_TurnManager.GenerateBattleObject(RoomType, hazard)` / `GenerateHubObject`, `BattleSpawner.GenerateMonster(hazard)` / `GenerateHubNPCs`
> - `playerOrder` 등록은 `MapPlayer.OnStartServer` → `PlayerInterfaceServer.GenerateGamePlayerOwnedObjects`(`M_TurnManager.RegisterPlayerOrder`)로 이관
> - 삭제: `Script/Map`, `Script/Map3D`, `Script/Mesh`, `M_MapManager`, `GamePlayerMap`, `ReadyButtonOnMap`, `Prefabs/Map`, `MapPlayer.prefab`,
>   `Resources/Map`, `GeneratedMeshes`, 맵 셰이더 2종, BalanceDB `USE_3D_MAP`/`MAP_SPECIAL_TILE_PERCENT`, `RoomType`의 타일 전용 값
> - 저장(`GameSaveService`)은 프로필만 (월드 상태 없음). 로드는 `M_HubManager.OnStartServer`가 수행
> - 잔여: NPC의 워프 버튼(폐기, Awake에서 숨김) → 프리팹 정리, 메르크리우스는 카드 상점 잔재(빈 상점) → Phase 6-2 상점 개편에서 아티팩트 상점으로
> - 행동비용 타이머/보스 추격/타일 리스폰 등 맵 탐험 규칙은 모두 소멸 — 위험도(hazard)는 스테이지 진입 파라미터로만 남음

**후속 (Phase 6에서 처리)**: 스테이지 선택 UI(1-1/1-2/1-3 → hazard 매핑), 던전(연속 전투), `MonsterGroupDB.csv` hazard 범위 확장, EVENT 콘텐츠.

---

## Phase 1 — 데이터·스탯 기반 ✅ (완료)

**목표**: 기획서의 스탯/속성/레벨 체계를 데이터로 정의한다. 기존 CSV+리플렉션 파이프라인(`DB/CsvTable.cs`) 재사용.

- [x] 1-1. 스탯 6종 — `GamePlayer`에 SyncVar 추가: level/exp + 힘/민첩/체력/지능/방어력/마법방어. `AddExp()` 레벨업 루프 + 성장치 반영, MaxHP = `PLAYER_INIT_HP` + 체력×`HP_PER_VITALITY`
      **체력(Vit) 스탯 폐기 (2026-08-31)**: 최대 HP 외 용도가 없어 제거. `GamePlayer.vitality`/`GetMaxHPByVitality`/`HP_PER_VITALITY`/세이브 `vitality` 삭제, `CharacterStatDB` BaseVit 컬럼 제거·GrowVit → `GrowHP`(×2 환산, HP 총 성장치).
      `GamePlayer.AddMaxHP()`를 레벨업(`LevelGrowthTable.Stat.HP`)/스킬트리 `HP` 노드(구 VIT)가 공유. 레벨업 팝업 7행(HP+6스탯)
- [x] 1-2. 속성 enum — `AttackAttribute { NONE, SLASH, STRIKE, PIERCE, MAGIC, RESONANCE }` + `BattleResourceType { NONE, RAGE, MP, HP }` (`Common/ProjectD.cs`)
- [x] 1-3. `LevelDB.csv` + `CharacterStatDB.csv`(기본치/성장치/약점·내성/자원) 신설. 로더: `DB/LevelData.cs`, `DB/CharacterStatData.cs` (BalanceData 패턴)
      **레벨업 시스템 확장 (2026-08-25)**: `LevelDB.csv` 1~99 (필요 EXP = 80×L + 20×L², 99 = 최대; 누적 약 676만) / `CharacterStatDB.csv`에 `SkillPointPerLevel`(1) + `BonusSkillPointEvery`(10 — 10레벨마다 +1) —
      레벨업 시 성장치(GrowX) 반영 + 포인트 지급 + 소유자 토스트(`ui.msg.level_up`), 최대 레벨이면 EXP 미적립 / `MonsterStatDB.csv`에 `Exp`(일반 12~26, 엘리트 50~60, 보스 200~220) —
      처치 시 `M_TurnManager.battleExpPool` 적립 → 전투 종료 시 파티원 각자 전액 지급(멀티 1.5배). 합 0이면 `BATTLE_REWARD_EXP` 폴백. 보스/NPC 스폰 경로도 `SpawnedMonster.monster` 참조 설정
      **성장치 랜덤 분배 (2026-08-31)**: `GrowX`를 "레벨당"에서 "만렙까지 총량"으로 변경. `DB/LevelGrowthTable.cs`가 `GamePlayer.growthSeed`(생성 시 부여, 세이브 `growthSeed`) 기준으로 총량을 98칸에 가중치 비례(정수 최대 나머지법) 분배 —
      레벨별 상승량은 랜덤, 만렙 합은 고정. 편차 `BalanceDB LEVEL_GROWTH_VARIANCE_PERCENT`. `ApplyLevelUpGrowth()`가 도달 레벨 칸을 읽음
      **레벨업 연출 (2026-08-31)**: `UI/PopUpComponent/LevelUpPopUp.cs` — 런타임 구성 팝업(씬/프리팹 참조 없음, 정식 프리팹으로 교체 가능). `RpcLevelUp`이 전/후 스탯 스냅샷(`GamePlayer.LevelUpStats`)을 전달 →
      소유자 화면에 "현재 값 (+N)" 스탯표(HP+7종)를 행별 시차로 표시, 확인/Enter로 닫음. 다중 캐릭터 레벨업은 큐로 순차 표시. 구 토스트(`ui.msg.level_up`)는 팝업으로 대체. 키: `ui.levelup.*`, `ui.stat.*`
- [x] 1-4. 몬스터 확장 스탯 — **`MonsterDB.csv` 직접 확장 대신 별도 `MonsterStatDB.csv`** (위치 기반 포맷 보호). 약점(복수 `|` 구분)/공격 속성/TP 실드. `MonsterData.LoadMonsterStatFromDB()`로 병합, 미등록 몬스터는 기본값
- [x] 1-5. 캐릭터 약점/내성 — `CharacterStatDB.csv`에 포함 (게오르크 약점MAGIC/내성SLASH, 홍단향 약점PIERCE/내성MAGIC, 에리스 약점STRIKE/내성RESONANCE)
- [x] 1-6. 자원 구조 — `GamePlayer.currentResource/maxResource` SyncVar + `BattleResourceType`. 초기값: 분노 0에서 시작, MP 가득, HP형은 max 0(자신의 HP 소모). **소모/충전 로직과 이치 대체는 Phase 2B에서** (전투가 아직 카드 기반이므로 이치는 그대로 둠)
- [x] 1-7. 경험치 — `Reward_Type.Exp` 추가, `RewardService.DistributeBattleRewards`에서 서버 즉시 지급. `BATTLE_REWARD_EXP`(20) × 멀티 시 `EXP_MULTIPLAYER_PERCENT`(150%). 보상 목록 UI 표시는 Phase 2로

**완료 기준**: 전투 없이도 인스펙터/로그로 스탯·레벨업·EXP 지급이 확인된다. → GamePlayer 인스펙터에서 스탯 확인, 전투 승리 시 EXP 지급·레벨업 로그 출력. (에디터 플레이 검증 필요)

---

## Phase 2 — 전투 코어 개조 (최대 리스크 — 수직 슬라이스 우선)

**목표**: 카드 전투 → TP 기반 턴제 커맨드 전투. **에리스 1명 + 몬스터 1종**으로 끝까지 도는 것을 먼저 만든다.

### 2A. 턴 시스템: BattleTurn → TP 게이지
- [x] 2A-1. TP 시스템 — `M_TurnManager.TpBattle.cs` 신설. 유닛별 TP가 민첩 비례 충전, 100 도달 시 턴 (이월분 유지 → **다회 턴 상한 없음**, 민첩 수치로 밸런싱 — 기획 확정). 진입: `BattleInitialize`가 `USE_TP_BATTLE`(BalanceDB)이면 카드 페이즈 대신 TP 루프 시작
- [ ] 2A-2. 타임라인 UI — 미구현 (현재는 OnGUI 텍스트로 현재 턴만 표시). 정식 UI 작업 시 진행
- [x] 2A-3. ~~PLAYER_END 전이 회수~~ → **불필요해짐**: 카드 페이즈 상태머신 자체를 우회 (TP 루프는 `M_CardManager` 경로를 타지 않음). 카드 코드 제거(2B-5) 시 함께 정리
- [x] 2A-4. 몬스터 AI 연결 — `SetNextAction`(예고)/`DoAction`(실행)을 TP 턴에서 그대로 구동 (기존 `MonsterActionSeuqence` 대기 규약 유지)

### 2B. 액션 시스템: 1턴 1액션
- [x] 2B-1. 액션 5종 — 공격/방어/스킬/이동(전열·중열·후열) 구현, 아이템은 스텁(Phase 4 소모품과 연결). **UI는 OnGUI 임시 액션 바** — 정식 UI는 후속
- [x] 2B-2. 실행 델리게이트 — `ExecuteSkill(SkillDef, user, targets)` 신설 + `SkillDB.csv` + `SkillData` 정적 리플렉션 바인딩 (CardData 패턴 계승, 카드 델리게이트는 2B-5까지 병존)
- [x] 2B-3. 검증 — TP 턴제는 동시 입력이 없어 **큐 대신 단일 턴 제출**(`CmdSubmitTpAction`: 자기 턴 검증 + 커넥션 검증 + 서버 코스트 지불). `GamePlayerSkill` 보유 시스템은 Phase 3 스킬트리에서 (현재는 캐릭터 전체 스킬 사용 가능)
- [x] 2B-4. 데미지 공식 — `BattleActions` 신설: 스탯(힘/지능)×계수%×대열 보정, 약점 배율, 받는 피해 대열 보정+방어력 스탯 경감(`DamageToPlayer` 통합, TP 전투 한정)
- [ ] 2B-5. 카드 시스템 제거 — 미착수 (USE_TP_BATTLE 토글로 우회 중). TP 전투 검증 후 일괄 제거

### 2C. 약점·TP 브레이크·포지션
- [x] 2C-1. 약점 판정 — `WEAKNESS_DAMAGE_PERCENT`(120%) + 대상 TP 감소 `TP_BREAK_BASE`(30) 반복 시 절반씩 체감(`TP_BREAK_MIN` 하한)
- [x] 2C-2. 포지션 효과 — 전열 공+20%/피격+20%, 후열 공-20%/피격-20% (BalanceDB 변수), 이동 액션으로 열 변경(`SwapPlayerOrder` 재사용). 중열 특전(위치 변환 무료)은 1턴 1액션 구조 확정 후 재설계 필요
- [ ] 2C-3. 몬스터 행동 예고에 공격 속성 표시 — 미구현 (`NextActionIndicator` 확장 필요, 데이터는 `MonsterStatDB.AttackAttribute`로 준비됨)

**완료 기준(= 마일스톤 M1)**: 에리스 1명 vs 몬스터 1종 전투가 TP 턴·5액션·약점·포지션 규칙으로 3인 멀티에서 완주된다.

### 2D. 캐릭터 확장
- [x] 2D-1. 기본 스킬 — 게오르크 "고행길"(HP 8 소모 → 분노 40, GS1), 에리스 "피의 가속"(HP 소모 → TP+30, ES2). **홍단향 "철귀 이동"은 철귀 시스템 재설계와 함께 후속** (현재 치유/실드 지원가 킷으로 대체)
- [x] 2D-2. 자원별 스킬 코스트 — 분노: 피해를 주면 +5/받으면 +10 충전, 전투 시작 시 0 리셋 / MP: 자기 턴 시작 시 +5 자연 회복 / HP: 빈사 가드 포함 소모. 카드 효과 코루틴 대량 이관은 Phase 3 스킬트리 볼륨 작업에서
- [x] 2D-3. 3캐릭터 전원 전투 가능 — 게오르크 5스킬(분노), 홍단향 4스킬(MP, 아군 대상 회복/실드 타겟팅 포함), 에리스 4스킬(HP). 임시 UI에 아군 선택·약점 힌트 추가

---

## Phase 3 — 스킬트리 (드퀘식) ✅ (1차 완료)

**목표**: 레벨업 포인트로 트리를 찍는 성장 시스템.

- [x] 3-1. `SkillTreeDB.csv` — 캐릭터당 2트리×4티어 (게오르크 연격/수호, 홍단향 개화/회복, 에리스 은하수/별무리). 로더 `DB/SkillTreeData.cs`. 습득 상태는 `GamePlayer.learnedNodes`(SyncList) + `skillPoints`(레벨업당 +1, 시작 1). **트리 볼륨 확장(기존 카드군 압도/쇠락/기사도 등 이관)은 후속 콘텐츠 작업**
- [x] 3-2. 노드 타입 — SKILL(액티브 습득) / SKILL_LEVEL(강화 — 노드당 피해 +20%) / STAT(스탯 상승, 체력은 MaxHP 재계산) / ULTIMATE. 미습득 스킬은 전투 UI 미표시 + 서버 `KnowsSkill` 재검증 (기본 스킬 고행길/피의 가속은 innate)
- [x] 3-3. 필살기 3종 — 대장군의 일격(단일 300%)/만개(전원 회복+실드)/초신성(전체 180%). **리스크 규칙: 사용 후 자신의 TP -50** (`ULTIMATE_TP_PENALTY`)
- [x] 3-4. 스킬트리 UI — OnGUI 임시 창 (우상단 버튼 토글, 트리별 노드 목록 + 습득/잠김 상태). 정식 팝업(`PopUpUIManager`)은 UI 정리 단계에서
      우상단 디버그 버튼 줄(2026-08-31): `스탯`(`GamePlayer.DebugStats.cs` — 레벨/EXP/HP/자원/스탯 6종 기본·장비·합계/다음 레벨 성장치/성장 시드) · `위험도 +1` · `레벨업` · `스킬트리` / 아래 `장비`. 창 3종은 같은 자리라 상호 배타

**완료 기준**: 레벨업 → 포인트 획득 → 트리 습득 → 전투에서 사용까지 루프가 돈다. → 구현 완료, 에디터 검증 필요.

---

## Phase 4 — 장비·아이템 ✅ (1차 완료)

**목표**: 장비 슬롯/소모품 시스템 구축. (기존 `Item`+`ItemEffectTime` 훅은 유물 전용으로 유지 — 장비는 스탯 가산형 독립 시스템 `EquipData`+`GamePlayer.Equipment`로 분리, 특수 효과 장비가 생기면 훅 연동)

- [x] 4-1. `EquipDB.csv` — 캐릭터별 무기 2종 + 공용 방어구(갑옷/투구/신발/악세사리, 악세 2슬롯) 총 17종. 옵션: 공격(캐릭터 공격 스탯 가산)/민첩/방어/마방/최대HP/자원 최대치/스킬레벨/요구레벨/등급/가격
- [x] 4-2. 착용 로직 — `GamePlayer.Equipment.cs`: 착탈 Command(서버 검증: 소유/레벨/캐릭터 전용/슬롯 자동 교체), 합산 스탯은 `Total*` 프로퍼티(전투 코드 전체 교체), MaxHP/자원 최대치는 착탈 델타 반영. 전투 중 착탈 불가
- [x] 4-3. 소모품 — `ConsumableDB.csv`(HP물약 2종/MP물약). 전투 '아이템' 액션(TpAction.ITEM) + 맵에서 사용 모두 연결. 버프 물약은 버프 지속 시스템 정리 후
- [x] 4-4. 인벤토리/장비창 — OnGUI 임시 창 (스킬트리 아래 '장비' 버튼, 상호 배타 토글). 정식 UI는 UI 정리 단계
- [x] 4-5. 획득 — 전투 승리 드랍(장비 30%/물약 40%, 인벤토리 직행) + 시작 시 기본 무기 장착 + HP물약 2개. **상인 구매(ShadowMan 개조)는 Phase 6 상점 작업에서**

**완료 기준**: 드랍→착용→스탯 변화→전투 반영, 멀티 동기화 포함. → 구현 완료, 에디터 검증 필요.

---

## Phase 5 — 영속 저장·메뉴 (이어서 하기)

**목표**: 런 소멸 구조 제거, 저장/로드 기반 진행.

- [x] 5-1. 저장 계층 신설 — **`GameSaveService`(정적 서비스, 네트워크 독립)**: `ProfileData`(레벨/EXP/스탯/스킬트리/장비/소모품/골드/HP — SteamID 키, 전원분) + 월드 상태(맵 시드/방문 완료/시야/현재 위치). 호스트가 `rpg_save.json` 단일 파일 소유. 구 `M_SaveManager`(카드 런 스냅샷)는 비활성 (카드 제거 시 삭제)
- [x] 5-2. 자동 저장 — 탐험 스텝 확정(`CmdInstantMove`)/전투 없는 투표 이동/전투·방 정리 완료(`NoneBattleEnd`) 시 지연 저장(`ScheduleSave`). 장비·트리 변경은 다음 이동/전투 저장에 포함됨
- [x] 5-4. 세이브 슬롯 3개 (2026-08-31) — `GameSaveService.SlotCount=3`, 파일 `rpg_save_{1..3}.json` (구 `rpg_save.json`은 최초 접근 시 슬롯 1로 이관). `CurrentSlot`(PlayerPrefs `rpg_save_slot`에 마지막 사용 기억)에 모든 자동/수동 저장.
      `BeginNewGame(slot)`(기존 데이터 삭제 + 로드 예약 해제) / `BeginContinue(slot)`(로드 예약) / `Peek(slot)`(요약) / `DeleteSlot(slot)`. UI: `UI/PopUpComponent/SaveSlotPanel.cs`(런타임 구성, 씬 참조 없음) —
      메뉴 '처음부터/이어하기'(MenuUI)와 멀티 '방 만들기'(CreateLobby) 공용. 슬롯마다 저장 시각(`savedAt`)/해금 스테이지/위험도/캐릭터·레벨 요약, 덮어쓰기·삭제는 두 번 클릭 확정, Esc/바깥 클릭 취소. 키 `ui.save.*`
- [x] 5-3. 로드 진입 — 방 만들기 화면에 '이어서 하기 ON/OFF' 임시 토글 (저장 파일 있을 때만, 기본 ON). 복원: 시드+진행은 `SphereMapNetwork` SyncVar/SyncList로 전 클라이언트 전파, 프로필은 `GenerateGamePlayer`에서 SteamID 매칭 적용(새 파티원·캐릭터 변경 시 신규 초기화). **정식 메뉴 개편(싱글/멀티 4버튼)은 UI 작업에서**
- [ ] 5-4. 싱글 플레이 모드 — **결정 대기: 1인이 3캐릭터 전원 조작 권장** (전투 밸런스를 3인 고정으로 통일, 호스트 단독 세션으로 기존 네트워크 구조 재사용)
- [ ] 5-5. 사망 처리 — **결정 대기**. 현재는 사망 → 세션 종료 → '이어서 하기'로 마지막 저장 시점 재개가 사실상의 패널티로 동작
- [x] 5-6. 씬 전환 시 상태 보존 — 저장이 이동/전투 종료 시점에 즉시 파일로 내려가므로 DDOL 파괴 전 flush가 불필요해짐 (설계로 해소)

**완료 기준**: 종료 후 "이어서 하기"로 같은 월드·같은 성장 상태가 복원된다 (호스트/게스트 각각). → 구현 완료, 에디터 검증 필요.

---

## Phase 6 — 월드 콘텐츠

- [x] 6-0. 출정/스테이지 선택 (1차) — 류진솔 메뉴 "출정"(구 체력 회복 자리) → `M_HubManager.OpenStageSelect()` 스테이지 선택 패널(임시 OnGUI) → `CmdStartStage(stageNo)`.
      `StageDB.csv`(1-1~3-3: 전투 종류/위험도) + `DB/StageData.cs`. 해금은 `unlockedStageCount`(SyncVar, 처음 1 = 1-1만) — 가장 높은 해금 스테이지 클리어 시 +1(`OnStageCleared`), `rpg_save.json`에 저장/복원. **정식 팝업 UI는 UI 정리 단계에서**
- [x] 6-1. 던전(방 진행) 1차 — 스테이지 진입 시 바로 전투가 아니라 **스테이지 화면**(세 번째 화면 루트 `Stage`: 배경 + 우측하단 방 패널 `StageRoomPanel`)으로 들어간다.
      **미로 구조(Isaac/Darkest Dungeon식)** — 입장할 때마다 서버가 격자 위에 랜덤 생성(`StageData.Entry.GenerateLayout`: 입구(0,0)에서 랜덤 확장, 트리 위주·25% 고리,
      입구에서 가장 먼 막다른 방 = 출구(EXIT)/보스(BOSS)) → `M_HubManager.stageRooms`(SyncList<StageRoomInfo>: 격자 좌표/종류/클리어).
      규칙은 `StageDB.csv`의 `RoomCount(입구·출구 제외 내용 방 수) / EliteCount(막다른 방 우선) / EmptyPercent / Type(BOSS)`: 1-1 = 5방(몬스터 또는 빈방)+출구, 1-3 = 6방(엘리트 1)+보스 … **기획서 "방 개수 규칙" 절 확정 시 CSV 수치만 수정**.
      이동 규칙: 파티는 입구에서 시작, **현재 방의 인접(상하좌우) 방만 보이고 이동 가능**(`CmdEnterRoom`, 서버 `IsAdjacent` 검증). 방문한 방은 계속 보이고 자유 왕복.
      미니맵(`StageRoomPanel`, 우측하단 640×440): 옛 맵 `UI_Map/Top Icon` 재활용 — 프레임(`Based`) + 종류 아이콘 + 연결선(`Line`) + 현재 위치 핀(`PIN`), 입구는 전용 프레임(`Base C Based`) + "입구" 라벨.
      **표시 = 현재 방 + 바로 옆(상하좌우) 방만** (`showVisitedRooms`를 켜면 방문한 방도 어둡게 유지). 첫 프레임에 영역 크기가 0이면 다음 프레임에 재시도(초기 미표시 버그 가드).
      전투 방 → 전투 → 승리(`OnBattleVictory`) → 방 화면 복귀 / CAMP 방 → 파티 회복 즉시 처리 / EVENT·상점 방 → 통과 스텁(6-5). 마지막 방 클리어 → 해금 + 거점.
      "귀환"(`CmdRetreat`)으로 전투 사이 이탈 가능(진행도 폐기). **잔여**: 전투 간 MP/물약 비회복 규칙, EVENT 콘텐츠, 정식 UI
- [x] 6-1b. 화면 전환 일원화 (2026-08-25) — 거점/스테이지/전투 전환은 모두 `M_TurnManager.Spawner`의 서버 코루틴이 **페이드 아웃(`M_DimmingManager.RpcFadeOut`, screenFade 이미지 0.8s) → 검은 화면 뒤에서 이전 타겟오브젝트 정리·루트 전환(`RpcSetView`)·새 오브젝트 스폰 → 스폰 반영 대기 0.35s → 페이드 인** 순서로 수행.
      스폰/파괴가 화면에 노출되지 않아 과도현상 없음. 전환 중 `isSceneTransitioning`으로 출정/방 이동/귀환 입력 차단. `NoneBattleEnd`는 더 이상 타겟오브젝트를 직접 지우지 않는다.
      GameCanvas의 카드 전투 UI(`CostMenu`: 이치 표시·뽑을덱 / `TurnMenu`: 턴 종료·버린덱·잊혀진덱) 삭제 — 카드 잔재 코드는 `GameUIManager.HasCardUI`/`SetEndTurnInteractable`로 가드 (2B-5에서 함께 제거)
- [ ] 6-2. 상인 — 거점 NPC 4종 역할 개편 (류진솔 아이템 / 소피아 장비 / 메르크리우스 아티팩트 / 그림자꾼 스킬 초기화). 기존 캠프·아이템상점·카드상점 팝업 골격 재활용
- [ ] 6-3. 보스 — 스테이지 마지막 단일 전투 (맵 추격 보스는 맵 제거로 소멸)
- [ ] 6-4. 스테이지 세분화 — 1-1/1-2/1-3 → hazard 매핑 (`MonsterGroupDB.csv` 범위 확장 필수)
- [ ] 6-5. EVENT 콘텐츠 — 던전 진행 중 이벤트 (`RoomType.EVENT_*` + `RpcStartNoneBattleEvent` 재활용)

---

## Phase 7 — 밸런스·마무리

- [ ] 7-1. 수치 밸런스 — 성장곡선/데미지 공식/TP 충전 속도/약점 배율 (`BalanceDB.csv` 변수화)
- [ ] 7-2. 로컬라이제이션 — `skill.*`, `equip.*`, `ui.*` 키 추가 (7개 언어, `Document/LOCALIZATION.md` 체계)
- [ ] 7-3. 구 시스템 청소 — 2D 헥사곤 맵, 카드 UI 잔재, 미사용 BuffType(~15종), CardDB 미이식분
- [ ] 7-4. 통합 플레이 테스트 — 싱글/3인 멀티 각각 처음부터~보스까지

---

## 마일스톤 요약

| 마일스톤 | 내용 | 필요 Phase |
|---|---|---|
| **M1 전투 슬라이스** | 에리스 1인 TP 전투 완주 (3인 멀티) | P1 + P2A~2C |
| **M2 성장 루프** | 3캐릭터 + 레벨업 + 스킬트리 + 장비 | P2D + P3 + P4 |
| **M3 이어서 하기** | 저장/로드 + 메뉴 개편 + 싱글 모드 | P5 |
| **M4 월드 완성** | 던전/상인/보스/스테이지 | P6 |
| **M5 출시 후보** | 밸런스 + 로컬라이즈 + 청소 | P7 |

## 미결 기획 결정 (구현 전 확정 필요)

1. **에리스 기본 스킬** — 기획서 공란 (제안: HP 소모 → TP 가속 "피의 가속")
2. **싱글 조작 범위** — 1인 3캐릭 조작 여부 (5-4)
3. ~~민첩 다회 턴 상한~~ — **확정: 상한 없음, 민첩 수치로 밸런싱** (2A-1 반영)
4. ~~필살기 리스크 규칙~~ — **1차 확정: 사용 후 자신의 TP -50** (3-3 반영, 밸런스 조정 여지)
5. **사망 패널티** (5-5)
6. **탐험 스텝 행동비용 소모 + 보스 타이머 존치 여부** (Phase 0 잔여)
7. **특수타일 리스폰 규칙** (6-5)
