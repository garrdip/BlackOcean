using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Mirror;

public class M_SaveManager : NetworkSingletonD<M_SaveManager>
{
    [SyncVar]
    public bool isSaveGame = false;
    public SaveData loadData;

    // 세이브 파일 경로 — BinaryFormatter(save.dat)에서 JSON(save.json)으로 교체. 구 포맷과 호환되지 않음 (개발용 스냅샷이라 마이그레이션 없음)
    static string FilePath => Application.persistentDataPath + "/save.json";

    public void SaveGameDataToFile(GamePlayer[] games)
    {
        SaveData data = new SaveData();

        for(int i = 0 ;i < games.Length ; i ++)
        {
            data.players[i] = new SaveDataPlayer();
            data.players[i].isActive = true;
            data.players[i].character = games[i].character;
            data.players[i].HP = games[i].HP;
            data.players[i].MaxHP = games[i].MaxHP;
            data.players[i].ownerSteamId = games[i].objectOwner.steamID;
        }

        File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
        Debug.Log(FilePath + " Save Done");
    }

    public void LoadGameDataFromFile()
    {
        if(File.Exists(FilePath))
        {
            try
            {
                loadData = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch(System.Exception e)
            {
                loadData = null;
                Debug.LogError($"[M_SaveManager] 세이브 파일 파싱 실패: {FilePath}\n{e}");
            }
        }
        else
        {
            Debug.Log("Save File does not exist");
        }
    }
}
