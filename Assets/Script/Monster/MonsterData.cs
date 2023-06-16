using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MonsterData
{
    public string name;
    public int MAXHP;
    public List<MonsterActionList> behavior = new List<MonsterActionList>();
    public List<Buff> buffList = new List<Buff>();
}
