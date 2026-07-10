using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;


public class Executioner : SpawnedMonster
{
    public override MonsterGrade monsterGrade { get { return MonsterGrade.ELITE; } }

    public override IEnumerator DoAction()
    {
        return base.DoAction();
    }

    public override void OnChangedNextTarget(ActionTarget oldVal, ActionTarget newVal)
    {
        
    }
}
