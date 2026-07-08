# BlackOcean 리팩토링 진행 결과

> 작성일: 2026-07-08 (1·2회차)
> 기준 문서: `REFACTORING_PLAN.md` (P0~P3 우선순위 계획)
> 검증: 각 단계마다 Unity 6000.3.7f1 에디터 리컴파일 통과 확인 (최종 error CS 0건) + 2회차는 에디터 내 런타임 파싱 검증 수행

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
| 1 | **멀티플레이 스모크 테스트** | ⬜ 필요 | 1·2회차 변경 검증. MenuScene→Room→Game 전투 1회 + 오더 스왑 + 골드 전달 + 클라 강제 이탈 시나리오 |
| 2 | .mat 82건 등 재직렬화 커밋 | ⬜ 대기 | 무해한 포맷 업그레이드. 리팩토링 커밋과 분리 권장 |
| 3 | ~~P1-6: Command 권한 검증~~ | ✅ 완료 (2회차) | |
| 4 | ~~P1-7: CSV 파서 공통화~~ | ✅ 완료 (2회차) | 런타임 파싱 검증까지 완료 |
| 5 | P1-3: M_TurnManager God Class 분해 (8책임) | ⬜ 미착수 | 주 단위. 플레이 테스트 가능 상태에서 진행해야 안전 |
| 6 | P1-4: 매니저 순환 참조 디커플링 | ⬜ 미착수 | P1-3과 병행 |
| 7 | P1-5: GamePlayerDeck 분해 + `Card.experience` 동기화 버그 수정 | ⬜ 미착수 | experience는 실행부 안전화만 됨, 동기화 자체는 미해결 |
| 8 | P1-8: 카드 효과 3파일 중복 제거 (공통 실행기, `_E` 래퍼 190개) | ⬜ 미착수 | `_E` 폴백 도입으로 위험은 낮아짐 |
| 9 | P1-9: 사운드 문자열 조회 75곳 ID화 | ⬜ 미착수 | |
| 10 | P1-10/11: TargetObject 분리, 몬스터 공통 시퀀스 상향 | ⬜ 미착수 | |
| 11 | P2 잔여: 밸런스 데이터화, 폴링/캐싱 정리, 죽은 코드 정리, IL2CPP 빌드 검증 | ⬜ 미착수 | TMP `Examples & Extras` 폴더 삭제도 여기서 (미사용 확인 후) |

## 다음 회차 권장 진행

1. **스모크 테스트** — 1·2회차 변경을 실제 멀티 환경에서 확인. 관찰 포인트: 클라 강제 이탈 시 `NetLookup` 경고 로그, 로비/맵 오더 스왑 정상 동작(권한 검증 추가됨), 골드 전달, 카드 로드 에러 로그 유무.
2. 이상 없으면 리팩토링분 커밋 → .mat 재직렬화 별도 커밋.
3. P1-3(M_TurnManager 분해) 착수 — 가장 큰 덩어리. `TargetIndicatorController`(뷰 로직 ~210줄)와 `BattleSpawner`(스폰 팩토리 ~210줄)처럼 경계가 명확한 것부터 떼어내는 순서 권장.
