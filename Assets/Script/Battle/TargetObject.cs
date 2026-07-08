using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Mirror;
using ProjectD;
using DG.Tweening;
using TMPro;
using Spine.Unity;
using Spine.Unity.Examples;
using System.Linq;

public partial class TargetObject : NetworkBehaviour
{
    [Header("공통 참조값")]
    public SkeletonAnimation anim;
    public GameObject targetObjectUI;
    public NamePlate selectedNamePlate;
    public BuffIndicatorController buffIndicator;
    public NextActionIndicator nextActionIndicator;
    public Dictionary<int,CardBlessEffect> buffTrunBeginEffect = new Dictionary<int, CardBlessEffect>();
    public Dictionary<int,CardBlessEffect> buffCardDrowEffect = new Dictionary<int, CardBlessEffect>();
    public Dictionary<int,CardBlessEffect> buffTurnEndEffect = new Dictionary<int, CardBlessEffect>();
    public Dictionary<int,CardBlessEffect> buffCardUseEffect = new Dictionary<int, CardBlessEffect>();


    [Header("플레이어용 참조값")]
    public GameObject avatar;
    public GameObject playerNamePlate;
    public Canvas playerHpCanvas;
    public Canvas playerNameCanvas;
    public Canvas playerShieldCanvas;
    public TextMeshProUGUI playerName;
    public Canvas playerMessageCavnas;
    public TextMeshProUGUI playerMessageBubble;
    public List<GameObject> characters; 
    public SkeletonDataAsset[] ironDemonData = new SkeletonDataAsset[2];
    public GameObject ironDemon; // 철귀
    public bool isIronDemonMoving = false;
    public int sizeOfIronDemon
    {
        get
        {
            return buffs.Find(x => x.type == BuffType.IRONDEMON).value;
        }
        set
        {
            buffs.Find(x => x.type == BuffType.IRONDEMON).value = value;
        }
    }
    public bool usingGOHENG = false;
    public List<int> usedGOHENG = new List<int>();


    [Header("몬스터용 참조값")]
    public GameObject monsterNamePlate;
    public Canvas monsterHpCanvas;
    public Canvas monsterNameCanvas;
    public Canvas monsterShieldCanvas;
    public Canvas nextActionCanvas;
    public TextMeshProUGUI monsterName;


    [Header("네트워크 참조값")]
    [SyncVar (hook = nameof(OnChangeObjectType))]
    public ObjectType objectType;

    // -------------------------------   플레이어용 Syncvar 변수들   ------------------------------------//

    [SyncVar]
    public GamePlayer player;

    [SyncVar (hook = nameof(OnChangedPlayerHP))]
    public int _playerHP;
    public int playerHP{
        get{
            return _playerHP;
        }
        set{
            SetPlayerHP(value);
        }
    }
    
    [SyncVar]
    public int playerMaxHP;

    [SyncVar (hook = nameof(OnChangedIronDemonLocation))]
    public TargetObject ironDemonLocation;

    [SyncVar]
    public bool isTransformed = false;

    [SyncVar (hook = nameof(OnChangedErisMode))]
    public ErisMode erisMode = ErisMode.NORMAL;
    public string GetErisMode()
    {
        string retVal = null;
        switch(erisMode)
        {
            case ErisMode.NORMAL : retVal = ""; break;
            case ErisMode.ANGER : retVal = "Ch"; break;
            case ErisMode.MAD : retVal = "V"; break;
        }
        return retVal;
    }

    [SyncVar]
    public int currentApDoRequirement = 8;

    [SyncVar]
    public bool isDying = false;

    // -------------------------------   몬스터용 Syncvar 변수들   ------------------------------------//
    
    [SyncVar]
    public SpawnedMonster monster;
    
    // -------------------------------   전투용 Syncvar 변수들   ------------------------------------//

    [SyncVar(hook = nameof(OnChangedDefense))]
    public int defense = 0;

    public readonly SyncList<Buff> buffs = new SyncList<Buff>();


    void Start()
    {
        buffs.Callback += OnChangedBuff;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if(objectType == ObjectType.PLAYER){
            player.onChangePlayerOrder += OnChangePlayerOrder; // 플레이어 타겟오브젝트인 경우 오더 변경 델리게이트 이벤트 리스너 추가
            InitTargetObjectPlayer(player);
            anim = avatar.GetComponent<SkeletonAnimation>();
        }else if(objectType == ObjectType.NPC){
            InitTargetObjectNPC(monster);
            anim = monster.GetComponent<SkeletonAnimation>();
        }else{
            InitTargetObjectMonster(monster);
            anim = monster.GetComponent<SkeletonAnimation>();
        }
        if(anim != null){
            anim.state.Event += OnAnimationEvent;
            anim.state.Start += OnAnimationStart;
            anim.state.Complete += OnAnimationComplete;
            anim.timeScale = Random.Range(0.9f,1.1f); // 칼군무 방지 코드
        }    
    }

