using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SpawnedMonster : NetworkBehaviour
{
    public string monsterName;

    [SyncVar]
    public int MAXHP;

    [SyncVar (hook = nameof(OnChangedHpValue))]
    public int HP;

    public List<MonsterAction> actionList = new List<MonsterAction>();

    [SyncVar]
    public MonsterAction nextAction;
    
    [SyncVar (hook = nameof(OnChangedMonsterData))]
    public MonsterData monsterData;

    readonly public SyncList<Buff> buffs = new SyncList<Buff>();

    
    [Command(requiresAuthority = false)]
    public void CmdAttackMonster(int damage)
    {
        Debug.Log(damage + "의 데미지로 몬스터 공격");
        HP = Math.Max(0, HP - damage);
        if(HP <= 0 && transform.parent != null){
            NetworkServer.Destroy(transform.parent.gameObject);
        }
    }

    public  void OnChangedMonsterData(MonsterData oldVal , MonsterData newVal)
    {
        monsterName = monsterData.name;
        MAXHP = monsterData.MAXHP;
        HP = monsterData.MAXHP;
        actionList = monsterData.actionList;
        //SyncVar Data는 서버에서 관리
        if(isServer)
        {
            HP = monsterData.HP;
            nextAction = actionList[0];
        }
    }

    public void OnChangedHpValue(int oldHpValue, int newHpValue)
    {
        if(transform.parent != null){
            transform.parent.GetComponent<TargetObject>().hpbar.value = newHpValue;
        }
    }
}
