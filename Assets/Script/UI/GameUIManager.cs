using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;
using TMPro;
using ProjectD;
using Steamworks;

public class GameUIManager : SingletonD<GameUIManager>
{
    [Header("게임 오브젝트")]
    public GameObject RootGameObject;
    public GameObject GameUI;
    public GameObject GameBackGround;
    public GameObject DeckListPanel;
    public GameObject CardOnHandsPanel;
    public GameObject PrefareDeck;
    public GameObject TrashDeck;
    public GameObject ChatUI;
    public GameObject TestUI;

    [Header("UI 컴포넌트")]

    // 카드 전투 UI
    public Button buttonEndTurn;
    public Button buttonPrefareDeck;
    public Button buttonTrashDeck;
    public Text textPrefareDeckCount;
    public Text textTrashDeckCount;

    // 채팅 UI
    public TextMeshProUGUI chatMessage;
    public TMP_InputField messageInput;
    public ScrollRect scrollRect;


    [Header("화면 Dim 처리용 이미지")]
    public Image blackCurtain;

    [Header("플레이어 리스트(플레이어 정보 및 턴 정보)")]
    public List<GameObject> playerOrderList;

    [Header("플레이어 코스트 (현재이치/최대이치) 디스플레이")]
    public TextMeshProUGUI ichiText;

    void Update()
    {
        HandleChatMessageInput();
        HandleChatMessageScrollBarByMouseWheel();
    }

    // 턴 종료
    public void HandleEndTurn()
    {
        NetworkClient.localPlayer.GetComponent<PlayerInterface>().endTurnActive = !NetworkClient.localPlayer.GetComponent<PlayerInterface>().endTurnActive;
    }


    // 댁 카운트 텍스트 컴포넌트들의 크기 변경 애니매이션(댁 카운트 변경 시 크기 커졌다 작아지는 애니매이션)
    public void DeckCountTextScaleAnimation(Text textComponent, int count)
    {
        Vector3 chagenScale = new Vector3(2f, 2f, 2f);
        Vector3 originScale = new Vector3(1f, 1f, 1f);
        textComponent.text = count.ToString();
        textComponent.transform.DOScale(chagenScale, 0.1f).SetEase(Ease.OutQuad)
        .OnComplete(() =>
        {
            textComponent.transform.DOScale(originScale, 0.1f).SetEase(Ease.InQuad);
        });
    }

    // 화면 전체 Dim 효과용 이미지 컴포넌트 Fade 애니매이션
    public void FadeBlackCurtain(System.Action<Image> callback = null)
    {
        blackCurtain.gameObject.SetActive(true);
        blackCurtain.DOFade(1.0f, 0.5f).OnComplete(() => {
            if(callback != null){
                callback(blackCurtain);
            }
        }); 
    }

    // 마우스 휠로 채팅창 스크롤 이동
    private void HandleChatMessageScrollBarByMouseWheel()
    {
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        float scrollValue = scrollDelta * 1f;
        scrollRect.verticalNormalizedPosition += scrollValue;
    }

    // Enter 키로 채팅 메시지 입력
    private void HandleChatMessageInput()
    {
        if(Input.GetKeyDown(KeyCode.Return)){
            SendChatMessage(messageInput.text);
            messageInput.ActivateInputField();       
        }
    }

    // 채팅 메시지 전송
    public void SendChatMessage(string input)
    {
        if (NetworkClient.connection != null && !string.IsNullOrWhiteSpace(messageInput.text)){
            PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
            playerInterface.CmdSendChatMessageGameScene(messageInput.text.Trim());
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

}