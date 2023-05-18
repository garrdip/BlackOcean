using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

public class CloneGamePlayer
{
    public int HP = 100;
    public int MaxHP = 100;
    public Character character;
    public bool isInitializeDone = false;
    public int selectOrder = 0;
    public bool isReady = false;
    public Vector2 destination;
    public ulong steamID;
    public CloneGamePlayer(GamePlayer data)
    {
        HP = data.HP;
        MaxHP = data.MaxHP;
        character = data.character;
        isInitializeDone = data.isInitializeDone;
        selectOrder = data.selectOrder;
        isReady = data.isReady;
        destination = data.destination;
        steamID = data.steamID;
    }
}
