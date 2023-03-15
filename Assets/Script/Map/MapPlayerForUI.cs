using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;

public class MapPlayerForUI : MonoBehaviour
{
    public NetworkIdentity netID;
    public GamePlayer gamePlayer;

    void Start()
    {
        GetComponent<TextMeshProUGUI>().text = gamePlayer.character.ToString();
    }
}
