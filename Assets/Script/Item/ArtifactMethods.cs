using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

// 공용 아티팩트 효과 구현부 — ArtifactDB.csv의 Number 컬럼과 동일한 이름의 메서드가 리플렉션으로 바인딩된다.
// 아티팩트는 M_TurnManager.teamArtifacts(파티 공용 목록)에 담기며, 발동 시점마다 모든 플레이어 각각에 대해
// (owner, sender)를 바꿔가며 호출되므로 효과 메서드 자체는 개인 아이템과 동일한 형태다 — 로직은 ItemMethods의 공통 헬퍼 재사용.
public partial class ItemData : SingletonD<ItemData>
{
    // 군단의 깃발 — 전투 시작 시 모든 플레이어 힘의 이치 증가
    public void A0(GamePlayerItem owner, TargetObject sender, Item item) => EffectGainIchiAttack(sender, item.value);

    // 수호자의 성소 — 전투 시작 시 모든 플레이어 방어의 이치 증가
    public void A1(GamePlayerItem owner, TargetObject sender, Item item) => EffectGainIchiDefense(sender, item.value);

    // (A2 심연의 제단 — 최대 이치 증가는 카드 이치 시스템 제거로 폐기, ArtifactDB 행도 삭제 2026-09-01)

    // 불침의 성벽 — 턴 시작 시 모든 플레이어 방어도 획득
    public void A3(GamePlayerItem owner, TargetObject sender, Item item) => EffectGainDefense(sender, item.value);
}
