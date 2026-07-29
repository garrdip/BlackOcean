# 버그·정합성 수정 TODOLIST

> `GAME_DESIGN.md` §14 「미구현·기획 확인 필요 목록」 중 **B. 밸런스·정합성 이슈** / **C. 코드 품질·데이터 오류** 기반 작업 목록.
> 컨텐츠 공백(§14-A)은 버그 수정이 아니므로 본 목록에서 제외 (맨 아래 참고란에만 정리).
> 진행 방식: 회차별로 [ ] 체크 → ParrelSync 플레이 테스트 → 한글 커밋.

---

## 🔴 P1 — 게임플레이에 직접 영향 (즉시 수정)

### 1. 게임오버 판정에서 에리스 영구 제외
- [x] `GamePlayer.cs:117-125` — 전멸 집계의 `!IsEris` 조건 수정
- 증상: 에리스가 파티에 있으면 전원 HP 0이어도 게임오버가 발생하지 않음
- 수정 방향: 에리스는 "광기 변신 가능 상태"일 때만 예외 처리하고, 광기 상태에서 사망하면 전멸 집계에 포함
- 검증: 에리스 포함 파티로 전원 사망 → 게임오버 팝업 확인 (광기 변신 1회 경유 케이스 포함)

### 2. 전투 종료 시 이치 하드코딩 리셋
- [x] `RewardService.cs:53-54` — `maxIchi=3; currentIchi=3` 하드코딩 제거
- 증상: 에리스(최대 2)의 기본값이 전투 종료마다 3으로 덮어써짐
- 수정 방향: `SetInitialIchi()` 재사용 — 캐릭터별 기본값(BalanceDB `ICHI_INIT_DEFAULT`/`ICHI_INIT_ERIS`)으로 리셋
- ✅ 기획 확정(2026-07-29): H5/H6의 최대 이치 증가는 **해당 전투 한정** — 전투 종료 시 기본값 복귀가 정상
- 검증: 에리스로 전투 종료 후 이치 2 유지 확인 / 홍단향 H5 사용 시 전투 중 최대 이치 4 → 전투 종료 후 3 복귀 확인

### 3. 홍단향 철귀이동(HA) 페이즈·검증 부재
- [x] `CardData_DanHyang.cs:70-79`, `AbilityButton.cs` — 철귀이동에 서버측 페이즈 검증 추가 (실제 수정 지점: `AbilityCtrlArrow.CmdEnQueueCardData`)
- 증상: 버튼 가시성만이 게이트라 `PLAYER_ACTIVE` 외 페이즈(몬스터 턴 등)에도 이동 명령 가능
- 수정 방향: 게오르크 어빌리티(`TargetObject.CharacterSpecific.cs:20-56`)와 동일하게 서버에서 `phase == PLAYER_ACTIVE` 검증 (횟수 제한은 기획상 무제한 유지)
- 검증: 몬스터 턴 중 어빌리티 명령 전송 시 반려되는지 확인

### 4. `sizeOfIronDemon` NRE 위험
- [x] `sizeOfIronDemon` 프로퍼티에 null 체크 추가 (IRONDEMON 버프 제거 상태 접근 시)
- 검증: 철귀 버프 제거 상황 재현 후 콘솔 NRE 없음 확인

### 5. `GamePlayerItem` 획득 시 예외 구조
- [x] `GamePlayerItem` — 아이템 이름/번호 키 불일치로 획득 시 예외 발생하는 구조 수정
- 참고: 아이템 시스템 자체는 스텁이지만, 예외가 터지는 코드 경로는 정합성 문제이므로 키 매칭만 먼저 정리
- 검증: 치트/테스트 경로로 아이템 획득 시 예외 없음 확인

---

## 🟠 P2 — 규칙·수치 정합성 (기획 수치와 코드 불일치)

### 6. `ICHI_LIMIT(6)` 상한 미적용
- [x] `GamePlayerDeck_IchiPart.cs` — `IncreaseMaxIchi(amount)` 공통 경로 신설(`limitiChi` 클램프), H5/H6가 이를 사용하도록 변경
- 수정 방향: 최대 이치 증가 지점(H5/H6 등)에서 `ICHI_LIMIT`로 클램프
- 검증: 최대 이치를 6 초과로 올리는 시도 시 6에서 고정 확인 (한 전투에서 H5/H6 반복 사용)

