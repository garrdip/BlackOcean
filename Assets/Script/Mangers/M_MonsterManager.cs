using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using ProjectD;

public class M_MonsterManager : MonoBehaviour
{
    public static List<MonsterData> monsterDataList = new List<MonsterData>();
    public static List<MonsterGroup> monsterGroups = new List<MonsterGroup>();

    void Start()
    {
        LoadMonsterDataFromDB();
        LoadMonsterGroupDataFromDB();
        CardData.LoadCardDataFromDB();
    }

    void LoadMonsterDataFromDB()
    {
        MonsterData monsterData = new MonsterData();
        TextAsset DBtext = Resources.Load<TextAsset>("DBs/MonsterDB");
        using (StringReader DB = new StringReader(DBtext.text))
        {
            while(true)
            {
                string[] values = DB.ReadLine().Trim().Split(",");
                if(values[0] == "Monster_Name") continue;
                if(values[0] == "EndOfData") {
                    monsterDataList.Add(monsterData);
                    monsterData = new MonsterData();
                    break;
                }
                if(values[0].Length == 0){
                    // 몬스터 이름이 없을경우 스킬 LIST만 추가
                    monsterData.actionList.Add(new MonsterAction(values[2],GetActionType(values[3]),int.Parse(values[4])));
                }
                else{
                    //새로운 몬스터 이름이 출현할 경우 기존 데이터를 LIST 추가후 새로운 몬스터 객체 생성
                    if(monsterData.name != null){
                        monsterDataList.Add(monsterData);
                        monsterData = new MonsterData();
                    }
                    //CSV 파일내의 데이터를 클래스 데이터로 저장
                    monsterData.name = values[0];
                    monsterData.HP = int.Parse(values[1]);
                    monsterData.MAXHP = int.Parse(values[1]);
                    monsterData.actionList.Add(new MonsterAction(values[2],GetActionType(values[3]),int.Parse(values[4])));
                }
            }
        }
    }

    void LoadMonsterGroupDataFromDB()
    {
        TextAsset DBtext = Resources.Load<TextAsset>("DBs/MonsterGroupDB");
        using (StringReader DB = new StringReader(DBtext.text))
        {
            while(true)
            {
                string[] values = DB.ReadLine().Trim().Split(",");
                if(values[0] == "Monster_Group_Name") continue;
                if(values[0] == "EndOfData") break;
                MonsterGroup monsterGroup = new MonsterGroup();
                monsterGroup.groupName = values[0];
                monsterGroup.minHazard = int.Parse(values[1]);
                monsterGroup.maxHazard = int.Parse(values[2]);
                for(int i = 3 ; i < values.Length ; i++)
                {
                    if(values[i] != "")
                        monsterGroup.monsters.Add(monsterDataList.Find(monster => monster.name == values[i]));
                }
                monsterGroups.Add(monsterGroup);
            }
        }
    }
    ActionType GetActionType(string type)
    {
        ActionType retVal = ActionType.SINGLEATTACK;
        retVal = (ActionType)Enum.Parse(typeof(ActionType),type);
        return retVal;
    }
}
