
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class MenuUI : MonoBehaviour
{
    public GameObject multiplayCanvas;
    public GameObject menuCanvas;
    public GameObject debris;
    public GameObject logoGroup;

    public Button buttonSinglePlay;
    public Button buttonMultiPlay;
    public Button buttonDeckBook;
    public Button buttonSettings;
    public Button buttonQuit;
    public Button buttonCloseDeckBook;

    void Start()
    {
        DeckBookUI.instance.onChangeDeckBookOpenState += OnChangeDeckBookOpenState;
        buttonSinglePlay.onClick.AddListener(() => HandleSinglePlay());
        buttonMultiPlay.onClick.AddListener(() => HandleMultiPlay());
        buttonDeckBook.onClick.AddListener(() => HandleOpenDeckBook());
        buttonSettings.onClick.AddListener(() => HandleSettings());
        buttonQuit.onClick.AddListener(() => HandleQuit());
    }

    public void HandleSinglePlay()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        M_NetworkRoomManager.maxConnections = 1; // 방 최대 인원 1명으로 설정
        M_NetworkRoomManager.StartHost(); // 호스트로 시작
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }   

    public void HandleMultiPlay()
    {
        menuCanvas.SetActive(false);
        multiplayCanvas.SetActive(true);
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    public void HandleOpenDeckBook()
    {
        DeckBookUI.instance.HandleOpenDeckBook();
    }

    public void OnChangeDeckBookOpenState(bool isOpen)
    {
        if(menuCanvas != null && debris != null && logoGroup != null){
            menuCanvas.SetActive(!isOpen);
            debris.SetActive(!isOpen);
            logoGroup.SetActive(!isOpen);
        }
    }

    public void HandleSettings()
    {
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        OptionUIManager.instance.HandShowOptionPopUp(true);
    }

    public void HandleQuit()
    {
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        Application.Quit();
    }
}
