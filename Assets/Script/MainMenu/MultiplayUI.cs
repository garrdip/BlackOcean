using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class MultiplayUI : MonoBehaviour
{
    public Button btn_Close;
    public Button btn_CreateRoom;
    public M_SteamManager steamManager;
    void Start()
    {
        btn_Close.onClick.AddListener(()=>HandleCloseWindow());
        btn_CreateRoom.onClick.AddListener(()=>HandleCreateRoom());
    }

    void HandleCloseWindow()
    {
        gameObject.SetActive(false);
    }

    void HandleCreateRoom()
    {
        steamManager.HostLobby();
    }

}
