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

    [SyncVar]
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

    [Header("A* 알고리즘 비용 변수")]
    public int GCost; // 시작 노드 ~ 검사할 노드까지의 비용
    public int HCost; // 검사할 노드 ~ 목적지 노드까지의 추정 비용
    public int FCost => GCost + HCost; // 최종 비용

    [Header("맵 타일 공통 컴포넌트")]
    public GameObject mapTileMask; // 맵 타일 마스크
    public GameObject originMapTile; // 원본 위치의 맵 타일 오브젝트(라인 렌더러 위치를 위한 용도)
    public GameObject hexagonMapRoomUI; // 맵 타일 UI
    private float expandValue;
    private float originValue;
    private const float expandDuration = 0.5f;

    [Header("맵 타일 스프라이트 랜더러(기본 상태)")]
    public GameObject mapTileBase; // 맵 타일 베이스 오브젝트
    public SpriteRenderer mapTileBaseRenderer; // 맵 타일 베이스 스프라이트 랜더러

    [Header("맵 타일 스프라이트 랜더러(선택 상태)")]
    public GameObject mapTileSelect; // 선택된 상태 맵 타일 베이스 오브젝트
    public SortingGroup mapTileSelectSortingGroup; // 선택된 맵 타일 정렬그룹
    public GameObject mapTilePathLine; // 경로 표시 맵 타일 라인 오브젝트
    public SortingGroup mapTilePathLineSortingGroup; // 경로 표시 맵 타일 라인 오브젝트 정렬그룹
    public SpriteRenderer mapTileBaseSelectRenderer;
    public SpriteRenderer mapTileCapSelectRenderer;
    public SpriteRenderer mapTileCapSelectLightRenderer;
    public SpriteRenderer mapTileIconSelectRenderer;
    public SpriteRenderer mapTileIconSelectLightRenderer;

    [Header("선택한 방의 정보창 스프라이트 랜더러")]
    public SpriteRenderer mapRoomInfoBase;
    public SpriteRenderer mapRoomInfoBaseLight;
    public SpriteRenderer mapRoomInfoIcon;
    public SpriteRenderer mapRoomInfoIconLight;

    [Header("로컬 플레이어의 방 선택 정보 창")]
    public GameObject myVoteLayout;
    public TextMeshPro textHazardState;
    public TextMeshPro textHazardValue;
    public SpriteRenderer hazardArrow;
    public TextMeshPro textMyRequireCost;
    public List<GameObject> mapVoteIconsMine = new List<GameObject>(); // 로컬 플레이어 선택한 맵 투표 아이콘

    [Header("다른 플레이어의 방 선택 정보 창")]
    public GameObject anotherVoteLayout;
    public TextMeshPro textAnotherRequireCost;
    public List<GameObject> mapVoteIconsAnother = new List<GameObject>(); // 다른 플레이어가 선택한 맵 투표 아이콘

    [Header("방 투표 아이콘")]
    public Sprite voteIconMinePick;
    public Sprite voteIconAnother;
    public Sprite voteIconAnotherPick;



    void Start()
    {
        transform.SetParent(M_MapManager.instance.MapRooms.transform);
        transform.localPosition = new Vector3(transform.position.x, transform.position.y, 0f);
        transform.localRotation = Quaternion.Euler(0, 0f, 0f);
        mapTileBaseRenderer.sortingOrder = -(int)(transform.position.y * 10f);
        mapTileSelectSortingGroup.sortingOrder = -(int)(transform.position.y * 10f);
        mapTilePathLineSortingGroup.sortingOrder = -(int)(transform.position.y * 10f);
        expandValue = mapTileBase.transform.localPosition.y + 0.2f;
        originValue = mapTileBase.transform.localPosition.y;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        mapTileBase.SetActive(isActive);
        votePlyers.Callback += OnUpdateVotePlayers;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        mapRoomInfoBaseLight.DOKill();
        mapRoomInfoIconLight.DOKill();
        mapTileSelect.transform.DOKill();
        mapTileBase.transform.DOKill();
        hexagonMapRoomUI.transform.DOKill();
    }

    private void OnDestroy()
    {
        mapRoomInfoBaseLight.DOKill();
        mapRoomInfoIconLight.DOKill();
        mapTileSelect.transform.DOKill();
        mapTileBase.transform.DOKill();
        hexagonMapRoomUI.transform.DOKill();
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
        ChangeMapTileByMouseOver(true, roomType);
    }

    private void OnMouseExit()
    {
        // 거점지역 정보 팝업 비활성화
        if(isRegion && region != null){
            MapUI.instance.RegionPopUpHide();
        }
        ChangeMapTileByMouseOver(false, roomType);
    }
 
    // ------------------------------------------------------------ Syncvar Hook --------------------------------------------------------------- //

    void OnChangedRoomType(RoomType oldVal, RoomType newVal)
    {
        switch(newVal)
        {
            case RoomType.START_LOCATION :
                mapTileBaseRenderer.sprite = M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Current);
                mapTileBaseSelectRenderer.sprite = M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Current_Default);
                mapTileCapSelectRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_Current);
                mapTileCapSelectLightRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_Current);
                break;
            case RoomType.MONSTER :
                mapTileBaseRenderer.sprite =  M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_NormalMonster);
                mapTileBaseSelectRenderer.sprite = M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_NormalMonster_Default);
                mapTileCapSelectRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_Monster);
                mapTileCapSelectLightRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_Monster_Light);
                mapTileIconSelectRenderer.sprite = M_MapManager.instance.mapTileIconAtlas.GetSprite(Const.M_I_NormalMonster);
                mapTileIconSelectLightRenderer.sprite = M_MapManager.instance.mapTileIconAtlas.GetSprite(Const.M_I_MormalMonster_Light);
                mapRoomInfoBase.sprite = M_MapManager.instance.mapRoomInfoBases[MapRoomInfoBase.NORMAL_MONSTER];
                mapRoomInfoBaseLight.sprite = M_MapManager.instance.mapRoomInfoBases[MapRoomInfoBase.NORMAL_MONSTER_L];
                mapRoomInfoIcon.sprite = M_MapManager.instance.mapRoomInfoIcons[MapRoomInfoIcon.NORMAL_MONSTER];
                mapRoomInfoIconLight.sprite = M_MapManager.instance.mapRoomInfoIcons[MapRoomInfoIcon.NORMAL_MONSTER_L];
                break;
            case RoomType.ELITE :
                mapTileBaseRenderer.sprite = M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_EliteMonster);
                mapTileBaseSelectRenderer.sprite = M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_EliteMonster_Default);
                mapTileCapSelectRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_Monster);
                mapTileCapSelectLightRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_Monster);
                mapTileIconSelectRenderer.sprite = M_MapManager.instance.mapTileIconAtlas.GetSprite(Const.M_I_EliteMonster);
                mapTileIconSelectLightRenderer.sprite = M_MapManager.instance.mapTileIconAtlas.GetSprite(Const.M_I_EliteMonster_Light);
                mapRoomInfoBase.sprite = M_MapManager.instance.mapRoomInfoBases[MapRoomInfoBase.ELITE_MONSTER];
                mapRoomInfoBaseLight.sprite = M_MapManager.instance.mapRoomInfoBases[MapRoomInfoBase.ELITE_MONSTER_L];
                mapRoomInfoIcon.sprite = M_MapManager.instance.mapRoomInfoIcons[MapRoomInfoIcon.ELITE_MONSTER];
                mapRoomInfoIconLight.sprite = M_MapManager.instance.mapRoomInfoIcons[MapRoomInfoIcon.ELITE_MONSTER_L];
                break;
            case RoomType.EVENT_POSITIIVE :
                mapTileBaseRenderer.sprite = M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Event);
                mapTileBaseSelectRenderer.sprite = M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Event_Default);
                mapTileCapSelectRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_Event);
                mapTileCapSelectLightRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_Event_Light);
                mapTileIconSelectRenderer.sprite = M_MapManager.instance.mapTileIconAtlas.GetSprite(Const.M_I_Event);
                mapTileIconSelectLightRenderer.sprite = M_MapManager.instance.mapTileIconAtlas.GetSprite(Const.M_I_Event_Light);
                mapRoomInfoBase.sprite = M_MapManager.instance.mapRoomInfoBases[MapRoomInfoBase.EVENT];
                mapRoomInfoBaseLight.sprite = M_MapManager.instance.mapRoomInfoBases[MapRoomInfoBase.EVENT_L];
                mapRoomInfoIcon.sprite = M_MapManager.instance.mapRoomInfoIcons[MapRoomInfoIcon.EVENT];
                mapRoomInfoIconLight.sprite = M_MapManager.instance.mapRoomInfoIcons[MapRoomInfoIcon.EVENT_L];
                break;
            case RoomType.EVENT_NEGATIVE :
                mapTileBaseRenderer.sprite = M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Event);
                mapTileBaseSelectRenderer.sprite = M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Event_Default);
                mapTileCapSelectRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_Event);
                mapTileCapSelectLightRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_Event_Light);
                mapTileIconSelectRenderer.sprite = M_MapManager.instance.mapTileIconAtlas.GetSprite(Const.M_I_Event);
                mapTileIconSelectLightRenderer.sprite = M_MapManager.instance.mapTileIconAtlas.GetSprite(Const.M_I_Event_Light);
                mapRoomInfoBase.sprite = M_MapManager.instance.mapRoomInfoBases[MapRoomInfoBase.EVENT];
                mapRoomInfoBaseLight.sprite = M_MapManager.instance.mapRoomInfoBases[MapRoomInfoBase.EVENT_L];
                mapRoomInfoIcon.sprite = M_MapManager.instance.mapRoomInfoIcons[MapRoomInfoIcon.EVENT];
                mapRoomInfoIconLight.sprite = M_MapManager.instance.mapRoomInfoIcons[MapRoomInfoIcon.EVENT_L];
                break;
            case RoomType.CAMP :
                mapTileBaseRenderer.sprite = M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Camp);
                mapTileBaseSelectRenderer.sprite = M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Camp_Default);
                mapTileCapSelectRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_Camp);
                mapTileCapSelectLightRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_Camp_Light);
                mapTileIconSelectRenderer.sprite = M_MapManager.instance.mapTileIconAtlas.GetSprite(Const.M_I_Camp);
                mapTileIconSelectLightRenderer.sprite = M_MapManager.instance.mapTileIconAtlas.GetSprite(Const.M_I_Camp_Light);
                mapRoomInfoBase.sprite = M_MapManager.instance.mapRoomInfoBases[MapRoomInfoBase.CAMP];
                mapRoomInfoBaseLight.sprite = M_MapManager.instance.mapRoomInfoBases[MapRoomInfoBase.CAMP_L];
                mapRoomInfoIcon.sprite = M_MapManager.instance.mapRoomInfoIcons[MapRoomInfoIcon.CAMP];
                mapRoomInfoIconLight.sprite = M_MapManager.instance.mapRoomInfoIcons[MapRoomInfoIcon.CAMP_L];
                break;
            case RoomType.ITEM_NPC :
                mapTileBaseRenderer.sprite = M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_ItemShop);
                mapTileBaseSelectRenderer.sprite = M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_ItemShop_Default);
                mapTileCapSelectRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_ItemShop);
                mapTileCapSelectLightRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_ItemShop_Light);
                mapTileIconSelectRenderer.sprite = M_MapManager.instance.mapTileIconAtlas.GetSprite(Const.M_I_ItemShop);
                mapTileIconSelectLightRenderer.sprite = M_MapManager.instance.mapTileIconAtlas.GetSprite(Const.M_I_ItemShop_Light);
                mapRoomInfoBase.sprite = M_MapManager.instance.mapRoomInfoBases[MapRoomInfoBase.ITEM_SHOP];
                mapRoomInfoBaseLight.sprite = M_MapManager.instance.mapRoomInfoBases[MapRoomInfoBase.ITEM_SHOP_L];
                mapRoomInfoIcon.sprite = M_MapManager.instance.mapRoomInfoIcons[MapRoomInfoIcon.ITEM_SHOP];
                mapRoomInfoIconLight.sprite = M_MapManager.instance.mapRoomInfoIcons[MapRoomInfoIcon.ITEM_SHOP_L];
                break;
            case RoomType.CARD_NPC :
                mapTileBaseRenderer.sprite = M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_CardShop);
                mapTileBaseSelectRenderer.sprite = M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_CardShop_Default);
                mapTileCapSelectRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_CardShop);
                mapTileCapSelectLightRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_CardShop_Light);
                mapTileIconSelectRenderer.sprite = M_MapManager.instance.mapTileIconAtlas.GetSprite(Const.M_I_CardShop);
                mapTileIconSelectLightRenderer.sprite = M_MapManager.instance.mapTileIconAtlas.GetSprite(Const.M_I_CardShop_Light);
                mapRoomInfoBase.sprite = M_MapManager.instance.mapRoomInfoBases[MapRoomInfoBase.CARD_SHOP];
                mapRoomInfoBaseLight.sprite = M_MapManager.instance.mapRoomInfoBases[MapRoomInfoBase.CARD_SHOP_L];
                mapRoomInfoIcon.sprite = M_MapManager.instance.mapRoomInfoIcons[MapRoomInfoIcon.CARD_SHOP];
                mapRoomInfoIconLight.sprite = M_MapManager.instance.mapRoomInfoIcons[MapRoomInfoIcon.CARD_SHOP_L];
                break;
            case RoomType.COMPLETE :
                mapTileBaseRenderer.sprite = M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Complete);
                mapTileBaseSelectRenderer.sprite = M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Complete_Default);
                mapTileCapSelectRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_Complete);
                mapTileCapSelectLightRenderer.sprite = M_MapManager.instance.mapTileCapAtlas.GetSprite(Const.M_C_Complete);
                mapTileIconSelectRenderer.sprite = M_MapManager.instance.mapTileIconAtlas.GetSprite(Const.M_I_Complete);
                mapTileIconSelectLightRenderer.sprite = M_MapManager.instance.mapTileIconAtlas.GetSprite(Const.M_I_Complete_Light);
                break;
            case RoomType.RUINS :
                mapTileBaseRenderer.color = ProjectD.ColorUtils.HexToColor("#E700FF");
                mapTileCapSelectRenderer.color = ProjectD.ColorUtils.HexToColor("#E700FF");
                mapTileCapSelectLightRenderer.color = ProjectD.ColorUtils.HexToColor("#E700FF");
                break;
            case RoomType.BOSS :
                mapTileBaseRenderer.color = ProjectD.ColorUtils.HexToColor("#E700FF");
                mapTileCapSelectRenderer.color = ProjectD.ColorUtils.HexToColor("#E700FF");
                mapTileCapSelectLightRenderer.color = ProjectD.ColorUtils.HexToColor("#E700FF");
                break;
        }
    }

    void OnChangedCoordinate(Vector2Int oldValue, Vector2Int newValue)
    {
        //textCoordinate.text = newValue.ToString();
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

    void OnUpdateVotePlayers(SyncList<uint>.Operation op, int index, uint oldVal, uint newVal)
    {
        ChangeHexagonMapRoomLayoutState();
        switch (op)
        {
            case SyncList<uint>.Operation.OP_ADD:
                // votePlayer에 추가될 때 추가된 플레이어의 order값에 맞는 위치의 아이콘 설정
                int addOrder = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == newVal);
                if(addOrder != -1){
                    if(newVal == NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId){
                        mapVoteIconsMine[addOrder].GetComponent<SpriteRenderer>().sprite = voteIconMinePick;
                        mapVoteIconsAnother[addOrder].GetComponent<SpriteRenderer>().sprite = voteIconMinePick;
                    }else{
                        mapVoteIconsMine[addOrder].GetComponent<SpriteRenderer>().sprite = voteIconAnotherPick;
                        mapVoteIconsAnother[addOrder].GetComponent<SpriteRenderer>().sprite = voteIconAnotherPick;
                    }
                }
                break;
            case SyncList<uint>.Operation.OP_INSERT:
                
                break;
            case SyncList<uint>.Operation.OP_REMOVEAT:
                // votePlayer에 제거될 때 제거된 플레이어의 order값에 맞는 위치의 아이콘 설정
                int removeOrder = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == oldVal);
                if(removeOrder != -1){
                    mapVoteIconsMine[removeOrder].GetComponent<SpriteRenderer>().sprite = voteIconAnother;
                    mapVoteIconsAnother[removeOrder].GetComponent<SpriteRenderer>().sprite = voteIconAnother;
                }
                break;
            case SyncList<uint>.Operation.OP_SET:
                
                break;
            case SyncList<uint>.Operation.OP_CLEAR:
                
                break;
        }
    }

    void OnUpdatedPlayerOrder(SyncList<uint>.Operation op, int index, uint oldVal, uint newVal)
    {
        switch (op)
        {
            case SyncList<uint>.Operation.OP_ADD:
            
                break;
            case SyncList<uint>.Operation.OP_INSERT:
                
                break;
            case SyncList<uint>.Operation.OP_REMOVEAT:

                break;
            case SyncList<uint>.Operation.OP_SET:
                // PlayerOrder 변경 수신 시 맵 투표 아이콘의 Order값 동기화
                if(votePlyers.Contains(newVal)){
                    if(votePlyers.Count <= 1){
                        for(int i=0; i<mapVoteIconsMine.Count; i++){
                            if(i == index){
                                mapVoteIconsMine[i].GetComponent<SpriteRenderer>().sprite = voteIconMinePick;
                                mapVoteIconsAnother[i].GetComponent<SpriteRenderer>().sprite = voteIconAnotherPick;
                            }else{
                                mapVoteIconsMine[i].GetComponent<SpriteRenderer>().sprite = voteIconAnother;
                                mapVoteIconsAnother[i].GetComponent<SpriteRenderer>().sprite = voteIconAnother;
                            }
                        }
                    }else{
                        for(int i=0; i<M_TurnManager.instance.playerOrder.Count; i++){
                            uint netId = M_TurnManager.instance.playerOrder[i];
                            if(netId == NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId){
                                mapVoteIconsMine[i].GetComponent<SpriteRenderer>().sprite = voteIconMinePick;
                                mapVoteIconsAnother[i].GetComponent<SpriteRenderer>().sprite = voteIconMinePick;
                            }else if(netId == 0){
                                mapVoteIconsMine[i].GetComponent<SpriteRenderer>().sprite = voteIconAnother;
                                mapVoteIconsAnother[i].GetComponent<SpriteRenderer>().sprite = voteIconAnother;
                            }else{
                                mapVoteIconsMine[i].GetComponent<SpriteRenderer>().sprite = voteIconAnotherPick;
                                mapVoteIconsAnother[i].GetComponent<SpriteRenderer>().sprite = voteIconAnotherPick;
                            }
                        }
                    }
                }
                break;
            case SyncList<uint>.Operation.OP_CLEAR:
                
                break;
        }
    }

    // ------------------------------------------------------------ Normal Method --------------------------------------------------------------- //


    // HexagonMapRoom의 컨테이너 레이아웃 오브젝트 활성화 상태 변경
    void ChangeHexagonRoomActive(bool isActive)
    {
        float alpha = isActive ? 1f : 0f;
        mapTileBase.SetActive(isActive);
    }

    // 방 레이아웃 상태 변경
    private void ChangeHexagonMapRoomLayoutState()
    {
        if(votePlyers.Count > 1){
            int idx = votePlyers.FindIndex((netId) => netId == NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId);
            if(idx != -1){
                myVoteLayout.SetActive(true);
                anotherVoteLayout.SetActive(false);
                ChangeMapRoomInfoState(true);
                mapTileIconSelectRenderer.gameObject.SetActive(false);
                mapTileIconSelectLightRenderer.gameObject.SetActive(false);
            }else{
                myVoteLayout.SetActive(false);
                anotherVoteLayout.SetActive(true);
                ChangeMapRoomInfoState(false);
                mapTileIconSelectRenderer.gameObject.SetActive(true);
                mapTileIconSelectLightRenderer.gameObject.SetActive(true);
            }
        }else{
            int idx = votePlyers.FindIndex((netId) => netId == NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId);
            if(idx != -1){
                myVoteLayout.SetActive(true);
                anotherVoteLayout.SetActive(false);
                ChangeMapRoomInfoState(true);
                mapTileIconSelectRenderer.gameObject.SetActive(false);
                mapTileIconSelectLightRenderer.gameObject.SetActive(false);
            }else{
                myVoteLayout.SetActive(false);
                anotherVoteLayout.SetActive(true);
                ChangeMapRoomInfoState(false);
                mapTileIconSelectRenderer.gameObject.SetActive(true);
                mapTileIconSelectLightRenderer.gameObject.SetActive(true);
            }
        }
    }

    // 방 선택 상태에 따라 Expand 상태 변경
    private void ChangeMapExpandedState(bool isSelected)
    {
        if(isSelected){
            mapTileSelect.SetActive(true);
            mapTileSelect.transform.DOLocalMoveY(expandValue, expandDuration);
            mapTileBase.transform.DOLocalMoveY(expandValue, expandDuration);
            hexagonMapRoomUI.transform.DOLocalMoveY(expandValue, expandDuration);
            mapTileMask.GetComponent<SpriteMask>().enabled = true;
            mapTileMask.transform.localPosition = new Vector3(
                mapTileMask.transform.localPosition.x,
                expandValue + 0.02f,
                mapTileMask.transform.localPosition.z
            );
            hexagonMapRoomUI.SetActive(true);
            ChangeSelectRoomRegionIndicatorMask(coordinate, SpriteMaskInteraction.None);
            M_TurnManager.instance.playerOrder.Callback += OnUpdatedPlayerOrder; // 방 선택 상태가 되면 오더 변경 이벤트 등록
        }else{
            mapTileSelect.SetActive(false);
            mapTileSelect.transform.DOLocalMoveY(originValue, expandDuration);
            mapTileBase.transform.DOLocalMoveY(originValue, expandDuration);
            hexagonMapRoomUI.transform.DOLocalMoveY(originValue, expandDuration);
            mapTileMask.GetComponent<SpriteMask>().enabled = false;
            mapTileMask.transform.localPosition = new Vector3(
                mapTileMask.transform.localPosition.x,
                originValue,
                mapTileMask.transform.localPosition.z
            );  
            hexagonMapRoomUI.SetActive(false);
            ChangeSelectRoomRegionIndicatorMask(coordinate, SpriteMaskInteraction.VisibleOutsideMask);
            M_TurnManager.instance.playerOrder.Callback -= OnUpdatedPlayerOrder; // 방 해제 상태가 되면 오더 변경 이벤트 해제
        }
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("ingame_menu_stage_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    // 방 위험도 표시
    private void ChangeMapHazardValue()
    {
        int hazardValue = hazard - M_MapManager.instance.currentRoom.hazard; // 현재 위치한 방과 다음 목적지로 선택한 방의 위험도 차이값
        textHazardValue.text = Mathf.Abs(hazardValue).ToString();
        if(hazardValue == 0){
            hazardArrow.gameObject.SetActive(false);
            textHazardState.text = "위험도 동일";
            hazardArrow.color = Color.white;
        }else{
            hazardArrow.gameObject.SetActive(true);
            textHazardState.text = hazardValue > 0 ? "위험도 증가" : "위험도 감소" ;
            hazardArrow.flipY = hazardValue > 0 ? false : true;
            hazardArrow.color =  hazardValue > 0 ? Color.red : ProjectD.ColorUtils.HexToColor("#0080ff");
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

    // 현재 선택된 방 주위의 거점지역 인디케이터를 조회하여 인덱스값이 2(4시), 3(6시), 4(8시) 인 인디케이터는 maskInteraction을 변경하여 expand된 방에 의해 가려지지 않도록 설정
    private void ChangeSelectRoomRegionIndicatorMask(Vector2Int selectRoomCoordinate, SpriteMaskInteraction spriteMaskInteraction)
    {
        foreach(RegionIndicator regionIndicator in M_MapManager.instance.regionsIndicators){
            if(regionIndicator.coordinate == selectRoomCoordinate){
                if(regionIndicator.index == 2 || regionIndicator.index == 3 || regionIndicator.index == 4){
                    SpriteRenderer spriteRenderer = regionIndicator.GetComponent<SpriteRenderer>();
                    spriteRenderer.maskInteraction = spriteMaskInteraction;
                }
            }
        }
    }

    private void ChangeMapRoomInfoState(bool isActive)
    {
        if(isActive){
            if(DOTween.IsTweening(mapRoomInfoBaseLight)){
                mapRoomInfoBaseLight.DOKill();
            }
            if(DOTween.IsTweening(mapRoomInfoIconLight)){
                mapRoomInfoIconLight.DOKill();
            }
            mapRoomInfoBaseLight.color = new Color(mapRoomInfoBaseLight.color.r, mapRoomInfoBaseLight.color.g, mapRoomInfoBaseLight.color.b, 0f);
            mapRoomInfoIconLight.color = new Color(mapRoomInfoIconLight.color.r, mapRoomInfoIconLight.color.g, mapRoomInfoIconLight.color.b, 0f);
            mapRoomInfoBaseLight.DOFade(1f, 1f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
            mapRoomInfoIconLight.DOFade(1f, 1f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
        }else{
            mapRoomInfoBaseLight.DOKill();
            mapRoomInfoIconLight.DOKill();
        }
    }

    private void ChangeMapTileByMouseOver(bool isMouseOver, RoomType roomType)
    {
        switch(roomType)
        {
            case RoomType.START_LOCATION :
                mapTileBaseRenderer.sprite = isMouseOver ? M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Current_Light) : M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Current);
                break;
            case RoomType.MONSTER :
                mapTileBaseRenderer.sprite = isMouseOver ? M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_NormalMonster_Light) : M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_NormalMonster);
                break;
            case RoomType.ELITE :
                mapTileBaseRenderer.sprite = isMouseOver ? M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_EliteMonster_Light) : M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_EliteMonster);
                break;
            case RoomType.EVENT_POSITIIVE :
                mapTileBaseRenderer.sprite = isMouseOver ? M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Event_Light) : M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Event);
                break;
            case RoomType.EVENT_NEGATIVE :
                mapTileBaseRenderer.sprite = isMouseOver ? M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Event_Light) : M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Event);
                break;
            case RoomType.CAMP :
                mapTileBaseRenderer.sprite = isMouseOver ? M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Camp_Light) : M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Camp);
                break;
            case RoomType.ITEM_NPC :
                mapTileBaseRenderer.sprite = isMouseOver ? M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_ItemShop_Light) : M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_ItemShop);
                break;
            case RoomType.CARD_NPC :
                mapTileBaseRenderer.sprite = isMouseOver ? M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_CardShop_Light) : M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_CardShop);
                break;
            case RoomType.COMPLETE :
                mapTileBaseRenderer.sprite = isMouseOver ? M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Complete_Light) : M_MapManager.instance.mapTileBaseAtlas.GetSprite(Const.M_B_Complete);
                break;
            case RoomType.RUINS :
                mapTileBaseRenderer.color = isMouseOver ? ProjectD.ColorUtils.HexToColor("#E700FF") : Color.white;
                break;
            case RoomType.BOSS :
                mapTileBaseRenderer.color = isMouseOver ? ProjectD.ColorUtils.HexToColor("#E700FF") : Color.white;
                break;
        }
    }
}