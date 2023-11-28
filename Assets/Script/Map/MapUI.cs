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
    public GameObject[] orderSelected;
    public GameObject[] playerProfiles;
    public GameObject regionPopUp;

    [Header("UI 컴포넌트")]
    public Image regionPopUpHeader;
    public TextMeshProUGUI textRegionGradeInfo;
    public TextMeshProUGUI textRegionDesc;
    public TextMeshProUGUI textCurrentActionCost;
    public TextMeshProUGUI textMaxActionCostCount;
    public Image turnGageBar;

    public List<GameObject> topIcons = new List<GameObject>();
    public List<Button> swapButtons = new List<Button>();

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

    [Header("카메라 이동 속도")]
    float cameraMoveSpeed = 20;


    void Start()
    {
        for(int i=0; i<swapButtons.Count; i++){
            int buttonIndex = i;
            swapButtons[i].transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            swapButtons[i].transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            swapButtons[i].onClick.AddListener(() => HandleMapPlayerSwap(buttonIndex));
        }
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
        int myIndex = M_MapManager.instance.mapPlayers.FindIndex((netId) => netId == M_MapManager.instance.ownedMapPlayer);
        if(myIndex != swapTargetIndex){ // 로컬유저 본인에 대한 요청은 제외
            if(M_MapManager.instance.mapPlayers[swapTargetIndex] == 0){
                M_MapManager.instance.CmdSwapMapPlayer(myIndex, swapTargetIndex); // 스왑 타겟이 빈슬롯이면 해당 슬롯으로 이동되도록 요청
            }else{
                if(NetworkClient.spawned.TryGetValue(M_MapManager.instance.mapPlayers[swapTargetIndex], out NetworkIdentity networkIdentity)){
                    MapPlayer mapPlayer = networkIdentity.GetComponent<MapPlayer>();
                    if(mapPlayer.isOwned){
                        M_MapManager.instance.CmdSwapMapPlayer(myIndex, swapTargetIndex); // 스왑 타겟이 본인 소유면 요청없이 스왑
                    }else{
                        M_MapManager.instance.CmdRequestSwap(myIndex, swapTargetIndex); // 스왑 타겟이 본인 소유 아니면 요청 전송
                    }
                }
            }
        }
    }

    // 스왑버튼 상태 변경
    public void ChangeSwapButtonsState(uint netId, int index)
    {
        if(netId == 0){
            swapButtons[index].transform.DOScale(new Vector3(0.8f, 0.8f, 0.8f), 0.5f);
            swapButtons[index].transform.DORotate(new Vector3(0f, 0f, 45f), 0.5f);
        }else{
            swapButtons[index].transform.DOScale(new Vector3(1f, 1f, 1f), 0.5f);
            swapButtons[index].transform.DORotate(new Vector3(0f, 0f, 0f), 0.5f);
        }  
    }

    public void SetOrderIndicator(int order)
    {
        orderSelected[0].SetActive(order == 0 ? true : false);
        orderSelected[1].SetActive(order == 1 ? true : false);
        orderSelected[2].SetActive(order == 2 ? true : false);
    }

    public void UpdateProfile()
    {
        PlayerInterface[] users = FindObjectsOfType<PlayerInterface>();
        foreach(PlayerInterface user in users)
        {
            if(M_LoadingManager.instance.state != LOADING_STATE.MAP_SCENE)return;
            // Avatar
            if(user.isAvatarUploadDone)
            {
                byte[] avatarImage = new byte[user.avatarWidth * user.avatarHeight * 4];
                for(int i = 0 ;i < user.avatarImage.Count ; i++)
                    avatarImage[i] = user.avatarImage[i];
                playerProfiles[user.selectOrder].transform.GetChild(6).GetComponent<RawImage>().texture = M_SteamManager.instance.GetSteamImageAsTexture(avatarImage,user.avatarWidth,user.avatarHeight);
            }
            playerProfiles[user.selectOrder].transform.GetChild(6).GetComponent<RawImage>().color = new Color(1,1,1,1);
            // Show ID
            playerProfiles[user.selectOrder].transform.GetChild(5).GetComponent<TextMeshProUGUI>().text = SteamFriends.GetFriendPersonaName((CSteamID)user.steamID);
            // Show Ready State
            playerProfiles[user.selectOrder].transform.GetChild(1).gameObject.SetActive(user.isReady == true ? true : false);
            // HP (Right 195 -> 0 : 0 -> Max)
            
            /*
            TODO
            playerProfiles[user.selectOrder].transform.GetChild(3).GetChild(0).GetComponent<RectTransform>().offsetMax = 
                new Vector2(( 195 * user.HP / user.MaxHP ) - 195,playerProfiles[user.selectOrder].transform.GetChild(3).GetChild(0).GetComponent<RectTransform>().offsetMax.y);
            */
            
            // Ichi
        }
    }

    public void OnChangeReadyState()
    {
        NetworkClient.localPlayer.GetComponent<PlayerInterface>().isReady = !NetworkClient.localPlayer.GetComponent<PlayerInterface>().isReady;
        UpdateProfile();
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
                regionPopUpHeader.color = new Color(1f, 0f, 0f);
                textRegionGradeInfo.color = new Color(1f, 0f, 0f);
                break;
            case RegionGrade.RARE :
                regionPopUpHeader.color = new Color(0f, 1f, 0f);
                textRegionGradeInfo.color = new Color(0f, 1f, 0f);
                break;
            case RegionGrade.UNIQUE :
                regionPopUpHeader.color = new Color(0f, 0f, 1f);
                textRegionGradeInfo.color = new Color(0f, 0f, 1f);
                break;
            case RegionGrade.LEGEND :
                regionPopUpHeader.color = new Color(1f, 0.8f, 0f);
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
}
