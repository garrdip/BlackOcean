using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using ProjectD;
using Steamworks;

public class MapUI : InstanceD<MapUI>
{
    public GameObject[] orderSelected;
    public GameObject[] playerProfiles;
    public GameObject regionPopUp;

    [Header("UI 컴포넌트")]
    public Button readyButton;
    public Scrollbar scrollbar;
    public TextMeshProUGUI chatMessage;
    public TMP_InputField messageInput;
    public ScrollRect scrollRect;
    public Image regionPopUpHeader;
    public TextMeshProUGUI textRegionGradeInfo;
    public TextMeshProUGUI textRegionDesc;
    public TextMeshProUGUI textTotalActionCostCount;

    [Header("현재 마우스 포인터가 채팅메시지 박스 위에 있는지 여부")]
    public bool isMouseOnChatBox = false;

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

    public void Start()
    {
        readyButton.onClick.AddListener(() => OnChangeReadyState());
    }

    void Update()
    { 
        if(Application.isFocused){
            //HandleCameraEdgeScrolling();
            HandleChatMessageInput();
            if(isMouseOnChatBox){
                HandleChatMessageScrollBarByMouseWheel();
            }else{
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

    // Enter 키로 채팅 메시지 입력
    private void HandleChatMessageInput()
    {
        if(Input.GetKeyDown(KeyCode.Return)){
            SendChatMessage(messageInput.text);
            messageInput.ActivateInputField();       
        }
    }

    // 마우스 휠로 채팅 메시지 스크롤 이동
    private void HandleChatMessageScrollBarByMouseWheel()
    {
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        float scrollValue = scrollDelta * 1f;
        scrollRect.verticalNormalizedPosition += scrollValue;
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

    public void SetOrderIndicator(int order)
    {
        orderSelected[0].SetActive(order == 0 ? true : false);
        orderSelected[1].SetActive(order == 1 ? true : false);
        orderSelected[2].SetActive(order == 2 ? true : false);
    }

    public void UpdateProfile()
    {
        GamePlayer[] users = FindObjectsOfType<GamePlayer>();
        foreach(GamePlayer user in users)
        {
            if(!user.isInitializeDone)return;
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
            playerProfiles[user.selectOrder].transform.GetChild(3).GetChild(0).GetComponent<RectTransform>().offsetMax = 
            new Vector2(( 195 * user.HP / user.MaxHP ) - 195,playerProfiles[user.selectOrder].transform.GetChild(3).GetChild(0).GetComponent<RectTransform>().offsetMax.y);
            // Ichi
        }
    }

    public void OnChangeReadyState()
    {
        NetworkClient.localPlayer.GetComponent<GamePlayer>().isReady = !NetworkClient.localPlayer.GetComponent<GamePlayer>().isReady;
        UpdateProfile();
    }

    // 채팅 메시지 전송
    public void SendChatMessage(string input)
    {
        if (NetworkClient.connection != null && !string.IsNullOrWhiteSpace(messageInput.text)){
            GamePlayer gamePlayer = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayer>();
            gamePlayer.CmdSendChatMessage(messageInput.text.Trim());
            messageInput.ActivateInputField();
            messageInput.text = string.Empty;;
        }
    }

    // 채팅 메시지 추가
    public void AppendMessage(Color color, string playerName, string message)
    {
        chatMessage.text += $"<size=18><color={ColorUtils.ToHex(color)}>{playerName}</color></size> : {message}\n";
        StartCoroutine(ScrollToBottom());
    }

    // 스크롤 이동
    IEnumerator ScrollToBottom()
    {
        // 스크롤뷰의 컨텐츠의 크기가 변경되고 한 프레임이 끝날때까지 지연
        yield return new WaitForEndOfFrame();

        // 스크롤바를 맨 아래로 이동
        scrollRect.normalizedPosition = new Vector2(0, 0);
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
