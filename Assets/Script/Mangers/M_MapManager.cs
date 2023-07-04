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
    public MapRoom currentRoom;
   
    [SyncVar]
    public int turnsLeft = 10;
    
    [Header("Main Camera")]
    public Camera mainCam;

    //방정보는 서버만 관리 (No SyncVar)
    [Header("Room List")]
    public List<MapRoom> rooms = new List<MapRoom>();

    // 맵 UI에 사용될 Gameplayer를 참조하는 커스텀 캐릭터 프리팹
    [Header("MapPlayerForUI Prefab")]
    public GameObject mapPlayerForUI;

    [Header("Map Scene")]
    public GameObject roommaps;

    [Header("Game Scene")]
    public GameObject game;

    [Header("Map Player List")]
    public List<GameObject> mapPlayerPieces;

    [Header("Map Player Select MapRoom")]
    [SerializedDictionary("NetworkIdentity", "MapRoom")]
    public SerializedDictionary<NetworkIdentity, MapRoom> playerVoteMapRoom = new SerializedDictionary<NetworkIdentity, MapRoom>();

    [SyncVar]
    MapRoom moveToRoomDestination;

    [SyncVar]
    MapRoom moveToRoomFrom;

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

    [Server]
    public void GenerateFloor()
    {
        //싱글톤 네트워크 매니저
        var netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        //최초 방 좌표
        Vector2[] loc = {new Vector2(0,0),new Vector2(1,0),new Vector2(-1,0),new Vector2(0,1),new Vector2(0,-1)};
        //모든 방 정보 삭제
        for(int i = 0; i < rooms.Count ; i++)
        {
            MapRoom destroyRoom = rooms[i];
            rooms.Remove(destroyRoom);
            NetworkServer.Destroy(destroyRoom.gameObject);
        }
        //새로운 방 5개 생성 
        for(int i = 0 ;i < 5 ;i ++)
        {
            //각 방의 좌표값 * 1.2 위치에 방 생성 (방간격)
            GameObject newRoom = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "MapRoom"),new Vector3(loc[i].x*1.2f,loc[i].y*1.2f,0),Quaternion.Euler(40,0,0),roommaps.transform);
            newRoom.transform.localPosition = new Vector3(loc[i].x*1.2f,loc[i].y*1.2f,0);
            newRoom.GetComponent<MapRoom>().location = loc[i];
            // Vector2 형식의 좌표 절대값의 합을 위험도로 지정 
            if( i == 0 ) newRoom.GetComponent<MapRoom>().roomType = RoomType.START_LOCATION;
            else newRoom.GetComponent<MapRoom>().roomType = GetRoomType();
            newRoom.GetComponent<MapRoom>().hazard = (int)Mathf.Abs(loc[i].x) + (int)Mathf.Abs(loc[i].y);
            NetworkServer.Spawn(newRoom);
            rooms.Add(newRoom.GetComponent<MapRoom>());
            if( i == 0) currentRoom = newRoom.GetComponent<MapRoom>();
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

    public Vector3 GetMapCameraLocation()
    {
        return currentRoom.transform.position;
    }

    [Server]
    public void SetDirection(MapRoom to)
    {
        moveToRoomDestination = to;
    }

    [Server]    
    public void MoveToRoom()
    {
        // 현재 위치 표시 여기서 해야함
        currentRoom = moveToRoomDestination;
        GenerateNextRoom();
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

    // East/West/South/North 방이 있는지 검색하고 없으면 생성 - for문이 쥰내 들어감 괜찮은지
    [Server]
    public void GenerateNextRoom()
    {
        Debug.Log("Generate Room");
        var netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        Vector2[] loc = {new Vector2(1,0),new Vector2(-1,0),new Vector2(0,1),new Vector2(0,-1)};
        for(int i = 0 ;i < 4 ;i ++)
        {
            bool isEmpty = true;
            foreach(MapRoom room in rooms)
            {
                if(room.location == (currentRoom.location + loc[i]))
                {
                    isEmpty = false;
                    break;
                }
            }
            if(isEmpty)
            {
                GameObject newRoom = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "MapRoom"),new Vector3((currentRoom.location.x + loc[i].x)*1.2f,(currentRoom.location.y + loc[i].y)*1.2f,0),Quaternion.Euler(40,0,0));
                newRoom.transform.localPosition = new Vector3((currentRoom.location.x + loc[i].x)*1.2f,(currentRoom.location.y + loc[i].y)*1.2f,0);
                newRoom.GetComponent<MapRoom>().location = new Vector2(currentRoom.location.x + loc[i].x, currentRoom.location.y + loc[i].y);
                newRoom.GetComponent<MapRoom>().hazard = (int)Mathf.Abs(currentRoom.location.x + loc[i].x) + (int)Mathf.Abs(currentRoom.location.y + loc[i].y);
                NetworkServer.Spawn(newRoom);
                newRoom.GetComponent<MapRoom>().roomType = GetRoomType();
                rooms.Add(newRoom.GetComponent<MapRoom>());
            }
        }
    }

    // 맵 플레이어들이 선택한 방 선택지 확인
    // 1. 중복값이 있다는것은 2명 이상이 해당 방을 선택한 것이며, 과반수 이상 이므로 해당 MapRoom 반환
    // 2. 중복값이 없다는 것은 모두 다른 선택을 한 것이므로 랜덤으로 돌려서 선택된 MapRoom 반환
    [Server]
    public MapRoom GetVoteMapRoomResult()
    {
        List<MapRoom> mapRooms = new List<MapRoom>();
        foreach (MapRoom mapRoom in playerVoteMapRoom.Values)
        {
            if(!mapRooms.Contains(mapRoom)){
                mapRooms.Add(mapRoom);
            }else{
                return mapRoom; // 중복 선택된 방
            }
        }
        if(mapRooms.Count > 0){
            int randomIndex = Random.Range(0, mapRooms.Count); // 선택된 방들 중에 랜덤
            return mapRooms[randomIndex];
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
}
