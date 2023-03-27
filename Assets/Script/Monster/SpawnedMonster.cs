using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SpawnedMonster : NetworkBehaviour
{
    public string name;

    public int MAXHP;

    [SyncVar]
    public int HP;

    public List<MonsterAction> actionList = new List<MonsterAction>();

    [SyncVar]
    public MonsterAction nextAction;
    
    [SyncVar]
    public MonsterData monsterData;


    public override void OnStartClient()
    {
        base.OnStartClient();
        name = monsterData.name;
        MAXHP = monsterData.MAXHP;
        HP = monsterData.HP;
        actionList = monsterData.actionList;
        nextAction = actionList[0];
    }
}
