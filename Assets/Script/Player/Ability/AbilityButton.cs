using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class AbilityButton : NetworkBehaviour
{
    GamePlayerDeck mydeck;

    public override void OnStartClient()
    {
        if(isOwned)
            transform.position = new Vector3(16,-5,0);

        mydeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
    }

    void OnMouseDrag()
    {
        
    }

    void OnMouseDown()
    {
        mydeck.abilityCtrlArrow.InitCardCtrlArrow(this);
    }

    // 드래그 후 마우스 땔때
    void OnMouseUp()
    {
   
    }

}
