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
    SyncList<Item> items = new SyncList<Item>();

    public override void OnStartLocalPlayer()
    {
        // Server Loading 종료 후 1층 데이터 생성
        if(isServer)
        {
            Debug.Log("Generate Floor");
            M_MapManager.instance.GenerateFloor();
            M_MapManager.instance.GenerateFloor();
            M_MapManager.instance.GenerateFloor();
        }
    }

}
