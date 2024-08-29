using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Mirror;
using ProjectD;
using TMPro;
using DG.Tweening;

[System.Serializable]
public class HexagonMapRoom : NetworkBehaviour
{
    public readonly SyncList<uint> votePlyers = new SyncList<uint>(); // 방에 투표한 GamePlayer의 netId 목록

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

    [SyncVar (hook = nameof(OnChangedIsSelected))]
    public bool isSelected = false;

    [SyncVar]
    public int hazard; // 위험도

    public int GCost; // 시작 노드 ~ 검사할 노드까지의 비용
    public int HCost; // 검사할 노드 ~ 목적지 노드까지의 추정 비용
    public int FCost => GCost + HCost; // 최종 비용

    [Header("맵 타일")]
    public GameObject mapTileBase; // 맵타일 베이스 오브젝트
    public GameObject mapTileMask; // 맵타일 마스크
    public GameObject originMapTile; // 원본 위치의 맵타일 오브젝트(라인 렌더러 위치를 위한 용도)
    public GameObject mapTileGrid;
    private float expandValue;
    private float originValue;
    private const float expandDuration = 0.5f;
    public TextMeshPro textRoomType;

    [Header("맵 UI")]
    public GameObject hexagonMapRoomUI;

    [Header("맵 아이콘 이미지")]
    public SpriteRenderer mapIconSmall;

    [Header("턴 정보 레이아웃")]
    public List<GameObject> mapVoteIcons = new List<GameObject>();
    public GameObject TurnLayout;
    public Canvas TurnLayoutCanvas;
    public TextMeshProUGUI textMyRequireCost;

    [Header("위험도 정보 레이아웃")]
    public GameObject DangerLayout;
    public Canvas DangerLayoutCanvas;
    public TextMeshProUGUI textHazardTitle;
    public TextMeshProUGUI textHazardCount;
    public SpriteRenderer hazardArrow;


    [Header("로컬 플레이어가 선택한 맵 인디케이터 레이아웃")]
    public GameObject PlayerChoiceLayout;


    [Header("다른 플레이어가 선택한 맵 인디케이터 레이아웃")]
    public List<GameObject> mapVoteIconsAnother = new List<GameObject>();
    public GameObject AnotherPlayerChoiceLayout;
    public Canvas AnotherPlayerChoiceLayoutCanvas;
    public TextMeshProUGUI textAnotherRequireCost;


    void Start()
    {
        transform.SetParent(M_MapManager.instance.MapRooms.transform);
        transform.localPosition = new Vector3(transform.position.x, transform.position.y, 0f);
        transform.localRotation = Quaternion.Euler(0, 0f, 0f);
        mapTileBase.GetComponent<SpriteRenderer>().sortingOrder = -(int)(transform.position.y * 10f);
        SetCanvasSortOrder();
        expandValue = mapTileBase.transform.localPosition.y + 0.2f;
        originValue = mapTileBase.transform.localPosition.y;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        mapTileBase.SetActive(isActive);
        textRoomType.gameObject.SetActive(isActive);
        votePlyers.Callback += OnUpdateVotePlayers;
    }

    private void OnMouseDown()
    {
        if(NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId != 0){
            GamePlayerMap gamePlayerMap = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerMap>();
            // 맵 플레이어가 이동할 방에 표시 및 이동 경로 표시(서버 요청)
            gamePlayerMap.CmdChangeMapPlayerDestinationPosition(this, GetComponent<Transform>().position, NetworkClient.localPlayer.GetComponent<NetworkIdentity>());
        }
    }

    private void OnMouseEnter()
    {
        // 거점지역 정보 팝업 활성화
        if(isRegion && region != null){
            MapUI.instance.RegionPopUpShow(region);
        }
        if(NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId != 0){
            GamePlayerMap gamePlayerMap = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerMap>();
            gamePlayerMap.DisplayFindPath(this, GetComponent<Transform>().position, NetworkClient.localPlayer.GetComponent<NetworkIdentity>());
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

    void OnUpdateVotePlayers(SyncList<uint>.Operation op, int index, uint oldVal, uint newVal)
    {
        ChangeHexagonMapRoomLayoutState();
        switch (op)
        {
            case SyncList<uint>.Operation.OP_ADD:
                // votePlayer에 추가될 때 추가된 플레이어의 order값에 맞는 위치의 아이콘 활성화
                int addOrder = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == newVal);
                if(addOrder != -1){
                    mapVoteIcons[addOrder].SetActive(true);
                    mapVoteIconsAnother[addOrder].SetActive(true);
                }
                break;
            case SyncList<uint>.Operation.OP_INSERT:
                
                break;
            case SyncList<uint>.Operation.OP_REMOVEAT:
                // votePlayer에 제거될 때 추가된 플레이어의 order값에 맞는 위치의 아이콘 비활성화
                int removeOrder = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == oldVal);
                if(removeOrder != -1){
                    mapVoteIcons[removeOrder].SetActive(false);
                    mapVoteIconsAnother[removeOrder].SetActive(false);
                }
                break;
            case SyncList<uint>.Operation.OP_SET:
                
                break;
            case SyncList<uint>.Operation.OP_CLEAR:
                
                break;
        }
    }

