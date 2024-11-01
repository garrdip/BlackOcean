using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Spine.Unity;
using ProjectD;
using AYellowpaper.SerializedCollections;
using DG.Tweening;

public class M_MapManager : NetworkSingletonD<M_MapManager>
{  
    public readonly SyncDictionary<NetworkIdentity, HexagonMapRoom> playerVoteHexagonMapRoom = new SyncDictionary<NetworkIdentity, HexagonMapRoom>(); // 플레이어의 NetworkIdentity + 플레이어가 선택한 맵  Dictionary 데이터

    public readonly SyncList<uint> hexagonMapRoomNetIds = new SyncList<uint>(); // hexagonMapRoom 오브젝트 NetId 리스트

    public readonly SyncList<Region> regions = new SyncList<Region>(); // 거점지역 리스트

    [SyncVar(hook = nameof(OnChangeCurrentRoom))]
    public HexagonMapRoom currentRoom;

    [SyncVar]
    HexagonMapRoom moveToRoomDestination;

    [SyncVar (hook = nameof(OnChangeMapBoss))]
    public MapBoss mapBoss; // 보스

    [SyncVar]
    public int mapSight; // 맵 시야 변수값

    [SyncVar (hook = nameof(OnChangedMaxActionCost))]
    public int maxActionCost; // 맵에서 소모되는 행동 비용 최대값
    
    [SyncVar (hook = nameof(OnChangedCurrentActionCost))]
    public int currentActionCost; // 현재 남은 액션 비용
    
    [SyncVar (hook = nameof(OnChangedActionCost))]
    public int actionCost; // 행동시 소모되는 행동 비용

    [Header("전투 화면의 요소들의 최상위 오브젝트")]
    public GameObject BattleScene;

    [Header("맵 화면의 요소들의 최상위 오브젝트")]
    public GameObject MapScene;

    [Header("전투 화면의 배경 플레어 스파인 오브젝트")]
    public SkeletonAnimation BackgroundLight;

    [Header("맵화면의 방 요소들의 부모 오브젝트")]
    public GameObject MapRooms;

    [Header("맵 경로 표시할 라인랜더러들의 부모 오브젝트")]
    public GameObject MapPathLines;

    [Header("그리드")]
    public GameObject hexagonGrid;

    [Header("그리드 부모 오브젝트")]
    public Transform gridParent;

    [Header("거점 지역 표시 오브젝트")]
    public GameObject regionIndicatorPrefab;
    public List<RegionIndicator> regionsIndicators = new List<RegionIndicator>();

    [Header("맵에서 플레이어가 컨트롤하는 오브젝트 리스트")]
    public List<GameObject> mapPlayerPieces;

    [Header("HexagonMapRoom 리스트")]
    public List<HexagonMapRoom> hexagonMapRooms = new List<HexagonMapRoom>();

    [Header("Boss Zone HexagonMapRoom 리스트")]
    public List<HexagonMapRoom> bossZoneMapRooms = new List<HexagonMapRoom>();

    [Header("Boss Zone 영역 범위값")]
    public int bossZoneRange = 2;

    [Header("현재 게임플레이어 NetId")]
    public uint ownedGamePlayer;

    private const float angleIncrement = 60f;  // 육각형의 각 면에 생성될 위치를 계산하기 위한 각도

    [Header("경로 표시용 스프라이트 라인랜더러 프리팹")]
    public GameObject pathLineRendererPrefab;

    [Header("검색된 경로를 표시할 스프라이트 라인랜더러 목록")]
    public List<GameObject> pathLineRenderers = new List<GameObject>();

    [Header("거리 측정을 위한 경로 표시 오브젝트 목록")]
    public List<HexagonMapRoom> findPaths = new List<HexagonMapRoom>();

    [Header("맵 타입 아이콘 목록")]
    [SerializedDictionary("MapTypeIcon", "Sprite")]
    public SerializedDictionary<MapTypeIcon, Sprite> mapTypeIcons = new SerializedDictionary<MapTypeIcon, Sprite>();

    [Header("맵 스테이지 아이콘 목록")]
    [SerializedDictionary("MapStage", "Sprite")]
    public SerializedDictionary<MapStage, Sprite> stageIcons = new SerializedDictionary<MapStage, Sprite>();

    public readonly Vector2Int[] offSets = {
        new Vector2Int(0, -1), // 12시
        new Vector2Int(-1, 0), // 11시
        new Vector2Int(-1, 1), // 7시
        new Vector2Int(0, 1), // 6시
        new Vector2Int(1, 0), // 5시
        new Vector2Int(1, -1) // 1시
    };

    private const int maxHexagonGridRange = 12;

    private void Awake()
    {
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.onClientDisconnected += OnClientDisconnected;
    }

    protected override void Start()
    {
        DontDestroyOnLoad(gameObject);
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.persistentManagers.Add(gameObject.name, gameObject);
        Camera.main.orthographicSize = 6.0f;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        playerVoteHexagonMapRoom.Callback += OnChangePlayerVoteHexagonMapRoom;
        // Process initial SyncDictionary payload
        foreach (KeyValuePair<NetworkIdentity, HexagonMapRoom> kvp in playerVoteHexagonMapRoom)
            OnChangePlayerVoteHexagonMapRoom(SyncDictionary<NetworkIdentity, HexagonMapRoom>.Operation.OP_ADD, kvp.Key, kvp.Value);
    }
    
    private void OnClientDisconnected(GamePlayer gamePlayer)
    {
        NetworkIdentity networkIdentity = gamePlayer.netIdentity;
        RemoveAllExistLineRenderer(); // 경로 제거
        if(playerVoteHexagonMapRoom.TryGetValue(networkIdentity, out HexagonMapRoom hexagonMapRoom)){
            hexagonMapRoom.isSelected = false; // 나간 플레이어가 선택했던 방 선택상태 해제
            hexagonMapRoom.votePlyers.Remove(networkIdentity.netId); // 나간 플레이어가 선택했던 방의 투표자 Synclist에서 해당 플레이어 데이터 제거
            playerVoteHexagonMapRoom.Remove(networkIdentity); // 투표자 + 방 Dictionary에서 해당 플레이어 데이터 제거
        }
    }

    #if UNITY_EDITOR
    // 에디터 환경 전용 : 서버유저가 마우스 오른쪽 버튼 누를 시 클릭한 맵과 주위의 맵시야 범위에 활성화 + 완료된 맵 생성
    void Update()
    {
        if(isServer && Input.GetMouseButtonDown(1)){
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            if(hit.collider != null){
                GameObject selectedObject = hit.collider.gameObject;
                HexagonMapRoom hexagonMapRoom = selectedObject.GetComponent<HexagonMapRoom>();
                if(hexagonMapRoom != null && hexagonMapRoom.roomType != RoomType.START_LOCATION){
                    hexagonMapRoom.roomType = RoomType.COMPLETE;
                    hexagonMapRoom.isActive = true;
                    for(int q = -mapSight ; q <= mapSight ; q++){
                        int rStart = Mathf.Max(-mapSight, -q - mapSight);
                        int rEnd = Mathf.Min(mapSight, -q + mapSight);
                        for(int r = rStart; r <= rEnd; r++){
                            Vector3 position = GetPosition(q, r, hexagonMapRoom.position);
                            HexagonMapRoom mapRoom = hexagonMapRooms.Find(room => room.position == position);
                            if(mapRoom != null){
                                mapRoom.isActive = true;
                                mapRoom.roomType = RoomType.COMPLETE;
                            }
                        }
                    }
                }
            }
        }  
    }
    #endif

