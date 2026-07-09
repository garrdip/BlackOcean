using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using ProjectD;
using Steamworks;
using DG.Tweening;

public class MapUI : InstanceD<MapUI>
{
    [Header("거점지역 팝업")]
    public GameObject regionPopUp;
    public TextMeshProUGUI textRegionGradeInfo;
    public TextMeshProUGUI textRegionDesc;

    [Header("보유한 덱 정보 팝업")]
    public GameObject deckInfoPopUp;
    public GridLayoutGroup gridLayoutGroup;
    public Button buttonCloseDeckInfoPopUp;
    public List<GameObject> deckInfoPopUpItems = new List<GameObject>();

    [Header("mapBaseLayout")]
    public GameObject mapBaseLayout;
    public GameObject mapInfoBase;
    public GameObject mapInfoBaseLight;
    public GameObject mapInfoBaseLightSec;
    public GameObject mapInfoOverBase;
    public GameObject mapInfoOverBaseLight;
    public GameObject mapDangerLayout;
    public GameObject mapTurnLayout;
    public Image mapStageIcon;

    [Header("mapDangerLayout")]
    public Vector3 mapDangerLayoutPosition;
    public TextMeshProUGUI textHazardValue;

    [Header("mapTurnLayout")]
    public Vector3 mapTurnLayoutPosition;

    [Header("UI 컴포넌트")]
    public TextMeshProUGUI textCurrentActionCost;
    public TextMeshProUGUI textMaxActionCostCount;
    public Image turnGageBar;
    public Button readyButton;
    public List<GameObject> topIcons = new List<GameObject>();
    public List<Button> swapButtons = new List<Button>();

    [Header("선택한 맵 정보 팝업")]
    public GameObject mapInfoPopUpItemPrefab;
    public GameObject ownerPosition;
    public VerticalLayoutGroup verticalLayoutGroup;
    public List<MapInfoPopUpItem> mapInfoPopUps = new List<MapInfoPopUpItem>();

    [Header("맵 화면의 카메라 줌 조절 변수값")]
    [SerializeField]
    private float minCameraFOV = 30f;
    
    [SerializeField]
    private float maxCameraFOV = 90f;
    
    [SerializeField]
    private float scrollSpeed = 10000f;

    [Header("맵 카메라")]
    public Camera cam;

    [Header("맵 배경 오브젝트")]
    public GameObject mapBackground;
    public GameObject mapDimBackground;
    private Material mapDimBackgroundMaterial;
    public Material blurMaterial;

    [Header("카메라 이동 속도")]
    float cameraMoveSpeed = 20;


    void Start()
    {
        Image mapDimBackgroundImage = mapDimBackground.GetComponent<Image>();
        mapDimBackgroundMaterial = new Material(blurMaterial);
        mapDimBackgroundImage.material = mapDimBackgroundMaterial;
        for(int i=0; i<swapButtons.Count; i++){
            int buttonIndex = i;
            swapButtons[i].transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            swapButtons[i].transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            swapButtons[i].onClick.AddListener(() => HandleMapPlayerSwap(buttonIndex));
        }
        mapDangerLayoutPosition = mapDangerLayout.GetComponent<RectTransform>().localPosition;
        mapTurnLayoutPosition = mapTurnLayout.GetComponent<RectTransform>().localPosition;
        buttonCloseDeckInfoPopUp.onClick.AddListener(() => {
            deckInfoPopUp.SetActive(false);
            ClearDeckInfoPopUpItem();
        });
    }

    void Update()
    { 
        if(Application.isFocused){
            //HandleCameraEdgeScrolling();
            if(!M_MessageManager.instance.isMouseOnChatBox){
                HandleMapCameraByMouseWheel();
            }
        }
    }

    // 스크린 상하좌우 각 변에 마우스 도달 시 카메라 위치 이동
    private void HandleCameraEdgeScrolling()
    {
        Vector3 inputDir = Vector3.zero;
        int edgeScrollSize = 20;
        if (Input.mousePosition.x < edgeScrollSize)
        {
            inputDir.x = -1f;
        }
        if (Input.mousePosition.y < edgeScrollSize)
        {
            inputDir.z = -1f;
            inputDir.y = -1f;
        }
        if (Input.mousePosition.x > Screen.width - edgeScrollSize)
        {
            inputDir.x = +1f;
        }
        if (Input.mousePosition.y > Screen.height - edgeScrollSize)
        {
            inputDir.z = +1f;
            inputDir.y = +1f;
        }
        Vector3 moveDir = cam.transform.forward * inputDir.z + cam.transform.right * inputDir.x + cam.transform.up * inputDir.y;

        cam.transform.position += moveDir * cameraMoveSpeed * Time.deltaTime; // 카메라 이동
        mapBackground.transform.position += moveDir * cameraMoveSpeed * Time.deltaTime; // 맵 배경 이동
    }


