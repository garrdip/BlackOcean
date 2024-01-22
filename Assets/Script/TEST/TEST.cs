using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectD;
using Spine;
using Spine.Unity;

public class TEST : MonoBehaviour
{
    public Button buttonEnhance;
    public Button buttonTranfrom;
    public bool isChatBoxActive;
    bool isIncrease = true;

    void Start()
    {
        isChatBoxActive = true;
        buttonEnhance.onClick.AddListener(() => TestEnhance());
        buttonTranfrom.onClick.AddListener(() => TestTransform());
    }

    void TestEnhance()
    {
        foreach(TargetObject tar in M_TurnManager.instance.spawnedPlayerList)
        {
            tar.GainBuff(ProjectD.BuffType.BOONGGUI,3,true,false,false,tar,null);
            tar.GainBuff(ProjectD.BuffType.ICHI_ATTACK,3,false,false,false,tar,null);
            tar.GainBuff(ProjectD.BuffType.ICHI_DEFENSE,3,false,false,false,tar,null);
        }
    }

    void TestTransform()
    {
        foreach(TargetObject tar in M_TurnManager.instance.spawnedPlayerList)
        {
            if(tar.player.character == ProjectD.Character.GEORK)
            {
                tar.isTransformed = true;
                StartCoroutine(GeorkTransfrom(tar));
            }
            if(tar.player.character == ProjectD.Character.ERIS)
            {
                StartCoroutine(ErisTransform(tar));
            }
        }
    }

    IEnumerator GeorkTransfrom(TargetObject tar)
    {
        M_TurnManager.instance.StartAnimation(tar,0,"Transform",false);
        yield return new WaitForSeconds(2.667f);
        M_TurnManager.instance.StartAnimation(tar,0,"HIdle",true);
    }

    IEnumerator ErisTransform(TargetObject tar)
    {
        switch(tar.erisMode)
        {
            case ErisMode.NORMAL :
                isIncrease = true;
                M_TurnManager.instance.StartAnimation(tar,0,"Change0",false);
                yield return new WaitForSeconds(2f);
                M_TurnManager.instance.StartAnimation(tar,0,"ChIdle",true);
                tar.erisMode = ErisMode.ANGER;
                Debug.Log("Action!");
                break;
            case ErisMode.ANGER :
                if(isIncrease)
                {
                    M_TurnManager.instance.StartAnimation(tar,0,"Change1",false);
                    yield return new WaitForSeconds(2f);
                    M_TurnManager.instance.StartAnimation(tar,0,"VIdle",true);
                    tar.erisMode = ErisMode.MAD;
                }
                else
                {
                    M_TurnManager.instance.StartAnimation(tar,0,"RChange0",false);
                    yield return new WaitForSeconds(2f);
                    M_TurnManager.instance.StartAnimation(tar,0,"Idle",true);
                    tar.erisMode = ErisMode.NORMAL;
                }
                break;
            case ErisMode.MAD :
                isIncrease = false;
                M_TurnManager.instance.StartAnimation(tar,0,"RChange1",false);
                yield return new WaitForSeconds(2f);
                M_TurnManager.instance.StartAnimation(tar,0,"ChIdle",true);
                tar.erisMode = ErisMode.ANGER;
                break;
        }
    }
}
