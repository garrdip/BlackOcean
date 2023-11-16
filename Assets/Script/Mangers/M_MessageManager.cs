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
    public CanvasGroup canvasGroup;
    public Image toastMessageContainer;
    public TextMeshProUGUI toastMessageText;
    public float fadeInTime = 1.0f;
    public float fadeOutTime = 1.0f;

    [Header("채팅창 컴포넌트용 필드")]
    public GameObject chatCanvas;
    public GameObject chatContainer;
    public ScrollRect scrollRect;
    public TextMeshProUGUI chatMessage;
    public TMP_InputField messageInput;
    public RectTransform chatMessageBoxRect;
    public RectTransform chatMessageInputRect;
    public RectTransform buttonChatBoxRect;
    public bool isChatBoxVisible = false; // 채팅창 활성화 상태 여부
    public bool isMouseOnChatBox = false; // 현재 마우스 포인터가 채팅메시지 박스 위에 있는지 여부

    protected override void Start()
    {
        DontDestroyOnLoad(gameObject);
        DontDestroyOnLoad(toastMessageCanvas); // 토스트 메시지 캔버스 오브젝트는 모든 씬에 필요하므로, 메시지 매니저 초기화 시점에 DontDestroyOnLoad 호출
        DontDestroyOnLoad(chatCanvas); // 채팅 캔버스 오브젝트는 모든 씬에 필요하므로, 메시지 매니저 초기화 시점에 DontDestroyOnLoad 호출
        chatStringBuilder = new StringBuilder();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Return)){
            messageInput.ActivateInputField();
            if(isChatBoxVisible){
                CmdSendChatMessage(messageInput.text); // 채팅창이 활성화 상태에서 엔터키를 누르면 채팅 메시지 전송
            }else{
                ChangeChatBoxVisibileState(); // 채팅창이 비활성화 상태에서 엔터키 누르면 채팅창 활성화
            }
        }
        if(isMouseOnChatBox){
            HandleChatMessageScrollBarByMouseWheel();
        }
    }

    // 토스트 메시지의 외곽 박스 색상 설정
    public M_MessageManager MessageBoxColor(Color color)
    {
        toastMessageContainer.color = color;
        return this;
    }

    // 토스트 메시지 FadeIn Time 설정
    public M_MessageManager FadeInTime(float time)
    {
        fadeInTime = time;
        return this;
    }

    // 토스트 메시지 FadeOut Time 설정
    public M_MessageManager FadeOutTime(float time)
    {
        fadeOutTime = time;
        return this;
    }

    // 토스트 메시지 위치 설정
    public M_MessageManager Position(ToastPosition position)
    {
        RectTransform canvasRectTransform = canvasGroup.GetComponent<RectTransform>();
        switch (position)
        {
            case ToastPosition.Top:
                canvasRectTransform.anchorMin = new Vector2(0.5f, 1);
                canvasRectTransform.anchorMax = new Vector2(0.5f, 1);
                canvasRectTransform.pivot = new Vector2(0.5f, 1);
                canvasRectTransform.anchoredPosition = new Vector2(canvasRectTransform.anchoredPosition.x, -250f);
                break;
            case ToastPosition.Bottom:
                canvasRectTransform.anchorMin = new Vector2(0.5f, 0);
                canvasRectTransform.anchorMax = new Vector2(0.5f, 0);
                canvasRectTransform.pivot = new Vector2(0.5f, 0);
                canvasRectTransform.anchoredPosition = new Vector2(canvasRectTransform.anchoredPosition.x, 250f);
                break;
        }
        return this;
    }

    // 토스트 메시지 텍스트 설정
    public M_MessageManager Text(string text)
    {
        toastMessageText.text = text;
        return this;
    }

    // 토스트 메시지 텍스트 색상설정
    public M_MessageManager TextColor(Color color)
    {
        toastMessageText.color = color;
        return this;
    }

    // 토스트 메시지 출력 후 사라짐
    public void Show()
    {
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.DOFade(1.0f, fadeInTime).OnComplete(() => {
            canvasGroup.DOFade(0.0f, fadeOutTime).OnComplete(() => {
            canvasGroup.gameObject.SetActive(false);
            canvasGroup.transform.DOKill();
            toastMessageText.text = string.Empty;
            });
        });
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
        isChatBoxVisible = !isChatBoxVisible;
        if(isChatBoxVisible){
            chatMessageBoxRect.DOAnchorPosX(chatMessageBoxRect.rect.width / 2, 0.5f);
            chatMessageInputRect.DOAnchorPosX(chatMessageBoxRect.rect.width / 2, 0.5f);
            buttonChatBoxRect.DOAnchorPosX((chatMessageBoxRect.rect.width) + (buttonChatBoxRect.rect.width / 2), 0.5f);
        }else{
            chatMessageBoxRect.DOAnchorPosX(-chatMessageBoxRect.rect.width / 2, 0.5f);
            chatMessageInputRect.DOAnchorPosX(-chatMessageBoxRect.rect.width / 2, 0.5f);
            buttonChatBoxRect.DOAnchorPosX((buttonChatBoxRect.rect.width / 2), 0.5f);
        }
    }

    // ------------------------------------------------------------ Command Method ---------------------------------------------------------------- //

    // 채팅 메시지 전송
    [Command(requiresAuthority = false)]
    public void CmdSendChatMessage(string message, NetworkConnectionToClient sender = null)
    {
        if(!string.IsNullOrWhiteSpace(message)){
            M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
            if(Utils.IsSceneActive(networkRoomManager.RoomScene)){
                RoomPlayer roomPlayer = NetworkClient.localPlayer.GetComponent<RoomPlayer>();
                Color color = roomPlayer.color;
                string playerName = SteamFriends.GetFriendPersonaName((CSteamID)roomPlayer.steamID);
                RpcReceiveChatMessage(color, playerName, message);
            }else if(Utils.IsSceneActive(networkRoomManager.GameplayScene)){
                PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
                Color color = playerInterface.color;
                string playerName = SteamFriends.GetFriendPersonaName((CSteamID)playerInterface.steamID);
                RpcReceiveChatMessage(color, playerName, message);
            }
        }
    }

    // ------------------------------------------------------------ ClientRpc Method -------------------------------------------------------------- //

    // 채팅 메시지 이벤트 수신
    [ClientRpc]
    void RpcReceiveChatMessage(Color color, string playerName, string message)
    {
        AppendMessage(color, playerName, message);
        messageInput.ActivateInputField();
        messageInput.text = string.Empty;;
    }

    // 룸씬에서 다른 클라 연결해제 이벤트 수신
    [ClientRpc]
    public void RpcOtherPlayerDisconnectedInRoomScene(string oldOwner, string newOwner)
    {
        /*
        M_MessageManager.instance
            .Position(ToastPosition.Bottom)
            .MessageBoxColor(Color.red)
            .TextColor(Color.white)
            .Text($"{oldOwner} 님이 대기방을 나갔습니다.\n{newOwner} 님에게 권한이 이전됩니다.")
            .Show();
            */
    }

    // 게임씬에서 다른 클라 연결해제 이벤트 수신
    [ClientRpc]
    public void RpcOtherPlayerDisconnectedInGameScene(string oldPlayer ,string newPlayer)
    {
        /*
        M_MessageManager.instance
            .Position(ToastPosition.Bottom)
            .MessageBoxColor(Color.red)
            .TextColor(Color.white)
            .Text($"{oldPlayer} 님이 게임을 나갔습니다.\n{newPlayer} 님에게 권한이 이전됩니다.")
            .Show();
            */
    }
}
