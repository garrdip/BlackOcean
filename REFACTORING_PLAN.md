# BlackOcean 리팩토링 우선순위 계획

> 작성일: 2026-07-08
> 기준: Unity 6000.3.7f1 마이그레이션 직후 전체 코드베이스 분석 (Assets/Script 162개 파일, 약 29,500줄)
> 심각도 기준 — **P0 치명**: 런타임 크래시/데이터 손실 직결, 개발 재개 전 필수. **P1 높음**: 구조적 문제, 기능 추가 생산성 직결. **P2 중간**: 품질/성능 개선. **P3 낮음**: 정리 수준.

---

## 요약

| 순위 | 항목 | 심각도 | 규모 |
|---|---|---|---|
| 1 | `NetworkServer/Client.spawned[]` 무검증 인덱싱 33곳 | P0 | 소~중 |
| 2 | 카드/아이템 리플렉션 바인딩 무검증 (CSV↔코드 수동 동기화) | P0 | 소 |
| 3 | M_TurnManager God Class 분해 (1,994줄 / 책임 8종) | P1 | 대 |
| 4 | 매니저 순환 참조 디커플링 | P1 | 대 |
| 5 | GamePlayerDeck 분리 + SyncList 동기화 전략 재검토 | P1 | 대 |
| 6 | Command 권한 검증 (치트/역동기화 방어) | P1 | 소 |
| 7 | CSV 파서 공통화 (인덱스 하드코딩·콤마 취약) | P1 | 중 |
| 8 | 캐릭터별 카드 효과 3파일 중복 제거 (~4,400줄) | P1 | 대 |
| 9 | 사운드 문자열 조회 75곳 캡슐화 | P1 | 중 |
| 10 | TargetObject 926줄 플레이어/몬스터 분리 | P1 | 중 |
| 11 | 몬스터 서브클래스 20개 복붙 패턴 상향 | P1 | 중 |
| 12 | 밸런스 매직 넘버 데이터 테이블화 | P2 | 중 |
| 13 | Unity 6 obsolete API (`FindObjectOfType` 계열 52건) 교체 | P2 | 소 |
| 14 | ParrelSync 이중 설치 확인·제거 | P2 | 소 |
| 15 | 매 프레임 폴링·GetComponent 반복 정리 | P2 | 중 |
| 16 | 죽은 코드·미구현 카드 스텁 정리 | P2 | 중 |
| 17 | Steam IL2CPP 빌드 검증 / DOTween 갱신 | P2 | 소 |
| 18 | .mat 재직렬화 82건 커밋 등 마이그레이션 마무리 | P3 | 소 |

**Unity 6 마이그레이션 자체는 건강한 상태다.** Mirror 81.4.0, Spine 4.2.64, URP 17.3.0 모두 Unity 6 공식 호환이며, 빌드를 막는 부채는 없다 (컴파일 오류였던 TMP 예제 2파일은 수정 완료). 남은 문제의 대부분은 마이그레이션과 무관한 **기존 코드의 구조적 부채**다.

---

## P0 — 치명: 개발 재개 전 반드시 해결

### 1. `NetworkServer/Client.spawned[]` 직접 인덱싱 33곳

`TryGetValue` 없이 딕셔너리를 직접 인덱싱한다. 대상 오브젝트가 스폰 해제됐거나 클라이언트에 아직 도착하지 않은 타이밍에 `KeyNotFoundException`으로 즉시 크래시한다. 멀티플레이어에서 접속 해제·씬 전환 타이밍에 재현되는 유형이라 가장 위험하다.

- `M_TurnManager.cs` 23곳 — `GetCurrentPlayerTargetObject` (145, 150행), `GetTargetObjectFromActionTarget` (1783~1815행), `SwapPlayerOrder` (486, 490행)
- `M_NetworkRoomManager.cs:100-102` — **연결 해제 처리 중** `NetworkServer.spawned[conn.identity.netId]` 접근. 가장 위험한 지점.
- `PlayerInterface.cs:15` — `currentGamePlayer` getter가 무검증 인덱싱을 프로퍼티로 노출. 코드 전역 수십 곳에서 참조하는 **단일 실패점**.
- `M_MapManager.cs` 5곳
- 파생 문제: `GetCurrentPlayerTargetObject`가 실패 시 null을 반환하는데 호출부(`GamePlayer.cs:74-75` 등)가 즉시 역참조 → NRE.

