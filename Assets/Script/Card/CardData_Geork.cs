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
	// 게오르크
	private void GeneralApDo(TargetObject user,TargetObject tar, int value)    
	{
		if(user.buffs.Find(buff => buff.type == BuffType.THEREISNOJABI) != null) // 자비는 없다 보유시 압도스택 => 데미지(힘의이치영향X)    
		{
			GeneralSingleDamage(tar,3+tar.buffs.Find(buff => buff.type == BuffType.APDO && buff.user == user.player.netId).value);
			tar.buffs.Remove(tar.buffs.Find(buff => buff.type == BuffType.APDO && buff.user == user.player.netId));
		}
		else    
		{
			//GeneralAddBuff(tar,BuffType.APDO,1,user);
		}
	}

	private void GeorkAnimation(TargetObject tar, string normal)    
	{
		M_TurnManager.instance.StartAnimation(tar,0,tar.isTransformed? "H" + normal : normal, false);
	}

	public bool IsGISADO(List<TargetObject> tar)
    {
        return ((int)tar[1].monster.nextTarget == tar[0].player.selectOrder)? true : false;
    }

	public IEnumerator G0(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		for(int i = 0 ;i < 3 ; i++)
		{
			GeorkAnimation(tar[0],"Attack0");
			yield return new WaitForSeconds(0.5f);
			GeneralSingleAttack(tar[0],tar[1],9);
			yield return new WaitForSeconds(0.5f);
			if(!IsGISADO(tar))break;
		}
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}
	public IEnumerator G0_E(Card card,List<TargetObject> tar)
	{
		yield return G0(card,tar);
	}
	public IEnumerator G0_H(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		tar[1].defense = 0;
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],30);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}
	public IEnumerator G0_H_E(Card card,List<TargetObject> tar)
	{
		yield return G0_H(card,tar);
	}
	public IEnumerator G1(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		GeneralGetDefense(tar[0],tar[0],15,card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));	
	}
	public IEnumerator G1_E(Card card,List<TargetObject> tar)
	{
		yield return G1(card,tar);
	}
	public IEnumerator G1_H(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		for(int i = tar[0].buffs.Count - 1 ; i >= 0 ; i--)
		{
			if(tar[0].buffs[i].isDebuff)
				tar[0].buffs[i].value -= 1;
			if(tar[0].buffs[i].value == 0)tar[0].buffs.RemoveAt(i);
		}
		GeneralGetDefense(tar[0],tar[0],20,card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));	
	}
	public IEnumerator G1_H_E(Card card,List<TargetObject> tar)
	{
		yield return G1_H(card,tar);
	}
	public IEnumerator G2(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G2_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G2_H(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G2_H_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G3(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G3_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G3_H(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G3_H_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G4(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G4_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G4_H(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G4_H_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G5(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G5_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G5_H(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G5_H_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G6(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G6_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G6_H(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G6_H_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G7(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G7_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G7_H(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G7_H_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G8(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G8_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G9(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G9_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G10(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G10_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G11(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G11_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G12(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G12_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G13(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G13_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G14(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G14_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G15(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G15_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G16(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G16_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G17(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G17_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G18(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G18_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G19(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G19_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G20(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G20_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G21(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G21_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G22(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G22_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G23(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G23_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G24(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G24_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G25(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G25_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G26(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G26_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G27(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G27_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G28(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G28_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G29(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G29_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G30(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G30_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G31(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G31_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G32(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G32_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G33(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G33_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G34(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G34_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G35(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G35_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G36(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G36_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G37(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G37_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G38(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G38_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G39(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G39_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G40(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G40_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G41(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G41_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G42(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G42_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G43(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G43_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G44(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G44_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G45(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G45_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G46(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G46_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G47(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G47_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G48(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G48_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G49(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G49_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G50(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G50_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G51(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G51_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G52(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G52_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G53(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G53_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G54(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G54_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G55(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G55_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G56(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G56_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G57(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G57_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G58(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G58_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G59(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G59_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G60(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G60_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G61(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G61_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G62(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G62_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G63(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G63_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G64(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G64_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G65(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G65_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G66(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G66_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G67(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G67_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}

}