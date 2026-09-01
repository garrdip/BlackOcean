## 메뉴 변경
1. 싱글 플레이어 
    - 처음부터 시작
    - 이어서 하기
2. 멀티 플레이어
    - 처음부터 시작
    - 이어서 하기
- 구현 (2026-08-27): 싱글 플레이 클릭 시 메뉴 전체가 "처음부터 시작 / 이어하기"로 전환(MenuUI — 기존 버튼 2개 라벨 교체·나머지 숨김, Esc로 복귀). 처음부터 = 기존 싱글 시작 루틴(호스트 시작), 이어하기 = GameSaveService.pendingLoad 예약 후 동일 루틴(저장 파일 없으면 비활성). 이어하기 시 룸 씬에서 저장 프로필(SteamID 매칭)의 캐릭터가 자동 선택되고 싱글은 룸 대기 없이 바로 게임 씬으로 진입, 게임 씬에서 프로필(레벨/스탯/스킬/장비/골드/HP)과 진행도(해금 스테이지/위험도)를 복원. 멀티의 처음부터/이어서는 방 만들기 화면 임시 토글(CreateLobby) 유지 — 정식 개편은 후속.
- 싱글 고정 파티 (2026-09-01): 싱글 플레이는 캐릭터 선택 없이 3인 전원(전열 게오르크 / 중열 에리스 / 후열 홍단향 — `M_NetworkRoomManager.SinglePlayParty`)으로 시작. 처음부터/이어하기 모두 룸 대기 없이 자동 진입하고, 호스트의 PlayerInterface가 3명의 GamePlayer를 소유(`ownedPlayers`) — 탭(TabLayout)으로 제어 캐릭터 전환, 이어하기는 SteamID+캐릭터로 프로필을 각각 복원. 골드는 GamePlayer별 보유(류진솔 회복 등은 현재 선택 캐릭터가 지불).

## 멀티플레이어 변경점
- 획득 경험치량, 아이템 드랍 1.5배.
- 싱글플레이 세이브 파일에서, 3개의 캐릭터 중 한개 또는 두개의 캐릭터를 들고 올 수 있음.

## 거점
- 류진솔 ( 출정 관리 / 회복 — 골드 20(`BalanceDB HUB_HEAL_COST`)을 내면 파티 전원 HP 전량 회복. 구 '골드 전달' 메뉴 대체 2026-08-31 )
- 소피아 ( 위험도 제어 돈으로 위험도를 낮출 수 있음, 저장 )
- 메르크리우스 상단 ( 장비 제작 )
- 그림자꾼 ( 스킬 초기화 )

## 맵 
- Darkest Dungeon 방식 ( 거점에서 할일하고, 스테이지 진입 )
- 스테이지 1-1, 1-2, 1-3 (엘리트1) 1-4, 1-5, 1-6 ( 보스1 )
- 스테이지 2-1, 2-2, 2-3 ()
- 스테이지 3-1, 3-2, 3-3 (보스3, Early Access)

## 스테이지 구성
- Darkest Dungeon 방식의 방 구성 
- 스테이지에 입장하면, 첫 방은 항상 빈방.
- 방은 1-1 기준 총 5개의 방으로 구성. 1-1 클리어시 1-2 이 열리는 방식
- 방의 개수는 앞의숫자 + 뒤의 숫자 + 3 개의 방으로 구성됨. 
- 방종류 : 전투, 엘리트, 빈방(휴식), 보물방 
- 방을 클리어 한뒤, 이동할 방을 선택. ( 갈수 있는 방은 미리 알 수 있음 )
- 탈출구를 찾으면 클리어.
- 구현 (2026-08-27): 스테이지별 등장 몬스터 그룹은 StageDB.csv의 MonsterGroups(몬스터 방)/EliteGroups(엘리트 방) 리스트('|' 구분, MonsterGroupDB 그룹 이름)에서 랜덤 선택.
  Type: MONSTER(일반) / ELITE(일반과 동일하되 엘리트 방 딱 1개 — 1-3, 2-3) / BOSS(가장 먼 방이 보스 — 3-3). MonsterGroupDB의 위험도 범위 컬럼은 폐기.

