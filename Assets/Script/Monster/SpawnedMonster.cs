using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Mirror;
using ProjectD;
using Spine.Unity;

public class SpawnedMonster : NetworkBehaviour
{
    [SyncVar]
    public string monsterName;

    [SyncVar]
    public int index;

    [SyncVar]
    public int MAXHP;

    [SyncVar (hook = nameof(OnChangedHpValue))]
    public int _HP;
    public int HP{
        get{
            return _HP;
        }
        set{
            SetMonsterHP(value);
        }
    }

    [SyncVar (hook = nameof(OnChanedNextAction))]
    public MonsterAction nextAction;

    public MonsterActionList currentBehavior;
    public int currentBehaviorSequence = 0;

    [SyncVar]
    public TargetObject nextTargetObject;

    [SyncVar (hook = nameof(OnChangedNextTarget))]
    public ActionTarget nextTarget = ActionTarget.UNDEFINED;
    
    [SyncVar]
    public Monster monster;

    [SyncVar (hook = nameof(OnChangeParent))]
    public TargetObject parent;
    
    [SyncVar]
    public int turn = 0;

    public bool isActive = false;

    private MaterialPropertyBlock materialPropertyBlock;

    private SkeletonRendererCustomMaterials skeletonRendererCustomMaterials;

    public SkeletonAnimation skeletonAnimation;

    [Header("몬스터 MeshRenderer")]
    public MeshRenderer meshRenderer;

    [Header("몬스터 기본 Material")]
    public Material originMaterial;

    [Header("몬스터 처치 효과 Material")]
    public Material dissolveMaterial;

    [Header("Dissolve 효과 파티클 오브젝트")]
    public ParticleSystem dissolveParticle;
    
    [Header("몬스터 스킬 이펙트 스파인 데이터 에셋")]
    public List<SkeletonDataAsset> effectDataAssets = new List<SkeletonDataAsset>();

    [Header("몬스터 스킬 이펙트 파티클")]
    public List<ParticleSystem> effectParticles = new List<ParticleSystem>();

    MonsterAction sturnedAction = new MonsterAction("APDO",0,0);

