using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.U2D;
using System.IO;
using System;
using AYellowpaper.SerializedCollections;
using ProjectD;

public partial class CardData : SingletonD<CardData>
{
    [Header("캐릭터별 카드 탬플릿")]
    [SerializedDictionary("Character", "Sprite")]
    public SerializedDictionary<Character, SerializedDictionary<string, Sprite>> characterCardTemplate = new SerializedDictionary<Character, SerializedDictionary<string, Sprite>>();
    
    public SpriteAtlas cardIllustAtlas; // 카드 일러스트 아틀라스
    
    public List<CardBase> cards = new List<CardBase>(); // DB에서 조회한 전체 카드 데이터 목록
    
    public Dictionary<string, ExecuteCard> CardMethods = new Dictionary<string, ExecuteCard>();

    public Dictionary<string, CurssEffect> curseEffect = new Dictionary<string, CurssEffect>();

    public Dictionary<string, InfomationDB> infomationDB = new Dictionary<string, InfomationDB>();

    public Dictionary<CardCharacteristic, string> cardCharacteristicToString = new Dictionary<CardCharacteristic, string>();

    // CSV 원문(한국어) 보관 — 표시용 데이터는 언어에 따라 이 원문에서 다시 만들어진다.
    // CardBase는 Mirror로 직렬화되므로 원문을 거기 두지 않고 여기(로컬 전용)에 둔다.
    readonly Dictionary<string, InfomationDB> infomationSource = new Dictionary<string, InfomationDB>();
    readonly Dictionary<CardCharacteristic, string> characteristicSource = new Dictionary<CardCharacteristic, string>();
    readonly Dictionary<string, string> cardNameSource = new Dictionary<string, string>();
    readonly Dictionary<string, string> cardDescriptionSource = new Dictionary<string, string>();
    readonly Dictionary<string, CardBase> cardByNumber = new Dictionary<string, CardBase>();


    public bool isCardOperatingTEST;
    
    public bool isCardOperating{get{
        return isCardOperatingTEST;
    }
    set{
        isCardOperatingTEST = value;
    }}

    public string[] colorList = {"<#ff0000>","<#00ff00>","<#0EB4FC>","<#ffff00>","<#00ffff>","<#ff00ff>"};

    string cardColor = "<#ffb0bb>";



    public CardSelectCallBack cardSelectCallBack;

    WaitForSeconds tempWait = new WaitForSeconds(0.3f);


    void Start()
    {
        LoadCardDataFromDB();
    }

