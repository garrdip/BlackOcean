using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class AbilityButton : NetworkBehaviour
{
    public override void OnStartClient()
    {
        if(isOwned){
            transform.position = new Vector3(16.75f, -5f, 0);
        }
        gameObject.SetActive(false); // 초기 시점에 버튼 비활성화
    }

    void OnMouseDrag()
    {
        
    }

    void OnMouseDown()
    {
        if(NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayer>().character == ProjectD.Character.HONGDANHYANG)
            NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().abilityCtrlArrow.InitCardCtrlArrow(this);
        else
            NetworkClient.spawned[NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerTarget>().targetObject].GetComponent<TargetObject>().UsingGoHeng();
    }

    // 드래그 후 마우스 땔때
    void OnMouseUp()
    {
   
    }

}
