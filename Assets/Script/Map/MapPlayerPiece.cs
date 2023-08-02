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
    public GamePlayer gamePlayer;

    public SpriteRenderer spriteRenderer;
    public TextMeshProUGUI textPlayerName;

    void Start()
    {
        transform.SetParent(M_MapManager.instance.roommaps.transform);
        if(isOwned){
            NetworkClient.connection.identity.GetComponent<GamePlayerMap>().CmdSetOwnMapPlayerPiece(this); // 클라이언트별로 각자 소유의 MapPlayerPiece 참조값 설정
        }
    }

    public void OnChangeSteamId(string oldSteamId, string newSteamId)
    {
        textPlayerName.text = newSteamId;
    }

    // GamePlayer참조값에서 selectOrder값에 따라 해당 플레이어 소유의 MapPlayerPiece 색상 변경
    public void OnChangeGamePlayer(GamePlayer oldValue, GamePlayer newValue)
    {
        if(newValue != null){
            switch(newValue.selectOrder)
            {
                case 0:
                    spriteRenderer.color = Color.red;
                    break;
                case 1:
                    spriteRenderer.color = Color.blue;
                    break;
                case 2:
                    spriteRenderer.color = Color.green;
                    break;
            }
        }
    }

    // 맵 플레이어 위치 변경 수신
    [ClientRpc]
    public void RpcChangeMapPlayerPiecePosition(Vector3 position)
    {
        if(gamePlayer != null){
            Vector3 offset = new Vector3(0f, 0f, 0f);
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
            transform.position = position + offset;
        }
    }
}