### 7. 에리스 「꿈을 본 인형」 DB 스펙 미구현 3항목 — ⚠ 기획 확정 선행
- [ ] 기획 확정: DB 스펙대로 갈지, 현재 코드 동작을 스펙으로 삼을지 결정
  - "한 전투에 한 번" 제한 — 코드는 무제한
  - "현재 체력 2배 이하의 공격" 조건 — 코드는 조건 없음
  - "체력 2 이상 복귀 시 해제" — 코드는 해제 없음 (전투 내내 광기)
- [ ] 확정안에 맞춰 `TargetObject.Damage.cs:48-53` 수정 또는 `BuffDB.csv` 설명문 수정
- [ ] 미사용 `ERIS_NORMAL/2ND/3RD` 버프 정리 (구현 or enum·CSV 삭제)

### 8. 손패 10장 상한 미구현
- [ ] `maxCardOnHandCount=10`이 레이아웃 상수로만 존재 — 실제 드로우/생성 시 상한 강제 추가
- ⚠ 기획 확정: 초과분 처리 방식(드로우 스킵 vs 버린덱행) 결정 필요
- 검증: 손패 10장 상태에서 드로우/카드 생성 시도

### 9. 카드 강화/제거 골드 미소모 + 상점 가격 임시값
- [ ] 상점 NPC 강화/제거에 골드 소모 로직 추가
- [ ] `SHOP_CARD_PRICE=1` 임시값 → 정식 가격 테이블 결정 후 BalanceDB 반영
- ⚠ 기획 확정: 강화/제거/카드 가격 수치 필요
- 검증: 골드 부족 시 구매/강화/제거 불가 확인, 멀티에서 골드 동기화 확인

### 10. 행동비용 초기값 3 vs 최대 30 — 의도 확인
- [ ] `M_MapManager.cs:189-195` — `currentActionCost=3` / `maxActionCost=30` 의도 확인 후 수정 or 확정
- ⚠ 기획 확정: 시작 행동비용 수치 결정 (현재 값이면 3칸 이동 만에 맵 보스 출현)

### 11. Saddy 행동 frequency 합 150 (100 초과)
- [x] `MonsterDB.csv` — Saddy 패턴 Frequency 50/50/50 → 34/33/33 (등확률 의도 가정)
- 증상: 룰렛이 0~99 롤이라 세 번째 패턴(방어 100)이 **한 번도 선택되지 않았음**
- 검증: Twins(Happy+Saddy) 전투에서 세 행동(두번공격/힘버프/방어) 모두 등장 확인

