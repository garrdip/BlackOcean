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


    // 로컬 플레이어로 시작 시 RoomUI의 레디 상태 변경 버튼이벤트 리스너 할당
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        if(RoomUI.instance != null){
            RoomUI.instance.onCharacterSelect += OnCharacterSelected; // 캐릭터 선택 델리게이트 연결
            transform.GetComponent<Image>().color = Color.yellow;
        }
    }

    // 클라 + 호스트 시작 시 RoomPlayer오브젝트를 참가자 목록 레이아웃 하위로 설정해 리스트 되도록 세팅
    public override void OnStartClient()
    {
        base.OnStartClient();
        if(RoomUI.instance != null){
            transform.SetParent(RoomUI.instance.participantsLayout.transform);
            transform.localScale = new Vector3(1, 1, 1);
        }
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
    public void OnCharacterSelected(Character character)
    {
        if(isLocalPlayer){
            CmdChangeCharacter(character);
        }
    }

    private void SetDisableAlradySelectedCharacter(Character character)
    {
        if(RoomUI.instance != null){
           M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
            List<NetworkRoomPlayer> players = M_NetworkRoomManager.roomSlots;
            foreach(RoomPlayer roomPlayer in players){
                if(roomPlayer.character.Equals(character)){
                    // TODO: 이벤트 수신 시 다른 플레이어가 선택한 캐릭터는 비활성화, 나머지는 활성화 되어 보이도록 해야함
                }
            }
        }
    }

    // 부모 클래스인 NetworkRoomPlayer에 구현된 syncvar hook 가상함수를 ovveride하여 사용
    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {
        TextMeshProUGUI readyStateText = gameObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        if (newReadyState)
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
            RpcReceive("PlayerName", message.Trim());
    }

    [ClientRpc]
    void RpcReceive(string playerName, string message)
    {
        Debug.Log(playerName + "의 메시지 : "+ message);
        if(RoomUI.instance != null){
            RoomUI.instance.AppendMessage(playerName, message);
        }
    }

}
