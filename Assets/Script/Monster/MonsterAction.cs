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

    public int actionFrequency;

    public MonsterAction(string name, ActionType type,int value, int freq)
    {
        actionName = name;
        actionType = type;
        actionValue = value;
        actionFrequency = freq;
    }
    public MonsterAction()
    {

    }
}
