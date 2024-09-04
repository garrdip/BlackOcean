using System.Collections;
using System.Collections.Generic;
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
    
    public Dictionary<string,ExecuteCard> CardMethods = new Dictionary<string, ExecuteCard>();
    public Dictionary<string,CurssEffect> curseEffect = new Dictionary<string, CurssEffect>();

    [Header("버프 타입별 아이콘")]
    [SerializedDictionary("BuffType", "Sprite")]
    public SerializedDictionary<BuffType, Sprite> buffIcons = new SerializedDictionary<BuffType, Sprite>();
    
    public Dictionary<string,InfomationDB> infomationDB = new Dictionary<string, InfomationDB>();

    public Dictionary<CardCharacteristic,string> cardCharacteristicToString = new Dictionary<CardCharacteristic, string>();

    public bool isCardOperatingTEST;
    
    public bool isCardOperating{get{
        return isCardOperatingTEST;
    }
    set{
        isCardOperatingTEST = value;
    }}

    public string[] colorList = {"<#ff0000>","<#00ff00>","<#0000ff>","<#ffff00>","<#00ffff>","<#ff00ff>"};

    string cardColor = "<#ffb0bb>";



    public CardSelectCallBack cardSelectCallBack;

    WaitForSeconds tempWait = new WaitForSeconds(0.3f);

    //Version 3
    public void LoadCardDataFromDB()
    {
        TextAsset DBtext = Resources.Load<TextAsset>("DBs/CardDB");
        using (StringReader DB = new StringReader(DBtext.text))
        {          
            while(true)
            {
                string value = DB.ReadLine();
                if( value == null ) break; // 마지막 데이터의 경우 null을 반환
                CardBase card = new CardBase();
                
                string[] values = value.Trim().Split(",");
                if(values[0] == "CardNo") continue; // 첫줄 데이터 스킵   


                card.cardNumber = values[0]; //카드 번호 사실상 메소드이름
                card.character = (Character)Enum.Parse<Character>(values[1]);//케릭터
                card.name = values[2];//카드이름
                //Debug.Log(values[2]);
                card.isTargetable = (values[6] == "Y") ? true : false;
                card.cardType = (CardType)Enum.Parse<CardType>(values[4]);
                card.description = GetCardDescription(values[3],card);
                card.cost = int.Parse(values[5]);
                card.validTarget = (ValidTarget)Enum.Parse<ValidTarget>(values[7]);
                card.maxExperience = int.Parse(values[8]);
                for(int i = 9; i < values.Length; i++)
                {
                    if(values[i] == "")break;
                    card.cardCharacteristics.Add((CardCharacteristic)Enum.Parse<CardCharacteristic>(values[i]));
                }
                ExecuteCard temp = (ExecuteCard)Delegate.CreateDelegate(typeof(ExecuteCard),this,values[0]); // valuse[0] : 메소드 이름
                cards.Add(card);
                CardMethods.Add(card.cardNumber,temp); // cardNumber
            }
        }
        // 게오르크 저주 메소드 추가
        curseEffect.Add("G0",G0_Effect);
        curseEffect.Add("G1",G1_Effect);
        curseEffect.Add("G2",G2_Effect);
        curseEffect.Add("G3",G3_Effect);
        curseEffect.Add("G4",G4_Effect);
        curseEffect.Add("G5",G5_Effect);
        curseEffect.Add("G6",G6_Effect);
        curseEffect.Add("G7",G7_Effect);
        DBtext = Resources.Load<TextAsset>("DBs/Description");
        using (StringReader DB = new StringReader(DBtext.text))
        {          
            while(true)
            {
                string value = DB.ReadLine();
                if( value == null ) break; // 마지막 데이터의 경우 null을 반환
                
                string[] values = value.Trim().Split(",");
                if(values[0] == "info") continue; // 첫줄 데이터 스킵   

                infomationDB.Add(values[0],new InfomationDB(values[1],values[2]));
            }
        }
        DBtext = Resources.Load<TextAsset>("DBs/CardCharacteristic");
        using (StringReader DB = new StringReader(DBtext.text))
        {          
            while(true)
            {
                string value = DB.ReadLine();
                if( value == null ) break; // 마지막 데이터의 경우 null을 반환
                
                string[] values = value.Trim().Split(",");
                if(values[0] == "enum") continue; // 첫줄 데이터 스킵   

                cardCharacteristicToString.Add((CardCharacteristic)Enum.Parse<CardCharacteristic>(values[0]),values[1]);
            }
        }
    }
    
    private string GetCardDescription(string desc, CardBase card)
    {
        int colorCnt = 0;
        string[] values = desc.Trim().Split(" ");
        for(int i = 0 ;i < values.Length ; i++)
        {
            if(values[i].ToCharArray()[0] == '@')
            {
                values[i] = values[i].Remove(0,1);
                card.info.Add(new Infomation(values[i],colorCnt));
                values[i] = colorList[colorCnt] + values[i] + "</color>";
                colorCnt++;
            }
            if(values[i].ToCharArray()[0] == '*') 
            {
                values[i] = values[i].Remove(0,1);
                card.cardInfo.Add(values[i]);
                string temp = cardColor + cards.Find(card => card.cardNumber == values[i]).name + "</color>";
                values[i] = temp;
            }
            if(values[i].ToCharArray()[0] == '~') // 뽑을덱, 버린덱, 잊혀진덱 굵은 글씨
            {
                values[i] = values[i].Remove(0,1);
                card.cardInfo.Add(values[i]);
                string temp = "<b>" + values[i] + "</b>";
                values[i] = temp;
            }

        }
        return string.Join(" ",values); // Concat 메서드를 사용하여 배열의 요소들을 하나로 합침
    }

    public IEnumerator RunCard(Card card,List<TargetObject> targets)
    {
        yield return StartCoroutine(RunCardCoroutine(card,targets));
    }

    private IEnumerator RunCardCoroutine(Card card,List<TargetObject> targets)
    {
        if(card.experience >= card.baseCard.maxExperience)
        {
            yield return CardMethods[card.baseCard.cardNumber+"_E"](card,targets);
            card.experience = 0;
        }
        else
        {
            yield return CardMethods[card.baseCard.cardNumber](card,targets);
            card.experience++;
        }
        isCardOperating = false;
    }
    private IEnumerator ExecuteCardCoroutine(Card card,List<TargetObject> targets)
    {
        if(card.experience >= card.baseCard.maxExperience)
        {
            yield return CardMethods[card.baseCard.cardNumber+"_E"](card,targets);
            card.experience = 0;
        }
        else
        {
            yield return CardMethods[card.baseCard.cardNumber](card,targets);
            card.experience++;
        }
    }

    public IEnumerator CurseCardEffect(Card card, TargetObject tar)
    {
        return CardData.instance.curseEffect[card.baseCard.cardNumber](tar);
    }

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
            if(remind >= value)
                from.playerHP += value;
            else
            {
                from.playerHP = from.playerMaxHP;
                from.defense += value - remind;
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