# BlackOcean 리팩토링 진행 결과

> 작성일: 2026-07-08 (1·2회차)
> 기준 문서: `REFACTORING_PLAN.md` (P0~P3 우선순위 계획)
> 검증: 각 단계마다 Unity 6000.3.7f1 에디터 리컴파일 통과 확인 (최종 error CS 0건) + 2회차는 에디터 내 런타임 파싱 검증 수행

---

## 3회차 완료 작업 (P1-3 1단계)

### ✅ P1-3 1단계 — M_TurnManager 책임별 partial class 분리

2,010줄 단일 파일을 책임 단위 7개 파일로 분리. **같은 클래스를 유지하는 partial 분리라 Mirror 네트워크 계약(RPC 해시, SyncVar, 위빙)이 완전히 보존됨** — 코드 원문을 그대로 이동했고 로직 변경 0건.

| 파일 | 내용 | 규모 |
|---|---|---|
| `M_TurnManager.cs` (코어) | SyncVar/SyncList 필드, 턴 상태머신(OnChangedPhase, BattleStandby, MonsterActive 등), 조회 헬퍼, SyncList 콜백 | 854줄 |
| `M_TurnManager.TargetIndicator.cs` | 타겟 인디케이터 뷰 로직 7개 메서드 | 277줄 |
| `M_TurnManager.Spawner.cs` | 플레이어/몬스터/보스/NPC 스폰 팩토리 8개 | 286줄 |
| `M_TurnManager.Presentation.cs` | 전투 연출 RPC (BGM/보이스/토스트/애니) 7개 | 250줄 |
| `M_TurnManager.Reward.cs` | 전투 보상·종료 처리 9개 | 200줄 |
| `M_TurnManager.CardQueue.cs` | 카드 큐 파이프라인 4개 | 151줄 |
| `M_TurnManager.IronDemon.cs` | 철귀 전용 연출/이동 5개 | 120줄 |

**검증**: 컴파일 0건 + 에디터 리플렉션 검증 — 이동 메서드 40개 전부 타입에 존재, Mirror 위버 UserCode_ 14개 정상 생성(RPC 위빙 확인).

### ✅ P1-3 2단계 — TargetIndicatorController 실제 컴포넌트 추출 (4회차)

RPC/SyncVar가 전혀 없는 순수 클라이언트 뷰 로직인 TargetIndicator 그룹을 M_TurnManager에서 완전히 분리해 **독립 컴포넌트 `TargetIndicatorController`(279줄, `InstanceD` 싱글톤)** 로 추출. M_TurnManager 코어는 839줄로 축소 (원본 대비 -58%).

- **이동**: 인스펙터 필드 4개(프리팹/컨테이너/인디케이터 리스트 2개) + 메서드 7개. 내부 오타 메서드명 `ClreatTargetIndicators` → `ClearTargetIndicators` 교정.
- **신규 `CreateIndicator(netId, position)`**: M_TurnManager의 SyncList 콜백 3곳에 중복돼 있던 인디케이터 생성 코드를 한 메서드로 통합.
- **호출자 갱신**: CardOnHand, CardCtrlArrow, SpawnedMonster, CampPopUp, CharactorSelector 등 6개 파일 18곳 → `TargetIndicatorController.instance.X()`.
- **씬 배선**: GameScene의 M_TurnManager 오브젝트에 컴포넌트 부착, 코드 변경 전 캡처해 둔 프리팹(`TargetIndicator.prefab`)·컨테이너(`Game/TargetIndicatorContainer`) 참조를 에디터 스크립트로 복원 후 씬 저장. 저장된 YAML에서 GUID 일치 검증 완료.
- 검증: 컴파일 0건, 씬 직렬화 확인. **플레이 검증 필요 포인트**: 카드 마우스오버 시 타겟 후보 표시, 화살표 타겟팅, 몬스터 마우스오버 시 액션 타겟 표시, 오더 스왑 시 인디케이터 갱신.

### ✅ P1-3 3단계 — BattleSpawner 추출 + 스폰 중복 통합 (5회차)

스폰 팩토리 7개 메서드를 독립 컴포넌트 **`BattleSpawner`(InstanceD 싱글톤, 184줄)** 로 추출. `GenerateBattleObject`는 RPC 연출 4종과 대기 코루틴을 호출하는 오케스트레이터라 M_TurnManager partial에 유지(34줄로 축소, 기존 286줄).

- **[Server] 어트리뷰트 대체**: NetworkBehaviour가 아니므로 각 공개 메서드에 `if(!NetworkServer.active) return;` 수동 가드.
- **복붙 블록 통합**: 보스 3종(Momos/Apates/Geras) 스폰 3중 복붙 블록과 NPC 4종(RyuJinSol/Sophia/ShadowMan/Mercurius) 스폰 코드를 `SpawnMonsterWithAvatar(name, position, objectType, addToSyncList)` 공통 경로 하나로 통합 — 약 130줄 감소. 프리팹/MonsterDB 누락 시 크래시 대신 에러 로그.
- **씬 배선**: GameScene의 M_TurnManager 오브젝트에 부착 (MenuScene이 미저장 상태라 GameScene을 Additive로 열어 GameScene만 저장). 직렬화 확인 완료. BattleSpawner는 인스펙터 필드가 없어 InstanceD 폴백(자동 생성)으로도 동작 가능.
- 검증: 컴파일 0건. **플레이 검증 포인트**: 일반/엘리트/보스 전투 진입 스폰, 전초기지·카드상점·아이템상점 방 진입, NPC 표시.

### ✅ P1-3 4단계 — RewardService 추출 (6회차)

보상 시스템을 독립 컴포넌트 **`RewardService`(InstanceD 싱글톤, 103줄)** 로 추출.

- **이동**: 보상 상태 필드 3개(playerRewardedDic/rewardObjects/rewardCardObjects) + 보상 UI 정리 메서드 4개 + `BattleEnd`의 서버 보상 분배 루프(→ `DistributeBattleRewards()`, NetworkServer.active 가드 + playerInterface null 가드 추가).
- **M_TurnManager에 유지**: `BattleEnd`(5줄 오케스트레이터로 축소)/`NoneBattleEnd`(흐름 제어), ClientRpc 2종, `ReturnToMap`(연출).
- **호출자 갱신**: BattleResultPopUp, RewardListItem, GamePlayerDeck, CardOnDeck 4개 파일 23곳 → `RewardService.instance.X`.
- **씬 배선**: GameScene M_TurnManager 오브젝트에 부착 (인스펙터 참조 필요 없음 — 전부 런타임 데이터).
- 검증: 컴파일 0건. **플레이 검증 포인트**: 전투 승리 → 보상 팝업 → 카드/골드 보상 수령 → 전원 수령 후 맵 복귀.

