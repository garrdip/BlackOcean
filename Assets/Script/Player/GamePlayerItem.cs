using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

// 플레이어의 개인 아이템 보유 목록과 효과 발동을 담당.
// - 개인 아이템(items): 상인·이벤트 보상으로 획득, 효과는 소유자에게만 적용.
// - 공용 아티팩트는 M_TurnManager.teamArtifacts에 있으며, 발동 시 이 컴포넌트의 InvokeEffects를 통해 플레이어별로 적용된다.
// 효과는 이벤트 구독 대신 발동 시점(ItemEffectTime)별로 목록을 순회해 호출한다
// — 아이템 제거/전체 초기화 시 구독 해제 관리가 필요 없고, SyncList가 곧 단일 진실 소스가 된다.
// 발동은 전부 서버 전용이며, 클라이언트는 SyncList 동기화로 보유 목록만 전달받는다.
public class GamePlayerItem : NetworkBehaviour
{
    public readonly SyncList<Item> items = new SyncList<Item>();

    // 개인 아이템 지급 (서버). ONCEGET(획득 즉시) 효과는 이 시점에 바로 발동한다.
    [Server]
    public void AddItem(Item newItem)
    {
        items.Add(newItem);
        if(newItem.effectTime == ItemEffectTime.ONCEGET)
            InvokeSingleEffect(newItem, GetComponent<GamePlayerTarget>().GetTargetObject());
    }

    // 개인 아이템의 특정 발동 시점 효과를 전부 발동 (서버)
    [Server]
    public void InvokeItemEffects(ItemEffectTime effectTime, TargetObject sender)
    {
        InvokeEffects(items, effectTime, sender);
    }

    // 임의 목록(개인 아이템/공용 아티팩트)의 특정 발동 시점 효과를 이 플레이어 기준으로 발동 (서버)
    [Server]
    public void InvokeEffects(IEnumerable<Item> source, ItemEffectTime effectTime, TargetObject sender)
    {
        foreach(Item item in source)
        {
            if(item.effectTime != effectTime) continue;
            InvokeSingleEffect(item, sender);
        }
    }

    [Server]
    public void InvokeSingleEffect(Item item, TargetObject sender)
    {
        // itemEffects의 키는 아이템 번호(=효과 메서드명)
        if(ItemData.instance.itemEffects.TryGetValue(item.itemNumber, out ItemEventHanddler effect))
            effect(this, sender, item);
        else
            Debug.LogError($"[GamePlayerItem] 아이템 효과 조회 실패: {item.itemName}({item.itemNumber})");
    }
}
