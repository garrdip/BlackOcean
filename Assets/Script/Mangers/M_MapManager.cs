using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class M_MapManager : NetworkBehaviour
{  
    public static M_MapManager Instance = null; 
    
    [SyncVar]
    public Vector2 currentLocation = new Vector2(0,0);
    
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
        foreach(MapRoom room in rooms)
        {
             NetworkServer.Destroy(room.gameObject);
        }
        //새로운 방 5개 생성 
        for(int i = 0 ;i < 5 ;i ++)
        {
            //각 방의 좌표값 * 1.2 위치에 방 생성 (방간격)
            GameObject newRoom = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "MapRoom"),new Vector3(loc[i].x*1.2f,loc[i].y*1.2f,0),Quaternion.Euler(40,0,0),roommaps.transform);
            newRoom.transform.localPosition = new Vector3(loc[i].x*1.2f,loc[i].y*1.2f,0);
            newRoom.GetComponent<MapRoom>().location = loc[i];
            // Vector2 형식의 좌표 절대값의 합을 위험도로 지정 
            newRoom.GetComponent<MapRoom>().hazard = (int)Mathf.Abs(loc[i].x) + (int)Mathf.Abs(loc[i].y);
            NetworkServer.Spawn(newRoom);
            rooms.Add(newRoom.GetComponent<MapRoom>());
        }
    }

    [Server]
    public void MoveToRoom(Vector2 tar, Vector3 pos)
    {
        if(Vector2.Distance(currentLocation,tar) > 1 || M_TurnManager.instance.isOrderSelect)
        {
            return;
        }
        else
        {
            PopUpOrderUI();
            SetRoomColor(tar);
            currentLocation = tar;
            GenerateNextRoom();
            MoveCameraPositionToRoom(pos);
            return;
        }
    }

    [Server]
    public void PopUpOrderUI()
    {
        M_TurnManager.instance.PopUpOrderUI();
    }

    [Server]
    public void SetRoomColor(Vector2 tar)
    {
        //현재 위치의 방 파란색으로 변경
        foreach(MapRoom room in rooms)
        {
            if(room.location == currentLocation)
            {
               room.SetSprite(new Color(1,0,0));
            }
        }
        //기존 방 빨간색으로 변경 
        foreach(MapRoom room in rooms)
        {
            if(room.location == tar)
            {
                room.SetSprite(new Color(0,0,1));
            }
        }
    }

    [ClientRpc]
    public void StartBattle()
    {
        roommaps.SetActive(false);
        game.SetActive(true);
        DeckUI.instance.GameUI.gameObject.SetActive(true);
        Camera.main.orthographic = true;
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
                if(room.location == (currentLocation + loc[i]))
                {
                    isEmpty = false;
                    break;
                }
            }
            if(isEmpty)
            {
                GameObject newRoom = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "MapRoom"),new Vector3((currentLocation.x + loc[i].x)*1.2f,(currentLocation.y + loc[i].y)*1.2f,0),Quaternion.Euler(40,0,0));
                newRoom.transform.localPosition = new Vector3((currentLocation.x + loc[i].x)*1.2f,(currentLocation.y + loc[i].y)*1.2f,0);
                newRoom.GetComponent<MapRoom>().location = new Vector2(currentLocation.x + loc[i].x, currentLocation.y + loc[i].y);
                newRoom.GetComponent<MapRoom>().hazard = (int)Mathf.Abs(currentLocation.x + loc[i].x) + (int)Mathf.Abs(currentLocation.y + loc[i].y);
                NetworkServer.Spawn(newRoom);
                rooms.Add(newRoom.GetComponent<MapRoom>());
            }
        }
    }

    // 방이동후 카메라 전환 (자유 이동으로 할지)
    [ClientRpc]
    public void MoveCameraPositionToRoom(Vector3 pos)
    {
        mainCam.transform.position = pos + new Vector3(0,0,-10);
    }
}
