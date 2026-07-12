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

        // ------------------------------------------------------------ 클라이언트 → 서버 투표 --------------------------------------------------------------- //

        [Command(requiresAuthority = false)]
        public void CmdVote(uint playerNetId, int tileIndex)
        {
            // 서버의 맵 상태로 유효성 검증 (오각형/미탐험/도달불가 거부)
            if (system == null || !system.IsValidDestination(tileIndex))
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
            int moveCost;
            if (chosen == system.currentTileIndex)
            {
                // 보스가 현재 방까지 도달한 경우의 제자리 보스전 (2D의 보스방 재진입 대응)
                if (system.GetRoomTypeOf(chosen) != RoomType.BOSS)
                    return false;
                moveCost = 0;
            }
            else if (bossExists)
            {
                // 보스 출현 시 1칸 이동, 행동 비용 무시 (2D와 동일)
                if (!system.IsValidDestination(chosen))
                    return false;
                moveCost = 0;
            }
            else
            {
                List<int> path = system.FindPath(system.currentTileIndex, chosen);
                if (path.Count == 0)
                    return false;
                // 행동 비용 확인 (2D EnterTheRoom과 동일)
                if (path.Count > M_MapManager.instance.currentActionCost)
                {
                    Debug.Log($"[3D맵] 행동 비용이 모자랍니다. 필요 : {path.Count} / 남은 비용 : {M_MapManager.instance.currentActionCost}");
                    return false;
                }
                moveCost = path.Count;
            }

            RoomType destType = system.GetRoomTypeOf(chosen);
            int destHazard = system.GetHazardOf(chosen);

            // 전투 진입 여부: 미방문 방이면 전투/이벤트가 시작되므로,
            // 이동/시야 확장 반영은 전투 클리어 후 맵 복귀 시점으로 보류 (딤 처리와 자연스럽게 이어지도록)
            bool entersBattle = !(destType == RoomType.COMPLETE || destType == RoomType.START_LOCATION);
            RpcMoveParty(chosen, !entersBattle); // 모든 클라이언트(호스트 포함)에 이동 전달
            votes.Clear();

            // 다음 맵 선택을 위해 레디 상태 리셋
            foreach (PlayerInterface player in PlayerRegistry.All)
                player.isReady = false;

            M_MapManager.instance.DecreaseTotalActionCost(moveCost); // 이동 거리만큼 행동 비용 감소 (0이 되면 보스 출현)

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
