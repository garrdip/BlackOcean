using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.IO;
using System;
using ProjectD;
using Mirror;
using Spine.Unity;

public partial class CardData : SingletonD<CardData>
{
    // 권능 : 찌르기
    public IEnumerator E0(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.8f);
		GeneralSingleAttack(tar[0],tar[1],5);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E0_E(Card card,List<TargetObject> tar)
    {
        yield return E0(card,tar);
    }

    // 권능 : 깊게 찌르기
    public IEnumerator E1(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
		GeneralSingleAttack(tar[0],tar[1],8);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E1_E(Card card,List<TargetObject> tar)
    {
        yield return E1(card,tar);
    }
    // 변형된 팔
    public IEnumerator E2(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		GeneralGetDefense(tar[0],tar[0],4,card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E2_E(Card card,List<TargetObject> tar)
    {
        yield return E2(card,tar);
    }
    // 구원의 팔
    public IEnumerator E3(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		GeneralGetDefense(tar[0],tar[1],3,card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E3_E(Card card,List<TargetObject> tar)
    {
        yield return E3(card,tar);
    }
    
    // 부서지세요
    public IEnumerator E4(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.8f);
		GeneralSingleAttack(tar[0],tar[1],5);
        tar[1].GainBuff(BuffType.BOONGGUI,1,true,false,true,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E4_E(Card card,List<TargetObject> tar)
    {
        yield return E4(card,tar);
    }

    // 별 따기
    public IEnumerator E5(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.8f);
		GeneralSingleAttack(tar[0],tar[1],2);
        tar[0].GainBuff(BuffType.BYEOLMURI,1,false,false,false,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E5_E(Card card,List<TargetObject> tar)
    {
        yield return E5(card,tar);
    }

    // 별의 축복
    public IEnumerator E6(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.8f);
		GeneralGetDefense(tar[0],tar[1],5,card);
        tar[1].GainBuff(BuffType.BYEOLMURI,1,false,false,false,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E6_E(Card card,List<TargetObject> tar)
    {
        yield return E6(card,tar);
    }
    // 돌로레
    public IEnumerator E7(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.8f);
		tar[0].player.GetComponent<GamePlayerDeck>().CmdSpawnCardOnHand(2);
        tar[0].DamageToPlayer(6);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E7_E(Card card,List<TargetObject> tar)
    {
        yield return E7(card,tar);
    }
    
    // 한번 볼까요
    public IEnumerator E8(Card card,List<TargetObject> tar)
    {
        cardSelectCallBack = H8_CallBack;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.8f);
        tar[0].player.GetComponent<GamePlayerDeck>().AddDrawCard(3);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
        yield return null;
    }

    public void H8_CallBack(GamePlayerDeck gpd, List<CardOnHand> cards)
    {
        gpd.ServerDestroyCardOnHandToForgotten(cards[2]);
        gpd.ServerDestroyCardOnHandToForgotten(cards[1]);
    }

    public IEnumerator E8_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E9(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E9_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E10(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E10_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E11(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E11_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E12(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E12_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E13(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E13_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E14(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E14_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E15(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E15_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E16(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E16_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E17(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E17_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E18(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E18_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E19(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E19_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E20(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E20_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E21(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E21_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E22(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E22_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E23(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E23_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E24(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E24_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E25(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E25_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E26(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E26_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E27(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E27_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E28(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E28_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E29(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E29_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E30(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E30_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E31(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E31_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E32(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E32_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E33(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E33_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E34(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E34_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E35(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E35_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E36(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E36_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E37(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E37_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E38(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E38_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E39(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E39_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E40(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E40_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E41(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E41_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E42(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E42_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E43(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E43_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E44(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E44_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E45(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E45_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E46(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E46_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E47(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E47_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E48(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E48_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E49(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E49_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E50(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E50_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E51(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E51_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E52(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E52_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E53(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E53_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E54(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E54_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E55(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E55_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E56(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E56_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E57(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E57_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E58(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E58_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E59(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E59_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E60(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E60_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }       
}