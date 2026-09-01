using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Mirror;
using ProjectD;
using DG.Tweening;
using TMPro;
using Spine.Unity;
using Spine.Unity.Examples;
using System.Linq;

// TargetObject partial — 캐릭터 전용 로직 (게오르크 고행 / 에리스 변신 / 홍단향 철귀)
public partial class TargetObject
{

    // ----------------------------------------------       게오르크 고행 관련 함수      ---------------------------------------------------//

    public void UsingGoHeng()
    {
        DrawGoHengCard();
    }


    [Command(requiresAuthority=false)]
    private void DrawGoHengCard(NetworkConnectionToClient sender = null)
    {
        // 고행은 이 타겟오브젝트의 소유 플레이어만 발동 가능
        if(sender != null && (player == null || player.connectionToClient != sender))return;
        if(M_TurnManager.instance.phase != BattleTurn.PLAYER_ACTIVE)return;
        if(usingGOHENG || usedGOHENG.Count == 3)return;
        if(player.GetComponent<GamePlayerDeck>().currentIchi < 1)return; // 위대한 자: 고행 드로우는 이치 1 소비
        usingGOHENG = true;
        player.GetComponent<GamePlayerDeck>().currentIchi -= 1;
        int selectedGoheng = 0;
        while(true)
        {
            selectedGoheng = Random.Range(0,3);
            if(!usedGOHENG.Exists(x => x == selectedGoheng))break;
        }
        usedGOHENG.Add(selectedGoheng);
        string nameOfGOHENGCard = "G" + selectedGoheng.ToString();
        if(buffs.FindIndex(buff => buff.type == BuffType.BRILLIANTCURSE) == -1)
            player.GetComponent<GamePlayerDeck>().GenerateCardOnHand(new Card(CardData.instance.cards.Find(card => card.cardNumber == nameOfGOHENGCard)),1);
        else
            player.GetComponent<GamePlayerDeck>().GenerateCardOnHand(new Card(CardData.instance.cards.Find(card => card.cardNumber == nameOfGOHENGCard + "_E")),1);
        if(selectedGoheng == 2)GainBuff(BuffType.GOHANG3_DEBUFF,0,true,true,false,false,this,null);
        if(selectedGoheng == 1)GainBuff(BuffType.GOHANG2_DEBUFF,0,true,true,false,false,this,null);
        foreach(CardOnHand cardOnHand in player.GetComponent<GamePlayerDeck>().cardOnHands)
            cardOnHand.OnChangeCardData(cardOnHand.card,cardOnHand.card);

        // 위대한 자: 고행 I·II·III이 전부 손패에 모이면 (3번째 고행 드로우 시) 영웅 상태로 변신
        if(!isTransformed && HasAllGohengInHand())
            StartCoroutine(GeorkTransform());
    }


    public int heroIchiBonusGiven = 0; // 영웅 상태 — 이번 턴에 부여한 힘의이치 보너스 (턴 종료 시 회수용)