## 위험도 시스템
- 위험도가 올라가면, 몬스터가 강해지고, 보상이 늘어남. 특정 보상의 경우 위험도 하한선이 존재 함. (파고들기 요소)
- 일반 스테이지 클리어시 +1, 엘리트 몹 처치시 +3, 보스 처치시 +5
- 소피아에게 돈을 주고 위험도를 낮출 수 있음.
- 구현 (2026-08-26):
    - 위험도는 전역 위험도(M_HubManager.hazardLevel, SyncVar, 세이브 저장/복원, 상한 없음) 하나뿐 — 스테이지 자체에 위험도는 없다.
    - 전역 위험도 상승: 일반(비보스) 스테이지 클리어 +1(M_HubManager.OnStageCleared), 엘리트 처치 +3 / 보스 처치 +5(M_TurnManager.ProcessMonsterDeathCoroutine). 수치는 BalanceDB(HAZARD_RISE_*).
    - 하향: 소피아 "기도" 버튼(구 골드 전달) → CampPopUp 기도 레이아웃(화면 중앙 확인 팝업 — 현재 위험도/비용/보유 골드 표시, 골드 부족 시 수락 비활성) → 수락 시 M_HubManager.CmdReduceHazard로 1 하향. 비용 = HAZARD_REDUCE_COST_BASE + 현재 위험도 x HAZARD_REDUCE_COST_PER_LEVEL. 로컬라이즈 키 ui.pray.* (전 로케일 등록).
    - 위험도 표시: 거점 우상단 Hub/HubCanvas/HazardLayout — 옛 육각형 맵 시절의 MapDangerLayout UI(전투 캔버스 잔존분 복제 + MapDangerIcon 복원)를 재사용, HubHazardUI가 hazardLevel을 표시.
    - 몬스터 강화: **각 몬스터별 위험도 보너스 스탯** — MonsterStatDB.csv의 HazardAtk/HazardDef/HazardHp 컬럼(소수 허용). 유효 위험도 1당 공격 피해/방어 획득량/최대체력이 해당 수치만큼 플랫 증가 (적용 시 반올림, 컬럼 없거나 0이면 영향 없음).
    - 적용 지점: 스폰 시 최대체력(BattleSpawner), 공격 피해·인디케이터 표시(SpawnedMonster.ScaledAttack, hazard SyncVar), 방어 획득량(TargetObject.GainDefense → ScaledDefense). 등장 몬스터 그룹은 위험도와 무관하게 StageDB 리스트에서 선택.
    - 보상: 전투 경험치/골드에 위험도 1당 +5% 배율(BalanceDB HAZARD_REWARD_PERCENT_PER_LEVEL, RewardService). 장비 드랍은 위험도 하한선(BalanceDB EQUIP_DROP_MIN_HAZARD) 미만이면 드랍되지 않음 — 위험도 하한선 보상의 첫 사례.

## 스텟
- 힘 ( 물리 공격력 )
- 민첩 
- 최대 HP ( 구 '체력' 스탯 폐기 2026-08-31 — 체력은 최대 HP 외 용도가 없어 HP가 직접 성장/스킬트리 대상. `CharacterStatDB` BaseHP = 1레벨 최대 HP, GrowHP = 만렙까지 총 성장치 )
- 지능 ( 마법 공격력 )
- 제어 ( MP 회복량 , 분노 생성량에 영향을 주는 스탯 )
- 방어력
- 마법방어

## 레벨
- 게오르크 : 힘,체력, 방어력 위주 성장
- 홍단향 : 지능, 마법방어력 위주 성장
- 에리스 : 힘, 민첩 위주 성장.
- 성장치 랜덤 분배 (2026-08-31): `CharacterStatDB.csv`의 `GrowX`는 **1레벨 → 최대 레벨(99)까지의 총 성장치**. 캐릭터 생성 시 부여되는 `growthSeed`(세이브 보존)로 이 총량을 98회의 레벨업에 랜덤 분배한다 (`DB/LevelGrowthTable.cs`).
  레벨업마다 오르는 양은 시드마다 다르지만 만렙에서의 합은 항상 `GrowX`로 같다. 편차 폭은 `BalanceDB LEVEL_GROWTH_VARIANCE_PERCENT`(50 → 레벨당 평균의 0.5~1.5배 가중치).

