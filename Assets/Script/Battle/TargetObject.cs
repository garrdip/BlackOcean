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

    [Header("쉴드 표시")]
    public TextMeshProUGUI shieldText;

    // Player 의 경우 
    [SyncVar (hook = nameof(InitTargetObjectPlayer))]
    public GamePlayer player;

    [SyncVar (hook = nameof(OnChangedPlayerHP))]
    public int playerHP;
    
    [SyncVar]
    public int playerMaxHP;

    public NetworkIdentity conn;

    // Monster 의 경우
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
    [SyncVar(hook = nameof(OnChangedDefense))]
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
        buffs.Callback += OnChangedBuff;
        anim = GetComponentInChildren<SkeletonAnimation>();
        if(anim != null){
            anim.state.Event += OnAnimationEvent;
            anim.state.Start += OnAnimationStart;
            anim.state.Complete += OnAnimationComplete;
        }
    }

    void Start()
    {
        StartCoroutine(FindChildObjects());
    }

    IEnumerator FindChildObjects()
    {
        while(true)
        {
            anim = GetComponentInChildren<SkeletonAnimation>();
            if(anim != null){
                anim.state.Event += OnAnimationEvent;
                anim.state.Start += OnAnimationStart;
                anim.state.Complete += OnAnimationComplete;
                anim.timeScale = Random.Range(0.9f,1.1f); // 칼군무 방지 코드
                break;
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    void OnDestroy()
    {
        // 오브젝트 제거 시 델리게이트 이벤트 리스너 해제
        if(anim != null){
            anim.state.Event -= OnAnimationEvent;
            anim.state.Start -= OnAnimationStart;
            anim.state.Complete -= OnAnimationComplete;
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


    public void InitTargetObjectPlayer(GamePlayer oldVal, GamePlayer newVal)
    {
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


    // ----------------------------------------------           Damage 관련 함수        ---------------------------------------------------//
    public void DamageToPlayer(int damage)
    {
        if(GetBuffValue(BuffType.BOONGGUI) > 0)
        {
            damage = (int)(damage * 1.5);
        }
        // 개화꽃 적용
        damage -= GetBuffValue(BuffType.FLOWER);
        if(damage <= 0) return;
        // 방어력 적용
        if(defense >= damage)
        {
            defense -= damage;
        }
        else
        {
            int remind = damage - defense;
            defense = 0;
            playerHP -= remind;
        }
    }

    public void DamageToMonster(int damage)
    {
        // 붕괴 적용
        if(GetBuffValue(BuffType.BOONGGUI) > 0)
        {
            damage = (int)(damage * 1.5);
        }
        // 개화꽃 적용
        damage += GetBuffValue(BuffType.FLOWER);
        // 방어력 적용
        if(defense >= damage)
        {
            defense -= damage;
        }
        else
        {
            int remind = damage - defense;
            defense = 0;
            monster.HP -= remind;
        }
    }

    // ----------------------------------------------           Buff 관련 함수          ---------------------------------------------------//
    public void GainBuff(BuffType buffType, int value, bool isDebuff, bool isInfinity, bool isDecrease, TargetObject tar)
    {
        if(buffs.Find(buff => buff.type == buffType) == null) // 버프 신규 등록
        {
            Buff newBuff = new Buff(buffType,value,isDebuff,isInfinity,isDecrease,tar);
            buffs.Add(newBuff);
        }
        else // 버프가 있을경우 중첩 상승
        {
            Buff oldItem = buffs.Find(buff => buff.type == buffType);
            int indexOfOldItem = buffs.FindIndex(buff => buff.type == buffType);
            oldItem.value += value;
            buffs.RemoveAt(indexOfOldItem);
            buffs.Insert(indexOfOldItem,oldItem);
        }
    }

    public int GetBuffValue(BuffType buffType)
    {
        if(buffs.Find(buff => buff.type == buffType) == null) return 0;
        else return buffs.Find(buff => buff.type == buffType).value;
    }

    public void GainDefense(int value)
    {
        defense += value;
    }

    // ----------------------------------------------  SyncVar, SyncList 콜백 처리 구간 ---------------------------------------------------//

    public void OnChangedBuff(SyncList<Buff>.Operation op, int index, Buff oldBuff, Buff newBuff)
    {
        if(newBuff != null)
            if((newBuff.type == BuffType.ICHI_ATTACK || newBuff.type == BuffType.ICHI_DEFENSE) && objectType == ObjectType.PLAYER)
                foreach(CardOnHand cardOnHand in player.GetComponent<GamePlayerDeck>().cardOnHands)
                    cardOnHand.CardInfoChangedEvent.Invoke();
    }

    void OnChangedPlayerHP(int oldVal, int newVal)
    {
        if(player != null)
        {
            if(player.netIdentity == NetworkClient.connection.identity)
            {
                player.HP = newVal;
            }
            hpbar.value = newVal; // HP 슬라이더값 업데이트
        }
    }

    void OnChangedDefense(int oldVal, int newVal)
    {
        shieldText.text = newVal.ToString();
    }

    // ---------------------------------------------- Spine Animation Event 처리 구간 ---------------------------------------------------//
    
    // Animation Event 총괄 처리
    public virtual void OnAnimationEvent(Spine.TrackEntry trackEntry, Spine.Event e)
    {    
        if(e.Data.Name == "AttackEnd") // 공격모션 종료시 
        {
            isAnimating = false; // 공격 애니메이팅 종료를 알림
        }
    }

    // Animation Event 시작 시 처리
    public void OnAnimationStart(Spine.TrackEntry trackEntry)
    {
        // TODO : Animation Event 시작 시점 처리
    }

    // Animationm Event 완료 시 처리
    public void OnAnimationComplete(Spine.TrackEntry trackEntry)
    {
        // TODO : Animation Event 종료 시점 처리
    }

}
