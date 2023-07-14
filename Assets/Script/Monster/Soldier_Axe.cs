using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class Soldier_Axe : SpawnedMonster
{

    public override void DoAction()
    {
        switch(nextAction.actionNumber){
            case 0 :
                GeneralAttack();
                DoAnimation();
                break;
            case 1 :
                parent.GainBuff(BuffType.ICHI_ATTACK,nextAction.actionValue);
                DoAnimation();
                break;
        }
    }

    [ClientRpc]
    public override void DoAnimation()
    {
        parent.anim.state.SetAnimation(1,"01Attack",false);
    }
}
