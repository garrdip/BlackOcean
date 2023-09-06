using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using ProjectD;
using Mirror;

public class NPC_Mercurius : SpawnedMonster
{
    void Start()
    {
        // NPC_Mercurius 생성될 때 현재 플레이어의 카드 데이터에서 6개의 랜덤 카드데이터를 추출하여 팝업에 6개의 상점카드 세팅
        if(transform.parent.GetComponent<TargetObject>().isCloneData){
            foreach(Card card in M_CardManager.instance.ExtractRandomCards(6)){
                PopUpUIManager.instance.mercuriusPopUp.GetComponent<MercuriusPopUp>().storeCards.Add(card);
            }
        }
    }

    void OnMouseDown()
    {
        if(!EventSystem.current.IsPointerOverGameObject()){ // NPC_Mercurius가 UI에 가려져 있을 경우(팝업이 활성화 된 경우) 클릭 이벤트 방지
            if(M_TurnManager.instance.phase == BattleTurn.NONE_BATTLE_SCENE){
                PopUpUIManager.instance.HandleMercuriusPopUp(true);
            }
        }
    }
}
