using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using DG.Tweening;
using Steamworks;
using ProjectD;

public enum ToastPosition
{
    Top,
    Bottom
}

public class M_MessageManager : NetworkSingletonD<M_MessageManager>
{
    private StringBuilder chatStringBuilder;

    [Header("토스트 메시지 컴포넌트용 필드")]
    public GameObject toastMessageCanvas;
    public GameObject toastMessageLayout;
    public GameObject toastMessagePrefab;
    public List<GameObject> toastMessages = new List<GameObject>();

    [Header("채팅창 컴포넌트용 필드")]
    public GameObject chatCanvas;
    public ScrollRect scrollRect;
    public TextMeshProUGUI chatMessage;
    public TMP_InputField messageInput;
    public RectTransform chatMessageBoxRect;
    public RectTransform chatMessageInputRect;
    public RectTransform buttonChatBoxRect;
    public bool isChatBoxVisible = false; // 채팅창 활성화 상태 여부
    public bool isMouseOnChatBox = false; // 현재 마우스 포인터가 채팅메시지 박스 위에 있는지 여부

    [Header("ChatBoxVisibilityButton")]
    public GameObject chatNotificationIcon;
    public GameObject chatNotificationIconLight; // 애니매이션으로 무한루프로 깜빡이도록

    // Dotween 참조값
    private Tween chatMessageBoxTween;
    private Tween chatInputTween;
    private Tween buttonChatBoxTween;
    private Tween fadeInTweener;
    private Tweener fadeOutTweener;
    private Sequence notiLoopSequence;

    protected override void Start()
    {
        // 메시지매니저, 토스트 캔버스, 채팅 캔버스를 DDOL로 전환하고, 네트워크 매니저에 관리용 리스트에 해당 값들 추가하여 관리되도록 할당
        DontDestroyOnLoad(gameObject);
        DontDestroyOnLoad(toastMessageCanvas);
        DontDestroyOnLoad(chatCanvas);
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.persistentManagers.Add(gameObject.name, gameObject);
        networkRoomManager.persistentComponents.Add(toastMessageCanvas.name, toastMessageCanvas);
        networkRoomManager.persistentComponents.Add(chatCanvas.name, chatCanvas);
        chatStringBuilder = new StringBuilder();
    }

