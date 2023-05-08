using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MonsterGroup
{
    public string groupName;

    public List<MonsterData> monsters = new List<MonsterData>();

    public int minHazard;

    public int maxHazard;
    
}
