using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using DG.Tweening;

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
            GameUIManager.instance.currentIchiIcons[i].transform.DOKill();
            Destroy(GameUIManager.instance.currentIchiIcons[i]);
            GameUIManager.instance.currentIchiIcons.RemoveAt(i);
        }
        for(int i=0; i < maxIchiValue; i++){
            GameObject costIcon = Instantiate(GameUIManager.instance.CurrentItchPrefab, Vector3.zero, Quaternion.identity);
            costIcon.transform.SetParent(GameUIManager.instance.CurrentItchIconLayout.transform);
            costIcon.transform.localScale = Vector3.one;
            GameUIManager.instance.currentIchiIcons.Add(costIcon);
        }
        // 추가된 이치 아이콘 스케일 애니매이션 실행
        GameObject newCurrentIchiIcon =  GameUIManager.instance.currentIchiIcons[ GameUIManager.instance.currentIchiIcons.Count - 1];
        newCurrentIchiIcon.transform.DOScale(2f, 0.5f).OnComplete(() => {
            newCurrentIchiIcon.transform.DOScale(1f, 0.5f);
        });
    }

    // 최대 이치 아이콘 생성
    private void CreateMaxItchIcon(int maxIchiValue)
    {
        for(int i=GameUIManager.instance.maxIchiIcons.Count-1; i >= 0; i--){
            GameUIManager.instance.maxIchiIcons[i].transform.DOKill();
            Destroy(GameUIManager.instance.maxIchiIcons[i]);
            GameUIManager.instance.maxIchiIcons.RemoveAt(i);
        }
        for(int i=0; i < maxIchiValue; i++){
            GameObject costIcon = Instantiate(GameUIManager.instance.MaxItchPrefab, Vector3.zero, Quaternion.identity);
            costIcon.transform.SetParent(GameUIManager.instance.MaxItchIconLayout.transform);
            costIcon.transform.localScale = Vector3.one;
            GameUIManager.instance.maxIchiIcons.Add(costIcon);
        }
        // 추가된 최대 이치 아이콘 스케일 애니매이션 실행
        GameObject newMaxIchiIcon = GameUIManager.instance.maxIchiIcons[GameUIManager.instance.maxIchiIcons.Count -1];
        newMaxIchiIcon.transform.DOScale(2f, 0.5f).OnComplete(() => {
            newMaxIchiIcon.transform.DOScale(1f, 0.5f);
        });
    }
}
