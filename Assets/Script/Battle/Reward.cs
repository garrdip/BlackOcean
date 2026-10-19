using System;
using System.Collections;
using System.Collections.Generic;


[System.Serializable]
public class Reward
{
    public uint netId; // 보상데이터 소유 게임플레이어 netId
    public string guid; // 보상데이터 고유값 (보상 데이터들 구분을 위한 용도)
    public Reward_Type reward_Type; // 보상데이터 타입
    public int rewardGold = 0; // 골드 보상
    public string rewardName = ""; // 장비/물약 드랍 표시 이름 (아이템 개편 Phase 7-5 — 이미 인벤토리에 지급된 항목의 표시 전용)
    public string rewardColorHex = ""; // 장비 등급 색 (EquipGradeDB ColorHex). 비면 흰색
}


public enum Reward_Type{
    Item,   // (구 유물 — 폐기됨. 분기만 남김)
    Gold,
    Exp,    // 경험치 (RPG 전환 — 전투 종료 시 서버가 즉시 지급, 목록 표시용)
    Equip,  // 장비 드랍 (아이템 개편 Phase 7 — 서버가 즉시 인벤토리에 지급, 목록은 표시 전용)
    Potion, // 소모품 드랍 (위와 같음)
}
