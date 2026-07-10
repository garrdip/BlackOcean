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


    IEnumerator ErisTransform()
    {
        M_TurnManager.instance.StartAnimation(this,0,"Change1",false);
        erisMode = ErisMode.MAD;
        yield return new WaitForSeconds(2f);
        M_TurnManager.instance.StartAnimation(this,0,"VIdle",true);
    }


    public int destructionMultiplier = 1; // 파괴의권능 — 현재 실행 중인 공격 카드의 피해 배수 (카드 큐 파이프라인이 설정·리셋, DamageToMonster에서 적용)


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
