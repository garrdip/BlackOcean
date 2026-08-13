using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectD;
using Mirror;
using Spine;
using Spine.Unity;

public class TEST : MonoBehaviour
{
    public Button buttonEnhance;
    public Button buttonTranfrom;
    public Button buttonOpenDeckBook;
    public bool isChatBoxActive;
    bool isIncrease = true;

    void Start()
    {
        isChatBoxActive = true;
        buttonEnhance.onClick.AddListener(() => TestEnhance());
        buttonTranfrom.onClick.AddListener(() => TestTransform());
        buttonOpenDeckBook.onClick.AddListener(() => TestOpenDeckBook());
    }

    void Update()
    {
        // [디버그 버튼 토글] F5 : 우측 디버그 버튼 4개(강화/변신/덱북/세이브) 표시·숨김 전환
        if(Input.GetKeyDown(KeyCode.F5)) ToggleDebugButtons();
        // [아이템 시스템 테스트] F6 : 모든 플레이어에게 DB의 전체 개인 아이템 지급 / F7 : 파티에 전체 공용 아티팩트 지급 (호스트 전용)
        if(Input.GetKeyDown(KeyCode.F6)) TestGrantAllItems();
        if(Input.GetKeyDown(KeyCode.F7)) TestGrantAllTeamArtifacts();
    }

    // TestCanvas의 자식(디버그 버튼)들을 일괄 토글 — 버튼이 추가되어도 별도 연결 없이 동작
    void ToggleDebugButtons()
    {
        for(int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            child.SetActive(!child.activeSelf);
        }
    }

    void TestGrantAllItems()
    {
        if(!NetworkServer.active) return;
        foreach(PlayerInterface playerInterface in PlayerRegistry.All)
        {
            foreach(GamePlayer gamePlayer in playerInterface.ownedPlayers)
            {
                GamePlayerItem gamePlayerItem = gamePlayer.GetComponent<GamePlayerItem>();
                foreach(Item item in ItemData.instance.items)
                {
                    gamePlayerItem.AddItem(new Item(item));
                    Debug.Log($"[TEST] 개인 아이템 지급 : {item.itemName}({item.itemNumber}) → {gamePlayer.name}");
                }
            }
        }
    }

    void TestGrantAllTeamArtifacts()
    {
        if(!NetworkServer.active) return;
        foreach(Item artifact in ItemData.instance.artifacts)
        {
            M_TurnManager.instance.AddTeamArtifact(new Item(artifact));
            Debug.Log($"[TEST] 공용 아티팩트 지급 : {artifact.itemName}({artifact.itemNumber})");
        }
    }

    void TestEnhance()
    {
        foreach(TargetObject tar in M_TurnManager.instance.spawnedPlayerList)
        {
            tar.GainBuff(ProjectD.BuffType.BOONGGUI,3,true,false,false,false,tar,null);
            tar.GainBuff(ProjectD.BuffType.ICHI_ATTACK,3,false,false,false,false,tar,null);
            tar.GainBuff(ProjectD.BuffType.ICHI_DEFENSE,3,false,false,false,false,tar,null);
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

    void TestOpenDeckBook()
    {
        DeckBookUI.instance.HandleOpenDeckBook();
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
