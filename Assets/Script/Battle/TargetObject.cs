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

public class TargetObject : NetworkBehaviour
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
    public GamePlayer player; // Player 의 경우

    [SyncVar (hook = nameof(OnChangedPlayerHP))]
    public int playerHP;
    
    [SyncVar]
    public int playerMaxHP;

    [SyncVar (hook = nameof(OnChangedIronDemonLocation))]
    public TargetObject ironDemonLocation;

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

    [SyncVar]
    public int currentApDoRequirement = 8;

    [SyncVar]
    public bool isDying = false;

    // -------------------------------   몬스터용 Syncvar 변수들   ------------------------------------//
    
    [SyncVar]
    public SpawnedMonster monster; // Monster 의 경우
    
    // -------------------------------   전투용 Syncvar 변수들   ------------------------------------//

    [SyncVar(hook = nameof(OnChangedDefense))]
    public int defense = 0;

    public readonly SyncList<Buff> buffs = new SyncList<Buff>();


    void Start()
    {
        buffs.Callback += OnChangedBuff;
        StartCoroutine(FindChildObjects());
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if(objectType == ObjectType.PLAYER){
            player.onChangePlayerOrder += OnChangePlayerOrder; // 플레이어 타겟오브젝트인 경우 오더 변경 델리게이트 이벤트 리스너 추가
            InitTargetObjectPlayer(player);
        }else{
            InitTargetObjectMonster(monster);
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
        if(player != null){
            player.onChangePlayerOrder -= OnChangePlayerOrder;
        }
    }

    // 플레이어 타입의 타겟오브젝트 초기화
    public void InitTargetObjectPlayer(GamePlayer player)
    {
        selectedNamePlate = playerNamePlate.GetComponent<NamePlate>();
        selectedNamePlate.SetHPValue(playerHP, playerMaxHP, 10);
        monsterNamePlate.SetActive(false);
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
                avatar.GetComponent<MeshRenderer>().sortingOrder = 1;
                ironDemon = Instantiate(characters.Find(x => x.name == "IronDemon"),transform.position,Quaternion.identity,transform);
                if(NetworkClient.localPlayer.transform.GetChild(0).GetComponent<GamePlayer>() == player){
                    ironDemon.GetComponent<SkeletonRenderTexture>().color.a = 1f;
                }else{
                    ironDemon.GetComponent<SkeletonRenderTexture>().color.a = 0.5f;
                }
                ironDemonLocation = this;
                ironDemon.GetComponent<SkeletonAnimation>().timeScale = Random.Range(0.9f,1.1f);
                break;
        }
        playerName.text = player.objectOwner.steamPersonaName;
        if(player.objectOwner.isLocalPlayer){
            avatar.GetComponent<MeshRenderer>().sortingOrder = 1;
            targetObjectUI.GetComponent<SortingGroup>().sortingOrder = avatar.GetComponent<MeshRenderer>().sortingOrder + 1;
            selectedNamePlate.nameCanvas.sortingOrder = avatar.GetComponent<MeshRenderer>().sortingOrder + 1;
            selectedNamePlate.hpCanvas.sortingOrder = avatar.GetComponent<MeshRenderer>().sortingOrder + 1;
            selectedNamePlate.shieldCanvas.sortingOrder = avatar.GetComponent<MeshRenderer>().sortingOrder + 1;
        }
    }

    // 몬스터 타입의 타겟오브젝트 초기화
    public void InitTargetObjectMonster(SpawnedMonster monster)
    {
        selectedNamePlate = monsterNamePlate.GetComponent<NamePlate>();
        selectedNamePlate.SetHPValue(monster.HP, monster.MAXHP, (int)transform.position.x);
        monsterName.text = monster.monsterName;
        playerNamePlate.SetActive(false);
    }

    public void OnChangePlayerOrder(int order)
    {
        transform.DOMove(M_TurnManager.instance.targetObjectPosition[order], 0.5f); // 플레이어 오더 변경 이벤트 수신하여 타겟오브젝트 위치 이동
    }

    // 남은 코스트 없음 표시하는 말풍선 페이드인 후 페이드아웃
    public void ShowCostNotReaminBubble(GamePlayer gamePlayer)
    {
        Dictionary<string, string> constDict = new Dictionary<string, string>();
        constDict.Add("georg_78", Const.Georg_78);
        constDict.Add("georg_79", Const.Georg_79);
        constDict.Add("georg_80", Const.Georg_80);
        constDict.Add("Eris_116", Const.Eris_116);
        constDict.Add("Eris_117", Const.Eris_117);
        constDict.Add("Eris_118", Const.Eris_118);
        constDict.Add("Eris_119", Const.Eris_119);
        constDict.Add("Hong_66", Const.Hong_66);
        constDict.Add("Hong_67", Const.Hong_67);
        constDict.Add("Hong_68", Const.Hong_68);

        // 캐릭터 별 음성 클립 재생
        switch (gamePlayer.character){
            case Character.GEORK:
                PlayCharacterRequireCostVoice(Character.GEORK, 77, 3, constDict);
                break;
            case Character.ERIS:
                PlayCharacterRequireCostVoice(Character.ERIS, 115, 4, constDict);
                break;
            case Character.HONGDANHYANG:
                PlayCharacterRequireCostVoice(Character.HONGDANHYANG, 65, 3, constDict);
                break;
        }
        // 페이드인 1초 후 페이드아웃 1초 
        CanvasGroup canvasGroup = playerMessageCavnas.GetComponent<CanvasGroup>();
        if(canvasGroup != null){
            if(DOTween.IsTweening(canvasGroup)){
                canvasGroup.DOKill();
            }
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.DOFade(1.0f, 1f).OnComplete(() => {
                canvasGroup.DOFade(0.0f, 1f).OnComplete(() => {
                    canvasGroup.gameObject.SetActive(false);
                }); 
            }); 
        }
    }

    // 캐릭터별 음성 생성 및 팝업창 텍스트 세팅
    private void PlayCharacterRequireCostVoice(Character character, int startClipIndex, int numberOfClips, Dictionary<string, string> constDict)
    {
        List<AudioClip> clips = new List<AudioClip>();
        switch(character){
            case Character.HONGDANHYANG:
                List<AudioClip> danhyangVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.HongDanHyang, startClipIndex, numberOfClips);
                foreach(AudioClip audioClip in danhyangVoices){
                    clips.Add(audioClip);
                }
                break;
            case Character.GEORK:
                List<AudioClip> georkVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Geork, startClipIndex, numberOfClips);
                foreach(AudioClip audioClip in georkVoices){
                    clips.Add(audioClip);
                }
                break;
            case Character.ERIS:
                List<AudioClip> erisVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Eris, startClipIndex, numberOfClips);
                foreach(AudioClip audioClip in erisVoices){
                    clips.Add(audioClip);
                }
            break;
        }
        if(clips.Count > 0){
            int randomIndex = Random.Range(0, clips.Count);
            AudioClip clipToPlay = clips[randomIndex];

            if(constDict.TryGetValue(clipToPlay.name, out string message)){
                playerMessageBubble.text = message;
            }
            M_SoundManager.instance.StopAllVoice();
            M_SoundManager.instance.PlayVoice(clipToPlay, clipToPlay.length);
        }
    }

    // 캐릭터별 피격 음성 재생
    private void PlayCharaterHitVoice()
    {
        AudioClip hitVoice = null;
        switch(player.character){
            case Character.HONGDANHYANG:
                List<AudioClip> danhyangVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.HongDanHyang, 58, 4);
                hitVoice = danhyangVoices[Random.Range(0, danhyangVoices.Count)];
                break;
            case Character.GEORK:
                List<AudioClip> georkVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Geork, 65, 9);
                hitVoice = georkVoices[Random.Range(0, georkVoices.Count)];
                break;
            case Character.ERIS:
                List<AudioClip> erisVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Eris, 99, 6);
                hitVoice = erisVoices[Random.Range(0, erisVoices.Count)];
                break;
        }
        M_SoundManager.instance.PlayVoice(hitVoice, hitVoice.length);
    }

    // 캐릭터별 사망 음성 재생
    private void PlayChararcterDeathVoice()
    {
        AudioClip playerDeathVoice = null;
        switch(player.character){
            case Character.HONGDANHYANG:
                List<AudioClip> danhyangVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.HongDanHyang, 62, 3);
                playerDeathVoice = danhyangVoices[Random.Range(0, danhyangVoices.Count)];
                break;
            case Character.GEORK:
                List<AudioClip> georkVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Geork, 74, 3);
                playerDeathVoice = georkVoices[Random.Range(0, georkVoices.Count)];
                break;
            case Character.ERIS:
                List<AudioClip> erisVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Eris, 112, 3);
                playerDeathVoice = erisVoices[Random.Range(0, erisVoices.Count)];
                break;
        }
        M_SoundManager.instance.PlayVoice(playerDeathVoice, playerDeathVoice.length);
    }

    // ----------------------------------------------       게오르크 고행 관련 함수      ---------------------------------------------------//

    public void UsingGoHeng()
    {
        DrawGoHengCard();
    }

    [Command(requiresAuthority=false)]
    private void DrawGoHengCard()
    {
        if(M_TurnManager.instance.phase != BattleTurn.PLAYER_ACTIVE)return;
        if(usingGOHENG || usedGOHENG.Count == 3)return;
        usingGOHENG = true;
        int selectedGoheng = 0;
        while(true)
        {
            selectedGoheng = Random.Range(0,3);
            if(!usedGOHENG.Exists(x => x == selectedGoheng))break;
        }
        usedGOHENG.Add(selectedGoheng);
        string nameOfGOHENGCard = "G" + selectedGoheng.ToString();
        if(buffs.FindIndex(buff => buff.type == BuffType.BRILLIANTCURSE) == -1)
            player.GetComponent<GamePlayerDeck>().GenerateCardOnHand(new Card(CardData.instance.cards.Find(card => card.cardNumber == nameOfGOHENGCard)),1);
        else
            player.GetComponent<GamePlayerDeck>().GenerateCardOnHand(new Card(CardData.instance.cards.Find(card => card.cardNumber == nameOfGOHENGCard + "_E")),1);
        if(selectedGoheng == 2)GainBuff(BuffType.GOHANG3_DEBUFF,0,true,true,false,false,this,null);
        if(selectedGoheng == 1)GainBuff(BuffType.GOHANG2_DEBUFF,0,true,true,false,false,this,null);
        foreach(CardOnHand cardOnHand in player.GetComponent<GamePlayerDeck>().cardOnHands)
            cardOnHand.OnChangeCardData(cardOnHand.card,cardOnHand.card);
    }

    // ----------------------------------------------           Damage 관련 함수        ---------------------------------------------------//
    public void DamageToPlayer(int damage)
    {
        if(GetBuffValue(BuffType.BOONGGUI, null) > 0)
        {
            damage = (int)(damage * 1.5);
        }
        // 개화꽃 적용
        foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
            damage -= GetBuffValue(BuffType.FLOWER,target);
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

    public void StaticDamageToPlayer(int damage)
    {
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

    public void DamageToMonster(int damage, TargetObject from)
    {
        // 붕괴 적용
        if(GetBuffValue(BuffType.BOONGGUI,null) > 0)
        {
            damage = (int)(damage * 1.5);
        }
        // 개화꽃 적용
        foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
            damage += GetBuffValue(BuffType.FLOWER,target);
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
                isDying = true;
                RpcMonsterDissolve();
            }
            monster.HP -= remind;
        }
    }

    public void StaticDamageToMonster(int damage)
    {
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
                isDying = true;
                RpcMonsterDissolve();
            }
            monster.HP -= remind;
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
                        while(ironDemon == null)
                            yield return new WaitForSeconds(0.01f);
                        ironDemon.GetComponent<SkeletonAnimation>().state.Complete += OnIronDemonAnimationComplete;
                    }
                break;
            }
        }
    }

    IEnumerator HongDanHyangEyeFlicker()
    {
        while(true)
        {
            yield return new WaitForSeconds(Random.Range(2f,5f));
            anim.state.SetAnimation(1,"Eye",false);
        }
    }

    IEnumerator ErisTransform()
    {
        M_TurnManager.instance.StartAnimation(this,0,"Change1",false);
        erisMode = ErisMode.MAD;
        yield return new WaitForSeconds(2f);
        M_TurnManager.instance.StartAnimation(this,0,"VIdle",true);
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

    // ----------------------------------------------           Buff 관련 함수          ---------------------------------------------------//

    // 붕괴 쇠락 등은 공유 // 꽃가루 뭐시기는 개인
    public int GainBuff(BuffType buffType, int value, bool isDebuff, bool isInfinity, bool isDecrease, bool isSeparate, TargetObject tar, Card card)
    {
        int retVal = 0;
        if(objectType == ObjectType.PLAYER && tar != this && CardData.instance.CheckCardCharacteristic(card,CardCharacteristic.GOOWON)) value *= 2; // 이곳에 구원 등록
        if(tar.HasBuff(BuffType.THEREISNOJABI) && buffType == BuffType.APDO)
        {
            int toalStack = GetBuffValue(buffType,tar) + value;
            StaticDamageToMonster(toalStack * tar.GetBuffValue(BuffType.THEREISNOJABI));
            if(HasBuff(buffType,tar))buffs.Remove(buffs.Find(buff => buff.type == buffType && buff.user == tar.netId));
            return 0;
        }

        if((buffs.Find(buff => buff.type == buffType && buff.user == tar.netId) == null && isSeparate )|| (buffs.Find(buff => buff.type == buffType) == null && !isSeparate )|| (isInfinity && value <= 0)) // 버프 신규 등록
        {
            if(value == 0 && !isInfinity)return 0;
            
            Buff newBuff = new Buff(buffType,value,isDebuff,isInfinity,isDecrease,isSeparate,tar);
            buffs.Add(newBuff);
            for(int i = 0 ;i < buffs.Count ; i++)
                Debug.Log(buffs[i].type);
            retVal =  buffs.FindIndex(buff => buff == newBuff);
        }
        else // 버프가 있을경우 중첩 상승
        {
            Buff modItem;
            int indexOfOldItem;
            if(isSeparate) 
            {
                modItem = new Buff(buffs.Find(buff => buff.type == buffType && buff.user == tar.netId));
                indexOfOldItem = buffs.FindIndex(buff => buff.type == buffType && buff.user == tar.netId);
            }
            else
            {
                modItem = new Buff(buffs.Find(buff => buff.type == buffType));
                indexOfOldItem = buffs.FindIndex(buff => buff.type == buffType);
            }
            
            modItem.value += value;
            if(modItem.type == BuffType.APDO && modItem.value >= currentApDoRequirement) // 압도 처리
            {
                monster.APDO();
                modItem.value -= currentApDoRequirement;
                currentApDoRequirement += 4;
            }
            if(modItem.value == 0)
                buffs.RemoveAt(indexOfOldItem);
            else
                buffs[indexOfOldItem] = modItem;
            retVal = indexOfOldItem;
        }
        return retVal;
    }

    public int GetBuffValue(BuffType buffType, TargetObject tar)
    {
        if(tar == null)
        {
            if(buffs.Find(buff => buff.type == buffType) == null) return 0;
            else return buffs.Find(buff => buff.type == buffType).value;
        }
        else
        {
            if(buffs.Find(buff => buff.type == buffType && buff.user == tar.netId) == null) return 0;
            else return buffs.Find(buff => buff.type == buffType && buff.user == tar.netId).value;
        }
    }

    public int GetBuffValue(BuffType buffType)
    {
        int retVal = 0;
        foreach(Buff buff in buffs)
        {
            if(buff.type  == buffType)
                retVal += buff.value;
        }
        return retVal;
    }

    public int GetBuffValueByIndex(int index)
    {
        return buffs[index].value;
    }

    public void GainBuffByIndex(int index, int value)
    {
        Buff newBuff = new Buff(buffs[index]);
        newBuff.value += value;
        buffs[index] = newBuff;
    }

    public void GainDefense(int value)
    {
        defense += value;
    }

    public bool HasBuff(BuffType buffType)
    {
        return buffs.FindIndex(buff => buff.type == buffType) != -1;
    }

    public bool HasBuff(BuffType buffType, TargetObject user)
    {
        return buffs.FindIndex(buff => buff.type == buffType && buff.user == user.netId) != -1;
    }

    private void ReArrangeBuffEffectIndex(int index)
    {
        buffTrunBeginEffect.Remove(index);
        buffCardDrowEffect.Remove(index);
        buffCardUseEffect.Remove(index);
        buffTurnEndEffect.Remove(index);
        List<int> keyList = new List<int>();
        keyList = buffTrunBeginEffect.Keys.ToList();
        foreach(int itemKey in keyList)
        {
            if(itemKey > index)
            {
                buffTrunBeginEffect.Add(itemKey-1,buffTrunBeginEffect[itemKey]);
                buffTrunBeginEffect.Remove(itemKey);
            }
        }
        keyList = buffCardDrowEffect.Keys.ToList();
        foreach(int itemKey in keyList)
        {
            if(itemKey > index)
            {
                buffCardDrowEffect.Add(itemKey-1,buffCardDrowEffect[itemKey]);
                buffCardDrowEffect.Remove(itemKey);
            }
        }
        keyList = buffCardUseEffect.Keys.ToList();
        foreach(int itemKey in keyList)
        {
            if(itemKey > index)
            {
                buffCardUseEffect.Add(itemKey-1,buffCardUseEffect[itemKey]);
                buffCardUseEffect.Remove(itemKey);
            }
        }
        keyList = buffTurnEndEffect.Keys.ToList();
        foreach(int itemKey in keyList)
        {
            if(itemKey > index)
            {
                buffTurnEndEffect.Add(itemKey-1,buffTurnEndEffect[itemKey]);
                buffTurnEndEffect.Remove(itemKey);
            }
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

    public void OnIronDemonAnimationComplete(Spine.TrackEntry trackEntry)
    {
        if(trackEntry.Animation.Name == "Defense")
            ironDemon.GetComponent<SkeletonAnimation>().state.SetAnimation(0,"Idle",true);
    }

    public void ApllyIronDemonAnimationCallbackFunction()
    {
        OnChangedIronDemonLocation(this,this);
    }

    // --------------------------------------------------------- Server Method -----------------------------------------------------------//

    [Server]
    IEnumerator PlayerDeathProcess()
    {
        foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
        {
            if(target.player.character == Character.HONGDANHYANG)
            {
                if(target.ironDemonLocation == this)
                {
                    yield return M_TurnManager.instance.IronDemonReturnProcess(target);
                }
            }
        }
        foreach(CardOnHand cardOnHand in player.GetComponent<GamePlayerDeck>().cardOnHands)
            NetworkServer.Destroy(cardOnHand.gameObject);
        player.GetComponent<GamePlayerDeck>().cardOnHands.Clear();
        M_TurnManager.instance.spawnedPlayerList.Remove(this);
        NetworkServer.Destroy(this.gameObject);
    }

    [Server]
    public void ServerProcessMonsterDeath()
    {
        M_TurnManager.instance.monsterDeathOperating = true;
        M_TurnManager.instance.ProcessMonsterDeath(this);
    }

    // --------------------------------------------------------- Rpc Method -----------------------------------------------------------//
    
    [ClientRpc]
    public void SetIronDemonParent(Transform p)
    {
        ironDemon.transform.parent = p;
    }

    [ClientRpc]
    public void RpcMonsterDissolve()
    {
        if(gameObject.activeSelf){
            monster.StartDissolveEffect(() => {
                if(isServer){
                    ServerProcessMonsterDeath();
                }
            });
        }
    }

    // ---------------------------------------------------------SynclList Callback ,Syncvar Hook -----------------------------------------------------------//

    public void OnChangedBuff(SyncList<Buff>.Operation op, int index, Buff oldBuff, Buff newBuff)
    {
        if(newBuff != null)
            if((newBuff.type == BuffType.ICHI_ATTACK || newBuff.type == BuffType.ICHI_DEFENSE) && objectType == ObjectType.PLAYER)
                foreach(CardOnHand cardOnHand in player.GetComponent<GamePlayerDeck>().cardOnHands)
                    cardOnHand.CardInfoChangedEvent.Invoke();
        switch (op)
        {
            case SyncList<Buff>.Operation.OP_ADD:
                buffIndicator.SetBuff(newBuff,index);
                break;
            case SyncList<Buff>.Operation.OP_INSERT:
                buffIndicator.SetBuff(newBuff,index);
                break;
            case SyncList<Buff>.Operation.OP_REMOVEAT:
                ReArrangeBuffEffectIndex(index);
                buffIndicator.RemoveBuff(index);
                buffTrunBeginEffect.Remove(index);
                break;
            case SyncList<Buff>.Operation.OP_SET:
                buffIndicator.SetBuff(newBuff,index);
                break;
            case SyncList<Buff>.Operation.OP_CLEAR:
                break;
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
            M_EffectManager.instance.DisPlayeDamage(this, (oldVal - newVal));
        }
        if(player != null){
            if(player.netIdentity == NetworkClient.connection.identity){
                player.HP = newVal;
            }
        }
        if(oldVal > 0 && newVal > 0){ // 체력이 0 이상이면, 캐릭터 피격음성 재생
            selectedNamePlate.SetHPValue(newVal, playerMaxHP, 10);
            PlayCharaterHitVoice();
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
                AudioClip buffSound = M_SoundManager.instance.sfxClips[SFX_TYPE.Common].Find((audioClip) => audioClip.name.Equals("common_shield_down"));
                M_SoundManager.instance.PlaySFX(buffSound, buffSound.length);
            }else{
                M_EffectManager.instance.DisplayDefence(this, true, value);
                AudioClip buffSound = M_SoundManager.instance.sfxClips[SFX_TYPE.Common].Find((audioClip) => audioClip.name.Equals("common_shield_up"));
                M_SoundManager.instance.PlaySFX(buffSound, buffSound.length);
            }
        }else{
            if(objectType != ObjectType.PLAYER && !M_TurnManager.instance.monsterShieldInitialize && isServer){
                monster.OnBreakedShield();
            }
        }
    }
}
