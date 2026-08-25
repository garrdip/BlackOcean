using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

// JSON(JsonUtility) 직렬화 대상 — 튜플·프로퍼티·딕셔너리는 직렬화되지 않으므로 public 필드만 사용할 것
[System.Serializable]
public class SaveData
{
    public SaveDataPlayer[] players = new SaveDataPlayer[3];
}

[System.Serializable]
public class SaveDataPlayer
{
    public ulong ownerSteamId;
    public bool isActive; // JSON 역직렬화 시 빈 슬롯도 기본 인스턴스로 채워지므로, 실제 저장된 플레이어인지 이 플래그로 구분
    public Character character = new Character();
    public int HP, MaxHP;
    public List<Card> cards = new List<Card>();
}
