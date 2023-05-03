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
        
    }

    public void OnChangeSteamId(string oldSteamId, string newSteamId)
    {
        textPlayerName.text = newSteamId;
    }
}
