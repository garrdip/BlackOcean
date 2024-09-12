using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MonsterGroup
{
    public string groupName;

    public List<Monster> monsters = new List<Monster>();

    public int minHazard;

    public int maxHazard;
    
}
