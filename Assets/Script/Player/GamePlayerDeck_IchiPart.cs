using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using TMPro;

public partial class GamePlayerDeck : NetworkBehaviour
{
    // 플레이어용 변수
    [SyncVar (hook = nameof(OnChangedCurrentIchi))]
    public int currentIchi;

    [SyncVar (hook = nameof(OnChangedMaxIchi))]
    public int maxIchi;

    [SyncVar]
    public int limitiChi;


    [Server]
    public void SetInitialIchi()
    {
        if(GetComponent<GamePlayer>().character == Character.ERIS){
            currentIchi = 2;
            maxIchi = 2;
            limitiChi = 6;
        }else{
            currentIchi = 3;
            maxIchi = 3;
            limitiChi = 6;
        }
    }

    void OnChangedCurrentIchi(int oldVal, int newVal)
    {
        if(isOwned){
            GameUIManager.instance.currentIchiText.text = newVal.ToString();
        }
    }

    void OnChangedMaxIchi(int oldVal, int newVal)
    {
        if(isOwned){
            GameUIManager.instance.maxIchiText.text = newVal.ToString();
        }  
    }
}
