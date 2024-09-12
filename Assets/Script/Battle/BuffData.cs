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
        TextAsset DBtext = Resources.Load<TextAsset>("DBs/BuffDB");
        using (StringReader DB = new StringReader(DBtext.text))
        {          
            while(true)
            {
                string value = DB.ReadLine();
                if( value == null ) break; // 마지막 데이터의 경우 null을 반환
                
                string[] values = value.Trim().Split(",");
                if(values[0] == "enum") continue; // 첫줄 데이터 스킵   

                BuffInformation buffInformation = new BuffInformation(){
                    name = values[1],
                    description = values[2]
                };
                buffDB.Add((BuffType)Enum.Parse<BuffType>(values[0]), buffInformation);
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