using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
public class M_LoadingManager : NetworkSingletonD<M_LoadingManager>
{
    public GameObject loadingScreen;

    [Server]
    public void SetLoadingScreen(bool onOff)
    {
        SetLoadingScreenOnOff(onOff);
    }

    [ClientRpc]
    public void SetLoadingScreenOnOff(bool onOff)
    {
        loadingScreen.SetActive(onOff);
    }

}
