# RPG 전환 계획 (turn-rpg 브랜치)

> 목표: 덱빌딩 로그라이크 → **커맨드/스킬 기반 턴제 + 영구 성장 RPG** 전환.
> 유지: 3인 코옵 (Mirror + Steam), 서버 권위 구조, 기존 전투 상태머신·몬스터 AI·버프 시스템.
> 작성일: 2026-08-19. 코드베이스 전수 조사 결과 기반.

---

## 0. 조사 결론 요약

### 그대로 살릴 수 있는 자산 (~4,500줄)

| 시스템 | 위치 | 비고 |
|---|---|---|
| 전투 상태머신 골격 | `M_TurnManager.cs` + partial 5개 | `BattleTurn` 14페이즈, 서버 권위. 카드 종속 부분만 도려내면 됨 |
| 전투 코어 (HP/방어/피해/버프/사망) | `Assets/Script/Battle/TargetObject.*` (1,172줄) | 무수정 재사용 |
| 몬스터 AI 전체 | `Assets/Script/Monster/**` (~1,500줄) | `actionName + actionValue + ActionTarget` 구조가 **이미 커맨드 방식** — 새 스킬 시스템의 참조 모델 |
| 버프 시스템 | `Buff.cs`, `TargetObject.Buff.cs`, `BuffData.cs` | 카드 결합 3곳뿐. `buffCardUseEffect` 등 턴 훅 4종은 "스킬 사용 시점"으로 그대로 매핑 |
| 타겟팅 | `GamePlayerTarget.cs`, `GetTargetObjectFromActionTarget` (`M_TurnManager.cs:754-837`), `TargetIndicatorController.cs`, `ValidTarget` enum | 카드 무관 |
| CSV + 리플렉션 데이터 파이프라인 | `DB/CsvTable.cs`, `CardData.cs:235-247`, `ItemData.cs:51-68` | SkillDB/LevelDB/EquipDB/QuestDB를 같은 패턴으로 추가 |
| 아이템/유물 효과 프레임워크 | `GamePlayerItem.cs`, `Item/*Methods.cs`, `ItemEffectTime` 12종 훅 | **장비 시스템의 기반으로 승격** (슬롯·요구레벨 필드만 추가) |
| 3D 구체 맵 | `Assets/Script/Map3D/` (SphereMapSystem 등) | 시드 결정적 생성 → **시드 저장만으로 고정 월드맵화**. 워프는 이미 골드 소비 패스트트래블 |
| 보상 서비스 | `Mangers/RewardService.cs` | `Reward_Type`에 `Exp`/`Equipment` 추가로 확장 |
| 커맨드 UX 원형 | `AbilityButton.cs` + `AbilityCtrlArrow.cs` (~280줄) | "버튼 클릭 → 타겟 화살표 → Cmd"가 이미 구현됨. **N개 스킬 버튼으로 확장이 최단 경로** |
| 카드 효과 코루틴 일부 | `CardData_*.cs` 225개 중 ~60-70% | 본문이 공격+버프뿐인 것들은 스킬 본문으로 거의 그대로 이식 |

### 교체/폐기 대상 (~9,000줄)

- `Assets/Script/Card/` 전체 (~5,900줄), `DB/CardData.cs`의 카드 유틸, `M_CardManager.cs` (667줄)
- `GamePlayerDeck.*` 5개 partial (~1,575줄) → `GamePlayerSkill`로 교체 (단 `serverCardPredictQueue` 검증 패턴은 계승)
- `M_TurnManager.CardQueue.cs`의 카드 후처리, `GameUIManager` 카드 UI 블록, `CardDB.csv`/`CardCharacteristic.csv`
- 카드 효과 중 덱/손패를 소재로 삼는 ~103건 (이식 불가, 스킬로 재설계)
- `BuffType` 50종 중 카드 규칙 전제 ~15종 (`CARDCOSTONE`, `BYEOLMURI`, `GOHANG*`, `SOIRAK` 등)

### 현재 없는 것 (신규 구축 필요)

