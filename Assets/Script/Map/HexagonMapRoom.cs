using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using TMPro;

[System.Serializable]
public class HexagonMapRoom : NetworkBehaviour
{
    [SyncVar (hook = nameof(OnChangedRoomType))]
    public RoomType roomType = RoomType.UNDEFINED; // 방 타입

    [SyncVar (hook = nameof(OnChangedCoordinate))]
    public Vector2Int coordinate; // 각 방의 고유 좌표계 값

    [SyncVar]
    public Vector3 position; // 인게임 좌표계 값

    [SyncVar (hook = nameof(OnChangeMapBoss))]
    public MapBoss mapBoss;

    [SyncVar (hook = nameof(OnChangedIsRegion))]
    public bool isRegion = false; // 거점지역 구분값

    [SyncVar]
    public Region region;

    [SyncVar (hook = nameof(OnChangedIsActive))]
    public bool isActive = false; // 방 활성화 상태 구분값

    [SyncVar]
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
        GamePlayerMap gamePlayerMap = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerMap>();
        // 맵 플레이어가 이동할 방에 표시 및 이동 경로 표시(로컬 클라이언트 전용)
        gamePlayerMap.ClientChangeMapPlayerDestinationPosition(this, GetComponent<Transform>().position, NetworkClient.localPlayer.GetComponent<NetworkIdentity>());
        // 맵 플레이어가 이동할 방에 표시 및 이동 경로 표시(서버 요청)
        gamePlayerMap.CmdChangeMapPlayerDestinationPosition(this, GetComponent<Transform>().position, NetworkClient.localPlayer.GetComponent<NetworkIdentity>());
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
 
    // ------------------------------------------------------------ Syncvar Hook --------------------------------------------------------------- //

    void OnChangedRoomType(RoomType oldVal, RoomType newVal)
    {
        switch(newVal)
        {
            case RoomType.START_LOCATION :
                spriteRenderer.color = Color.white;
                textRoomType.color = Color.gray;
                textRoomType.text = Const.RoomType_StartLocation;
                break;
            case RoomType.MONSTER :
                spriteRenderer.color = Color.white;
                textRoomType.color = Color.red;
                textRoomType.text = Const.RoomType_Monster;
                break;
            case RoomType.ELITE :
                spriteRenderer.color = Color.white;
                textRoomType.color = Color.red;
                textRoomType.text = Const.RoomType_Elite;
                break;
            case RoomType.EVENT :
                spriteRenderer.color = Color.white;
                textRoomType.color = Color.yellow;
                textRoomType.text = Const.RoomType_Event;
                break;
            case RoomType.CAMP :
                spriteRenderer.color = Color.white;
                textRoomType.color = Color.green;
                textRoomType.text = Const.RoomType_Camp;
                break;
            case RoomType.ITEM_NPC :
                spriteRenderer.color = Color.white;
                textRoomType.color = Color.blue;
                textRoomType.text = Const.RoomType_ItemNpc;
                break;
            case RoomType.CARD_NPC :
                spriteRenderer.color = Color.white;
                textRoomType.color = Color.magenta;
                textRoomType.text = Const.RoomType_CardNpc;
                break;
            case RoomType.COMPLETE :
                spriteRenderer.color = Color.gray;
                textRoomType.color = Color.black;
                textRoomType.text = Const.RoomType_Complete;
                break;
            case RoomType.RUINS :
                spriteRenderer.color = ColorUtils.HexToColor("#2F745A");
                textRoomType.color = Color.black;
                textRoomType.text = Const.RoomType_Ruins;
                break;
            case RoomType.BOSS :
                spriteRenderer.color = Color.red;
                textRoomType.color = Color.black;
                textRoomType.text = Const.RoomType_Boss;
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
    void ChangeHexagonRoomActive(bool isActive)
    {
        float alpha = isActive ? 1f : 0f;
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, alpha);
        textRoomType.gameObject.SetActive(isActive);
        //textCoordinate.gameObject.SetActive(isActive);
    }

    // HexagonMapRoom의 SyncVar참조값인 MapBoss의 변화 감지(방의 MapBoss참조값이 할당되었다는 것은 해당 방으로 보스가 이동했다는 것)
    void OnChangeMapBoss(MapBoss oldValue, MapBoss newValue)
    {
        if(isServer && newValue != null){
            M_MapManager.instance.SetRoomTypeBossRoom(this);
        }
    }
}