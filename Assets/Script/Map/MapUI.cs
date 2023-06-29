using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using Steamworks;

public class MapUI : InstanceD<MapUI>
{
    public GameObject[] orderSelected;
    public GameObject[] playerProfiles;

    [Header("UI 컴포넌트")]
    public Button readyButton;
    public Scrollbar scrollbar;
    public TextMeshProUGUI chatMessage;
    public TMP_InputField messageInput;
    public ScrollRect scrollRect;

    [Header("현재 마우스 포인터가 채팅메시지 박스 위에 있는지 여부")]
    public bool isMouseOnChatBox = false;

    [Header("맵 화면의 카메라 줌 조절 변수값")]
    [SerializeField]
    private float minCameraFOV = 30f;
    [SerializeField]
    private float maxCameraFOV = 90f;
    [SerializeField]
    private float scrollSpeed = 10000f;

    public void Start()
    {
        readyButton.onClick.AddListener(() => OnChangeReadyState());
    }

    void Update()
    { 
        if (Input.GetKeyDown(KeyCode.Return)){
            SendChatMessage(messageInput.text);
            messageInput.ActivateInputField();       
        }
        if(isMouseOnChatBox){
            HandleChatMessageScrollBarByMouseWheel();
        }else{
            HandleMapCameraByMouseWheel();
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
    public void HandleMapCameraByMouseWheel()
    {
        float scrollWhell = -Input.GetAxis("Mouse ScrollWheel");
        if(scrollWhell < 0)
        {
            if(Camera.main.fieldOfView > 30)
            {
                Camera.main.fieldOfView += scrollWhell * Time.deltaTime * scrollSpeed;
            }
        }
        else
        {
            if(Camera.main.fieldOfView < 90)
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
            int imageId = SteamFriends.GetLargeFriendAvatar((CSteamID)user.steamID);
            playerProfiles[user.selectOrder].transform.GetChild(6).GetComponent<RawImage>().texture = M_SteamManager.instance.GetSteamImageAsTexture(imageId);
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
    public void AppendMessage(string playerName, string message)
    {
        chatMessage.text += playerName + " : " + message + "\n";
        StartCoroutine(ScrollToBottom());
    }

    // 스크롤 이동
    IEnumerator ScrollToBottom()
    {
        // it takes 2 frames for the UI to update ?!?!
        yield return null;
        yield return null;

        scrollbar.value = 0;
    }
}
