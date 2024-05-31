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

    [SyncVar (hook = nameof(OnChanedNextAction))]
    public MonsterAction nextAction;

    public MonsterActionList currentBehavior;
    public int currentBehaviorSequence = 0;

    [SyncVar]
    public TargetObject nextTargetObject;

    [SyncVar (hook = nameof(OnChangedNextTarget))]
    public ActionTarget nextTarget = ActionTarget.UNDEFINED;
    
    [SyncVar (hook = nameof(OnChangedMonsterData))]
    public MonsterData monsterData;

    [SyncVar (hook = nameof(OnChangeParent))]
    public TargetObject parent;
    
    [SyncVar]
    public int turn = 0;

    public bool isActive = false;

    private MaterialPropertyBlock materialPropertyBlock;

    private SkeletonRendererCustomMaterials skeletonRendererCustomMaterials;

    [Header("몬스터 MeshRenderer")]
    public MeshRenderer meshRenderer;

    [Header("몬스터 처치 효과 Material")]
    public Material dissolveMaterial;

    [Header("Dissolve 효과 파티클 오브젝트")]
    public ParticleSystem dissolveParticle;

    SkeletonAnimation anim;
    
    MonsterAction sturnedAction = new MonsterAction("APDO",0,0);

    void Start()
    {
        skeletonRendererCustomMaterials = GetComponent<SkeletonRendererCustomMaterials>();
        skeletonRendererCustomMaterials.enabled = false;
        materialPropertyBlock = new MaterialPropertyBlock();
        meshRenderer = GetComponent<MeshRenderer>();
        anim = GetComponent<SkeletonAnimation>();
        if(dissolveParticle != null){
            dissolveParticle.GetComponent<ParticleSystemRenderer>().sortingLayerName = "FrontLayer";
            dissolveParticle.GetComponent<ParticleSystemRenderer>().sortingOrder = 999;
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if(collider != null && collider.tag.Equals("CardArrowHead") && HP > 0){
            skeletonRendererCustomMaterials.enabled = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if(collider != null && collider.tag.Equals("CardArrowHead") && HP > 0){
            skeletonRendererCustomMaterials.enabled = false;
        }
    }

    public void SetDissolveLevel(float dissolveRatio)
    {
        materialPropertyBlock.SetFloat("_Level", dissolveRatio);
        meshRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    [Server]
    public void SetNextAction()
    {
        GetNextAction();
        nextTarget = GetActionTarget(nextAction.actionTarget);
    }

    ActionTarget GetActionTarget(ActionTarget act)
    {
        ActionTarget retVal = act;
        if(act == ActionTarget.RANDOM_MIDDLE_BACK)
        {
            if(UnityEngine.Random.Range(0,2) == 0)retVal = ActionTarget.MIDDLE;
            else retVal = ActionTarget.BACK;
        }
        if(act == ActionTarget.ENEMY_SINGLE)
        {
            foreach(TargetObject tar in M_TurnManager.instance.spawnedMonsterList)
            {
                if( tar != parent )
                    nextTargetObject = tar;
            }
            if(nextTargetObject == null)
                nextTargetObject = parent;
        }
        if(act == ActionTarget.RANDOM_SINGLE)
        {
            int num = UnityEngine.Random.Range(0,3);
            switch(num)
            {
                case 0 :
                    retVal = ActionTarget.FRONT;
                    break;
                case 1 :
                    retVal = ActionTarget.MIDDLE;
                    break;
                case 2 :
                    retVal = ActionTarget.BACK;
                    break;
            }
        }
        return retVal;
    }

    public virtual void GetNextAction()
    {
        if( currentBehaviorSequence < currentBehavior.ActionList.Count - 1 )
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
                    currentBehavior.ActionList = actionList.ActionList;
                    break;
                }
            }
        }
    }

    public virtual void OnChanedNextAction(MonsterAction oldVal, MonsterAction newVal)
    {

    }

    public virtual void OnChangedNextTarget(ActionTarget oldVal, ActionTarget newVal)
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

    [ClientRpc]
    public void RpcBreakedShield()
    {
        // 실드 파괴음
        AudioClip buffSound = M_SoundManager.instance.sfxClips[SFX_TYPE.Common].Find((audioClip) => audioClip.name.Equals("common_shield_down"));
        M_SoundManager.instance.PlaySFX(buffSound, buffSound.length);
    }

    [Server]
    public virtual void OnChangedSheild(int oldValue, int newValue)
    {

    }

    [Server]
    public virtual void OnAppliedCard(Card card, TargetObject[] tar)
    {

    }

    [Server]    
    public virtual void OnBreakedShield()
    {
        RpcBreakedShield();
    }

    [Server]
    public void APDO()
    {
        nextAction = sturnedAction;
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

    public virtual void OnChangedHpValue(int oldHpValue, int newHpValue)
    {
        if(transform.parent != null){
            TargetObject targetObject = transform.parent.GetComponent<TargetObject>();
            targetObject.selectedNamePlate.SetHPValue(newHpValue,MAXHP,(int)transform.parent.position.x);
            M_EffectManager.instance.DisPlayeDamage(targetObject, (oldHpValue - newHpValue));
        }
    }

    public void OnChangeParent(TargetObject oldPrent, TargetObject newParent)
    {
        transform.SetParent(newParent.transform);
        newParent.monsterName.text = monsterName;
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
            nextTargetObject.DamageToPlayer(nextAction.actionValue + parent.GetBuffValue(BuffType.ICHI_ATTACK));
            M_TurnManager.instance.StartAnimation(nextTargetObject,0,"Defense",false);
            if(nextTargetObject.player.character == Character.HONGDANHYANG && nextTargetObject.ironDemonLocation == nextTargetObject)
                nextTargetObject.ironDemon.GetComponent<SkeletonAnimation>().state.SetAnimation(0,"Defense",false);
        }
        else
        {
            foreach(TargetObject tar in M_TurnManager.instance.GetTargetObjectFromActionTarget(nextTarget))
            {
                if(tar == null) return;
                else if(tar.playerHP == 0)return;

                switch(tar.player.character)
                {
                    case Character.GEORK :
                        if(tar.isTransformed)
                            M_TurnManager.instance.StartAnimation(tar,0,"HDefense0",false);
                        else
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

                tar.DamageToPlayer(nextAction.actionValue + parent.GetBuffValue(BuffType.ICHI_ATTACK));
            }
        }
    }

}
