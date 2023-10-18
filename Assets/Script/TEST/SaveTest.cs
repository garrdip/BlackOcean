using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveTest : MonoBehaviour
{
    public Button btn;

    void Start()
    {
        btn.onClick.AddListener(() => SaveGameHandler());
    }

    void SaveGameHandler()
    {
        GamePlayer[] gamePlayers = FindObjectsOfType<GamePlayer>();
        M_SaveManager.instance.SaveGameDataToFile(gamePlayers);
        M_SaveManager.instance.LoadGameDataFromFile();
    }
}
