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
        usingGOHENG = true;
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
