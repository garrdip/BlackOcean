using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using ProjectD;
using Spine.Unity;
public class SpawnedMonster : NetworkBehaviour
{
    public string monsterName;

    [SyncVar]
    public int MAXHP;

    [SyncVar (hook = nameof(OnChangedHpValue))]
    public int HP;

    [SyncVar (hook = nameof(OnChangedSheild))]
    public int sheild;

    public int[] aggro = new int[3];

    [SyncVar (hook = nameof(OnChanedNextAction))]
    public MonsterAction nextAction;

    public MonsterActionList currentBehavior;
    public int currentBehaviorSequence = 0;

    [SyncVar]
    public TargetObject nextTargetPlayer;
    [SyncVar]
    public ActionTarget nextTarget;
    
    [SyncVar (hook = nameof(OnChangedMonsterData))]
    public MonsterData monsterData;

    [SyncVar (hook = nameof(OnChangeParent))]
    public TargetObject parent;

    public bool isActive = false;

    [Header("몬스터 MeshRenderer")]
    public MeshRenderer meshRenderer;

    [Header("몬스터 기본 Material")]
    public Material defaultMaterial;

    [Header("몬스터 외곽선 Material")]
    public Material outLineMaterial;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();   
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if(collider != null && collider.tag.Equals("CardArrowHead")){
            meshRenderer.material = outLineMaterial;
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if(collider != null && collider.tag.Equals("CardArrowHead")){
            meshRenderer.material = defaultMaterial;
        }
    }

    [Server]
    public void SetNextAction()
    {
        GetNextAction();
        nextTarget = nextAction.actionTarget;
    }

    void GetNextAction()
    {
        if(currentBehavior.ActionList.Count - 1 < currentBehaviorSequence && currentBehavior.ActionList.Count != 0)
        {
            currentBehaviorSequence++;
            nextAction = currentBehavior.ActionList[currentBehaviorSequence];
        }
        else
        {
            int randomValue = UnityEngine.Random.Range(0,100); // 0 ~ 99
            foreach(MonsterActionList actionList in monsterData.behavior)
            {
                randomValue -= actionList.frequency;
                if(randomValue < 0) {
                    nextAction = actionList.ActionList[0];
                    currentBehaviorSequence = 0;
                    break;
                }
            }
        }
    }

    public virtual void OnChanedNextAction(MonsterAction oldVal, MonsterAction newVal)
    {

    }

    [Server]
    public virtual IEnumerator DoAction()
    {
        yield return null;
    }

    [Server]
    public virtual IEnumerator OnHitAnimation()
    {
        yield return null;
    }

    [ClientRpc]
    public virtual void ReturnToIdleAnimation()
    {

    }

    [Server]
    public virtual void OnChangedSheild(int oldValue, int newValue)
    {

    }

    [Server]
    public virtual void OnAppliedCard(Card card, TargetObject[] tar)
    {

    }

    // ------------------------------------------------------------------ SyncVar Hook ------------------------------------------------------------------------//

    public void OnChangedMonsterData(MonsterData oldVal , MonsterData newVal)
    {
        monsterName = monsterData.name;
        MAXHP = monsterData.MAXHP;
        HP = monsterData.MAXHP;
        sheild = 0;
        //SyncVar Data는 서버에서 관리
        if(isServer)
        {
            HP = monsterData.MAXHP;
        }
    }

    public void OnChangedHpValue(int oldHpValue, int newHpValue)
    {
        if(HP <= 0)
        {
            if(isServer){
                if(parent.isCloneData)
                {
                    M_TurnManager.instance.cloneMonsterList.Remove(parent);
                    parent.origin.clone = null;
                }
                else
                    M_TurnManager.instance.spawnedMonsterList.Remove(parent);
                foreach(TargetObject tar in M_TurnManager.instance.spawnedPlayerList)
                {
                    if(tar.ironDemonLocation == parent.GetComponent<TargetObject>())
                    {
                        tar.SetIronDemonParent(tar.transform);
                    }
                }
                foreach(TargetObject tar in M_TurnManager.instance.clonePlayerList)
                {
                    if(tar.ironDemonLocation == parent.GetComponent<TargetObject>())
                    {
                        tar.SetIronDemonParent(tar.transform);
                    }
                }
                NetworkServer.Destroy(this.gameObject);
            }
            return;
        }
        if(transform.parent != null){
            transform.parent.GetComponent<TargetObject>().selectedNamePlate.SetHPValue(newHpValue,MAXHP,(int)transform.parent.position.x);
        }
    }

    public void OnChangeParent(TargetObject oldPrent, TargetObject newParent)
    {
        transform.SetParent(newParent.transform);
        newParent.targetObjectName.text = monsterName;
        StartCoroutine(InitMonsterNamePlate());
    }

    IEnumerator InitMonsterNamePlate()
    {
        while(true)
        {
            if(parent.monster != null)
            {
                if(parent.monster.MAXHP != 0)
                {
                    parent.InitMonsterNamePlate();
                }
            }
            yield return new WaitForSeconds(0.01f);
        }
    }
    
    //-------------------------------------- Battle Method ----------------------------------//
    public void GeneralAttack()
    {
        if(nextTarget == ActionTarget.FIXEDPLAYER)
        {
            // 고정 상대일경우 수정 필요!!//
            nextTargetPlayer.DamageToPlayer(nextAction.actionValue + parent.GetBuffValue(BuffType.ICHI_ATTACK));
            M_TurnManager.instance.StartAnimation(nextTargetPlayer,0,"Defense",false);
            if(nextTargetPlayer.player.character == Character.HONGDANHYANG && nextTargetPlayer.ironDemonLocation == nextTargetPlayer)
                nextTargetPlayer.ironDemon.GetComponent<SkeletonAnimation>().state.SetAnimation(0,"Defense",false);
        }
        else
        {
            foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget))
            {
                tar.DamageToPlayer(nextAction.actionValue + parent.GetBuffValue(BuffType.ICHI_ATTACK));
                switch(tar.player.character)
                {
                    case Character.GEORK :
                        M_TurnManager.instance.StartAnimation(tar,0,"Defense0",false);
                        break;
                    case Character.ERIS :
                        M_TurnManager.instance.StartAnimation(tar,0,tar.GetErisMode() + "Defense0",false);
                        break;
                    case Character.HONGDANHYANG :
                        M_TurnManager.instance.StartAnimation(tar,0,"Defense",false);
                        break;
                }
                
                if(tar.player.character == Character.HONGDANHYANG && tar.ironDemonLocation == tar)
                    tar.ironDemon.GetComponent<SkeletonAnimation>().state.SetAnimation(0,"Defense",false);
            }
        }
    }

}
