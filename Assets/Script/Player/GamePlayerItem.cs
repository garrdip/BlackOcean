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
        ART_OnStartBattleEvent?.Invoke(M_TurnManager.instance.spawnedPlayerList.Find(tar => tar.player == GetComponent<GamePlayer>()));
    }

    void OnArtifactUpdated(SyncList<Item>.Operation op, int index, Item oldArtifact, Item newArtifact)
    {
        switch (op)
        {
            case SyncList<Item>.Operation.OP_ADD:
                switch(newArtifact.effectTime){
                    case ItemEffectTime.STARTBATTLE :
                        // itemEffects의 키는 아이템 번호(=효과 메서드명) — itemName으로 조회하면 KeyNotFoundException
                        if(ItemData.instance.itemEffects.TryGetValue(newArtifact.itemNumber, out ItemEventHanddler effect)){
                            ART_OnStartBattleEvent += effect;
                        }else{
                            Debug.LogError($"[GamePlayerItem] 아이템 효과 조회 실패: {newArtifact.itemName}({newArtifact.itemNumber})");
                        }
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
