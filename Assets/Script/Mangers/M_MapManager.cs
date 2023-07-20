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
    
    [Header("메인 카메라")]
    public Camera mainCam;

    [Header("게임 화면의 요소들의 최상위 오브젝트")]
    public GameObject game;

    [Header("맵 화면의 요소들의 최상위 오브젝트")]
    public GameObject roommaps;

    [Header("맵화면의 방 요소들의 부모 오브젝트")]
    public GameObject MapRooms;

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

    public const float rangeExistOtherHexagon = 0.5f; // 현재 위치한 육각형 주위에 다른 육각형이 존재하는지 확인용 중심점 간의 거리 차이 계산값
    private const float angleIncrement = 60f;  // 육각형의 각 면에 생성될 위치를 계산하기 위한 각도
    public List<Vector3> hexagonPositions = new List<Vector3>(); // 각 육각형 방의 위치 리스트

    // 거점 지역 등급
    public RegionGrade[] regionGrades = { RegionGrade.NORMAL, RegionGrade.RARE, RegionGrade.UNIQUE, RegionGrade.LEGEND };
    public float[] weight = { 0.6f, 0.25f, 0.13f, 0.02f }; // 확률 가중치(60%, 25%, 13%, 2%)

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
        GenerateHexagonRoom(currentRoom.transform.localPosition);
        return;
    }

    [Server]
    public void PopUpOrderUI()
    {
        M_TurnManager.instance.PopUpOrderUI();
    }

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

            // 각 플레이어들의 카드와 화살표, 몬스터 오브젝트 생성 요청
            M_CardManager.instance.SpawnPlayerOwnedCardAndArrow();
            if(isServer)M_TurnManager.instance.GenerateBattleObject();
            if(isServer)M_MapManager.instance.MoveToRoom(); // 이순간에 새로운 맵 생성
        });
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


    // 방이동후 카메라 전환 (자유 이동으로 할지)
    [ClientRpc]
    public void MoveCameraPositionToRoom(Vector3 pos)
    {
        mainCam.transform.position = pos + new Vector3(0,0,-10);
    }


    // 처음 시작시 가운데 1개, 각 변에 6개 생성
    [Server]
    public void GenerateStartHexagonRoom()
    {
        var networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        
        // 가운데 육각형 생성
        GameObject centerRoom = Instantiate(
            networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "HexagonMapRoom"),
            new Vector3(0f, 0f, 0f),
            Quaternion.identity
        );
        NetworkServer.Spawn(centerRoom);

        // 육각형 위치 리스트에 추가
        hexagonPositions.Add(new Vector3(0f, 0f, 0f));
        
        // 가운데 주위에 6개 생성
        for (int i = 0; i < 6; i++)
        {
            float angle = i * angleIncrement; // 새로 생성될 육각형의 각도 계산
            Vector3 offset = Quaternion.Euler(0f, 0f, angle) * Vector3.up; // 60도씩 육각형의 6면을 돌며 각 면의 위치에 생성
            Vector3 position = centerRoom.transform.position + offset;

            // 새로운 육각형 생성
            GameObject aroundRoom = Instantiate(
                networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "HexagonMapRoom"),
                position,
                Quaternion.identity
            );
            NetworkServer.Spawn(aroundRoom);
            aroundRoom.GetComponent<HexagonMapRoom>().roomType = GetRoomType();

            // 육각형 위치 리스트에 추가
            hexagonPositions.Add(position);
        }
    }
    
    public void GenerateHexgonGrid(int width, int height)
    {      
        int widthIn = (width % 4 == 1)?width : (width/4)*4 + 1;
        int heightIn = (height % 2 == 1)?height : (height/2)*2 + 1;
        Vector3 currLoc = new Vector3(0,0,0);
        for(int i = 0 ; i < widthIn ; i ++)
        {
            currLoc = GetPosition(-widthIn/2 + i,-heightIn/2);
            for(int j = 0 ; j < heightIn ; j++)
            {
                GameObject newGrid = Instantiate(hexagonGrid,currLoc,Quaternion.identity,gridParent);
                newGrid.transform.localPosition = newGrid.transform.position;
                newGrid.transform.localRotation = Quaternion.Euler(0,0,0);

                currLoc += new Vector3(0,1,0);
            }
        }
    }

    public Vector3 GetPosition(int x,int y)
    {
        float length = 1/Mathf.Tan(Mathf.PI/3);
        Vector3 retVal = new Vector3(0,0,0);
        retVal.x = 1.5f*x*length;
        retVal.y = y - (Mathf.Abs(x)%2)*0.5f;
        return retVal;
    }

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
            SetRegionWithColor(newRegion);
        }
    }

    Vector3 MoveRandomDirection(Vector3 loc)
    {
        Vector3 retVal = loc;
        int addVal = (loc.x%2 == 0)? 0 : -1;
        switch(Random.Range(0,6))
        {
            case 0: // North
                retVal += new Vector3(0,1,0);
                break;
            case 1: // 1si
                retVal += new Vector3(1,1+addVal,0);
                break;
            case 2: // 5si
                retVal += new Vector3(1,addVal,0);
                break;
            case 3: // South
                retVal += new Vector3(0,-1,0);
                break;
            case 4: // 7si
                retVal += new Vector3(-1,addVal,0);
                break;
            case 5: // 11si
                retVal += new Vector3(-1,addVal+1,0);
                break;
        }
        return retVal;
    }

    public void SetRegionWithColor(Region region)
    {
        foreach(Tile loc in region.tiles)
        {
            Debug.Log(loc.coordinate);
            int addVal = (Mathf.Abs(loc.coordinate.x)%2 == 0)? 0 : -1; // X의 홀수축은 짝수축보다 아래에 위치
            for(int i = 0;  i < 6 ; i ++)
            {
                switch(i)
                {
                    case 0 : // North
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x,loc.coordinate.y+1,0)))
                            continue;
                        break;
                    case 1 : // 2시
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x+1,loc.coordinate.y+1+addVal,0)))
                            continue;
                        break;
                    case 2 : // 5시
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x+1,loc.coordinate.y+addVal,0)))
                            continue;
                        break;
                    case 3 :// 6시
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x,loc.coordinate.y-1,0)))
                            continue;
                        break;
                    case 4 :// 7시
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x-1,loc.coordinate.y+addVal,0)))
                            continue;
                        break;
                    case 5 :// 10시
                        if(region.tiles.Exists(pos => pos.coordinate == new Vector3(loc.coordinate.x-1,loc.coordinate.y+1+addVal,0)))
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

    // 현재 위치한 육각형의 각 변에 새로운 육각형 생성, 이미 존재하면 생성하지 않음
    [Server]
    public void GenerateHexagonRoom(Vector3 hexagonPosition)
    {
        // 6개의 육각형을 생성하여 각 면에 배치
        for (int i = 0; i < 6; i++)
        {
            // 새로 생성될 육각형의 각도 계산
            float angle = i * angleIncrement;
            Vector3 offset = Quaternion.Euler(0f, 0f, angle) * Vector3.up;

            // 새로운 육각형이 생성될 위치
            Vector3 position = hexagonPosition + offset;

            // 생성 위치가 중복이 아닌 경우 육각형 생성
            if(!IsPositionDuplicated(position))
            {
                // 새로운 육각형 생성
                var networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
                GameObject hexagonMapRoom = Instantiate(
                    networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "HexagonMapRoom"),
                    position,
                    Quaternion.identity
                );
                NetworkServer.Spawn(hexagonMapRoom);
                hexagonMapRoom.GetComponent<HexagonMapRoom>().roomType = GetRoomType();

                // 육각형 위치 리스트에 추가
                hexagonPositions.Add(position);
            }
        }
    }

    // 랜덤 거점지역 생성
    [Server]
    public void GenerateHexagonRegion()
    {
        var networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        int regionCount = Random.Range(2, 5); // 몇개의 지역이 생성될지 랜덤

        // 랜덤 갯수만큼 거점지역 생성
        for (int i = 0; i < regionCount; i++)
        {
            Debug.Log("지역갯수 : " + regionCount);
            int ranomDistance = Random.Range(5, 10); // 얼마나 떨어진 위치에 생성될지 랜덤
            float randomDirection = (Random.Range(0, 7) * angleIncrement); // 6방향중 어느 방향일지 랜덤(추후 방향을 전환하도록 바꾸면 랜덤성 증가)
            int randomRoomCount = Random.Range(5, 15); // 거점지역에 몇개의 육각형이 생성될지 랜덤
            
            Vector3 position = Quaternion.Euler(0f, 0f, randomDirection) * (Vector3.up * ranomDistance); // 랜덤방향 + 랜덤거리
            RegionGrade regionGrade = GenerateRandomGrade(); // 거짐지역의 등급 랜덤으로 결정
        
            Debug.Log("육각형갯수 : " + (randomRoomCount) + " // " + regionGrade);
            for(int j = 0; j < randomRoomCount; j++)
            {
                position += Quaternion.Euler(0f, 0f, (Random.Range(-7, 7) * angleIncrement)) * Vector3.up; // 생성된 거점지역의 육각형에 생성 위치 + 방향값 랜덤성 추가
                if(IsPositionDuplicated(position)){
                    Debug.Log("겹침 위치바꿔");
                    position += Vector3.up;
                }

                // 새로운 육각형 생성
                GameObject hexagonMapRoom = Instantiate(networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "HexagonMapRoom"), position, Quaternion.identity);
                hexagonMapRoom.transform.SetParent(hexagonMapRoom.transform);

                NetworkServer.Spawn(hexagonMapRoom);

                
                // 육각형 위치 리스트에 추가
                hexagonPositions.Add(hexagonMapRoom.transform.position);

            }
        }
    }

    // 가중치 랜덤을 사용한 랜덤등급 산출 함수
    private RegionGrade GenerateRandomGrade()
    {
        float randomValue = Random.Range(0.0f, 1.0f); // 0.0 ~ 1.0 사이의 랜덤한 값 얻기
        float cumulativeWeight = 0f; // 누적 가중치

        for (int i = 0; i < weight.Length; i++)
        {
            cumulativeWeight += weight[i];
            if (randomValue <= cumulativeWeight)
            {
                switch (i)
                {
                    case 0:
                        return RegionGrade.NORMAL;
                    case 1:
                        return RegionGrade.RARE;
                    case 2:
                        return RegionGrade.UNIQUE;
                    case 3:
                        return RegionGrade.LEGEND;
                    default:
                        break;
                }
            }
        }
        return RegionGrade.NONE; 
    }

    // 육각형 방 생성하려는 위치에 이미 방이 존재하는지 체크
    private bool IsPositionDuplicated(Vector3 spawnPosition)
    {
        // 육각형 위치 리스트를 순회하며 중복 체크
        foreach(Vector3 position in hexagonPositions)
        {
            if(Vector3.Distance(spawnPosition, position) < rangeExistOtherHexagon)
            {
                return true; // 중복된 위치가 있음
            }
        }
        return false; // 중복된 위치가 없음
    }
}
