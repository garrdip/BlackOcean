using System;
using System.Collections;
using System.Collections.Generic;


[System.Serializable]
public class Reward
{   
    public uint netId; // 보상데이터 소유 게임플레이어 netId
    public string guid; // 보상데이터 고유값 (보상 데이터들 구분을 위한 용도)
    public Reward_Type reward_Type; // 보상데이터 타입
}


public enum Reward_Type{
    Item,
    Card,
    Gold,
}