**조치**: `TryGetValue` 일괄 전환 + 실패 시 로그/조기 반환. `currentGamePlayer`는 null 가능성을 시그니처에 드러내거나 안전 접근자로 교체.

### 2. 리플렉션 카드 바인딩 무검증

카드 효과가 CSV의 메서드명 → `Delegate.CreateDelegate`로 바인딩되는데 검증 계층이 전혀 없다.

- `CardData.cs:86` — try/catch 없는 `CreateDelegate`. CSV 메서드명 오타 1건이면 예외로 **로드 루프 전체가 중단**되어 이후 모든 카드가 로드 실패. 어떤 카드가 문제인지 로그도 없음.
- `ItemData.cs:36, 55` — Artifact/Legacy 로더에 동일 패턴 복제.
- `CardData.cs:246, 251, 260, 265, 272` — `CardMethods[cardNumber + "_E"]` 등 존재 확인 없는 딕셔너리 인덱싱. CSV에 강화(`_E`) 행 누락 시 `KeyNotFoundException`.

**조치**: 로드 타임 검증 계층 도입 — 바인딩 실패를 카드 단위로 try/catch하여 누락 목록을 집계 로그로 출력하고 나머지 카드는 계속 로드. `_E`/curse 키는 로드 완료 후 일괄 사전 검증. (에디터에서 CSV↔메서드 전수 검사하는 메뉴/테스트를 만들면 이후 카드 추가 작업이 안전해진다.)

---

## P1 — 높음: 구조적 부채, 기능 개발 재개 전 우선 정리

### 3. M_TurnManager God Class 분해 (1,994줄)

"턴 상태머신"은 일부일 뿐, 최소 8개 책임이 한 클래스에 있다:

| 책임 | 근거 |
|---|---|
| 턴 상태머신 (본래 역할) | `OnChangedPhase()` switch 498~547행 |
| 타겟 인디케이터 UI (~210줄 뷰 로직) | 241~455행 |
| 몬스터/NPC/보스 스폰 팩토리 (~210줄) | `GenerateMonster` 1073행 외 |
| 전투 보상 시스템 | `BattleEnd()` 727~781행 |
| 카드 큐 파이프라인 | `ProcessCardQueue` 834~904행 |
| 사운드/보이스/토스트 연출 (~200줄) | `RpcStartBattleEvent` 1451행 외 |
| 철귀(홍단향 전용) 캐릭터 로직 | `MoveIronDemon` 1630행 외 |
| 맵 이동/씬 전환 | `EnterTheRoom` 1325행, `ReturnToMap` 1752행 |

`[Server]` 27개 + `[ClientRpc]` 14개가 공존해 서버 권한 로직과 클라 연출이 뒤섞여 있다.

**조치**: `TurnStateMachine` / `BattleSpawner` / `RewardService` / `TargetIndicatorController` / `IronDemonController` / `BattlePresentation`(연출)으로 분해. P0-1(spawned 인덱싱)의 대부분이 이 파일 안이므로 **동시에 진행하는 것이 효율적**.

### 4. 매니저 순환 참조 디커플링

정적 싱글톤 `.instance` 직접 참조가 얽혀 있어 한 매니저 수정이 연쇄 파손을 일으킨다.

- 확인된 순환: `M_TurnManager ⇄ M_MapManager` (16회 ↔ 13회), `M_TurnManager ⇄ PlayerInterface ⇄ GamePlayerDeck ⇄ M_TurnManager`
- 전투 종료 흐름이 `BattleEnd → PlayerInterface.OnCompleteReward → NoneBattleEnd → MapManager`로 세 매니저를 왕복.
- M_TurnManager → M_SoundManager 호출만 51회.

**조치**: 게임 이벤트 버스 또는 인터페이스 기반으로 단방향 의존 정리. 특히 "전투 종료" 같은 흐름은 이벤트 발행으로 전환.

### 5. GamePlayerDeck 분해 + 동기화 전략 재검토 (1,483줄)

