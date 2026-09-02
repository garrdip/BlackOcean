using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using ProjectD;

public partial class ItemData : SingletonD<ItemData>
{
    public List<Item> items = new List<Item>();      // 개인 아이템 (상인·이벤트 보상, 소유자에게만 효과)
    public List<Item> artifacts = new List<Item>();  // 공용 아티팩트 (지역거점 클리어 보상, 전원에게 효과)

    // 아이템 번호(=효과 메서드명)를 키값으로 가지는 Dictionary
    public Dictionary<string, ItemEventHanddler> itemEffects = new Dictionary<string, ItemEventHanddler>();


    void Start()
    {
        LoadItemData();
        LoadArtifactData();
    }

    //Version 4
    public void LoadItemData()
    {
        LoadItemTable("DB/ItemDB", items);
    }

    public void LoadArtifactData()
    {
        LoadItemTable("DB/ArtifactDB", artifacts);
    }

    // 지역거점 클리어 보상용 — 거점 등급(RegionGrade)과 동일한 등급의 아티팩트 중 랜덤 추출. 해당 등급이 없으면 전체에서 추출.
    public Item GetRandomArtifactByGrade(ItemGrade grade)
    {
        List<Item> candidates = artifacts.FindAll(artifact => artifact.itemGrade == grade);
        if(candidates.Count == 0) candidates = artifacts;
        if(candidates.Count == 0) return null;
        return new Item(candidates[UnityEngine.Random.Range(0, candidates.Count)]);
    }

    // Item/Artifact/Legacy 공통 로더 — 한 아이템의 오류(메서드명 오타 등)가 전체 로드를 중단시키지 않도록 행 단위로 격리
    private void LoadItemTable(string resourcePath, List<Item> destination)
    {
        foreach(CsvTable.Row row in CsvTable.LoadFromResources(resourcePath).rows)
        {
            try
            {
                string methodName = row.Get("Number");
                Item newItem = new Item(row.Get("Name"), methodName, row.GetEnum<ItemGrade>("Grade"), row.GetEnum<ItemEffectTime>("EffectTime"), row.GetInt("Value"), row.Get("Description"));
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
