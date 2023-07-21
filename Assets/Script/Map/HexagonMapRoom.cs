using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using TMPro;

public class HexagonMapRoom : NetworkBehaviour
{
    [SyncVar (hook = nameof(OnChangedRoomType))]
    public RoomType roomType = RoomType.UNDEFINED;

    public SpriteRenderer spriteRenderer;
    public TextMeshProUGUI textRoomType;

    void Start()
    {
       transform.SetParent(M_MapManager.instance.MapRooms.transform);
       transform.localPosition = new Vector3(transform.position.x, transform.position.y, 0f);
       transform.localRotation = Quaternion.Euler(0, 0f, 0f);
    }

    private void OnMouseDown()
    {
        // 클릭한 육각형으로 맵플레이어 이동 및 현재 선택된 맵으로 저장
        NetworkClient.localPlayer.GetComponent<GamePlayerMap>().CmdSelectHexagonMapRoom(this, NetworkClient.connection.identity);
        // 맵 플레이어가 이동할 방에 표시
        NetworkClient.localPlayer.GetComponent<GamePlayerMap>().CmdChangeMapPlayerDestinationPosition(this, GetComponent<Transform>().position);
    }

    void OnChangedRoomType(RoomType oldVal, RoomType newVal)
    {
        switch(roomType)
        {
            case RoomType.START_LOCATION :
                textRoomType.color = Color.gray;
                textRoomType.text = "시작";
                break;
            case RoomType.MONSTER :
                textRoomType.color = Color.red;
                textRoomType.text = "몬스터";
                break;
            case RoomType.ELITE :
                textRoomType.color = Color.red;
                textRoomType.text = "엘리트";
                break;
            case RoomType.EVENT :
                textRoomType.color = Color.yellow;
                textRoomType.text = "이벤트";
                break;
            case RoomType.CAMP :
                textRoomType.color = Color.green;
                textRoomType.text = "캠프";
                break;
            case RoomType.ITEM_NPC :
                textRoomType.color = Color.blue;
                textRoomType.text = "아이템";
                break;
            case RoomType.CARD_NPC :
                textRoomType.color = Color.magenta;
                textRoomType.text = "엔피씨";
                break;
        }
    }
}