- **영속 저장**: 메타 진행 저장 전무. `save.json`은 디버그 버튼으로만 저장되는 런 스냅샷 (`SaveTest.cs:18`이 유일 호출자)
- **플레이어 성장**: 레벨/경험치/공격력 개념 0. 스탯은 HP/골드뿐 (`GamePlayer.cs:19-43`)
- **승리 조건**: 미구현. 보스 잡아도 일반 보상 후 맵 복귀
- **퀘스트/스토리**: 전무
- 유물/유산 지급 경로 없음 (디버그 버튼뿐), `LegacyDB.csv`는 스텁 2행 — **빈 슬롯이라 오히려 활용 가치 높음**

---

## 1단계 — 전투 골격 전환: 카드 → 스킬 (최우선)

핵심 5개 수술. 이 단계가 끝나면 "스킬 버튼으로 싸우는 턴제 전투"가 성립한다.

### 수술 1: 델리게이트 시그니처 치환
`ProjectD.cs:63`
```csharp
// AS-IS: public delegate IEnumerator ExecuteCard(Card card, List<TargetObject> target);
// TO-BE: public delegate IEnumerator ExecuteSkill(SkillInstance skill, List<TargetObject> target);
```
- `CardData.CreateCardDelegate()`(`CardData.cs:235-247`)의 CSV명↔메서드명 리플렉션 바인딩을 `SkillDB.csv`로 재사용
- `_E` 폴백 메서드 = 스킬 레벨 분기로 재해석
- `tar[0]`=시전자, `tar[1]`=지정 타겟 규약 유지

### 수술 2: 실행 큐 튜플 변경
`M_TurnManager.cs:80`
```csharp
// AS-IS: Queue<(GamePlayerDeck, int cost, CardOnHand, List<TargetObject>)>
// TO-BE: Queue<(TargetObject actor, int cost, SkillInstance, List<TargetObject>)>
```
- `ProcessCardQueue`(`CardQueue.cs:17-123`)의 직렬 실행·타겟 사망 재검증·환불 골격 유지
- 카드 후처리(도돌이표/별무리/화합/숙련·중력, `:67-112`)만 제거
- **3인 동시 입력 → 전역 FIFO 직렬 실행**이라는 멀티플레이 핵심 설계 보존

### 수술 3: ★ PLAYER_END 전이 회수 (필수, 최고 위험)
`PLAYER_END → MONSTER_ORDERSELECT` 전이가 `M_TurnManager`가 아니라 **`M_CardManager.cs:654`에 있음** (손패 버리기 연출 완료 콜백 체인 끝). 카드를 제거하면 턴이 영구 정지한다.
- `M_CardManager.cs:654`의 `phase = MONSTER_ORDERSELECT`를 `M_TurnManager.PlayerEndTurn()`(`:418-423`)으로 이동
- `PlayerInterface.cs:312-323`의 `cardThrowAwayDone` 체인, `M_TurnManager.cs:255-259` `PlayerCardThrowAwaySetDefault()` 제거

### 수술 4: PLAYER_DRAW 재정의
`M_TurnManager.cs:331-354`에서 이치 회복(`:334-339`)만 남기고 드로우·손패 스캔 제거. 페이즈명 `PLAYER_PREPARE`로 변경 또는 `PLAYER_PREEFFECT`에 병합.

### 수술 5: 입력 계층 신설 — `GamePlayerSkill`
- `GamePlayerDeck` → `GamePlayerSkill` (보유 스킬 SyncList + 쿨다운)
- `serverCardPredictQueue` 검증 패턴(`GamePlayerDeck.cs:283-332`: 코스트 검산→차감→타겟 null 반려→전역 큐 투입) 계승
- UI: `AbilityButton`/`AbilityCtrlArrow`를 스킬 버튼 N개로 확장. `CardOnHand`(NetworkBehaviour 스폰 객체) 제거로 동기화 부하 대폭 감소 — `PlayerInterface.destroyCards` 등 왕복 핸드셰이크 루프 함께 정리
- 자원(이치)은 `GamePlayerDeck_IchiPart.cs`의 로직 유지, UI 직접 조작 훅(`:47-130`)은 이벤트로 분리