**현재 M_TurnManager 구성**: 코어 833줄 + partial 4개(Reward 108 / CardQueue 151 / IronDemon 120 / Presentation 250 / Spawner 33) + 추출된 컴포넌트 3개(TargetIndicatorController 279 / BattleSpawner 177 / RewardService 103). 원본 2,010줄 단일 God Class → 코어 833줄 (-59%).

**다음 단계(P1-3 5단계, 미착수)**: CardQueue/IronDemon/Presentation은 RPC·SyncList 의존이 커서 partial 유지가 적절. P1-3은 여기서 실질 완료로 보고, 다음은 P1-4(순환 참조 정리) 또는 P1-9(사운드 ID화) 권장.

---

## 22회차 완료 작업 (카드 감사 후속 2차 — 수치 불일치 8건, 설명 기준 통일)

### ✅ CARD_AUDIT.md C절 전건 수정 (기획 결정: 설명(CSV)이 정답)

| 카드 | 변경 |
|---|---|
| G4_H 태양빛의 갑주 | 방어 6 → 18 |
| E4 부서지세요 | 피해 5 → 3 |
| E55 파편 분배 | 피해 5 → 10 |
| E56 은하의 선율 | 피해 10×1회 → 3×2회 (G41 다회 타격 관례 적용) |
| G52 맺어지는 열매 | 저주/영웅 카드당 +8 → +3 (3곳 — CURSE 전용 +8을 쓰는 G31 라인은 보존) |
| H45 뿌리내리기 | else 누락 보강 — 꽃가루 시 14, 아니면 7 (기존 21 중복 타격 제거) |
| H34 꽃과 철의 분노 | 카운터 초기값 -1 → 0 (4장 주기 오프바이원 → 3장 주기) |
| H23 아버지의 화신 | 반복 카운터(numOfUsedIronTeeth) 증가를 H31/H32/H33/H39에서 제거 — H0/H1만 카운트 (메서드 본문 한정 스크립트 제거로 H0/H1 보존 확인) |

- 검증: 컴파일 0건 + 감사 스크립트 재실행 — 수치 플래그 0건, 잔여 플래그는 의도적 스텁 G6 계열 4건뿐. 바인딩 203+195=398 유지.
- **플레이 검증 포인트**: 위 8장 실사용 수치 확인 (특히 H45 꽃가루 유/무 각각, H34 3장째 화합, H23 반복 횟수).

---

## 21회차 완료 작업 (카드 감사 후속 1차 — 명백 버그 14건)

### ✅ CARD_AUDIT.md의 A(빈 강화)·B(반전/대상 오류)·E(CSV 타겟) 전건 수정

- **A. 빈 `_E` 메서드 4개 삭제** (G41_E/G49_E/G53_E/H32_E): 본문이 `yield return null`뿐임을 스크립트로 재확인 후 제거 → 기본 메서드 폴백 복구. 바인딩 검증: 직접 203 + 폴백 195 = 398, 실패 0.
- **B. 반전/대상 오류 7건**:
  - `G61 신의 뜻`: isTransformed 분기 반전 교정 (영웅 상태에서 60, 평상시 2)
  - `H38 철의 장막`: 방어 8 부여 루프를 `spawnedMonsterList` → `spawnedPlayerList`로 (자신 제외 조건 유지)
  - `G59 시체 걸기`: 사망 트리거 압도 3의 수신자를 `tar[0]`(시전자) → `target`(각 적)으로
  - `G5 빌린 이름`: 자신 방어 6(G4 복사본) → 적에게 쇠락 1 (G9의 적 쇠락 부여 플래그 관례 적용, 디밍 범위 0,2로)
  - `E30 창조의 권능`: E10 2장 생성 위치를 패 → 뽑을덱(prefareDeck, G23 패턴). ※ "덱"의 해석은 전투 덱으로 판단 — 영구 덱(deck) 의도면 알려줄 것
  - `E45 쌍둥이 자리`: 자신(tar[0])에게도 별무리 1 부여 추가 (버프 소비 로직은 미구현 상태 그대로 — 시스템 구현 시 동작)
  - `H58 어버이의 후예들`: `Contains("H0"/"H1")` 부분일치 → `IsIronToothCard()` 정확 일치(H0/H0_E/H1/H1_E), 버린덱 분량의 잊혀진덱 중복 추가를 `removableDeck.Clear()`로 차단, 디버그 로그 3줄 제거
- **E. CSV 타겟 3건**: G35·G36 `ValidTarget NONE → ENEMY`, H51 `NONE → MEMBER` (타겟팅 UI의 ENEMY/MEMBER 처리 존재 확인 후 적용)
- 검증: 컴파일 0건 + 에디터 바인딩 전수 검증.
- **플레이 검증 포인트**: G61 평상시/영웅 피해, H38 아군 방어, G59 처치 시 적 압도, G5 적 쇠락 타겟팅, E30 뽑을덱 매수, H58 발동 대상(H10번대 미발동), G35/G36 적만 타겟 가능, H51 아군 선택 가능.
- **남은 감사 후속**: C(수치 8건 — 기획 판단 대기), D(로직 버그 — G2/G16/G19/G31/H32/H34/E10 등), 에리스 버프 시스템 구현(설계 작업).

---

## 20회차 진행 (P2-17 — 빌드 검증, 부분 완료)

### 🔶 P2-17 — 스탠드얼론 빌드 검증: Mono 스모크 빌드 성공, IL2CPP는 모듈 설치 필요

- ✅ **Linux Mono 스모크 빌드 성공** (Unity 6000.3.7f1, 씬 3개 전부 포함, 총 1.1GB, 빌드 에러 0건) — 리팩토링 19회차 이후 플레이어 빌드 파이프라인(에셋 참조/코드 스트리핑/Resources 포함)이 통과함을 확인. 산출물에 Steamworks.NET DLL + `libsteam_api.so` 포함 확인.
- ⛔ **IL2CPP 검증은 에디터 모듈 미설치로 블로킹**: 현재 에디터에는 Linux Mono 변형만 설치됨 (Windows 지원 모듈 없음, Linux IL2CPP 변형 없음). Standalone 백엔드 설정은 Mono2x (계획서 지적대로 미명시 상태였음 — IL2CPP 전환은 모듈 설치 후에).
- **남은 사용자 작업** (스팀 친구 배포 빌드 만들 때): ① Unity Hub → 6000.3.7f1 → 모듈 추가 → **Windows Build Support (IL2CPP)** (스팀 배포 대상) 및/또는 Linux IL2CPP ② Player Settings → Standalone → Scripting Backend를 IL2CPP로 ③ 빌드 후 Steam 실행 상태에서 Steamworks 초기화(SteamFailUI 미출현) 확인. 이 단계는 세션에서 요청 시 함께 진행 가능.

