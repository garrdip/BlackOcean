using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DG.Tweening;

public class M_LobbyMananger : NetworkSingletonD<M_LobbyMananger>
{
    public uint ownedLobbyPlayer;

    [SyncVar]
    public int lobbyPlayersCount = 0; // lobbyPlayers SyncList의 요소들을 0으로 초기화 한 상태로 사용하기 때문에 참가한 lobbyPlayer의 카운트는 따로 변수 관리
    
    public readonly SyncList<uint> lobbyPlayers = new SyncList<uint>(){ 0, 0, 0 }; // 리스트 요소들을 0으로 초기화. 0인 인덱스는 아직 LobbyPlayer가 추가되지 않은 빈 슬롯 상태

    protected override void Start()
    {
        DontDestroyOnLoad(gameObject);
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.managers.Add(gameObject);
    }

    public override void OnStartClient()
    {
        lobbyPlayers.Callback += OnChangeLobbyPlayerOrderChanged;
    }

    // 로비플레이어들 오더 관리용 Synclist의 요소를 인덱스에 맞게 스왑 수행
    [Command (requiresAuthority = false)]
    public void CmdSwapLobbyPlayer(int oldIndex, int newIndex)
    {
        uint temp = lobbyPlayers[oldIndex];
        lobbyPlayers[oldIndex] = lobbyPlayers[newIndex];
        lobbyPlayers[newIndex] = temp;
    }

    // 스왑 요청을 받은 로비플레이어의 SyncVar 변수에 인덱스 저장 + 요청받은 로비플레이어만 수락,거절 UI 활성화되도록 TargetRpc 전송
    [Command (requiresAuthority = false)]
    public void CmdRequestSwap(int oldIndex, int newIndex)
    {
        uint targetNetId = lobbyPlayers[newIndex];
        LobbyPlayer targetLobbyPlayer = NetworkServer.spawned[targetNetId].GetComponent<LobbyPlayer>();
        targetLobbyPlayer.TargetResponseSwap(targetLobbyPlayer.GetComponent<NetworkIdentity>().connectionToClient);
        targetLobbyPlayer.oldIndex = oldIndex; // 요청한 로비플레이어의 인덱스
        targetLobbyPlayer.newIndex = newIndex; // 요청한 로비플레이어의 교환상대 인덱스
    }

    // 로비 플레이어 리스트에 추가
    [Server]
    public void AddLobbyPlayer(uint targetNetId)
    {
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
            lobbyPlayersCount++; // LobbyPlayer Count 증가
        }
    }

    // 로비 플레이어 리스트에서 제거
    [Server]
    public void RemoveLobbyPlayer(uint targetNetId)
    {
        int index = lobbyPlayers.FindIndex((netId) => netId == targetNetId);
        lobbyPlayers[index] = 0;
        lobbyPlayersCount--; // LobbyPlayer Count 감소
    }

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
                if(NetworkClient.spawned.TryGetValue(newVal, out NetworkIdentity networkIdentity)){
                    LobbyPlayer lobbyPlayer = NetworkClient.spawned[newVal].GetComponent<LobbyPlayer>();
                    lobbyPlayer.ChangeLobbyPlayerView(index); 
                }
                RoomUI.instance.ChangeSwapButtonsState(newVal, index);
                break;
            case SyncList<uint>.Operation.OP_CLEAR:
                
                break;
        }
    }
}
