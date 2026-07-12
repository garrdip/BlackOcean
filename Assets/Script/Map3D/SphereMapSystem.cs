using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using Mirror;
using DG.Tweening;

namespace ProjectD
{
    /// <summary>
    /// 3D 구체 맵의 게임 로직 레이어.
    /// 기존 2D 맵 시스템과 같은 규칙을 이웃 그래프 기반으로 재현한다:
    /// - 방 타입 배정: M_MapManager.GetRoomType()과 동일한 확률 분포 (몬스터 40%, 나머지 각 10%)
    /// - 위험도(hazard): 시작 방 기준 BFS 홉 거리
    /// - 시야: 시작 방 + 이웃만 활성화, 이동할 때마다 주변 방 활성화
    /// - 경로 탐색: 방문 완료(COMPLETE)/시작 방만 통과 가능 (2D GetNeighbours 필터와 동일)
    /// - 오각형 12개: 이동 불가 지역
    ///
    /// 맵 상태(방 타입/시야/위험도/현재 위치)는 뷰(SphereMapView 타일 오브젝트)와 분리되어 있어
    /// 전투 중 뷰가 꺼졌다 켜져도 유지된다. 맵 생성은 시드 기반 결정적이라
    /// SphereMapNetwork가 서버 시드를 뿌리면 모든 클라이언트가 동일한 맵을 만든다.
    ///
    /// 이동 확정은 로컬 버튼이 아니라 기존 2D와 동일하게
    /// "타일 클릭 = 투표 → 모든 플레이어 레디 → 서버가 이동" 흐름을 따른다. (SphereMapNetwork 참고)
    /// </summary>
    [RequireComponent(typeof(SphereMapView))]
    public class SphereMapSystem : MonoBehaviour
    {
        [Header("아이콘")]
        [Tooltip("방 타입 아이콘 아틀라스(MapTileIcon). 비워두면 런타임에 M_MapManager의 mapTileIconAtlas 사용")]
        public SpriteAtlas iconAtlas;
        [Tooltip("아이콘 로컬 스케일")]
        [Min(0.01f)] public float iconScale = 1f;

        [Header("방 타입 색상")]
        public Color startColor = new Color(0.30f, 0.65f, 1.00f);
        public Color monsterColor = new Color(0.90f, 0.45f, 0.40f);
        public Color eliteColor = new Color(0.75f, 0.30f, 0.75f);
        public Color eventColor = new Color(0.45f, 0.85f, 0.50f);
        public Color campColor = new Color(0.95f, 0.70f, 0.35f);
        public Color itemShopColor = new Color(0.95f, 0.90f, 0.45f);
        public Color cardShopColor = new Color(0.45f, 0.85f, 0.90f);
        public Color completeColor = new Color(0.72f, 0.72f, 0.72f);
        [Tooltip("보스 존 색 (2D의 #E700FF 대응)")]
        public Color bossColor = new Color(0.91f, 0.00f, 1.00f);
        [Tooltip("폐허(보스가 지나간 방) 색")]
        public Color ruinsColor = new Color(0.60f, 0.25f, 0.90f);
        [Tooltip("아직 밝혀지지 않은 방(비활성) 색")]
        public Color inactiveColor = new Color(0.28f, 0.28f, 0.32f);
        [Tooltip("경로 하이라이트 색")]
        public Color pathColor = new Color(1.00f, 0.98f, 0.70f);
        [Tooltip("다른 플레이어가 투표한 방 표시 색 혼합 비율")]
        [Range(0f, 1f)] public float votedTint = 0.2f;

        [Header("보스")]
        [Tooltip("보스 존 반경 (2D의 bossZoneRange 대응)")]
        public int bossZoneRange = 2;
        [Tooltip("보스 비주얼(스프라이트/파티클) 스케일")]
        public float bossVisualScale = 1f;
        [Tooltip("보스 육각 판 색")]
        public Color bossPlateColor = new Color(0.12f, 0.10f, 0.14f);
        [Tooltip("보스 말 이동 애니메이션 시간")]
        public float bossMoveDuration = 1.2f;

        [Header("상태 (읽기 전용)")]
        public int currentTileIndex = -1; // 현재 위치한 방
        public int destinationIndex = -1; // 내가 선택(투표)한 목적지

        // ---- 뷰와 독립적인 맵 상태 (뷰가 꺼져 있어도 유지) ----
        bool _hasState;
        int _seed;
        int _tileCount;
        bool[] _isPentagon;
        List<int>[] _neighbors;
        RoomType[] _roomTypes;
        bool[] _activeRooms;
        int[] _hazards;
        Vector3[] _normals;

