using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using TMPro;

public partial class GamePlayerDeck : NetworkBehaviour
{
    // 플레이어용 변수
    [SyncVar (hook = nameof(OnChangedIchi))]
    public int currentIchi = 3;
    [SyncVar (hook = nameof(OnChangedIchi))]
    public int maxIchi = 3;
    [SyncVar]
    public int limitiChi = 6;

    public TextMeshProUGUI ichiText;

    [Server]
    public void SetInitialIchi()
    {
        if(GetComponent<GamePlayer>().character == Character.ERIS)
        {
            currentIchi = 2;
            maxIchi = 2;
        }
        else
        {
            currentIchi = 3;
            maxIchi = 3;
        }
    }

    public void InitIchi()
    {
        ichiText = GameUIManager.instance.ichiText;
        OnChangedIchi(0,0);
    }

    void OnChangedIchi(int oldVal, int newVal)
    {
        if(isOwned && ichiText != null)
            ichiText.text = currentIchi.ToString() + " / " + maxIchi.ToString();
    }

}
