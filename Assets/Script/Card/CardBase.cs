using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

[System.Serializable]
public class CardBase
{
    // 주의: 이 클래스는 Card에 담겨 Mirror로 통째로 직렬화된다(필드 추가 = 카드 동기화 대역폭 증가).
    // 그래서 번역 원문 같은 로컬 전용 데이터는 여기 두지 않고 CardData가 따로 들고 있다.
    // 네트워크로 받은 CardBase는 보낸 쪽 언어의 문자열이므로, 화면 표시는
    // CardData.instance.GetLocalizedName/Description(baseCard)로 로컬 언어 원본을 거쳐야 한다.
    public Character character;
    public string name;
    public string description;
    public string cardNumber;
    public bool isTargetable = true;
    public CardType cardType;
    public int cost;
    public int maxExperience;
    public ValidTarget validTarget;
    public List<Infomation> info = new List<Infomation>();
    public List<CardCharacteristic> cardCharacteristics;
    public List<string> cardInfo = new List<string>();

    public CardBase()
    {
        cardCharacteristics = new List<CardCharacteristic>();
    }
}

[System.Serializable]
public class Infomation
{
    public string info = "";
    public int colorCode = 0;

    public Infomation(){}

    public Infomation(string str, int num)
    {
        info = str;
        colorCode = num;
    }
}