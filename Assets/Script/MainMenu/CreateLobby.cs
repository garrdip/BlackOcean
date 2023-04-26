using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectD;
using TMPro;

public class CreateLobby : MonoBehaviour
{
    public TextMeshProUGUI lobbyName;
    public TextMeshProUGUI password;
    public Button btnCreateRoom;

    void Awake()
    {
        btnCreateRoom.onClick.AddListener(()=>HandleCreateRoom());
    }

    void HandleCreateRoom()
    {
       M_SteamManager.instance.HostLobby(lobbyName.text,StringUtils.RemoveZWSP(password.text));
    }
    
}
