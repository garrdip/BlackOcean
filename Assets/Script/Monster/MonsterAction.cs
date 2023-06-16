using ProjectD;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MonsterAction
{

    public string actionName;

    public ActionType actionType;

    public int actionValue;

    public MonsterAction(string name, ActionType type,int value)
    {
        actionName = name;
        actionType = type;
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