- 덱 관리·핸드 스폰·상점·보상·팝업·큐 예측·RPC를 한 클래스가 전담 (멤버 약 120개, `[Command]` 22개+).
- `SyncList` 10개 (`GamePlayerDeck.cs:42-58`) — Card 구조체 전체를 동기화, 카드 이동마다 리스트 델타 브로드캐스트.
- **잠재 버그**: `Card.experience`(`Card.cs:14`)가 일반 필드인데 서버에서만 증감(`CardData.cs:252, 266`) → 클라이언트와 경험치 비동기화 소지.

**조치**: 덱 상태 / 핸드 스폰 / 상점·보상 / 네트워크 RPC로 책임 분리. experience 동기화 경로를 명시적으로 수정.

### 6. Command 권한 검증

- `[Command(requiresAuthority = false)]` + 발신자 검증 없음: `M_MapManager.cs:200` (`CmdSwapMapPlayer` — 임의 플레이어 오더 스왑 가능), `M_CardManager.cs:515`, `M_MapManager.cs:207`, `M_LobbyMananger.cs:28, 37`, `M_MessageManager.cs:166`
- `GamePlayer.cs:88` `CmdAddGoldValue(uint localPlayerNetId, ...)` — 클라가 보낸 netId를 서버가 그대로 신뢰. 발신자 위장 여지.

**조치**: `sender` 파라미터(`NetworkConnectionToClient sender = null`)로 발신자를 서버가 직접 확인하고, 클라 제공 netId 신뢰 제거.

### 7. CSV 파서 공통화

- 모든 로더(`CardData`/`BuffData`/`MonsterData`/`ItemData`)가 `while + ReadLine + Split(",")` 루프를 복붙.
- `CardData.cs:71-85` — `values[0]`~`values[8]` 고정 인덱스. **컬럼 순서 변경/삽입 시 조용히 어긋남.**
- 따옴표/이스케이프 미지원 — 설명 텍스트에 콤마 하나 들어가면 행 전체 붕괴.
- `MonsterData.cs:54-61` — `values[i*3]` 3의 배수 하드코딩, 헤더 검증 없음.

**조치**: 헤더 기반 컬럼 매핑을 지원하는 공통 CSV 파서 1개 추출 (따옴표 처리 포함). 데이터 작업 재개(카드/몬스터 추가) 전에 해두면 이후 작업이 전부 편해진다.

### 8. 캐릭터별 카드 효과 3파일 중복 제거 (~4,400줄)

- `CardData_Geork.cs` 1,526줄(163메서드) / `CardData_DanHyang.cs` 1,647줄(126) / `CardData_Eris.cs` 1,238줄(124)
- **`_E`(강화) 래퍼 약 190개가 본체를 한 줄 호출하는 순수 보일러플레이트** (Geork 71, Eris 61, DanHyang 60).
- 실행 골격 동일 반복: `StartDimming → 애니 → WaitForSeconds(0.5f) → GeneralSingleAttack/GeneralGetDefense/GainBuff → OnHitAnimation → StopDimming`. (`StartDimming` 호출 75/59/54회, `WaitForSeconds(0.5f)` 143/59/43회)

**조치**: 데미지/방어/드로우/버프를 (수치 + 애니 키 + 타겟 범위) 데이터로 기술하는 공통 실행기 도입. 특수 카드만 커스텀 코루틴 유지. `_E` 껍데기는 규약(기본은 본체와 동일, 차이만 오버라이드)으로 제거.

### 9. 사운드 문자열 조회 75곳 캡슐화

- 패턴: `sfxClips[SFX_TYPE.X].Find(c => c.name.Equals("문자열"))` — **31개 파일 75곳** 산재 (`TargetObject.cs:913-918`, `PopUpUIManager.cs:197` 외 4곳, `HexagonMapRoom.cs:477`, `SpawnedMonster.cs:308` 등).
- 클립명 오타 시 컴파일 통과 → 런타임 NRE (`.length` 접근 즉시 크래시). 매 호출 선형 탐색.
- 음성 인덱스 매직 넘버: `TargetObject.cs:321-360`의 `(58,4)/(65,9)/(99,6)` 등 — 클립 순서 변경 시 조용히 오작동.

**조치**: `SoundId` enum(또는 ScriptableObject 매핑) → `M_SoundManager.PlaySFX(SoundId.X)` 헬퍼로 캡슐화, 로드 타임에 매핑 전수 검증.

