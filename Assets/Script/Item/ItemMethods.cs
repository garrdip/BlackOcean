using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

// 개인 아이템 효과 구현부 — ItemDB.csv의 Number 컬럼과 동일한 이름의 메서드가 리플렉션으로 바인딩된다 (CardData와 동일한 패턴).
// STARTBATTLE/STARTTURN 효과는 M_TurnManager가 전투 페이즈에서 sender(타겟오브젝트)와 함께 호출하고,
// ONCEGET 효과는 획득 즉시 GamePlayerItem.AddItem에서 호출된다 (이때 sender는 null일 수 있음).
// 아티팩트(ArtifactMethods)와 효과 종류가 겹치므로 실제 로직은 아래 공통 헬퍼에 두고 번호 메서드는 위임만 한다.
public partial class ItemData : SingletonD<ItemData>
{
    // 맹공의 문장 — 전투 시작 시 힘의 이치 증가
    public void I0(GamePlayerItem owner, TargetObject sender, Item item) => EffectGainIchiAttack(sender, item.value);

    // 수호의 문장 — 전투 시작 시 방어의 이치 증가
    public void I1(GamePlayerItem owner, TargetObject sender, Item item) => EffectGainIchiDefense(sender, item.value);

    // 심연의 그릇 — 최대 이치 영구 증가
    public void I2(GamePlayerItem owner, TargetObject sender, Item item) => EffectIncreaseMaxIchi(owner, item.value);

    // 낡은 방패 — 턴 시작 시 방어도 획득
    public void I3(GamePlayerItem owner, TargetObject sender, Item item) => EffectGainDefense(sender, item.value);


    // ---------------------------------------------------- 공통 효과 헬퍼 (아이템/아티팩트 공용) ----------------------------------------------------//

    void EffectGainIchiAttack(TargetObject sender, int value)
    {
        if(sender == null) return;
        sender.GainBuff(BuffType.ICHI_ATTACK, value, false, false, false, false, sender, null);
    }

    void EffectGainIchiDefense(TargetObject sender, int value)
    {
        if(sender == null) return;
        sender.GainBuff(BuffType.ICHI_DEFENSE, value, false, false, false, false, sender, null);
    }

    // 최대 이치 영구 증가 — 전투 종료 시 SetInitialIchi 리셋에도 유지되도록 bonusMaxIchi에 기록
    void EffectIncreaseMaxIchi(GamePlayerItem owner, int value)
    {
        GamePlayerDeck deck = owner.GetComponent<GamePlayerDeck>();
        deck.bonusMaxIchi += value;
        deck.IncreaseMaxIchi(value);
    }

    // 턴 시작 방어도 — PLAYER_PREEFFECT의 방어도 초기화 직후 호출되는 것을 전제로 한다
    void EffectGainDefense(TargetObject sender, int value)
    {
        if(sender == null) return;
        sender.GainDefense(value);
    }
}
