using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.IO;
using System;
using ProjectD;
using Mirror;
using Spine.Unity;

public partial class ItemData : SingletonD<CardData>
{
    public List<Artifact> artifacts;
    public List<Legacy> legacies;

    //Version 4
    public void LoadArtifactData()
    {
        TextAsset DBtext = Resources.Load<TextAsset>("DBs/ItemDB");
        using (StringReader DB = new StringReader(DBtext.text))
        {          
            while(true)
            {
                string value = DB.ReadLine();
                if( value == null ) break; // 마지막 데이터의 경우 null을 반환
                CardBase card = new CardBase();
                
            }
        }
    }

    public void LoadLegacyData()
    {
        TextAsset DBtext = Resources.Load<TextAsset>("DBs/ItemDB");
        using (StringReader DB = new StringReader(DBtext.text))
        {          
            while(true)
            {
                string value = DB.ReadLine();
                if( value == null ) break; // 마지막 데이터의 경우 null을 반환
                CardBase card = new CardBase();
                
            }
        }
    }
}