using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

[System.Serializable]
public class Monster
{
    public string name;
    public int MAXHP;
    public List<MonsterActionList> behavior = new List<MonsterActionList>();
    public List<Buff> buffList = new List<Buff>();

    // RPG 전환 확장 스탯 (MonsterStatDB.csv — MonsterDB의 위치 기반 포맷과 분리)
    public List<AttackAttribute> weaknesses = new List<AttackAttribute>(); // 약점 속성 (1개 이상)
    public AttackAttribute attackAttribute = AttackAttribute.NONE;         // 이 몬스터의 공격 속성
    public int tpShield = 0;                                               // TP 실드 — 약점 공격 시 깎이는 브레이크 게이지
    public int agility = 5;                                                // 민첩 — TP(턴 게이지) 충전 속도
    public int exp = 10;                                                   // 처치 경험치 (MonsterStatDB Exp) — 전투 종료 시 파티 전원에게 합산 지급

    // 위험도 보너스 스탯 (위험도 시스템, MonsterStatDB HazardAtk/HazardDef/HazardHp) — 유효 위험도 1당 증가량 (소수 허용, 적용 시 반올림)
    public float hazardAtkBonus = 0f;                                      // 위험도 1당 공격 피해 증가량
    public float hazardDefBonus = 0f;                                      // 위험도 1당 방어 획득량 증가량
    public float hazardHpBonus = 0f;                                       // 위험도 1당 최대체력 증가량
}