### 10. TargetObject 분리 (926줄)

- 플레이어/몬스터/NPC 3책임을 `objectType` 분기로 한 클래스에 수용 (초기화 함수 3벌: 174~224행).
- 캐릭터 특수 로직이 공용 클래스에 직접 구현: `ErisTransform`(506행), `ErisAdditionalMadAnimation`(514~570행), `DrawGoHengCard`(369~391행).
- `OnDestroy`가 몬스터에는 없는 `playerMessageCavnas` 참조에 무방비 접근(157행) → 파괴 시 NRE 위험.

**조치**: `TargetObjectBase` + `PlayerTargetObject` / `MonsterTargetObject` 분리, 캐릭터 특수 로직은 캐릭터 컴포넌트로 위임.

### 11. 몬스터 서브클래스 20개 복붙 패턴 상향

- `OnStartClient`의 인디케이터 위치 보정 코드 8개 파일 동일 복붙 (WacherA/B, Saddy, Guardian, SpearManA/B, Devourer, Happy).
- `OnHitAnimationRPC` 13개 파일 동일 복붙.
- `DoAction` 스위치 골격 동일: `DoAnimation → WaitForSeconds(0.5f) → 이펙트/버프 → WaitForSeconds(0.833f) → ReturnToIdle`. 액션 이름이 한글 문자열("쇠락부여" 등)이라 오타 시 조용히 무동작.

**조치**: 공통 시퀀스를 `SpawnedMonster`의 protected 헬퍼로 상향(`PlaySkillSequence(...)`), 액션 정의는 장기적으로 데이터(ScriptableObject) 기술로 전환.

---

## P2 — 중간: 품질·성능·마이그레이션 후속

### 12. 밸런스 매직 넘버 데이터 테이블화
- 초기 스탯 하드코딩: `PlayerInterface.cs:115-118` (HP 50 / 회복 15 / 골드 100), 이치 `M_TurnManager.cs:762-763`, 보상골드 `:739`, 상점가 `:1207`(`// TODO 임시`), 버프 수치 `:486, 490, 1002` 등.
- 카드 데미지/방어값도 코드 직박음: `CardData_Geork.cs:60` `GeneralSingleAttack(..., 9)` 등 다수.
- NPC 스폰 좌표 `new Vector3(11,-3,0)` 7회 반복, 오더 슬롯 `i==0/1/2` 판별 10곳+ 산재.
- **조치**: 밸런스 값은 CSV/ScriptableObject로, 좌표·슬롯은 상수/enum으로.

### 13. Unity 6 obsolete API 교체 (52건)
- `FindObjectOfType` 4 + `FindObjectsOfType` 28 (Assets/Script), ExternalLibrary 20건.
- 싱글톤 베이스 3종(`SingletonD`, `NetworkSingletonD`, `InstaceD`)에 박혀 있어 전역 초기화 경로에서 반복 호출됨.
- **조치**: `FindFirstObjectByType` / `FindObjectsByType(FindObjectsSortMode.None)`으로 교체. 싱글톤 베이스 3파일부터.

### 14. ParrelSync 이중 설치
- `Assets/Plugins/ParrelSync/` 폴더와 `Packages/manifest.json`의 `com.veriorpies.parrelsync` git UPM이 **동시에 존재** → 타입 충돌 위험. 하나 제거.

### 15. 매 프레임 폴링·캐싱 부재 정리
- `GameUIManager.Update`(98~102행) 매 프레임 스크롤 가시성 재계산 + `SetActive` 반복 → 이벤트 기반 전환.
- `M_SoundManager.OnUpdate` 코루틴(208~272행) 매 프레임 믹서 `GetFloat` 폴링, 볼륨 변경 시 `FindObjectsOfType<SoundEffect>` 전체 스캔(1259행) → 옵션 슬라이더 이벤트로.
- `NetworkClient.localPlayer.GetComponent<PlayerInterface>()` 체인 매 호출 재조회: `HexagonMapRoom.cs` 6곳, `M_CardManager.cs` 13곳, `M_TurnManager.cs` 9곳 — null 가드도 대부분 없음(P0-1과 연관).
- GetComponent 반복 체이닝: LobbyPlayer 49회, HexagonMapRoom 26회 등 → 캐싱.
- SyncVar hook 내부 `FindObjectsOfType<PlayerInterface>()` 전수 스캔(`PlayerInterface.cs:221, 242, 264`) — 성능 + 타이밍 의존 버그 소지.

