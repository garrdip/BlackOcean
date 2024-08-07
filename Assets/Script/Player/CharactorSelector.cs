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
        GamePlayer targetPlayer = transform.parent.GetComponent<TargetObject>().player;
        if(IsSelectablePlayer()){
            skeletonRendererCustomMaterials.enabled = true;
        }else if(IsNPCRoomType() && targetPlayer.isSelectable){
            skeletonRendererCustomMaterials.enabled = true;
        }
    }

    void OnMouseExit()
    {
        GamePlayer targetPlayer = transform.parent.GetComponent<TargetObject>().player;
        if(IsSelectablePlayer()){
            skeletonRendererCustomMaterials.enabled = false;
        }else if(IsNPCRoomType() && targetPlayer.isSelectable){
            skeletonRendererCustomMaterials.enabled = false;
        }
    }

    void OnMouseDown()
    {
        PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
        GamePlayer targetPlayer = transform.parent.GetComponent<TargetObject>().player; // 클릭한 캐릭터의 GamePlayer 인스턴스
        GamePlayer localPlayer = playerInterface.currentGamePlayer; // 로컬 플레이어의 GamePlayer 인스턴스
        if(IsSelectablePlayer() && IsPopUpOpened()){
            playerInterface.currentGamePlayerNetId = targetPlayer.netId;
            if(targetPlayer.GetComponent<GamePlayerDeck>().cardOnHands.Count == 0 && targetPlayer.GetComponent<GamePlayerDeck>().trashDeck.Count == 0)
                targetPlayer.GetComponent<GamePlayerDeck>().CmdSpawnCardOnHand();
            M_CardManager.instance.SetCurrentGamePlayerDeck(targetPlayer.GetComponent<GamePlayerDeck>());
        }else if(IsNPCRoomType() && targetPlayer.isSelectable){
            if(localPlayer.recoveryLimitCount <= 0){
                M_MessageManager.instance
                    .MakeToast()
                    .Position(ToastPosition.Bottom)
                    .MessageBoxColor(Color.red)
                    .TextColor(Color.white)
                    .Text("체력 회복 제한 횟수를 초과하였습니다.")
                    .FadeInTime(2f)
                    .FadeOutTime(2f)
                    .Show();
            }else{
                localPlayer.CmdHpRecovery(targetPlayer.netId);
            }
            M_TurnManager.instance.SetPlayerSelectable(false);
            skeletonRendererCustomMaterials.enabled = false;
        }
    }

    // 팝업 UI에 등록된 팝업목록들중 활성화된 팝업이 있으면 캐릭터 클릭되지 않도록 조건 체크
    private bool IsPopUpOpened()
    {
        int index = PopUpUIManager.instance.popUpList.FindIndex((popUp) => popUp.activeSelf);
        if(index != -1){
            return false;
        }
        return true;
    }

    // 이벤트 호출하려는 플레이어 오브젝트가 현재 유저가 선택 가능한 플레이어인지 확인하는 함수
    private bool IsSelectablePlayer()
    {
        PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
        GamePlayer gamePlayer = transform.parent.GetComponent<TargetObject>().player;
        if(
            playerInterface.isServer // 서버 권한인 경우
            && gamePlayer.isOwned // 선택하려는 플레이어가 소유권이 있는 경우
            && playerInterface.ownedPlayers.Count > 1 // 소유권한이 있는 플레이어수가 2명 이상인 경우
            && !M_CardManager.instance.isArrowActive // 화살표가 비활성화 상태인 경우
            && !PopUpUIManager.instance.battleResultPopUp.activeSelf // 전투보상팝업이 비활성화인 경우
            && IsBattleRoomType() // 전투 방인 경우
        ){
            return true;
        }
        return false;
    }

    // 전투가 이루어지는 방 타입인지 체크
    private bool IsBattleRoomType()
    { 
        if(M_MapManager.instance.currentRoom.roomType == RoomType.MONSTER || M_MapManager.instance.currentRoom.roomType == RoomType.ELITE || M_MapManager.instance.currentRoom.roomType == RoomType.BOSS)
        {
            return true;
        }
        return false;
    }

    // 전초기지, 카드 상점, 아이템 상점 방 타입인지 체크
    private bool IsNPCRoomType()
    {
        if(M_MapManager.instance.currentRoom.roomType == RoomType.CAMP || M_MapManager.instance.currentRoom.roomType == RoomType.CARD_NPC || M_MapManager.instance.currentRoom.roomType == RoomType.ITEM_NPC)
        {
            return true;
        }
        return false; 
    }
}
