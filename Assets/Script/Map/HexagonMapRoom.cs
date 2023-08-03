using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using TMPro;

public class HexagonMapRoom : NetworkBehaviour
{
    [SyncVar (hook = nameof(OnChangedRoomType))]
    public RoomType roomType = RoomType.UNDEFINED; // 방 타입

    [SyncVar (hook = nameof(OnChangedCoordinate))]
    public Vector2Int coordinate; // 각 방의 고유 좌표계 값

    [SyncVar]
    public Vector3 position; // 인게임 좌표계 값

    [SyncVar (hook = nameof(OnChangedIsRegion))]
    public bool isRegion = false; // 거점지역 구분값

    [SyncVar]
    public Region region;

    [SyncVar (hook = nameof(OnChangedIsActive))]
    public bool isActive = false; // 방 활성화 상태 구분값

    [SyncVar (hook = nameof(OnChangedIsComplete))]
    public bool isComplete = false; // 방 정복 완료 상태 구분값

    [Header("A* 알고리즘에 사용되는 멤버 변수들")]
    public HexagonMapRoom previousNode; // 현재 위치 이전의 노드
    public int GCost; // 시작 노드 ~ 검사할 노드까지의 비용
    public int HCost; // 검사할 노드 ~ 목적지 노드까지의 추정 비용
    public int FCost => GCost + HCost; // 최종 비용

    [Header("UI 컴포넌트")]
    public SpriteRenderer spriteRenderer;
    public TextMeshProUGUI textRoomType;
    public TextMeshProUGUI textCoordinate;


    void Start()
    {
       transform.SetParent(M_MapManager.instance.MapRooms.transform);
       transform.localPosition = new Vector3(transform.position.x, transform.position.y, 0f);
       transform.localRotation = Quaternion.Euler(0, 0f, 0f);
    }

    private void OnMouseDown()
    {
        if(isRegion && !isActive) return; // 거점지역인 경우 아직 비활성화 상태면 이동 불가

        // if(M_MapManager.instance.GetDistanceFromCurrentCoordinate(this.coordinate) > M_MapManager.instance.mapSight) return; // 맵 시야값 이상은 이동 불가

        // 클릭한 육각형으로 맵플레이어 이동 및 현재 선택된 맵으로 저장
        NetworkClient.localPlayer.GetComponent<GamePlayerMap>().CmdSelectHexagonMapRoom(this, NetworkClient.connection.identity);
        // 맵 플레이어가 이동할 방에 표시
        NetworkClient.localPlayer.GetComponent<GamePlayerMap>().CmdChangeMapPlayerDestinationPosition(this, GetComponent<Transform>().position);
    }

    private void OnMouseEnter()
    {
        // 거점지역 정보 팝업 활성화
        if(isRegion && region != null){
            MapUI.instance.RegionPopUpShow(region);
        }
        
    }

    private void OnMouseExit()
    {
        // 거점지역 정보 팝업 비활성화
        if(isRegion && region != null){
            MapUI.instance.RegionPopUpHide();
        }
    }

    void OnChangedRoomType(RoomType oldVal, RoomType newVal)
    {
        switch(newVal)
        {
            case RoomType.START_LOCATION :
                textRoomType.color = Color.gray;
                textRoomType.text = Const.RoomType_StartLocation;
                break;
            case RoomType.MONSTER :
                textRoomType.color = Color.red;
                textRoomType.text = Const.RoomType_Monster;
                break;
            case RoomType.ELITE :
                textRoomType.color = Color.red;
                textRoomType.text = Const.RoomType_Elite;
                break;
            case RoomType.EVENT :
                textRoomType.color = Color.yellow;
                textRoomType.text = Const.RoomType_Event;
                break;
            case RoomType.CAMP :
                textRoomType.color = Color.green;
                textRoomType.text = Const.RoomType_Camp;
                break;
            case RoomType.ITEM_NPC :
                textRoomType.color = Color.blue;
                textRoomType.text = Const.RoomType_ItemNpc;
                break;
            case RoomType.CARD_NPC :
                textRoomType.color = Color.magenta;
                textRoomType.text = Const.RoomType_CardNpc;
                break;
        }
    }

    void OnChangedCoordinate(Vector2Int oldValue, Vector2Int newValue)
    {
        textCoordinate.text = newValue.ToString();
    }

    // HexagonMapRoom이 isRegion인 경우 비활성화 상태
    void OnChangedIsRegion(bool oldValue, bool newValue)
    {
        if(newValue){
            ChangeHexagonRoomActive(false);
        }
    }

    // 활성화 상태 변수값에 따라 방활성화 상태 변경
    void OnChangedIsActive(bool oldValue, bool newValue)
    {
        ChangeHexagonRoomActive(newValue);
    }

    // HexagonMapRoom의 스프라이트 알파값과 텍스트 상태값 변경
    public void ChangeHexagonRoomActive(bool isActive)
    {
        float alpha = isActive ? 1f : 0f;
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, alpha);
        textRoomType.gameObject.SetActive(isActive);
        //textCoordinate.gameObject.SetActive(isActive);
    }

    // 방 완료시 상태변경
    void OnChangedIsComplete(bool oldValue, bool newValue)
    {
        spriteRenderer.color = Color.gray;
        textRoomType.color = Color.green;
        textRoomType.text = Const.RoomState_Complete;
    }

}