    void OnDestroy()
    {
        transform.DOKill();
        if(playerMessageCavnas != null) // 몬스터/NPC 타입은 플레이어 말풍선 캔버스가 없음
            playerMessageCavnas.GetComponent<CanvasGroup>().DOKill();
        // 오브젝트 제거 시 델리게이트 이벤트 리스너 해제
        if(anim != null){
            anim.state.Event -= OnAnimationEvent;
            anim.state.Start -= OnAnimationStart;
            anim.state.Complete -= OnAnimationComplete;
        }
        if(ironDemon != null)
        {
            ironDemon.GetComponent<SkeletonAnimation>().state.Complete -= OnIronDemonAnimationComplete;
        }
        if(player != null){
            player.onChangePlayerOrder -= OnChangePlayerOrder;
        }
    }

    // 플레이어 타입의 타겟오브젝트 초기화
    public void InitTargetObjectPlayer(GamePlayer player)
    {
        selectedNamePlate = playerNamePlate.GetComponent<NamePlate>();
        selectedNamePlate.InitHpValue(playerHP, playerMaxHP);
        monsterNamePlate.SetActive(false);
        playerName.text = player.objectOwner.steamPersonaName;
        nextActionIndicator.gameObject.SetActive(false);
        switch(player.character)
        {
            case Character.GEORK :
                avatar = Instantiate(characters[2],transform.position,Quaternion.identity,transform);
                break;
            case Character.ERIS :
                avatar = Instantiate(characters[1],transform.position,Quaternion.identity,transform);
                break;
            case Character.HONGDANHYANG :
                avatar = Instantiate(characters[0],transform.position,Quaternion.identity,transform);
                ironDemon = Instantiate(characters.Find(x => x.name == "IronDemon"),transform.position,Quaternion.identity,transform);
                if(NetworkClient.localPlayer.transform.GetChild(0).GetComponent<GamePlayer>() == player){
                    ironDemon.GetComponent<SkeletonRenderTexture>().color.a = 1f;
                }else{
                    ironDemon.GetComponent<SkeletonRenderTexture>().color.a = 0.5f;
                }
                ironDemonLocation = this;
                ironDemon.GetComponent<SkeletonAnimation>().timeScale = Random.Range(0.9f,1.1f);
                ironDemon.GetComponent<SkeletonAnimation>().state.Complete += OnIronDemonAnimationComplete;
                StartCoroutine(HongDanHyangEyeFlicker());  
                break;
        }
        SetPlayerTargetObjectOrder(player.selectOrder);
    }

    // NPC 타입의 타겟오브젝트 초기화
    public void InitTargetObjectNPC(SpawnedMonster monster)
    {
        selectedNamePlate = monsterNamePlate.GetComponent<NamePlate>();
        selectedNamePlate.InitHpValue(monster.HP, monster.MAXHP);
        monsterName.text = monster.monsterName;
        playerNamePlate.SetActive(false);
        nextActionIndicator.gameObject.SetActive(false);
    }

    // 몬스터 타입의 타겟오브젝트 초기화
    public void InitTargetObjectMonster(SpawnedMonster monster)
    {
        selectedNamePlate = monsterNamePlate.GetComponent<NamePlate>();
        selectedNamePlate.InitHpValue(monster.HP, monster.MAXHP);
        monsterName.text = monster.monsterName;
        playerNamePlate.SetActive(false);
        nextActionIndicator.StartBounce(monster.index);
    }

    public void OnChangePlayerOrder(int order)
    {
        transform.DOMove(M_TurnManager.instance.targetObjectPosition[order], 0.5f); // 플레이어 오더 변경 이벤트 수신하여 타겟오브젝트 위치 이동
        SetPlayerTargetObjectOrder(order);
    }