    public virtual void Start() 
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        if(TryGetComponent(out SkeletonRendererCustomMaterials skeletonRendererCustomMaterials)){
            skeletonRendererCustomMaterials.enabled = false;
        }
        if(TryGetComponent(out MeshRenderer meshRenderer)){
            meshRenderer.sortingOrder = index;
        }
        materialPropertyBlock = new MaterialPropertyBlock();
        if(dissolveParticle != null){
            dissolveParticle.GetComponent<ParticleSystemRenderer>().sortingLayerName = "Effect";
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if(collider != null && collider.tag.Equals("CardArrowHead") && HP > 0){
            if(TryGetComponent(out SkeletonRendererCustomMaterials skeletonRendererCustomMaterials)){
                skeletonRendererCustomMaterials.enabled = true;
            }
            if(TryGetComponent(out MeshRenderer meshRenderer)){
                meshRenderer.sortingLayerName = "FrontLayer";
            }
            parent.targetObjectUI.GetComponent<SortingGroup>().sortingLayerName = "FrontLayer";
            parent.monsterHpCanvas.sortingLayerName = "FrontLayer";
            parent.monsterNameCanvas.sortingLayerName = "FrontLayer";
            parent.monsterShieldCanvas.sortingLayerName = "FrontLayer";
            parent.nextActionCanvas.sortingLayerName = "FrontLayer";
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if(collider != null && collider.tag.Equals("CardArrowHead") && HP > 0){
            if(TryGetComponent(out SkeletonRendererCustomMaterials skeletonRendererCustomMaterials)){
                skeletonRendererCustomMaterials.enabled = false;
            }
            if(TryGetComponent(out MeshRenderer meshRenderer)){
                meshRenderer.sortingLayerName = "BackLayer";
            }
            parent.targetObjectUI.GetComponent<SortingGroup>().sortingLayerName = "BackLayer";
            parent.monsterHpCanvas.sortingLayerName = "BackLayer";
            parent.monsterNameCanvas.sortingLayerName = "BackLayer";
            parent.monsterShieldCanvas.sortingLayerName = "BackLayer";
            parent.nextActionCanvas.sortingLayerName = "BackLayer";
        }
    }

    void OnMouseEnter()
    {
        if(!M_CardManager.instance.isArrowActive && !M_CardManager.instance.IsDragCardExist()){
            TargetIndicatorController.instance.EnalbleTargetIndicatorByMonster(nextTarget, parent.netId);
        }
    }

    void OnMouseExit()
    {
        if(!M_CardManager.instance.isArrowActive && !M_CardManager.instance.IsDragCardExist()){
            TargetIndicatorController.instance.DisableTargetIndicator();
        }
    }

    // 몬스터 스킬 이펙트 오브젝트 동적 생성
    public IEnumerator StartEffect(SkeletonDataAsset skeletonDataAsset, string animationName, Vector3 position, AudioClip sfx, string layer)
    {
        yield return new WaitForSeconds(0.01f); 
        var spineObject = SkeletonAnimation.NewSkeletonAnimationGameObject(skeletonDataAsset); // https://ko.esotericsoftware.com/spine-unity#Advanced---Instantiation-at-Runtime
        spineObject.gameObject.name = animationName;
        EffectBase cardEffectBase = spineObject.gameObject.AddComponent<EffectBase>();
        cardEffectBase.sfx = sfx;
        spineObject.transform.position = position;
        spineObject.GetComponent<MeshRenderer>().sortingLayerName = layer;
        spineObject.AnimationState.SetAnimation(0, animationName, false);
    }

    // 사라지는 이펙트
    public void StartDissolveEffect(System.Action dissloveCallback = null)
    {
        SkeletonAnimation skeletonAnimation = GetComponent<SkeletonAnimation>();
        skeletonAnimation.timeScale = 0f;
        skeletonAnimation.CustomMaterialOverride[originMaterial] = dissolveMaterial; // 몬스터의 머티리얼을 dissolveMaterial로 변경
        dissolveParticle.gameObject.SetActive(true); // dissolveParticle 활성화
        StartCoroutine(DissolveCoroutine(() => {
            dissloveCallback();
        }));
    }
    
    // Dissolve 효과 코루틴 (materialPropertyBlock을 이용해 Dissolve 머티리얼의 프로퍼티값 변경)
    public IEnumerator DissolveCoroutine(System.Action callback = null)
    {
        float duration = 2.5f;
        float timer = 0f;
        while (timer < duration)
        {
            float dissolveRatio = timer / duration;
            materialPropertyBlock.SetFloat("_Level", dissolveRatio);
            meshRenderer.SetPropertyBlock(materialPropertyBlock);
            timer += Time.deltaTime;
            yield return null;
        }
        if(callback != null){
            callback();
        }
    }

    // ------------------------------------------------------------------ Server Method ------------------------------------------------------------------------//

    [Server]
    private void SetMonsterHP(int newHp)
    {
        _HP = Mathf.Clamp(newHp, 0, MAXHP); 
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
            foreach(MonsterActionList actionList in monster.behavior)
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

    [Server]
    public virtual void OnBreakedShield()
    {
        RpcBreadkShield();
    }

    [Server]
    public void APDO()
    {
        nextAction = sturnedAction;
    }

    // ------------------------------------------------------------------ Rpc Method ------------------------------------------------------------------------//
    
    [ClientRpc]
    public virtual void DoAnimation(string actionName)
    {
        meshRenderer.sortingOrder = Const.MAX_ORDER;
        skeletonAnimation.state.SetAnimation(1, actionName, false);
        parent.nextActionIndicator.NextActionIndicatorFocusOn();
    }

    [ClientRpc]
    public virtual void ReturnToIdleAnimation()
    {
        meshRenderer.sortingOrder = index;
        skeletonAnimation.state.SetAnimation(1, "Idle", true);
        parent.nextActionIndicator.NextActionIndicatorFocusOff();
    }

    [ClientRpc]
    public void RpcBreadkShield()
    {
        // 실드 파괴음
        AudioClip buffSound = M_SoundManager.instance.sfxClips[SFX_TYPE.Common].Find((audioClip) => audioClip.name.Equals("common_shield_down"));
        M_SoundManager.instance.PlaySFX(buffSound, buffSound.length);
    }


    /*
        [몬스터 스킬 이펙트 스파인 오브젝트 생성]
        effectIndex : 스켈레톤 데이터 에셋 인덱스 값
        animationName : 스켈레톤 데이터 에셋에서 실행할 애니매이션 이름
        position : 이펙트가 보여질 위치 값
        sft_type : 사운드 매니저에서 해당 이펙트의 효과음이 있는 리스트의 카테고리 값(Dic 키값)
        audioClipIndex : 사운드 매니저에서 해당 이펙트 효과음 오디오 클립 인덱스 값
        layer : 이펙트 랜더링 정렬 값
    */
    [ClientRpc]
    public void RpcStartSkillEffect(int effectIndex, string animationName, Vector3 position, SFX_TYPE sfx_type, int audioClipIndex, string layer)
    {
        AudioClip audioClip = M_SoundManager.instance.sfxClips[sfx_type][audioClipIndex];
        StartCoroutine(StartEffect(
            effectDataAssets[effectIndex],
            animationName,
            position,
            audioClip,
            layer)
        );
    }

    /*
        [몬스터 스킬 이펙트 파티클 생성]
        index : 파티클 리스트에서 해당 파티클의 인덱스 값
        position : 이펙트 보여질 위치 값
    */
    [ClientRpc]
    public void RpcStartSkillParticle(int index, Vector3 position)
    {
        ParticleSystem particleSystem = Instantiate(effectParticles[index], position, Quaternion.identity);
        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "Effect";
    }

    // ------------------------------------------------------------------ SyncVar Hook ------------------------------------------------------------------------//

    public virtual void OnChanedNextAction(MonsterAction oldVal, MonsterAction newVal)
    {

    }

    public virtual void OnChangedNextTarget(ActionTarget oldVal, ActionTarget newVal)
    {
        
    }

    public virtual void OnChangedSheild(int oldValue, int newValue)
    {

    }

    public virtual void OnChangedHpValue(int oldHpValue, int newHpValue)
    {
        if(transform.parent != null){
            TargetObject targetObject = transform.parent.GetComponent<TargetObject>();
            M_EffectManager.instance.OnHitEffectParticle(transform.position + new Vector3(0f, 3f, 0f));
            M_EffectManager.instance.DisPlayeDamage(targetObject, (oldHpValue - newHpValue));
            targetObject.selectedNamePlate.SetHpValue(newHpValue, MAXHP, targetObject);
        }
    }

    public void OnChangeParent(TargetObject oldPrent, TargetObject newParent)
    {
        transform.SetParent(newParent.transform);
        transform.localPosition = Vector3.zero;
        // UI 및 캔버스 오브젝트들을 매쉬랜더러 보다 1단계 위에 랜더링
        newParent.targetObjectUI.GetComponent<SortingGroup>().sortingOrder = index + 1;
        newParent.monsterHpCanvas.sortingOrder = index + 1;
        newParent.monsterNameCanvas.sortingOrder = index + 1;
        newParent.monsterShieldCanvas.sortingOrder = index + 1;
        newParent.nextActionCanvas.sortingOrder = index + 1;
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
