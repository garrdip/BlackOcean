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
    public List<MonsterAction> actionList = new List<MonsterAction>();

    [SyncVar]
    public MonsterAction nextAction;
    [SyncVar]
    public TargetObject nextTarget;
    
    [SyncVar (hook = nameof(OnChangedMonsterData))]
    public MonsterData monsterData;

    public  void OnChangedMonsterData(MonsterData oldVal , MonsterData newVal)
    {
        monsterName = monsterData.name;
        MAXHP = monsterData.MAXHP;
        HP = monsterData.MAXHP;
        actionList = monsterData.actionList;
        sheild = 0;
        //SyncVar Data는 서버에서 관리
        if(isServer)
        {
            HP = monsterData.MAXHP;
            nextAction = actionList[0];
        }
    }

    public void OnChangedHpValue(int oldHpValue, int newHpValue)
    {
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
        int frequencySum = 0;
        foreach(MonsterAction action in actionList)
            frequencySum += action.actionFrequency;
        int freq = UnityEngine.Random.Range(0,frequencySum);
        foreach(MonsterAction action in actionList)
        {
            if(action.actionFrequency > freq) return action;
            else freq -= action.actionFrequency;
        }
        Debug.Log("버그 : 몬스터 다음 액션 찾기 실패");
        return null;
    }

    [Server]
    public void DoAction()
    {
        transform.parent.GetComponent<TargetObject>().isAnimating = true;
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

    [ClientRpc]
    public void ResumeIdleAnimation()
    {
        transform.parent.GetComponent<TargetObject>().anim.state.SetAnimation(1,"00Normal",true);
        transform.parent.GetComponent<TargetObject>().isAnimating = false;
    }

    void FixedUpdate()
    {
        if(isServer)
        {
            if(HP <= 0)
            {
                NetworkServer.Destroy(this.gameObject);
            }
        }
    }
}