---

## 19회차 완료 작업 (P3 — M_SoundManager 슬림화)

### ✅ SoundEffect/VoiceEffect 동일 복붙 2클래스 → 공통 베이스 통합 (-110줄)

- 문자 그대로 동일했던 95줄짜리 두 MonoBehaviour를 **`AudioEffect` 추상 베이스**로 통합, SoundEffect/VoiceEffect는 풀 구분용 빈 파생 타입으로 유지 (`FindObjectsByType<SoundEffect>` 등 기존 조회 경로 보존).
- 두 클래스 모두에 있던 **미사용 필드 `playbackPosition` 제거** (프로퍼티는 `audioSource.time`을 직접 읽고 있었음).
- 안전 근거: 두 클래스는 런타임 `AddComponent`로만 생성(TempSFX/TempVoice 오브젝트) — 씬/프리팹 직렬화 참조가 없어 상속 구조 변경이 데이터에 영향 없음. 외부 스크립트 사용처 0건 확인.
- 검증: 컴파일 0건 + 리플렉션(상속 구조, 기존 프로퍼티 API 10종 전부 유지).
- **플레이 검증 포인트**: 효과음/음성 재생 전반 (풀 재사용 — 같은 효과음 연속 재생, 음성 재생 완료 후 정리).

**리팩토링 계획 잔여**: P2-17 IL2CPP 빌드 검증(스팀 배포 빌드 제작 시 함께)만 남음. 코드 항목은 전부 완료.

---

## 18회차 완료 작업 (TMP 예제 폴더 삭제)

### ✅ TMP `Examples & Extras` 폴더 삭제 (에셋 250파일 제거, 잔여 CS0618 경고 소멸)

계획서에 "예제 미사용 확인 후 삭제"로 남아 있던 항목. **확인 결과 미사용이 아니었다** — GUID 전수 스캔(예제 에셋 130개 GUID ↔ 폴더 밖 전체 씬/프리팹/머티리얼)에서 게임이 실제 사용 중인 예제 에셋 3개 발견:

- **`Anton SDF.asset` — 프로젝트 기본 TMP 폰트였음** (TMP Settings의 m_defaultFontAsset + 씬 2개·핵심 프리팹 9개가 참조). 계속 재직렬화되던 미스터리 파일의 정체.
- `Fruit Jelly (B&W).jpg` / `Floor Cement.jpg` — Orb.mat, CardEnhancedOutline.mat이 사용하는 텍스처.

**조치**: 사용 중인 3개 + Anton SDF의 소스 폰트 `Anton.ttf`를 `AssetDatabase.MoveAsset`으로 **GUID 보존 이동** (Anton은 기존 프로젝트 폰트들이 있는 `TextMesh Pro/Resources/Fonts & Materials/`로 — Resources 상대 경로도 동일하게 유지, 텍스처는 신규 `Assets/Material/Texture/`) 후 폴더 삭제. 참조하던 씬/프리팹은 GUID가 그대로라 수정 불필요.

**검증**: 컴파일 0건, 삭제된 GUID 126개에 대한 전체 에셋 재스캔 — **깨진 참조 0건**. TMP 예제 스크립트가 사라져 잔여 CS0618 경고 전부 소멸 (2회차 때 수정했던 VertexZoom.cs 등 예제 스크립트도 함께 제거됨).

**플레이 검증 포인트**: 게임 전반의 텍스트 렌더링 (기본 폰트가 Anton SDF — 메뉴/로비/전투 UI 텍스트), 카드 강화 아웃라인·오브 UI 머티리얼 표시.

---

## 17회차 완료 작업 (P2-12)

### ✅ P2-12 — 밸런스 매직넘버 데이터화 (BalanceDB.csv 도입)

코드 곳곳에 박혀 있던 밸런스 수치를 **`Assets/Resources/DB/BalanceDB.csv` + 정적 로더 `BalanceData`**(CsvTable 기반, 지연 로드)로 중앙화. 이제 밸런스 튜닝은 재컴파일 없이 CSV 수정만으로 가능.

- **키 12종 전환**: 초기 HP 50/회복 15/골드 100(PlayerInterface), 이치 기본 3·에리스 2·상한 6(GamePlayerDeck_IchiPart), 전투 보상골드 10(RewardService), 상점 카드가 1(BattleSpawner, TODO 임시값), 턴당 드로우 5·상점 6·보상 3·제거 3(GamePlayerDeck).
- **안전 설계**: 파일/키 누락·정수 파싱 실패 시 에러 로그 + 호출부 기본값(기존 하드코딩 값)으로 동작 — CSV 문제가 게임을 멈추지 않음.
- **겸사겸사 정리**: `GamePlayerDeck.SetInitialValue`의 세이브/신규 분기 양쪽에 복붙돼 있던 초기화 5줄을 분기 밖 공통 경로로 통합. NPC 스폰 좌표 `new Vector3(11,-3,0)` 3곳 → `npcSpawnPosition` 상수. 모든 전환 지점은 서버 전용 경로([Command]/[Server]/서버 가드)라 동기화 영향 없음.
- **검증**: 컴파일 0건 + 에디터 런타임 검증 — 12개 키 전수 CSV 실조회 확인(센티널 기본값 테스트로 폴백 아님을 증명), 누락 키 폴백 동작 확인. 값이 기존과 동일해 동작 변화 0.
- **잔여(의도적 보류)**: 카드 데미지/방어 수치 데이터화는 P1-8b(데이터 주도 실행기)와 한 몸이라 그때 진행. 오더 슬롯 `i==0/1/2` enum화는 실익 대비 변경 범위가 넓어 보류. 버프 지속 수치 일부는 전투 로직과 결합돼 카드 실행기 설계 시 함께.
- **플레이 검증 포인트**: 게임 시작 시 초기 HP/골드/이치 표시, 캠프 회복량, 전투 승리 보상골드 10, 카드상점 가격 — 전부 기존과 동일해야 함.

---

## 16회차 완료 작업 (P2-15 3회차 — P2-15 완결)

### ✅ P2-15c — LobbyPlayer·HexagonMapRoom GetComponent 반복 캐싱

