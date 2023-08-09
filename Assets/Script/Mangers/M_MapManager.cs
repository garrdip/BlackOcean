using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;
using ProjectD;
using AYellowpaper.SerializedCollections;

public class M_MapManager : NetworkBehaviour
{  
    public static M_MapManager Instance = null; 
    
    [SyncVar]
    public HexagonMapRoom currentRoom;
   
    [SyncVar]
    public int turnsLeft = 10;

    [SyncVar]
    HexagonMapRoom moveToRoomDestination;

    [SyncVar]
    public int mapSight; // 맵 시야 변수값

    [SyncVar]
    public int totaActionCost; // 맵에서 소모되는 행동비용 총량
    
    [SyncVar]
    public int actionCost; // 행동시 소모되는 비용
    
    [Header("메인 카메라")]
    public Camera mainCam;

    [Header("게임 화면의 요소들의 최상위 오브젝트")]
    public GameObject game;

    [Header("맵 화면의 요소들의 최상위 오브젝트")]
    public GameObject roommaps;

    [Header("맵화면의 방 요소들의 부모 오브젝트")]
    public GameObject MapRooms;

    [Header("맵 경로 표시할 라인랜더러들의 부모 오브젝트")]
    public GameObject MapPathLines;

    [Header("그리드")]
    public GameObject hexagonGrid;

    [Header("그리드 부모 오브젝트")]
    public Transform gridParent;
    public GameObject regionIndicator;

    public readonly SyncList<Region> regions = new SyncList<Region>();

    //public readonly SyncList<Vector2> colorRegion;

    [Header("맵에서 플레이어가 컨트롤하는 오브젝트 리스트")]
    public List<GameObject> mapPlayerPieces;

    [Header("맵에서 선택된 방 정보(HEXAGON)")]
    [SerializedDictionary("NetworkIdentity", "MapRoom")]
    public SerializedDictionary<NetworkIdentity, HexagonMapRoom> playerVoteHexagonMapRoom = new SerializedDictionary<NetworkIdentity, HexagonMapRoom>();

    [Header("HexagonMapRoom 리스트")]
    public List<HexagonMapRoom> hexagonMapRooms = new List<HexagonMapRoom>();

    public const float rangeExistOtherHexagon = 0.5f; // 현재 위치한 육각형 주위에 다른 육각형이 존재하는지 확인용 중심점 간의 거리 차이 계산값
    private const float angleIncrement = 60f;  // 육각형의 각 면에 생성될 위치를 계산하기 위한 각도

    [Header("경로 표시용 스프라이트 라인랜더러 프리팹")]
    public GameObject pathLineRendererPrefab;

    [Header("검색된 경로를 표시할 스프라이트 라인랜더러 목록")]
    public List<GameObject> pathLineRenderers = new List<GameObject>();

    private static readonly Vector2Int[] offSets = {
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(-1, 1),
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(1, -1)
    };

    public HexagonMapRoom startAt;
    public HexagonMapRoom endAt;


    public static M_MapManager instance
    {
        get
        {
            if (Instance == null)
            {
                Instance = FindObjectOfType<M_MapManager>();
            }
            return Instance;
        }
    }

    public override void OnStartServer()
    {
        mapSight = 1;
        actionCost = 1;
        totaActionCost = 30;
    }

    // ------------------------------------------------------------ Server Method -------------------------------------------------------------- //

    [Server]
    public void SetDirection(HexagonMapRoom to)
    {
        moveToRoomDestination = to;
    }

    [Server]    
    public void MoveToRoom()
    {
        // 현재 위치 표시 여기서 해야함
        currentRoom = moveToRoomDestination;
        GenerateHexagonRoom(currentRoom);
        return;
    }

    [Server]
    public void PopUpOrderUI()
    {
        M_TurnManager.instance.PopUpOrderUI();
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
        GameObject centerRoom = Instantiate(
            networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "HexagonMapRoom"),
            Vector3.zero,
            Quaternion.identity
        );
        NetworkServer.Spawn(centerRoom);
        