    void OnChangedRoomType(RoomType oldVal, RoomType newVal)
    {
        switch(newVal)
        {
            case RoomType.START_LOCATION :
                mapTileBase.GetComponent<SpriteRenderer>().color = Color.red;
                textRoomType.text = "Start";
                break;
            case RoomType.MONSTER :
                textRoomType.text = "Normal Monster";
                break;
            case RoomType.ELITE :
                textRoomType.text = "Elite Monster";
                break;
            case RoomType.EVENT_POSITIIVE :
                textRoomType.text = "Event";
                break;
            case RoomType.EVENT_NEGATIVE :
                textRoomType.text = "Event";
                break;
            case RoomType.CAMP :
                textRoomType.text = "Camp";
                break;
            case RoomType.ITEM_NPC :
                textRoomType.text = "Item Shop";
                break;
            case RoomType.CARD_NPC :
                textRoomType.text = "Card Shop";
                break;
            case RoomType.COMPLETE :
                textRoomType.text = "Complete";
                break;
            case RoomType.RUINS :
                mapTileBase.GetComponent<SpriteRenderer>().color = ProjectD.ColorUtils.HexToColor("#E700FF");    
                textRoomType.text = "Ruins";
                break;
            case RoomType.BOSS :
                mapTileBase.GetComponent<SpriteRenderer>().color = ProjectD.ColorUtils.HexToColor("#E700FF");
                textRoomType.text = "Boss";
                break;
        }
    }

    void OnChangedCoordinate(Vector2Int oldValue, Vector2Int newValue)
    {
        //textCoordinate.text = newValue.ToString();
    }

    // HexagonMapRoom이 isRegion인 경우 비활성화 상태
    void OnChangedIsRegion(bool oldValue, bool newValue)
    {
        
    }

    // 활성화 상태 변수값에 따라 방활성화 상태 변경
    void OnChangedIsActive(bool oldValue, bool newValue)
    {
        ChangeHexagonRoomActive(newValue);
    }

    // HexagonMapRoom 선택 상태 변경
    void OnChangedIsSelected(bool oldValue, bool newValue)
    {
        ChangeMapExpandedState(newValue);
        ChangeMapVoteIconState();
        ChangeMapHazardValue();
        ChangeMapBossExpandedPosition(newValue);
    }

    // HexagonMapRoom의 SyncVar참조값인 MapBoss의 변화 감지(방의 MapBoss참조값이 할당되었다는 것은 해당 방으로 보스가 이동했다는 것)
    void OnChangeMapBoss(MapBoss oldValue, MapBoss newValue)
    {
        if(isServer && newValue != null){
            if(!isActive && isRegion){
                return; // 활성화 되지 않은 거점지역은 보스룸 변화에서 제외
            }else{
                M_MapManager.instance.SetRoomTypeBossRoom(this);
            }
        }
    }

    // ------------------------------------------------------------ Normal Method --------------------------------------------------------------- //


    // HexagonMapRoom의 컨테이너 레이아웃 오브젝트 활성화 상태 변경
    void ChangeHexagonRoomActive(bool isActive)
    {
        float alpha = isActive ? 1f : 0f;
        mapTileBase.SetActive(isActive);
        textRoomType.gameObject.SetActive(isActive);
    }

    // 선택한 HexaonMapRoom의 UI 컴포넌트들의 활성화 상태 변경(본인이 선택한 경우와 다른 플레이어가 선택한 경우 구분)
    public void ChangeHexagonRoomUIByOwner(bool isActive)
    {
        AnotherPlayerChoiceLayout.SetActive(!isActive);
        TurnLayout.SetActive(isActive);
        DangerLayout.SetActive(isActive);
        PlayerChoiceLayout.SetActive(isActive);
    }

    private void SetCanvasSortOrder()
    {
        TurnLayoutCanvas.sortingLayerName = "MapPlayerPiece";
        TurnLayoutCanvas.sortingOrder = 1000;
        DangerLayoutCanvas.sortingLayerName = "MapPlayerPiece";
        DangerLayoutCanvas.sortingOrder = 1000;
        AnotherPlayerChoiceLayoutCanvas.sortingLayerName = "MapPlayerPiece";
        AnotherPlayerChoiceLayoutCanvas.sortingOrder = 1000;
    }

