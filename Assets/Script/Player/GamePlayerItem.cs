using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class GamePlayerItem : NetworkBehaviour
{
    public readonly SyncList<Item> artifacts;
    public event ItemEventHanddler ART_OnStartBattleEvent;
    public event ItemEventHanddler ART_OnChangePosition;
    public event ItemEventHanddler ART_OnDead;
    public event ItemEventHanddler ART_OnEndBattle;

    public override void OnStartServer()
    {
        artifacts.Callback += OnArtifactUpdated;
    }

    public void ART_OnStartBattleEvent_Invoke()
    {
        ART_OnStartBattleEvent.Invoke(M_TurnManager.instance.spawnedPlayerList.Find(tar => tar.player == GetComponent<GamePlayer>()));
    }

    void OnArtifactUpdated(SyncList<Item>.Operation op, int index, Item oldArtifact, Item newArtifact)
    {
        switch (op)
        {
            case SyncList<Item>.Operation.OP_ADD:
                switch(newArtifact.effectTime){
                    case ItemEffectTime.STARTBATTLE :
                        ART_OnStartBattleEvent += ItemData.instance.itemEffects[newArtifact.itemName];
                    break;
                }
                break;
            case SyncList<Item>.Operation.OP_INSERT:
                
                break;
            case SyncList<Item>.Operation.OP_REMOVEAT:

                break;
            case SyncList<Item>.Operation.OP_SET:
                
                break;
            case SyncList<Item>.Operation.OP_CLEAR:
                
                break;
        }
    }

}