### 1단계 기타
- `CardData.instance.isCardOperating` 전역 플래그 → `M_TurnManager.isActionOperating`으로 이관 (몬스터 사망 처리 `M_TurnManager.cs:540` + 큐 게이팅이 공유 중)
- `SwapPlayerOrder`의 `CardData.instance.GeneralGetDefense()` 역방향 의존 해소 — 공용 전투 유틸(`GeneralSingleAttack`/`GeneralGetDefense`, `CardData.cs:316-388`)을 별도 `BattleActions` 클래스로 추출
- `SkillDB.csv` 신설: CardDB 스키마 참고 (No=메서드명, 코스트, ValidTarget, 설명키). `CardMarkup` 마크업 파서 재사용
- 캐릭터 고유 능력: 에리스(파괴의 권능)·홍단향(철귀)은 패시브/위치 기반이라 보존, **게오르크(저주 카드 3장 → 영웅 변신)는 카드 없이는 성립 불가 → 재설계 필요**

---

## 2단계 — 영속 계층 신설 (영구 성장의 토대)

### 2-1. 저장 구조 재설계
- `M_SaveManager`를 `NetworkSingletonD` 상속에서 분리 → 일반 정적 서비스/MonoBehaviour. 메뉴/오프라인 컨텍스트에서도 저장·로드 가능해야 함
- `SaveData`를 3분할:
  - **`PlayerProfile`** — 캐릭터, 레벨/EXP, 스탯, 습득 스킬, 장비, 골드, 유산, 스토리/퀘스트 플래그 (플레이어 로컬, SteamID 키)
  - **`WorldState`** — 맵 시드, 탐험/개방 진행, 처치 플래그 (호스트 저장)
  - `RunSnapshot` (선택) — 세션 중단 복구용
- 저장 시점: 전투 종료·거점 도착·장비 변경 등 **자동 저장** (현재는 자동 저장 0회)

