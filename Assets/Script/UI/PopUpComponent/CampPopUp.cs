using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using Mirror;

public class CampPopUp : SingletonD<CampPopUp>, IPointerClickHandler
{
    public CanvasGroup canvasGroup;
    public bool isMouseOnFrame = false;
    public GameObject healingLayout;
    public GameObject giveGoldLayout;


    protected override void Awake()
    {
        PopUpUIManager.instance.onCampPopUpShow += OnCampPopUpShow;
        PopUpUIManager.instance.onCampPopUpHide += OnCampPopUpHide;
    }

    private void HandleClickGiveGold()
    {
        GamePlayer gamePlayer = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer;
    }

    public void OnPointerEnterFramLayout(PointerEventData eventData)
    {
        isMouseOnFrame = true;
    }

    public void OnPointerExitFramLayout(PointerEventData eventData)
    {
        isMouseOnFrame = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(!isMouseOnFrame){
            PopUpUIManager.instance.HandleCampPopUp(false);
        }
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // 전초기지 팝업 활성화 콜백
    public void OnCampPopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        foreach(uint netId in M_TurnManager.instance.playerOrder){
             if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                TargetObject targetObject = M_TurnManager.instance.GetCurrentPlayerTargetObject(gamePlayer);
                M_DimmingManager.instance.SetTargetObjectLayer(targetObject, "CardOnHandOverPopUp");
            }
        }
    }

    // 전초기지 팝업 비활성화 콜백
    public void OnCampPopUpHide()
    {
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
            isMouseOnFrame = false;
            giveGoldLayout.SetActive(false);
            healingLayout.SetActive(false);
        });
        foreach(uint netId in M_TurnManager.instance.playerOrder){
             if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                TargetObject targetObject = M_TurnManager.instance.GetCurrentPlayerTargetObject(gamePlayer);
                M_DimmingManager.instance.SetTargetObjectLayer(targetObject, "BackLayer");
            }
        }
        M_TurnManager.instance.SetPlayerSelectable(false);
    }
}
