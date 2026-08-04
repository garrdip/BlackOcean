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

    // CSV 원문(한국어) 보관 — buffDB는 언어에 따라 이 원문에서 다시 만들어진다
    readonly Dictionary<BuffType, BuffInformation> buffSource = new Dictionary<BuffType, BuffInformation>();


    void Start()
    {
        LoadBuffDataFromDB();
    }

    void OnDestroy()
    {
        M_LanguageManager.onLocaleChanged -= ApplyLocalizedText;
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
                buffSource.Add(row.GetEnum<BuffType>("enum"), buffInformation);
            }
            catch (Exception e)
            {
                // 한 버프의 오류(enum 오타 등)가 전체 로드를 중단시키지 않도록 개별 처리
                Debug.LogError($"[BuffData] BuffDB 로드 실패: {row[0]} ({row.lineNumber}행) — {e.Message}");
            }
        }

        ApplyLocalizedText();
        M_LanguageManager.onLocaleChanged -= ApplyLocalizedText;
        M_LanguageManager.onLocaleChanged += ApplyLocalizedText;
    }

    /// <summary>현재 언어로 버프 이름·설명을 다시 만든다. 번역이 없으면 CSV의 한국어 원문을 쓴다.</summary>
    public void ApplyLocalizedText()
    {
        buffDB.Clear();
        foreach(KeyValuePair<BuffType, BuffInformation> pair in buffSource)
        {
            string buffEnum = pair.Key.ToString();
            buffDB.Add(pair.Key, new BuffInformation(){
                name = M_LanguageManager.Get(LocKey.BuffName(buffEnum), pair.Value.name),
                description = M_LanguageManager.Get(LocKey.BuffDesc(buffEnum), pair.Value.description)
            });
        }
    }
}

[System.Serializable]
public class BuffInformation
{
    public string name;
    public string description;
}