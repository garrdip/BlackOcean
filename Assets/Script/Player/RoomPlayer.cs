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


    // 로컬 플레이어로 시작 시 캐릭터 선택 델리게이트 연결 및 로컬플레이어 구분색 변경
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        RoomUI.Instance.onCharacterSelect += OnCharacterSelected; 
        transform.GetComponent<Image>().color = Color.yellow;
    }

    // 클라 + 호스트 시작 시 RoomPlayer오브젝트를 참가자 목록 레이아웃 하위로 설정해 리스트 되도록 세팅
    public override void OnStartClient()
    {
        base.OnStartClient();
        transform.SetParent(RoomUI.Instance.participantsLayout.transform);
        transform.localScale = new Vector3(1, 1, 1);
    }

    // RoomUI 클래스로부터 캐릭터 선택 델리게이트 이벤트를 수신하여, 선택한 캐릭터 정보 서버에 송신
    public void OnCharacterSelected(Character character)
    {
        if(isLocalPlayer){
            CmdChangeCharacter(character);
        }
    }

    // 선택된 캐릭터 뷰 컴포넌트 활성화, 비활성화 변경
    private void ChangeSelectedCharacterActiveState(Character character)
    {
        // 매 이벤트 수신 시 모든 캐릭터 이미지, 버튼 활성화
        RoomUI.Instance.characters[0].GetComponent<Image>().color = new Color(1f, 1f, 1f);
        RoomUI.Instance.characters[1].GetComponent<Image>().color = new Color(1f, 1f, 1f);
        RoomUI.Instance.characters[2].GetComponent<Image>().color = new Color(1f, 1f, 1f);
        RoomUI.Instance.characters[0].GetComponent<Button>().interactable = true;
        RoomUI.Instance.characters[1].GetComponent<Button>().interactable = true;
        RoomUI.Instance.characters[2].GetComponent<Button>().interactable = true;
        // 현재 방에 참가한 유저들이 선택한 캐릭터 이미지, 버튼은 비활성화
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        List<NetworkRoomPlayer> players = M_NetworkRoomManager.roomSlots;
        foreach(RoomPlayer roomPlayer in players){
            GameObject gameObject = RoomUI.Instance.characters.Find((obj) => obj.name.Equals(roomPlayer.character.ToString()));
            if(gameObject != null){
                gameObject.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f);
                gameObject.GetComponent<Button>().interactable = false;
            }
        }
    }

    // 부모 클래스인 NetworkRoomPlayer에 구현된 syncvar hook 가상함수를 override하여 사용
    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {
        TextMeshProUGUI readyStateText = gameObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        if (newReadyState)
        {
            if(isLocalPlayer){
                RoomUI.Instance.buttonReady.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.red;
            }
            readyStateText.color = Color.red;
            readyStateText.text = "Ready";
        }
        else
        {
            if(isLocalPlayer){
                RoomUI.Instance.buttonReady.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.black;
            }
            readyStateText.color = Color.black;
            readyStateText.text = "Idle";
        }
    }

    // 캐릭터 선택 이벤트 송신
    [Command]
    public void CmdChangeCharacter(Character selectedCharacter)
    {
        character = selectedCharacter;
    }

    // 캐릭터 선택 이벤트 수신
    private void OnSelectCharacterChanged(Character oldCharacter, Character newCharacter)
    {
        TextMeshProUGUI selectedCharacterNameText = gameObject.transform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>();
        selectedCharacterNameText.text = newCharacter.ToString();
        Debug.Log("선택된 캐릭터 :" + newCharacter);
        ChangeSelectedCharacterActiveState(newCharacter);
    }

    // 채팅 메시지 이벤트 송신
    [Command]
    public void CmdSend(string message, NetworkConnectionToClient sender = null)
    {
        if (!string.IsNullOrWhiteSpace(message))
            RpcReceive("PlayerName", message.Trim());
    }

    // 채팅 메시지 이벤트 수신
    [ClientRpc]
    void RpcReceive(string playerName, string message)
    {
        Debug.Log(playerName + "의 메시지 : "+ message);
        RoomUI.Instance.AppendMessage(playerName, message);
    }
}