### 2-2. GamePlayer에서 영속 스탯 분리
- `CharacterProfile`(순수 C# 직렬화) ↔ `GamePlayer`(세션 네트워크 뷰) 이원화
- `PlayerInterface.GenerateGamePlayer`(`PlayerInterface.cs:122-142`)가 BalanceDB 상수 대신 프로필에서 초기화
- 신규 스탯: 레벨, EXP, 공격력/방어력 등 (전투 수치가 현재 100% 카드/버프에서 나오므로, 스킬 데미지 = 기본치 × 스탯 스케일 공식 도입)

### 2-3. 레벨/EXP 도입
- `Reward_Type`에 `Exp` 추가 → `RewardService.cs:30` 인근에서 지급 → 프로필 적립
- `LevelDB.csv` 신설 (기존 `CsvTable` 패턴). 레벨업 시 스탯 상승 + 스킬 습득 (덱빌딩의 "카드 획득" 보상 루프를 "스킬/장비 습득"으로 대체)

### 2-4. 장비 시스템
- `Item` 프레임워크 승격: 슬롯(`EquipSlot`)·요구레벨 필드 추가, `ItemEffectTime` 훅으로 효과 발동 (기존 경로 그대로)
- `M_TurnManager.teamArtifacts`(세션 객체 소유)를 프로필/월드 상태로 이전
- `ItemType.LEGACY` + `LegacyDB.csv`(현재 빈 슬롯)를 **영구 특성/유산 컨테이너로 활용** — 로더·발동 경로 이미 동작함

---

## 3단계 — 진행 구조 전환: 런 → 월드

### 3-1. 사망 처리 분기
- `GamePlayer.CheckAllPlayersIsDead`(`GamePlayer.cs:118-125`) → `RpcGameOver` 대신 "패널티 + 거점 귀환"
- `PopUpUIManager.HandleHideGameOverPopUp`(`:445-457`)의 `NetworkServer.Shutdown()` + 로비 탈퇴 경로는 명시적 종료 시에만
- `M_NetworkRoomManager.OnChangedActiveScene`(`:161-175`)의 DDOL 전량 Destroy 전에 프로필 flush 훅 삽입

### 3-2. 월드맵 확정 — 3D 구체 맵 채택
- 2D 헥사곤 맵(`M_MapManager`의 생성부)은 이미 비활성 (`M_LoadingManager.cs:105-121` 주석 처리됨) → **레거시 동결, 3D가 본선**
- `SphereMapSystem.SetupNewMap(seed)`의 시드를 `WorldState`에 저장 → 고정 월드맵
- 전투 진입은 기존 프록시 방 패턴(`SphereMapNetwork.cs:293-299` → `M_MapManager.StartBattle`) 유지

### 3-3. 런 장치 제거/개조
- 행동비용(`currentActionCost`) 타이머 + 보스 추격 + RUINS 변질 (`M_MapManager.cs:657-741`) → 제거 또는 스토리 이벤트로 개조
- `RoomType.COMPLETE` 1회성 소모 모델(`SetRoomStateComplete:639`) → 재진입 시 리스폰(타이머 or 고정) 모델로
- `GetRoomType` 랜덤 가중치(`:920`) → 존 설계 기반 배치. `hazard`(중심 거리) → 존별 몬스터 레벨로 자연 매핑 (`MonsterData.GetMonsterGroup(hazard)` 이미 사용 중)
- 전투 종료 시 덱 Clear + 이치 리셋(`RewardService.cs:48-50`) → 스킬 쿨다운/자원 회복 규칙으로 대체
- 미구현이던 `EVENT_POSITIIVE/EVENT_NEGATIVE` 방 → 퀘스트/이벤트 트리거로 활용 가능

### 3-4. 승리 조건/스토리 골격
- 현재 클리어 개념 없음 → 스토리 플래그 기반 진행 (`PlayerProfile` 플래그 + 퀘스트 테이블 `QuestDB.csv` 신설)

---

## 4단계 — UI/콘텐츠 마무리

- 전투 UI: `GameUIManager`의 카드 블록(`:29-79`) 제거, 스킬 바 + 파티 상태창으로 교체. **UI에 섞인 비즈니스 로직(`M_CardManager.cs:654`의 페이즈 전이 등) 회수 원칙 유지**
- NPC 개편: CARD_NPC(Mercurius) → 스킬/장비 상점, CAMP → 여관/거점
- 로컬라이제이션: `skill.<no>.desc`, `quest.*` 키 추가 (기존 `Locales.csv` 체계 그대로)
- 밸런싱: 스탯 스케일 공식 도입에 따른 MonsterDB 수치 재조정

---

## 리스크 및 결정 필요 사항

| # | 항목 | 내용 |
|---|---|---|
| 1 | **게오르크 재설계** | 저주 카드 수집 → 영웅 변신 메커니즘은 카드 전제. 대체 기믹 기획 필요 |
| 2 | 스킬 습득 모델 | 레벨업 자동 습득 / 스킬트리 / NPC 구매 중 선택 필요 |
| 3 | 카드 효과 이식 범위 | 225개 중 ~60-70%는 이식 가능, ~103건은 덱 소재라 폐기. 캐릭터당 스킬 몇 개로 압축할지 결정 필요 |
| 4 | 멀티 저장 정책 | 파티원 각자 프로필 로컬 저장 + 월드는 호스트 저장 방식 권장. 레벨 차이 나는 파티 매칭 규칙 필요 |
| 5 | Mirror 동기화 재설계 | `CardOnHand` 스폰 객체 제거로 단순해지지만, 관련 대기 루프/핸드셰이크 정리 범위가 넓음 |
| 6 | 2D 맵 코드 처리 | 동결(권장) vs 삭제. `M_MapManager` 1,349줄 중 전투 진입·A*만 살리고 생성부는 정리 대상 |

## 권장 실행 순서

1단계(전투)를 **수직 슬라이스**로 먼저 완성 — 캐릭터 1명(에리스 권장, 카드 의존 최소) × 스킬 4~6개 × 몬스터 전투 1판이 3인 멀티에서 도는 것 확인 → 2단계(영속) → 3단계(월드) → 4단계(콘텐츠). 각 단계는 독립 커밋 단위로 쪼개 ParrelSync 멀티 테스트를 반복한다.
