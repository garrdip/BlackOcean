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
            GameUIManager.instance.maxIchiText.text = maxIchi.ToString();
            if(GameUIManager.instance.costIconList.Count > 0){
                if(GameUIManager.instance.costIconList.Count < maxIchi){
                    CreateCostIcon(maxIchi);
                }
                SetFillAsMaxItch(maxIchi);
                SetEmptyAsUsedCurrentItch(maxIchi - newVal);
            }
        }
    }

    void OnChangedMaxIchi(int oldVal, int newVal)
    {
        if(isOwned){
            GameUIManager.instance.currentIchiText.text = currentIchi.ToString();
            GameUIManager.instance.maxIchiText.text = newVal.ToString();
            CreateCostIcon(newVal);
        }  
    }

    // ------------------------------------------------------------------- Normal Method --------------------------------------------------------------//

    // 최대 이치값 만큼 코스트 아이콘 생성
    private void CreateCostIcon(int maxIchiValue)
    {
        RemoveCostIconList();
        for(int i=0; i < maxIchiValue; i++){
            GameObject costIcon = Instantiate(GameUIManager.instance.CostIocnPrefab, Vector3.zero, Quaternion.identity);
            costIcon.transform.SetParent(GameUIManager.instance.CostIconLayout.transform);
            costIcon.transform.localScale = Vector3.one;
            costIcon.transform.GetChild(2).gameObject.SetActive(true);
            GameUIManager.instance.costIconList.Add(costIcon);
        }
    }

    // 코스트 아이콘 제거
    private void RemoveCostIconList()
    {
        for(int i=GameUIManager.instance.costIconList.Count-1; i >= 0; i--){
            Destroy(GameUIManager.instance.costIconList[i]);
            GameUIManager.instance.costIconList.RemoveAt(i);
        }
    }

    // 최대 이치값 만큼 코스트 아이콘의 Fill 오브젝트 활성화
    private void SetFillAsMaxItch(int value)
    {
        for(int i=0; i < value; i++){
            GameUIManager.instance.costIconList[i].transform.GetChild(2).gameObject.SetActive(true);
        }
    }

    // 사용된 이치값 만큼 코스트 아이콘의 Fill 오브젝트 비활성화
    private void SetEmptyAsUsedCurrentItch(int value)
    {
        for(int i=0; i < value; i++){
            GameUIManager.instance.costIconList[i].transform.GetChild(2).gameObject.SetActive(false);
        }
    }
}