        SphereMapView _view;
        readonly List<int> _currentPath = new List<int>();
        int _pendingMoveIndex = -1; // 전투 클리어 후 맵 복귀 시점에 적용할 보류 이동
        readonly List<int> _pendingPath = new List<int>(); // 보류 이동의 경로 (이동 확정 시점에 확정)
        readonly List<GameObject> _voteMarkers = new List<GameObject>(); // 플레이어별 투표 마커
        Mesh _voteMarkerMesh;
        GameObject _bossPiece;      // 구체 위 보스 말 (육각 판 + 2D 보스 비주얼 복제)
        int _bossVisualTile = -1;   // 보스 말이 현재 표시된 타일 (뷰 재생성 후 접근 애니메이션 재현용)
        Mesh _bossPlateMesh;
        Tween _bossTween;

        public bool HasState => _hasState;

        // ------------------------------------------------------------ Unity Lifecycle --------------------------------------------------------------- //

        void OnEnable()
        {
            EnsureView();
            _view.OnTileClicked = HandleTileClicked;
            _view.OnEmptySpaceClicked = HandleEmptySpaceClicked;
            _view.OnRebuilt += HandleViewRebuilt;
            // 맵 복귀(전투 클리어 후) 시점에 보류된 이동을 반영 — 화면이 딤에 가려진 동안 적용되므로 자연스럽다
            if (_pendingMoveIndex >= 0)
                ApplyPendingMove();
            else if (_hasState)
                ApplyStateToTiles();
        }

        void OnDisable()
        {
            _bossTween?.Kill();
            if (_view != null)
            {
                _view.OnTileClicked = null;
                _view.OnEmptySpaceClicked = null;
                _view.OnRebuilt -= HandleViewRebuilt;
            }
        }

        void EnsureView()
        {
            if (_view == null)
                _view = GetComponent<SphereMapView>();
        }

        void HandleViewRebuilt()
        {
            if (_pendingMoveIndex >= 0)
                ApplyPendingMove();
            else if (_hasState)
                ApplyStateToTiles();
        }

        void OnGUI()
        {
            if (!Application.isPlaying || !_hasState)
                return;

            string info = "3D 맵 테스트  |  현재 방: " + currentTileIndex;
            if (destinationIndex >= 0)
            {
                info += "  →  목적지 투표 완료 (" + _roomTypes[destinationIndex] + ", 위험도 " + _hazards[destinationIndex]
                    + ", 거리 " + _currentPath.Count + ") — 모든 플레이어가 레디하면 이동합니다";
            }
            GUI.Label(new Rect(10f, 10f, 900f, 24f), info);
        }

        // ------------------------------------------------------------ Map Setup --------------------------------------------------------------- //

        /// <summary>네트워크에서 받은 시드로 맵 구성 (모든 클라이언트 동일 결과)</summary>
        public void SetNetworkSeed(int seed)
        {
            if (_hasState && _seed == seed)
                return;
            SetupNewMap(seed);
        }

        [ContextMenu("Setup New Map (Random Seed)")]
        public void SetupNewMapRandom()
        {
            SetupNewMap(Random.Range(1, int.MaxValue));
        }

        /// <summary>시드 기반 결정적 맵 생성: 시작 방, 방 타입, 위험도(BFS), 초기 시야</summary>
        public void SetupNewMap(int seed)
        {
            EnsureView();
            List<GoldbergSphereGeometry.Tile> tiles = GoldbergSphereGeometry.Generate(_view.subdivision);
            _tileCount = tiles.Count;
            _isPentagon = new bool[_tileCount];
            _neighbors = new List<int>[_tileCount];
            _roomTypes = new RoomType[_tileCount];
            _activeRooms = new bool[_tileCount];
            _hazards = new int[_tileCount];
            _normals = new Vector3[_tileCount];

            for (int i = 0; i < _tileCount; i++)
            {
                _isPentagon[i] = tiles[i].isPentagon;
                _neighbors[i] = new List<int>(tiles[i].neighbors);
                _normals[i] = tiles[i].normal;
            }

            _seed = seed;
            Random.InitState(seed); // 시드 고정 → 모든 클라이언트가 같은 방 배치를 얻는다

            // 시작 방: 카메라 정면(-Z)에 가장 가까운 육각형 (결정적)
            int start = -1;
            float bestDot = -2f;
            for (int i = 0; i < _tileCount; i++)
            {
                if (_isPentagon[i])
                    continue;
                float d = Vector3.Dot(_normals[i], Vector3.back);
                if (d > bestDot)
                {
                    bestDot = d;
                    start = i;
                }
            }
            currentTileIndex = start;

            // 방 타입 배정 (2D GetRoomType과 동일 분포)
            for (int i = 0; i < _tileCount; i++)
            {
                if (_isPentagon[i])
                {
                    _roomTypes[i] = RoomType.UNDEFINED;
                    continue;
                }
                _roomTypes[i] = (i == start) ? RoomType.START_LOCATION : GetRandomRoomType();
            }

            // 위험도: 시작 방 기준 BFS 홉 거리
            for (int i = 0; i < _tileCount; i++)
                _hazards[i] = -1;
            var queue = new Queue<int>();
            _hazards[start] = 0;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (int next in _neighbors[current])
                {
                    if (_isPentagon[next] || _hazards[next] >= 0)
                        continue;
                    _hazards[next] = _hazards[current] + 1;
                    queue.Enqueue(next);
                }
            }

