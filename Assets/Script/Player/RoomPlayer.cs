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
        RoomUI.instance.onCharacterSelect += OnCharacterSelected; 
        transform.GetComponent<Image>().color = Color.yellow;
    }

    // 클라 + 호스트 시작 시 RoomPlayer 오브젝트를 참조하는 RoomPlayerForUI 오브젝트를 생성하여 참가자 목록 레이아웃 하위로 설정해 리스트 되도록 세팅
    public override void OnStartClient()
    {
        base.OnStartClient();

        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        GameObject p = Instantiate(M_NetworkRoomManager.roomPlayerForUI);
        p.transform.SetParent(RoomUI.instance.participantsLayout.transform);
        p.transform.localScale = new Vector3(1, 1, 1);
        p.GetComponent<RoomPlayerForUI>().roomPlayer = this;
        M_NetworkRoomManager.listRoomPlayerForUI.Add(p);
    }

    // 클라 + 호스트 종료 시 RoomPlayerForUI 오브젝트 파괴 및 리스트에서 제거
    public override void OnStopClient()
    {
        base.OnStopClient();

        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        int findIndex = M_NetworkRoomManager.listRoomPlayerForUI.FindIndex((obj) =>  obj.GetComponent<RoomPlayerForUI>().roomPlayer == this);
        Destroy(M_NetworkRoomManager.listRoomPlayerForUI[findIndex]);
        M_NetworkRoomManager.listRoomPlayerForUI.RemoveAt(findIndex);
    }

    // RoomUI 클래스로부터 캐릭터 선택 델리게이트 이벤트를 수신하여, 선택한 캐릭터 정보 서버에 송신
    public void OnCharacterSelected(Character character)
    {
        if(isLocalPlayer){
            CmdChangeCharacter(character);
        }
    }

    // 부모 클래스인 NetworkRoomPlayer에 구현된 syncvar hook 가상함수를 override하여 사용
    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {
        TextMeshProUGUI readyStateText = gameObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        if (newReadyState)
        {
            EmitReadyChange(true);
        }
        else
        {
            EmitReadyChange(false);
        }
    }

    // RoomPlayerForUI 컴포넌트에 레디 상태 변경 델리게이트 이벤트 전송
    public void EmitReadyChange(bool isReady)
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        NetworkIdentity networkIdentity = GetComponent<NetworkIdentity>();
        foreach(GameObject gameObject in M_NetworkRoomManager.listRoomPlayerForUI){
            gameObject.GetComponent<RoomPlayerForUI>().EmitReadyStateRoomPlayerForUI(networkIdentity, isReady);
        }
    }

    // RoomPlayerForUI 컴포넌트에 캐릭터 선택 변경 델리게이트 이벤트 전송
    public void EmitCharacterChange(Character character)
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        NetworkIdentity networkIdentity = GetComponent<NetworkIdentity>();
        foreach(GameObject gameObject in M_NetworkRoomManager.listRoomPlayerForUI){
            gameObject.GetComponent<RoomPlayerForUI>().EmitCharacterChangeRoomPlayerForUI(networkIdentity, character);
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
        EmitCharacterChange(newCharacter);
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
        RoomUI.instance.AppendMessage(playerName, message);
    }
}

