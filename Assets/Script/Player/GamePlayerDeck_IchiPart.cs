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


    // ------------------------------------------------------------- Server Method --------------------------------------------------------------------//

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

    // ---------------------------------------------------------------- SyncVar Hook Method ----------------------------------------------------------//

    void OnChangedCurrentIchi(int oldVal, int newVal)
    {
        if(isOwned){
            GameUIManager.instance.currentIchiText.text = newVal.ToString();
            CreateCurrentItchIcon(newVal);
        }
    }

    void OnChangedMaxIchi(int oldVal, int newVal)
    {
        if(isOwned){
            GameUIManager.instance.maxIchiText.text = newVal.ToString();
            CreateMaxItchIcon(newVal);
        }  
    }

    // ------------------------------------------------------------------- Normal Method --------------------------------------------------------------//

    // 현재 이치 아이콘 생성
    private void CreateCurrentItchIcon(int maxIchiValue)
    {
        for(int i=GameUIManager.instance.currentIchiIcons.Count-1; i >= 0; i--){
            Destroy(GameUIManager.instance.currentIchiIcons[i]);
            GameUIManager.instance.currentIchiIcons.RemoveAt(i);
        }
        for(int i=0; i < maxIchiValue; i++){
            GameObject costIcon = Instantiate(GameUIManager.instance.CurrentItchPrefab, Vector3.zero, Quaternion.identity);
            costIcon.transform.SetParent(GameUIManager.instance.CurrentItchIconLayout.transform);
            costIcon.transform.localScale = Vector3.one;
            GameUIManager.instance.currentIchiIcons.Add(costIcon);
        }
    }

    // 최대 이치 아이콘 생성
    private void CreateMaxItchIcon(int maxIchiValue)
    {
        for(int i=GameUIManager.instance.maxIchiIcons.Count-1; i >= 0; i--){
            Destroy(GameUIManager.instance.maxIchiIcons[i]);
            GameUIManager.instance.maxIchiIcons.RemoveAt(i);
        }
        for(int i=0; i < maxIchiValue; i++){
            GameObject costIcon = Instantiate(GameUIManager.instance.MaxItchPrefab, Vector3.zero, Quaternion.identity);
            costIcon.transform.SetParent(GameUIManager.instance.MaxItchIconLayout.transform);
            costIcon.transform.localScale = Vector3.one;
            GameUIManager.instance.maxIchiIcons.Add(costIcon);
        }
    }
}
