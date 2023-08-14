using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TEST : MonoBehaviour
{
    public Button testBtn;
    void Start()
    {
        testBtn.onClick.AddListener(()=>TESTHandler());
    }

    void TESTHandler()
    {
        foreach(TargetObject tar in M_TurnManager.instance.spawnedPlayerList)
        {
            tar.GainBuff(ProjectD.BuffType.ICHI_ATTACK,1);
            tar.GainBuff(ProjectD.BuffType.ICHI_DEFENSE,1);
        }
    }

}