    // 마우스 휠로 카메라 줌인, 줌아웃
    private void HandleMapCameraByMouseWheel()
    {
        float scrollWhell = -Input.GetAxis("Mouse ScrollWheel");
        if(scrollWhell < 0)
        {
            if(Camera.main.fieldOfView > minCameraFOV)
            {
                Camera.main.fieldOfView += scrollWhell * Time.deltaTime * scrollSpeed;
            }
        }
        else
        {
            if(Camera.main.fieldOfView < maxCameraFOV)
            {
                Camera.main.fieldOfView += scrollWhell * Time.deltaTime * scrollSpeed;
            }
        }
    }

    // 맵 플레이어 스왑 버튼 클릭
    public void HandleMapPlayerSwap(int swapTargetIndex)
    {
        int myIndex = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == M_MapManager.instance.ownedGamePlayer);
        if(myIndex != swapTargetIndex){ // 로컬유저 본인에 대한 요청은 제외
            if(M_TurnManager.instance.playerOrder[swapTargetIndex] == 0){
                M_MapManager.instance.CmdSwapMapPlayer(myIndex, swapTargetIndex); // 스왑 타겟이 빈슬롯이면 해당 슬롯으로 이동되도록 요청
            }else{
                if(NetworkClient.spawned.TryGetValue(M_TurnManager.instance.playerOrder[swapTargetIndex], out NetworkIdentity networkIdentity)){
                    GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                    MapPlayer mapPlayer = NetLookup.Client<MapPlayer>(gamePlayer.mapPlayerNetId);
                    if(mapPlayer.isOwned){
                        M_MapManager.instance.CmdSwapMapPlayer(myIndex, swapTargetIndex); // 스왑 타겟이 본인 소유면 요청없이 스왑
                    }else{
                        M_MapManager.instance.CmdRequestSwap(myIndex, swapTargetIndex); // 스왑 타겟이 본인 소유 아니면 요청 전송
                    }
                }
            }
        }
    }

    public void OnChangeReadyState()
    {
        PlayerRegistry.Local.isReady = !PlayerRegistry.Local.isReady;
    }


    // 거점지역 정보 팝업 활성화 및 팝업에 표시될 데이터 세팅
    public void RegionPopUpShow(Region region)
    {
        regionPopUp.SetActive(true);
        regionPopUp.transform.position = Input.mousePosition; // 팝업 위치는 마우스 위치로
        textRegionGradeInfo.text = region.regionGrade.ToString();
        // textRegionDesc.text = region.regionDescription.ToString();
        // 등급별 팝업 헤더이미지 및 텍스트 색상 변경
        switch(region.regionGrade){
            case RegionGrade.NORMAL :
                textRegionGradeInfo.color = new Color(1f, 0f, 0f);
                break;
            case RegionGrade.RARE :
                textRegionGradeInfo.color = new Color(0f, 1f, 0f);
                break;
            case RegionGrade.UNIQUE :
                textRegionGradeInfo.color = new Color(0f, 0f, 1f);
                break;
            case RegionGrade.LEGEND :
                textRegionGradeInfo.color = new Color(1f, 0.8f, 0f);
                break;      
        }
    }

    // 거점지역 정보 팝업 비활성화
    public void RegionPopUpHide()
    {
        regionPopUp.SetActive(false);
        textRegionGradeInfo.text = string.Empty;
        textRegionDesc.text = string.Empty;
    }

    // 맵 보스 출현시 맵정보 UI 상태 변경
    public void SetMapInfoStateMapBossApperance()
    {
        mapInfoBase.SetActive(true);
        mapInfoBaseLight.SetActive(true);
        mapInfoBaseLightSec.SetActive(true);
        mapInfoOverBase.SetActive(true);
        mapInfoOverBaseLight.SetActive(true);
        mapInfoOverBase.GetComponent<RectTransform>().localScale = new Vector3(1.05f, 1.05f, 1.05f);
        mapInfoOverBaseLight.GetComponent<RectTransform>().localScale = new Vector3(1.05f, 1.05f, 1.05f);
        mapDangerLayout.GetComponent<RectTransform>().localPosition = mapDangerLayoutPosition + new Vector3(0f, -10f, 0f);
        mapDangerLayout.GetComponent<MapDangerInfo>().originPosition = mapDangerLayoutPosition + new Vector3(0f, -10f, 0f);
        mapTurnLayout.GetComponent<RectTransform>().localPosition = mapTurnLayoutPosition + new Vector3(-10f, 3f, 0f);
        mapTurnLayout.GetComponent<MapTurnInfo>().originPosition = mapTurnLayoutPosition + new Vector3(-10f, 3f, 0f);
    }

    // 맵씬 스왑버튼 상태 변경
    public void ChangeSwapButtonsState(int index)
    { 
        for(int i=0; i<swapButtons.Count; i++){
            SwapButtonOnMap swapButtonOnMap = swapButtons[i].GetComponent<SwapButtonOnMap>();
            uint netId = M_TurnManager.instance.playerOrder[i];
            if(netId == 0){
                swapButtonOnMap.transform.DOScale(new Vector3(0.8f, 0.8f, 0.8f), 0.5f);
                swapButtonOnMap.transform.DORotate(new Vector3(0f, 0f, 45f), 0.5f);
            }else{
                swapButtonOnMap.transform.DOScale(new Vector3(1f, 1f, 1f), 0.5f);
                swapButtonOnMap.transform.DORotate(new Vector3(0f, 0f, 0f), 0.5f);
            }  
        }
    }

    // 맵씬 스왑버튼 아이콘 상태 변경
    public void ChangeSwapButtonsIconState()
    {
        for(int i=0; i<swapButtons.Count; i++){
            SwapButtonOnMap swapButtonOnMap = swapButtons[i].GetComponent<SwapButtonOnMap>();
            uint netId = M_TurnManager.instance.playerOrder[i];
            if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                if(gamePlayer.objectOwner.isReady){
                    swapButtonOnMap.t_Ready_Icon.SetActive(true);
                    swapButtonOnMap.t_Chan_Icon.SetActive(false);
                    swapButtonOnMap.t_Chan_Icon_Light.SetActive(false);
                    swapButtonOnMap.t_M_Icon.SetActive(false);
                    swapButtonOnMap.t_M_Icon_Light.SetActive(false);
                }else{
                    if(gamePlayer.isOwned){
                        swapButtonOnMap.t_M_Icon.SetActive(true);
                        swapButtonOnMap.t_Chan_Icon.SetActive(false);
                        swapButtonOnMap.t_Chan_Icon_Light.SetActive(false);
                        swapButtonOnMap.t_Ready_Icon.SetActive(false);
                        swapButtonOnMap.t_Ready_Icon_Light.SetActive(false);
                    }else{
                        swapButtonOnMap.t_Chan_Icon.SetActive(true);
                        swapButtonOnMap.t_M_Icon.SetActive(false);
                        swapButtonOnMap.t_M_Icon_Light.SetActive(false);
                        swapButtonOnMap.t_Ready_Icon.SetActive(false);
                        swapButtonOnMap.t_Ready_Icon_Light.SetActive(false);
                    }
                }
            }else{
                swapButtonOnMap.t_Chan_Icon.SetActive(true);
                swapButtonOnMap.t_M_Icon.SetActive(false);
                swapButtonOnMap.t_M_Icon_Light.SetActive(false);
                swapButtonOnMap.t_Ready_Icon.SetActive(false);
                swapButtonOnMap.t_Ready_Icon_Light.SetActive(false);
            }
        }
    }

    public void ChangeMapStageIcon(MapStage mapStage)
    {
        mapStageIcon.sprite = M_MapManager.instance.stageIcons[mapStage];
    }

    public void ChangeMapDimBackground(bool isActive)
    {
        StartCoroutine((ChangeMapDimBackgroundBlur(isActive)));
    }

    public void RemoveAllMapInfoPopUps()
    {
        foreach(MapInfoPopUpItem mapInfoPopUp in mapInfoPopUps){
            Destroy(mapInfoPopUp.gameObject);
        }
        mapInfoPopUps.Clear();
    }

    public void CreatDeckInfoPopUpItem(GamePlayerDeck gamePlayerDeck)
    {
        foreach(Card card in gamePlayerDeck.deck){
            GameObject cardObject = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab, Vector3.zero, Quaternion.identity);
            cardObject.transform.SetParent(MapUI.instance.gridLayoutGroup.transform);
            cardObject.transform.localScale = Vector3.one;
            CardOnDeck cardOnDeck = cardObject.GetComponent<CardOnDeck>();
            cardOnDeck.card = card.CardDeepCopy(false);
            deckInfoPopUpItems.Add(cardObject);
        }
    }

    public void ClearDeckInfoPopUpItem()
    {
        foreach(GameObject gameObject in deckInfoPopUpItems){
            Destroy(gameObject);
        }
        deckInfoPopUpItems.Clear();
    }

    public IEnumerator ChangeMapDimBackgroundBlur(bool isActive)
    {
        string propertyName = "_Blur";
        float duration = 0.5f;
        float startValue = isActive ? 0f : 10f;
        float endValue = isActive ? 10f : 0f;
        float elapsedTime = 0f;

        while (elapsedTime < duration){
            elapsedTime += Time.deltaTime;
            float currentValue = Mathf.Lerp(startValue, endValue, elapsedTime / duration);
            mapDimBackgroundMaterial.SetFloat(propertyName, currentValue);
            yield return null;
        }
        mapDimBackgroundMaterial.SetFloat(propertyName, endValue);
    }
}