        // 방 타입, 고유 좌표계값, 인게임 좌표계값, 활성화 상태 초기값 설정
        centerRoom.GetComponent<HexagonMapRoom>().roomType = RoomType.START_LOCATION;
        centerRoom.GetComponent<HexagonMapRoom>().coordinate = new Vector2Int(0, 0);
        centerRoom.GetComponent<HexagonMapRoom>().position = Vector3.zero;
        centerRoom.GetComponent<HexagonMapRoom>().isActive = true;
        // 육각형 위치 리스트에 추가
        hexagonMapRooms.Add(centerRoom.GetComponent<HexagonMapRoom>());
        
        // 가운데 주위에 6개 생성
        for (int i = 0; i < 6; i++)
        {
            float angle = i * angleIncrement; // 새로 생성될 육각형의 각도 계산
            Vector3 offset = Quaternion.Euler(0f, 0f, angle) * Vector3.up; // 60도씩 반시계 방향으로 육각형의 6면을 돌며 각 면의 위치에 생성
            Vector3 position = centerRoom.transform.position + offset;

            GameObject aroundRoom = Instantiate(
                networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "HexagonMapRoom"),
                position,
                Quaternion.identity
            );
            NetworkServer.Spawn(aroundRoom);

            // 방 타입 설정
            aroundRoom.GetComponent<HexagonMapRoom>().roomType = GetRoomType();
            // 인게임 좌표계 값 설정
            aroundRoom.GetComponent<HexagonMapRoom>().position = position;
            // 방 활성화 상태 설정
            aroundRoom.GetComponent<HexagonMapRoom>().isActive = true;
            // 고유 좌표계 값 설정
            SetHexagonMapRoomCoordinate(i, centerRoom.GetComponent<HexagonMapRoom>(), aroundRoom.GetComponent<HexagonMapRoom>());
            // 육각형 위치 및 오브젝트 클래스 리스트에 추가
            hexagonMapRooms.Add(aroundRoom.GetComponent<HexagonMapRoom>());
        }
    }

    // 현재 위치한 육각형의 각 변에 새로운 육각형 생성
    [Server]
    public void GenerateHexagonRoom(HexagonMapRoom currentHexagonMapRoom)
    {
        // 6개의 육각형을 생성하여 각 면에 배치
        for (int i = 0; i < 6; i++)
        {
            // 새로 생성될 육각형의 각도 계산
            float angle = i * angleIncrement;
            Vector3 offset = Quaternion.Euler(0f, 0f, angle) * Vector3.up;

            // 새로운 육각형이 생성될 위치
            Vector3 position = currentHexagonMapRoom.transform.localPosition + offset;

            if(IsPositionDuplicated(position)){
                // 위치 중복인 경우 생성하지 않지만, Region의 경우에는 거점지역이 생성할떄 육각형도 같이 생성해 두기 때문에 해당 육각형을 활성화함.
                HexagonMapRoom hexagonMapRoom = hexagonMapRooms.Find((room) => room.position == position);
                if(hexagonMapRoom.isRegion){
                    hexagonMapRoom.GetComponent<HexagonMapRoom>().isActive = true;
                }
            }else{
                // 위치 중복 아닌 경우 새로운 육각형 생성 
                var networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
                GameObject hexagonMapRoom = Instantiate(
                    networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "HexagonMapRoom"),
                    position,
                    Quaternion.identity
                );
                NetworkServer.Spawn(hexagonMapRoom);

                // 방 타입 설정
                hexagonMapRoom.GetComponent<HexagonMapRoom>().roomType = GetRoomType();
                // 인게임 좌표계 값 설정
                hexagonMapRoom.GetComponent<HexagonMapRoom>().position = position;
                // 방 활성화 상태값 설정
                hexagonMapRoom.GetComponent<HexagonMapRoom>().isActive = true;
                // 고유 좌표계 값 설정
                SetHexagonMapRoomCoordinate(i, currentHexagonMapRoom, hexagonMapRoom.GetComponent<HexagonMapRoom>());
                // 육각형 위치 및 오브젝트 클래스 리스트에 추가
                hexagonMapRooms.Add(hexagonMapRoom.GetComponent<HexagonMapRoom>());
            }
        }
    }

    // 거점지역 생성
    [Server]
    public void GenerateColorRegion()
    {
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
                int distance = Random.Range(7,11);
                float angle = Random.Range(0,2*Mathf.PI);
                centerPos.x = (int)(distance * Mathf.Cos(angle));
                centerPos.y = (int)(distance * Mathf.Sin(angle));
            }while(regions.Find(x => x.tiles.Exists(tile => tile.coordinate == centerPos)) != null);
            newRegion.tiles.Add(new Tile(centerPos));

            //각각의 타일의 위치를 정의하는 곳.
            for(int j = 0 ; j < numberOfTiles - 1 ; j++)
            {
                Vector3 newPos = MoveRandomDirection(centerPos); //랜덤 좌표 선택
                if(regions.Find(x => x.tiles.Exists(tile => tile.coordinate == newPos)) != null || newPos == new Vector3(0,0,0))
                {
                    j--;
                    continue;
                }
                newRegion.tiles.Add(new Tile(newPos));
                centerPos = newPos;
            }
        }
        GenerateHexagonRoomOnRegion(); // 거점지역 생성시 그 위치에 HexagonMapRoom오브젝트도 생성
    }

    // 생성된 거점 지역에 HexagonRoom 오브젝트 생성
    [Server]
    public void GenerateHexagonRoomOnRegion()
    {
        foreach(Region region in regions){
            foreach(Tile loc in region.tiles)
            {
                Vector3 position = GetPosition((int)loc.coordinate.x, (int)loc.coordinate.y);
                if(!IsPositionDuplicated(position)){
                    var networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
                    GameObject hexagonMapRoom = Instantiate(
                        networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "HexagonMapRoom"),
                        position,
                        Quaternion.identity
                    );
                    NetworkServer.Spawn(hexagonMapRoom);

                    // 방 타입 설정
                    hexagonMapRoom.GetComponent<HexagonMapRoom>().roomType = GetRoomType();
                    // 거점지역 데이터 설정
                    hexagonMapRoom.GetComponent<HexagonMapRoom>().region = region;
                    // 거점지역 구분 변수값 설정
                    hexagonMapRoom.GetComponent<HexagonMapRoom>().isRegion = true;
                    // 방 활성화 상태 변수값 false 설정(거점지역의 오브젝트는 그 지역에 도달하기 전까지는 비활성화 상태여야 하므로)
                    hexagonMapRoom.GetComponent<HexagonMapRoom>().isActive = false;
                    // 인게임 좌표계 값 설정
                    hexagonMapRoom.GetComponent<HexagonMapRoom>().position = position;
                    // 고유 좌표계값
                    hexagonMapRoom.GetComponent<HexagonMapRoom>().coordinate = new Vector2Int((int)loc.coordinate.x, (int)loc.coordinate.y);
                    // 육각형 위치 및 오브젝트 클래스 리스트에 추가
                    hexagonMapRooms.Add(hexagonMapRoom.GetComponent<HexagonMapRoom>());
                }
            }
        }
    }

    // HexagonMapRoom의 고유 좌표계값을 설정 : Axial 좌표계
    // (currentHexagonMapRoom을 중심으로 각 방들이 생성될 때 index값을 기반으로 각도가 정해지므로, 해당 각도에 따라 고유 좌표계값을 증감시켜 값을 설정)
    [Server]
    private void SetHexagonMapRoomCoordinate(int index, HexagonMapRoom currentHexagonMapRoom, HexagonMapRoom aroundHexagonMapRoom)
    {
        Vector2Int currentHexagonMapRoomCoordinate = currentHexagonMapRoom.GetComponent<HexagonMapRoom>().coordinate;
        switch(index)
        {
            case 0: // North
                aroundHexagonMapRoom.GetComponent<HexagonMapRoom>().coordinate = currentHexagonMapRoomCoordinate + new Vector2Int(0, -1);
                break;
            case 1: // 11시
                aroundHexagonMapRoom.GetComponent<HexagonMapRoom>().coordinate = currentHexagonMapRoomCoordinate + new Vector2Int(-1, 0);
                break;
            case 2: // 7시
                aroundHexagonMapRoom.GetComponent<HexagonMapRoom>().coordinate = currentHexagonMapRoomCoordinate + new Vector2Int(-1, 1);
                break;
            case 3: // South
                aroundHexagonMapRoom.GetComponent<HexagonMapRoom>().coordinate = currentHexagonMapRoomCoordinate + new Vector2Int(0, 1);
                break;
            case 4: // 5시
                aroundHexagonMapRoom.GetComponent<HexagonMapRoom>().coordinate = currentHexagonMapRoomCoordinate + new Vector2Int(1, 0);
                break;
            case 5: // 1시
                aroundHexagonMapRoom.GetComponent<HexagonMapRoom>().coordinate = currentHexagonMapRoomCoordinate + new Vector2Int(1, -1);
                break;
        }
    }
    

    // 방 완료상태로 변경
    [Server]
    public void SetRoomStateComplete()
    {
        if(currentRoom != null){
            currentRoom.isComplete = true;
        }
    }

    // ------------------------------------------------------------ ClientRpc Method -------------------------------------------------------------- //
    
    [ClientRpc]
    public void StartBattle()
    {
        GameUIManager.instance.FadeBlackCurtain((blackCurtain) => {
            // 카메라 위치 리셋
            Camera.main.orthographic = true;
            Camera.main.transform.position = new Vector3(0f, 0f, -10f);

            // UI 활성화 상태 변경
            roommaps.SetActive(false);
            game.SetActive(true);
            GameUIManager.instance.GameUI.gameObject.SetActive(true);
            GameUIManager.instance.GameBackGround.gameObject.SetActive(true);

            // Dim배경 상태 변경
            blackCurtain.gameObject.SetActive(false);
            blackCurtain.DOFade(0.0f, 0.5f); // 원래 알파값으로 변경

            // 검색된 경로 모두 삭제
            RemoveAllExistLineRenderer();

            // 모든 MapPlayerDestination 비활성화
            ChangeAllMapPlayerDestinationState(false);

            // 각 플레이어들의 카드와 화살표, 몬스터 오브젝트 생성 요청
            M_CardManager.instance.SpawnPlayerOwnedCardAndArrow();
            if(isServer)M_TurnManager.instance.GenerateBattleObject();
            if(isServer)M_MapManager.instance.MoveToRoom(); // 이순간에 새로운 맵 생성
        });
    }
    
    // 방이동후 카메라 전환 (자유 이동으로 할지)
    [ClientRpc]
    public void MoveCameraPositionToRoom(Vector3 pos)
    {
        mainCam.transform.position = pos + new Vector3(0,0,-10);
    }

    [ClientRpc]
    public void SetRegionWithColorRPC()
    {
        Debug.Log("Color Region Start!" + regions.Count);
        foreach(Region region in regions)
            SetRegionWithColor(region);
    }

    // ------------------------------------------------------------ Normal Method -------------------------------------------------------------- //
    
    // 가중치 랜덤 수행으로 방 타입 결정하여 반환
    private RoomType GetRoomType()
    {
        int ramdomValue = Random.Range(0,100);
        if(ramdomValue < 10) return RoomType.CAMP;
        if(ramdomValue < 30) return RoomType.EVENT;
        if(ramdomValue < 40) return RoomType.ITEM_NPC;
        if(ramdomValue < 50) return RoomType.CARD_NPC;
        if(ramdomValue < 60) return RoomType.ELITE;
        else return RoomType.MONSTER;
    }

    // 육각형 그리드(배경) 생성
    public void GenerateHexgonGrid(int maxDistance)
    {      
        Vector3 currLoc = new Vector3(0,0,0);
        for(int i = -maxDistance ; i <= maxDistance ; i ++)
        {
            if(i <= 0)
            {
                for(int j = Mathf.Abs(i) - maxDistance ; j <= maxDistance ; j++)
                {
                    currLoc = GetPosition(i,j);
                    GameObject newGrid = Instantiate(hexagonGrid,currLoc,Quaternion.identity,gridParent);
                    newGrid.transform.localPosition = newGrid.transform.position;
                    newGrid.transform.localRotation = Quaternion.Euler(0,0,0);

                    currLoc += new Vector3(0,1,0);
                }
            }
            else
            {
                for(int j = -maxDistance ; j <= maxDistance - Mathf.Abs(i) ; j++)
                {
                    currLoc = GetPosition(i,j);
                    GameObject newGrid = Instantiate(hexagonGrid,currLoc,Quaternion.identity,gridParent);
                    newGrid.transform.localPosition = newGrid.transform.position;
                    newGrid.transform.localRotation = Quaternion.Euler(0,0,0);

                    currLoc += new Vector3(0,1,0);
                }
            }
        }
    }

    public Vector3 GetPosition(int x,int y)
    {
        float length = 1/Mathf.Tan(Mathf.PI/3);
        Vector3 retVal = new Vector3(0,0,0);
        retVal.x = 1.5f*x*length;
        retVal.y = - y - ( x * 0.5f );
        return retVal;
    } 

    Vector3 MoveRandomDirection(Vector3 loc)
    {
        Vector3 retVal = loc;
        switch(Random.Range(0,6))
        {
            case 0: // North
                retVal += new Vector3(0,-1,0);
                break;
            case 1: // 1si
                retVal += new Vector3(1,-1,0);
                break;
            case 2: // 5si
                retVal += new Vector3(1,0,0);
                break;
            case 3: // South
                retVal += new Vector3(0,1,0);
                break;
            case 4: // 7si
                retVal += new Vector3(-1,1,0);
                break;
            case 5: // 11si
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
                    case 0 : // North
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x,loc.coordinate.y-1,0)))
                            continue;
                        break;
                    case 1 : // 2시
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x+1,loc.coordinate.y-1,0)))
                            continue;
                        break;
                    case 2 : // 5시
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x+1,loc.coordinate.y,0)))
                            continue;
                        break;
                    case 3 :// 6시
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x,loc.coordinate.y+1,0)))
                            continue;
                        break;
                    case 4 :// 7시
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x-1,loc.coordinate.y+1,0)))
                            continue;
                        break;
                    case 5 :// 10시
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x-1,loc.coordinate.y,0)))
                            continue;
                        break;
                }
                GameObject newRegion = Instantiate(regionIndicator,GetPosition((int)loc.coordinate.x,(int)loc.coordinate.y),Quaternion.identity,gridParent);
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
                newRegion.transform.localRotation = Quaternion.Euler(0,0,-60*i);
            }
        }
    }

    // Axial 좌표계를 이용한 시스템에서 현재 좌표에서 목표 좌표까지의 거리를 반환
    public int GetDistanceFromCurrentCoordinate(Vector2Int targetCoordinate)
    {
        if(currentRoom != null){
            int dQ = targetCoordinate.x - currentRoom.coordinate.x;
            int dR = targetCoordinate.y - currentRoom.coordinate.y;
            int distance = (Mathf.Abs(dQ) + Mathf.Abs(dR) + Mathf.Abs(dQ + dR)) / 2;
            return distance;
        }
        return 0;
    }

    // 육각형 방 생성하려는 위치에 이미 방이 존재하는지 체크
    private bool IsPositionDuplicated(Vector3 spawnPosition)
    {
        // 육각형 방 리스트를 순회하며 위치 중복 체크
        foreach(HexagonMapRoom hexagonMapRoom in hexagonMapRooms)
        {
            Vector3 position = hexagonMapRoom.position;
            if(Vector3.Distance(spawnPosition, position) < rangeExistOtherHexagon)
            {
                return true; // 중복된 위치가 있음
            }
        }
        return false; // 중복된 위치가 없음
    }

    // ------------------------------------------------------------ A* Algorithm Method -------------------------------------------------------------- //

    // A* 알고리즘을 이용한 경로검색
    public List<HexagonMapRoom> FindPath(HexagonMapRoom start, HexagonMapRoom target)
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
            if(currentNode.coordinate == target.coordinate)
            {
                return CreatePath(start, currentNode);
            }

            List<HexagonMapRoom> neighbours = GetNeighbours(currentNode); // 현재 노드의 주변 이웃 노드 조회
            foreach (HexagonMapRoom neighbour in neighbours)
            {
                if(closedSet.Contains(neighbour)) // 이웃노드가 방문한 목록에 있으면 그대로 진행
                    continue;

                // Cost값 갱신
                int newGCost = currentNode.GCost + 1;
                int newHCost = CalculateHeuristics(neighbour.coordinate, target.coordinate);
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
    private List<HexagonMapRoom> GetNeighbours(HexagonMapRoom currentHexagonRoom)
    {
        List<HexagonMapRoom> neighbours = new List<HexagonMapRoom>();
        for(int i = 0; i < 6; i++)
        {
            HexagonMapRoom neighbour = hexagonMapRooms.Find((room) => room.coordinate == currentHexagonRoom.coordinate + offSets[i]);
            if(neighbour != null && neighbour.isComplete){ // 완료된 방의 경우에만 경로 탐색 가능
                neighbours.Add(neighbour);
            }
        }
        return neighbours;
    }

    // [휴리스틱함수]
    // A* 알고리즘에서 목적지까지 가는데 걸리는 비용 예측 함수. 해당함수의 로직에 따라 알고리즘 효율성 결정.
    // 현재는 단순 두 지점간의 좌표계에 따른 거리값만 계산(Axial 좌표계에서의 맨해튼거리 계산식)
    private int CalculateHeuristics(Vector2Int currentCoord, Vector2Int targetCoord)
    {
        int dQ = currentCoord.x - targetCoord.x;
        int dR = currentCoord.y - targetCoord.y;
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
        return path;
    }

    // netId에 해당하는 유저의 경로 랜더링
    public void RenderVisualizePath(List<HexagonMapRoom> findPath, uint netId, MapPlayerDestination currentMapPlayerDestination)
    {
        for(int i=0; i<findPath.Count-1; i++)
        {
            GameObject pathLineRenderer = Instantiate(pathLineRendererPrefab, Vector3.zero, Quaternion.identity);
            PathLineRenderer pathLineRendererComponent = pathLineRenderer.GetComponent<PathLineRenderer>();
            SpriteRenderer spriteRenderer = pathLineRenderer.GetComponent<SpriteRenderer>();

            spriteRenderer.color = currentMapPlayerDestination.GetComponent<SpriteRenderer>().color; // 경로표시 색상은 MapPlayerDestination과 동일한 색으로 설정

            pathLineRendererComponent.netId = netId;
            pathLineRendererComponent.rotationZ = GetAngleFromCoordinate(findPath[i].coordinate, findPath[i + 1].coordinate);

            // 선의 방향과 길이 계산
            Vector3 startPos = findPath[i].transform.position;
            Vector3 endPos = findPath[i + 1].transform.position;
            Vector3 direction = endPos - startPos;

            // 선의 중심 위치 계산
            Vector3 centerPoint = (startPos + endPos) / 2f;

            pathLineRenderer.transform.position = centerPoint;
            pathLineRenderers.Add(pathLineRenderer);
        }
    }

    // 고유좌표계를 이용해 시작점과 끝점 사이의 각도 뱐환
    public float GetAngleFromCoordinate(Vector2Int start, Vector2Int end)
    {
        Vector2Int offset = end - start;
        if (offset == new Vector2Int(0, -1))
            return 90f;  // 북
        else if (offset == new Vector2Int(-1, 0))
            return -30f; // 11시
        else if (offset == new Vector2Int(-1, 1))
            return 30f;  // 7시
        else if (offset == new Vector2Int(0, 1))
            return -90f; // 남
        else if (offset == new Vector2Int(1, 0))
            return -30f; // 5시
        else if (offset == new Vector2Int(1, -1))
            return 30f;  // 1시
        else
            return 0f;
    }

    // netId에 해당하는 유저의 기존 랜더링된 경로 삭제
    public void RemoveExistLineRenderer(uint netId)
    {
        for(int i=pathLineRenderers.Count-1; i>=0; i--){
            if(pathLineRenderers[i].GetComponent<PathLineRenderer>().netId == netId){
                Destroy(M_MapManager.instance.pathLineRenderers[i]);
                pathLineRenderers.RemoveAt(i);
            }
        }
    }

    // 랜더링된 경로 모두 삭제
    public void RemoveAllExistLineRenderer()
    {
        for(int i=pathLineRenderers.Count-1; i>=0; i--){
            Destroy(M_MapManager.instance.pathLineRenderers[i]);
            pathLineRenderers.RemoveAt(i);
        }
    }

    // MapPlayerDestination 모두 상태 변경
    private void ChangeAllMapPlayerDestinationState(bool isActive)
    {
        foreach(GamePlayer gamePlayer in M_TurnManager.instance.playerOrder)
        {
            GamePlayerMap gamePlayerMap = gamePlayer.GetComponent<GamePlayerMap>();
            MapPlayerDestination mapPlayerDestination = gamePlayerMap.currentMapPlayerDestination;
            mapPlayerDestination.gameObject.SetActive(isActive);
        }
    }
}
