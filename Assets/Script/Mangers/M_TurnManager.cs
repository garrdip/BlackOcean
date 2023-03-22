using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class M_TurnManager : NetworkBehaviour
{
    public static M_TurnManager Instance = null;

    // 서버에서 관리할 player 리스트
    public List<GamePlayer> players;

    // 서버에서 관리하지만 유저도 쓸지 몰라서 일단 SyncList
    [SyncVar]
    public SyncList<GamePlayer> playerOrder = new SyncList<GamePlayer>();

    [SyncVar]
    public GamePlayer currentPlayer;

    public GameObject orderUI;

    public bool isOrderSelect = false;

    public List<Button> selectOrderButtons;

    public GameObject startButton;
    
    public static M_TurnManager instance
    {
        get
        {
            if (Instance == null)
            {
                Instance = FindObjectOfType<M_TurnManager>();
            }
            return Instance;
        }
    }
    public void SetOrderButtonListener()
    {
        selectOrderButtons[0].onClick.AddListener(() => SelectOrder(1));
        selectOrderButtons[1].onClick.AddListener(() => SelectOrder(2));
        selectOrderButtons[2].onClick.AddListener(() => SelectOrder(3));
        startButton.GetComponent<Button>().onClick.AddListener(() =>HandleStartBattle());
    }

    [Server]
    public void SetNextTurn()
    {
        
    }

    [Server]
    public void HandleStartBattle()
    {
        foreach(GamePlayer player in players)
        {
            playerOrder[player.selectOrder - 1] = player;
        }
        currentPlayer = playerOrder[0];
        M_MapManager.instance.StartBattle();
    }

    [Server]
    public void InitiateGamePlayerList()
    {
        players = new List<GamePlayer>(FindObjectsOfType<GamePlayer>());
        foreach(GamePlayer player in players)
        {
            playerOrder.Add(player);
        }
    }

    public void SelectOrder(int num)
    {
        Debug.Log("버튼 클릭!");
        NetworkClient.localPlayer.GetComponent<GamePlayer>().SetOrderByUI(num);
    }

    [ClientRpc]
    public void PopUpOrderUI()
    {
        orderUI.SetActive(true);
        isOrderSelect = true;
    }

    [Server]
    public void OnChangedPlayerOrder()
    {
        //모두가 다른 순서로 선택 완료했는지 체크
        int[] temp = new int[]{0,0};
        int sequence = 0;
        foreach(GamePlayer user in players)
        {
            if(user.selectOrder == 0) {
                startButton.SetActive(false);
                return;
            }
            for(int i = 0 ;i < sequence ; i++)
                if(user.selectOrder == temp[i]){
                    startButton.SetActive(false);
                    return;
                } 
            temp[sequence] = user.selectOrder;
            sequence++;
        }
        startButton.SetActive(true);
    }
}
