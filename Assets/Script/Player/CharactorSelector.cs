using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Spine.Unity;
using ProjectD;

public class CharactorSelector : MonoBehaviour
{
    private SkeletonRendererCustomMaterials skeletonRendererCustomMaterials;

    void Start()
    {
        skeletonRendererCustomMaterials = GetComponent<SkeletonRendererCustomMaterials>();
        skeletonRendererCustomMaterials.enabled = false;
    }

    void OnMouseEnter()
    {
        if(IsIronDemonMoveTarget() || IsSkillAllyTarget()){ skeletonRendererCustomMaterials.enabled = true; return; } // 철귀 이동 / 아군·자신 스킬 대상 후보 — 아웃라인
        GamePlayer targetPlayer = transform.parent.GetComponent<TargetObject>().player;
        if(IsServerAuthorityPlayer() && !IsOpenedPopUpExist()){
            skeletonRendererCustomMaterials.enabled = true;
        }else if(IsInHub() && targetPlayer.isSelectable){
            skeletonRendererCustomMaterials.enabled = true;
        }
    }

    void OnMouseExit()
    {
        if(IsIronDemonMoveTarget() || IsSkillAllyTarget()){ skeletonRendererCustomMaterials.enabled = false; return; }
        GamePlayer targetPlayer = transform.parent.GetComponent<TargetObject>().player;
        if(IsServerAuthorityPlayer() && !IsOpenedPopUpExist()){
            skeletonRendererCustomMaterials.enabled = false;
        }else if(IsInHub() && targetPlayer.isSelectable){
            skeletonRendererCustomMaterials.enabled = false;
        }
    }

    void OnMouseDown()
    {
        // 홍단향 턴에 아군 캐릭터 클릭 = 철귀 이동 (HS0 — 버튼 없음, 2026-11-02). 호스트의 캐릭터 전환 클릭보다 우선한다
        if(IsIronDemonMoveTarget()){
            skeletonRendererCustomMaterials.enabled = false;
            M_TurnManager.instance.SubmitIronDemonTarget(transform.parent.GetComponent<TargetObject>());
            return;
        }
        // 스킬 버튼을 누른 뒤 아군 캐릭터 클릭 = 아군·자신 대상 스킬(보호·치유·기도·자세 등) 확정 (2026-11-02 — 아군 버튼 목록 대체)
        if(IsSkillAllyTarget()){
            skeletonRendererCustomMaterials.enabled = false;
            M_TurnManager.instance.SubmitAllyTarget(transform.parent.GetComponent<TargetObject>());
            return;
        }
        PlayerInterface playerInterface = PlayerRegistry.Local;
        GamePlayer targetPlayer = transform.parent.GetComponent<TargetObject>().player; // 클릭한 캐릭터의 GamePlayer 인스턴스
        GamePlayer localPlayer = playerInterface.currentGamePlayer; // 로컬 플레이어의 GamePlayer 인스턴스
        if(IsServerAuthorityPlayer() && IsInBattle() && !IsOpenedPopUpExist() && !targetPlayer.isSelectable){
            playerInterface.currentGamePlayerNetId = targetPlayer.netId; // // 클라이언트 나간 경우 서버권한 유저는 다른 플레이어 클릭해서 선택한 플레이어를 제어
        }else if(IsInHub() && targetPlayer.isSelectable){
            CampPopUp campPopUp = PopUpUIManager.instance.campPopUp.GetComponent<CampPopUp>();
            switch(campPopUp.campAction){
                case CampAction.Heal:
                    localPlayer.CmdHpRecovery(targetPlayer.netId);
                    PopUpUIManager.instance.HandleCampPopUpHide();
                    break;
                case CampAction.Gold:
                    campPopUp.goldInputLayout.SetActive(true);
                    campPopUp.targetPlayerNetId = targetPlayer.netId;
                    break;
            }
        }
        TargetIndicatorController.instance.SetPlayerSelectable(false);
        skeletonRendererCustomMaterials.enabled = false;
    }

    // 지금 이 아군을 클릭하면 철귀가 이동하는지 — 홍단향 자기 턴·행동 선택 전·이번 턴 미사용·철귀가 아직 여기 없음 (M_TurnManager.CanMoveIronDemonTo)
    private bool IsIronDemonMoveTarget()
    {
        if(M_TurnManager.instance == null || IsOpenedPopUpExist()) return false;
        return M_TurnManager.instance.CanMoveIronDemonTo(transform.parent.GetComponent<TargetObject>());
    }

    // 지금 이 아군을 클릭하면 선택 중인 아군·자신 대상 스킬의 대상이 되는지 (M_TurnManager.CanTargetAlly — 보호 등 MEMBER/TEAM은 아군 누구나, NONE은 행동 중인 자신만)
    private bool IsSkillAllyTarget()
    {
        if(M_TurnManager.instance == null || IsOpenedPopUpExist()) return false;
        return M_TurnManager.instance.CanTargetAlly(transform.parent.GetComponent<TargetObject>());
    }

    // 팝업 UI에 등록된 팝업목록들중 활성화된 팝업이 있으면 캐릭터 클릭되지 않도록 조건 체크
    private bool IsOpenedPopUpExist()
    {
        int index = PopUpUIManager.instance.popUpList.FindIndex((popUp) => popUp != null && popUp.activeSelf); // 씬에서 제거된 팝업 참조(null) 무시
        if(index == -1){
            return false;
        }
        return true;
    }

    // 이벤트 호출하려는 플레이어 오브젝트가 현재 유저가 선택 가능한 플레이어인지 확인하는 함수
    private bool IsServerAuthorityPlayer()
    {
        PlayerInterface playerInterface = PlayerRegistry.Local;
        GamePlayer gamePlayer = transform.parent.GetComponent<TargetObject>().player;
        if(
            playerInterface.isServer // 서버 권한인 경우
            && gamePlayer.isOwned // 선택하려는 플레이어가 소유권이 있는 경우
            && playerInterface.ownedPlayers.Count > 1 // 소유권한이 있는 플레이어수가 2명 이상인 경우
            && !PopUpUIManager.instance.battleResultPopUp.activeSelf // 전투보상팝업이 비활성화인 경우
        ){
            return true;
        }
        return false;
    }

    // 거점(NPC 상주 화면) 상태인지 — 류진솔/소피아의 치유·골드 전달 대상 선택은 거점에서만
    private bool IsInHub()
    {
        return M_HubManager.instance != null && M_HubManager.instance.isInHub && M_TurnManager.instance.phase == BattleTurn.NONE_BATTLE_SCENE;
    }

    // 스테이지 전투 중인지
    private bool IsInBattle()
    {
        return M_HubManager.instance != null && !M_HubManager.instance.isInHub;
    }
}
