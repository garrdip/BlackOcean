using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class M_MapManager : NetworkBehaviour
{  
    public static M_MapManager Instance = null; 
    [SyncVar]
    public Vector2 currentLocation = new Vector2(0,0);
    public Transform floors;
    public GameObject mapRoom;
    public Camera mainCam;
    //방정보는 서버만 관리 (No SyncVar)
    public List<MapRoom> rooms = new List<MapRoom>();
    public int number = 0;

    public static M_MapManager instance
    {
        get
        {
            // 객체 직접 생성 대신 게임오브젝트&컴포넌트 생성
            if (Instance == null)
            {
                // 씬 전체에서 탐색
                Instance = FindObjectOfType<M_MapManager>();

                // 그래도 없는 경우, 새로 생성
                if (Instance == null)
                {
                    GameObject container = new GameObject("M_MapManager");
                    Instance = container.AddComponent<M_MapManager>();
                }
            }
            return Instance;
        }
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    [Server]
    public void GenerateFloor()
    {
        var netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        //최초 방 좌표
        (int,int)[] loc = {(0,0),(1,0),(-1,0),(0,1),(0,-1)};
        //모든 방 정보 삭제
        foreach(MapRoom room in rooms)
        {
             NetworkServer.Destroy(room.gameObject);
        }
        //새로운 방 5개 생성 
        for(int i = 0 ;i < 5 ;i ++)
        {
            GameObject newRoom = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "MapRoom"),new Vector3(loc[i].Item1,loc[i].Item2,0),Quaternion.identity);
            newRoom.GetComponent<MapRoom>().number = number;
            number++;
            NetworkServer.Spawn(newRoom);
            rooms.Add(newRoom.GetComponent<MapRoom>());
        }
    }

}
