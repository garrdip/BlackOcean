using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using ProjectD;


public class MonsterData : SingletonD<MonsterData>
{
    public List<Monster> monsterDataList = new List<Monster>();
    public List<MonsterGroup> monsterGroups = new List<MonsterGroup>();

    void Start()
    {
        LoadMonsterDataFromDB();
        LoadMonsterStatFromDB();
        LoadMonsterGroupDataFromDB();
    }

    // MonsterDB는 헤더 이후 (ActionName,ActionValue,ActionTarget) 3컬럼이 반복되는 위치 기반 구조.
    // 첫 컬럼이 비어있는 행은 직전 몬스터의 추가 행동 리스트다.
    void LoadMonsterDataFromDB()
    {
        Monster monsterData = new Monster();
        foreach(CsvTable.Row row in CsvTable.LoadFromResources("DB/MonsterDB").rows)
        {
            try
            {
                if(row[0].Length != 0) // 새로운 몬스터 데이터 시작
                {
                    monsterData = new Monster();
                    monsterDataList.Add(monsterData);

                    //CSV 파일내의 데이터를 클래스 데이터로 저장
                    monsterData.name = row[0];
                    monsterData.MAXHP = int.Parse(row[1]);
                }

                // 몬스터 이름이 없을경우 스킬 LIST만 추가
                if(row[2] == "Buff")
                {
                    // 상시 버프 행 — 스폰 시 BattleSpawner가 GainBuff로 적용. user는 스폰 시점에 자기 자신으로 설정
                    Buff constantBuff = new Buff();
                    constantBuff.type = GetEnumData<BuffType>(row[3]); // 잘못된 enum이면 throw → 아래 catch에서 행 단위 격리
                    constantBuff.value = int.Parse(row[4]);
                    constantBuff.isInfinity = true; // 턴 경과로 감소하지 않는 영구 버프
                    monsterData.buffList.Add(constantBuff);
                }
                else
                {
                    MonsterActionList monsterActionList = new MonsterActionList();
                    monsterData.behavior.Add(monsterActionList);
                    monsterActionList.frequency = int.Parse(row[2]);
                    for(int i = 1 ; i < row.Count/3 ; i++ ) // 순차적 액션 저장
                    {
                        if(row[i*3].Length == 0)break;
                        MonsterAction monsterActions = new MonsterAction();
                        monsterActions.actionName = row[i*3];
                        monsterActions.actionNumber = i-1;
                        monsterActions.actionValue = int.Parse(row[i*3+1]);
                        monsterActions.actionTarget = (row[i*3+2] == "") ? ActionTarget.NONE : GetEnumData<ActionTarget>(row[i*3+2]);
                        monsterActionList.ActionList.Add(monsterActions);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonsterData] MonsterDB 로드 실패: {row[0]} ({row.lineNumber}행) — {e.Message}");
            }
        }
    }

    // RPG 확장 스탯(약점/공격 속성/TP 실드) 로드 — MonsterDB의 3컬럼 반복 위치 기반 포맷을 건드리지 않도록 별도 CSV로 분리.
    // MonsterStatDB에 없는 몬스터는 기본값(약점 없음/무속성/실드 0)으로 동작한다.
    void LoadMonsterStatFromDB()
    {
        foreach(CsvTable.Row row in CsvTable.LoadFromResources("DB/MonsterStatDB").rows)
        {
            try
            {
                string monsterName = row.Get("Monster_Name").Trim();
                Monster monster = monsterDataList.Find(m => m.name == monsterName);
                if(monster == null)
                {
                    Debug.LogError($"[MonsterData] MonsterStatDB {row.lineNumber}행 — MonsterDB에 없는 몬스터 이름: '{monsterName}'");
                    continue;
                }
                foreach(string weakness in row.Get("Weakness").Split('|'))
                {
                    if(weakness.Trim().Length == 0) continue;
                    monster.weaknesses.Add(GetEnumData<AttackAttribute>(weakness.Trim()));
                }
                string attribute = row.Get("AttackAttribute").Trim();
                monster.attackAttribute = (attribute.Length == 0) ? AttackAttribute.NONE : GetEnumData<AttackAttribute>(attribute);
                monster.tpShield = row.GetInt("TPShield");
                monster.agility = row.GetInt("Agility");
                monster.exp = row.GetInt("Exp");
                // 위험도 보너스 스탯 (위험도 시스템) — 컬럼이 없거나 비어 있으면 0 (위험도에 영향받지 않음)
                monster.hazardAtkBonus = GetFloat(row, "HazardAtk");
                monster.hazardDefBonus = GetFloat(row, "HazardDef");
                monster.hazardHpBonus = GetFloat(row, "HazardHp");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonsterData] MonsterStatDB 로드 실패: {row[0]} ({row.lineNumber}행) — {e.Message}");
            }
        }
    }

    void LoadMonsterGroupDataFromDB()
    {
        foreach(CsvTable.Row row in CsvTable.LoadFromResources("DB/MonsterGroupDB").rows)
        {
            try
            {
                MonsterGroup monsterGroup = new MonsterGroup();
                monsterGroup.groupName = row.Get("Group_Name");
                monsterGroup.minHazard = row.GetInt("Minimum_Hazard");
                monsterGroup.maxHazard = row.GetInt("Maximum_Hazard");
                for(int i = 3 ; i < row.Count ; i++)
                {
                    if(row[i] == "") continue;
                    Monster monster = monsterDataList.Find(m => m.name == row[i]);
                    if(monster != null) monsterGroup.monsters.Add(monster);
                    else Debug.LogError($"[MonsterData] MonsterGroupDB {row.lineNumber}행 — MonsterDB에 없는 몬스터 이름: '{row[i]}'");
                }
                monsterGroups.Add(monsterGroup);
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonsterData] MonsterGroupDB 로드 실패: {row[0]} ({row.lineNumber}행) — {e.Message}");
            }
        }
    }
    T GetEnumData<T>(string data)
    {
        return (T)Enum.Parse(typeof(T),data);
    }

    // 선택 컬럼의 실수 파싱 — 컬럼 없음/빈 값/파싱 실패는 0
    static float GetFloat(CsvTable.Row row, string column)
    {
        return float.TryParse(row.Get(column), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value) ? value : 0f;
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