            // 초기 시야: 시작 방 + 이웃 육각형 활성화
            _activeRooms[start] = true;
            ActivateNeighbours(start);

            destinationIndex = -1;
            _currentPath.Clear();
            _hasState = true;
            ApplyStateToTiles();
        }

        RoomType GetRandomRoomType()
        {
            int randomValue = Random.Range(0, 100);
            if (randomValue < 10) return RoomType.CAMP;
            if (randomValue < 20) return RoomType.EVENT_POSITIIVE;
            if (randomValue < 30) return RoomType.EVENT_NEGATIVE;
            if (randomValue < 40) return RoomType.ITEM_NPC;
            if (randomValue < 50) return RoomType.CARD_NPC;
            if (randomValue < 60) return RoomType.ELITE;
            return RoomType.MONSTER;
        }

        void ActivateNeighbours(int index)
        {
            foreach (int next in _neighbors[index])
            {
                if (!_isPentagon[next])
                    _activeRooms[next] = true;
            }
        }

        // ------------------------------------------------------------ State Query --------------------------------------------------------------- //

        public RoomType GetRoomTypeOf(int index) => _hasState ? _roomTypes[index] : RoomType.UNDEFINED;
        public int GetHazardOf(int index) => _hasState ? _hazards[index] : 0;

        /// <summary>투표/이동 목적지로 유효한지 검사 (서버 검증에도 사용)</summary>
        public bool IsValidDestination(int index)
        {
            if (!_hasState || index < 0 || index >= _tileCount)
                return false;
            if (_isPentagon[index] || !_activeRooms[index])
                return false;
            // 제자리는 보스가 도달해 보스방이 된 경우에만 허용 (2D와 동일)
            if (index == currentTileIndex)
                return _roomTypes[index] == RoomType.BOSS;
            // 보스 출현 시 1칸(이웃)만 이동 가능 (2D GamePlayerMap의 거리 제한과 동일)
            if (IsBossExists())
                return _neighbors[currentTileIndex].Contains(index);
            return FindPath(currentTileIndex, index).Count > 0;
        }

        bool IsBossExists()
        {
            return Application.isPlaying && M_MapManager.instance != null && M_MapManager.instance.mapBoss != null;
        }

        // ------------------------------------------------------------ Click (투표) --------------------------------------------------------------- //

        void HandleTileClicked(SphereMapTile tile)
        {
            if (!_hasState)
                return;
            int index = tile.index;
            if (_isPentagon[index] || !_activeRooms[index])
                return;
            if (index == currentTileIndex && _roomTypes[index] != RoomType.BOSS)
                return; // 제자리는 보스가 도달한 경우(보스전)에만 선택 가능
            if (index == destinationIndex)
            {
                // 같은 목적지 재클릭 → 선택/투표 취소
                ClearSelection();
                SendCancelVote();
                return;
            }

            List<int> path;
            if (index == currentTileIndex)
            {
                path = new List<int>(); // 제자리 보스전 (이동 없음)
            }
            else if (IsBossExists())
            {
                if (!_neighbors[currentTileIndex].Contains(index))
                    return; // 보스 출현 시 1칸만 이동 가능
                path = new List<int> { index };
            }
            else
            {
                path = FindPath(currentTileIndex, index);
                if (path.Count == 0)
                    return; // 도달 불가
            }

            destinationIndex = index;
            _currentPath.Clear();
            _currentPath.AddRange(path);

            ApplyStateToTiles();
            _view.FocusTile(index); // 방사형 상승 + 나머지 어둡게
            SendVote(index);        // 서버에 투표 (모든 플레이어 레디 시 이동)
        }

        void HandleEmptySpaceClicked()
        {
            ClearSelection();
            SendCancelVote();
        }

        void SendVote(int index)
        {
            if (!Application.isPlaying || SphereMapNetwork.instance == null || PlayerRegistry.Local == null)
                return;
            SphereMapNetwork.instance.CmdVote(PlayerRegistry.Local.netId, index);
        }

        void SendCancelVote()
        {
            if (!Application.isPlaying || SphereMapNetwork.instance == null || PlayerRegistry.Local == null)
                return;
            SphereMapNetwork.instance.CmdCancelVote(PlayerRegistry.Local.netId);
        }

