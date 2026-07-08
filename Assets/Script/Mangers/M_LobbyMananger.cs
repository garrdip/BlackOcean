using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;

public class M_LobbyMananger : NetworkSingletonD<M_LobbyMananger>
{
    public uint ownedLobbyPlayer;

    public readonly SyncList<uint> lobbyPlayers = new SyncList<uint>(){ 0, 0, 0 }; // 리스트 요소들을 0으로 초기화. 0인 인덱스는 아직 LobbyPlayer가 추가되지 않은 빈 슬롯 상태

    protected override void Start()
    {
        DontDestroyOnLoad(gameObject);
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.persistentManagers.Add(gameObject.name, gameObject);
    }

    public override void OnStartClient()
    {
        lobbyPlayers.Callback += OnChangeLobbyPlayerOrderChanged;
    }

    // ----------------------------------------------------------------- Command Method --------------------------------------------------------------------------------//

    // 로비플레이어들 오더 관리용 Synclist의 요소를 인덱스에 맞게 스왑 수행
    [Command (requiresAuthority = false)]
    public void CmdSwapLobbyPlayer(int oldIndex, int newIndex, NetworkConnectionToClient sender = null)
    {
        if(!IsValidSlotIndex(oldIndex) || !IsValidSlotIndex(newIndex)) return;
        if(!IsSenderInvolvedInSwap(sender, oldIndex, newIndex)) return;
        uint temp = lobbyPlayers[oldIndex];
        lobbyPlayers[oldIndex] = lobbyPlayers[newIndex];
        lobbyPlayers[newIndex] = temp;
    }

