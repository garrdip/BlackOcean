using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Monster : NetworkBehaviour
{
    [SyncVar]
    public string name;

    [SyncVar]
    public int MAXHP;

    [SyncVar]
    public int HP;

    public readonly SyncList<MonsterAction> actionList = new SyncList<MonsterAction>();

    [SyncVar]
    public MonsterAction nextAction;


}
