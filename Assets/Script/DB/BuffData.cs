using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using AYellowpaper.SerializedCollections;
using ProjectD;


public class BuffData : SingletonD<BuffData>
{
    [Header("버프 타입별 아이콘")]
    [SerializedDictionary("BuffType", "Sprite")]
    public SerializedDictionary<BuffType, Sprite> buffIcons = new SerializedDictionary<BuffType, Sprite>();

    public Dictionary<BuffType, BuffInformation> buffDB = new SerializedDictionary<BuffType, BuffInformation>();


    void Start()
    {
        LoadBuffDataFromDB();
    }

    
    // 버프 DB 로드
    public void LoadBuffDataFromDB()
    {
        foreach(CsvTable.Row row in CsvTable.LoadFromResources("DB/BuffDB").rows)
        {
            try
            {
                BuffInformation buffInformation = new BuffInformation(){
                    name = row.Get("name"),
                    description = row.Get("description")
                };
                buffDB.Add(row.GetEnum<BuffType>("enum"), buffInformation);
            }
            catch (Exception e)
            {
                // 한 버프의 오류(enum 오타 등)가 전체 로드를 중단시키지 않도록 개별 처리
                Debug.LogError($"[BuffData] BuffDB 로드 실패: {row[0]} ({row.lineNumber}행) — {e.Message}");
            }
        }
    }
}

[System.Serializable]
public class BuffInformation
{
    public string name;
    public string description;
}