### 12. Guardian·Saddy 상시 버프 행 파서 주석 처리
- [x] MonsterDB 파서의 상시 버프 행 재구현 (`MonsterData.cs` 파싱 + `BattleSpawner.cs` 스폰 시 `GainBuff` 적용)
- [x] `BuffIndicatorController.cs` — 아이콘 미등록 버프의 `KeyNotFoundException` 방어 (에러 로그로 대체)
- 증상: 두 몬스터의 상시 버프가 미작동
- 검증: Guardian 전투 진입 시 버프 등록 확인 (아이콘 스프라이트는 미등록 → 빈 아이콘 + 에러 로그)
- ⚠ 잔여 (기획·아트 필요):
  - SUHOJA 아이콘이 `BuffData.buffIcons`(MenuScene)에 미등록 — 스프라이트 지정 필요
  - SUHOJA 실제 효과 코드 미구현 + BuffDB.csv 행 없음 (#16과 연계)
  - Saddy의 상시 버프 `E2`는 존재하지 않는 BuffType — 구 명명(E1=Happy, E2=Saddy 추정)의 잔재로 보이며 실제 어떤 버프인지 기획 확정 필요 (현재는 로드 시 행 단위 에러 로그 후 스킵)

### 13. G2(고행 III) 저주 사후효과 주석 처리로 무효과
- [ ] `CardData_Geork.cs` — G2 사후효과("고행길") 주석 구간 확인
- ⚠ 기획 확정: 의도적 무효과인지, 효과 복원인지 결정 (G0=방어 10 삭감, G1=붕괴 1 대비 G2만 공백)

---

## 🟡 P3 — 데이터·CSV 오류 (코드 수정 없이 데이터만)

### 14. CSV 오타 일괄 수정
- [x] `BLESS ` 끝 공백 10건 (CardDB.csv) — 해당 10장(에리스 축복)은 **로드 자체가 실패**하고 있었음
- [x] `BYULMOORI ` enum 끝 공백 → 별무리 특성 툴팁 로드 실패 원인 (CardCharacteristic.csv)
- [x] `Garde` → `Grade` 오타 (ArtifactDB/LegacyDB 헤더 + `ItemData.cs` 참조)
- [x] (추가 발견) `GOOWON ` 끝 공백 14건 (CardDB — H3/H4 계열 로드 실패), `WHOLE ` 5건 (MonsterDB)
- [x] (구조 개선) `CsvTable`이 모든 필드를 Trim — 같은 부류의 오타가 재발해도 파싱이 깨지지 않음
- 검증: 별무리 카드 마우스오버 시 특성 툴팁 정상 표시 / 에리스 축복 카드(E20 등)·H3/H4가 카드풀에 등장

### 15. Description.csv 툴팁 누락 11종
- [x] 신규 용어 9종 추가: `고정피해`/`고행길`/`영웅카드`/`이치의저주`/`이치의축복`/`철귀`/`크기`/`파괴의권능`/`전열`
- [x] 조사 붙은 토큰(`@압도를`/`@크기의`/`@전열과`) — `CardData.ResolveInfoKey()` 최장 전방일치 정규화로 해결
- 검증: 해당 용어 포함 카드(G0, G25, G44, H28, H57, H59, E50 등) 설명에서 툴팁 팝업 확인
- 에디터 검증 완료: CardDB 398행 전체 @토큰 미해결 0건

### 16. BuffDB 설명 미작성 5건
- [x] `GOHANG2_DEBUFF`, `GOHANG3_DEBUFF`, `GOHANG3` 설명 작성 (코드 실제 동작 기준)
- [ ] `ERIS_2ND`, `ERIS_3RD` — #7 기획 확정 결과에 따라 작성 or 삭제 (보류)

---

## 🟢 P4 — 코드 품질·죽은 코드 정리

### 17. `BinaryFormatter` 세이브 교체
- [ ] `M_SaveManager.cs` — deprecated `BinaryFormatter` → JSON(JsonUtility/Newtonsoft) 등으로 교체
- 참고: 세이브 포맷이 바뀌므로 기존 `save.dat` 호환은 포기해도 무방 (개발용 스냅샷)

### 18. 로비 보안·선택 검증
- [ ] `M_SteamManager.cs:134-135` — 로비 비밀번호 실제 검증 추가 (현재 자물쇠 표시만)
- [ ] `CharacterSelectUI.cs:29-37` — 캐릭터 중복 선택 방지 (⚠ 기획 확정: 중복 허용이 의도일 수도 있음)

### 19. 죽은 코드·누수 정리
- [ ] 에리스에게도 HA 어빌리티 카드 스폰됨 (`PlayerInterfaceServer.cs:41-63` 근처) — 스폰 제외
- [ ] 홍단향 어빌리티 카드 `destroyCardList` 무한 누적 — 정리 경로 추가
- [ ] `CardType.WOUND` / `BattleTurn.PLAYER_ORDERSELECT` 데드 enum 제거 (⚠ SyncVar 직렬화·CSV 파싱 영향 확인 후)
- [ ] `CARD_AUDIT.md` G61 항목 낡음 — 문서 갱신

### 20. 잊혀진덱 회수 경로 검증
- [ ] `GAME_DESIGN.md` §5-4 — 잊혀진덱 `Clear()`만 되고 회수 없음 → 원본이 총괄 덱에 남는지 실측 검증
- 검증: 찰나 카드 사용 후 전투 종료 → 총괄 덱에 해당 카드 존재 확인 (문제 없으면 문서에 확인 완료 표기)

---

## 진행 규칙

1. **회차 단위**: P1부터 순서대로, 한 회차에 관련 항목 1~3개씩 묶어 처리
2. **테스트**: ParrelSync 클론으로 2~3인 멀티 상황 재현 (특히 #1, #3, #9는 멀티 필수)
3. **커밋**: 한글 커밋 메시지, 항목 번호 명시 (예: `TODOLIST #1 게임오버 에리스 제외 수정`)
4. **⚠ 기획 확정 표시 항목**(#7, #8, #9, #10, #13, #18)은 결정 먼저 → 코드 수정은 그 다음 회차

---

## 진행 로그

> 회차 완료 시마다 갱신: 항목 / 실제 수정 지점 / 테스트·커밋 상태

### 회차 1 — 2026-07-29 · P1 #1~#5 ✅ 테스트 완료
- **#1** `GamePlayer.cs` — `!IsEris` → `CanErisRevive`(광기 변신 가능 상태만 예외, MAD 사망·오브젝트 소멸은 전멸 집계 포함)
- **#2** `RewardService.cs` — 하드코딩 리셋 → `SetInitialIchi()` 재사용. 기획 확정: H5/H6 최대 이치 증가는 해당 전투 한정
- **#3** `AbilityCtrlArrow.cs` `CmdEnQueueCardData` — null/소유권/`PLAYER_ACTIVE` 페이즈 서버 검증 추가
- **#4** `TargetObject.cs` `sizeOfIronDemon` — IRONDEMON 버프 부재 시 getter 0 반환·setter 무시
- **#5** `GamePlayerItem.cs` — `itemEffects` 조회 키 `itemName` → `itemNumber`(+`TryGetValue`, 실패 로그)
- 부수 갱신: `GAME_DESIGN.md` 이치 표·홍단향 카드 항목에 전투 한정 기획 확정 반영

### 회차 2 — 2026-07-29 · P2 #6, #11, #12 🔄 테스트 대기
- **#6** `GamePlayerDeck_IchiPart.cs` — `IncreaseMaxIchi()` 신설(ICHI_LIMIT 클램프), `CardData_DanHyang.cs` H5/H6가 사용
- **#11** `MonsterDB.csv` — Saddy frequency 50/50/50 → 34/33/33 (기존엔 세 번째 패턴 '방어'가 선택 불가였음)
- **#12** `MonsterData.cs` 상시 버프 행 파싱 재구현 + `BattleSpawner.cs` 스폰 시 적용 + `BuffIndicatorController.cs` 아이콘 미등록 방어
  - ⚠ 잔여: SUHOJA 아이콘/효과/BuffDB 미비, Saddy `E2` 버프 정체 불명 (항목 #12 잔여란 참고)
- 컴파일 검증 완료 (error CS 0건)
- **플레이 테스트 체크리스트**:
  - [ ] 홍단향 H5를 한 전투에서 반복 사용 → 최대 이치 6에서 고정
  - [ ] Twins(Happy+Saddy) 전투 → Saddy가 두번공격/힘버프/방어 세 가지 모두 사용
  - [ ] Guardian 전투 진입 → 버프 슬롯 1개 등록 (아이콘 빈 상태 + `SUHOJA 아이콘 미등록` 에러 로그 1건 = 정상)
  - [ ] 게임 로드 시 `MonsterDB 로드 실패 (31행)` 에러 1건 = Saddy E2 행 의도된 스킵 (정상)

### 회차 3 — 2026-07-29 · P3 #14, #15, #16(부분) 🔄 테스트 대기
- **#14** CSV 오타 일괄 수정 + `CsvTable` 필드 Trim 구조 개선, `ItemData.cs` `Garde`→`Grade`
  - 추가 발견: `GOOWON ` 14건(H3/H4 계열 로드 실패 중이었음), `WHOLE ` 5건 — 함께 수정
- **#15** `Description.csv` 9종 추가 + `CardData.ResolveInfoKey()` 조사 토큰 최장 전방일치
- **#16** GOHANG 3종 설명 작성 (ERIS_2ND/3RD는 #7 대기)
- 검증: 컴파일 0건 + 에디터 script-execute로 전 DB 파싱 검증 — CardDB 398행 enum 실패 0 / @토큰 미해결 0 / MonsterDB 실패 1(의도된 E2행) / Artifact·Legacy 0
- **플레이 테스트 체크리스트**:
  - [ ] 에리스 카드풀에 축복 카드(E20 템페스토소, E29, E33, E50, E59) 등장
  - [ ] 홍단향 H3/H4(구원 특성) 카드 정상 로드·구원 툴팁 표시
  - [ ] 별무리 카드 마우스오버 시 특성 툴팁 표시
  - [ ] G0(고행길)·H28(크기)·E50(파괴의권능)·G44(압도를) 마우스오버 시 용어 툴팁 팝업
  - [ ] 고행 II/III 드로우 시 버프 아이콘 마우스오버 → 새 설명문 표시

---

## (참고) 본 목록 제외 — §14-A 컨텐츠 공백

버그가 아닌 컨텐츠 작업이므로 별도 트랙으로 관리: 게임 클리어/엔딩, 스테이지 2·3 진행(DB 등록), 난이도 선택, 아이템/유산 시스템 본구현, 메타 프로그레션, 이벤트 방 실효과, 보상 테이블, 거점 등급 효과, 엘리트 전용 테이블, 시작 덱 구성 복원.
