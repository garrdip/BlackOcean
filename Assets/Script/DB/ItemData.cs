using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using ProjectD;

public partial class ItemData : SingletonD<ItemData>
{
    public List<Item> artifacts = new List<Item>();
    public List<Item> legacies = new List<Item>();

    // 아이템이름을 키값으로 가지는 DIctionary
    public Dictionary<string, ItemEventHanddler> itemEffects = new Dictionary<string, ItemEventHanddler>();


    void Start()
    {
        LoadArtifactData();
        LoadLegacyData();
    }

    //Version 4
    public void LoadArtifactData()
    {
        LoadItemTable("DB/ArtifactDB", artifacts);
    }

    public void LoadLegacyData()
    {
        LoadItemTable("DB/LegacyDB", legacies);
    }

    // Artifact/Legacy 공통 로더 — 한 아이템의 오류(메서드명 오타 등)가 전체 로드를 중단시키지 않도록 행 단위로 격리
    private void LoadItemTable(string resourcePath, List<Item> destination)
    {
        foreach(CsvTable.Row row in CsvTable.LoadFromResources(resourcePath).rows)
        {
            try
            {
                string methodName = row.Get("Number");
                Item newItem = new Item(row.Get("Name"), methodName, row.GetEnum<ItemGrade>("Garde"), row.GetEnum<ItemEffectTime>("EffectTime"));
                ItemEventHanddler temp = (ItemEventHanddler)Delegate.CreateDelegate(typeof(ItemEventHanddler), this, methodName);
                destination.Add(newItem);
                itemEffects.Add(methodName, temp);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemData] {resourcePath} 로드 실패: {row[0]} ({row.lineNumber}행) — {e.GetType().Name}: {e.Message}");
            }
        }
    }
}