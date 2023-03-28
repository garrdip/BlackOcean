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
    
    [SyncVar (hook = nameof(OnChangedMonsterData))]
    public MonsterData monsterData;

    readonly public SyncList<Buff> buffs = new SyncList<Buff>();


    public  void OnChangedMonsterData(MonsterData oldVal , MonsterData newVal)
    {
        name = monsterData.name;
        MAXHP = monsterData.MAXHP;
        actionList = monsterData.actionList;
        //SyncVar Data는 서버에서 관리
        if(isServer)
        {
            HP = monsterData.HP;
            nextAction = actionList[0];
        }
    }
}