    // 서버에 생성 시 SyncVar 값들 초기화
    public override void OnStartServer()
    {
        mapSight = 1; // 맵시야
        actionCost = 1; // 행동 비용
        maxActionCost = 30; // 행동비용 최대값
        currentActionCost = 3; // 현재 남은 행동비용
    }

    // ------------------------------------------------------------ Command Method -------------------------------------------------------------- //

    // 맵플레이어 스왑 수행
    [Command (requiresAuthority = false)]
    public void CmdSwapMapPlayer(int oldIndex, int newIndex)
    {
        M_TurnManager.instance.SwapPlayerOrder(oldIndex, newIndex);
    }

    // 스왑 요청을 받은 맵플레이어의 SyncVar 변수에 인덱스 저장 + 요청받은 맵플레이어만 수락,거절 UI 활성화되도록 TargetRpc 전송
    [Command (requiresAuthority = false)]
    public void CmdRequestSwap(int oldIndex, int newIndex)
    {
        uint targetNetId = M_TurnManager.instance.playerOrder[newIndex];
        if(targetNetId != 0 && NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity networkIdentity)){
            GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
            if(gamePlayer.mapPlayerNetId != 0 && NetworkServer.spawned.TryGetValue(gamePlayer.mapPlayerNetId, out NetworkIdentity mapPlayerNetIdentity)){
                MapPlayer targetMapPlayer = mapPlayerNetIdentity.GetComponent<MapPlayer>();
                targetMapPlayer.TargetResponseSwap(targetMapPlayer.GetComponent<NetworkIdentity>().connectionToClient);
                targetMapPlayer.oldIndex = oldIndex; // 요청한 맵플레이어의 인덱스
                targetMapPlayer.newIndex = newIndex; // 요청한 맵플레이어의 교환상대 인덱스
            }
        }
    }


    // ------------------------------------------------------------ Server Method -------------------------------------------------------------- //

    // 오더 관리용 Synclist에 맵 플레이어에 참조되는 게임플레이어의 netId 추가
    [Server]
    public void AddMapPlayer(int index, uint targetNetId)
    {
        M_TurnManager.instance.playerOrder.RemoveAt(index);
        M_TurnManager.instance.playerOrder.Insert(index, targetNetId);
    }

    // 오더 관리용 Synclist에서 맵 플레이어에 참조되는 게임플레이어의 netId 제거
    [Server]
    public void RemoveMapPlayer(uint targetNetId)
    {
        int index = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == targetNetId);
        M_TurnManager.instance.playerOrder[index] = 0;
    }

    [Server]
    public void SetDirection(HexagonMapRoom to)
    {
        moveToRoomDestination = to;
    }

    [Server]    
    public void MoveToRoom()
    {
        int actionCost = FindPath(currentRoom, moveToRoomDestination).Count; // 현재 위치와 목적지 간 거리차 계산
        currentRoom = moveToRoomDestination; // 위치 이동
        DecreaseTotalActionCost(actionCost); // 거리차 만큼 행동 비용 감소
        GenerateHexagonRoom(currentRoom); // 주변 방 생성
        ApproachBossToPlayer(); // 보스가 플레이어에게로 이동
        StartBattle(currentRoom);
    }

    // 맵 플레이어들이 선택한 방 선택지 확인
    // 1. 중복값이 있다는것은 2명 이상이 해당 방을 선택한 것이며, 과반수 이상 이므로 해당 MapRoom 반환
    // 2. 중복값이 없다는 것은 모두 다른 선택을 한 것이므로 랜덤으로 돌려서 선택된 MapRoom 반환
    [Server]
    public HexagonMapRoom GetVoteHexagonMapRoomResult()
    {
        List<HexagonMapRoom> hexagonMapRooms = new List<HexagonMapRoom>();
        foreach(HexagonMapRoom hexagonMapRoom in playerVoteHexagonMapRoom.Values)
        {
            if(!hexagonMapRooms.Contains(hexagonMapRoom)){
                hexagonMapRooms.Add(hexagonMapRoom);
            }else{
                return hexagonMapRoom; // 중복 선택된 방
            }
        }
        if(hexagonMapRooms.Count > 0){
            int randomIndex = Random.Range(0, hexagonMapRooms.Count); // 선택된 방들 중에 랜덤
            return hexagonMapRooms[randomIndex];
        }else{
            return null;
        }
    }

    // 처음 시작시 가운데 1개, 각 변에 6개 생성
    [Server]
    public void GenerateStartHexagonRoom()
    {
        var networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        
        // 가운데 육각형 생성
        GameObject centerRoomObject = Instantiate(
            networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "HexagonMapRoom"),
            Vector3.zero,
            Quaternion.identity
        );
        NetworkServer.Spawn(centerRoomObject);
        
        HexagonMapRoom centerRoom = centerRoomObject.GetComponent<HexagonMapRoom>();
        // 방 타입, 고유 좌표계값, 인게임 좌표계값, 활성화 상태 초기값 설정
        centerRoom.roomType = RoomType.START_LOCATION;
        centerRoom.coordinate = Vector2Int.zero;
        centerRoom.position = Vector3.zero;
        centerRoom.isActive = true;

        // 시작지점을 현재방으로 설정
        currentRoom = centerRoom;

        // 육각형 위치 리스트에 추가
        hexagonMapRooms.Add(centerRoom);
        hexagonMapRoomNetIds.Add(centerRoom.GetComponent<NetworkIdentity>().netId);
        
        // 시작지점 6방향에 활성화된 방 생성
        for(int q = -1 ; q <= 1 ; q++)
        {
            int rStart = Mathf.Max(-1, -q - 1);
            int rEnd = Mathf.Min(1, -q + 1);
     
            for(int r = rStart; r <= rEnd; r++)
            {
                Vector3 position = GetPosition(q, r, centerRoom.position);
                if(!IsPositionDuplicated(position)){
                    GameObject aroundRoomObject = Instantiate(
                            networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "HexagonMapRoom"),
                            position,
                            Quaternion.identity
                        );
                        HexagonMapRoom aroundRoom = aroundRoomObject.GetComponent<HexagonMapRoom>();
                        // 방 타입 설정
                        aroundRoom.roomType = GetRoomType();
                        // 인게임 좌표계 값 설정
                        aroundRoom.position = position;
                        // 방 활성화 상태 설정
                        aroundRoom.isActive = true;
                        // 고유 좌표계 값 설정
                        aroundRoom.coordinate = centerRoom.coordinate + new Vector2Int(q, r);
                        // 위험도값 설정
                        aroundRoom.hazard = GetDistanceFromCurrentCoordinate(centerRoom.coordinate, aroundRoom.coordinate);
                
                        NetworkServer.Spawn(aroundRoomObject);

                        // 육각형 위치 및 오브젝트 클래스 리스트에 추가
                        hexagonMapRooms.Add(aroundRoom);
                        hexagonMapRoomNetIds.Add(aroundRoom.GetComponent<NetworkIdentity>().netId);
                }
            }
        }
        GenerateHexagonMapWorld(maxHexagonGridRange, centerRoom); // 주변 일정범위를 채우는 비활성화된 맵 생성
    }

    // 초기생성된 맵 주변에 비활성화된 맵 생성
    [Server]
    public void GenerateHexagonMapWorld(int count, HexagonMapRoom centerRoom)
    {
        var networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        for(int q = -count ; q <= count ; q++)
        {
            int rStart = Mathf.Max(-count, -q - count);
            int rEnd = Mathf.Min(count, -q + count);
     
            for(int r = rStart; r <= rEnd; r++)
            {
                Vector3 position = GetPosition(q, r, centerRoom.position);
                if(!IsPositionDuplicated(position)){
                    GameObject hexagonMapRoomObject = Instantiate(
                        networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "HexagonMapRoom"),
                        position,
                        Quaternion.identity
                    );
                    HexagonMapRoom hexagonMapRoom = hexagonMapRoomObject.GetComponent<HexagonMapRoom>();
                    // 방 타입 설정
                    hexagonMapRoom.roomType = GetRoomType();
                    // 인게임 좌표계 값 설정
                    hexagonMapRoom.position = position;
                    // 방 활성화 상태값 설정
                    hexagonMapRoom.isActive = false;
                    // 고유 좌표계 값(Axial 좌표계) 설정
                    hexagonMapRoom.coordinate = centerRoom.coordinate + new Vector2Int(q, r);
                    // 위험도값 설정
                    hexagonMapRoom.hazard = GetDistanceFromCurrentCoordinate(centerRoom.coordinate, hexagonMapRoom.coordinate);
                    
                    NetworkServer.Spawn(hexagonMapRoomObject);

                    // 육각형 위치 및 오브젝트 클래스 리스트에 추가
                    hexagonMapRooms.Add(hexagonMapRoom);
                    hexagonMapRoomNetIds.Add(hexagonMapRoom.GetComponent<NetworkIdentity>().netId);
                }
            }
        }
    }

    [Server]
    public void RegenerateStartHexsagonRoom(SaveData saveData)
    {
        var networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        
        foreach(SaveDataMapRoom saveDataMapRoom in saveData.map.hexagonMapRooms)
        {
            Vector3 position = new Vector3(saveDataMapRoom.position.Item1,saveDataMapRoom.position.Item2,saveDataMapRoom.position.Item3);
            GameObject aroundRoomObject = Instantiate(
                networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "HexagonMapRoom"),
                position,
                Quaternion.identity
            );
            HexagonMapRoom aroundRoom = aroundRoomObject.GetComponent<HexagonMapRoom>();
            // 방 타입 설정
            aroundRoom.roomType = saveDataMapRoom.roomType;
            // 인게임 좌표계 값 설정
            aroundRoom.position = position;
            // 방 활성화 상태 설정
            aroundRoom.isActive = saveDataMapRoom.isActive;
            aroundRoom.isRegion = saveDataMapRoom.isRegion;
            
            // 고유 좌표계 값 설정
            aroundRoom.coordinate = new Vector2Int(saveDataMapRoom.coordinate.Item1,saveDataMapRoom.coordinate.Item2);
            NetworkServer.Spawn(aroundRoomObject);

            // 육각형 위치 및 오브젝트 클래스 리스트에 추가
            hexagonMapRooms.Add(aroundRoom);
            hexagonMapRoomNetIds.Add(aroundRoom.GetComponent<NetworkIdentity>().netId);
            
            if(saveData.map.currentRoom == saveDataMapRoom.coordinate){
                currentRoom = aroundRoom;
                GenerateHexagonMapWorld(maxHexagonGridRange, currentRoom);
            }
        }
    }

    // 현재 위치를 중심으로 주변 육각형 생성 : mapSight 값에 따라 생성되는 범위 동적으로 변경
    [Server]
    public void GenerateHexagonRoom(HexagonMapRoom currentHexagonMapRoom)
    {      
        for(int q = -mapSight ; q <= mapSight ; q++)
        {
            int rStart = Mathf.Max(-mapSight, -q - mapSight);
            int rEnd = Mathf.Min(mapSight, -q + mapSight);
     
            for(int r = rStart; r <= rEnd; r++)
            {
                Vector3 position = GetPosition(q, r, currentHexagonMapRoom.position);
                HexagonMapRoom hexagonMapRoom = hexagonMapRooms.Find(room => room.position == position);
                if(hexagonMapRoom != null){
                    hexagonMapRoom.isActive = true;
                }
            }
        }
    }

    // 거점지역 생성
    [Server]
    public void GenerateColorRegion()
    {
        int totalTry = 0;
        int numberOfRegion = Random.Range(6,9); // 총 구역의 수
        for(int i = 0 ;i < numberOfRegion ; i ++)
        {
            Region newRegion = new Region();
            newRegion.GetRegionGrade();
            regions.Add(newRegion);

            int numberOfTiles = (newRegion.regionGrade == RegionGrade.NORMAL) ? Random.Range(4,6) :
                                (newRegion.regionGrade == RegionGrade.RARE) ? Random.Range(5,8) :
                                (newRegion.regionGrade == RegionGrade.UNIQUE) ? Random.Range(6,10) : Random.Range(7,12);
                                                                        
            // 거리와 각도를 이용하여 좌표를 계산
            Vector3 centerPos = new Vector3(0,0,0);
            do{
                if(totalTry >= 100){ // 새로운 지역 생성 불가시 생성 종료
                    regions.Remove(newRegion);
                    return;
                }
                int distance = Random.Range(7,9);
                float angle = Random.Range(0,2*Mathf.PI);
                centerPos.x = (int)(distance * Mathf.Cos(angle));
                centerPos.y = (int)(distance * Mathf.Sin(angle));
                totalTry++;
            }while(regions.Find(x => x.tiles.Exists(tile => tile.coordinate == centerPos)) != null);
            newRegion.tiles.Add(new Tile(centerPos));
            totalTry = 0;
            //각각의 타일의 위치를 정의하는 곳.
            for(int j = 0 ; j < numberOfTiles - 1 ; j++)
            {
                Vector3 newPos = MoveRandomDirection(centerPos); //랜덤 좌표 선택
                if(regions.Find(x => x.tiles.Exists(tile => tile.coordinate == newPos)) != null || newPos == new Vector3(0,0,0))
                {
                    if(totalTry >= 6)break; // 6면이 모두 막혔을경우 종료 (작은 지역으로 생성됨 TBD)
                    j--;
                    totalTry++;
                    continue;
                }
                newRegion.tiles.Add(new Tile(newPos));
                centerPos = newPos;
            }
        }
        GenerateHexagonRoomOnRegion();
    }

    [Server]
    public void RegenerateColorRegion(SaveData saveData)
    {
        foreach(SaveDataRegion saveDataRegion in saveData.map.regions)
        {
            Region newRegion = new Region();
            newRegion.regionGrade = saveDataRegion.regionGrade;
            regions.Add(newRegion);

            //각각의 타일의 위치를 정의하는 곳.
            foreach(SaveDataTile saveDataTile in saveDataRegion.tiles)
            {
                Tile newTile = new Tile(new Vector3(saveDataTile.coordinate.Item1,saveDataTile.coordinate.Item2,0));                
                newRegion.tiles.Add(newTile);
            }
        }
        GenerateHexagonRoomOnRegion();
    }

    // 거점지역으로 결정된 위치에 생성된 HexagonMapRoom에 region 설정값 부여
    [Server]
    public void GenerateHexagonRoomOnRegion()
    {
        foreach(Region region in regions){
            foreach(Tile loc in region.tiles){
                Vector2Int coordinate = new Vector2Int((int)loc.coordinate.x, (int)loc.coordinate.y);
                foreach(HexagonMapRoom hexagonMapRoom in hexagonMapRooms){
                    if(hexagonMapRoom.coordinate == coordinate){
                        // 방 타입 설정
                        hexagonMapRoom.roomType = GetRoomType();
                        // 거점지역 데이터 설정
                        hexagonMapRoom.region = region;
                        // 거점지역 구분 변수값 설정
                        hexagonMapRoom.isRegion = true;
                        // 방 활성화 상태 변수값 false 설정(거점지역의 오브젝트는 그 지역에 도달하기 전까지는 비활성화 상태여야 하므로)
                        hexagonMapRoom.isActive = false;

                        // 거점지역이 시작지점에 생긴경우는 활성화상태 true로 설정
                        foreach(Vector2Int startRoomCoordinate in M_MapManager.instance.offSets){
                            if(startRoomCoordinate == hexagonMapRoom.coordinate){
                               hexagonMapRoom.isActive = true;
                            }
                        }
                    }
                }
            }
        }
    }

    // 맵의 랜덤 위치에 보스 생성
    [Server]
    public void GenreateMapBoss()
    {
        Vector3 centerPos = Vector3.zero;
        int distance = Random.Range(maxHexagonGridRange-2, maxHexagonGridRange);
        float angle = Random.Range(0, 2 * Mathf.PI);
        centerPos.x = (int)(distance * Mathf.Cos(angle));
        centerPos.y = (int)(distance * Mathf.Sin(angle));
        Vector3 position = GetPosition((int)centerPos.x, (int)centerPos.y) + new Vector3(0f, 0.15f, 0f);
        var networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
            GameObject mapBossObject = Instantiate(
                networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "MapBoss"),
                position,
                Quaternion.identity
            );
        MapBoss mapBoss = mapBossObject.GetComponent<MapBoss>();
        mapBoss.coordinate = new Vector2Int((int)centerPos.x, (int)centerPos.y); // 고유 좌표계 설정
        NetworkServer.Spawn(mapBossObject);
        
        this.mapBoss = mapBoss; // 서버에 참조값 생성
    }

    // 맵플레이어가 선택한 MapRoom값을 Dictionary<NetworkIdentity, MapRoom> 형태로 저장
    [Server]
    public void VoteHexagonMapRoom(HexagonMapRoom hexagonMapRoom, NetworkIdentity networkIdentity)
    {
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        // 이전에 선택한 방의 votePlayers에서 현재 플레이어 제거 후 새로 선택한 방의 votePlayers에 추가
        if(playerVoteHexagonMapRoom.TryGetValue(networkIdentity, out HexagonMapRoom prevMapRoom)){
            prevMapRoom.votePlyers.Remove(networkIdentity.netId);
        }
        hexagonMapRoom.votePlyers.Add(networkIdentity.netId);

        // playerVoteHexagonMapRoom에  networkIdentity + hexagonMapRoom을 쌍으로 데이터 저장
        if(playerVoteHexagonMapRoom.ContainsKey(networkIdentity)){
            playerVoteHexagonMapRoom[networkIdentity] = hexagonMapRoom;
        }else{
            playerVoteHexagonMapRoom.Add(networkIdentity, hexagonMapRoom);
        }

        // 맵플레이어가 선택한 MapRoom의 isSelected 상태 변경
        if(networkRoomManager.numPlayers > 1 && hexagonMapRoom.votePlyers.Count > 1 && hexagonMapRoom.isSelected){ // 1인 이상 플레이일때, 여러명이 같은 방을 선택한 경우
            hexagonMapRoom.isSelected = true;
        }else{
            hexagonMapRoom.isSelected = !hexagonMapRoom.isSelected; // 맵 선택상태 토글
            if(hexagonMapRoom.isSelected == false){
                // MapRoom이 비활성화면 투표데이터 제거 및 라인렌더러 제거
                hexagonMapRoom.votePlyers.Remove(networkIdentity.netId);
                playerVoteHexagonMapRoom.Remove(networkIdentity);
                GamePlayerMap gamePlayerMap = NetworkServer.spawned[networkIdentity.netId].GetComponent<GamePlayerMap>();
                gamePlayerMap.RpcHidePath(gamePlayerMap.GetComponent<GamePlayer>().objectOwner.netId);
            }
        }

        // hexagonMapRooms 리스트의 값을 초기값으로 가지는 HashSet생성(중복 방지)
        HashSet<HexagonMapRoom> voteHexagonMapRoomExcept = new HashSet<HexagonMapRoom>(hexagonMapRooms);
        foreach(HexagonMapRoom voteHexagonMapRoom in playerVoteHexagonMapRoom.Values){
            voteHexagonMapRoomExcept.Remove(voteHexagonMapRoom); // 맵플레이어가 선택한 방들을 제외
        }
        foreach(HexagonMapRoom otherHexagonMapRoom in voteHexagonMapRoomExcept){
            otherHexagonMapRoom.isSelected = false; // 맵플레이어가 선택하지 않은 남은 방들에 대해 isSelected를 false로 설정
        }
    }

    // 방 완료상태로 변경
    [Server]
    public void SetRoomStateComplete()
    {
        if(currentRoom != null){
            currentRoom.roomType = RoomType.COMPLETE;
        }
    }

    // 방 투표 목록 Dictionary 리셋
    [Server]
    public void ClearPlayerVoteHexagonMapRooms()
    {
        playerVoteHexagonMapRoom.Clear();
    }

    // 행동 비용 감소
    // 1. 맵에서 방 이동투표 후 최종 이동 시 비용 소모 : 완료된 방들 사이의 이동은 거리만큼이 소모 비용(cost 인자값)
    // 2. 맵에서 방 클리어 후 비용 소모
    [Server]
    public void DecreaseTotalActionCost(int cost = 0)
    {
        if(currentActionCost > 0){
            int reduceActionCost = (cost > 0) ? cost : actionCost; // 비용값이 설정된 경우 그 비용값 만큼 감소, 아닐 경우 기본 비용값 만큼 감소
            if(reduceActionCost <= currentActionCost){
                currentActionCost = Mathf.Max(0, currentActionCost - reduceActionCost);
                if(currentActionCost == 0 && mapBoss == null){
                    GenreateMapBoss(); // 코스트값이 0이면 서버에서 보스 생성
                }
            }
        }
    }

    // 행동비용이 0이 되어 보스가 생성되었을때, 매 ReturnToMap 호출시마다 정해진 칸만큼 보스가 플레이어에게 가까워지게 위치 이동
    [Server]
    public void ApproachBossToPlayer()
    {
        if(mapBoss != null){
            int stepCount = 2; // 보스가 한번에 이동하는 칸 수
            Vector2Int mapPlayerCoordinate = currentRoom.coordinate;
            Vector2Int bossCoordinate = mapBoss.coordinate;
            Vector2Int direction = mapPlayerCoordinate - bossCoordinate;
            direction.x = Mathf.Clamp(direction.x, -stepCount, stepCount);
            direction.y = Mathf.Clamp(direction.y, -stepCount, stepCount);

            if (direction != Vector2Int.zero) {
                mapBoss.coordinate += direction;
            }

            // 보스 위치 변경
            mapBoss.bossPosition = GetPosition(mapBoss.coordinate.x, mapBoss.coordinate.y) + new Vector3(0f, 0.15f, 0f);

            foreach(HexagonMapRoom hexagonMapRoom in hexagonMapRooms){
                hexagonMapRoom.mapBoss = null;
                if(hexagonMapRoom.coordinate == mapBoss.coordinate){
                    hexagonMapRoom.mapBoss = mapBoss;
                }
            }
        }
    }

    // 방의 타입에 따라 전투 및 이벤트 진입
    [Server]
    public void StartBattle(HexagonMapRoom hexagonMapRoom)
    {
        if(mapBoss != null){ // 보스가 출현한 경우
            if(hexagonMapRoom.roomType == RoomType.BOSS){ // 목적지가 보스방일 경우 -> 보스전
                M_TurnManager.instance.GenerateBattleObject(hexagonMapRoom);
                ChangeBattleScene(hexagonMapRoom);
            }else{
                if(hexagonMapRoom.roomType == RoomType.COMPLETE || hexagonMapRoom.roomType == RoomType.START_LOCATION){ // 목적지가 완료된 방 또는 시작지점인 경우
                    if(hexagonMapRoom.mapBoss == null){ 
                        MoveWithoutBattle(); // 목적지에 보스가 없을 경우 -> 이동만 수행
                    }else{ 
                        M_TurnManager.instance.GenerateBattleObject(hexagonMapRoom);
                        ChangeBattleScene(hexagonMapRoom); // 목적지에 보스가 있을 경우 -> 보스전
                    }
                }else{ // 목적지가 완료되지 않은 방인 경우
                    if(hexagonMapRoom.mapBoss != null){ 
                        M_TurnManager.instance.GenerateBattleObject(hexagonMapRoom);
                        ChangeBattleScene(hexagonMapRoom); // 목적지에 보스가 있을 경우 -> 보스전
                    }else{ 
                        M_TurnManager.instance.GenerateBattleObject(hexagonMapRoom);
                        ChangeBattleScene(hexagonMapRoom); // 목적지에 보스가 없을 경우 -> 전투 혹은 이벤트 시작
                    }
                }
            }
        }else{ // 보스가 출현하지 않은 경우
            if(hexagonMapRoom.roomType == RoomType.COMPLETE || hexagonMapRoom.roomType == RoomType.START_LOCATION){
                MoveWithoutBattle(); // 목적지가 완료된 방 또는 시작지점인 경우 -> 이동만 수행
            }else{
                M_TurnManager.instance.GenerateBattleObject(hexagonMapRoom);
                ChangeBattleScene(hexagonMapRoom); // 목적지가 완료되지 않은 방의 경우 -> 전투 혹은 이벤트 시작
            }
        }
    }

    // 보스가 위치한 방의 주변을 보스방 존으로 변경 + 보스가 지나간 방을 폐허로 변경
    [Server]
    public void SetRoomTypeBossRoom(HexagonMapRoom currentBossRoom)
    {   
        // 이전에 보스방이었던 곳은 모두 폐허로 변경
        List<HexagonMapRoom> prevBossRooms = hexagonMapRooms.FindAll((room) => room.roomType == RoomType.BOSS);
        foreach(HexagonMapRoom prevBossRoom in prevBossRooms){
            prevBossRoom.roomType = RoomType.RUINS;
        }
        // 현재 보스가 위치한 방과 주위 방을 보스방으로 변경(변경 범위는 bossZoneRange값에 따라 유동적)
        for (int q = -bossZoneRange; q <= bossZoneRange; q++) {
            for (int r = Mathf.Max(-bossZoneRange, -q - bossZoneRange); r <= Mathf.Min(bossZoneRange, -q + bossZoneRange); r++) {
                Vector2Int axialCoordinates = new Vector2Int(currentBossRoom.coordinate.x + q, currentBossRoom.coordinate.y + r);
                HexagonMapRoom hexagonMapRoom = hexagonMapRooms.Find(room => room.coordinate == axialCoordinates);
                if (hexagonMapRoom != null) {
                    hexagonMapRoom.roomType = RoomType.BOSS;
                    bossZoneMapRooms.Add(hexagonMapRoom);
                }
            }
        }
    }

    // 서버에서 이웃 방 검색 : hexagonMapRooms List에서 검색
    [Server]
    private HexagonMapRoom FindNeighboursForServer(int index, HexagonMapRoom currentHexagonRoom)
    {
        return hexagonMapRooms.Find((room) => room.coordinate == currentHexagonRoom.coordinate + offSets[index]); 
    }

    // ------------------------------------------------------------ Client Method ----------------------------------------------------------------- //

    // 클라에서 이웃 방 검색 : NetworkClient.spawned Dictionary에서 NetId를 통해 검색
    [Client]
    private HexagonMapRoom FindNeighboursForClient(int index, HexagonMapRoom currentHexagonRoom)
    {
        foreach (uint netId in hexagonMapRoomNetIds){
            HexagonMapRoom mapRoom = NetworkClient.spawned[netId].GetComponent<HexagonMapRoom>();
            if(mapRoom.coordinate == currentHexagonRoom.coordinate + offSets[index]){
                return mapRoom;
            }
        }
        return null;
    }

    // ------------------------------------------------------------ ClientRpc Method -------------------------------------------------------------- //
    
    // 화면 딤처리 후 전투씬으로 변경
    [ClientRpc]
    private void ChangeBattleScene(HexagonMapRoom hexagonMapRoom)
    {
        GameUIManager.instance.DoScreenChangeIn(() => {
            Camera.main.orthographicSize = GameUIManager.battelSceneCameraSize;

            // UI 활성화 상태 변경
            MapScene.SetActive(false);
            BattleScene.SetActive(true);
            BackgroundLight.GetComponent<MeshRenderer>().sortingLayerName = "BackLayer"; // 배경 플레어 정렬 오더 변경

            RemoveAllExistLineRenderer(); // 라인 랜더러 비활성화
            ChangeAllMapPlayerDestinationState(false); // 맵 위치 화살표 비활성화
            StartCoroutine(CheckTargetObject());
            StartCoroutine(M_TurnManager.instance.ProcessMonsterDeathCoroutine()); // 몬스터 사망처리 코루틴 시작

            GameUIManager.instance.DoScreenChangeOut();
        });
    }
    
    public IEnumerator CheckTargetObject()
    {
        // TODO
        while(true)
        {
            yield return new WaitForSeconds(0.1f);
            if(NetworkClient.connection.identity.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerTarget>().targetObject != 0 )
            {
                NetworkClient.connection.identity.GetComponent<PlayerInterface>().isTargetObjectInitDone = true;
                break;
            }
        }
    }

    // 전투 없이 이동 수행 : MapPlayerDestination 오브젝트 삭제 + 라인렌더러 삭제
    [ClientRpc]
    public void MoveWithoutBattle()
    {
        RemoveAllExistLineRenderer();
        ChangeAllMapPlayerDestinationState(false);
    }

    [ClientRpc]
    public void SetRegionWithColorRPC()
    {
        foreach(Region region in regions)
            SetRegionWithColor(region);
    }

    // ------------------------------------------------------------ Syncvar Hook --------------------------------------------------------------- //

    // 행동비용 총량 변경 이벤트 수신
    public void OnChangedCurrentActionCost(int oldValue, int newValue)
    {
        Debug.Log($"행동 비용이 {oldValue} -> {newValue} 감소했습니다.");
        MapUI.instance.textCurrentActionCost.text = $"{newValue.ToString()}턴";
        GameUIManager.instance.textCurrentActionCost.text = $"{newValue.ToString()}턴";
        MapUI.instance.turnGageBar.fillAmount = ((float)newValue / (float)maxActionCost);
    }

    public void OnChangedMaxActionCost(int oldValue, int newValue)
    {
        MapUI.instance.textMaxActionCostCount.text = $"{newValue.ToString()}턴";
        GameUIManager.instance.textMaxActionCost.text = $"{newValue.ToString()}턴";
    }

    // 행동비용 변경 이벤트 수신(1회당 소모되는 행동비용 값)
    public void OnChangedActionCost(int oldValue, int newValue)
    {
        // TBD
    }

    // 맵 보스 변경 이벤트 수신
    public void OnChangeMapBoss(MapBoss oldValue, MapBoss newValue)
    {
        if(newValue != null){
            M_MessageManager.instance
                .MakeToast()
                .Position(ToastPosition.Top)
                .FadeInTime(2.5f)
                .FadeOutTime(1.5f)
                .MessageBoxColor(ColorUtils.HexToColor("#E700FF"))
                .TextColor(Color.white)
                .Text("맵에 보스가 출현 했습니다.")
                .Show();
            AudioClip audioClip_map = M_SoundManager.instance.bgmClips[BGM_TYPE.Map].Find((audioClip) => audioClip.name.Equals("Stage_1_Map_Boss_Spawn"));
            M_SoundManager.instance.PlayBGM(audioClip_map, MusicTransition.CrossFade, 2f); // 맵 보스 배경음 재생
            List<AudioClip> clips = new List<AudioClip>();
            for(int i=37; i<43; i++){
                AudioClip audioClip = M_SoundManager.instance.voiceClips[VOICE_TYPE.MoonGirl][i];
                clips.Add(audioClip);
            }
            AudioClip mapBossVoice = clips[Random.Range(0, clips.Count)];
            M_SoundManager.instance.StopAllVoice(); // 모든 음성 재생 중지
            M_SoundManager.instance.PlayVoice(mapBossVoice, mapBossVoice.length); // 맵 보스 출현 나레이션 음성 재생(feat. MoonGirl)

            MapUI.instance.SetMapInfoStateMapBossApperance();
        }
    }

    // 현재 플레이어들이 위치하는 방 변경 이벤트 수신
    public void OnChangeCurrentRoom(HexagonMapRoom oldValue, HexagonMapRoom newValue)
    {
        MapUI.instance.textHazardValue.text = newValue.hazard.ToString();
    }

    // 맵 투표 정보 SyncDictionary Callback
    void OnChangePlayerVoteHexagonMapRoom(SyncDictionary<NetworkIdentity, HexagonMapRoom>.Operation op, NetworkIdentity key, HexagonMapRoom item)
    {
        switch (op)
        {
            case SyncIDictionary<NetworkIdentity, HexagonMapRoom>.Operation.OP_ADD:
                GamePlayer addGamePlayer = NetworkClient.spawned[key.netId].GetComponent<GamePlayer>();
                RemoveMapInfoPopUpItem(addGamePlayer);
                CreateMapInfoPopUpItem(addGamePlayer, item);
                ChangeDimmingByPlayerVote(key, true);
                break;
            case SyncIDictionary<NetworkIdentity, HexagonMapRoom>.Operation.OP_SET:
                GamePlayer setGamePlayer = NetworkClient.spawned[key.netId].GetComponent<GamePlayer>();
                RemoveMapInfoPopUpItem(setGamePlayer);
                CreateMapInfoPopUpItem(setGamePlayer, item);
                ChangeDimmingByPlayerVote(key, true);
                break;
            case SyncIDictionary<NetworkIdentity, HexagonMapRoom>.Operation.OP_REMOVE:
                GamePlayer removeGamePlayer = NetworkClient.spawned[key.netId].GetComponent<GamePlayer>();
                RemoveMapInfoPopUpItem(removeGamePlayer);
                ChangeDimmingByPlayerVote(key, false);
                break;
            case SyncIDictionary<NetworkIdentity, HexagonMapRoom>.Operation.OP_CLEAR:
                
                break;
        }
    }

    // ------------------------------------------------------------ Normal Method -------------------------------------------------------------- //

    // 가중치 랜덤 수행으로 방 타입 결정하여 반환
    private RoomType GetRoomType()
    {
        int ramdomValue = Random.Range(0,100);
        if(ramdomValue < 10) return RoomType.CAMP;
        if(ramdomValue < 20) return RoomType.EVENT_POSITIIVE;
        if(ramdomValue < 30) return RoomType.EVENT_NEGATIVE;
        if(ramdomValue < 40) return RoomType.ITEM_NPC;
        if(ramdomValue < 50) return RoomType.CARD_NPC;
        if(ramdomValue < 60) return RoomType.ELITE;
        else return RoomType.MONSTER;
    }

    // 그리드 Axial좌표계 -> 인게임 좌표계 반환(중심점 동적 설정 Version)
    public Vector3 GetPosition(int x, int y, Vector3 centerPosition)
    {
        float yOffset = 0.6f;
        float length = 1 / Mathf.Tan(Mathf.PI / 3);
        Vector3 retVal = new Vector3(0, 0, 0);
        retVal.x = centerPosition.x + (1.5f * x * length);
        retVal.y = centerPosition.y - (y * yOffset) - (x * yOffset * 0.5f);
        return retVal;
    }

    // 그리드 Axial좌표계 -> 인게임 좌표계 반환(중심점 0,0,0 Version)
    public Vector3 GetPosition(int x, int y)
    {
        float yOffset = 0.6f;
        float length = 1 / Mathf.Tan(Mathf.PI / 3);
        Vector3 retVal = new Vector3(0, 0, 0);
        retVal.x = 1.5f * x * length;
        retVal.y = (-y * yOffset) - (x * yOffset * 0.5f);
        return retVal;
    } 

    Vector3 MoveRandomDirection(Vector3 loc)
    {
        Vector3 retVal = loc;
        switch(Random.Range(0,6))
        {
            case 0: // 12시
                retVal += new Vector3(0,-1,0);
                break;
            case 1: // 2시
                retVal += new Vector3(1,-1,0);
                break;
            case 2: // 4시
                retVal += new Vector3(1,0,0);
                break;
            case 3: // 6시
                retVal += new Vector3(0,1,0);
                break;
            case 4: // 8시
                retVal += new Vector3(-1,1,0);
                break;
            case 5: // 10시
                retVal += new Vector3(-1,0,0);
                break;
        }
        return retVal;
    }

    public void SetRegionWithColor(Region region)
    {
        foreach(Tile loc in region.tiles)
        {
            for(int i = 0;  i < 6 ; i ++)
            {
                switch(i)
                {
                    case 0 : // 12시
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x,loc.coordinate.y-1,0)))
                            continue;
                        break;
                    case 1 : // 2시
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x+1,loc.coordinate.y-1,0)))
                            continue;
                        break;
                    case 2 : // 4시
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x+1,loc.coordinate.y,0)))
                            continue;
                        break;
                    case 3 :// 6시
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x,loc.coordinate.y+1,0)))
                            continue;
                        break;
                    case 4 :// 8시
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x-1,loc.coordinate.y+1,0)))
                            continue;
                        break;
                    case 5 :// 10시
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x-1,loc.coordinate.y,0)))
                            continue;
                        break;
                }
                GameObject newRegion = Instantiate(regionIndicatorPrefab, GetPosition((int)loc.coordinate.x,(int)loc.coordinate.y), Quaternion.identity,gridParent);
                RegionIndicator regionIndicator = newRegion.GetComponent<RegionIndicator>();
                regionIndicator.coordinate = new Vector2Int((int)loc.coordinate.x,(int)loc.coordinate.y);
                regionIndicator.index = i;
                newRegion.transform.localPosition = newRegion.transform.position;
                Color regionColor = new Color(0,0,0);
                switch(region.regionGrade)
                {
                    case RegionGrade.NORMAL :
                        regionColor = new Color(1,0,0);
                        break;
                    case RegionGrade.RARE :
                        regionColor = new Color(0,1,0);
                        break;
                    case RegionGrade.UNIQUE :
                        regionColor = new Color(0,0,1);
                        break;
                    case RegionGrade.LEGEND :
                        regionColor = new Color(1,0.8f,0);
                        break;
                        
                }
                newRegion.GetComponent<SpriteRenderer>().color = regionColor;
                regionsIndicators.Add(regionIndicator);
            }
        }
    }

    // Axial 좌표계를 이용한 시스템에서 현재 좌표에서 목표 좌표까지의 거리를 반환
    public int GetDistanceFromCurrentCoordinate(Vector2Int startAt, Vector2Int endAt)
    {
        int dQ = endAt.x - startAt.x;
        int dR = endAt.y - startAt.y;
        int distance = (Mathf.Abs(dQ) + Mathf.Abs(dR) + Mathf.Abs(dQ + dR)) / 2;
        return distance;
    }

    // 육각형 방 생성하려는 위치에 이미 방이 존재하는지 체크
    private bool IsPositionDuplicated(Vector3 spawnPosition)
    {
        foreach(HexagonMapRoom hexagonMapRoom in hexagonMapRooms){
            if(hexagonMapRoom.position == spawnPosition){
                return true;
            }
        }
        return false;
    }

    public void CreateMapInfoPopUpItem(GamePlayer gamePlayer, HexagonMapRoom hexagonMapRoom)
    {
        GameObject mapInfoPopUpItemObject = Instantiate(MapUI.instance.mapInfoPopUpItemPrefab, Vector3.zero, Quaternion.identity);
        MapInfoPopUpItem mapInfoPopUpItem = mapInfoPopUpItemObject.GetComponent<MapInfoPopUpItem>();
        mapInfoPopUpItem.netId = gamePlayer.netId;
        if(hexagonMapRoom.roomType == RoomType.EVENT_POSITIIVE || hexagonMapRoom.roomType == RoomType.EVENT_NEGATIVE){
            mapInfoPopUpItem.textRoomType.text = "EVENT"; // 긍정적 or 부정적 이벤트인 경우 정보창에는 EVENT로만 표시
        }else{
            mapInfoPopUpItem.textRoomType.text = hexagonMapRoom.roomType.ToString();
        }
        MapUI.instance.mapInfoPopUps.Add(mapInfoPopUpItem);
        if(gamePlayer.isOwned){
            mapInfoPopUpItemObject.transform.SetParent(MapUI.instance.ownerPosition.transform);
            mapInfoPopUpItemObject.transform.localScale = Vector3.one;
            mapInfoPopUpItemObject.transform.localPosition = Vector3.zero;
        }else{
            mapInfoPopUpItemObject.transform.SetParent(MapUI.instance.verticalLayoutGroup.transform);
            mapInfoPopUpItemObject.transform.localScale = Vector3.one;
            mapInfoPopUpItemObject.transform.localPosition = Vector3.zero;
        }
    }

    public void RemoveMapInfoPopUpItem(GamePlayer gamePlayer)
    {
       int index = MapUI.instance.mapInfoPopUps.FindIndex((item) => item.netId == gamePlayer.netId);
        if(index != -1){
            Destroy(MapUI.instance.mapInfoPopUps[index].gameObject);
            MapUI.instance.mapInfoPopUps.RemoveAt(index);
        }
    }

    // 로컬 플레이어가 투표한 방인 경우 맵 화면 딤처리 상태 변경
    public void ChangeDimmingByPlayerVote(NetworkIdentity votePlayerNetIdentity, bool isActive)
    {
        bool isLocalPlayerVote = (votePlayerNetIdentity == NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.netIdentity);
        if(isLocalPlayerVote){
            MapUI.instance.ChangeMapDimBackground(isActive);
        }
    }

    // ------------------------------------------------------------ A* Algorithm Method -------------------------------------------------------------- //

    // A* 알고리즘을 이용한 경로검색
    public List<HexagonMapRoom> FindPath(HexagonMapRoom start, HexagonMapRoom destination)
    { 
        List<HexagonMapRoom> openSet = new List<HexagonMapRoom>(); // 아직 방문하지 않은 노드들 목록
        HashSet<HexagonMapRoom> closedSet = new HashSet<HexagonMapRoom>(); // 이미 방문한 노드들의 목록(중복제거)
        openSet.Add(start); // 시작점 추가

        // 검색 시작
        while(openSet.Count > 0)
        {
            // FCost와 HCost를 비교해서 오름차순 정렬
            openSet.Sort((nodeA, nodeB) => {
                int costComparison = nodeA.FCost.CompareTo(nodeB.FCost); // openSet에서 FCost가 가장 낮은 노드를 선택
                if (costComparison == 0) { // FCost 같은 경우 HCost가 낮은 노드를 선택
                    return nodeA.HCost.CompareTo(nodeB.HCost);
                }
                return costComparison;
            });
            HexagonMapRoom currentNode = openSet[0];

            openSet.Remove(currentNode); // openset에서 현재 노드 제거
            closedSet.Add(currentNode); // closedSet에 현재 노드 추가

            // 현재 노트가 목적지 노드와 같다면 목적지에 도달한 것이므로 경로를 생성해서 반환
            if(currentNode.coordinate == destination.coordinate)
            {
                return CreatePath(start, currentNode);
            }

            List<HexagonMapRoom> neighbours = GetNeighbours(currentNode, destination.coordinate); // 현재 노드의 주변 이웃 노드 조회
            foreach (HexagonMapRoom neighbour in neighbours)
            {
                if(closedSet.Contains(neighbour)) // 이웃노드가 방문한 목록에 있으면 그대로 진행
                    continue;

                // Cost값 갱신
                int newGCost = currentNode.GCost + 1;
                int newHCost = CalculateHeuristics(neighbour.coordinate, destination.coordinate);
                int newFCost = newGCost + newHCost;

                if(newFCost < neighbour.FCost || !openSet.Contains(neighbour))
                {
                    // 이웃노드들의 Cost값 갱신
                    neighbour.GCost = newGCost;
                    neighbour.HCost = newHCost;
                    neighbour.previousNode = currentNode;

                    // openSet리스트에 중복을 허용하지 않고 이웃노드 추가
                    if(!openSet.Contains(neighbour)) 
                    {
                        openSet.Add(neighbour);
                    }
                }
            }
        }
        return new List<HexagonMapRoom>(); // 경로 검색에 실패하면 빈 리스트 반환
    }

    // 현재 방의 6방향좌표에 있는 이웃 방 검색
    private List<HexagonMapRoom> GetNeighbours(HexagonMapRoom currentHexagonRoom, Vector2Int destinationCoord)
    {
        List<HexagonMapRoom> neighbours = new List<HexagonMapRoom>();
        for(int i = 0; i < 6; i++)
        {
            HexagonMapRoom neighbour = isServer ? FindNeighboursForServer(i, currentHexagonRoom) : FindNeighboursForClient(i, currentHexagonRoom); // 서버, 클라 분기 처리하여 이웃방 검색
            if(neighbour != null && (neighbour.roomType == RoomType.COMPLETE || neighbour.roomType == RoomType.START_LOCATION || neighbour.coordinate == destinationCoord)){
                neighbours.Add(neighbour);
            }
        }
        return neighbours;
    }

    // [휴리스틱함수]
    // A* 알고리즘에서 목적지까지 가는데 걸리는 비용 예측 함수. 해당함수의 로직에 따라 알고리즘 효율성 결정.
    // 현재는 단순 두 지점간의 좌표계에 따른 거리값만 계산(Axial 좌표계에서의 맨해튼거리 계산식)
    public int CalculateHeuristics(Vector2Int currentCoord, Vector2Int destinationCoord)
    {
        int dQ = currentCoord.x - destinationCoord.x;
        int dR = currentCoord.y - destinationCoord.y;
        int distance = (Mathf.Abs(dQ) + Mathf.Abs(dR) + Mathf.Abs(dQ + dR)) / 2;
        return distance;
    }

    // 검색된 최단경로들에 있는 HexagonMapRoom들의 목록 생성해서 반환
    // 현재 노드의 멤버변수로 이전 노드가 존재하기 때문에 검색된 노드들을 역추적하며 경로를 생성하는 방식
    private List<HexagonMapRoom> CreatePath(HexagonMapRoom startNode, HexagonMapRoom endNode)
    {
        List<HexagonMapRoom> path = new List<HexagonMapRoom>();
        HexagonMapRoom currentNode = endNode;

        while(currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.previousNode;
        }
        path.Reverse();

        // 이동하려는 위치까지의 비용이 현재 남은 비용보다 작은 경우, 검색된 경로에서 모자라는 만큼 노드를 제거(= 검색된 경로중 현재 남은 비용으로 이동 가능한 만큼의 노드만 반환)
        if(currentActionCost < path.Count){
            int numToRemove = path.Count - currentActionCost;
            for(int i = 0; i < numToRemove; i++){
                int lastIndex = path.Count - 1;
                path.RemoveAt(lastIndex);
            }
        }
        return path;
    }

    // netId에 해당하는 유저의 경로 랜더링
    public void RenderVisualizePath(HexagonMapRoom startAt, List<HexagonMapRoom> findPath, uint netId, MapPlayerDestination currentMapPlayerDestination)
    {
        // 현재 플레이어 위치 시작지점으로 경로 추가
        GameObject startPathLineRenderer = Instantiate(pathLineRendererPrefab, Vector3.zero, Quaternion.identity, MapPathLines.transform);
        PathLineRenderer path = startPathLineRenderer.GetComponent<PathLineRenderer>();
        SpriteRenderer sprite = startPathLineRenderer.GetComponent<SpriteRenderer>();
        sprite.color = currentMapPlayerDestination.GetComponent<SpriteRenderer>().color;
        path.netId = netId;
        float startAngle = GetAngleFromCoordinate(startAt.coordinate, findPath[0].coordinate); // 선의 회전값 계산
        path.rotationZ = startAngle;
        SetPathLineScaleByAngle(startAngle, startPathLineRenderer);
        Vector3 startPosition = ((startAt.originMapTile.transform.position) + (findPath[0].originMapTile.transform.position)) / 2f; // 선의 중심 위치 계산
        startPathLineRenderer.transform.position = startPosition;
        pathLineRenderers.Add(startPathLineRenderer);

        // 검색된 경로 추가
        for(int i=0; i<findPath.Count-1; i++)
        {
            GameObject pathLineRenderer = Instantiate(pathLineRendererPrefab, Vector3.zero, Quaternion.identity, MapPathLines.transform);
            PathLineRenderer pathLineRendererComponent = pathLineRenderer.GetComponent<PathLineRenderer>();
            SpriteRenderer spriteRenderer = pathLineRenderer.GetComponent<SpriteRenderer>();
            spriteRenderer.color = currentMapPlayerDestination.GetComponent<SpriteRenderer>().color;
            pathLineRendererComponent.netId = netId;
            float angle = GetAngleFromCoordinate(findPath[i].coordinate, findPath[i + 1].coordinate); // 선의 회전값 계산
            pathLineRendererComponent.rotationZ = angle;
            SetPathLineScaleByAngle(angle, pathLineRenderer);
            Vector3 pathPosition = ((findPath[i].originMapTile.transform.position) + (findPath[i + 1].originMapTile.transform.position)) / 2f; // 선의 중심 위치 계산
            pathLineRenderer.transform.position = pathPosition;
            pathLineRenderers.Add(pathLineRenderer);
        }
    }

    // 검색 경로 라인렌더러 스케일 변경
    private void SetPathLineScaleByAngle(float angle, GameObject pathLineRenderer)
    {
        if(angle == 90f || angle == -90f){
            pathLineRenderer.transform.localScale = new Vector3(0.09f, 0.5f, 1f);
        }else{
            pathLineRenderer.transform.localScale = new Vector3(0.135f, 0.5f, 1f);
        }
        pathLineRenderers.Add(pathLineRenderer);
    }

    // 고유좌표계를 이용해 시작점과 끝점 사이의 각도 뱐환
    private float GetAngleFromCoordinate(Vector2Int start, Vector2Int end)
    {
        Vector2Int offset = end - start;
        if (offset == new Vector2Int(0, -1))
            return 90f;  // 북
        else if (offset == new Vector2Int(-1, 0))
            return -19f; // 11시
        else if (offset == new Vector2Int(-1, 1))
            return 19f;  // 7시
        else if (offset == new Vector2Int(0, 1))
            return -90f; // 남
        else if (offset == new Vector2Int(1, 0))
            return -19f; // 5시
        else if (offset == new Vector2Int(1, -1))
            return 19f;  // 1시
        else
            return 0f;
    }

    // netId에 해당하는 유저의 기존 랜더링된 경로 삭제
    public void RemoveExistLineRenderer(uint netId)
    {
        for(int i=pathLineRenderers.Count-1; i>=0; i--){
            if(pathLineRenderers[i].GetComponent<PathLineRenderer>().netId == netId){
                Destroy(pathLineRenderers[i]);
                pathLineRenderers.RemoveAt(i);
            }
        }
    }

    // 랜더링된 경로 모두 삭제
    public void RemoveAllExistLineRenderer()
    {
        for(int i=pathLineRenderers.Count-1; i>=0; i--){
            Destroy(pathLineRenderers[i]);
            pathLineRenderers.RemoveAt(i);
        }
    }

    // MapPlayerDestination 모두 상태 변경
    public void ChangeAllMapPlayerDestinationState(bool isActive)
    {
        foreach(uint netId in M_TurnManager.instance.playerOrder){
            if(netId != 0 && NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayerMap gamePlayerMap = networkIdentity.GetComponent<GamePlayerMap>();
                MapPlayerDestination mapPlayerDestination = gamePlayerMap.currentMapPlayerDestination;
                mapPlayerDestination.gameObject.SetActive(isActive); 
            }
        }
    }
}

public enum MapTypeIcon {
    Normal_Monster,
    Elite_Monster,
    Card_Shop
}

public enum MapStage {
    Stage1,
    Stage2,
    Stage3
}
