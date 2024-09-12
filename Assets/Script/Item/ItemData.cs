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
        TextAsset DBtext = Resources.Load<TextAsset>("DBs/ArtifactDB");
        using (StringReader DB = new StringReader(DBtext.text))
        {          
            while(true)
            {
                string value = DB.ReadLine();
                if( value == null ) break; // 마지막 데이터의 경우 null을 반환
                string[] values = value.Trim().Split(",");
                if(values[0] == "Name") continue; // 첫줄 데이터 스킵   
                Item newItem = new Item(values[0],values[1],(ItemGrade)Enum.Parse<ItemGrade>(values[2]),(ItemEffectTime)Enum.Parse<ItemEffectTime>(values[3]));
                ItemEventHanddler temp = (ItemEventHanddler)Delegate.CreateDelegate(typeof(ItemEventHanddler),this,values[1]);
                artifacts.Add(newItem);
                itemEffects.Add(values[1],temp);
            }
        }
    }

    public void LoadLegacyData()
    {
        TextAsset DBtext = Resources.Load<TextAsset>("DBs/LegacyDB");
        using (StringReader DB = new StringReader(DBtext.text))
        {          
            while(true)
            {
                string value = DB.ReadLine();
                if( value == null ) break; // 마지막 데이터의 경우 null을 반환
                string[] values = value.Trim().Split(",");
                if(values[0] == "Name") continue; // 첫줄 데이터 스킵   
                Item newItem = new Item(values[0],values[1],(ItemGrade)Enum.Parse<ItemGrade>(values[2]),(ItemEffectTime)Enum.Parse<ItemEffectTime>(values[3]));
                ItemEventHanddler temp = (ItemEventHanddler)Delegate.CreateDelegate(typeof(ItemEventHanddler),this,values[1]);
                legacies.Add(newItem);
                itemEffects.Add(values[1],temp);       
            }
        }
    }
}