- **LobbyPlayer (49곳 → 2곳)**: 자기 자신·클래스 레이아웃 3종의 CanvasGroup/RectTransform 반복 조회를 Awake 1회 조회 캐시 필드 9개로 전환. `characterSelectCompleteImage.GetComponent<RectTransform>()`는 Image의 내장 `rectTransform` 프로퍼티로. `GetComponent<NetworkIdentity>().netId` 3곳은 NetworkBehaviour 내장 `netId`로. CmdSwapAccept/Reject의 "TryGetValue로 얻은 NetworkIdentity → GetComponent<LobbyPlayer> → 도로 GetComponent<NetworkIdentity>" 왕복 3곳을 이미 손에 든 identity의 `connectionToClient` 직접 사용으로 정리. 남은 2곳은 Start의 버튼 리스너 등록(1회성)뿐.
- **HexagonMapRoom (26곳 → 3곳)**: SyncList 콜백마다 반복되던 투표 아이콘 `GetComponent<SpriteRenderer>` 17곳을 Awake에서 구축한 렌더러 리스트 2개로 전환 (아이콘 리스트와 인덱스 정합 유지). `mapTileMask` SpriteMask 2곳 캐시. `GetComponent<Transform>().position` → `transform.position`, `NetworkClient.localPlayer.GetComponent<NetworkIdentity>()` → localPlayer 자체가 NetworkIdentity이므로 직접 전달. 남은 3곳은 동적 대상 조회(1회성 지역변수 포함)라 캐싱 부적합.
- 검증: 컴파일 0건 + 리플렉션(캐시 필드 존재, LobbyPlayer UserCode_ 6개 = Cmd 3 + TargetRpc 3 위빙 정상).
- **플레이 검증 포인트**: 로비 — 캐릭터 선택/해제 애니메이션, 오더 스왑 요청·수락·거절, 강퇴 아이콘, 클라 이탈 시 로비플레이어 제거. 맵 — 방 투표 아이콘 표시/갱신, 방 선택 확장 연출(마스크).

**P2-15 완결**: a(로컬 조회 캐싱 79곳) + b(사운드 폴링 제거) + c(GetComponent 캐싱). 계획서의 P2-15 항목 전부 처리.

---

## 15회차 완료 작업 (P2-15 2회차)

### ✅ P2-15b — M_SoundManager 매 프레임 볼륨 폴링 제거

OnUpdate 코루틴이 매 프레임 수행하던 볼륨 변경 감지 폴링(`IsMusicAltered/IsSoundFxAltered/IsVoiceAltered` — 믹서 `GetFloat` 최대 3회 + `Trim()` 문자열 할당 3회/프레임)을 제거.

- **제거 근거(전수 확인)**: 볼륨/음소거를 바꾸는 경로는 OptionUIManager 슬라이더·토글 → `MusicVolume/SoundVolume/VoiceVolume`·`IsMusicOn/IsSoundOn/IsVoiceOn` 프로퍼티 → `SetXVolume/ToggleXMute` 뿐이며, 이들은 전부 **즉시 적용형**(소스·이펙트·믹서 직접 반영). 외부에서 오디오 믹서를 `SetFloat`하는 코드는 프로젝트 전체에 없음 — 폴링이 감지할 "외부 변경"이 존재하지 않는다.
- **유일한 폴링 의존이던 시작 시 PlayerPrefs 적용**: `Initialise()` 끝(musicSource 구성 후)에 `ToggleXMute + SetXVolume` 6종 명시 호출로 대체.
- OnUpdate에는 실제로 매 프레임 필요한 것만 유지: 이펙트 풀 관리(ManageSound/VoiceEffects) + BGM 크로스페이드/페이드 전환.
- 한계(정직한 기록): 에디터 인스펙터에서 런타임에 `_musicVolume` 필드를 직접 조작하는 디버깅은 이제 실시간 반영되지 않음 (옵션 UI 경로는 정상). `SetSFXVolume`의 `FindObjectsByType<SoundEffect>` 스캔은 슬라이더 변경 시에만 실행되므로 유지.
- 검증: 컴파일 0건 + 리플렉션(감지 함수 3개 소멸, 적용 함수·OnUpdate 유지).
- **플레이 검증 포인트**: ① **게임 시작 시 저장된 볼륨/음소거 설정 적용** (핵심 — 적용 경로가 바뀜) ② 옵션에서 BGM/SFX/음성 슬라이더 실시간 변경 ③ 음소거 토글 ④ 씬 전환 BGM 크로스페이드.
- 남은 P2-15(후속): LobbyPlayer(49회)·HexagonMapRoom(26회) GetComponent 반복 캐싱.

---

## 14회차 완료 작업 (P2-15 1회차)

### ✅ P2-15a — 로컬 PlayerInterface 조회 캐싱 (79곳) + 스크롤 버튼 SetActive 상태 가드

- **`PlayerRegistry.Local` 도입**: `NetworkClient.localPlayer.GetComponent<PlayerInterface>()` 매 호출 재조회(41개 파일 79곳 — 계획서 추정 28곳보다 훨씬 많았음)를 캐시 접근자로 전체 전환. `PlayerInterface.OnStartLocalPlayer`에서 설정, `Unregister`(OnDestroy)에서 해제, 캐시가 비어 있으면(씬 전환 직후 등) localPlayer에서 1회 재조회하는 **자가 복구 게터**라 타이밍 회귀 위험 없음. RoomPlayer 조회나 localPlayer null 가드 등 다른 패턴 13곳은 의도적으로 유지.
- **GameUIManager 스크롤 버튼**: 매 프레임 무조건 `SetActive` 4~6회 호출하던 것을 상태 비교 후 변경 시에만 호출로 교정 (가시성 판정 로직은 동일).
- 검증: 컴파일 0건 + 에디터 리플렉션 검증(Local/SetLocal 존재, 비플레이 상태 Local 접근 시 예외 없이 null 반환).
- **플레이 검증 포인트**: 로컬 플레이어 UI 전반 — 덱 3종 버튼 팝업, 턴 종료 버튼, 카드 사용/타겟팅, 맵 방 클릭·투표, NPC 상호작용. 특히 **룸→게임씬 전환 직후**와 맵↔전투 왕복 후에도 정상인지 (Local 캐시 수명 검증).
- 남은 P2-15(후속): M_SoundManager OnUpdate 믹서 폴링 이벤트화, LobbyPlayer(49회)·HexagonMapRoom(26회) GetComponent 반복 캐싱.

---

## 13회차 완료 작업 (P2-16 죽은 코드 정리)

### ✅ P2-16 — 죽은 코드 정리 (-224줄, 로직 변경 0건)

전부 동작 보존 삭제. 컴파일 0건 + 에디터 리플렉션 검증(새 어셈블리 로드·Mirror 위빙 정상: M_TurnManager UserCode_ 14 / GamePlayerDeck 28 — 이전 회차 기록과 동일).

