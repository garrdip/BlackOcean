using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SteamFailUI : MonoBehaviour
{

    public Button terminateClientBtn;

    void Start()
    {
        terminateClientBtn.onClick.AddListener(() => HandleTerminateClient());
    }

    void HandleTerminateClient()
    {
        Application.Quit();
    }

}