### 16. 죽은 코드·미구현 정리
- 미구현 카드 스텁: `CardData_Geork.cs:281-300` ("존재하지 않는 카드" 빈 메서드 5개), Eris에 임시 버프만 부여된 미완 효과 13곳(TODO 주석), 주석 처리 라인 Geork 67 / Eris 88 / DanHyang 77.
- 빈 SyncList 콜백 switch case 다수 (`M_TurnManager.cs:1880-1960`, `HexagonMapRoom.cs:330-397`).
- **로직 버그 흔적**: `HexagonMapRoom.ChangeHexagonMapRoomLayoutState`(410~443행) — if/else 양쪽 분기가 완전 동일, 조건 무의미. 의도 확인 필요.
- `PopUpUIManager.cs:84, 86` — 카드 제거 팝업 델리게이트가 `OnCardEnhancePopUpShow` 타입으로 선언된 복붙 오타.
- 주석 처리된 상태 전이(`M_TurnManager.cs:1042`) 등 의도 불명 라인 정리.

### 17. 빌드·플러그인 검증
- Standalone(Steam 타깃) 스크립팅 백엔드가 ProjectSettings에 명시돼 있지 않음 — IL2CPP 빌드로 Steamworks.NET 20.1.0 동작 실검증 필요.
- DOTween이 버전 미상의 precompiled DLL — 최신본 갱신 검토.

---

## P3 — 낮음: 마무리 정리

- **.mat 82건 재직렬화 커밋**: Unity 6 머티리얼 포맷 업그레이드(`m_Parent`, `m_LockedProperties` 등 필드 추가)로 무해함. 커밋해서 이후 diff 노이즈 제거. git status의 삭제 1건은 커밋 전 확인.
- **M_SoundManager 슬림화** (1,769줄): 범용 오디오 엔진 통짜 이식이 원인. `SoundEffect`/`VoiceEffect` 클래스가 약 95줄씩 거의 동일 중복(1527~1721행) → 공통 베이스로 통합, 미사용 오버로드·죽은 풀 로직 제거.
- Mirror 81.4.0 / Spine 4.2.64 / URP 17.3.0 — 모두 Unity 6 호환, 조치 불필요.

---

## 권장 진행 로드맵

1. **0단계 — 마이그레이션 마무리 (반나절)**: .mat 등 재직렬화 커밋 → ParrelSync 중복 제거 → obsolete API 52건 교체 → 에디터 2인(ParrelSync 클론) 멀티 플레이 스모크 테스트로 기준선 확보.
2. **1단계 — 크래시 방어 (P0, 수일)**: `spawned[]` 33곳 TryGetValue 전환, 카드 바인딩 로드 타임 검증. *이때 CSV↔메서드 전수 검증 에디터 테스트를 함께 만들 것.*
3. **2단계 — 데이터 레이어 (P1-7, 수일)**: 공통 CSV 파서 + 헤더 매핑. 이후 카드/몬스터 데이터 작업이 안전해짐.
4. **3단계 — 구조 분해 (P1-3~5, 주 단위)**: M_TurnManager 분해를 축으로 순환 참조 정리, GamePlayerDeck 분리. 한 번에 하나씩, 단계마다 멀티 스모크 테스트.
5. **4단계 — 중복 제거 (P1-8~11)**: 카드 공통 실행기 → 사운드 ID화 → TargetObject/몬스터 분리. 콘텐츠 제작 속도가 여기서 결정됨.
6. **5단계 — P2 잔여**: 밸런스 데이터화, 폴링/캐싱 정리, 죽은 코드 삭제, IL2CPP 빌드 검증.

> 원칙: **P0를 끝내기 전에는 새 콘텐츠(카드/몬스터) 추가를 시작하지 말 것.** 현재 구조에서는 CSV 한 줄 오타가 전체 카드 로드를 무너뜨리고, 네트워크 타이밍 이슈가 재현 어려운 크래시로 이어진다.
