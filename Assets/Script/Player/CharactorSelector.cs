using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class CharactorSelector : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public Material defaultMaterial;
    public Material outLineMaterial;

    void OnMouseEnter()
    {
        if(IsSelectablePlayer()){
            meshRenderer.material = outLineMaterial;
        }
    }

    void OnMouseExit()
    {
        if(IsSelectablePlayer()){
            meshRenderer.material = defaultMaterial;
        }
    }

    void OnMouseDown()
    {
        if(IsSelectablePlayer()){
            PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
            GamePlayer gamePlayer = transform.parent.GetComponent<TargetObject>().player;
            playerInterface.currentGamePlayerNetId = gamePlayer.netId;
        }
    }

    // 이벤트 호출하려는 플레이어 오브젝트가 현재 유저가 선택 가능한 플레이어인지 확인하는 함수
    private bool IsSelectablePlayer()
    {
        PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
        GamePlayer gamePlayer = transform.parent.GetComponent<TargetObject>().player;
        // 선택하려는 플레이어가 소유권이있는지 + 소유권한이 있는 플레이어수가 2명 이상인 경우 + 화살표가 비활성화 상태인 경우 체크
        if(gamePlayer.isOwned && playerInterface.ownedPlayers.Count > 1 && !M_CardManager.instance.isArrowActive){
            return true;
        }
        return false;
    }
}
