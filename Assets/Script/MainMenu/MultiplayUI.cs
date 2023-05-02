using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Steamworks;


public class MultiplayUI : InstanceD<MultiplayUI>
{
    public Button btn_Close;
    public Button btn_CreateRoom;

    public GameObject createLobbyIcon;
    public GameObject shadowMaker;
    
    public Button backButton;
    public Button refreshButton;
    public GameObject popUpUI;

    public GameObject multiPlayerUI;
    public GameObject mainMenuUI;
    int originLocation = 888;
    int targetLocation = 614;
    List<GameObject> lobbyList = new List<GameObject>();
    public GameObject prefabLobbyData;
    public Transform lobbyListTransform;

    void Start()
    {
        btn_Close.onClick.AddListener(()=>HandleCloseWindow());
        btn_CreateRoom.onClick.AddListener(()=>HandleCreateRoom());
        backButton.onClick.AddListener(()=> HandleBackToLobbyList());
        refreshButton.onClick.AddListener(()=> HandleRefreshRoom());
    }
    
    void HandleRefreshRoom()
    {
        M_SteamManager.instance.GetLobbyList();
    }

    public void ClearLobbyList()
    {
        foreach(GameObject del in lobbyList)
        {
            Destroy(del);
        }
    }

    public void AddLobbyData(CSteamID lobbyId,string lobbyName, bool hasPassword)
    {
        GameObject newLobby = Instantiate(prefabLobbyData,transform.position,Quaternion.identity,lobbyListTransform);
        newLobby.GetComponent<LobbyData>().SetLockState(hasPassword);
        newLobby.GetComponent<LobbyData>().SetLobbyName(lobbyName);
        newLobby.GetComponent<LobbyData>().lobbyId = lobbyId;
        lobbyList.Add(newLobby);
    }

    void HandleCloseWindow()
    {
        
    }

    void HandleCreateRoom()
    {
        shadowMaker.SetActive(true);
        createLobbyIcon.transform.DOLocalMove(new Vector3(0,targetLocation,0),0.5f).OnComplete(() =>OnCompleteMoveIcon());
    }

    void HandleBackToLobbyList()
    {
        if(popUpUI.activeSelf) // 방생성 PopUp UI가 있을경우 PopUp UI만 Disable
        {
            popUpUI.SetActive(false);
            createLobbyIcon.transform.DOLocalMove(new Vector3(0,originLocation,0),0.5f);
            shadowMaker.SetActive(false);
        }
        else // 메인메뉴로 돌아감
        {
            multiPlayerUI.SetActive(false);
            mainMenuUI.SetActive(true);
        }
    }
    void OnCompleteMoveIcon()
    {
        popUpUI.SetActive(true);
    }
}
