using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Mirror;
using TMPro;

public class CampPopUp : SingletonD<CampPopUp>
{
    public CampAction campAction;
    public CanvasGroup canvasGroup;
    public bool isMouseOnFrame = false;
    public GameObject healingLayout;
    public GameObject giveGoldLayout;
    public GameObject goldInputLayout;
    public TMP_InputField inputFieldGold;
    public Button buttonGivGoldOk;
    public Button buttonGivGoldCancel;
    public uint targetPlayerNetId;


    protected override void Awake()
    {
        campAction = CampAction.None;
        PopUpUIManager.instance.onCampPopUpShow += OnCampPopUpShow;
        PopUpUIManager.instance.onCampPopUpHide += OnCampPopUpHide;
        buttonGivGoldOk.onClick.AddListener(() => {
            HandClickGiveGoldOk();
            PopUpUIManager.instance.HandleCampPopUpHide();
        });
        buttonGivGoldCancel.onClick.AddListener(() => {
            PopUpUIManager.instance.HandleCampPopUpHide();
        });
    }

    public void HandClickGiveGoldOk()
    {
        if(string.IsNullOrEmpty(inputFieldGold.text)){
            M_MessageManager.instance
                .MakeToast()
                .Position(ToastPosition.Bottom)
                .MessageBoxColor(Color.red)
                .TextColor(Color.white)
                .Text("골드 금액을 정확하게 입력하세요.")
                .FadeInTime(2f)
                .FadeOutTime(2f)
                .Show();
        }else{
            GamePlayer localPlayer = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer;
            localPlayer.CmdAddGoldValue(localPlayer.netId, targetPlayerNetId, int.Parse(inputFieldGold.text));
            inputFieldGold.text = string.Empty;
            inputFieldGold.ActivateInputField();
        }
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // 전초기지 팝업 활성화 콜백
    public void OnCampPopUpShow(CampAction campAction)
    {
        this.campAction = campAction;
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
        campAction = CampAction.None;
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
            isMouseOnFrame = false;
            giveGoldLayout.SetActive(false);
            goldInputLayout.SetActive(false);
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
        targetPlayerNetId = 0;
    }
}

public enum CampAction {
    None,
    Heal,
    Gold
}