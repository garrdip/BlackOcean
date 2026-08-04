using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using Mirror;
using DG.Tweening;
using TMPro;

namespace ProjectD
{
    /// <summary>
    /// 3D 구체 맵의 게임 로직 레이어.
    /// 기존 2D 맵 시스템과 같은 규칙을 이웃 그래프 기반으로 재현한다:
    /// - 방 타입 배정: M_MapManager.GetRoomType()과 동일한 확률 분포 (몬스터 40%, 나머지 각 10%)
    /// - 위험도(hazard): 시작 방에서 1칸 멀어질 때마다 +3, 1턴 지날 때마다 +1
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

        [Header("방 정보 팝업 (2D hexagonMapRoomUI 대응)")]
        [Tooltip("투표 정보 창 배율. 1이면 2D 맵과 같은 비율(타일 아이콘과 동일한 기준 스케일)이 된다")]
        [Min(0.01f)] public float roomInfoScale = 1f;

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

        [Header("거점지역")]
        [Tooltip("맵 생성 시 만들 거점지역 개수 (공간이 부족하면 더 적게 생성될 수 있음)")]
        public int regionCount = 8;
        [Tooltip("거점지역 크기(타일 수) 최소값")]
        public int regionSizeMin = 8;
        [Tooltip("거점지역 크기(타일 수) 최대값")]
        public int regionSizeMax = 14;
        [Tooltip("등급별 외곽선 색 (2D SetRegionWithColor와 동일값)")]
        public Color regionNormalColor = new Color(1f, 0f, 0f);
        public Color regionRareColor = new Color(0f, 1f, 0f);
        public Color regionUniqueColor = new Color(0f, 0f, 1f);
        public Color regionLegendColor = new Color(1f, 0.8f, 0f);

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
        readonly List<List<int>> _regionTiles = new List<List<int>>();   // 거점지역별 타일 인덱스 묶음
        readonly List<RegionGrade> _regionGrades = new List<RegionGrade>();
        List<GoldbergSphereGeometry.Tile> _geoTiles; // 외곽선 메쉬 생성용 기하 캐시 (SetupNewMap에서 채움)
        readonly List<GameObject> _regionBorders = new List<GameObject>(); // 지역별 외곽선 메쉬 오브젝트

        SphereMapView _view;
        readonly List<int> _currentPath = new List<int>();
        int _pendingMoveIndex = -1; // 전투 클리어 후 맵 복귀 시점에 적용할 보류 이동
        readonly List<int> _pendingPath = new List<int>(); // 보류 이동의 경로 (이동 확정 시점에 확정)
        readonly List<GameObject> _voteMarkers = new List<GameObject>(); // 플레이어별 투표 마커
        GameObject _bossPiece;      // 구체 위 보스 말 (육각 판 + 2D 보스 비주얼 복제)
        int _bossVisualTile = -1;   // 보스 말이 현재 표시된 타일 (뷰 재생성 후 접근 애니메이션 재현용)
        Mesh _bossPlateMesh;
        Tween _bossTween;
        // 구체 위 장식(아이콘·방 정보 창·텍스트)이 쓰는 정렬 레이어. 2D 헥사 맵이 쓰던 것과 같은 레이어다.
        // 기본값 "Default"는 프로젝트 정렬 레이어 목록의 맨 앞이라, URP 2D 렌더러가 그 배치를 가장 먼저 그린 뒤
        // 뒤 배치를 처리하면서 타일 틈새(깊이가 비어 있는 픽셀)를 다시 칠해 장식이 틈새에서만 지워졌다.
        const string SortingLayerIcon = "HexagonMapRoomIcon";
        const string SortingLayerRoomUI = "HexagonMapRoomUI";

        /// <summary>시작 지점에서 1칸 멀어질 때마다 오르는 위험도</summary>
        const int HazardPerTile = 3;
        HexagonMapRoom _voteWindowTemplate; // 투표 정보 창을 복제해 올 2D 방 프리팹

