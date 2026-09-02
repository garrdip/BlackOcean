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
}


public enum Reward_Type{
    Item,
    Gold,
    Exp, // 경험치 (RPG 전환 — 전투 종료 시 서버가 즉시 지급, 목록 표시용)
}