        public void ClearSelection()
        {
            destinationIndex = -1;
            _currentPath.Clear();
            ApplyStateToTiles();
            if (_view != null)
                _view.UnfocusTile();
        }

        // ------------------------------------------------------------ Move (서버 RPC로 호출) --------------------------------------------------------------- //

        /// <summary>
        /// 파티를 해당 타일로 이동. SphereMapNetwork의 RPC가 모든 클라이언트에서 호출한다.
        /// applyImmediately=false(전투 진입)면 이동/시야 확장 반영을 보류했다가
        /// 전투 클리어 후 맵 복귀 시점(화면이 딤에 가려진 동안)에 적용한다.
        /// </summary>
        public void MovePartyTo(int destination, bool applyImmediately)
        {
            if (!_hasState)
                return;

            // 선택/포커스는 즉시 정리 (전투 전환 딤과 자연스럽게 이어지도록)
            destinationIndex = -1;
            _currentPath.Clear();
            if (_view != null)
                _view.UnfocusTile();

            // 경로는 이동 확정 시점의 상태로 확정해 둔다 (이후 보스 존 변화 등에 영향받지 않도록)
            List<int> path = BuildMovePath(destination);

            if (applyImmediately)
            {
                ApplyMove(destination, path);
            }
            else
            {
                _pendingMoveIndex = destination;
                _pendingPath.Clear();
                _pendingPath.AddRange(path);
                ApplyStateToTiles(); // 경로 하이라이트 제거만 반영, 맵 상태는 그대로
            }
        }

        List<int> BuildMovePath(int destination)
        {
            if (destination == currentTileIndex)
                return new List<int> { destination }; // 제자리 보스전: 현재 방만 방문 처리
            List<int> path = FindPath(currentTileIndex, destination);
            if (path.Count == 0)
                path = new List<int> { destination }; // 보스 출현 시 1칸 이동 / 상태 불일치 폴백
            return path;
        }

        // 경유/도착 방을 방문 완료(COMPLETE) 처리하고 주변 시야를 밝힌다
        void ApplyMove(int destination, List<int> path)
        {
            foreach (int idx in path)
            {
                _roomTypes[idx] = RoomType.COMPLETE;
                _activeRooms[idx] = true;
                ActivateNeighbours(idx);
            }
            currentTileIndex = destination;
            ApplyStateToTiles();
        }

        void ApplyPendingMove()
        {
            int destination = _pendingMoveIndex;
            _pendingMoveIndex = -1;
            var path = new List<int>(_pendingPath);
            _pendingPath.Clear();
            ApplyMove(destination, path);
        }

        // ------------------------------------------------------------ Boss --------------------------------------------------------------- //

        /// <summary>보스 출현 위치 선정: 현재 위치에서 BFS 거리가 가장 먼 육각형 (서버에서 호출)</summary>
        public int GetFarthestHexagonFrom(int start)
        {
            int[] dist = BFSDistances(start);
            int best = -1;
            int bestDist = -1;
            for (int i = 0; i < _tileCount; i++)
            {
                if (_isPentagon[i])
                    continue;
                if (dist[i] > bestDist)
                {
                    bestDist = dist[i];
                    best = i;
                }
            }
            return best;
        }

