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
        Debug.Log(" 클릭 !");
        // TODO : 이전에 선택 혹은 버려진 맵들은 선택되지 않도록 해야함
        if( Vector2.Distance(location,M_MapManager.instance.currentLocation) == 1f )
            NetworkClient.localPlayer.GetComponent<GamePlayer>().destination = location;
            NetworkClient.localPlayer.GetComponent<GamePlayerMap>().CmdSelectMapRoom(this, NetworkClient.connection.identity);
            NetworkClient.localPlayer.GetComponent<GamePlayerMap>().CmdChangeCurrentMapPlayerPosition(GetComponent<Transform>().position);
    }


    [ClientRpc]
    public void SetSprite(Color color)
    {
        testSprite.color = color;
    }

}
