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
        if(IsSelectablePlayer()){
            skeletonRendererCustomMaterials.enabled = true;
        }
    }

    void OnMouseExit()
    {
        if(IsSelectablePlayer()){
            skeletonRendererCustomMaterials.enabled = false;
        }
    }

    void OnMouseDown()
    {
        if(IsSelectablePlayer() && IsSelectableState()){
            PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
            GamePlayer gamePlayer = transform.parent.GetComponent<TargetObject>().player;
            playerInterface.currentGamePlayerNetId = gamePlayer.netId;
            if(gamePlayer.GetComponent<GamePlayerDeck>().cardOnHands.Count == 0 && gamePlayer.GetComponent<GamePlayerDeck>().trashDeck.Count == 0)
                gamePlayer.GetComponent<GamePlayerDeck>().CmdSpawnCardOnHand();
            M_CardManager.instance.SetCurrentGamePlayerDeck(gamePlayer.GetComponent<GamePlayerDeck>());
        }
    }

    // 팝업 UI에 등록된 팝업목록들중 활성화된 팝업이 있으면 캐릭터 클릭되지 않도록 조건 체크
    private bool IsSelectableState()
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

    // 전투가 이루어지는 방 타입인지 여부 체크
    private bool IsBattleRoomType()
    { 
        if(M_MapManager.instance.currentRoom.roomType == RoomType.MONSTER || M_MapManager.instance.currentRoom.roomType == RoomType.ELITE || M_MapManager.instance.currentRoom.roomType == RoomType.BOSS)
        {
            return true;
        }
        return false;
    }
}
