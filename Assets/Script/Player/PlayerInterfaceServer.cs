using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;


public class PlayerInterfaceServer : NetworkBehaviour
{

    // ------------------------------------------------------------- Command Method ------------------------------------------------------------------//

    // 게임플레이어 소유 오브젝트 생성 + 플레이어 오더 등록.
    // (카드포켓/카드 화살표/어빌리티 버튼 생성은 카드 시스템 제거로 삭제 — 2026-09-01)
    [Command]
    public void GenerateGamePlayerOwnedObjects(GamePlayer gamePlayer)
    {
        // 플레이어 오더 슬롯 등록 (룸에서 정한 selectOrder 인덱스에 netId)
        M_TurnManager.instance.RegisterPlayerOrder(gamePlayer.selectOrder, gamePlayer.netId);
    }
}
