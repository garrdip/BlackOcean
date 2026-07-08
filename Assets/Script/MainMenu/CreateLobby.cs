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
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        M_SteamManager.instance.HostLobby(lobbyName.text,StringUtils.RemoveZWSP(password.text));
        if(StringUtils.RemoveZWSP(lobbyName.text) == "load")
        {
            M_SaveManager.instance.LoadGameDataFromFile();
            M_SaveManager.instance.isSaveGame = true;
        }
    }
    
}
