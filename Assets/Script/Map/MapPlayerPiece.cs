using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class MapPlayerPiece: NetworkBehaviour
{
    [SyncVar(hook = nameof(OnChangeSteamId))]
    public string steamId;

    [SyncVar(hook = nameof(OnChangeGamePlayer))]
    public uint gamePlayer;

    public SpriteRenderer spriteRenderer;
    public TextMeshProUGUI textPlayerName;

    void Start()
    {
        transform.SetParent(M_MapManager.instance.roommaps.transform);
    }

    public void OnChangeSteamId(string oldSteamId, string newSteamId)
    {
        textPlayerName.text = newSteamId;
    }

    // GamePlayer참조값에서 selectOrder값에 따라 해당 플레이어 소유의 MapPlayerPiece 색상 변경
    public void OnChangeGamePlayer(uint oldValue, uint newValue)
    {
        PlayerInterface playerInterface = NetworkClient.spawned[newValue].GetComponent<PlayerInterface>();
        spriteRenderer.color = playerInterface.color;
    }

    // 맵 플레이어 위치 변경 수신
    [ClientRpc]
    public void RpcChangeMapPlayerPiecePosition(Vector3 position)
    {
        Vector3 offset = new Vector3(0f, 0f, 0f);
        /*
        switch(gamePlayer.selectOrder){
            case 0:
                offset += new Vector3(-0.2f, 0f, 0f);
                break;
            case 1:
                offset += new Vector3(0f, 0f, 0f);
                break;
            case 2:
                    offset += new Vector3(0.2f, 0f, 0f);
                break;
        }
        */
        transform.position = position + offset;
    }
}