    void OnDestroy()
    {
        chatMessageBoxTween.Kill();
        chatInputTween.Kill();
        buttonChatBoxTween.Kill();
        fadeInTweener.Kill();
        fadeOutTweener.Kill();
        notiLoopSequence.Kill();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Return)){
            messageInput.ActivateInputField();
            if(isChatBoxVisible){
                SendChatMessage(messageInput.text); // 채팅창이 활성화 상태에서 엔터키를 누르면 채팅 메시지 전송
            }else{
                ChangeChatBoxVisibileState(); // 채팅창이 비활성화 상태에서 엔터키 누르면 채팅창 활성화
            }
        }
        if(isMouseOnChatBox){
            HandleChatMessageScrollBarByMouseWheel();
        }
    }

    // 토스트 메시지 생성
    public ToastMessage MakeToast()
    {
        GameObject toastMessageObject = Instantiate(toastMessagePrefab, Vector3.zero, Quaternion.identity, toastMessageLayout.transform);
        ToastMessage toastMessage = toastMessageObject.GetComponent<ToastMessage>();
        toastMessages.Add(toastMessageObject);
        return toastMessage;
    }

    // 채팅 내용 세팅
    public void AppendMessage(Color color,string playerName, string message)
    {
        chatStringBuilder.Append($"<size=35><color={ColorUtils.ColorToHex(color)}>{playerName}</color></size> : {message}\n");
        chatMessage.SetText(chatStringBuilder);
        StartCoroutine(ScrollToBottom());
    }

    // 스크롤 이동
    IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame(); // 스크롤뷰의 컨텐츠의 크기가 변경되고 한 프레임이 끝날때까지 지연
        scrollRect.normalizedPosition = new Vector2(0, 0); // 스크롤바를 맨 아래로 이동
    }

    // 채팅창 스크롤바 마우스 휠로 컨트롤
    private void HandleChatMessageScrollBarByMouseWheel()
    {
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        float scrollValue = scrollDelta * 1f;
        scrollRect.verticalNormalizedPosition += scrollValue;
    }

    // 채팅창 Visible 상태 변경(이벤트는 인스펙터에 할당되어 있음)
    public void ChangeChatBoxVisibileState()
    {
        if(!DeckBookUI.instance.dekcBookMenu.activeSelf){
            isChatBoxVisible = !isChatBoxVisible;
            if(isChatBoxVisible){
                chatMessageBoxTween = chatMessageBoxRect.DOAnchorPosX(chatMessageBoxRect.rect.width / 2, 0.5f);
                chatInputTween = chatMessageInputRect.DOAnchorPosX(chatMessageBoxRect.rect.width / 2, 0.5f);
                buttonChatBoxTween = buttonChatBoxRect.DOAnchorPosX(875f, 0.5f);
                chatNotificationIcon.SetActive(false);
                chatNotificationIconLight.SetActive(false);
                fadeInTweener.Kill();
                fadeOutTweener.Kill();
                notiLoopSequence.Kill();
            }else{
                chatMessageBoxTween = chatMessageBoxRect.DOAnchorPosX(-chatMessageBoxRect.rect.width / 2, 0.5f);
                chatInputTween =  chatMessageInputRect.DOAnchorPosX(-chatMessageBoxRect.rect.width / 2, 0.5f);
                buttonChatBoxTween = buttonChatBoxRect.DOAnchorPosX(150f, 0.5f);
            }
        }
    }

    public void SendChatMessage(string message)
    {
       if(!string.IsNullOrWhiteSpace(message)){
            M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
            if(Utils.IsSceneActive(networkRoomManager.RoomScene)){
                RoomPlayer roomPlayer = NetworkClient.localPlayer.GetComponent<RoomPlayer>();
                Color color = roomPlayer.color;
                string playerName = SteamFriends.GetFriendPersonaName((CSteamID)roomPlayer.steamID);
                CmdSendChatMessage(color, playerName, message);
            }else if(Utils.IsSceneActive(networkRoomManager.GameplayScene)){
                PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
                Color color = playerInterface.color;
                string playerName = SteamFriends.GetFriendPersonaName((CSteamID)playerInterface.steamID);
                CmdSendChatMessage(color, playerName, message);
            }
            messageInput.text = string.Empty;
            messageInput.ActivateInputField();
        }
    }

    // ------------------------------------------------------------ Command Method ---------------------------------------------------------------- //

    // 채팅 메시지 전송
    [Command(requiresAuthority = false)]
    public void CmdSendChatMessage(Color color, string playerName, string message, NetworkConnectionToClient sender = null)
    {
        RpcReceiveChatMessage(color, playerName, message);
    }

    // ------------------------------------------------------------ ClientRpc Method -------------------------------------------------------------- //

    // 채팅 메시지 이벤트 수신
    [ClientRpc]
    void RpcReceiveChatMessage(Color color, string playerName, string message)
    {
        AppendMessage(color, playerName, message);
        if(!isChatBoxVisible){
            chatNotificationIcon.SetActive(true);
            chatNotificationIconLight.SetActive(true);
            fadeInTweener = chatNotificationIconLight.GetComponent<Image>().DOFade(0f, 0.3f);
            fadeOutTweener = chatNotificationIconLight.GetComponent<Image>().DOFade(1f, 0.3f);
            notiLoopSequence = DOTween.Sequence()
                .Append(fadeInTweener)
                .Append(fadeOutTweener)
                .SetLoops(-1, LoopType.Restart);
        }
    }

    // 룸씬에서 다른 클라 연결해제 이벤트 수신
    [ClientRpc]
    public void RpcOtherPlayerDisconnectedInRoomScene(string oldOwner, string newOwner)
    {
        MakeToast()
        .Position(ToastPosition.Bottom)
        .MessageBoxColor(Color.red)
        .TextColor(Color.white)
        .Text($"{oldOwner} 님이 대기방을 나갔습니다.")
        .Show();
    }

    // 게임씬에서 다른 클라 연결해제 이벤트 수신
    [ClientRpc]
    public void RpcOtherPlayerDisconnectedInGameScene(string oldPlayer ,string newPlayer)
    {
        MakeToast()
        .Position(ToastPosition.Bottom)
        .MessageBoxColor(Color.red)
        .TextColor(Color.white)
        .Text($"{oldPlayer} 님이 게임을 나갔습니다.\n{newPlayer} 님에게 권한이 이전됩니다.")
        .Show();
    }
}