        /// <summary>보스가 플레이어 쪽으로 steps칸 접근했을 때의 타일 (2D ApproachBossToPlayer 대응, 방 타입 무시하고 최단 경로)</summary>
        public int GetBossApproachTile(int bossTile, int playerTile, int steps)
        {
            if (bossTile < 0 || playerTile < 0 || bossTile == playerTile)
                return bossTile;

            // 육각형 그래프에서 BFS 역추적으로 보스→플레이어 최단 경로 생성
            var previous = new Dictionary<int, int>();
            var visited = new HashSet<int> { bossTile };
            var queue = new Queue<int>();
            queue.Enqueue(bossTile);
            while (queue.Count > 0 && !visited.Contains(playerTile))
            {
                int current = queue.Dequeue();
                foreach (int next in _neighbors[current])
                {
                    if (_isPentagon[next] || !visited.Add(next))
                        continue;
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }
            if (!previous.ContainsKey(playerTile))
                return bossTile;

            var path = new List<int>();
            int node = playerTile;
            while (node != bossTile)
            {
                path.Add(node);
                node = previous[node];
            }
            path.Reverse();
            return path[Mathf.Min(steps, path.Count) - 1];
        }

        /// <summary>
        /// 보스 타일 변경 반영: 이전 보스 존은 폐허(RUINS), 새 보스 존은 BOSS로 변경.
        /// (2D SetRoomTypeBossRoom과 동일 규칙, SphereMapNetwork의 SyncVar 훅이 호출)
        /// </summary>
        public void SetBossTile(int oldTile, int newTile)
        {
            if (!_hasState)
                return;
            if (oldTile >= 0)
            {
                foreach (int idx in GetZone(oldTile, bossZoneRange))
                {
                    if (_roomTypes[idx] == RoomType.BOSS)
                        _roomTypes[idx] = RoomType.RUINS;
                }
            }
            if (newTile >= 0)
            {
                foreach (int idx in GetZone(newTile, bossZoneRange))
                    _roomTypes[idx] = RoomType.BOSS;
            }
            ApplyStateToTiles();
        }

        // 중심에서 range 거리 이내의 육각형 타일들 (BFS)
        List<int> GetZone(int center, int range)
        {
            var result = new List<int>();
            int[] dist = BFSDistances(center);
            for (int i = 0; i < _tileCount; i++)
            {
                if (!_isPentagon[i] && dist[i] >= 0 && dist[i] <= range)
                    result.Add(i);
            }
            return result;
        }

        int[] BFSDistances(int start)
        {
            var dist = new int[_tileCount];
            for (int i = 0; i < _tileCount; i++)
                dist[i] = -1;
            if (start < 0 || _isPentagon[start])
                return dist;
            var queue = new Queue<int>();
            dist[start] = 0;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (int next in _neighbors[current])
                {
                    if (_isPentagon[next] || dist[next] >= 0)
                        continue;
                    dist[next] = dist[current] + 1;
                    queue.Enqueue(next);
                }
            }
            return dist;
        }

        // ------------------------------------------------------------ Path Finding --------------------------------------------------------------- //

