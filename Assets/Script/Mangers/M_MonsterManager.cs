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
        ItemData.instance.LoadArtifactData();
        ItemData.instance.LoadLegacyData();
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
                if(values[0].Length != 0) // 새로운 몬스터 데이터 시작
                {
                    monsterData = new MonsterData();
                    monsterDataList.Add(monsterData);
                    
                    //CSV 파일내의 데이터를 클래스 데이터로 저장
                    monsterData.name = values[0];
                    monsterData.MAXHP = int.Parse(values[1]);
                }

                // 몬스터 이름이 없을경우 스킬 LIST만 추가
                if(values[2] == "Buff")
                {
                    //Buff newBuff = new Buff(GetEnumData<BuffType>(values[3]),(int.Parse(values[4])),false,true,false,null);
                    //monsterData.buffList.Add(newBuff);
                }
                else
                {
                    MonsterActionList monsterActionList = new MonsterActionList();
                    monsterData.behavior.Add(monsterActionList);
                    monsterActionList.frequency = int.Parse(values[2]);
                    for(int i = 1 ; i < values.Length/3 ; i++ ) // 순차적 액션 저장
                    {
                        if(values[i*3].Length == 0)break;
                        MonsterAction monsterActions = new MonsterAction();
                        monsterActions.actionName = values[i*3];
                        monsterActions.actionNumber = i-1;
                        monsterActions.actionValue = int.Parse(values[i*3+1]);
                        monsterActions.actionTarget = (values[i*3+2] == "") ? ActionTarget.NONE : GetEnumData<ActionTarget>(values[i*3+2]);
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
    T GetEnumData<T>(string data)
    {
        return (T)Enum.Parse(typeof(T),data);
    }

    public MonsterGroup GetMonsterGroup(int hazard)
    {
        List<MonsterGroup> listOfMonsterGroup = new List<MonsterGroup>();
        foreach(MonsterGroup monsterGroup in monsterGroups)
        {
            if(monsterGroup.minHazard <= hazard && hazard <= monsterGroup.maxHazard)
                listOfMonsterGroup.Add(monsterGroup);
        }
        if(listOfMonsterGroup.Count == 0)return monsterGroups[0];

        int index = UnityEngine.Random.Range(0, listOfMonsterGroup.Count);
        return listOfMonsterGroup[index];
    }

}
