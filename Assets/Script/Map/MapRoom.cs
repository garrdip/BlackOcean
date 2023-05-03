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
        if( Vector2.Distance(location,M_MapManager.instance.currentLocation) == 1f )
            NetworkClient.localPlayer.GetComponent<GamePlayer>().destination = location;
            NetworkClient.localPlayer.GetComponent<GamePlayerMap>().CmdChangeCurrentMapPlayerPosition(location);
    }


    [ClientRpc]
    public void SetSprite(Color color)
    {
        testSprite.color = color;
    }

}
