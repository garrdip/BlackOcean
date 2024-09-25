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

    private readonly int maxDisplayCostValue = 5; // UI 표시될 이치 아이콘 오브젝트의 최대 갯수

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
            SetCardOnHandCostTextState(newVal);
            GameUIManager.instance.currentIchiText.text = newVal.ToString();
            if(newVal > oldVal){
                CreateCurrentItchIcon(newVal - oldVal); // 증가수치만큼 현재 이치 아이콘 생성
            }else{
                RemoveCurrentItchIcon(oldVal - newVal, newVal); // 감소수치만큼 현재 이치 아이콘 제거
            }
        }
    }

    void OnChangedMaxIchi(int oldVal, int newVal)
    {
        if(isOwned){
            GameUIManager.instance.maxIchiText.text = newVal.ToString();
            if(newVal > oldVal){
                CreateMaxItchIcon(newVal - oldVal); // 증가수치만큼 최대 이치 아이콘 생성
            }else{
                RemoveMaxItchIcon(oldVal - newVal, newVal); // 감소수치만큼 최대 이치 아이콘 제거
            }
        }  
    }

    // ------------------------------------------------------------------- Normal Method --------------------------------------------------------------//

    // 현재 이치 아이콘 생성
    private void CreateCurrentItchIcon(int count)
    {
        int createCount = Mathf.Min(count, maxDisplayCostValue - GameUIManager.instance.currentIchiIcons.Count);
        for (int i = 0; i < createCount; i++){
            GameObject costIcon = Instantiate(GameUIManager.instance.CurrentItchPrefab, Vector3.zero, Quaternion.identity);
            costIcon.transform.SetParent(GameUIManager.instance.CurrentItchIconLayout.transform);
            costIcon.transform.localScale = Vector3.one;
            GameUIManager.instance.currentIchiIcons.Add(costIcon);
            costIcon.transform.DOScale(1.5f, 0.25f).OnComplete(() => {
                costIcon.transform.DOScale(1f, 0.25f);
            });
        }
    }

    // 현재 이치 아이콘 제거
    private void RemoveCurrentItchIcon(int count, int currentIchi)
    {
        if(GameUIManager.instance.currentIchiIcons.Count > 0){
            int removeCount = GameUIManager.instance.currentIchiIcons.Count - currentIchi;
            for(int i=0; i < removeCount; i++){
                int lastIndex = GameUIManager.instance.currentIchiIcons.Count - 1; // 마지막 인덱스에서 하나씩 제거
                GameUIManager.instance.currentIchiIcons[lastIndex].transform.DOKill();
                Destroy(GameUIManager.instance.currentIchiIcons[lastIndex]);
                GameUIManager.instance.currentIchiIcons.RemoveAt(lastIndex);
            }
        }
    }

    // 최대 이치 아이콘 생성
    private void CreateMaxItchIcon(int count)
    {
        int createCount = Mathf.Min(count, maxDisplayCostValue - GameUIManager.instance.maxIchiIcons.Count);
        for(int i=0; i < createCount; i++){
            GameObject costIcon = Instantiate(GameUIManager.instance.MaxItchPrefab, Vector3.zero, Quaternion.identity);
            costIcon.transform.SetParent(GameUIManager.instance.MaxItchIconLayout.transform);
            costIcon.transform.localScale = Vector3.one;
            GameUIManager.instance.maxIchiIcons.Add(costIcon);
            costIcon.transform.DOScale(1.5f, 0.25f).OnComplete(() => {
                costIcon.transform.DOScale(1f, 0.25f);
            });
        }
    }

    // 최대 이치 아이콘 제거
    private void RemoveMaxItchIcon(int count, int maxIchi)
    {
        if(GameUIManager.instance.maxIchiIcons.Count > 0){
            int removeCount = GameUIManager.instance.maxIchiIcons.Count - maxIchi;
            for (int i = 0; i < removeCount; i++){
                int lastIndex = GameUIManager.instance.maxIchiIcons.Count - 1; // 마지막 인덱스에서 하나씩 제거
                GameUIManager.instance.maxIchiIcons[lastIndex].transform.DOKill();
                Destroy(GameUIManager.instance.maxIchiIcons[lastIndex]);
                GameUIManager.instance.maxIchiIcons.RemoveAt(lastIndex);
            }
        }
    }

    private void SetCardOnHandCostTextState(int cost)
    {
        foreach(CardOnHand cardOnHand in cardOnHands){
            if(cost < cardOnHand.card.baseCard.cost){
                cardOnHand.textCardCost.color = Color.red;
            }else{
                cardOnHand.textCardCost.color = Color.white;
            }
        }
    }
}
