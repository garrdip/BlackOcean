using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TEST : MonoBehaviour
{
    public GameObject gameSceneChatBox;
    public Button buttonEnhance;
    public Button buttonChangeChatBoxState;
    public Button buttonTranfrom;
    public bool isChatBoxActive;

    void Start()
    {
        isChatBoxActive = true;
        buttonEnhance.onClick.AddListener(() => TestEnhance());
        buttonChangeChatBoxState.onClick.AddListener(() => TestChatBoxState());
        buttonTranfrom.onClick.AddListener(() => TestTransform());

    }

    void TestEnhance()
    {
        foreach(TargetObject tar in M_TurnManager.instance.spawnedPlayerList)
        {
            tar.GainBuff(ProjectD.BuffType.ICHI_ATTACK,3,false,false,false,tar);
            tar.GainBuff(ProjectD.BuffType.ICHI_DEFENSE,3,false,false,false,tar);
        }
        foreach(TargetObject tar in M_TurnManager.instance.clonePlayerList)
        {
            tar.GainBuff(ProjectD.BuffType.ICHI_ATTACK,3,false,false,false,tar);
            tar.GainBuff(ProjectD.BuffType.ICHI_DEFENSE,3,false,false,false,tar);
        }
    }

    void TestChatBoxState()
    {
        isChatBoxActive = !isChatBoxActive;
        gameSceneChatBox.SetActive(isChatBoxActive);
    }

    void TestTransform()
    {
        //TODO
        //foreach(TargetObject tar in M_TurnManager.instance.spawnedPlayerList)
        //{
        //    if(tar.player.character == ProjectD.Character.GEORK)
        //    {
        //        tar.isTransformed = true;
        //        StartCoroutine(GeorkTransfrom(tar));
        //    }
        //}
    }

    IEnumerator GeorkTransfrom(TargetObject tar)
    {
        M_TurnManager.instance.StartAnimation(tar,0,"Transform",false);
        yield return new WaitForSeconds(2.667f);
        M_TurnManager.instance.StartAnimation(tar,0,"HIdle",true);
    }
}
