using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using ProjectD;

public class M_MonsterManager : SingletonD<M_MonsterManager>
{
    public List<MonsterData> monsterDataList = new List<MonsterData>();
    public List<MonsterGroup> monsterGroups = new List<MonsterGroup>();

    void Start()
    {
        LoadMonsterDataFromDB();
        LoadMonsterGroupDataFromDB();
        CardData.instance.LoadCardDataFromDB();
    }

    void LoadMonsterDataFromDB()
    {
        MonsterData monsterData = new MonsterData();
        TextAsset DBtext = Resources.Load<TextAsset>("DBs/MonsterDB");
        using (StringReader DB = new StringReader(DBtext.text))
        {
            while(true)
            {
                string value = DB.ReadLine();
                if( value == null ) break; // 마지막 라인
                string[] values = value.Trim().Split(",");
                
                if(values[0] == "Monster_Name") continue; // 첫번째 라인 스킵
                if(values[0].Length != 0)
                {
                    monsterData = new MonsterData();
                    monsterDataList.Add(monsterData);
                    
                    //CSV 파일내의 데이터를 클래스 데이터로 저장
                    monsterData.name = values[0];
                    monsterData.MAXHP = int.Parse(values[1]);
                }

                // 몬스터 이름이 없을경우 스킬 LIST만 추가
                if(values[3] == "Buff")
                {
                    Buff newBuff = new Buff((BuffType)Enum.Parse<BuffType>(values[4]),int.Parse(values[5]),false,null);
                    monsterData.buffList.Add(newBuff);
                }
                else
                {
                    MonsterActionList monsterActionList = new MonsterActionList();
                    monsterData.behavior.Add(monsterActionList);
                    monsterActionList.frequency = int.Parse(values[2]);
                    for(int i = 1 ; i < values.Length/3 ; i++ )
                    {
                        if(values[i*3].Length == 0)break;
                        MonsterAction monsterActions = new MonsterAction();
                        monsterActions.actionName = values[i*3];
                        monsterActions.actionType = (ActionType)Enum.Parse<ActionType>(values[i*3 + 1]);
                        monsterActions.actionValue = int.Parse(values[i*3+2]);
                        monsterActionList.ActionList.Add(monsterActions);
                    }
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
                string value = DB.ReadLine();
                if( value == null ) break;
                string[] values = value.Trim().Split(",");

                if(values[0] == "Group_Name") continue;
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
