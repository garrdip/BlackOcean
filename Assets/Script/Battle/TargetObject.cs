using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;
using Steamworks;
using TMPro;
using Spine.Unity;

public class TargetObject : NetworkBehaviour
{
    [Header("HP 슬라이더")]
    public Slider hpbar;

    [Header("타겟 이름")]
    public TextMeshProUGUI textTargetName;

    [Header("삼각형 화살표")]
    public GameObject currentPlayerMark;

    [Header("Cost 아이콘")]
    public GameObject currentPlayerTargetCosts;

    [Header("바닥 오오라")]
    public GameObject currentPlayerGroundIndicator;

    [Header("타겟 오브젝트 타입")]
    [SyncVar]
    public ObjectType objectType;

    // Player 의 경우 
    [SyncVar (hook = nameof(InitTargetObjectPlayer))]
    public GamePlayer player;
    public CloneGamePlayer cloneGamePlayer;

    public NetworkIdentity conn;

    // Monster 의 경우
    [SyncVar (hook = nameof(InitTargetObjectEmemy))]
    public SpawnedMonster monster;

    public List<GameObject> characters;
    public List<GameObject> monsters;

    public TargetObject clone;
    
    [SyncVar]
    public bool isCloneData = false;
    public GameObject avatar;

    public SkeletonAnimation anim;

    public bool isAnimating = false;

    public readonly SyncList<Buff> buffs = new SyncList<Buff>();


// 전투용 변수들
    [SyncVar]
    public int defense = 0;
// 플레이어용 변수
    [SyncVar]
    public int currentIchi = 3;
    [SyncVar]
    public int maxIchi = 3;
    [SyncVar]
    public int limitiChi = 6;
    [SyncVar]
    public bool isTransformed = false;

    void Awake()
    {
        StartCoroutine(FindSkeletonAnimation());
        buffs.Callback += OnChangedBuff;
    }

    public void OnChangedBuff(SyncList<Buff>.Operation op, int index, Buff oldBuff, Buff newBuff)
    {
        //Buff 추가 제거시 UI 효과 여기서 해야할듯
    }
    // 
    IEnumerator FindSkeletonAnimation()
    {
        WaitForSeconds loopTime = new WaitForSeconds(0.01f);
        while(true)
        {
            if(GetComponentInChildren<SkeletonAnimation>() != null)
            {
                anim = GetComponentInChildren<SkeletonAnimation>();
                anim.state.Event += OnAnimationEvent;
                break;
            }
            yield return loopTime;
        }
    }

    // Animation Event 처리 구간
    public void OnAnimationEvent(Spine.TrackEntry trackEntry, Spine.Event e)
    {    
        if(e.Data.Name == "AttackEnd") // 공격모션 종료시 
        {
            anim.state.SetAnimation(1,"00Normal",true); // IDLE 애니메이션 재구동
            isAnimating = false; // 공격 애니메이팅 종료를 알림
        }
    }
    public void InitTargetObjectPlayer(GamePlayer oldVal, GamePlayer newVal)
    {
        Debug.Log("Init Player!");
        if(objectType == ObjectType.PLAYER)
        {
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
                break;
            }
            textTargetName.text = SteamFriends.GetFriendPersonaName((CSteamID)newVal.steamID);
            hpbar.maxValue = newVal.MaxHP;
            hpbar.value = newVal.HP;
            if(newVal.isLocalPlayer){
                currentPlayerMark.SetActive(true);
                currentPlayerTargetCosts.SetActive(true);
                currentPlayerGroundIndicator.SetActive(true);
                float hpbarWidth = hpbar.GetComponent<RectTransform>().rect.width;
                float hpbarHeight = hpbar.GetComponent<RectTransform>().rect.height;
                hpbar.GetComponent<RectTransform>().sizeDelta = new Vector2(hpbarWidth + 300f, hpbarHeight + 100f);
            }
        }
    }

    public void InitTargetObjectEmemy(SpawnedMonster oldVal, SpawnedMonster newVal)
    {
        StartCoroutine(nameof(EmemyTargetObjectGenerator));
    }

    IEnumerator EmemyTargetObjectGenerator()
    {
        WaitForSeconds loopSecond = new WaitForSeconds(0.01f);
        while(true)
        {
            if(objectType == ObjectType.ENEMY && monster.monsterData != null)
            {
                switch(monster.monsterData.name)
                {
                    case "Monster_Goblin" :
                        avatar = Instantiate(monsters.Find(prefab => prefab.name == "Goblin"),transform.position,Quaternion.identity,transform);
                    break;
                    case "Monster_Troll" :
                        avatar = Instantiate(monsters.Find(prefab => prefab.name == "Troll"),transform.position,Quaternion.identity,transform);
                    break;
                }
                break;
            }
            yield return loopSecond;
        }
    }

    void FixedUpdate()
    {
        if(isServer)
        {
            if(objectType == ObjectType.PLAYER) 
            {
                // 플레이어 사망시 처리
            }
            else
            {
                if(monster == null)
                {
                    if(isCloneData)
                        M_TurnManager.instance.cloneMonsterList.Remove(this);
                    else
                        M_TurnManager.instance.spawnedMonsterList.Remove(this);
                        
                    NetworkServer.Destroy(this.gameObject);
                    if(!isCloneData)M_TurnManager.instance.OnChangedMonsterList(); 
                }
            }
        }
    }

}
