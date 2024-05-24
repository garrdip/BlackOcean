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
    public GameObject mapTileMask; // 맵 타일 마스크
    public GameObject originMapTile; // 원본 위치의 맵타일 오브젝트(라인 렌더러 위치를 위한 용도)
    public GameObject expandMapTile; // 위쪽 방향으로 확장되는 맵타일 오브젝트
    public GameObject mapTileBase;
    public GameObject mapTileLayer;
    public GameObject mapTileIcon;
    public GameObject mapIcon; // 기본 상태에서 보여지는 맵 아이콘 이미지
    public GameObject mapTileGrid;
    public SortingGroup sortingGroup;

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
        sortingGroup.sortingOrder = -(int)(transform.position.y * 10f);
        SetCanvasSortOrder();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if(!isActive){
            mapTileBase.SetActive(false);
            mapTileLayer.SetActive(false);
            mapTileIcon.SetActive(false);
            mapIcon.SetActive(false);
            mapTileGrid.SetActive(false);
        }
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
                mapIcon.SetActive(false);
                break;
            case RoomType.MONSTER :
                mapIcon.GetComponent<SpriteRenderer>().sprite = M_MapManager.instance.mapTypeIcons[MapTypeIcon.Normal_Monster];
                mapIconSmall.sprite = M_MapManager.instance.mapTypeIcons[MapTypeIcon.Normal_Monster];
                break;
            case RoomType.ELITE :
                mapIcon.GetComponent<SpriteRenderer>().sprite = M_MapManager.instance.mapTypeIcons[MapTypeIcon.Elite_Monster];
                mapIconSmall.sprite = M_MapManager.instance.mapTypeIcons[MapTypeIcon.Elite_Monster];
                break;
            case RoomType.EVENT_POSITIIVE :
                mapIcon.GetComponent<SpriteRenderer>().sprite = M_MapManager.instance.mapTypeIcons[MapTypeIcon.Card_Shop];
                mapIconSmall.sprite = M_MapManager.instance.mapTypeIcons[MapTypeIcon.Card_Shop];
                break;
            case RoomType.EVENT_NEGATIVE :
                mapIcon.GetComponent<SpriteRenderer>().sprite = M_MapManager.instance.mapTypeIcons[MapTypeIcon.Card_Shop];
                mapIconSmall.sprite = M_MapManager.instance.mapTypeIcons[MapTypeIcon.Card_Shop];
                break;
            case RoomType.CAMP :
                mapIcon.GetComponent<SpriteRenderer>().sprite = M_MapManager.instance.mapTypeIcons[MapTypeIcon.Card_Shop];
                mapIconSmall.sprite = M_MapManager.instance.mapTypeIcons[MapTypeIcon.Card_Shop];
                break;
            case RoomType.ITEM_NPC :
                mapIcon.GetComponent<SpriteRenderer>().sprite = M_MapManager.instance.mapTypeIcons[MapTypeIcon.Card_Shop];
                mapIconSmall.sprite = M_MapManager.instance.mapTypeIcons[MapTypeIcon.Card_Shop];
                break;
            case RoomType.CARD_NPC :
                mapIcon.GetComponent<SpriteRenderer>().sprite = M_MapManager.instance.mapTypeIcons[MapTypeIcon.Card_Shop];
                mapIconSmall.sprite = M_MapManager.instance.mapTypeIcons[MapTypeIcon.Card_Shop];
                break;
            case RoomType.COMPLETE :
                mapIcon.SetActive(false);
                mapTileIcon.GetComponent<SpriteRenderer>().color = Color.black;
                break;
            case RoomType.RUINS :
                mapIcon.SetActive(false);
                break;
            case RoomType.BOSS :
                mapIcon.SetActive(false);
                mapTileIcon.GetComponent<SpriteRenderer>().color = ColorUtils.HexToColor("#E700FF");
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
        if(newValue){
            expandMapTile.transform.DOKill();
            expandMapTile.transform.DOLocalMoveY(0.25f, 0.5f);
            mapTileMask.GetComponent<SpriteMask>().enabled = true;
            mapTileBase.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            mapTileBase.SetActive(true);
            hexagonMapRoomUI.transform.DOLocalMoveY(0.25f, 0.5f);
            hexagonMapRoomUI.SetActive(true);              
            AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("ingame_menu_stage_mouseclick"));
            M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        }else{
            expandMapTile.transform.DOLocalMoveY(0f, 0.5f).OnComplete(() => {
                mapTileMask.GetComponent<SpriteMask>().enabled = false;
                mapTileBase.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.None;
                mapTileBase.SetActive(false);
            });
            hexagonMapRoomUI.transform.DOLocalMoveY(0f, 0.5f);
            hexagonMapRoomUI.SetActive(false);
        }
        ChangeMapVoteIconState();
        mapIcon.GetComponent<SpriteRenderer>().DOFade(newValue == true ? 0.25f : 1f, 0.5f);
        sortingGroup.sortingLayerName = newValue ? "HexagonMapRoomSelected" : "HexagonMapRoom";

        int hazardValue = hazard - M_MapManager.instance.currentRoom.hazard;
        textHazardCount.text = Mathf.Abs(hazardValue).ToString();
        if(hazardValue == 0){
            hazardArrow.gameObject.SetActive(false);
            textHazardTitle.text = "위험도 동일";
            hazardArrow.color = Color.white;
        }else{
            hazardArrow.gameObject.SetActive(true);
            textHazardTitle.text = hazardValue > 0 ? "위험도 증가" : "위험도 감소" ;
            hazardArrow.flipY = hazardValue > 0 ? false : true;
            hazardArrow.color =  hazardValue > 0 ? Color.red : ColorUtils.HexToColor("#0080ff");
        }
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
        expandMapTile.SetActive(isActive);
        mapTileLayer.SetActive(isActive);
        mapTileIcon.SetActive(isActive);
        if(roomType == RoomType.COMPLETE){
            mapIcon.SetActive(false);
        }else{
            if(isRegion && !isActive){
                mapIcon.SetActive(false);
            }else{
                mapIcon.SetActive(isActive);
            }
        }
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
}