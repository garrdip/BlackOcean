using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class NPC_Mercurius : SpawnedMonster
{
    void OnMouseDown()
    {
        Debug.Log("클릭!");
        if(M_TurnManager.instance.phase == BattleTurn.NONE_BATTLE_SCENE)
        {
            Debug.Log("팝업 시작!");
            PopUpUIManager.instance.HandleMercuriusPopUp(true);
        }
    }
}