- **HexagonMapRoom.ChangeHexagonMapRoomLayoutState**: 계획서에서 "로직 버그 흔적, 의도 확인 필요"로 지적된 if/else 동일 분기 — 양쪽 본문이 문자 그대로 동일함을 확인하고 조건 제거, 34줄 → 8줄 단일 경로로 축약 (동작 동일). 인접한 `ChangeHexagonRoomActive`의 미사용 지역변수 `alpha`도 제거.
- **빈 SyncList 콜백 case 36블록 삭제** (7개 파일): GamePlayerDeck.SyncCallbacks 29 / M_LobbyMananger 4 / M_TurnManager 3콜백 / HexagonMapRoom·M_MapManager·M_TurnManager.CardQueue·TargetObject.Buff 각 1. 스택 라벨(fallthrough) 오삭제 방지를 위해 "라벨+빈 본문+break" run 전체만 제거하는 스크립트로 처리.
- **완전 미사용 메서드 2개 삭제**: `CardData.GeneralApDo`(호출부 0곳), `TargetObject.SetIronDemonParent`(미사용 `[ClientRpc]` — 호출부가 주석 1곳뿐. RPC 1개 제거라 TargetObject UserCode_ 수는 감소하나 전 클라이언트가 같은 빌드를 쓰므로 무해). 함께 콜백까지 통째로 빈 껍데기가 된 `GamePlayerDeck.OnAddtionCardUpdated`도 등록부와 함께 삭제.
- **주석 처리된 죽은 코드 3줄 삭제**: `M_TurnManager.MonsterSetOrder`의 `//phase = MONSTER_PREEFFECT`(계획서의 "의도 불명 상태 전이"), CardData_DanHyang의 `//SetIronDemonParent` 호출, GeneralApDo 내부 주석.
- **의도적으로 유지한 것(오탐 정정)**: ① **G6 계열 스텁 5개** — 계획서는 "존재하지 않는 카드 빈 메서드"로 분류했으나 CardDB.csv 150~153행에 실카드 4장이 존재하고 `curseEffect["G6"]` 등록 + DeckBookTab 제외 처리까지 있는 **미구현 컨텐츠 자리**라 삭제 불가. ② 카드 파일 주석 ~230줄 — 대부분 카드명·TODO 문서 주석이고 실제 죽은 코드 주석은 위 3줄뿐이었음(계획서의 67/88/77 집계는 전체 주석 라인 수). ③ `G2_Effect`의 주석 처리된 GainBuff — 등록된 저주 효과의 미완 구현 기록이라 컨텐츠 작업 목록으로 유지.
- **플레이 검증 포인트**: 맵 방 투표 시 내/타인 투표 레이아웃 표시(HexagonMapRoom 단순화 지점), 덱/버린덱/잊혀진덱 카운트 UI, 로비 오더 스왑 — 나머지는 순수 삭제라 회귀 위험 극히 낮음.

---

## 12회차 완료 작업 (P1-4 1회차)

### ✅ P1-4a — PlayerRegistry 도입 + 흐름 전이 판정을 M_TurnManager로 집중

순환 참조의 한 축(PlayerInterface → TurnManager/MapManager 흐름 제어)과 SyncVar 훅 내 전수 스캔 위험(분석 2-4항)을 함께 해소.

- **신규 `PlayerRegistry`**: PlayerInterface가 스폰/파괴 시 자기 등록/해제하는 정적 목록. `FindObjectsByType<PlayerInterface>` 전수 스캔 11곳 전부 대체 (성능 + 씬 전환 타이밍 취약성 제거). 파괴 누락 항목 자동 정리.
- **집계 판정 이동**: PlayerInterface의 SyncVar 훅 3개(턴종료/보상완료/레디)에 흩어져 있던 "전원 상태 체크 → 페이즈 전이/방 진입" 서버 로직을 M_TurnManager의 `CheckAllPlayersEndTurn/RewardDone/ReadyForMapMove`로 이동. 훅은 로컬 UI 갱신 + 알림 한 줄만 담당 — 전이 로직이 상태머신 소유자 한 곳에 모임.
- **NRE 수정**: 턴종료 집계의 `user.currentGamePlayer.HP` — currentGamePlayer가 null일 수 있는 타이밍(스폰 직후/해제 직후) 가드 추가.
- 검증: 컴파일 0건. **플레이 검증 포인트**: 전원 턴 종료 → 페이즈 전환, 전원 보상 수령 → 맵 복귀, 전원 레디 → 투표 방 이동, 게임씬 진입 로딩(로딩 매니저가 레지스트리 사용).
- 남은 P1-4b(후속): M_TurnManager ⇄ M_MapManager 상호 호출 정리(이벤트화) — 필요성 낮아지면 보류 가능.

---

## 11회차 완료 작업 (P1-10 일부)

### ✅ P1-10a — TargetObject 책임별 partial 분리 (929줄 → 코어 347줄, -63%) + OnDestroy 크래시 수정

- **크래시 수정**: `OnDestroy`가 몬스터/NPC 타입에는 없는 `playerMessageCavnas`에 무방비 접근하던 NRE 위험(분석 때 지적) — null 가드 추가.
- **partial 분리**: 코어(필드/SyncVar/초기화/애니 콜백 347줄) + Buff(버프 시스템 182줄) + Damage(피해·사망 153줄) + CharacterSpecific(고행·에리스·철귀 127줄) + Voice(음성·말풍선 120줄). 스크립트 파일 GUID가 유지되므로 TargetObject 프리팹 참조에 영향 없음.
- 검증: 컴파일 0건 + 리플렉션 검증(메서드 누락 0, 위빙 정상).
- **보류(의도적)**: 플레이어/몬스터 클래스 완전 분리는 스폰 프리팹 이원화 + SyncVar 재배치가 필요한 네트워크 계약 변경 — partial 경계가 확정됐으므로 필요해지는 시점(예: 캐릭터 4번째 추가)에 진행 권장.

---

## 10회차 완료 작업 (P1-8 일부)

### ✅ P1-8a — 카드 강화(_E) 보일러플레이트 191개 제거 (-765줄)

- **새 규약 도입**: 강화 카드의 효과가 기본 카드와 동일하면 `_E` 메서드를 생략할 수 있다. `CardData.CreateCardDelegate()`가 로드 타임에 `_E` 메서드 부재 시 기본 메서드로 자동 바인딩.
- 캐릭터별 카드 파일 3개에서 `yield return 본체(card,tar);` 한 줄짜리 순수 위임 래퍼 191개 삭제 (Geork 71 / Eris 60 / DanHyang 60). **실제 강화 로직이 있는 _E 메서드 8개는 유지.**
- **에디터 런타임 검증**: CardDB 398행 전수 — 직접 바인딩 207 + 폴백 191 + 실패 0 (삭제 수와 폴백 수 정확히 일치).
- 효과: 앞으로 강화 효과가 다른 카드만 `_E` 메서드를 작성하면 됨. 카드 파일 합계 4,414줄 → 3,649줄.
- 남은 P1-8b(선택): 공통 실행 골격(디밍→애니→대기→효과→피격→디밍해제)의 데이터 주도 실행기 — 카드 컨텐츠 제작 방식을 바꾸는 설계 작업이라 별도 진행 권장.

