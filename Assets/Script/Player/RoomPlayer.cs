using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using ProjectD;

public class RoomPlayer : NetworkRoomPlayer
{
    [SyncVar(hook = nameof(OnSelectCharacterChanged))]
    public Character character = Character.NONE;

    [SyncVar(hook = nameof(OnReadyStatusChanged))]
    public bool isReady = false;


    // 로컬 플레이어로 시작 시 RoomUI의 레디 상태 변경 버튼이벤트 리스너 할당
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        InitReadyButton();
        RoomUI.instance.onCharacterSelect += OnCharacterSelected; // 캐릭터 선택 델리게이트 연결
    }

    // 클라 + 호스트 시작 시 RoomPlayer오브젝트를 참가자 목록 레이아웃 하위로 설정해 리스트 되도록 세팅
    public override void OnStartClient()
    {
        base.OnStartClient();
        if(RoomUI.instance != null){
            transform.SetParent(RoomUI.instance.participantsLayout.transform);
        }
    }

    // 레디 상태 변경 버튼 클릭 이벤트
    // 커스텀 레디 상태 커맨드와 부모클래스인 NetworkRoomManger 클래스의 레디 상태 커맨드 함수 병행 사용 
    // 이유 : OnRoomServerPlayersReady, OnRoomServerPlayersNotReady 콜백함수 사용하기 위해
    private void InitReadyButton()
    {
        RoomUI.instance.buttonReady.onClick.AddListener(() => {
            if(isLocalPlayer){
                if (base.readyToBegin)
                {
                    CmdChangeReadyStatus(false);
                    base.CmdChangeReadyState(false);
                }
                else
                {
                    CmdChangeReadyStatus(true);
                    base.CmdChangeReadyState(true);
                }
            }
        });
    }

    // RoomPlayer오브젝트의 뷰 요소들 초기 세팅
    private void InitRoomPlayerViewComponents()
    {
        transform.SetParent(RoomUI.instance.participantsLayout.transform); // 대기방 입장 시 RoomPlayer오브젝트를 참가자 패널UI 오브젝트의 하위오브젝트로 설정
        TextMeshProUGUI selectedCharacterNameText = gameObject.transform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>();
        selectedCharacterNameText.text = "Select Your Character";
        TextMeshProUGUI buttonReadyText = gameObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        buttonReadyText.text = "Idle";
    }

    // RoomUI 클래스로부터 캐릭터 선택 델리게이트 이벤트를 수신하여, 선택한 캐릭터 정보 서버에 송신
    public void OnCharacterSelected(GameObject selectedGameObject)
    {
        if(isLocalPlayer){
            switch(selectedGameObject.name){
                case "Geork":
                    CmdChangeCharacter(Character.GEORK);
                    break;
                case "Eris":
                    CmdChangeCharacter(Character.ERIS);
                    break;
                case "Danhyang":
                    CmdChangeCharacter(Character.HONGDANHYANG);
                    break;
                default:
                    break;
            }
        }
    }

    private void SetDisableAlradySelectedCharacter(Character character)
    {
        if(RoomUI.instance != null){
            switch(character){
                case Character.GEORK:
                    RoomUI.instance.characters.Find((character) => character.name.Equals("Geork")).SetActive(false);
                    break;
                case Character.ERIS:
                    RoomUI.instance.characters.Find((character) => character.name.Equals("Eris")).SetActive(false);
                    break;
                case Character.HONGDANHYANG:
                    RoomUI.instance.characters.Find((character) => character.name.Equals("Danhyang")).SetActive(false);
                    break;
                default:
                    RoomUI.instance.characters.Find((character) => character.name.Equals("Geork")).SetActive(true);
                    RoomUI.instance.characters.Find((character) => character.name.Equals("Eris")).SetActive(true);
                    RoomUI.instance.characters.Find((character) => character.name.Equals("Danhyang")).SetActive(true);
                break;
            }
        }
    }

    // 커스텀 레디 상태 이벤트 송신
    [Command]
    private void CmdChangeReadyStatus(bool readyStatus)
    {
        isReady = readyStatus;
    }

    // 커스텀 레디 상태 이벤트 수신
    private void OnReadyStatusChanged(bool oldStatus, bool newStatus)
    {
        TextMeshProUGUI readyStateText = gameObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        if (newStatus)
        {
            if(isLocalPlayer){
                RoomUI.instance.buttonReady.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.red;
            }
            readyStateText.color = Color.red;
            readyStateText.text = "Ready";
        }
        else
        {
            if(isLocalPlayer){
                RoomUI.instance.buttonReady.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.black;
            }
            readyStateText.color = Color.black;
            readyStateText.text = "Idle";
        }
    }

    // 캐릭터 선택 이벤트 송신
    [Command]
    private void CmdChangeCharacter(Character selectedCharacter)
    {
        character = selectedCharacter;
    }

    // 캐릭터 선택 이벤트 수신
    private void OnSelectCharacterChanged(Character oldCharacter, Character newCharacter)
    {
        TextMeshProUGUI selectedCharacterNameText = gameObject.transform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>();
        selectedCharacterNameText.text = newCharacter.ToString();
        Debug.Log("선택된 캐릭터 :" + newCharacter);
        SetDisableAlradySelectedCharacter(newCharacter);
    }

    [Command]
    public void CmdSend(string message, NetworkConnectionToClient sender = null)
    {
        if (!string.IsNullOrWhiteSpace(message))
            RpcReceive("테스트", message.Trim());
    }

    [ClientRpc]
    void RpcReceive(string playerName, string message)
    {
        Debug.Log(playerName + "의 메시지 : "+ message);
        if(RoomUI.instance != null){
            RoomUI.instance.AppendMessage(message);
        }
    }

}