    // 플레이어 타겟오브젝트의 정렬값 변경 (로컬플레이어는 최대값으로 설정하여 항상 맨 앞, 나머지는 player의 selectorder값으로 정렬값 설정)
    private void SetPlayerTargetObjectOrder(int order)
    {
        avatar.GetComponent<MeshRenderer>().sortingOrder = player.objectOwner.isLocalPlayer ? Const.MAX_ORDER :  order;
        targetObjectUI.GetComponent<SortingGroup>().sortingOrder = player.objectOwner.isLocalPlayer ? Const.MAX_ORDER : order + 1;
        selectedNamePlate.nameCanvas.sortingOrder = player.objectOwner.isLocalPlayer ? Const.MAX_ORDER : order + 1;
        selectedNamePlate.hpCanvas.sortingOrder = player.objectOwner.isLocalPlayer ? Const.MAX_ORDER : order + 1;
        selectedNamePlate.shieldCanvas.sortingOrder = player.objectOwner.isLocalPlayer ? Const.MAX_ORDER : order + 1;
    }

    // ---------------------------------------------- Spine Animation Event 처리 구간 ---------------------------------------------------//
    
    // Animation Event 총괄 처리
    public virtual void OnAnimationEvent(Spine.TrackEntry trackEntry, Spine.Event e)
    {    

    }

    // Animation Event 시작 시 처리
    public void OnAnimationStart(Spine.TrackEntry trackEntry)
    {

    }

    // Animationm Event 완료 시 처리
    public void OnAnimationComplete(Spine.TrackEntry trackEntry)
    {
        // 플레이어 아바타의 경우 이곳에서 아이들 애니메이션 처리
        if(objectType == ObjectType.PLAYER && !isIronDemonMoving)
        {
            if( trackEntry.Animation.Name != "Eye" && !trackEntry.Animation.Name.Contains("Idle") && trackEntry.TrackIndex == 0)
            {
                anim.state.ClearTrack(0);
                if(player.character == Character.ERIS)
                    anim.state.SetAnimation(0,GetErisMode() + "Idle",true);
                else
                    anim.state.SetAnimation(0,"Idle",true);
            }
        }
    }

    public void OnChangeObjectType(ObjectType oldValue, ObjectType newValue)
    {
        switch(objectType){
            case ObjectType.PLAYER:
                playerNamePlate.SetActive(true);
                monsterNamePlate.SetActive(false);
                break;
            case ObjectType.ENEMY:
                playerNamePlate.SetActive(false);
                monsterNamePlate.SetActive(true);
                break;
            case ObjectType.NPC:
                playerNamePlate.SetActive(false);
                monsterNamePlate.SetActive(true);
                break;
        }
    }

    void OnChangedIronDemonLocation(TargetObject oldVal, TargetObject newVal)
    {
        if(newVal == this)
        { 
            ironDemon.GetComponent<SkeletonAnimation>().state.Complete -= OnIronDemonAnimationComplete;
            ironDemon.GetComponent<SkeletonAnimation>().state.Complete += OnIronDemonAnimationComplete;
        }
    }

    void OnChangedErisMode(ErisMode oldVal, ErisMode newVal)
    {
        if(newVal == ErisMode.MAD)
        {
            StartCoroutine(ErisAdditionalMadAnimation());
        }
    }

    void OnChangedPlayerHP(int oldVal, int newVal)
    {
        if(oldVal > 0){
            M_EffectManager.instance.DisPlayeDamage(this, (oldVal - newVal)); // 데미지 or 회복 표시 이펙트 생성
            if(newVal > 0){
                selectedNamePlate.SetHpValue(newVal, playerMaxHP, this);
                if((oldVal - newVal) > 0){
                    PlayCharaterHitVoice(); // 체력이 떨어지면, 캐릭터 피격음성 재생
                }
            }
        }else if(newVal == 0){ // 체력이 0이면 캐릭터 사망 음성 재생
            if(isServer){
                StartCoroutine(PlayerDeathProcess());
            }
            PlayChararcterDeathVoice();
        }
    }

    void OnChangedDefense(int oldVal, int newVal)
    {
        selectedNamePlate.SetShieldValue(newVal,oldVal == 0, objectType != ObjectType.PLAYER);
        if(newVal > 0){
            int value = newVal - oldVal;
            if(oldVal > newVal){
                M_EffectManager.instance.DisplayDefence(this, false, value);
                AudioClip buffSound = M_SoundManager.instance.GetSFXClip(SFX_TYPE.Common, "common_shield_down");
                M_SoundManager.instance.PlaySFX(buffSound, buffSound.length);
            }else{
                M_EffectManager.instance.DisplayDefence(this, true, value);
                AudioClip buffSound = M_SoundManager.instance.GetSFXClip(SFX_TYPE.Common, "common_shield_up");
                M_SoundManager.instance.PlaySFX(buffSound, buffSound.length);
            }
        }else{
            if(objectType != ObjectType.PLAYER && !M_TurnManager.instance.monsterShieldInitialize && isServer){
                monster.OnBreakedShield();
            }
        }
    }
}
