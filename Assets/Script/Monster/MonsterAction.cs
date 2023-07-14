using ProjectD;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MonsterAction
{

    public string actionName;

    public int actionNumber;

    public int actionValue;
    public ActionTarget actionTarget;

    public MonsterAction(string name, int type,int value)
    {
        actionName = name;
        actionNumber = type;
        actionValue = value;
    }
    public MonsterAction()
    {

    }
}

[System.Serializable]
public class MonsterActionList
{
    public List<MonsterAction> ActionList = new List<MonsterAction>();
    public int frequency;
}