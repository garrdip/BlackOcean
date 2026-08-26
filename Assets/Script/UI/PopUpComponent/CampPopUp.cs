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

    [Header("기도 레이아웃 — 소피아 위험도 하향 확인 (위험도 시스템)")]
    public GameObject prayLayout;
    public TextMeshProUGUI prayInfoText;
    public Button buttonPrayOk;
    public Button buttonPrayCancel;


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
        buttonPrayOk.onClick.AddListener(() => {
            HandleClickPrayOk();
            PopUpUIManager.instance.HandleCampPopUpHide();
        });
        buttonPrayCancel.onClick.AddListener(() => {
            PopUpUIManager.instance.HandleCampPopUpHide();
        });
    }

    // ------------------------------------------------------------- 기도 (소피아 — 위험도 하향 확인) ------------------------------------------------- //

    /// <summary>기도 레이아웃 텍스트 갱신 — 현재 위험도/하향 비용/보유 골드. 팝업을 열 때 NPC_Sophia가 호출</summary>
    public void RefreshPrayInfo()
    {
        int hazardLevel = M_HubManager.instance.hazardLevel;
        GamePlayer localPlayer = PlayerRegistry.Local.currentGamePlayer;
        int gold = localPlayer != null ? localPlayer.gold : 0;
        bool isMinHazard = hazardLevel <= 0;

        // 최저 위험도 — 수락 없이 "닫기" 버튼 하나만 중앙에 표시
        buttonPrayOk.gameObject.SetActive(!isMinHazard);
        RectTransform cancelRect = (RectTransform)buttonPrayCancel.transform;
        cancelRect.anchoredPosition = new Vector2(isMinHazard ? 0f : 120f, cancelRect.anchoredPosition.y);
        TextMeshProUGUI cancelLabel = buttonPrayCancel.GetComponentInChildren<TextMeshProUGUI>(true);
        TextUpdater cancelUpdater = cancelLabel != null ? cancelLabel.GetComponent<TextUpdater>() : null;
        if(cancelUpdater != null) cancelUpdater.key = isMinHazard ? "ui.pray.close" : "ui.pray.cancel"; // 언어 변경 시에도 올바른 라벨 유지
        if(cancelLabel != null)
            cancelLabel.text = isMinHazard ? M_LanguageManager.Get("ui.pray.close", "닫기") : M_LanguageManager.Get("ui.pray.cancel", "거절");

        if(isMinHazard){
            prayInfoText.text = M_LanguageManager.Get("ui.pray.min_hazard", "위험도가 이미 최저 상태입니다");
            return;
        }
        int cost = M_HubManager.instance.GetHazardReduceCost();
        string confirmText = M_LanguageManager.Get("ui.pray.confirm", "위험도를 {0} → {1}(으)로 낮추시겠습니까?")
            .Replace("{0}", hazardLevel.ToString()).Replace("{1}", (hazardLevel - 1).ToString());
        string costText = M_LanguageManager.Get("ui.pray.cost", "비용: {0} 골드 (보유 {1})")
            .Replace("{0}", cost.ToString()).Replace("{1}", gold.ToString());
        prayInfoText.text = confirmText + "\n" + costText;
        buttonPrayOk.interactable = gold >= cost; // 골드 부족 시 수락 불가
    }

    // 수락 — 서버에 위험도 하향 요청 (골드 차감/재검증은 서버 M_HubManager.ReduceHazard)
    void HandleClickPrayOk()
    {
        GamePlayer localPlayer = PlayerRegistry.Local.currentGamePlayer;
        if(localPlayer == null) return;
        M_HubManager.instance.CmdReduceHazard(localPlayer.netId);
    }

    public void HandClickGiveGoldOk()
    {
        if(string.IsNullOrEmpty(inputFieldGold.text)){
            M_MessageManager.instance
                .MakeToast()
                .Position(ToastPosition.Bottom)
                .MessageBoxColor(Color.red)
                .TextColor(Color.white)
                .Text(M_LanguageManager.Get("ui.msg.gold_input_invalid", "골드 금액을 정확하게 입력하세요."))
                .FadeInTime(2f)
                .FadeOutTime(2f)
                .Show();
        }else{
            GamePlayer localPlayer = PlayerRegistry.Local.currentGamePlayer;
            localPlayer.CmdAddGoldValue(targetPlayerNetId, int.Parse(inputFieldGold.text));
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
                if(targetObject != null) M_DimmingManager.instance.SetTargetObjectLayer(targetObject, "CardOnHandOverPopUp"); // 거점에는 파티 아바타가 없음
            }
        }
        TargetIndicatorController.instance.SetPlayerSelectable(true);
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
            if(prayLayout != null) prayLayout.SetActive(false);
        });
        foreach(uint netId in M_TurnManager.instance.playerOrder){
             if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                TargetObject targetObject = M_TurnManager.instance.GetCurrentPlayerTargetObject(gamePlayer);
                if(targetObject != null) M_DimmingManager.instance.SetTargetObjectLayer(targetObject, "BackLayer");
            }
        }
        TargetIndicatorController.instance.SetPlayerSelectable(false);
        targetPlayerNetId = 0;
    }
}

public enum CampAction {
    None,
    Heal,
    Gold,
    Pray // 소피아 기도 — 위험도 하향 확인 (위험도 시스템)
}