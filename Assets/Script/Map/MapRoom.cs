using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;
using TMPro;

public class MapRoom : NetworkBehaviour
{
    [SyncVar]
    public Vector2 location;
    
    [SyncVar]
    public int hazard;
    
    [SyncVar]
    public bool isComplete = false;

    [SyncVar (hook = nameof(OnChangedRoomType))]
    public RoomType roomType = RoomType.UNDEFINED;
    
    SpriteRenderer testSprite;

    public List<Sprite> MapMarkers;

    Camera mainCamera;

    void Awake()
    {
        testSprite = GetComponent<SpriteRenderer>();
        gameObject.transform.SetParent(Floor.instance.transform);
        mainCamera = Camera.main;
    }

    void OnMouseDown()
    {
        // 맵은 상하좌우 한칸씩만 이동가능
        if(Vector2.Distance(location, M_MapManager.instance.currentRoom.location) <= 1.2f && roomType != RoomType.START_LOCATION){
            Debug.Log(" 클릭 : " + location + " / " + M_MapManager.instance.currentRoom);
            NetworkClient.localPlayer.GetComponent<GamePlayer>().destination = location;
            NetworkClient.localPlayer.GetComponent<GamePlayerMap>().CmdSelectMapRoom(this, NetworkClient.connection.identity);
            NetworkClient.localPlayer.GetComponent<GamePlayerMap>().CmdChangeCurrentMapPlayerPosition(this, GetComponent<Transform>().position);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        OnChangedRoomType(RoomType.START_LOCATION,RoomType.MONSTER);
    }

    void OnChangedRoomType(RoomType oldVal, RoomType newVal)
    {
        switch(roomType)
        {
            case RoomType.START_LOCATION :
                testSprite.sprite = MapMarkers[0];
                break;
            case RoomType.MONSTER :
                testSprite.sprite = MapMarkers[1];
                break;
            case RoomType.ELITE :
                testSprite.sprite = MapMarkers[2];
                break;
            case RoomType.EVENT :
                testSprite.sprite = MapMarkers[3];
                break;
            case RoomType.CAMP :
                testSprite.sprite = MapMarkers[4];
                break;
            case RoomType.ITEM_NPC :
                testSprite.sprite = MapMarkers[5];
                break;
            case RoomType.CARD_NPC :
                testSprite.sprite = MapMarkers[6];
                break;
        }
    }


}