    // 방 레이아웃 상태 변경
    private void ChangeHexagonMapRoomLayoutState()
    {
        if(votePlyers.Count > 1){
            int idx = votePlyers.FindIndex((netId) => netId == NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId);
            if(idx != -1){
                PlayerChoiceLayout.SetActive(true);
                AnotherPlayerChoiceLayout.SetActive(false);
                TurnLayout.SetActive(true);
                DangerLayout.SetActive(true);
            }else{
                PlayerChoiceLayout.SetActive(false);
                AnotherPlayerChoiceLayout.SetActive(true);
                TurnLayout.SetActive(false);
                DangerLayout.SetActive(false);
            }
        }else{
            int idx = votePlyers.FindIndex((netId) => netId == NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId);
            if(idx != -1){
                PlayerChoiceLayout.SetActive(true);
                AnotherPlayerChoiceLayout.SetActive(false);
                TurnLayout.SetActive(true);
                DangerLayout.SetActive(true);
            }else{
                PlayerChoiceLayout.SetActive(false);
                AnotherPlayerChoiceLayout.SetActive(true);
                TurnLayout.SetActive(false);
                DangerLayout.SetActive(false);
            }
        }
    }

    // 방 선택 상태에 따라 Expand 상태 변경
    private void ChangeMapExpandedState(bool isSelected)
    {
        if(isSelected){
            mapTileBase.transform.DOLocalMoveY(expandValue, expandDuration);
            hexagonMapRoomUI.transform.DOLocalMoveY(expandValue, expandDuration);
            mapTileMask.GetComponent<SpriteMask>().enabled = true;
            mapTileMask.transform.localPosition = new Vector3(
                mapTileMask.transform.localPosition.x,
                expandValue + 0.05f, // 마스크 확장 위치는 베이스보다 y축 0.05f 높은위치에(타일 하단부는 딱 맞게)
                mapTileMask.transform.localPosition.z
            );
            hexagonMapRoomUI.SetActive(true);
        }else{
            mapTileBase.transform.DOLocalMoveY(originValue, expandDuration);
            hexagonMapRoomUI.transform.DOLocalMoveY(originValue, expandDuration);
            mapTileMask.GetComponent<SpriteMask>().enabled = false;
            mapTileMask.transform.localPosition = new Vector3(
                mapTileMask.transform.localPosition.x,
                originValue,
                mapTileMask.transform.localPosition.z
            );  
            hexagonMapRoomUI.SetActive(false);
        }
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("ingame_menu_stage_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    // 방 위험도 표시
    private void ChangeMapHazardValue()
    {
        int hazardValue = hazard - M_MapManager.instance.currentRoom.hazard; // 현재 위치한 방과 다음 목적지로 선택한 방의 위험도 차이값
        textHazardCount.text = Mathf.Abs(hazardValue).ToString();
        if(hazardValue == 0){
            hazardArrow.gameObject.SetActive(false);
            textHazardTitle.text = "위험도 동일";
            hazardArrow.color = Color.white;
        }else{
            hazardArrow.gameObject.SetActive(true);
            textHazardTitle.text = hazardValue > 0 ? "위험도 증가" : "위험도 감소" ;
            hazardArrow.flipY = hazardValue > 0 ? false : true;
            hazardArrow.color =  hazardValue > 0 ? Color.red : ProjectD.ColorUtils.HexToColor("#0080ff");
        }
    }

    // 방 투표 상태 아이콘 변경
    private void ChangeMapVoteIconState()
    {
        int index = votePlyers.FindIndex((netId) => netId == NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId); // 해당방에 로컬플레이어가 투표했는지 확인
        int order = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.selectOrder;
        if(index != -1){
            mapVoteIcons[order].transform.GetChild(0).gameObject.SetActive(true);
            mapVoteIcons[order].transform.GetChild(1).gameObject.SetActive(true);
        }else{
            mapVoteIcons[order].transform.GetChild(0).gameObject.SetActive(true);
            mapVoteIcons[order].transform.GetChild(1).gameObject.SetActive(true);
            mapVoteIconsAnother[order].transform.GetChild(0).gameObject.SetActive(true);
            mapVoteIconsAnother[order].transform.GetChild(1).gameObject.SetActive(true);
        }
    }

    // 해당 방에 위치한 맵보스의 위치 Y좌표 조정(확장되는 맵타일과 동일하게)
    public void ChangeMapBossExpandedPosition(bool isSelected)
    {
        if(mapBoss != null){
            if(isSelected){
                mapBoss.transform.DOMoveY(transform.position.y + 0.35f, expandDuration);
            }else{
                mapBoss.transform.DOMoveY(transform.position.y + 0.15f, expandDuration);
            }
        }
    }
}