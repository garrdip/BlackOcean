using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class M_SaveManager : SingletonD<M_SaveManager>
{


    public void SaveGameDataToFile(GamePlayer[] games)
    {
        SaveData data = new SaveData();
        
        for(int i = 0 ;i < games.Length ; i ++)
        {
            data.players[i] = new SaveDataPlayer();
            data.players[i].character = games[i].character;
            data.players[i].HP = games[i].HP;
            data.players[i].MaxHP = games[i].MaxHP;
            foreach(Card card in games[i].GetComponent<GamePlayerDeck>().deck)
            {
                data.players[i].cards.Add(card);
            }
        }

        data.map.currentRoom = (M_MapManager.instance.currentRoom.coordinate.x,M_MapManager.instance.currentRoom.coordinate.y);

        foreach(HexagonMapRoom mapRoom in M_MapManager.instance.hexagonMapRooms)
            data.map.hexagonMapRooms.Add(new SaveDataMapRoom(mapRoom));

        foreach(Region region in M_MapManager.instance.regions)
        {
            SaveDataRegion saveDataRegion = new SaveDataRegion();
            saveDataRegion.regionGrade = region.regionGrade;
            foreach(Tile tile in region.tiles)
            {
                saveDataRegion.tiles.Add(new SaveDataTile(tile));
                
            }
        }

        string filePath = Application.persistentDataPath + "/save.dat";
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(filePath, FileMode.Create);

        formatter.Serialize(stream, data);
        stream.Close();
        Debug.Log(filePath +" Save Done");
    }

    public void LoadGameDataFromFile()
    {
        string filePath = Application.persistentDataPath + "/save.dat";
        if(File.Exists(filePath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(filePath, FileMode.Open);

            SaveData loadData = formatter.Deserialize(stream) as SaveData;
            stream.Close();
            foreach(SaveDataPlayer pp in loadData.players)
            {
                if(pp == null) continue;
                Debug.Log("Player : " + pp.character);
                foreach(Card card in pp.cards)
                {
                    Debug.Log(card.baseCard.name);
                    Debug.Log(card.experience);
                }
            }
        }
        else
        {
            Debug.Log("Save File does not exist");
        }
    }
}

