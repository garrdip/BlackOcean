using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class MapPlayerPiece: NetworkBehaviour
{
    [SyncVar(hook = nameof(OnChangeSteamId))]
    public string steamId;

    public TextMeshProUGUI textPlayerName;

    void Start()
    {
        transform.SetParent(M_MapManager.instance.roommaps.transform);
    }

    public void OnChangeSteamId(string oldSteamId, string newSteamId)
    {
        textPlayerName.text = newSteamId;
    }

    // 맵 플레이어 위치 변경 수신
    [ClientRpc]
    public void OnChangeMapPlayerPiecePosition(Vector3 position)
    {
        transform.position = position;
    }
}
