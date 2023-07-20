using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.IO;
using System;
using ProjectD;
using Spine.Unity;

public partial class ItemData : SingletonD<ItemData>
{
    public List<Item> artifacts;
    public List<Item> legacies;
    // 아이템이름을 키값으로 가지는 DIctionary
    public Dictionary<string,ItemEventHanddler> itemEffects;

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

                
            }
        }
    }
}