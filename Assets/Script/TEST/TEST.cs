using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TEST : MonoBehaviour
{
    public GameObject gameSceneChatBox;
    public Button buttonEnhance;
    public Button buttonChangeChatBoxState;
    public bool isChatBoxActive;

    void Start()
    {
        isChatBoxActive = true;
        buttonEnhance.onClick.AddListener(() => TestEnhance());
        buttonChangeChatBoxState.onClick.AddListener(() => TestChatBoxState());
    }

    void TestEnhance()
    {
        foreach(TargetObject tar in M_TurnManager.instance.spawnedPlayerList)
        {
            tar.GainBuff(ProjectD.BuffType.ICHI_ATTACK,3,false,true,false,tar);
            tar.GainBuff(ProjectD.BuffType.ICHI_DEFENSE,3,false,true,false,tar);
        }
        foreach(TargetObject tar in M_TurnManager.instance.clonePlayerList)
        {
            tar.GainBuff(ProjectD.BuffType.ICHI_ATTACK,3,false,true,false,tar);
            tar.GainBuff(ProjectD.BuffType.ICHI_DEFENSE,3,false,true,false,tar);
        }
    }

    void TestChatBoxState()
    {
        isChatBoxActive = !isChatBoxActive;
        gameSceneChatBox.SetActive(isChatBoxActive);
    }
}