    //Version 3
    public void LoadCardDataFromDB()
    {
        CsvTable cardTable = CsvTable.LoadFromResources("DB/CardDB");
        List<string> failedCards = new List<string>(); // 로드 실패한 카드 집계 (한 카드의 오류가 전체 로드를 중단시키지 않도록)
        int characteristicStart = cardTable.GetColumnIndex("Characteristic"); // 특성은 이 컬럼부터 가변 길이
        foreach(CsvTable.Row row in cardTable.rows)
        {
            CardBase card = new CardBase();
            try
            {
                card.cardNumber = row.Get("CardNo"); //카드 번호 사실상 메소드이름
                card.character = row.GetEnum<Character>("Character");//케릭터
                // CSV 값은 한국어 원문 = 번역 폴백. 실제 표시 문자열은 ApplyLocalizedText()에서 만든다
                card.name = row.Get("Name");//카드이름
                card.description = row.Get("Description");
                cardNameSource[card.cardNumber] = card.name;
                cardDescriptionSource[card.cardNumber] = card.description;
                card.cardType = row.GetEnum<CardType>("TYPE");
                card.cost = row.GetInt("Cost");
                card.isTargetable = row.Get("Targetable") == "Y";
                card.validTarget = row.GetEnum<ValidTarget>("ValidTarget");
                card.maxExperience = row.GetInt("MaxExp");
                for(int i = characteristicStart; i < row.Count; i++)
                {
                    if(row[i] == "")break;
                    card.cardCharacteristics.Add((CardCharacteristic)Enum.Parse<CardCharacteristic>(row[i]));
                }
                ExecuteCard temp = CreateCardDelegate(card.cardNumber); // 카드번호 = 메소드 이름. 강화 카드는 _E 메서드 생략 시 기본 메서드로 바인딩
                cards.Add(card);
                CardMethods.Add(card.cardNumber,temp); // cardNumber
            }
            catch (Exception e)
            {
                failedCards.Add($"{row[0]} ({row.lineNumber}행) — {e.GetType().Name}: {e.Message}");
            }
        }
        if(failedCards.Count > 0)
            Debug.LogError($"[CardData] CardDB 로드 실패 {failedCards.Count}건. CSV의 CardNo와 CardData_* 파일의 메서드명이 일치하는지 확인하세요.\n{string.Join("\n", failedCards)}");
        // 게오르크 저주 메소드 추가
        curseEffect.Add("G0",G0_Effect);
        curseEffect.Add("G1",G1_Effect);
        curseEffect.Add("G2",G2_Effect);
        curseEffect.Add("G3",G3_Effect);
        curseEffect.Add("G4",G4_Effect);
        curseEffect.Add("G5",G5_Effect);
        curseEffect.Add("G6",G6_Effect);
        curseEffect.Add("G7",G7_Effect);
        foreach(CsvTable.Row row in CsvTable.LoadFromResources("DB/Description").rows)
        {
            try
            {
                infomationSource.Add(row.Get("info"), new InfomationDB(row.Get("name"), row.Get("description")));
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardData] Description 로드 실패: {row[0]} ({row.lineNumber}행) — {e.Message}");
            }
        }
        foreach(CsvTable.Row row in CsvTable.LoadFromResources("DB/CardCharacteristic").rows)
        {
            try
            {
                characteristicSource.Add(row.GetEnum<CardCharacteristic>("enum"), row.Get("name"));
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardData] CardCharacteristic 로드 실패: {row[0]} ({row.lineNumber}행) — {e.Message}");
            }
        }

        // 표시 문자열(카드 이름·설명, 용어, 특성명)을 현재 언어로 조립. 언어가 바뀌면 이 함수만 다시 돈다
        ApplyLocalizedText();
        M_LanguageManager.onLocaleChanged -= ApplyLocalizedText;
        M_LanguageManager.onLocaleChanged += ApplyLocalizedText;
    }

    void OnDestroy()
    {
        M_LanguageManager.onLocaleChanged -= ApplyLocalizedText;
    }

    /// <summary>
    /// 현재 언어 기준으로 표시 문자열을 다시 만든다.
    /// 번역이 없으면 CSV의 한국어 원문(nameSource/descriptionSource)이 그대로 쓰인다.
    /// </summary>
    public void ApplyLocalizedText()
    {
        infomationDB.Clear();
        foreach(KeyValuePair<string, InfomationDB> pair in infomationSource)
        {
            infomationDB.Add(pair.Key, new InfomationDB(
                M_LanguageManager.Get(LocKey.TermName(pair.Key), pair.Value.name),
                M_LanguageManager.Get(LocKey.TermDesc(pair.Key), pair.Value.description)));
        }

        cardCharacteristicToString.Clear();
        foreach(KeyValuePair<CardCharacteristic, string> pair in characteristicSource)
            cardCharacteristicToString.Add(pair.Key, M_LanguageManager.Get(LocKey.Characteristic(pair.Key.ToString()), pair.Value));

        // 이름을 먼저 전부 확정해야 설명문의 *{카드번호} 참조가 번역된 이름으로 치환된다
        cardByNumber.Clear();
        foreach(CardBase cardBase in cards)
        {
            cardNameSource.TryGetValue(cardBase.cardNumber, out string nameSource);
            cardBase.name = M_LanguageManager.Get(LocKey.CardName(cardBase.cardNumber), nameSource);
            cardByNumber[cardBase.cardNumber] = cardBase;
        }

        foreach(CardBase cardBase in cards)
        {
            cardBase.info.Clear();      // 언어 변경 시 툴팁 목록이 중복 누적되지 않도록 초기화
            cardBase.cardInfo.Clear();
            cardDescriptionSource.TryGetValue(cardBase.cardNumber, out string descriptionSource);
            string source = M_LanguageManager.Get(LocKey.CardDesc(cardBase.cardNumber), descriptionSource);
            cardBase.description = CardMarkup.ApplyStructural(source, cardBase, GetCardNameByNumber, colorList, cardColor);
        }
    }

    string GetCardNameByNumber(string cardNumber)
    {
        return cardByNumber.TryGetValue(cardNumber, out CardBase found) ? found.name : null;
    }

    /// <summary>
    /// 네트워크로 받은 CardBase를 로컬 카드 데이터로 되돌린다.
    /// Card는 Mirror로 통째로 직렬화되므로 클라이언트가 받은 name/description은 '보낸 쪽 언어'다.
    /// 화면에 글자를 찍을 때는 반드시 이 함수를 거쳐 각자의 언어로 표시한다.
    /// </summary>
    public CardBase GetLocalCardBase(CardBase networkCard)
    {
        if(networkCard == null) return null;
        return cardByNumber.TryGetValue(networkCard.cardNumber, out CardBase local) ? local : networkCard;
    }

    public string GetLocalizedName(CardBase networkCard)
    {
        CardBase local = GetLocalCardBase(networkCard);
        return local != null ? local.name : "";
    }

    public string GetLocalizedDescription(CardBase networkCard)
    {
        CardBase local = GetLocalCardBase(networkCard);
        return local != null ? local.description : "";
    }

    // 카드 설명의 수치 토큰(!{} #{} ^{} &{} ${}{})을 원본값 그대로 표시 — 버프 미반영 정적 표시용
    public string ReplaceDescription(string str)
    {
        return CardMarkup.ApplyValues(str);
    }

    // 숫자값에 따라 조사(을, 를) 구분해서 반환 — 한국어가 아니면 빈 문자열
    public string GetPrepositionalParticle(int number)
    {
        if(!M_LanguageManager.IsKorean) return "";
        int lastDigit = System.Math.Abs(number) % 10; // 매개변수로 넘어오는 숫자의 마지막 자리 숫자
        if(lastDigit == 0 || lastDigit == 1 || lastDigit == 3 || lastDigit == 6 || lastDigit == 7 || lastDigit == 8){
            return "을"; // 받침 있는 숫자는 '을' 반환
        }else{
            return "를"; // 받침 없는 숫자는 '를' 반환
        }
    }

    public IEnumerator RunCard(Card card,List<TargetObject> targets)
    {
        yield return StartCoroutine(RunCardCoroutine(card,targets));
    }

    // 카드번호 → 실행 메서드 바인딩.
    // 규약: 강화(_E) 카드의 효과가 기본 카드와 동일하면 _E 메서드를 생략할 수 있다 — 이 경우 기본 메서드로 바인딩된다.
    private ExecuteCard CreateCardDelegate(string methodName)
    {
        try
        {
            return (ExecuteCard)Delegate.CreateDelegate(typeof(ExecuteCard), this, methodName);
        }
        catch (Exception)
        {
            if (methodName.EndsWith("_E"))
                return (ExecuteCard)Delegate.CreateDelegate(typeof(ExecuteCard), this, methodName.Substring(0, methodName.Length - 2));
            throw;
        }
    }

    // CardMethods 안전 조회 — 키가 없으면 에러 로그 후 null 반환 (KeyNotFoundException 크래시 방지)
    private ExecuteCard GetCardMethod(string cardNumber)
    {
        if(CardMethods.TryGetValue(cardNumber, out ExecuteCard method))
            return method;
        Debug.LogError($"[CardData] CardMethods에 '{cardNumber}'가 없습니다. CardDB.csv의 CardNo와 카드 메서드명을 확인하세요.");
        return null;
    }

    private IEnumerator RunCardCoroutine(Card card,List<TargetObject> targets)
    {
        if(card.experience >= card.baseCard.maxExperience)
        {
            // 강화(_E) 메서드가 누락된 경우 기본 메서드로 폴백
            ExecuteCard enhanced = GetCardMethod(card.baseCard.cardNumber+"_E") ?? GetCardMethod(card.baseCard.cardNumber);
            if(enhanced != null) yield return enhanced(card,targets);
            card.experience = 0;
        }
        else
        {
            ExecuteCard method = GetCardMethod(card.baseCard.cardNumber);
            if(method != null) yield return method(card,targets);
            card.experience++;
        }
        isCardOperating = false;
    }
    private IEnumerator ExecuteCardCoroutine(Card card,List<TargetObject> targets)
    {
        if(card.experience >= card.baseCard.maxExperience)
        {
            ExecuteCard enhanced = GetCardMethod(card.baseCard.cardNumber+"_E") ?? GetCardMethod(card.baseCard.cardNumber);
            if(enhanced != null) yield return enhanced(card,targets);
            card.experience = 0;
        }
        else
        {
            ExecuteCard method = GetCardMethod(card.baseCard.cardNumber);
            if(method != null) yield return method(card,targets);
            card.experience++;
        }
    }

    public IEnumerator CurseCardEffect(Card card, TargetObject tar)
    {
        if(CardData.instance.curseEffect.TryGetValue(card.baseCard.cardNumber, out CurssEffect effect))
            return effect(tar);
        Debug.LogError($"[CardData] curseEffect에 '{card.baseCard.cardNumber}'가 없습니다.");
        return EmptyEffect();
    }

    private IEnumerator EmptyEffect() { yield break; }

    // 카드 특성 확인
    public bool CheckCardCharacteristic(Card card, CardCharacteristic character)
    {
        if(card == null)return false;
        return (card.cardCharacteristics.Exists(cha => cha == character) || card.baseCard.cardCharacteristics.Exists(cha => cha == character));
    }

    // TargetObject List 구조 : 
    /*
    Index : 내용
    0 : 카드 사용한 Player 
    1 : Target Monster
    이후 : 모든 플레이어 및 몬스터
    */

    public void GeneralSingleAttack(TargetObject from, TargetObject tar, int damage)
    {
        if(from.HasBuff(BuffType.CLOSEPOSE))GeneralGetDefense(from,from,3,null);

        // 이곳에 최소 딜레이 넣어야함
        if(from.buffs.Find(buff => buff.type == BuffType.ICHI_ATTACK) == null)
        {
            tar.DamageToMonster(damage,from);
        }
        else
        {
            if(tar.buffs.Find(buff => buff.type == BuffType.FLOWER) == null)
                tar.DamageToMonster( damage + from.buffs.Find(buff => buff.type == BuffType.ICHI_ATTACK).value,from);
            else
                tar.DamageToMonster( damage + from.buffs.Find(buff => buff.type == BuffType.ICHI_ATTACK).value+ tar.buffs.Find(buff => buff.type == BuffType.FLOWER).value,from);
        }     
    }
    public void GeneralSingleAttack(TargetObject from, TargetObject tar, int damage, int attackMultiply)
    {
        if(from.HasBuff(BuffType.CLOSEPOSE))GeneralGetDefense(from,from,3,null);
        // 이곳에 최소 딜레이 넣어야함
        if(from.buffs.Find(buff => buff.type == BuffType.ICHI_ATTACK) == null)
        {
            tar.DamageToMonster(damage,from);
        }
        else
        {
            if(tar.buffs.Find(buff => buff.type == BuffType.FLOWER) == null)
                tar.DamageToMonster( damage + from.buffs.Find(buff => buff.type == BuffType.ICHI_ATTACK).value * attackMultiply,from);
            else
                tar.DamageToMonster( damage + from.buffs.Find(buff => buff.type == BuffType.ICHI_ATTACK).value * attackMultiply+ tar.buffs.Find(buff => buff.type == BuffType.FLOWER).value,from);
        }     
    }

    private void GeneralSingleDamage(TargetObject tar, int damage)
    {
        tar.DamageToMonster(damage,tar);
    }

    public void GeneralGetDefense(TargetObject from, TargetObject tar, int value, Card card)
    {
        if(from.HasBuff(BuffType.WRAPWINGS))value *= 2;

        if(from.player.character == Character.ERIS && from == tar) // 에리스의 경우 피가 닳아있을경우 체력을 채움
        {
            int remind = from.playerMaxHP - from.playerHP;
            int healAmount = Mathf.Min(remind, value);
            int defensePart = value - healAmount;
            if(healAmount > 0)
                from.HealPlayer(healAmount); // 허물 강화(ENHANCESKIN) 보유 시 회복분이 방어로 전환됨
            if(defensePart > 0)
            {
                // 뒤틀림의 끝: 체력이 가득 찬 상태에서 자신의 카드로 얻을 방어를 적 전체 피해로 전환
                if(card != null && from.HasBuff(BuffType.ENDOFDISTORTION))
                {
                    foreach(TargetObject enemy in M_TurnManager.instance.spawnedMonsterList)
                        enemy.DamageToMonster(defensePart, from);
                }
                else
                    from.defense += defensePart;
            }
        }
        else
        {
            int defenseValue = value;
            if(CheckCardCharacteristic(card,CardCharacteristic.GOOWON)&& tar != from) // 카드 또는 카드 베이스
                defenseValue *= 2;
            if(from.buffs.Find(buff => buff.type == BuffType.ICHI_DEFENSE) == null)
                tar.defense += defenseValue;
            else
                tar.defense += ( defenseValue + from.buffs.Find(buff => buff.type == BuffType.ICHI_DEFENSE).value );
        }
    }

    private void MovePosition(bool isForward,TargetObject tar)
    {
        if(isForward)
        {
            if(tar.player.netId != M_TurnManager.instance.playerOrder[2])
            {
                int currentIndex = M_TurnManager.instance.playerOrder.FindIndex(x => x == tar.player.netId);
                M_TurnManager.instance.SwapPlayerOrder(currentIndex,++currentIndex);
            }
        }
        else
        {
            if(tar.player.netId != M_TurnManager.instance.playerOrder[0])
            {
                int currentIndex = M_TurnManager.instance.playerOrder.FindIndex(x => x == tar.player.netId);
                M_TurnManager.instance.SwapPlayerOrder(currentIndex,--currentIndex);
            }
        }
    }

    private void MovePosition(TargetObject from, TargetObject to)
    {
        int currentIndex = M_TurnManager.instance.playerOrder.FindIndex(x => x == from.player.netId);
        int targetIndex = M_TurnManager.instance.playerOrder.FindIndex(x => x == to.player.netId);
        M_TurnManager.instance.SwapPlayerOrder(currentIndex,targetIndex);
    }

    private void ChangePosition(TargetObject from, int index)
    {
        int currentIndex = M_TurnManager.instance.playerOrder.FindIndex(x => x == from.player.netId);
        M_TurnManager.instance.SwapPlayerOrder(currentIndex,index);
    }

}

public class InfomationDB
{
    public string name;
    public string description;

    public InfomationDB(string nameIn, string descriptionIn)
    {
        name = nameIn;
        description = descriptionIn;
    }
}