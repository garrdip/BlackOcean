using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Mirror;

public class M_SaveManager : NetworkSingletonD<M_SaveManager>
{
    [SyncVar]
    public bool isSaveGame = false;
    public SaveData loadData;

    public void SaveGameDataToFile(GamePlayer[] games)
    {
        SaveData data = new SaveData();
        
        for(int i = 0 ;i < games.Length ; i ++)
        {
            data.players[i] = new SaveDataPlayer();
            data.players[i].character = games[i].character;
            data.players[i].HP = games[i].HP;
            data.players[i].MaxHP = games[i].MaxHP;
            data.players[i].ownerSteamId = games[i].objectOwner.steamID;
            foreach(Card card in games[i].GetComponent<GamePlayerDeck>().deck)
            {
                data.players[i].cards.Add(card);
            }
        }

        // [3D 맵 리뉴얼 테스트] 2D 맵 미생성 상태에서는 currentRoom이 null일 수 있음
        data.map.currentRoom = M_MapManager.instance.currentRoom != null
            ? (M_MapManager.instance.currentRoom.coordinate.x, M_MapManager.instance.currentRoom.coordinate.y)
            : (0, 0);

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
            data.map.regions.Add(saveDataRegion);
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

            loadData = formatter.Deserialize(stream) as SaveData;
            stream.Close();

        }
        else
        {
            Debug.Log("Save File does not exist");
        }
    }
}

