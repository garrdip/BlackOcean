using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterData
{

    public string name;

    public int MAXHP;

    public int HP;

    public List<MonsterAction> actionList = new List<MonsterAction>();

    public MonsterAction nextAction;


}