        /// <summary>
        /// 이웃 그래프 기반 최단 경로 검색 (다익스트라).
        /// 2D와 동일 규칙: 경유지는 COMPLETE/START_LOCATION 방만 허용, 목적지는 예외.
        /// 반환 경로는 시작 방을 제외하고 목적지를 포함한다. (경로 길이 = 이동 비용)
        /// </summary>
        public List<int> FindPath(int start, int destination)
        {
            var result = new List<int>();
            if (!_hasState || start < 0 || destination < 0)
                return result;

            var previous = new Dictionary<int, int>();
            var cost = new Dictionary<int, int> { [start] = 0 };
            var visited = new HashSet<int>();
            var openSet = new List<int> { start };

            while (openSet.Count > 0)
            {
                int bestIdx = 0;
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (cost[openSet[i]] < cost[openSet[bestIdx]])
                        bestIdx = i;
                }
                int current = openSet[bestIdx];
                openSet.RemoveAt(bestIdx);
                if (!visited.Add(current))
                    continue;

                if (current == destination)
                {
                    int node = destination;
                    while (node != start)
                    {
                        result.Add(node);
                        node = previous[node];
                    }
                    result.Reverse();
                    return result;
                }

                foreach (int next in _neighbors[current])
                {
                    if (_isPentagon[next] || visited.Contains(next))
                        continue;
                    if (!(_roomTypes[next] == RoomType.COMPLETE || _roomTypes[next] == RoomType.START_LOCATION || next == destination))
                        continue;

                    int newCost = cost[current] + 1;
                    if (!cost.ContainsKey(next) || newCost < cost[next])
                    {
                        cost[next] = newCost;
                        previous[next] = current;
                        if (!openSet.Contains(next))
                            openSet.Add(next);
                    }
                }
            }
            return result; // 도달 불가 → 빈 리스트
        }

        // ------------------------------------------------------------ Visual --------------------------------------------------------------- //

        /// <summary>상태 배열을 뷰 타일에 반영하고 색/아이콘 갱신</summary>
        public void ApplyStateToTiles()
        {
            EnsureView();
            if (!_hasState || _view == null)
                return;
            IReadOnlyList<SphereMapTile> tiles = _view.Tiles;
            if (tiles.Count != _tileCount)
                return; // 뷰가 아직 생성 전이거나 분할 수가 다름

            for (int i = 0; i < _tileCount; i++)
            {
                SphereMapTile tile = tiles[i];
                tile.roomType = _roomTypes[i];
                tile.isActiveRoom = _activeRooms[i];
                tile.hazard = _hazards[i];
                tile.highlight = _currentPath.Contains(i);
                ApplyTileVisual(tile);
            }
            _view.RefreshColors();
            UpdateVoteMarkers();
            UpdateBossPiece();
        }

        /// <summary>색/아이콘만 갱신 (투표 표시 변경 등)</summary>
        public void RefreshAllVisuals()
        {
            ApplyStateToTiles();
        }

        void ApplyTileVisual(SphereMapTile tile)
        {
            if (tile.isPentagon)
            {
                tile.baseColor = _view.pentagonColor; // 이동 불가 지역
                UpdateIcon(tile, null);
                return;
            }

            if (!tile.isActiveRoom)
            {
                // 보스 존/폐허는 미탐험 지역이라도 표시 (보스 위협이 보이도록)
                if (tile.roomType == RoomType.BOSS || tile.roomType == RoomType.RUINS)
                {
                    tile.baseColor = GetRoomColor(tile.roomType);
                    UpdateIcon(tile, tile.roomType == RoomType.BOSS ? GetIconSprite(tile.roomType) : null);
                    return;
                }
                tile.baseColor = inactiveColor; // 아직 밝혀지지 않은 방
                UpdateIcon(tile, null);
                return;
            }

            Color color = GetRoomColor(tile.roomType);
            if (tile.index == currentTileIndex)
                color = startColor; // 현재 위치 표시
            else if (tile.highlight && tile.index != destinationIndex)
                color = Color.Lerp(color, pathColor, 0.6f); // 경로 표시
            else if (IsTileVotedByAnyone(tile.index))
                color = Color.Lerp(color, Color.white, votedTint); // 누군가 투표한 방 표시

            tile.baseColor = color;
            UpdateIcon(tile, tile.index == currentTileIndex ? null : GetIconSprite(tile.roomType));
        }

        bool IsTileVotedByAnyone(int index)
        {
            return Application.isPlaying && SphereMapNetwork.instance != null && SphereMapNetwork.instance.IsTileVoted(index);
        }

        Color GetRoomColor(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.START_LOCATION: return startColor;
                case RoomType.MONSTER: return monsterColor;
                case RoomType.ELITE: return eliteColor;
                case RoomType.EVENT_POSITIIVE: return eventColor;
                case RoomType.EVENT_NEGATIVE: return eventColor;
                case RoomType.CAMP: return campColor;
                case RoomType.ITEM_NPC: return itemShopColor;
                case RoomType.CARD_NPC: return cardShopColor;
                case RoomType.COMPLETE: return completeColor;
                case RoomType.BOSS: return bossColor;
                case RoomType.RUINS: return ruinsColor;
                default: return _view.hexagonColor;
            }
        }

        Sprite GetIconSprite(RoomType roomType)
        {
            SpriteAtlas atlas = ResolveIconAtlas();
            if (atlas == null)
                return null;
            switch (roomType)
            {
                case RoomType.MONSTER: return atlas.GetSprite(Const.M_I_NormalMonster);
                case RoomType.ELITE: return atlas.GetSprite(Const.M_I_EliteMonster);
                case RoomType.EVENT_POSITIIVE: return atlas.GetSprite(Const.M_I_Event);
                case RoomType.EVENT_NEGATIVE: return atlas.GetSprite(Const.M_I_Event);
                case RoomType.CAMP: return atlas.GetSprite(Const.M_I_Camp);
                case RoomType.ITEM_NPC: return atlas.GetSprite(Const.M_I_ItemShop);
                case RoomType.CARD_NPC: return atlas.GetSprite(Const.M_I_CardShop);
                case RoomType.COMPLETE: return atlas.GetSprite(Const.M_I_Complete);
                case RoomType.BOSS: return atlas.GetSprite(Const.M_I_EliteMonster); // 보스 전용 아이콘이 없어 엘리트 아이콘 사용
                default: return null;
            }
        }

        SpriteAtlas ResolveIconAtlas()
        {
            if (iconAtlas != null)
                return iconAtlas;
            if (M_MapManager.instance != null)
                return M_MapManager.instance.mapTileIconAtlas;
            return null;
        }

        // ------------------------------------------------------------ Boss Piece --------------------------------------------------------------- //

        // 구체 위 보스 말 갱신: 없으면 생성, 보스 타일이 바뀌었으면 표면을 따라 이동 애니메이션
        void UpdateBossPiece()
        {
            int target = (Application.isPlaying && SphereMapNetwork.instance != null)
                ? SphereMapNetwork.instance.bossTileIndex : -1;

            if (target < 0)
            {
                if (_bossPiece != null)
                {
                    _bossTween?.Kill();
                    Destroy(_bossPiece);
                    _bossPiece = null;
                }
                _bossVisualTile = -1;
                return;
            }

            IReadOnlyList<SphereMapTile> tiles = _view.Tiles;
            if (tiles.Count != _tileCount)
                return;

            if (_bossPiece == null)
            {
                // 뷰 재생성 직후에는 이전 표시 위치(_bossVisualTile)에 만들어서 접근 애니메이션이 보이게 한다
                int startTile = (_bossVisualTile >= 0 && _bossVisualTile < _tileCount) ? _bossVisualTile : target;
                _bossPiece = CreateBossPiece(tiles[startTile]);
                _bossVisualTile = startTile;
            }

            if (_bossVisualTile != target)
                AnimateBossPiece(_bossVisualTile, target);
        }

        void AnimateBossPiece(int fromTile, int toTile)
        {
            IReadOnlyList<SphereMapTile> tiles = _view.Tiles;
            Vector3 fromPos = tiles[fromTile].center + tiles[fromTile].normal * 0.06f;
            Vector3 toPos = tiles[toTile].center + tiles[toTile].normal * 0.06f;
            Vector3 fromNormal = tiles[fromTile].normal;
            Vector3 toNormal = tiles[toTile].normal;
            _bossVisualTile = toTile;

            _bossTween?.Kill();
            if (!Application.isPlaying)
            {
                if (_bossPiece != null)
                    SetBossPiecePose(_bossPiece.transform, toPos, toNormal);
                return;
            }

            // 구 표면을 따라(Slerp) 이동
            float t = 0f;
            _bossTween = DOTween.To(() => t, v =>
            {
                t = v;
                if (_bossPiece == null)
                    return;
                Vector3 pos = Vector3.Slerp(fromPos, toPos, t);
                Vector3 normal = Vector3.Slerp(fromNormal, toNormal, t).normalized;
                SetBossPiecePose(_bossPiece.transform, pos, normal);
            }, 1f, bossMoveDuration).SetDelay(0.5f).SetEase(Ease.InOutSine);
        }

        static void SetBossPiecePose(Transform pieceTransform, Vector3 localPosition, Vector3 normal)
        {
            pieceTransform.localPosition = localPosition;
            Vector3 upHint = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            // 아이콘과 동일한 규칙: -Z(스프라이트 정면)가 구 바깥을 향하도록
            pieceTransform.localRotation = Quaternion.LookRotation(-normal, upHint);
        }

        GameObject CreateBossPiece(SphereMapTile atTile)
        {
            var piece = new GameObject("BossPiece");
            piece.hideFlags = HideFlags.DontSave;
            piece.transform.SetParent(_view.TileRoot, false); // 구체 회전을 따라감

            // 육각 판: 타일 크기(이웃 중심 간 거리 기준)의 70%
            if (_bossPlateMesh == null)
            {
                _bossPlateMesh = FlatPolygonMeshGenerator.CreateHexagon(ComputeBossPlateEdge(atTile.index), 0.08f);
                _bossPlateMesh.hideFlags = HideFlags.DontSave;
            }
            var plate = new GameObject("Plate");
            plate.hideFlags = HideFlags.DontSave;
            plate.transform.SetParent(piece.transform, false);
            plate.transform.localRotation = Quaternion.FromToRotation(Vector3.up, Vector3.back); // 판 윗면이 구 바깥(-Z 로컬)을 향하도록
            plate.AddComponent<MeshFilter>().sharedMesh = _bossPlateMesh;
            var plateRenderer = plate.AddComponent<MeshRenderer>();
            plateRenderer.sharedMaterial = atTile.Renderer.sharedMaterial;
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", bossPlateColor);
            plateRenderer.SetPropertyBlock(mpb);

            // 2D MapBoss 프리팹의 비주얼(스프라이트/파티클)을 그대로 복제 — 네트워크/로직 컴포넌트는 제거
            var networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
            GameObject bossPrefab = networkRoomManager != null
                ? networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "MapBoss") : null;
            if (bossPrefab != null)
            {
                GameObject visual = Instantiate(bossPrefab, piece.transform);
                visual.name = "BossVisual";
                visual.hideFlags = HideFlags.DontSave;
                foreach (NetworkBehaviour behaviour in visual.GetComponentsInChildren<NetworkBehaviour>(true))
                    DestroyImmediate(behaviour);
                foreach (NetworkIdentity identity in visual.GetComponentsInChildren<NetworkIdentity>(true))
                    DestroyImmediate(identity);
                visual.transform.localPosition = Vector3.back * 0.14f; // 판 위(구 바깥쪽)로 살짝 띄움
                visual.transform.localRotation = Quaternion.identity;  // 스프라이트 정면이 구 바깥을 향함
                visual.transform.localScale = Vector3.one * bossVisualScale;
            }

            SetBossPiecePose(piece.transform, atTile.center + atTile.normal * 0.06f, atTile.normal);
            return piece;
        }

        // 이웃 중심 간 거리의 절반 ≈ 타일 아포템 → 그 70%를 보스 판 크기로 사용
        float ComputeBossPlateEdge(int tileIndex)
        {
            IReadOnlyList<SphereMapTile> tiles = _view.Tiles;
            float minDist = float.MaxValue;
            foreach (int neighbor in _neighbors[tileIndex])
                minDist = Mathf.Min(minDist, Vector3.Distance(tiles[tileIndex].center, tiles[neighbor].center));
            if (minDist == float.MaxValue)
                minDist = 0.6f;
            float apothem = minDist * 0.5f * 0.7f;
            return apothem * 2f / Mathf.Sqrt(3f); // 아포템 → 정육각형 변 길이
        }

        // ------------------------------------------------------------ Vote Marker --------------------------------------------------------------- //

        // 투표한 플레이어의 색으로 작은 육각형 마커를 타일 위에 표시 (2D의 투표 아이콘 대응)
        void UpdateVoteMarkers()
        {
            foreach (GameObject marker in _voteMarkers)
            {
                if (marker != null)
                    Destroy(marker);
            }
            _voteMarkers.Clear();

            if (!Application.isPlaying || SphereMapNetwork.instance == null || _view == null)
                return;
            IReadOnlyList<SphereMapTile> tiles = _view.Tiles;
            if (tiles.Count != _tileCount)
                return;

            // 타일별 투표자 색 수집
            var colorsByTile = new Dictionary<int, List<Color>>();
            foreach (KeyValuePair<uint, int> vote in SphereMapNetwork.instance.votes)
            {
                if (vote.Value < 0 || vote.Value >= _tileCount)
                    continue;
                if (!colorsByTile.TryGetValue(vote.Value, out List<Color> colors))
                {
                    colors = new List<Color>();
                    colorsByTile.Add(vote.Value, colors);
                }
                colors.Add(GetPlayerColor(vote.Key));
            }

            foreach (KeyValuePair<int, List<Color>> entry in colorsByTile)
            {
                SphereMapTile tile = tiles[entry.Key];
                // 접평면 기저 (마커를 가로로 나란히 배치)
                Vector3 t1 = Vector3.Cross(tile.normal, Vector3.up);
                if (t1.sqrMagnitude < 1e-6f)
                    t1 = Vector3.Cross(tile.normal, Vector3.right);
                t1.Normalize();
                Vector3 t2 = Vector3.Cross(tile.normal, t1);

                for (int i = 0; i < entry.Value.Count; i++)
                {
                    var go = new GameObject("VoteMarker");
                    go.hideFlags = HideFlags.DontSave;
                    go.transform.SetParent(tile.transform, false); // 타일이 상승하면 마커도 함께 이동
                    float offset = (i - (entry.Value.Count - 1) * 0.5f) * 0.22f;
                    go.transform.localPosition = tile.center + tile.normal * 0.08f + t1 * offset + t2 * 0.16f;
                    go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, tile.normal);

                    go.AddComponent<MeshFilter>().sharedMesh = GetVoteMarkerMesh();
                    var meshRenderer = go.AddComponent<MeshRenderer>();
                    meshRenderer.sharedMaterial = tile.Renderer.sharedMaterial;
                    var mpb = new MaterialPropertyBlock();
                    mpb.SetColor("_Color", entry.Value[i]);
                    meshRenderer.SetPropertyBlock(mpb);

                    _voteMarkers.Add(go);
                }
            }
        }

        Mesh GetVoteMarkerMesh()
        {
            if (_voteMarkerMesh == null)
            {
                _voteMarkerMesh = FlatPolygonMeshGenerator.CreateHexagon(0.09f, 0.06f);
                _voteMarkerMesh.hideFlags = HideFlags.DontSave;
            }
            return _voteMarkerMesh;
        }

        Color GetPlayerColor(uint playerNetId)
        {
            foreach (PlayerInterface player in PlayerRegistry.All)
            {
                if (player.netId == playerNetId)
                    return player.color;
            }
            return Color.white;
        }

        void UpdateIcon(SphereMapTile tile, Sprite sprite)
        {
            if (sprite == null)
            {
                if (tile.iconRenderer != null)
                    tile.iconRenderer.gameObject.SetActive(false);
                return;
            }

            if (tile.iconRenderer == null)
            {
                var go = new GameObject("Icon");
                go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(tile.transform, false);
                // 타일 면 중심에서 살짝 띄우고, 스프라이트 정면이 구 바깥을 향하도록 회전
                go.transform.localPosition = tile.center + tile.normal * 0.05f;
                Vector3 upHint = Mathf.Abs(Vector3.Dot(tile.normal, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
                go.transform.localRotation = Quaternion.LookRotation(-tile.normal, upHint);
                tile.iconRenderer = go.AddComponent<SpriteRenderer>();
            }

            tile.iconRenderer.gameObject.SetActive(true);
            if (tile.iconRenderer.sprite != sprite)
                tile.iconRenderer.sprite = sprite;
            tile.iconRenderer.transform.localScale = Vector3.one * iconScale;
        }
    }
}
