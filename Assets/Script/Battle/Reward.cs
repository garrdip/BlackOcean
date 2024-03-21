using System;
using System.Collections;
using System.Collections.Generic;


[System.Serializable]
public class Reward
{   
    public Guid guid; // 보상데이터 고유 아이디값(보상 데이터들 구분을 위한 용도)
    public Reward_Type reward_Type; // 보상데이터 타입
}


public enum Reward_Type{
    Item,
    Card,
    Gold,
}