---

## 9회차 완료 작업 (P1-11 일부)

### ✅ P1-11a — 몬스터 피격 애니메이션 복붙 통합 (12개 파일, -81줄)

- `SpawnedMonster` 베이스에 `PlayHitAnimationSequence(애니이름, 대기시간)` + `RpcPlayHitAnimation` 공통 경로 추가.
- 서브클래스 12개의 복붙된 `OnHitAnimation`+`OnHitAnimationRPC` 쌍을 한 줄 위임으로 축약. **분석 정정**: RPC 본문이 "13개 동일 복붙"이 아니라 스파인 애니메이션 이름이 3종(Defense0/Defence0/3Defence)으로 달랐음 — 파라미터화로 해결.
- Boss_Momos는 피격 음성 재생이 결합된 고유 구현이라 의도적으로 유지.
- **보류(의도적)**: ① OnStartClient 인디케이터 오프셋 8곳 — 20종 중 8종에만 적용된 조건부 동작이라 베이스 승격 시 나머지 12종의 표시 위치가 바뀜. ② DoAction 스위치 골격 — 데이터 주도(ScriptableObject) 전환이 정답이며 별도 설계 작업으로 분리.

---

## 8회차 완료 작업 (P1-5)

### ✅ P1-5a — Card.experience 동기화 버그 수정

계획 수립 때 지적된 잠재 버그를 수정. `CardOnHand.card`는 클래스 타입 SyncVar라 **같은 참조의 내부 필드 변경(experience++, costAddition±)은 클라이언트에 전파되지 않았다** — 서버 판정은 정상이지만 클라 UI의 경험치 핍·비용 표시가 어긋나는 버그.

- `M_TurnManager.CardQueue.cs` 카드 실행 완료 지점에서 `cardOnHand.card = new Card(cardOnHand.card)` 재할당으로 SyncVar 전파 강제 — 경험치와 특성(숙련/중력) 비용 가감이 함께 동기화됨.
- **덤으로 발견·수정**: `Card(Card)` 복사 생성자가 5개 필드(isReturnable/isSoldout/cardPrice/stackCount/isChargedCard)를 누락하고 있었음 — 전체 필드 복사로 보강, cardCharacteristics도 참조 공유 대신 리스트 복사로 교정.

### ✅ P1-5b — GamePlayerDeck 책임별 partial 분리 (1,497줄 → 코어 402줄, -73%)

M_TurnManager과 같은 방식(코드 원문 그대로 이동, 네트워크 계약 보존)으로 분리:

| 파일 | 내용 | 규모 |
|---|---|---|
| `GamePlayerDeck.cs` (코어) | SyncVar/SyncList 필드, 초기화, 코스트 계산, 카드 큐 예측, 드로우 충전 | 402줄 |
| `.DeckOps.cs` | 덱 간 카드 이동/추가/제거/강화 (Command 12 + TargetRpc 4 포함) | 481줄 |
| `.SyncCallbacks.cs` | SyncVar 훅 4개 + SyncList 콜백 9개 | 363줄 |
| `.Draw.cs` | 드로우/핸드/어빌리티 카드 스폰 | 199줄 |
| `.RewardShop.cs` | 보상/상점 커맨드 | 52줄 |
| (기존) `_IchiPart.cs` | 이치(코스트) 파트 | 유지 |

**검증**: 컴파일 0건 + 리플렉션 검증(메서드 누락 0, Mirror UserCode_ 위빙 28개 정상, 오버로드 2쌍 보존).

---

## 7회차 완료 작업 (P1-9)

### ✅ P1-9 — 사운드 클립 조회 안전화 (125곳)

계획서에서 "런타임 NRE 위험이 코드 전역 산재"로 지적된 원시 클립 접근을 **전부 안전 조회 헬퍼로 전환**. 이제 클립 이름 오타/인덱스 범위 초과가 크래시 대신 원인이 명시된 에러 로그가 된다.

- **M_SoundManager에 헬퍼 5종 추가**: `GetBGMClip/GetSFXClip(type, name)`(이름 조회), `GetSFXClipAt/GetVoiceClipAt(type, index)`(인덱스 조회), `GetVoiceClips(type)`(목록 조회) — 전부 TryGetValue + 실패 시 에러 로그 + null/빈 목록 반환.
- **전환 규모**: 39개 파일 125곳 — 이름 Find 패턴 74곳(SFX 59/BGM 15), 인덱스 접근 46곳(SFX 16/음성 30), 목록 접근 5곳.
- **재생 경로 안전 확인**: 종단 재생 함수(PlayAudioClipBGM/SFX/Voice)에 기존 null 가드가 있어, 헬퍼가 null을 반환해도 재생은 조용히 스킵 + 에러 로그로 원인 추적 가능.
- `GetVoiceClipsByVoiceType`(음성 인덱스 범위 조회)도 범위 가드 적용.
- 한계(정직한 기록): 클립 조회 직후 `clip.length`를 쓰는 일부 호출부는 클립 누락 시 여전히 NRE 가능 — 다만 직전에 어떤 클립이 없는지 에러 로그가 남아 즉시 진단 가능. 완전한 SoundId enum 체계 도입은 선택적 후속 작업.

---

## 2회차 완료 작업 (P1-6, P1-7)

### ✅ P1-6 — Command 권한 검증 (치트/오작동 방어)

Mirror의 트레일링 `NetworkConnectionToClient sender = null` 파라미터(서버가 자동 주입)를 활용해 기존 호출부 수정 없이 검증을 추가.

| 위치 | 추가된 검증 |
|---|---|
| `M_LobbyMananger.CmdSwapLobbyPlayer / CmdRequestSwap` | ① 슬롯 인덱스 범위 검증 ② **요청자가 스왑 당사자(두 슬롯 중 하나의 소유자)인지 검증** — 임의 클라이언트가 남의 로비 오더를 조작하던 구멍 차단 |
| `M_MapManager.CmdSwapMapPlayer / CmdRequestSwap` | ① 플레이어 슬롯(0~2)만 허용 — 몬스터 슬롯 조작·범위 밖 인덱스 방어 ② 스왑 당사자 검증 (거부 시 경고 로그) |
| `GamePlayer.CmdAddGoldValue` | **클라가 보낸 `localPlayerNetId` 파라미터 제거** — 서버가 아는 자기 오브젝트의 `netId` 사용(발신자 위장 방지). `giveGold <= 0` 방어 추가(역방향 갈취 방지). 호출부 `CampPopUp.cs` 갱신 |
| `TargetObject.DrawGoHengCard` | 고행 발동 요청자가 해당 타겟오브젝트의 소유 플레이어인지 검증 |
| `M_CardManager.CMDCurseCardEffect` | card/baseCard/tar null 가드 — 서버 저주 큐 오염 방지 |

