using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;

public class GamePlayer : NetworkBehaviour
{
    [SyncVar]
    public int HP;

    [SyncVar]
    public int MaxHP = 0;

    [SyncVar]
    public Character character;

    [SyncVar]
    public bool isInitializeDone = false;

    [SyncVar (hook = nameof(OnChangedSelectOrder))]
    public int selectOrder = 0;


    public void SetOrderByUI(int num)
    {
        if(isLocalPlayer)
            selectOrder = num;
    }

    public void OnChangedSelectOrder(int oldVal,int newVal)
    {
        if(isServer)
            M_TurnManager.instance.OnChangedPlayerOrder();
    }

    public override void OnStartLocalPlayer()
    {
        // Server Loading 종료 후 1층 데이터 생성
        if(isServer)
        {
            M_MapManager.instance.GenerateFloor();
        }
        if(isLocalPlayer)
        {
            isInitializeDone = true;
            Debug.Log("다른 플레이어 기다림 시작!");
            StartCoroutine(nameof(WaitPlayerList));
        }
    }

    IEnumerator WaitPlayerList()
    {
        M_NetworkRoomManager netManger = NetworkRoomManager.singleton as M_NetworkRoomManager;
        //GamePlayer가 모두 로드 될때까지 기다림
        while(true)
        {
            GamePlayer[] users = FindObjectsOfType<GamePlayer>();
            if(users.Length == netManger.roomSlots.Count) break;
            yield return new WaitForSeconds(0.01f);
        }
        //GamePlayer가 모두 Initial Value 초기화 될때까지 기다림
        while(true)
        {
            int cnt = 0;
            GamePlayer[] users = FindObjectsOfType<GamePlayer>();
            foreach(GamePlayer user in users)
            {
                if(user.isInitializeDone) cnt++;
            }
            if(cnt == netManger.roomSlots.Count) break;
            yield return new WaitForSeconds(0.01f);
        }
        SetUserStatusUI();
        M_TurnManager.instance.SetOrderButtonListener();
        // 플레이어 로딩이 끝나면 턴매니저로 플레이어 리스트를 전달함
        if(isServer)
            M_TurnManager.instance.InitiateGamePlayerList();
    }

    public void SetUserStatusUI()
    {
        GamePlayer[] users = FindObjectsOfType<GamePlayer>();
        //자신의 UI를 최상단에 표시
        foreach( GamePlayer user in users )
        {
            if(user.isLocalPlayer)
            {
                GameObject userUI = Instantiate(M_MapManager.instance.mapPlayerForUI);
                userUI.transform.SetParent(CharacterInfoUI.instance.gamePlayerListLayout.transform);
                userUI.transform.localScale = new Vector3(1, 1, 1);
                userUI.GetComponent<MapPlayerForUI>().netID =  user.GetComponent<NetworkIdentity>();
                userUI.GetComponent<MapPlayerForUI>().gamePlayer = user;
            }
        }
        foreach( GamePlayer user in users )
        {
            if(!user.isLocalPlayer)
            {
                GameObject userUI = Instantiate(M_MapManager.instance.mapPlayerForUI);
                userUI.transform.SetParent(CharacterInfoUI.instance.gamePlayerListLayout.transform);
                userUI.transform.localScale = new Vector3(1, 1, 1);
                userUI.GetComponent<MapPlayerForUI>().netID =  user.GetComponent<NetworkIdentity>();
                userUI.GetComponent<MapPlayerForUI>().gamePlayer = user;
            }
        }
    }
}
