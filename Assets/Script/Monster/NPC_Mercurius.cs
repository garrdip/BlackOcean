using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using ProjectD;
using Mirror;

public class NPC_Mercurius : SpawnedMonster
{
    public MercuriusPopUp mercuriusPopUp;
    
    [SyncVar]
    public bool isOrigin = false; // 원본 오브젝트인지 구분값(타겟오브젝트들은 원본과 클론이 존재해서 둘중 하나만 호출되어야 함)

    void Awake()
    {
        mercuriusPopUp = PopUpUIManager.instance.mercuriusPopUp.GetComponent<MercuriusPopUp>();
    }

    // NPC_Mercurius 각 클라이언트에 생성될 때 현재 플레이어의 카드 데이터에서 6개의 랜덤 카드데이터를 추출하여 팝업에 6개의 상점카드 세팅
    public override void OnStartClient()
    {
        if(isOrigin && mercuriusPopUp != null){
            foreach(Card card in  M_CardManager.instance.ExtractRandomCards(6)){
                mercuriusPopUp.storeCards.Add(card);
            }
        }
    }

    void OnDestroy()
    {
        // NPC_Mercurius 파괴될 때 리스트 비움
        if(mercuriusPopUp != null){
            mercuriusPopUp.storeCards.Clear();
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
