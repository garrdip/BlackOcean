using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

[System.Serializable]
public class Card
{
    public CardBase baseCard;
    public string guid; // 카드 구분 용도로 사용되는 고유아이디 변수
    public bool isEnhanced;
    public int costAddition;
    public List<CardCharacteristic> cardCharacteristics = new List<CardCharacteristic>();
    public int experience;
    public bool tempEnhanced;
    public bool isReturnable; // 지치지 않는자에서만 런타임으로 쓰임
    public bool isSoldout; // 상점카드에서만 사용되는 구매상태 변수
    public int cardPrice; // 상점카드에서만 사용되는 카드가격 변수


    public Card(CardBase basecard)
    {
        baseCard = basecard;
        guid = System.Guid.NewGuid().ToString();
    }

    public Card(){} // For Mirror Library default constructor

    public void EndBattleCardInitialize()
    {
        cardCharacteristics.Clear();
        tempEnhanced = false;
        costAddition = 0;
    }

    public Card (Card card)
    {
        baseCard = card.baseCard;
        guid = card.guid;
        isEnhanced = card.isEnhanced;
        costAddition = card.costAddition;
        experience = card.experience;
        cardCharacteristics = card.cardCharacteristics;
        tempEnhanced = card.tempEnhanced;
    }

    // 카드 클래스 깊은복사
    public Card CardDeepCopy(bool isEndBattle)
    {
        Card card = new Card();
        card.baseCard = baseCard;
        card.guid = guid;
        card.isEnhanced = isEnhanced;
        card.costAddition = costAddition;
        card.experience = experience;
        tempEnhanced = false;
        if(!isEndBattle) // 전투 종료 후 남길 특성 여기서 넣어줘야함
        {
            foreach(CardCharacteristic cardChar in cardCharacteristics)
                card.cardCharacteristics.Add(cardChar);
        }
        return card;
    }
}


