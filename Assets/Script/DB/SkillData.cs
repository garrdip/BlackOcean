using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ProjectD;

/// <summary>
/// 스킬 테이블 (Resources/DB/SkillDB.csv) + 효과 리플렉션 바인딩.
/// CardData의 CSV명↔메서드명 바인딩 패턴을 계승하되, 효과 메서드는 이 클래스의 정적 메서드다
/// (SkillNo 컬럼 = 메서드명. 캐릭터별 partial 파일에 구현 — SkillData.Eris.cs 등).
/// 스킬 실행은 서버(M_TurnManager의 TP 전투 루프)가 코루틴으로 구동한다.
/// </summary>
public static partial class SkillData
{
    public class SkillDef
    {
        public string skillNo;            // 메서드명이자 고유 키
        public Character character;
        public string skillName;
        public BattleResourceType costType;
        public int cost;
        public AttackAttribute attribute;
        public ValidTarget validTarget;   // ENEMY(단일)/ENEMY_ALL(전체)/NONE(자신·대상 불필요)
        public int power;                 // 계수 % (스탯 x power / 100) — 스킬 레벨 1 기준
        public bool scalesWithInt;        // true면 지능, false면 힘 계수
        public bool innate;               // 기본 스킬 — 스킬트리 습득 없이 사용 가능 (고행길/피의 가속)
        public int powerPerLevel;         // 스킬 레벨(SKILL_LEVEL 노드)당 계수 % 증가량 — 0이면 구 방식(+20%/레벨) 사용
        public bool passive;              // 패시브 스킬 (기사도 등) — 전투 액션 목록에 표시하지 않으며 execute는 호출되지 않는다
        public string description;
        public ExecuteSkill execute;
    }

    static Dictionary<string, SkillDef> skills;

    static void EnsureLoaded()
    {
        if (skills != null) return;
        skills = new Dictionary<string, SkillDef>();
        CsvTable table = CsvTable.LoadFromResources("DB/SkillDB");
        foreach (CsvTable.Row row in table.rows)
        {
            try
            {
                var def = new SkillDef
                {
                    skillNo = row.Get("SkillNo").Trim(),
                    character = (Character)Enum.Parse(typeof(Character), row.Get("Character").Trim()),
                    skillName = row.Get("Name").Trim(),
                    costType = (BattleResourceType)Enum.Parse(typeof(BattleResourceType), row.Get("CostType").Trim()),
                    cost = row.GetInt("Cost"),
                    attribute = (AttackAttribute)Enum.Parse(typeof(AttackAttribute), row.Get("Attribute").Trim()),
                    validTarget = (ValidTarget)Enum.Parse(typeof(ValidTarget), row.Get("ValidTarget").Trim()),
                    power = row.GetInt("Power"),
                    scalesWithInt = row.Get("ScaleStat").Trim() == "INT",
                    innate = row.Get("Innate").Trim() == "1",
                    powerPerLevel = int.TryParse(row.Get("PowerPerLevel"), out int perLevel) ? perLevel : 0,
                    passive = row.Get("Passive").Trim() == "1",
                    description = row.Get("Description"),
                };

                MethodInfo method = typeof(SkillData).GetMethod(def.skillNo, BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    Debug.LogError($"[SkillData] 스킬 메서드가 없습니다: {def.skillNo} ({row.lineNumber}행) — SkillData partial에 동명의 정적 메서드 필요");
                    continue;
                }
                def.execute = (ExecuteSkill)Delegate.CreateDelegate(typeof(ExecuteSkill), method);
                skills[def.skillNo] = def;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SkillData] SkillDB 로드 실패 ({row.lineNumber}행) — {e.Message}");
            }
        }
    }

    public static SkillDef Get(string skillNo)
    {
        EnsureLoaded();
        return skills.TryGetValue(skillNo, out SkillDef def) ? def : null;
    }

    /// <summary>해당 캐릭터의 스킬 목록. 스킬트리(Phase 3) 도입 전까지는 캐릭터의 전체 스킬을 보유한 것으로 취급한다</summary>
    public static List<SkillDef> GetSkillsByCharacter(Character character)
    {
        EnsureLoaded();
        var result = new List<SkillDef>();
        foreach (SkillDef def in skills.Values)
        {
            if (def.character == character) result.Add(def);
        }
        return result;
    }
}
