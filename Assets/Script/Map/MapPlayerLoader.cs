using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;

public class MapPlayerLoader : MonoBehaviour
{
    public NetworkIdentity netID;
    public GamePlayer gamePlayer;

    void Start()
    {
        GamePlayer[] players = FindObjectsOfType<GamePlayer>();
        foreach(GamePlayer player in players)
        {
            if(player.netIdentity == netID )
            {
                GetComponent<TextMeshProUGUI>().text = player.character.ToString();
                gamePlayer = player;
            }
        }
    }
}
