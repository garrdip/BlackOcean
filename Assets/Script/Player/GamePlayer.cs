using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    public SyncList<Artifact> artifacts = new SyncList<Artifact>();

    public SyncList<Card> deck =  new SyncList<Card>();
    
    public SyncList<Item> items = new SyncList<Item>();


    public override void OnStartLocalPlayer()
    {
        // Server Loading 종료 후 1층 데이터 생성
        if(isServer)
        {
            Debug.Log("Generate Floor");
            M_MapManager.instance.GenerateFloor();
        }
    }

    // Host, Client 시작 시 맵 UI 사용될 커스텀 MapPlayer생성해서 플레이어 참가 목록UI에 세팅
    public override void OnStartClient()
    {
        base.OnStartClient();

        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        GameObject user = Instantiate(M_MapManager.instance.mapPlayerForUI);
        user.transform.SetParent(CharacterInfoUI.instance.gamePlayerListLayout.transform);
        user.transform.localScale = new Vector3(1, 1, 1);
        user.GetComponent<MapPlayerForUI>().netID =  GetComponent<NetworkIdentity>();
        user.GetComponent<MapPlayerForUI>().gamePlayer = this;
    }
}
