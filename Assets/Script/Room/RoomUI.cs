using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class RoomUI : MonoBehaviour
{
    public static RoomUI instance = null;

    // 캐릭터 선택 이벤트
    public delegate void OnCharacterSelect(GameObject gameObject);
    public event OnCharacterSelect onCharacterSelect;

    // 캐릭터 오브젝트 (인스펙터 창에서 캐릭터 리스트 생성 및 오브젝트 참조 해두는 방법으로 구현)
    public List<GameObject> characters;

    public VerticalLayoutGroup participantsLayout;
    public Button buttonStart;
    public Button buttonReady;
    public Button buttonSend;
    public TMP_InputField messageInput;
    public Scrollbar scrollbar;
    public TextMeshProUGUI chatMessage;


    public static RoomUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<RoomUI>();
                if (instance == null)
                {
                    GameObject container = new GameObject("RoomUISingleton");
                    instance = container.AddComponent<RoomUI>();
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        buttonStart.onClick.AddListener(() => HandleChangeGameScene());
        buttonSend.onClick.AddListener(() => SendChatMessage(messageInput.text));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return)){
            SendChatMessage(messageInput.text);
        }
    }

    // 게임 씬으로 이동
    public void HandleChangeGameScene()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        M_NetworkRoomManager.ServerChangeScene(M_NetworkRoomManager.GameplayScene);
    }

    // 캐릭터 선택 이벤트 송신
    public void EmitCharacterSelectEvent(GameObject gameObject)
    {
        if(onCharacterSelect != null){
            onCharacterSelect.Invoke(gameObject);
        }
    }

    // 채팅 메시지 전송
    public void SendChatMessage(string input)
    {
        if (NetworkClient.connection != null && !string.IsNullOrWhiteSpace(messageInput.text)){
            RoomPlayer roomPlayer = NetworkClient.connection.identity.gameObject.gameObject.GetComponent<RoomPlayer>();
            roomPlayer.CmdSend(messageInput.text.Trim());
            messageInput.ActivateInputField();
            messageInput.text = string.Empty;;
        }
    }

    public void AppendMessage(string message)
    {
        StartCoroutine(ScrollToBottom(message));
    }

    IEnumerator ScrollToBottom(string message)
    {
        chatMessage.text += message + "\n";

        // it takes 2 frames for the UI to update ?!?!
        yield return null;
        yield return null;

        scrollbar.value = 0;
    }
}
