using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DG.Tweening;

public class M_LobbyMananger : NetworkSingletonD<M_LobbyMananger>
{
    public uint ownedLobbyPlayer;
    public readonly SyncList<uint> lobbyPlayers = new SyncList<uint>();

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

    [Command (requiresAuthority = false)]
    public void CmdSwapLobbyPlayer(int oldIndex, int newIndex)
    {
        uint temp = lobbyPlayers[oldIndex];
        lobbyPlayers[oldIndex] = lobbyPlayers[newIndex];
        lobbyPlayers[newIndex] = temp;
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
            case SyncList<uint>.Operation.OP_SET:
                Debug.Log("인덱스 :" + index);
                LobbyPlayer lobbyPlayer = NetworkClient.spawned[newVal].GetComponent<LobbyPlayer>();
                lobbyPlayer.transform.SetParent(RoomUI.instance.topIcons[index].transform);
                lobbyPlayer.transform.localScale = new Vector3(1f, 1f, 1f);
                lobbyPlayer.transform.DOLocalMoveX(0f, 0.5f);
                lobbyPlayer.transform.SetAsFirstSibling();
                RoomUI.instance.topIconImages[index].sprite = lobbyPlayer.isOwned ? lobbyPlayer.topIconMy : lobbyPlayer.topIconExChange;
                break;
            case SyncList<uint>.Operation.OP_CLEAR:
                
                break;
        }
    }

}
