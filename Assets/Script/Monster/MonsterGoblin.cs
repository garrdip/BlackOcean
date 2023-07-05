using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;

public class MonsterGoblin : SpawnedMonster
{
    [Server]
    public override void DoAction()
    {
        switch(nextAction.actionType)
        {
            case ActionType.SINGLEATTACK :
                GeneralSingleAttack();
                DoAnimation();
                break;
            case ActionType.DEFENSE :
                sheild += nextAction.actionValue;
                DoAnimation();
                break;
            case ActionType.FULLSCALEATTACK :
                DoAnimation();
                GeneralFullScaleAttack();
                break;
        }
    }

    [ClientRpc]
    public override void DoAnimation()
    {
        transform.parent.GetComponent<TargetObject>().anim.state.SetAnimation(1,"01Attack",false);
    }
}
