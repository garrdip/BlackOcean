using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;
using Spine.Unity;

public class SpawnedMonster : NetworkBehaviour
{
    public string monsterName;

    [SyncVar]
    public int MAXHP;

    [SyncVar (hook = nameof(OnChangedHpValue))]
    public int HP;

    [SyncVar]
    public int sheild;
    
    [SyncVar]
    public MonsterAction nextAction;

    public MonsterActionList currentBehavior;
    [SyncVar]
    public TargetObject nextTarget;
    
    [SyncVar (hook = nameof(OnChangedMonsterData))]
    public MonsterData monsterData;

    public  void OnChangedMonsterData(MonsterData oldVal , MonsterData newVal)
    {
        monsterName = monsterData.name;
        MAXHP = monsterData.MAXHP;
        HP = monsterData.MAXHP;
        sheild = 0;
        //SyncVar Data는 서버에서 관리
        if(isServer)
        {
            HP = monsterData.MAXHP;
        }
    }

    public void OnChangedHpValue(int oldHpValue, int newHpValue)
    {
        if(HP <= 0)
        {
            if(isServer)NetworkServer.Destroy(this.gameObject);
            return;
        }
        if(transform.parent != null){
            transform.parent.GetComponent<TargetObject>().hpbar.value = newHpValue;
        }
    }

    [Server]
    public void SetNextTarget()
    {
        nextTarget = M_TurnManager.instance.spawnedPlayerList[0];
    }

    [Server]
    public void SetNextAction()
    {
        nextAction = GetNextAction();
    }

    MonsterAction GetNextAction()
    {
        // 다음 액션 찾는 알고리즘 추가 부분
        return monsterData.behavior[0].ActionList[0];
    }

    [Server]
    public void DoAction()
    {
        switch(nextAction.actionType)
        {
            case ActionType.SINGLEATTACK :
                nextTarget.player.HP -= nextAction.actionValue;
                DoAnimation();
                break;
            case ActionType.DEFENSE :
                sheild += nextAction.actionValue;
                DoAnimation();
                break;
            case ActionType.FULLSCALEATTACK :
                DoAnimation();
                foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
                {
                    target.player.HP -= nextAction.actionValue;
                }
                break;
        }
    }

    [ClientRpc]
    public void DoAnimation()
    {
        transform.parent.GetComponent<TargetObject>().anim.state.SetAnimation(1,"01Attack",false);
    }
}
