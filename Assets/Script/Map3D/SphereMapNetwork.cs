using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace ProjectD
{
    /// <summary>
    /// 3D 구체 맵의 네트워크 동기화 레이어 (씬 배치 네트워크 오브젝트).
    /// - 서버가 맵 시드를 SyncVar로 뿌려 모든 클라이언트가 동일한 맵을 생성 (SphereMapSystem은 시드 기반 결정적)
    /// - 타일 클릭 = 투표(SyncDictionary), 규칙은 2D의 GetVoteHexagonMapRoomResult와 동일 (중복 선택 우선, 아니면 랜덤)
    /// - 모든 플레이어 레디 → M_TurnManager.CheckAllPlayersReadyForMapMove가 TryMoveByVotes 호출
    /// - 이동은 RPC로 전 클라이언트에 적용하고, 전투 진입은 프록시 HexagonMapRoom에 방 타입/위험도를 실어
    ///   기존 파이프라인(M_MapManager.StartBattle → 몬스터 스폰/전투 전환/보상/맵 복귀)을 그대로 재사용한다.
    /// </summary>
    public class SphereMapNetwork : NetworkBehaviour
    {
        public static SphereMapNetwork instance;

        [Tooltip("3D 구체 맵 로직 (SphereMapView3D의 SphereMapSystem)")]
        public SphereMapSystem system;

        [SyncVar(hook = nameof(OnChangedSeed))]
        public int mapSeed;

        [SyncVar(hook = nameof(OnChangedBossTile))]
        public int bossTileIndex = -1; // 보스가 위치한 타일 (-1 = 미출현)

        // PlayerInterface netId → 투표한 타일 인덱스
        public readonly SyncDictionary<uint, int> votes = new SyncDictionary<uint, int>();

        // ---------------- 워프 (전초기지 → 전초기지 유료 이동) ----------------
        public const int WarpRange = 5;        // 워프 후보 탐색 범위 (칸)
        public const int WarpGoldPerTile = 10; // 1칸당 워프 비용 (골드)

        // 전초기지 입장 시 서버가 채우는 워프 목적지 후보 (5칸 이내의 미방문 전초기지 타일)
        public readonly SyncList<int> warpCampTiles = new SyncList<int>();

        // 워프 후보로 한 번이라도 밝혀진 타일 누적 목록 — 전초기지를 벗어나 워프 기회가 사라져도
        // 이미 알려진 위치이므로 맵 표시는 유지한다 (선택 가능 여부와 무관, 표시 전용)
        public readonly SyncList<int> revealedTiles = new SyncList<int>();

        // 워프 목적지 선택 모드 — 워프 버튼을 누르면 켜지고, 켜진 동안에는 워프 후보 전초기지만 선택할 수 있다.
        // 이동 확정 또는 골드 부족 실패 시 꺼진다 (골드 부족 시에도 꺼야 일반 이동으로 빠져나갈 수 있다)
        [SyncVar(hook = nameof(OnChangedWarpMode))]
        public bool warpMode;

        HexagonMapRoom _proxyRoom; // 전투 진입 파이프라인 재사용용 프록시 방 (화면 밖, 비활성 비주얼)

        void Awake()
        {
            instance = this;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            mapSeed = Random.Range(1, int.MaxValue);
            if (system != null)
                system.SetNetworkSeed(mapSeed);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            votes.Callback += OnVotesChanged;
            warpCampTiles.Callback += OnWarpCampTilesChanged;
            revealedTiles.Callback += OnWarpCampTilesChanged; // 표시 갱신 동작이 동일하므로 콜백 공유
            if (mapSeed != 0 && system != null)
                system.SetNetworkSeed(mapSeed);
        }

        void OnChangedSeed(int oldVal, int newVal)
        {
            if (system != null)
                system.SetNetworkSeed(newVal);
        }

        void OnChangedBossTile(int oldVal, int newVal)
        {
            if (system != null)
                system.SetBossTile(oldVal, newVal); // 이전 존 → 폐허, 새 존 → 보스방
        }

        void OnVotesChanged(SyncDictionary<uint, int>.Operation op, uint key, int value)
        {
            if (system != null)
                system.RefreshAllVisuals(); // 투표 표시 갱신
        }

        void OnWarpCampTilesChanged(SyncList<int>.Operation op, int index, int oldVal, int newVal)
        {
            if (system != null)
                system.RefreshAllVisuals(); // 워프 후보 표시 갱신
        }

        void OnChangedWarpMode(bool oldVal, bool newVal)
        {
            if (system != null)
                system.RefreshAllVisuals();
            if (MapUI.instance != null)
                MapUI.instance.SetWarpPromptActive(newVal); // 상단 중앙 "워프할 전초기지를 선택하세요" 배너
        }

        /// <summary>워프 목적지 선택 모드 진입/해제 (전초기지 워프 버튼에서 호출)</summary>
        [Command(requiresAuthority = false)]
        public void CmdSetWarpMode(bool active)
        {
            if (active && warpCampTiles.Count == 0)
                return; // 후보 없이 워프 모드 진입 불가
            SetWarpModeServer(active);
        }

        [Server]
        void SetWarpModeServer(bool active)
        {
            if (warpMode == active)
                return;
            warpMode = active;
            OnChangedWarpMode(!active, active); // 호스트에서는 SyncVar 훅이 자동 호출되지 않으므로 직접 반영
        }

        /// <summary>해당 타일에 투표한 플레이어가 있는지 (투표 표시용)</summary>
        public bool IsTileVoted(int tileIndex)
        {
            foreach (int voted in votes.Values)
            {
                if (voted == tileIndex)
                    return true;
            }
            return false;
        }

        /// <summary>워프 목적지 후보 타일인지</summary>
        public bool IsWarpCandidate(int tileIndex)
        {
            return warpCampTiles.Contains(tileIndex);
        }

        /// <summary>워프 후보로 한 번이라도 밝혀진 타일인지 (표시 전용 — 선택 가능 여부와 무관)</summary>
        public bool IsRevealedTile(int tileIndex)
        {
            return revealedTiles.Contains(tileIndex);
        }

        /// <summary>
        /// 전초기지 입장 시 워프 후보 갱신 (서버 — TryMoveByVotes에서 전초기지 도착이 확정될 때 호출).
        /// 워프로 도착한 경우에도 새 전초기지 기준으로 다시 계산되므로 연쇄 워프가 가능하다.
        /// 입장한 전초기지 기준 WarpRange칸 이내의 미방문 전초기지가 후보가 되며 SyncList로 전 클라이언트에 표시된다.
        /// </summary>
        [Server]
        void SetupWarpCandidates(int originTile)
        {
            if (system == null || !system.HasState)
                return;
            warpCampTiles.Clear();
            foreach (int tileIndex in system.FindTilesInRange(originTile, WarpRange, RoomType.CAMP))
            {
                warpCampTiles.Add(tileIndex);
                if (!revealedTiles.Contains(tileIndex))
                    revealedTiles.Add(tileIndex); // 한 번 밝혀진 전초기지는 워프 기회가 끝나도 계속 표시
            }
        }

        [Server]
        void ClearWarpCandidates()
        {
            if (warpCampTiles.Count > 0)
                warpCampTiles.Clear();
        }

        // ------------------------------------------------------------ 클라이언트 → 서버 투표 --------------------------------------------------------------- //

        [Command(requiresAuthority = false)]
        public void CmdVote(uint playerNetId, int tileIndex)
        {
            // 서버의 맵 상태로 유효성 검증 (오각형/미탐험/도달불가 거부).
            // 워프 후보는 워프 모드 중에만 허용 — 평상시에는 클리어한 타일로 이어진 경로가 있어야만 투표할 수 있다
            if (system == null || (!system.IsValidDestination(tileIndex) && !(warpMode && IsWarpCandidate(tileIndex))))
                return;
            // 워프 모드 중에는 워프 후보 전초기지만 투표 가능
            if (warpMode && !IsWarpCandidate(tileIndex))
                return;
            if (votes.ContainsKey(playerNetId))
                votes[playerNetId] = tileIndex;
            else
                votes.Add(playerNetId, tileIndex);
        }

        [Command(requiresAuthority = false)]
        public void CmdCancelVote(uint playerNetId)
        {
            if (votes.ContainsKey(playerNetId))
                votes.Remove(playerNetId);
        }

        // ------------------------------------------------------------ 서버: 전원 레디 시 이동 --------------------------------------------------------------- //

        /// <summary>
        /// 투표 결과 방으로 파티 이동 + 전투 진입. (2D의 GetVoteHexagonMapRoomResult + EnterTheRoom 대응)
        /// M_TurnManager.CheckAllPlayersReadyForMapMove에서 모든 플레이어가 레디일 때 호출된다.
        /// </summary>
        [Server]
        static void ResetPlayersReady()
        {
            foreach (PlayerInterface player in PlayerRegistry.All)
                player.isReady = false;
        }

        [Server]
        public bool TryMoveByVotes()
        {
            if (system == null || !system.HasState || votes.Count == 0)
                return false;

            // 중복(과반) 선택 우선, 없으면 랜덤 — 2D와 동일 규칙
            int chosen = -1;
            var seen = new List<int>();
            foreach (int tile in votes.Values)
            {
                if (seen.Contains(tile))
                {
                    chosen = tile;
                    break;
                }
                seen.Add(tile);
            }
            if (chosen < 0)
                chosen = seen[Random.Range(0, seen.Count)];

            bool bossExists = M_MapManager.instance.mapBoss != null;
            bool isBattleInPlace = chosen == system.currentTileIndex; // 이동이 아니므로 이동분 턴은 소모하지 않는다
            // 워프 모드 중에 선택된, 일반 경로로 도달 불가한 워프 후보 = 워프 이동 (골드 차감).
            // 탐험된 후보라면 일반 이동으로 처리해 비용을 물리지 않고, 워프 모드가 아니면 미연결 타일 이동은 아래 일반 검증에서 거부된다
            bool isWarp = !isBattleInPlace && warpMode && IsWarpCandidate(chosen) && !system.IsValidDestination(chosen);
            if (isBattleInPlace)
            {
                // 보스가 현재 방까지 도달한 경우의 제자리 보스전 (2D의 보스방 재진입 대응)
                if (system.GetRoomTypeOf(chosen) != RoomType.BOSS)
                    return false;
            }
            else if (isWarp)
            {
                // 워프 비용: 홉 거리 1칸당 WarpGoldPerTile 골드 — 전 플레이어가 각자 지불 (한 명이라도 부족하면 실패)
                int distance = system.GetHexDistance(system.currentTileIndex, chosen);
                if (distance <= 0 || !TryChargeAllPlayersForWarp(distance * WarpGoldPerTile))
                {
                    votes.Clear();
                    ResetPlayersReady(); // 다시 목적지를 고를 수 있도록 레디/투표 리셋
                    SetWarpModeServer(false); // 워프 모드 해제 — 일반 이동으로 빠져나갈 수 있게 (소프트락 방지)
                    RpcNotifyWarpFailed();
                    return false;
                }
            }
            else if (bossExists)
            {
                // 보스 출현 시 1칸 이동만 허용 (2D와 동일)
                if (!system.IsValidDestination(chosen))
                    return false;
            }
            else if (system.FindPath(system.currentTileIndex, chosen).Count == 0)
            {
                return false; // 도달 불가
            }

            RoomType destType = system.GetRoomTypeOf(chosen);
            int destHazard = system.GetHazardOf(chosen);

            // 전투 진입 여부: 미방문 방이면 전투/이벤트가 시작되므로,
            // 이동/시야 확장 반영은 전투 클리어 후 맵 복귀 시점으로 보류 (딤 처리와 자연스럽게 이어지도록)
            bool entersBattle = !(destType == RoomType.COMPLETE || destType == RoomType.START_LOCATION);
            RpcMoveParty(chosen, !entersBattle); // 모든 클라이언트(호스트 포함)에 이동 전달
            votes.Clear();
            // 전초기지 도착(워프 도착 포함)이면 새 위치 기준으로 워프 후보 재계산, 그 외 이동이면 워프 기회 종료.
            // 호스트 클라이언트의 이동 RPC 처리 시점에 의존하지 않도록 서버가 확정한 목적지(chosen)를 직접 기준으로 삼는다.
            if (destType == RoomType.CAMP)
                SetupWarpCandidates(chosen);
            else
                ClearWarpCandidates();
            SetWarpModeServer(false); // 이동 확정 → 워프 모드 종료 (도착한 전초기지에서 다시 워프 가능)

            ResetPlayersReady(); // 다음 맵 선택을 위해 레디 상태 리셋

            // 턴 소모: 이동 자체에 1턴. 거리와 무관하게 몇 칸을 가든 이동은 1턴이다.
            // 미클리어 방으로 이동하면 방 정리 시점(M_TurnManager.NoneBattleEnd)에 1턴이 더 빠져 합계 2턴,
            // 이미 클리어한 방으로 이동하면 NoneBattleEnd를 거치지 않으므로 1턴만 소모된다.
            if (!isBattleInPlace)
                M_MapManager.instance.DecreaseTotalActionCost(1);

            // 보스 접근 (2D ApproachBossToPlayer 대응): 파티가 이동할 때마다 보스가 2칸씩 접근
            if (bossTileIndex >= 0 && bossTileIndex != chosen)
                bossTileIndex = system.GetBossApproachTile(bossTileIndex, chosen, 2);

            // 기존 전투 진입 파이프라인 재사용: 목적지 정보를 프록시 방에 실어 StartBattle 호출
            // (COMPLETE/START 방이면 StartBattle이 알아서 이동만 수행)
            EnsureProxyRoom();
            _proxyRoom.roomType = destType;
            _proxyRoom.hazard = destHazard;
            M_MapManager.instance.currentRoom = _proxyRoom;
            M_MapManager.instance.StartBattle(_proxyRoom);
            return true;
        }

        [ClientRpc]
        void RpcMoveParty(int tileIndex, bool applyImmediately)
        {
            if (system != null)
                system.MovePartyTo(tileIndex, applyImmediately);
        }

        // ------------------------------------------------------------ 워프 과금 --------------------------------------------------------------- //

        // 모든 플레이어(캐릭터)가 각자 cost 골드를 지불. 한 명이라도 부족하면 아무도 차감하지 않고 실패
        [Server]
        bool TryChargeAllPlayersForWarp(int cost)
        {
            var players = new List<GamePlayer>();
            foreach (PlayerInterface playerInterface in PlayerRegistry.All)
            {
                foreach (GamePlayer gamePlayer in playerInterface.ownedPlayers)
                    players.Add(gamePlayer);
            }
            if (players.Count == 0)
                return false;
            foreach (GamePlayer gamePlayer in players)
            {
                if (gamePlayer.gold < cost)
                    return false;
            }
            foreach (GamePlayer gamePlayer in players)
                gamePlayer.gold -= cost;
            return true;
        }

        [ClientRpc]
        void RpcNotifyWarpFailed()
        {
            M_MessageManager.instance
                .MakeToast()
                .Position(ToastPosition.Bottom)
                .MessageBoxColor(Color.red)
                .TextColor(Color.white)
                .Text(M_LanguageManager.Get("ui.msg.warp_no_gold", "골드가 부족하여 워프할 수 없습니다. 1칸당 10골드가 필요합니다."))
                .FadeInTime(1.5f)
                .FadeOutTime(1.5f)
                .Show();
        }

        // ------------------------------------------------------------ 보스 --------------------------------------------------------------- //

        /// <summary>
        /// 3D 맵 위에 보스 출현 (행동 비용 0 도달 시 M_MapManager.DecreaseTotalActionCost에서 호출).
        /// MapBoss 네트워크 오브젝트는 기존 시스템 연동(토스트/BGM/이동 제한/보스전 분기)을 위해
        /// 화면 밖에 스폰하고, 보스의 실제 위치는 bossTileIndex(SyncVar)로 구체 타일에 표시한다.
        /// </summary>
        [Server]
        public void SpawnBoss()
        {
            if (system == null || !system.HasState || M_MapManager.instance.mapBoss != null)
                return;

            // 기존 MapBoss 오브젝트 스폰 (화면 밖) → mapBoss SyncVar 훅으로 출현 토스트/BGM 등 기존 연출 재사용
            var networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
            GameObject mapBossObject = Instantiate(
                networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "MapBoss"),
                new Vector3(2000f, 2100f, 0f),
                Quaternion.identity);
            MapBoss mapBoss = mapBossObject.GetComponent<MapBoss>();
            mapBoss.coordinate = new Vector2Int(9999, 9998); // 2D 좌표계와 겹치지 않는 값
            NetworkServer.Spawn(mapBossObject);
            M_MapManager.instance.mapBoss = mapBoss;

            // 보스 출현 위치: 파티에서 가장 먼 육각형 (SyncVar 훅으로 전 클라이언트 보스 존 반영)
            bossTileIndex = system.GetFarthestHexagonFrom(system.currentTileIndex);
        }

        [Server]
        void EnsureProxyRoom()
        {
            if (_proxyRoom != null)
                return;
            var networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
            GameObject proxyObject = Instantiate(
                networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "HexagonMapRoom"),
                new Vector3(2000f, 2000f, 0f), // 화면 밖
                Quaternion.identity);
            _proxyRoom = proxyObject.GetComponent<HexagonMapRoom>();
            _proxyRoom.coordinate = new Vector2Int(9999, 9999); // 2D 좌표계와 겹치지 않는 값
            _proxyRoom.position = proxyObject.transform.position;
            _proxyRoom.isActive = false; // 타일 비주얼 숨김
            NetworkServer.Spawn(proxyObject);
        }
    }
}
