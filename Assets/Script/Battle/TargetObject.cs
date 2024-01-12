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

public class TargetObject : NetworkBehaviour
{
    public GameObject playerNamePlate;
    public GameObject monsterNamePlate;
    public NamePlate selectedNamePlate;
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI monsterName;
    public Canvas playerMessageCavnas;
    public SortingGroup sortingGroup;
    public TextMeshProUGUI playerMessageBubble;
    public BuffIndicatorController buffIndicator;
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
    
    public GameObject avatar;

    // 철귀
    public GameObject ironDemon;
    [SyncVar (hook = nameof(OnChangedIronDemonLocation))]
    public TargetObject ironDemonLocation;

    [SyncVar (hook = nameof(OnChangedIronDemonSize))]
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

    void Start()
    {
        buffs.Callback += OnChangedBuff;
        if(player != null){
            player.onChangePlayerOrder += OnChangePlayerOrder; // 플레이어 타겟오브젝트인 경우 오더 변경 델리게이트 이벤트 리스너 추가
        }
        StartCoroutine(FindChildObjects());
        StartCoroutine(InitNamePlate());
    }

    public void OnChangePlayerOrder(int order)
    {
        transform.DOMove(M_TurnManager.instance.targetObjectPosition[order], 0.5f); // 플레이어 오더 변경 이벤트 수신하여 타겟오브젝트 위치 이동
    }