    // 고행 I(G0)·II(G1)·III(G2)이 전부 손패에 있는지 — 강화(_E) 포함. 고행을 써버리면 다시 모을 수 없다 (드로우는 종류당 1회)
    private bool HasAllGohengInHand()
    {
        GamePlayerDeck gamePlayerDeck = player.GetComponent<GamePlayerDeck>();
        for(int i = 0; i < 3; i++)
        {
            string cardNumber = "G" + i;
            bool exists = false;
            foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands)
                if(cardOnHand.card.baseCard.cardNumber == cardNumber || cardOnHand.card.baseCard.cardNumber == cardNumber + "_E")
                {
                    exists = true;
                    break;
                }
            if(!exists)return false;
        }
        return true;
    }


    // 영웅 변신 (위대한 자 — BuffDB GREATMAN/HERO 스펙): HERO 버프 값 = 잔여 턴 (턴 종료마다 M_TurnManager에서 1 감소, 0에서 해제).
    // 지속 턴·보너스 수치는 BalanceDB(HERO_DURATION_TURN / HERO_BONUS_PER_CARD)에서 튜닝.
    IEnumerator GeorkTransform()
    {
        isTransformed = true;
        // 고행길 디버프는 영웅 승화와 함께 소멸 — 원래 고행 II/III '사용' 시 제거되는데, 변신 후엔 카드가 _H가 되어 제거 경로가 사라지므로 여기서 정리
        // (해제 시 재부여하지 않음 — 저주 고행을 사용해 해소한 것과 동일 취급)
        if(HasBuff(BuffType.GOHANG2_DEBUFF))buffs.Remove(buffs.Find(buff => buff.type == BuffType.GOHANG2_DEBUFF));
        if(HasBuff(BuffType.GOHANG3_DEBUFF))buffs.Remove(buffs.Find(buff => buff.type == BuffType.GOHANG3_DEBUFF));
        int heroIndex = GainBuff(BuffType.HERO, BalanceData.Get("HERO_DURATION_TURN", 3), false, false, false, false, this, null);
        buffTrunBeginEffect.Add(heroIndex, CardData.instance.HERO_TurnBeginEffect);
        buffTurnEndEffect.Add(heroIndex, CardData.instance.HERO_TurnEndEffect);
        player.GetComponent<GamePlayerDeck>().ConvertCurseHeroCards(true); // 이치의저주 → 영웅 카드
        M_TurnManager.instance.StartAnimation(this,0,"Transform",false);
        yield return new WaitForSeconds(2.667f);
        M_TurnManager.instance.StartAnimation(this,0,"HIdle",true);
    }


    // 영웅 상태 해제 — HERO 버프가 0이 될 때 M_TurnManager.PlayerEndTurnEffect에서 호출 (등록한 훅은 버프 제거 시 자동 정리)
    [Server]
    public void RevertGeorkTransform()
    {
        isTransformed = false;
        heroIchiBonusGiven = 0;
        player.GetComponent<GamePlayerDeck>().ConvertCurseHeroCards(false); // 영웅 → 이치의저주 원복
        M_TurnManager.instance.StartAnimation(this,0,"Idle",true);
    }


    IEnumerator HongDanHyangEyeFlicker()
    {
        while(true)
        {
            yield return new WaitForSeconds(Random.Range(2f,5f));
            anim.state.SetAnimation(1,"Eye",false);
        }
    }


    // ---- 에리스 변신 매커니즘 (RPG_CONVERSION_DESIGN, 2026-09-01) ----
    // HP 비율로 상태가 정해진다: 최대 HP의 ERIS_MAD_HP_PERCENT(10%) 이하 → 광기(MAD, 가하는 피해 +50%),
    // ERIS_ANGER_HP_PERCENT(40%) 이하 → 1차 변신(ANGER, +20%), 그 위 → NORMAL. 회복으로 비율이 오르면 되돌아간다.
    // HP가 바뀌는 모든 경로(SetPlayerHP)에서 서버가 갱신하고, 피해 배수는 BattleActions.ScaleByErisMode가 적용한다.
    // 애니메이션: 1차 변신 "Change1" → 완료 시 "ChIdle", 광기 "Change2" → "VIdle", 하향은 "RChange2"(광기→1차) / "RChange1"(1차→기본).
    // 변신 모션이 끝나면 OnAnimationComplete(TargetObject.cs)가 현재 erisMode 프리픽스(GetErisMode) + "Idle"을 재생하므로 여기서는 모션만 건다
    public bool erisReviveUsed = false; // 꿈을 본 인형 — 치사 피해를 HP 1로 버티는 부활은 한 전투에 한 번 (TargetObject는 전투마다 새로 스폰되므로 자연 리셋)
    public int erisDestroyUses = 0;     // '권능 : 파괴'(ES7) 이번 전투 사용 횟수 — 사용마다 계수 +30%p (SkillData.Eris), 전투마다 자연 리셋
    public float pendingHitDelay = 0f;  // 공격 모션 시작 후 첫 타격까지의 지연(초) — M_TurnManager.PlayPlayerActionAnimation이 설정(BalanceDB {캐릭터}_{모션}_HIT_DELAY_MS 우선, 없으면 {캐릭터}_ATTACK_HIT_DELAY_MS — 에리스 1.0 / 게오르크 Attack0 0.6·Attack1 0.5), BattleActions.AttackTarget이 소비

    [Server]
    public void UpdateErisMode()
    {
        if(player == null || player.character != Character.ERIS || playerMaxHP <= 0) return;
        int percent = playerHP * 100 / playerMaxHP;
        ErisMode next = ErisMode.NORMAL;
        if(playerHP > 0 && percent <= BalanceData.Get("ERIS_MAD_HP_PERCENT", 10)) next = ErisMode.MAD;
        else if(playerHP > 0 && percent <= BalanceData.Get("ERIS_ANGER_HP_PERCENT", 40)) next = ErisMode.ANGER;
        if(next == erisMode) return;
        ErisMode previous = erisMode;
        erisMode = next; // 모션보다 먼저 확정 — 완료 시 Idle 프리픽스와 피해 배율이 새 상태를 본다
        // 전이별 모션 (기획 확정 2026-09-01) — 완료 후 Idle은 OnAnimationComplete가 상태 프리픽스로 재생. 트랙 1의 광기 추가 모션 잔여는 OnChangedErisMode가 비운다
        //   기본→1차 Change0 / 1차→광기 Change1 / 기본→광기 Change2
        //   광기→기본 RChange2 / 광기→1차 RChange1 / 1차→기본 RChange0
        M_TurnManager.instance.StartAnimation(this, 0, GetErisTransitionMotion(previous, next), false);
    }

    static string GetErisTransitionMotion(ErisMode from, ErisMode to)
    {
        switch(from, to)
        {
            case (ErisMode.NORMAL, ErisMode.ANGER): return "Change0";
            case (ErisMode.ANGER, ErisMode.MAD): return "Change1";
            case (ErisMode.NORMAL, ErisMode.MAD): return "Change2";
            case (ErisMode.MAD, ErisMode.NORMAL): return "RChange2";
            case (ErisMode.MAD, ErisMode.ANGER): return "RChange1";
            case (ErisMode.ANGER, ErisMode.NORMAL): return "RChange0";
            default: return (to == ErisMode.MAD ? "V" : to == ErisMode.ANGER ? "Ch" : "") + "Idle"; // 도달 불가 조합 — 해당 상태 대기로 폴백
        }
    }

    // 치사 피해 시 에리스 부활 판정 — 한 전투에 한 번 HP 1로 살아남는다 (호출부는 이 결과로 HP를 1 또는 0으로 확정). 광기 진입은 SetPlayerHP → UpdateErisMode가 처리
    bool TryErisRevive()
    {
        if(player == null || player.character != Character.ERIS || erisReviveUsed) return false;
        erisReviveUsed = true;
        return true;
    }

    // 에리스 변신에 따른 가하는 피해 배율 % (BattleActions가 사용) — NORMAL 100 / ANGER +20 / MAD +50
    public int ErisAttackPercent()
    {
        if(player == null || player.character != Character.ERIS) return 100;
        switch(erisMode)
        {
            case ErisMode.MAD: return 100 + BalanceData.Get("ERIS_MAD_ATTACK_PERCENT", 50);
            case ErisMode.ANGER: return 100 + BalanceData.Get("ERIS_ANGER_ATTACK_PERCENT", 20);
            default: return 100;
        }
    }


    public int destructionMultiplier = 1; // 파괴의권능 — 현재 실행 중인 공격 카드의 피해 배수 (카드 큐 파이프라인이 설정·리셋, DamageToMonster에서 적용)


    public TargetObject grandiosoTarget; // 그랜디오소(E48) — 카드 실행 후 열리는 선택 팝업 완료 시 추가 피해를 줄 대상


    // 파괴의권능 (에리스 상시 패시브 — BuffDB 정의가 스펙): 공격 카드 사용 시 체력 2 소모(이 효과로는 1 미만 불가),
    // 소모 후 체력이 절반 이하면 공격 카드 2배 피해, 광기(MAD) 상태면 3배. 단말마(DEATHTHROES) 보유 시 발동 배수 +1.
    // E10(뒤틀리는 생명) 계열은 효과를 받지 않음. 체력 소모는 피해가 아닌 코스트 — 방어/단말마 증폭을 타지 않는다.
    [Server]
    public void ApplyPowerOfDestruction(Card card)
    {
        destructionMultiplier = 1;
        if(player == null || player.character != Character.ERIS) return;
        if(card.baseCard.cardType != CardType.ATTACK) return;
        if(card.baseCard.cardNumber == "E10" || card.baseCard.cardNumber == "E10_E") return;
        int hpBefore = playerHP;
        playerHP = Mathf.Max(1, playerHP - 2);
        AccumulateTempestosoHpLost(hpBefore - playerHP); // 체력 상실이므로 템페스토소 드로우 누적 대상
        if(erisMode == ErisMode.MAD) destructionMultiplier = 3;
        else if(playerHP <= playerMaxHP / 2) destructionMultiplier = 2;
        if(destructionMultiplier > 1 && HasBuff(BuffType.DEATHTHROES)) destructionMultiplier += 1; // 단말마: 파괴의권능 +1배
        if(destructionMultiplier > 1 && (card.baseCard.cardNumber == "E51" || card.baseCard.cardNumber == "E51_E")) destructionMultiplier *= 2; // 아프나요?: 파괴의권능 효과 2배 (기획 확정 2026-07-10 — 체력 소모는 2 유지)
    }


    IEnumerator ErisAdditionalMadAnimation()
    {
        WaitForSeconds loopTime = new WaitForSeconds(0.1f);
        float haedTimer = Random.Range(1f,2f);
        float lbTimer = Random.Range(1f,2f);
        float ltTimer = Random.Range(1f,2f);
        float rTimer = Random.Range(1f,2f);
        Spine.TrackEntry track = null;
        while(erisMode == ErisMode.MAD)
        {
            if(haedTimer <= 0f)
            {
                haedTimer = Random.Range(1f,2f);
                track =  anim.state.SetAnimation(1,"VAniHead",false);
                track.MixBlend = Spine.MixBlend.Add;
                track.Alpha = 1f;
            }
            if(lbTimer <= 0f)
            {

                lbTimer = Random.Range(1f,2f);
                if(Random.Range(0,2) == 0)
                    track =  anim.state.SetAnimation(1,"VAniLBArm0",false);
                else
                    track =  anim.state.SetAnimation(1,"VAniLBArm1",false);
                track.MixBlend = Spine.MixBlend.Add;
                track.Alpha = 1f;

            }
            if(ltTimer <= 0f)
            {

                ltTimer = Random.Range(1f,2f);
                if(Random.Range(0,2) == 0)
                    track =  anim.state.SetAnimation(1,"VAniLTArm0",false);
                else
                    track =  anim.state.SetAnimation(1,"VAniLTArm1",false);
                track.MixBlend = Spine.MixBlend.Add;
                track.Alpha = 1f;
            }
            if(rTimer <= 0f)
            {
                rTimer = Random.Range(1f,2f);
                if(Random.Range(0,2) == 0)
                    track =  anim.state.SetAnimation(1,"VAniRArm0",false);
                else
                    track =  anim.state.SetAnimation(1,"VAniRArm1",false);
                track.MixBlend = Spine.MixBlend.Add;
                track.Alpha = 1f;
            }
            haedTimer -= 0.1f;
            lbTimer -= 0.1f;
            ltTimer -= 0.1f;
            rTimer -= 0.1f;
            yield return loopTime;
        }
    }


    public void OnIronDemonAnimationComplete(Spine.TrackEntry trackEntry)
    {
        if(trackEntry.Animation.Name == "Defense")
            ironDemon.GetComponent<SkeletonAnimation>().state.SetAnimation(0,"Idle",true);
    }


    public void ApllyIronDemonAnimationCallbackFunction()
    {
        OnChangedIronDemonLocation(this,this);
    }


}
