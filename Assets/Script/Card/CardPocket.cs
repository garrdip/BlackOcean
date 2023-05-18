using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CardPocket : NetworkBehaviour
{
    // TODO : 관전모드 구현 시 관전하려는 플레이어의 CardPocket의 위치를 화면 중앙 하단으로 이동시켜 관전하도록 구현.
    // 카드들은 이미 NetworkTransform에 의해 각 소유의 플레이어들에의해 제어되고 있으므로 Pocket의 위치만 옮겨주면 해당 플레이어의 제어화면을 볼 수 있음.
    // 또한 액션 수행 로직은 각 플레이어들에 의해 충돌판정 후 서버에 요청하여 클라이언트에 동기화하므로, 관전자 화면에 보여지는 위치에 상관없이 로직은 정상 수행.

    [Header("카드 포켓 활성화 위치(플레이어가 제어 or 관전 시 중앙하단위치)")]
    public Vector3 cardPocketActivePosition;

    [Header("카드 포켓 비활성화 위치")]
    public Vector3 cardPocketInActivePosition;

    void Start()
    {
        cardPocketActivePosition = new Vector3(0f, -4f, 0f);
        cardPocketInActivePosition = new Vector3(-100f, -4f, 0f);
    }

    public override void OnStartClient()
    {
        transform.SetParent(DeckUI.instance.CardOnHandsPanel.transform);
        if(isOwned){
            transform.position = cardPocketActivePosition; // 현재 플레이어 소유의 카드는 화면 중앙 하단위치
        }else{
            transform.position = cardPocketInActivePosition; // 다른 플레이어 소유의 카드는 좌측 -100 위치
        }
    }
}
