using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MonsterGroup
{
    public string groupName;

    public List<Monster> monsters = new List<Monster>(); // MonsterGroupDB 행의 몬스터 구성 (등장 스테이지는 StageDB MonsterGroups/EliteGroups가 결정)
}