미조치(의도적): `M_MessageManager.CmdSendChatMessage`의 playerName 위장 — 3인 코옵에서 실익 대비 과한 방어라 보류. 필요 시 sender의 identity에서 이름을 역산하는 방식으로 추가 가능.

### ✅ P1-7 — CSV 파서 공통화

**신규 파일**: `Assets/Script/DB/CsvTable.cs`
- **헤더 기반 컬럼 매핑** (`row.Get("Name")`, `GetInt`, `GetEnum<T>`) — 컬럼 순서 변경/삽입에 안전. 존재하지 않는 컬럼은 최초 1회 에러 로그.
- **따옴표 필드 지원** (`"a,b"`, `""` 이스케이프) — 설명 텍스트에 콤마가 들어가도 행이 깨지지 않음.
- 위치 기반 인덱서 `row[i]`는 범위 밖 접근 시 빈 문자열 반환 (MonsterDB의 반복 3컬럼 구조용).
- 행별 파일 라인 번호 보존 → 오류 로그에 `(N행)` 표기.

**전환된 로더** (5개 파일, `while+ReadLine+Split` 복붙 루프 전부 제거):
- `CardData.cs` — CardDB(헤더 매핑 + 특성 가변 컬럼), Description, CardCharacteristic
- `BuffData.cs` — BuffDB (행 단위 오류 격리 추가)
- `ItemData.cs` — Artifact/Legacy 중복 로더 2개를 공통 `LoadItemTable()` 하나로 통합
- `MonsterData.cs` — MonsterDB(위치 기반 유지 + 행 단위 오류 격리), MonsterGroupDB(헤더 매핑). **개선**: 그룹에 존재하지 않는 몬스터 이름이 있으면 종전처럼 null을 리스트에 넣지 않고 에러 로그 후 스킵.

**런타임 검증 (에디터 내 실행)**:
- 8개 CSV 전부 파싱 행 수 = 원본 비어있지 않은 행 수 - 헤더 → **데이터 손실 0건** (CardDB 398 / BuffDB 43 / MonsterDB 44 / MonsterGroupDB 15 / Artifact·Legacy 각 2 / Description 30 / CardCharacteristic 17)
- 헤더 매핑 샘플 검증: CardDB 첫 행 필드 일치 확인
- **CardDB 398장 전수 CardNo↔메서드 리플렉션 매칭 OK** (누락 0건), Artifact/Legacy 메서드 매칭 OK

---

## 이번 회차에 완료한 작업

### ✅ 사전 작업 — Unity 6 마이그레이션 컴파일 오류 해결

TMP 예제 스크립트 2개가 Unity 6 내장 TMP의 `uvs0` 타입 변경(`Vector2[]` → `Vector4[]`)으로 컴파일 실패하던 것을 수정.

| 파일 | 수정 내용 |
|---|---|
| `Assets/TextMesh Pro/Examples & Extras/Scripts/VertexZoom.cs` | UV 배열 선언 2곳 `Vector4[]`로 변경, `mesh.uv = uvs0` → `mesh.SetUVs(0, uvs0)` |
| `Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextSelector_B.cs` | `src_uv0s`/`dst_uv0s` 선언 `Vector4[]`로 변경 |

### ✅ 0단계 — obsolete API 32건 교체 (계획 P2-13 선행 처리)

`Assets/Script` 전체에서 Unity 6 obsolete API를 권장 API로 일괄 교체. 벤더링 라이브러리(`Assets/ExternalLibrary`)는 의도적으로 제외.

- `FindObjectOfType<T>()` → `FindFirstObjectByType<T>()` — 4건 (싱글톤 베이스 `SingletonD`, `NetworkSingletonD`, `InstaceD` 포함)
- `FindObjectsOfType<T>()` → `FindObjectsByType<T>(FindObjectsSortMode.None)` — 28건 (정렬 생략으로 기존보다 오히려 빨라짐)
- 대상 파일 10개: M_NetworkRoomManager, M_LoadingManager, M_LobbyMananger, M_TurnManager, M_SoundManager, PlayerInterface, SaveTest, 싱글톤 베이스 3종

### ✅ P0-1 — `spawned[]` 무검증 인덱싱 안전화 (**56곳**, 계획서 추정 33곳보다 많았음)

**신규 파일**: `Assets/Script/Common/NetLookup.cs`
`NetworkServer/NetworkClient.spawned` 딕셔너리를 `TryGetValue`로 안전 조회하는 정적 헬퍼. 조회 실패 시 **어떤 netId·타입이 없는지 경고 로그**를 남기고 null 반환 → 기존의 무언(無言) `KeyNotFoundException` 크래시가 "추적 가능한 로그 + 방어 가능한 null"로 바뀜.

- `NetLookup.Server<T>(netId)` / `NetLookup.Client<T>(netId)`

**전환 규모**: 17개 파일 56곳 전부 `spawned[id].GetComponent<T>()` → `NetLookup.Server/Client<T>(id)` 전환.
(M_TurnManager 23, M_NetworkRoomManager 6, M_MapManager 5, PlayerInterface 4, M_CardManager 3, PlayerOrder 3, CardData_DanHyang 3, CardData_Eris 2, MapPlayerPiece 2, GamePlayerTarget·AbilityButton·MapPlayerDestination·MapUI·GameUIManager 각 1 등)

**추가로 null 가드까지 넣은 고위험 지점**:

| 위치 | 조치 |
|---|---|
| `M_NetworkRoomManager.AssignAuthorityFromDisconnectClientToServer` | serverPlayer/disconnectedPlayer null 시 기존 "로딩 중 이탈" 폴백 분기로 유도 — **접속 해제 크래시 경로 차단** |
| `M_NetworkRoomManager.BroadCastToClientDisconnected` | 두 플레이어 조회 성공 시에만 RPC 발송 |
| `M_TurnManager.GetCurrentPlayerTargetObject` | Find 람다 내 조회 실패 시 false 반환 (서버/클라 양쪽) |
| `M_TurnManager.GetTargetObjectFromActionTarget` | `AddIfSpawned` 로컬 헬퍼로 재구성 — 사망/접속해제로 무효해진 타겟은 건너뛰고, 유효 타겟이 하나도 없으면 전체 플레이어로 폴백 (몬스터 행동이 null 타겟으로 크래시하던 경로 제거) |
| `M_TurnManager.OnChangeSpawnedPlayer/MonsterUpdated` (SyncList 콜백) | SyncList 델타가 스폰 메시지보다 먼저 도착하는 Mirror 레이스에서 NRE 나던 것을 위치 폴백으로 가드 — 인디케이터는 생성 유지 |
| `GameUIManager.HandleCardQueuePopUp` | 불필요한 이중 GetComponent 정리 |