## 자원
- 게오르크 : 분노 ( 데미지를 주거나(방어력 포함된 최종수치에 영향), 데미지를 입을때 (방어력을 제외한 데미지에 영향) )
- 홍단향 : MP 
- 에리스 : HP 

## 무기
- 게오르크 : 검
- 홍단향 : 지팡이
- 에리스 : 크리스털 코어

## 방어구
- 갑옷 
- 투구
- 신발
- 악세사리1
- 악세사리2

## 장비 옵션
- 공격력, 스킬레벨, 방어력, 최대HP, 최대MP 

## 아이템
- HP물약, MP물약
- 각종 버프 물약

## 스킬 (RPG_CONVERSION_SKILLS.md 참조)
- 스킬트리 ( 드퀘식 )
- 게오르크 
- 홍단향 
- 에리스 

## 기본 스킬 (턴소모 하지 않음)
- 게오르크 : 고행길 ( 자신에게 디버프 부여, 분노 상승 )
- 홍단향 : 철귀 이동 ( 자신의 턴 종료시, 공격 및 방어력 제공(방어력 버프개념) )
- 에리스 : 자해 ( 자신의 HP 소모 )

## 에리스 변신 매커니즘
- 체력 40% 이하 : 1차 변신 - 공격력 20% 증가
- 체력 10% 이하 : 광기 변신 - 공격력 50% 증가
- 구현 (2026-09-01): 상태는 HP 비율로만 정해진다 — `TargetObject.UpdateErisMode`(서버)가 HP가 바뀌는 모든 경로(`SetPlayerHP` — 피해/회복/스킬 코스트)와 전투 시작 시 재판정. `ErisMode` NORMAL / ANGER(≤`ERIS_ANGER_HP_PERCENT` 40%) / MAD(≤`ERIS_MAD_HP_PERCENT` 10%), 회복으로 비율이 오르면 되돌아간다.
  가하는 피해 배율은 `BattleActions.ScaleByErisMode`(기본 공격·스킬 공통, `ERIS_ANGER_ATTACK_PERCENT` 20 / `ERIS_MAD_ATTACK_PERCENT` 50 — BalanceDB). 연출(전이별 모션 1회, 기획 확정): 기본→1차 `Change0` / 1차→광기 `Change1` / 기본→광기 `Change2` / 광기→기본 `RChange2` / 광기→1차 `RChange1` / 1차→기본 `RChange0` (`TargetObject.GetErisTransitionMotion`). 완료 시 해당 상태 Idle(Idle/ChIdle/VIdle) 전환은 `TargetObject.OnAnimationComplete`가 상태 프리픽스로 처리. 광기 해제 시 트랙 1의 광기 추가 모션(VAni*)은 `OnChangedErisMode`가 비운다.
  치사 피해 시 HP 1 생존(꿈을 본 인형)은 **한 전투에 한 번**(`TargetObject.erisReviveUsed`)으로 정리 — 종전 "광기가 아니면 무제한 생존" 규칙은 회복으로 광기가 풀리는 새 구조에서 무한 생존이 되므로 폐기. 전멸 판정(`GamePlayer.CanErisRevive`)도 같은 플래그를 본다.

## 전투
- 턴제 ( 민첩기반, 순서만 정하는게 아닌, 여러번 턴이 돌아올 수 있음 )
    TP : 민첩이 높을수록 차오르는 속도가 빨라지며, 100이되면 자신의 턴이 됨.
- 포지션 
    1. 전열 : 공격력 증가, 방어력 하락.
    2. 중열 : 위치 변환 코스트 삭제.
    3. 후열 : 공격력 하락, 방어력 증가
- 필살기 : 스킽트리에서 찍어야 하며, 각 트리의 마지막 스킬로 하이리스크 하이리턴 스킬로 구성할 예정.
- 1턴당 1번의 액션을 할 수 있음 
    액션 : 공격, 방어, 스킬, 아이템, 이동(전열 중열 후열)