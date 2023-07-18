using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class GamePlayerItem : NetworkBehaviour
{
    public readonly SyncList<Artifact> artifacts;
    public event ITEM_EventHanddler ART_OnStartBattleEvent;
    public event ITEM_EventHanddler ART_OnChangePosition;
    public event ITEM_EventHanddler ART_OnDead;
    public event ITEM_EventHanddler ART_OnEndBattle;

    public override void OnStartServer()
    {
        artifacts.Callback += OnArtifactUpdated;
    }


    void OnArtifactUpdated(SyncList<Artifact>.Operation op, int index, Artifact oldArtifact, Artifact newArtifact)
    {
        switch (op)
        {
            case SyncList<Artifact>.Operation.OP_ADD:
                switch(newArtifact.effectTime){
                    case ITEM_EffectTime.STARTBATTLE :
                        ART_OnStartBattleEvent += newArtifact.artifactEffect;
                    break;
                }
                break;
            case SyncList<Artifact>.Operation.OP_INSERT:
                
                break;
            case SyncList<Artifact>.Operation.OP_REMOVEAT:

                break;
            case SyncList<Artifact>.Operation.OP_SET:
                
                break;
            case SyncList<Artifact>.Operation.OP_CLEAR:
                
                break;
        }
    }

}
