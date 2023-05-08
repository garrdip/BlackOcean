using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;

public class MapRoom : NetworkBehaviour
{
    [SyncVar]
    public Vector2 location;
    
    [SyncVar]
    public int hazard;
    
    [SyncVar]
    public bool isComplete = false;

    [SyncVar]
    public RoomType roomType;
    
    SpriteRenderer testSprite;

    Camera mainCamera;

    void Awake()
    {
        testSprite = GetComponent<SpriteRenderer>();
        gameObject.transform.SetParent(Floor.instance.transform);
        mainCamera = Camera.main;
    }

    void  OnMouseDown()
    {
        // 맵은 상하좌우 한칸씩만 이동가능
        if(Vector2.Distance(location, M_MapManager.instance.currentLocation) <= 1f){
            Debug.Log(" 클릭 : " + location + " / " + M_MapManager.instance.currentLocation);
            NetworkClient.localPlayer.GetComponent<GamePlayer>().destination = location;
            NetworkClient.localPlayer.GetComponent<GamePlayerMap>().CmdSelectMapRoom(this, NetworkClient.connection.identity);
            NetworkClient.localPlayer.GetComponent<GamePlayerMap>().CmdChangeCurrentMapPlayerPosition(this, GetComponent<Transform>().position);
        }
    }


    [ClientRpc]
    public void SetSprite(Color color)
    {
        testSprite.color = color;
    }

}
