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
    public GameObject playerNamePlate;
    public GameObject monsterNamePlate;
    public NamePlate selectedNamePlate;
    public TextMeshProUGUI targetObjectName;

    public BuffIndicator buffIndicator;
    public NextActionIndicator nextActionIndicator;

    [Header("타겟 오브젝트 타입")]
    [SyncVar]
    public ObjectType objectType;

    // Player 의 경우 
    [SyncVar (hook = nameof(InitTargetObjectPlayer))]
    public GamePlayer player;
    public SkeletonDataAsset[] ironDemonData = new SkeletonDataAsset[2];

    [SyncVar (hook = nameof(OnChangedPlayerHP))]
    public int playerHP;
    
    [SyncVar]
    public int playerMaxHP;

    public NetworkIdentity conn;

    // Monster 의 경우
    [SyncVar]
    public SpawnedMonster monster;

    public List<GameObject> characters;
    public List<GameObject> monsters;

    public TargetObject clone;
    public TargetObject origin;
    
    [SyncVar]
    public bool isCloneData = false;
    public GameObject avatar;

    // 철귀
    public GameObject ironDemon;
    [SyncVar (hook = nameof(OnChangedIronDemonLocation))]
    public TargetObject ironDemonLocation;

    [SyncVar]
    public int sizeOfIronDemon;

    public SkeletonAnimation anim;

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

    void Start()
    {
        buffs.Callback += OnChangedBuff;
        StartCoroutine(FindChildObjects());
    }

    IEnumerator FindChildObjects()
    {
        while(true)
        {
            yield return new WaitForSeconds(0.01f);
            if(avatar == null && monster == null)
                continue;

            if(objectType == ObjectType.PLAYER)
            {
                anim = avatar.GetComponent<SkeletonAnimation>();
            }
            else
            {
                anim = monster.GetComponent<SkeletonAnimation>();
            }
            if(anim != null){
                anim.state.Event += OnAnimationEvent;
                anim.state.Start += OnAnimationStart;
                anim.state.Complete += OnAnimationComplete;
                anim.timeScale = Random.Range(0.9f,1.1f); // 칼군무 방지 코드
                if(objectType == ObjectType.PLAYER)
                    if(player.character == Character.HONGDANHYANG){
                        StartCoroutine(HongDanHyangEyeFlicker());
                        if(!isCloneData)ironDemon.GetComponent<SkeletonAnimation>().state.Complete += OnIronDemonAnimationComplete;
                    }
                break;
            }

        }
    }

    public void InitMonsterNamePlate()
    {
        selectedNamePlate = monsterNamePlate.GetComponent<NamePlate>();
        playerNamePlate.SetActive(false);
    }

    IEnumerator HongDanHyangEyeFlicker()
    {
        while(true)
        {
            yield return new WaitForSeconds(Random.Range(2f,5f));
            anim.state.SetAnimation(1,"Eye",false);
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
        if(ironDemon != null)
        {
            ironDemon.GetComponent<SkeletonAnimation>().state.Complete -= OnIronDemonAnimationComplete;
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
                    if(NetworkClient.connection.identity.GetComponent<GamePlayer>() == player)
                    {
                        ironDemon = Instantiate(characters.Find(x => x.name == "IronDemon"),transform.position,Quaternion.identity,transform);
                        ironDemon.GetComponent<MeshRenderer>().sortingOrder = 1;
                    }
                    else
                        ironDemon = Instantiate(characters.Find(x => x.name == "IronDemonTransparent"),transform.position,Quaternion.identity,transform);
                    ironDemonLocation = this;
                    ironDemon.GetComponent<SkeletonAnimation>().timeScale = Random.Range(0.9f,1.1f);
                    
                break;
            }
            selectedNamePlate = playerNamePlate.GetComponent<NamePlate>();
            targetObjectName.text = player.steamPersonaName;
            monsterNamePlate.SetActive(false);
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
            if(isServer && monster.HP <= remind){
                M_TurnManager.instance.monsterDeathOperating = true;
                M_TurnManager.instance.ProcessMonsterDeath(this);
            }
            monster.HP -= remind;
            monster.OnChangedHpValue(monster.HP,monster.HP);
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
        {
            if((newBuff.type == BuffType.ICHI_ATTACK || newBuff.type == BuffType.ICHI_DEFENSE) && objectType == ObjectType.PLAYER)
                foreach(CardOnHand cardOnHand in player.GetComponent<GamePlayerDeck>().cardOnHands)
                    cardOnHand.CardInfoChangedEvent.Invoke();
            switch (op)
            {
                case SyncList<Buff>.Operation.OP_ADD:
                    buffIndicator.SetBuff(newBuff);
                    Debug.Log("BUFF ADD");
                    break;
                case SyncList<Buff>.Operation.OP_INSERT:
                    buffIndicator.SetBuff(newBuff);
                    Debug.Log("BUFF Insert");
                    break;
                case SyncList<Buff>.Operation.OP_REMOVEAT:

                    break;
                case SyncList<Buff>.Operation.OP_SET:
                    buffIndicator.SetBuff(newBuff);
                    Debug.Log("BUFF set");
                    break;
                case SyncList<Buff>.Operation.OP_CLEAR:

                    break;
            }
        }
    }

    void OnChangedPlayerHP(int oldVal, int newVal)
    {
        if(player != null)
        {
            if(player.netIdentity == NetworkClient.connection.identity)
            {
                player.HP = newVal;
            }
            selectedNamePlate.SetHPValue(playerHP,playerMaxHP);
        }
    }

    void OnChangedDefense(int oldVal, int newVal)
    {
       selectedNamePlate.SetShieldValue(newVal,oldVal == 0,objectType != ObjectType.PLAYER);
    }

    [ClientRpc]
    public void SetIronDemonParent(Transform p)
    {
        ironDemon.transform.parent = p;
    }

    // ---------------------------------------------- Spine Animation Event 처리 구간 ---------------------------------------------------//
    
    // Animation Event 총괄 처리
    public virtual void OnAnimationEvent(Spine.TrackEntry trackEntry, Spine.Event e)
    {    

    }

    // Animation Event 시작 시 처리
    public void OnAnimationStart(Spine.TrackEntry trackEntry)
    {
        // TODO : Animation Event 시작 시점 처리
    }

    // Animationm Event 완료 시 처리
    public void OnAnimationComplete(Spine.TrackEntry trackEntry)
    {
        // 플레이어 아바타의 경우 이곳에서 아이들 애니메이션 처리
        if(objectType == ObjectType.PLAYER)
        {
            if(trackEntry.Animation.Name != "Idle" && trackEntry.Animation.Name != "Eye")
            {
                anim.state.ClearTrack(0);
                anim.state.SetAnimation(0,"Idle",true);
            }
        }
    }

    public void OnIronDemonAnimationComplete(Spine.TrackEntry trackEntry)
    {
        if(trackEntry.Animation.Name == "Defense")
            ironDemon.GetComponent<SkeletonAnimation>().state.SetAnimation(0,"Idle",true);
    }

    void OnChangedIronDemonLocation(TargetObject oldVal, TargetObject newVal)
    {
        if(newVal == this)
        {
            Debug.Log(" 철귀 애니메이션 복귀 " );       
            ironDemon.GetComponent<SkeletonAnimation>().state.Complete += OnIronDemonAnimationComplete;
        }
    }


}