    // 스왑 요청을 받은 로비플레이어의 SyncVar 변수에 인덱스 저장 + 요청받은 로비플레이어만 수락,거절 UI 활성화되도록 TargetRpc 전송
    [Command (requiresAuthority = false)]
    public void CmdRequestSwap(int oldIndex, int newIndex, NetworkConnectionToClient sender = null)
    {
        if(!IsValidSlotIndex(oldIndex) || !IsValidSlotIndex(newIndex)) return;
        if(!IsSenderInvolvedInSwap(sender, oldIndex, newIndex)) return;
        uint targetNetId = lobbyPlayers[newIndex];
        if(targetNetId != 0 && NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity networkIdentity)){
            LobbyPlayer targetLobbyPlayer = networkIdentity.GetComponent<LobbyPlayer>();
            targetLobbyPlayer.TargetResponseSwap(targetLobbyPlayer.GetComponent<NetworkIdentity>().connectionToClient);
            targetLobbyPlayer.oldIndex = oldIndex; // 요청한 로비플레이어의 인덱스
            targetLobbyPlayer.newIndex = newIndex; // 요청한 로비플레이어의 교환상대 인덱스
        }
    }

    private bool IsValidSlotIndex(int index)
    {
        return index >= 0 && index < lobbyPlayers.Count;
    }

    // 스왑 요청자가 스왑 당사자(두 슬롯 중 하나의 소유자)인지 검증 — 임의 클라이언트가 남의 오더를 조작하지 못하도록 방어
    private bool IsSenderInvolvedInSwap(NetworkConnectionToClient sender, int oldIndex, int newIndex)
    {
        if(sender == null) return true; // 서버 내부 호출

        bool OwnsSlot(int index)
        {
            uint slotNetId = lobbyPlayers[index];
            return slotNetId != 0 && NetworkServer.spawned.TryGetValue(slotNetId, out NetworkIdentity identity) && identity.connectionToClient == sender;
        }

        if(OwnsSlot(oldIndex) || OwnsSlot(newIndex)) return true;
        Debug.LogWarning($"[M_LobbyMananger] 스왑 거부 — 요청자가 스왑 당사자가 아닙니다. sender: {sender}, oldIndex: {oldIndex}, newIndex: {newIndex}");
        return false;
    }

    // ----------------------------------------------------------------- Server Method --------------------------------------------------------------------------------//

    // 로비 플레이어 리스트에 추가
    [Server]
    public void AddLobbyPlayer(uint targetNetId)
    {
        int lobbyPlayersCount = lobbyPlayers.FindAll((netId) => netId != 0).Count;
        if(lobbyPlayersCount <= lobbyPlayers.Count){
            if(lobbyPlayersCount == 0 || lobbyPlayers[0] == 0){ // 로비플레이어 인원이 0인 경우 or 0번이 빈 슬롯인 경우
                lobbyPlayers.RemoveAt(0);
                lobbyPlayers.Insert(0, targetNetId);
            }else if(lobbyPlayers[lobbyPlayersCount] == 0){ // 현재 LobbyPlayer 리스트에 있는 유저의 다음 인덱스 위치가 빈 슬롯인 경우 해당 인덱스에 새로 들어온 LobbyPlayer 추가
                lobbyPlayers.RemoveAt(lobbyPlayersCount);
                lobbyPlayers.Insert(lobbyPlayersCount, targetNetId);
            }else{
                int index = lobbyPlayers.FindIndex((netId) => netId == 0); // 그 외의 경우 빈 슬롯 찾아서 해당 인덱스에 LobbyPlayer 추가
                if(index != -1){
                    lobbyPlayers.RemoveAt(index);
                    lobbyPlayers.Insert(index, targetNetId);
                }
            }
        }
    }

    // 로비 플레이어 리스트에서 제거
    [Server]
    public void RemoveLobbyPlayer(uint targetNetId)
    {
        int index = lobbyPlayers.FindIndex((netId) => netId == targetNetId);
        if(index != -1){
            lobbyPlayers[index] = 0;
        }
    }

    // 로비 플레이어 강제 퇴장
    [Server]
    public void LobbyPlayerKickOut(RoomPlayer roomPlayer)
    {
        roomPlayer.GetComponent<NetworkIdentity>().connectionToClient.Disconnect();
    }

    // RoomPlayer의 레디 상태 체크
    [Server]
    public void RoomPlayerReadyCheck()
    {
        int num = 0;
        RoomPlayer[] players = FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None);
        for(int i = 0 ;i < players.Length ; i++)
        {
            if(players[i].isReady) num++;
            if(players[i].character == Character.NONE) num--;
        }
        if(num == players.Length - 1){
            RoomUI.instance.SetReadyButton("START");
            ReadyButtonOnRoom readyButtonOnRoom = RoomUI.instance.readyButton.GetComponent<ReadyButtonOnRoom>();
            readyButtonOnRoom.SetReadyButtonViewByReadyState(true);
        }else{
            RoomUI.instance.SetReadyButton("");
            ReadyButtonOnRoom readyButtonOnRoom = RoomUI.instance.readyButton.GetComponent<ReadyButtonOnRoom>();
            readyButtonOnRoom.SetReadyButtonViewByReadyState(false);
        }
    }

    // ----------------------------------------------------------------- SyncVar Hook --------------------------------------------------------------------------------//
    
    void OnChangeLobbyPlayerOrderChanged(SyncList<uint>.Operation op, int index, uint oldVal, uint newVal)
    {
        switch (op)
        {
            case SyncList<uint>.Operation.OP_ADD:
                
                break;
            case SyncList<uint>.Operation.OP_INSERT:
                
                break;
            case SyncList<uint>.Operation.OP_REMOVEAT:

                break;
            case SyncList<uint>.Operation.OP_SET: // SyncList 스왑 이벤트 수신
                SetLobbyPlayerSwap(newVal, index);
                RoomUI.instance.ChangeSwapButtonsState(newVal, index);
                break;
            case SyncList<uint>.Operation.OP_CLEAR:
                
                break;
        }
    }

    // ----------------------------------------------------------------- Normal Method --------------------------------------------------------------------------------//

    private void SetLobbyPlayerSwap(uint netId, int index)
    {
        if(isServer){
            if(NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                LobbyPlayer lobbyPlayer = networkIdentity.GetComponent<LobbyPlayer>();
                lobbyPlayer.roomPlayer.order = (PlayOrder)index;
                lobbyPlayer.ChangeLobbyPlayerViewByOrder(index);
            }
        }else{
            if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                LobbyPlayer lobbyPlayer = networkIdentity.GetComponent<LobbyPlayer>();
                lobbyPlayer.roomPlayer.order = (PlayOrder)index;
                lobbyPlayer.ChangeLobbyPlayerViewByOrder(index);
            }   
        }
    }
}