    IEnumerator InitNamePlate()
    {
        while(true)
        {
            if(playerHP > 0 && playerMaxHP >0)
            {
                selectedNamePlate.SetHPValue(playerHP,playerMaxHP,10);
                break;
            }
            yield return new WaitForSeconds(0.01f);
        }
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
                        ironDemon.GetComponent<SkeletonAnimation>().state.Complete += OnIronDemonAnimationComplete;
                    }
                break;
            }

        }
    }

    public void InitMonsterNamePlate()
    {
        selectedNamePlate = monsterNamePlate.GetComponent<NamePlate>();
        selectedNamePlate.SetHPValue(monster.HP,monster.MAXHP,(int)transform.position.x);
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
        transform.DOKill();
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
                        ironDemon.GetComponent<MeshRenderer>().sortingOrder = 0;
                    }
                    else
                        ironDemon = Instantiate(characters.Find(x => x.name == "IronDemonTransparent"),transform.position,Quaternion.identity,transform);
                    ironDemonLocation = this;
                    ironDemon.GetComponent<SkeletonAnimation>().timeScale = Random.Range(0.9f,1.1f);
                    
                break;
            }
            selectedNamePlate = playerNamePlate.GetComponent<NamePlate>();
            playerName.text = player.objectOwner.steamPersonaName;
            monsterNamePlate.SetActive(false);
            if(newVal.objectOwner.isLocalPlayer){
                sortingGroup.sortingOrder = 2;
                // 캔버스는 스프라이트 정렬그룹에 영향을 받지 않아 직접 설정
                selectedNamePlate.nameCanvas.sortingOrder = sortingGroup.sortingOrder + 1;
                selectedNamePlate.hpCanvas.sortingOrder = sortingGroup.sortingOrder + 1;
                selectedNamePlate.shieldCanvas.sortingOrder = sortingGroup.sortingOrder + 1;
            }
        }
    }

    // 남은 코스트 없음 표시하는 말풍선 페이드인 후 페이드아웃
    public void ShowCostNotReaminBubble(GamePlayer gamePlayer)
    {
        // 캐릭터 별로 메시지 버블 텍스트 분기처리
        switch(gamePlayer.character){
            case Character.GEORK:
                playerMessageBubble.text = Const.COST_NOT_REMAIN_TEXT_GEORK;
                break;
            case Character.ERIS:
                playerMessageBubble.text = Const.COST_NOT_REMAIN_TEXT_ERIS;
                break;
            case Character.HONGDANHYANG:
                playerMessageBubble.text = Const.COST_NOT_REMAIN_TEXT_HONGDANHYANG;
                break;
        }
        // 페이드인 1초 후 페이드아웃 1초 
        CanvasGroup canvasGroup = playerMessageCavnas.GetComponent<CanvasGroup>();
        if(DOTween.IsTweening(canvasGroup)){
            DOTween.Kill(canvasGroup);
        }
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.DOFade(1.0f, 1f).OnComplete(() => {
            canvasGroup.DOFade(0.0f, 1f).OnComplete(() => {
                canvasGroup.gameObject.SetActive(false);
            }); 
        }); 

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
            if(playerHP <= 0){
                if(player.character == Character.ERIS && erisMode != ErisMode.MAD)
                {
                    playerHP = 1;
                    StartCoroutine(ErisTransform());
                }
                else
                    playerHP = 0;
            }
            player.HP = playerHP;
        }
    }

    IEnumerator ErisTransform()
    {
        M_TurnManager.instance.StartAnimation(this,0,"Change1",false);
        erisMode = ErisMode.MAD;
        yield return new WaitForSeconds(2f);
        M_TurnManager.instance.StartAnimation(this,0,"VIdle",true);
    }

    public void DamageToMonster(int damage, TargetObject from)
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
            if(isServer && monster.HP <= remind ){
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
                    break;
                case SyncList<Buff>.Operation.OP_INSERT:
                    buffIndicator.SetBuff(newBuff);
                    break;
                case SyncList<Buff>.Operation.OP_REMOVEAT:

                    break;
                case SyncList<Buff>.Operation.OP_SET:
                    buffIndicator.SetBuff(newBuff);
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
        }
        if(playerMaxHP != 0)
            selectedNamePlate.SetHPValue(playerHP,playerMaxHP,(int)transform.position.x);
        
        if(isServer && playerHP == 0)
        {
            foreach(CardOnHand cardOnHand in player.GetComponent<GamePlayerDeck>().cardOnHands)
                NetworkServer.Destroy(cardOnHand.gameObject);
            player.GetComponent<GamePlayerDeck>().cardOnHands.Clear();
            M_TurnManager.instance.spawnedPlayerList.Remove(this);
            NetworkServer.Destroy(this.gameObject);
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

    public void OnChangedIronDemonSize(int oldVal, int newVal)
    {
        Buff ironDemonBuff = new Buff(BuffType.IRONDEMON,newVal,false,false,false,this);
        buffIndicator.SetBuff(ironDemonBuff);
    }

    void OnChangedErisMode(ErisMode oldVal, ErisMode newVal)
    {
        if(newVal == ErisMode.MAD)
        {
            StartCoroutine(ErisAdditionalMadAnimation());
        }
    }

    IEnumerator ErisAdditionalMadAnimation()
    {
        WaitForSeconds loopTime = new WaitForSeconds(0.1f);
        float haedTimer = Random.Range(1f,2f);
        float lbTimer = Random.Range(1f,2f);
        float ltTimer = Random.Range(1f,2f);
        float rTimer = Random.Range(1f,2f);
        Spine.TrackEntry track = null;
        while(erisMode == ErisMode.MAD)
        {
            if(haedTimer <= 0f)
            {
                haedTimer = Random.Range(1f,2f);
                track =  anim.state.SetAnimation(1,"VAniHead",false);
                track.MixBlend = Spine.MixBlend.Add;
                track.Alpha = 1f;
            }
            if(lbTimer <= 0f)
            {

                lbTimer = Random.Range(1f,2f);
                if(Random.Range(0,2) == 0)
                    track =  anim.state.SetAnimation(1,"VAniLBArm0",false);
                else
                    track =  anim.state.SetAnimation(1,"VAniLBArm1",false);
                track.MixBlend = Spine.MixBlend.Add;
                track.Alpha = 1f;

            }
            if(ltTimer <= 0f)
            {

                ltTimer = Random.Range(1f,2f);
                if(Random.Range(0,2) == 0)
                    track =  anim.state.SetAnimation(1,"VAniLTArm0",false);
                else
                    track =  anim.state.SetAnimation(1,"VAniLTArm1",false);
                track.MixBlend = Spine.MixBlend.Add;
                track.Alpha = 1f;
            }
            if(rTimer <= 0f)
            {
                rTimer = Random.Range(1f,2f);
                if(Random.Range(0,2) == 0)
                    track =  anim.state.SetAnimation(1,"VAniRArm0",false);
                else
                    track =  anim.state.SetAnimation(1,"VAniRArm1",false);
                track.MixBlend = Spine.MixBlend.Add;
                track.Alpha = 1f;
            }
            haedTimer -= 0.1f;
            lbTimer -= 0.1f;
            ltTimer -= 0.1f;
            rTimer -= 0.1f;
            yield return loopTime;
        }
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

    public void OnIronDemonAnimationComplete(Spine.TrackEntry trackEntry)
    {
        if(trackEntry.Animation.Name == "Defense")
            ironDemon.GetComponent<SkeletonAnimation>().state.SetAnimation(0,"Idle",true);
    }

    public void ApllyIronDemonAnimationCallbackFunction()
    {
        OnChangedIronDemonLocation(this,this);
    }

    void OnChangedIronDemonLocation(TargetObject oldVal, TargetObject newVal)
    {
        if(newVal == this)
        { 
            ironDemon.GetComponent<SkeletonAnimation>().state.Complete -= OnIronDemonAnimationComplete;
            ironDemon.GetComponent<SkeletonAnimation>().state.Complete += OnIronDemonAnimationComplete;
        }
    }
}