**한계(정직한 기록)**: 나머지 일반 지점들은 조회 실패 시 null이 반환되므로 후속 역참조에서 NRE가 날 수 있음. 다만 (a) 실패 원인이 로그로 남고, (b) 크래시 지점이 예외 없는 딕셔너리 내부가 아니라 호출부가 되어 디버깅이 가능해짐. 완전한 가드는 P1 구조 분해와 함께 진행 권장.

### ✅ P0-2 — 카드/아이템 리플렉션 바인딩 로드타임 검증

**`Assets/Script/DB/CardData.cs`**:
- `LoadCardDataFromDB()` 파싱 루프를 **카드(행) 단위 try/catch**로 감쌈 — CSV 메서드명 오타·파싱 오류가 있어도 해당 카드만 실패 처리하고 나머지는 정상 로드. 종전에는 오타 1건이 전체 카드 로드를 중단시켰음.
- 실패 카드를 집계하여 `[CardData] CardDB 로드 실패 N건 …` 에러 로그로 카드번호·예외 내용을 일괄 출력.
- 빈 줄 스킵 가드 추가.
- 카드 실행 시점의 `CardMethods[key]` 무검증 인덱싱 4곳 → `GetCardMethod()` 안전 조회로 교체. **강화(`_E`) 메서드 누락 시 기본 메서드로 폴백**하고 에러 로그 (게임은 계속 진행).
- `curseEffect[key]` → `TryGetValue` + 누락 시 빈 코루틴 반환.

**`Assets/Script/DB/ItemData.cs`**:
- Artifact/Legacy 로더 두 곳 모두 아이템 단위 try/catch + 실패 로그 + 빈 줄 가드. 동일 원리.

### ✅ P2 퀵윈 — PopUpUIManager 델리게이트 타입 오타

`PopUpUIManager.cs:84,86` — 카드 **제거** 팝업 델리게이트 필드가 카드 **강화** 팝업 타입(`OnCardEnhancePopUpShow/Hide`)으로 선언돼 있던 복붙 오타를 `OnCardRemovePopUpShow/Hide`로 교정. 구독부(`CardRemovePopUp.cs`)는 메서드 그룹 방식이라 호환 확인 완료.

### ✅ 오탐 정정 (계획서 수정 사항)

- **P2-14 ParrelSync 이중 설치 → 오탐**. `Assets/Plugins/ParrelSync/`에는 설정용 ScriptableObject만 있고 실제 코드는 UPM 패키지(`com.veriorpies.parrelsync`) 단독. **조치 불필요.**

---

## 변경 통계

- 스크립트 변경: **24개 파일, +196 / -132줄** (신규 `NetLookup.cs` 포함)
- 컴파일: error CS **0건** (잔여 경고는 TMP 예제 폴더의 CS0618뿐 — 게임 코드와 무관, 폴더 삭제 시 소멸)
- 동작 검증 상태: **에디터 컴파일 통과까지 확인**. 멀티플레이 스모크 테스트(호스트 + ParrelSync 클론 클라 1)는 미실시 — 아래 "다음 작업" 1번으로 권장.

---

## 남은 작업 (우선순위순, `REFACTORING_PLAN.md` 기준)

| # | 항목 | 상태 | 비고 |
|---|---|---|---|
| 1 | **P1-4a 플레이 테스트** | ⬜ 필요 | 12회차(PlayerRegistry) + 13회차(P2-16) 변경 검증. `NEXT_SESSION.md` 체크리스트 참고 |
| 2 | .mat 82건 등 재직렬화 커밋 | ⬜ 대기 | 무해한 포맷 업그레이드. 리팩토링 커밋과 분리 권장 |
| 3 | ~~P1-6: Command 권한 검증~~ | ✅ 완료 (2회차) | |
| 4 | ~~P1-7: CSV 파서 공통화~~ | ✅ 완료 (2회차) | 런타임 파싱 검증까지 완료 |
| 5 | ~~P1-3: M_TurnManager God Class 분해~~ | ✅ 실질 완료 (3~6회차) | 코어 -59% + 컴포넌트 3개 추출. CardQueue/IronDemon/Presentation은 RPC 의존이 커서 partial 유지가 적절 |
| 6 | P1-4: 매니저 순환 참조 디커플링 | 🔶 P1-4a 완료 (12회차) | P1-4b(TurnManager⇄MapManager 이벤트화)는 필요성 낮아 보류 가능 |
| 7 | ~~P1-5: GamePlayerDeck 분해 + `Card.experience` 동기화 버그~~ | ✅ 완료 (8회차) | 동기화 버그 수정 + partial 분리(코어 -73%). 컴포넌트 추출은 선택적 후속 |
| 8 | P1-8: 카드 효과 3파일 중복 제거 | 🔶 P1-8a 완료 (10회차) | `_E` 래퍼 191개 제거. P1-8b(데이터 주도 실행기)는 카드 대량 추가 전 별도 설계 작업 |
| 9 | ~~P1-9: 사운드 클립 조회 안전화~~ | ✅ 완료 (7회차) | 125곳 헬퍼 전환. SoundId enum 완전 도입은 선택적 후속 |
| 10 | ~~P1-10/11: TargetObject 분리, 몬스터 공통 시퀀스 상향~~ | ✅ 실질 완료 (9·11회차) | 클래스 완전 분리·DoAction 데이터화는 컨텐츠 확장 시점에 |
| 11 | P2 잔여: 밸런스 데이터화(P2-12), 폴링/캐싱 정리(P2-15), IL2CPP 빌드 검증(P2-17) | 🔶 P2-16 완료 (13회차) | TMP `Examples & Extras` 폴더 삭제도 여기서 (미사용 확인 후) |

## 다음 회차 권장 진행

1. **플레이 테스트** — `NEXT_SESSION.md` 체크리스트(P1-4a 흐름 관문 전부 + 13회차 맵 투표 레이아웃) 수행.
2. 이상 없으면 13회차(P2-16)분 커밋.
3. 다음 코드 작업: **P2-15 매 프레임 폴링/캐싱 정리** (GameUIManager.Update, M_SoundManager 믹서 폴링, localPlayer.GetComponent 체인 캐싱) → P2-12 밸런스 데이터화 순.
