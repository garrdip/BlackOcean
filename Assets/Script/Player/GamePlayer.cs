using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;
using Steamworks;

public class GamePlayer : NetworkBehaviour
{
    [SyncVar (hook = nameof(OnChangedObjectOwner))]
    public PlayerInterface objectOwner;

    [SyncVar (hook = nameof(OnChangedSelectOrder))]
    public int selectOrder = 0;

    [SyncVar]
    public Character character;

    public readonly SyncList<CardOnHand> destroyCards = new SyncList<CardOnHand>();

    public void SetOrderByUI(int num)
    {
        if(isLocalPlayer)
            selectOrder = num;
    }
    // ------------------------------------------------------------- Command Method ------------------------------------------------------------------//

    // ------------------------------------------------------------- Server Method --------------------------------------------------------------------//

    [Server]
    public void SetPlayerOrder(int num)
    {
        SetPlayerOrderRPC(num);
    }

    // ---------------------------------------------------------------- ClientRpc Method -------------------------------------------------------------//

    [ClientRpc]
    public void RemoveDestroyCardList(CardOnHand cardOnHand)
    {
        if(isOwned)
        {
            destroyCards.Remove(cardOnHand);
        }
    }

    [ClientRpc]
    void SetPlayerOrderRPC(int num)
    {
        if(isLocalPlayer)
        {
            selectOrder = num;
        }
    }

    // ---------------------------------------------------------------- SyncVar Hook Method ----------------------------------------------------------//

    public void OnChangedSelectOrder(int oldVal,int newVal)
    {
        MapUI.instance.UpdateProfile();
        if(isLocalPlayer)
            MapUI.instance.SetOrderIndicator(newVal);
    }

    public void OnChangedObjectOwner(PlayerInterface oldVal, PlayerInterface newVal)
    {
        transform.SetParent(newVal.transform);
    }

}