        public bool HasState => _hasState;

        // ------------------------------------------------------------ Unity Lifecycle --------------------------------------------------------------- //

        void OnEnable()
        {
            EnsureView();
            _view.OnTileClicked = HandleTileClicked;
            _view.OnEmptySpaceClicked = HandleEmptySpaceClicked;
            _view.OnTileHovered = HandleTileHovered;
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
            foreach (GameObject marker in _voteMarkers)
            {
                if (marker != null)
                {
                    marker.transform.DOKill();
                    foreach (SpriteRenderer sr in marker.GetComponentsInChildren<SpriteRenderer>(true))
                        sr.DOKill();
                }
            }
            if (_view != null)
            {
                _view.OnTileClicked = null;
                _view.OnEmptySpaceClicked = null;
                _view.OnTileHovered = null;
                _view.OnRebuilt -= HandleViewRebuilt;
            }
            HideRegionPopUp();
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
                info += "  →  목적지 투표 완료 (" + _roomTypes[destinationIndex] + ", 위험도 " + GetHazardOf(destinationIndex)
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
            _geoTiles = tiles; // 외곽선 메쉬 생성에 재사용
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

            // 거점지역: 시드 고정 상태이므로 모든 클라이언트가 같은 지역 배치를 얻는다
            GenerateRegions(start);

            // 위험도(거리분): 시작 방 기준 BFS 홉 거리 1칸당 HazardPerTile 씩 증가.
            // 여기에 경과 턴수가 더해진 값이 최종 위험도다 (GetHazardOf 참조).
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
                    _hazards[next] = _hazards[current] + HazardPerTile;
                    queue.Enqueue(next);
                }
            }

            // 초기 시야: 시작 방 + 이웃 육각형 활성화
            _activeRooms[start] = true;
            ActivateNeighbours(start);

            destinationIndex = -1;
            _currentPath.Clear();
            _hasState = true;
            ClearRegionBorders(); // 이전 맵의 외곽선 제거 (ApplyStateToTiles에서 새로 생성)
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

        // ------------------------------------------------------------ Region PopUp (거점지역 정보 팝업) --------------------------------------------------------------- //

        // 2D 맵의 HexagonMapRoom.OnMouseEnter/Exit 대응 — 거점지역 소속 타일에 호버하면 등급 팝업 표시
        void HandleTileHovered(SphereMapTile tile)
        {
            if (MapUI.instance == null)
                return;
            if (tile != null && _hasState && !_isPentagon[tile.index])
            {
                int regionIndex = FindRegionIndex(tile.index);
                if (regionIndex >= 0)
                {
                    MapUI.instance.RegionPopUpShow(_regionGrades[regionIndex]);
                    return;
                }
            }
            MapUI.instance.RegionPopUpHide();
        }

        void HideRegionPopUp()
        {
            if (MapUI.instance != null)
                MapUI.instance.RegionPopUpHide();
        }

        // 타일이 속한 거점지역 인덱스 (-1 = 소속 없음). 지역 수가 적어(기본 8개) 선형 탐색로 충분
        int FindRegionIndex(int tileIndex)
        {
            for (int i = 0; i < _regionTiles.Count; i++)
            {
                if (_regionTiles[i].Contains(tileIndex))
                    return i;
            }
            return -1;
        }

        // ------------------------------------------------------------ Region (거점지역) --------------------------------------------------------------- //

        // 거점지역 생성: 랜덤 시드 타일에서 이웃으로 성장시킨 육각형 덩어리 (2D의 Region.tiles 대응).
        // 지역끼리 최소 1타일 간격을 보장해 외곽선이 같은 홈을 공유하지 않게 한다.
        // Random.InitState(seed) 이후 호출되므로 모든 클라이언트에서 결정적이다.
        void GenerateRegions(int start)
        {
            _regionTiles.Clear();
            _regionGrades.Clear();

            var blocked = new bool[_tileCount];
            for (int i = 0; i < _tileCount; i++)
                blocked[i] = _isPentagon[i];
            // 시작 방과 초기 시야(이웃)는 지역에 포함하지 않는다
            blocked[start] = true;
            foreach (int nb in _neighbors[start])
                blocked[nb] = true;

            int attempts = 0;
            while (_regionTiles.Count < regionCount && attempts < 200)
            {
                attempts++;
                int seedTile = Random.Range(0, _tileCount);
                if (blocked[seedTile])
                    continue;

                int targetSize = Random.Range(regionSizeMin, regionSizeMax + 1);
                var regionTiles = new List<int> { seedTile };
                var frontier = new List<int>();
                AddRegionFrontier(seedTile, blocked, regionTiles, frontier);
                while (regionTiles.Count < targetSize && frontier.Count > 0)
                {
                    int pickIdx = Random.Range(0, frontier.Count);
                    int pick = frontier[pickIdx];
                    frontier.RemoveAt(pickIdx);
                    regionTiles.Add(pick);
                    AddRegionFrontier(pick, blocked, regionTiles, frontier);
                }
                if (regionTiles.Count < regionSizeMin)
                {
                    blocked[seedTile] = true; // 공간이 부족한 시드는 재시도 대상에서 제외
                    continue;
                }

                foreach (int t in regionTiles)
                {
                    blocked[t] = true;
                    foreach (int nb in _neighbors[t])
                        blocked[nb] = true; // 지역 간 간격 확보
                }
                _regionTiles.Add(regionTiles);
                _regionGrades.Add(GetRandomRegionGrade());
            }
        }

        void AddRegionFrontier(int tileIndex, bool[] blocked, List<int> regionTiles, List<int> frontier)
        {
            foreach (int nb in _neighbors[tileIndex])
            {
                if (!blocked[nb] && !regionTiles.Contains(nb) && !frontier.Contains(nb))
                    frontier.Add(nb);
            }
        }

        // 2D Region.GetRegionGrade와 동일 분포 (각 25%)
        RegionGrade GetRandomRegionGrade()
        {
            int value = Random.Range(0, 100);
            if (value < 25) return RegionGrade.LEGEND;
            if (value < 50) return RegionGrade.UNIQUE;
            if (value < 75) return RegionGrade.RARE;
            return RegionGrade.NORMAL;
        }

        Color GetRegionGradeColor(RegionGrade grade)
        {
            switch (grade)
            {
                case RegionGrade.RARE: return regionRareColor;
                case RegionGrade.UNIQUE: return regionUniqueColor;
                case RegionGrade.LEGEND: return regionLegendColor;
                default: return regionNormalColor;
            }
        }

        // ------------------------------------------------------------ State Query --------------------------------------------------------------- //

        public RoomType GetRoomTypeOf(int index) => _hasState ? _roomTypes[index] : RoomType.UNDEFINED;

        /// <summary>방 위험도 = 시작 지점에서의 거리분(1칸당 HazardPerTile) + 게임 시작 후 경과 턴수</summary>
        public int GetHazardOf(int index) => _hasState ? _hazards[index] + ElapsedTurnHazard : 0;

        /// <summary>경과 턴수만큼 모든 방에 공통으로 더해지는 위험도. 남은 턴이 줄어든 만큼이 경과 턴수다.</summary>
        static int ElapsedTurnHazard
        {
            get
            {
                if (!Application.isPlaying || M_MapManager.instance == null)
                    return 0;
                return Mathf.Max(0, M_MapManager.instance.maxActionCost - M_MapManager.instance.currentActionCost);
            }
        }

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
                tile.hazard = GetHazardOf(i);
                tile.highlight = _currentPath.Contains(i);
                ApplyTileVisual(tile);
            }
            _view.RefreshColors();
            UpdateVoteMarkers();
            UpdateBossPiece();
            EnsureRegionBorders();
        }

        // ------------------------------------------------------------ Region Border (거점지역 외곽선) --------------------------------------------------------------- //

        // 거점지역 외곽선: 타일 사이 홈(spacing)에 정확히 끼워지는 상감 메쉬 (2D SetRegionWithColor의 RegionIndicator 대응).
        // TileRoot 자식이라 구체 회전을 따라가고, 뷰 Rebuild 시 함께 파괴되므로 null 감지로 재생성한다.
        void EnsureRegionBorders()
        {
            if (_regionBorders.Count > 0 && _regionBorders[0] != null)
                return;
            _regionBorders.Clear();
            if (_geoTiles == null || _regionTiles.Count == 0 || _view == null)
                return;
            IReadOnlyList<SphereMapTile> tiles = _view.Tiles;
            if (tiles.Count != _tileCount || tiles.Count == 0)
                return;

            for (int r = 0; r < _regionTiles.Count; r++)
            {
                Mesh mesh = GoldbergSphereGeometry.BuildRegionBorderMesh(_geoTiles, _regionTiles[r], _view.radius, _view.spacing);
                if (mesh == null)
                    continue;
                var go = new GameObject("RegionBorder_" + r);
                go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(_view.TileRoot, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var meshRenderer = go.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = tiles[0].Renderer.sharedMaterial;
                var mpb = new MaterialPropertyBlock();
                mpb.SetColor("_Color", GetRegionGradeColor(_regionGrades[r]));
                meshRenderer.SetPropertyBlock(mpb);
                _regionBorders.Add(go);
            }
        }

        void ClearRegionBorders()
        {
            foreach (GameObject border in _regionBorders)
            {
                if (border == null)
                    continue;
                var meshFilter = border.GetComponent<MeshFilter>();
                if (meshFilter != null)
                    DestroyObjectSafe(meshFilter.sharedMesh);
                DestroyObjectSafe(border);
            }
            _regionBorders.Clear();
        }

        // ContextMenu(에디트 모드)에서도 호출될 수 있으므로 플레이 여부에 따라 파괴 방식 분기
        static void DestroyObjectSafe(Object target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
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
        // 2D MapPlayerDestination과 동일하게 남은 이동 거리 숫자 + 업다운 바운스 연출 포함
        void UpdateVoteMarkers()
        {
            foreach (GameObject marker in _voteMarkers)
            {
                if (marker != null)
                {
                    marker.transform.DOKill();
                    foreach (SpriteRenderer sr in marker.GetComponentsInChildren<SpriteRenderer>(true))
                        sr.DOKill(); // 방 정보 창 라이트 펄스(DOFade)는 트랜스폼이 아닌 렌더러 대상 트윈
                    Destroy(marker);
                }
            }
            _voteMarkers.Clear();

            if (!Application.isPlaying || SphereMapNetwork.instance == null || _view == null)
                return;
            IReadOnlyList<SphereMapTile> tiles = _view.Tiles;
            if (tiles.Count != _tileCount)
                return;

            // 타일별 투표자 수집 (2D는 방마다 votePlyers 목록을 들고 있다)
            var votersByTile = new Dictionary<int, List<uint>>();
            foreach (KeyValuePair<uint, int> vote in SphereMapNetwork.instance.votes)
            {
                if (vote.Value < 0 || vote.Value >= _tileCount)
                    continue;
                if (!votersByTile.TryGetValue(vote.Value, out List<uint> voters))
                {
                    voters = new List<uint>();
                    votersByTile.Add(vote.Value, voters);
                }
                voters.Add(vote.Key);
            }

            foreach (KeyValuePair<int, List<uint>> entry in votersByTile)
            {
                SphereMapTile tile = tiles[entry.Key];

                // 타일별 그룹 루트 — 투표 정보 창을 묶어 함께 바운스 (2D MapPlayerDestination.MoveBounce 대응)
                var group = new GameObject("VoteMarkerGroup");
                group.hideFlags = HideFlags.DontSave;
                group.transform.SetParent(tile.transform, false); // 타일이 상승하면 정보 창도 함께 이동

                // 내가 이 타일에 투표했는지 (2D ChangeHexagonMapRoomLayoutState와 동일하게 레이아웃을 나눈다)
                bool isMyVote = PlayerRegistry.Local != null && entry.Value.Contains(PlayerRegistry.Local.netId);
                CreateVoteInfoWindow(group.transform, tile, entry.Key, isMyVote, entry.Value);

                // 법선 방향 업다운 바운스 무한 반복 (2D의 0.2f/0.3s 업다운과 동일 리듬)
                group.transform.DOLocalMove(tile.normal * 0.12f, 0.3f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);

                _voteMarkers.Add(group);
            }

            UpdateVoteInfoPopUps();
        }

        // 화면 우상단 방 정보창 갱신 — 2D의 playerVoteHexagonMapRoom 콜백(CreateMapInfoPopUpItem) 대응
        void UpdateVoteInfoPopUps()
        {
            if (!Application.isPlaying || MapUI.instance == null || M_MapManager.instance == null || SphereMapNetwork.instance == null)
                return;
            foreach (PlayerInterface player in PlayerRegistry.All)
            {
                GamePlayer gamePlayer = player.currentGamePlayer;
                if (gamePlayer == null)
                    continue;
                M_MapManager.instance.RemoveMapInfoPopUpItem(gamePlayer);
                if (SphereMapNetwork.instance.votes.TryGetValue(player.netId, out int votedTile)
                    && votedTile >= 0 && votedTile < _tileCount)
                    M_MapManager.instance.CreateMapInfoPopUpItem(gamePlayer, _roomTypes[votedTile]);
            }
        }

        // 현재 방에서 투표 타일까지의 이동 거리 (2D는 findPath.Count를 그대로 표기)
        int ComputeVoteDistance(int tileIndex)
        {
            if (tileIndex == currentTileIndex)
                return 0; // 제자리 보스전
            if (IsBossExists())
                return 1; // 보스 출현 시 1칸 이동만 허용됨
            return FindPath(currentTileIndex, tileIndex).Count;
        }

        // ------------------------------------------------------------ Room Info Panel (방 정보 팝업) --------------------------------------------------------------- //

        // 2D 방 프리팹(HexagonMapRoom)의 투표 정보 창을 통째로 복제해 타일 위에 세운다.
        // 손으로 다시 배치하지 않고 원본을 그대로 쓰므로 배경 창(PopWindow)·핀·라인·위험도 화살표까지 2D와 동일하게 나온다.
        // (2D MapBoss 비주얼을 복제하는 CreateBossPiece와 같은 방식)
        void CreateVoteInfoWindow(Transform parent, SphereMapTile tile, int tileIndex, bool isMyVote, List<uint> voters)
        {
            HexagonMapRoom template = GetVoteWindowTemplate();
            GameObject layoutTemplate = template == null ? null : (isMyVote ? template.myVoteLayout : template.anotherVoteLayout);
            if (layoutTemplate == null)
                return;

            // 앵커: 타일 면 위에 서서 스프라이트 정면이 구 바깥을 향한다. 2D 레이아웃이 방 중심 기준으로 배치돼 있으므로
            // 복제본의 로컬 위치(예: MyVoteLayout의 y=+1.26)를 그대로 두면 2D와 같은 간격이 유지된다.
            var anchor = new GameObject("VoteInfoWindow");
            anchor.hideFlags = HideFlags.DontSave;
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = tile.center + tile.normal * 0.12f;
            Vector3 upHint = Mathf.Abs(Vector3.Dot(tile.normal, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            anchor.transform.localRotation = Quaternion.LookRotation(-tile.normal, upHint);
            anchor.transform.localScale = Vector3.one * (iconScale * roomInfoScale);

            GameObject layout = Instantiate(layoutTemplate, anchor.transform, false);
            layout.name = "VoteLayout";
            layout.hideFlags = HideFlags.DontSave;
            layout.SetActive(true); // 프리팹에서는 꺼진 상태로 보관된다

            // 복제본은 전부 Default 레이어라 타일 틈새에서 지워진다 — 맵 레이어로 옮기고 계층 순서대로 정렬값 부여
            int order = 0;
            foreach (Renderer renderer in layout.GetComponentsInChildren<Renderer>(true))
                ApplyMapSortingLayer(renderer, SortingLayerRoomUI, order++);

            PopulateVoteWindow(layout.transform, tileIndex, isMyVote, voters, template);
        }

        // 2D 방 프리팹 (보스 말과 같은 경로로 얻는다 — 플레이 중에만 유효)
        HexagonMapRoom GetVoteWindowTemplate()
        {
            if (_voteWindowTemplate != null)
                return _voteWindowTemplate;
            var networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
            GameObject roomPrefab = networkRoomManager != null
                ? networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "HexagonMapRoom") : null;
            if (roomPrefab != null)
                _voteWindowTemplate = roomPrefab.GetComponent<HexagonMapRoom>();
            return _voteWindowTemplate;
        }

        // 2D 방 타일 폭에 맞춰 축소 — 구체 타일(이웃 중심 간 거리)이 2D 방보다 훨씬 작다
        // 복제한 2D 레이아웃에 현재 맵 데이터를 채운다 (2D의 ChangeMapHazardValue / OnUpdateVotePlayers 대응)
        void PopulateVoteWindow(Transform layout, int tileIndex, bool isMyVote, List<uint> voters, HexagonMapRoom template)
        {
            // 이동 비용 — 2D는 findPath.Count를 그대로 표기
            string distance = ComputeVoteDistance(tileIndex).ToString();
            SetLayoutText(layout, "TextMyRequireCost", distance);
            SetLayoutText(layout, "TextAnotherRequireCost", distance);

            ApplyHazardIndicator(layout, tileIndex);
            ApplyRoomInfoEmblem(layout, tileIndex, isMyVote);
            ApplyVoteIcons(layout, voters, template);
        }

        // 위험도 증감 표시 — 2D ChangeMapHazardValue와 동일 (화살표 뒤집기 + 색상)
        void ApplyHazardIndicator(Transform layout, int tileIndex)
        {
            Transform arrowTransform = layout.Find("HazardArrow");
            SpriteRenderer arrow = arrowTransform != null ? arrowTransform.GetComponent<SpriteRenderer>() : null;
            if (currentTileIndex < 0)
            {
                if (arrow != null)
                    arrow.gameObject.SetActive(false);
                return;
            }

            int hazardDiff = GetHazardOf(tileIndex) - GetHazardOf(currentTileIndex); // 경과 턴분은 서로 상쇄되어 거리 차이만 남는다
            SetLayoutText(layout, "TextHazardValue", Mathf.Abs(hazardDiff).ToString());
            if (hazardDiff == 0)
            {
                SetLayoutText(layout, "TextHazardState", M_LanguageManager.Get("ui.hazard.same", "위험도 동일"));
                if (arrow != null)
                {
                    arrow.gameObject.SetActive(false);
                    arrow.color = Color.white;
                }
                return;
            }

            SetLayoutText(layout, "TextHazardState", hazardDiff > 0 ? M_LanguageManager.Get("ui.hazard.up", "위험도 증가") : M_LanguageManager.Get("ui.hazard.down", "위험도 감소"));
            if (arrow != null)
            {
                arrow.gameObject.SetActive(true);
                arrow.flipY = hazardDiff < 0;
                arrow.color = hazardDiff > 0 ? Color.red : ProjectD.ColorUtils.HexToColor("#0080ff");
            }
        }

        // 방 타입 문장(배경+아이콘) — 2D OnChangedRoomType의 매핑을 그대로 사용
        void ApplyRoomInfoEmblem(Transform layout, int tileIndex, bool isMyVote)
        {
            Transform info = layout.Find("MapRoomInfo");
            if (info == null || M_MapManager.instance == null)
                return; // AnotherVoteLayout에는 문장이 없다 (2D와 동일)
            if (!TryGetRoomInfoKeys(_roomTypes[tileIndex],
                out MapRoomInfoBase baseKey, out MapRoomInfoBase baseLightKey,
                out MapRoomInfoIcon iconKey, out MapRoomInfoIcon iconLightKey))
                return; // 정보 창 스프라이트가 없는 방 타입 (2D와 동일하게 미표시)

            M_MapManager.instance.mapRoomInfoBases.TryGetValue(baseKey, out Sprite baseSprite);
            M_MapManager.instance.mapRoomInfoBases.TryGetValue(baseLightKey, out Sprite baseLightSprite);
            M_MapManager.instance.mapRoomInfoIcons.TryGetValue(iconKey, out Sprite iconSprite);
            M_MapManager.instance.mapRoomInfoIcons.TryGetValue(iconLightKey, out Sprite iconLightSprite);

            SetLayoutSprite(info, "Base", baseSprite);
            SpriteRenderer baseLight = SetLayoutSprite(info, "BaseLight", baseLightSprite);
            SetLayoutSprite(info, "Icon", iconSprite);
            SpriteRenderer iconLight = SetLayoutSprite(info, "IconLight", iconLightSprite);

            // 내가 투표한 방에서만 라이트 펄스 (2D ChangeMapRoomInfoState 대응)
            if (isMyVote)
            {
                PulseInfoLight(baseLight);
                PulseInfoLight(iconLight);
            }
            else
            {
                SetSpriteAlpha(baseLight, 0f);
                SetSpriteAlpha(iconLight, 0f);
            }
        }

        // 플레이어 순번 자리마다 투표 아이콘 — 2D OnUpdateVotePlayers 대응
        void ApplyVoteIcons(Transform layout, List<uint> voters, HexagonMapRoom template)
        {
            Transform icons = layout.Find("VoteIcons");
            if (icons == null || template == null)
                return;

            for (int i = 0; i < icons.childCount; i++)
            {
                var renderer = icons.GetChild(i).GetComponent<SpriteRenderer>();
                if (renderer != null)
                    renderer.sprite = template.voteIconAnother; // 기본값: 미선택
            }

            if (M_TurnManager.instance == null)
                return;
            foreach (uint voterNetId in voters)
            {
                PlayerInterface voter = FindPlayer(voterNetId);
                if (voter == null)
                    continue;
                int order = M_TurnManager.instance.playerOrder.FindIndex(netId => netId == voter.currentGamePlayerNetId);
                if (order < 0 || order >= icons.childCount)
                    continue;
                var renderer = icons.GetChild(order).GetComponent<SpriteRenderer>();
                if (renderer != null)
                    renderer.sprite = voter == PlayerRegistry.Local ? template.voteIconMinePick : template.voteIconAnotherPick;
            }
        }

        static PlayerInterface FindPlayer(uint netId)
        {
            foreach (PlayerInterface player in PlayerRegistry.All)
            {
                if (player.netId == netId)
                    return player;
            }
            return null;
        }

        static void SetLayoutText(Transform layout, string childName, string value)
        {
            Transform child = layout.Find(childName);
            if (child == null)
                return;
            var text = child.GetComponent<TextMeshPro>();
            if (text != null)
                text.text = value;
        }

        static SpriteRenderer SetLayoutSprite(Transform parent, string childName, Sprite sprite)
        {
            Transform child = parent.Find(childName);
            if (child == null)
                return null;
            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer != null && sprite != null)
                renderer.sprite = sprite;
            return renderer;
        }

        // 라이트 스프라이트 알파 0→1 왕복 펄스 (2D와 동일: 1초 Linear Yoyo 무한)
        void PulseInfoLight(SpriteRenderer spriteRenderer)
        {
            if (spriteRenderer == null)
                return;
            SetSpriteAlpha(spriteRenderer, 0f);
            spriteRenderer.DOFade(1f, 1f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
        }

        void SetSpriteAlpha(SpriteRenderer spriteRenderer, float alpha)
        {
            if (spriteRenderer == null)
                return;
            Color color = spriteRenderer.color;
            spriteRenderer.color = new Color(color.r, color.g, color.b, alpha);
        }

        // RoomType → 방 정보 창 스프라이트 키 (2D HexagonMapRoom.OnChangedRoomType의 매핑과 동일)
        bool TryGetRoomInfoKeys(RoomType roomType,
            out MapRoomInfoBase baseKey, out MapRoomInfoBase baseLightKey,
            out MapRoomInfoIcon iconKey, out MapRoomInfoIcon iconLightKey)
        {
            switch (roomType)
            {
                case RoomType.MONSTER:
                    baseKey = MapRoomInfoBase.NORMAL_MONSTER; baseLightKey = MapRoomInfoBase.NORMAL_MONSTER_L;
                    iconKey = MapRoomInfoIcon.NORMAL_MONSTER; iconLightKey = MapRoomInfoIcon.NORMAL_MONSTER_L;
                    return true;
                case RoomType.ELITE:
                    baseKey = MapRoomInfoBase.ELITE_MONSTER; baseLightKey = MapRoomInfoBase.ELITE_MONSTER_L;
                    iconKey = MapRoomInfoIcon.ELITE_MONSTER; iconLightKey = MapRoomInfoIcon.ELITE_MONSTER_L;
                    return true;
                case RoomType.EVENT_POSITIIVE:
                case RoomType.EVENT_NEGATIVE:
                    baseKey = MapRoomInfoBase.EVENT; baseLightKey = MapRoomInfoBase.EVENT_L;
                    iconKey = MapRoomInfoIcon.EVENT; iconLightKey = MapRoomInfoIcon.EVENT_L;
                    return true;
                case RoomType.CAMP:
                    baseKey = MapRoomInfoBase.CAMP; baseLightKey = MapRoomInfoBase.CAMP_L;
                    iconKey = MapRoomInfoIcon.CAMP; iconLightKey = MapRoomInfoIcon.CAMP_L;
                    return true;
                case RoomType.ITEM_NPC:
                    baseKey = MapRoomInfoBase.ITEM_SHOP; baseLightKey = MapRoomInfoBase.ITEM_SHOP_L;
                    iconKey = MapRoomInfoIcon.ITEM_SHOP; iconLightKey = MapRoomInfoIcon.ITEM_SHOP_L;
                    return true;
                case RoomType.CARD_NPC:
                    baseKey = MapRoomInfoBase.CARD_SHOP; baseLightKey = MapRoomInfoBase.CARD_SHOP_L;
                    iconKey = MapRoomInfoIcon.CARD_SHOP; iconLightKey = MapRoomInfoIcon.CARD_SHOP_L;
                    return true;
                default:
                    baseKey = default; baseLightKey = default; iconKey = default; iconLightKey = default;
                    return false;
            }
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
                ApplyMapSortingLayer(tile.iconRenderer, SortingLayerIcon, 0);
            }

            tile.iconRenderer.gameObject.SetActive(true);
            if (tile.iconRenderer.sprite != sprite)
                tile.iconRenderer.sprite = sprite;
            tile.iconRenderer.transform.localScale = Vector3.one * iconScale;
        }

        // 구체 위 장식을 맵 전용 정렬 레이어로 옮긴다. 레이어가 프로젝트에 없으면 정렬 순서만 적용한다.
        static void ApplyMapSortingLayer(Renderer renderer, string layerName, int order)
        {
            if (renderer == null)
                return;
            if (SortingLayer.NameToID(layerName) != 0)
                renderer.sortingLayerName = layerName;
            renderer.sortingOrder = order;
        }
    }
}
