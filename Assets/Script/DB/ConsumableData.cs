using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ProjectD;

/// <summary>
/// 소모품 테이블 (Resources/DB/ConsumableDB.csv — 아이템 개편 Phase 6, ToDo_RPG_ITEM 0-9·6-1) + 효과 리플렉션 바인딩.
/// Effect 컬럼 = ConsumableEffects의 정적 메서드 이름 (SkillData와 같은 방식 — 새 효과는 CSV 행 + 메서드 1개, 같은 효과의 변형은 행만).
/// ValidTarget은 스킬과 같은 enum이며 NONE = 자신 (1차는 NONE만). BattleOnly = 전투 전용, UsableResource = 자원 종류가 맞는 캐릭터만 (ANY = 제한 없음).
/// 보유는 GamePlayer.inventoryConsumables(SyncList, 인스턴스당 1행 = 중복이 곧 개수), 사용은 GamePlayer.ServerUseConsumable 한 곳.
/// </summary>
public static class ConsumableData
{
    public class Def
    {
        public string potionNo;
        public string potionName;
        public string effectName;              // ConsumableEffects 메서드명
        public ValidTarget validTarget;        // NONE = 자신 (1차). 추후 MEMBER/ENEMY
        public int value;                      // 회복량·버프량
        public int duration;                   // 버프 지속 턴 (사용 턴 제외 — Q33)
        public bool battleOnly;                // 전투 중에만 사용 가능
        public BattleResourceType usableResource; // NONE = 아무 캐릭터 / MP·RAGE·HP = 그 자원 캐릭터만
        public int price;
        public int dropWeight;                 // 풀 지정자(ANY_POTION) 드랍 가중치
        public string description;
        public ExecuteConsumable execute;
    }

    static Dictionary<string, Def> potions;

    static void EnsureLoaded()
    {
        if (potions != null) return;
        potions = new Dictionary<string, Def>();
        CsvTable table = CsvTable.LoadFromResources("DB/ConsumableDB");
        foreach (CsvTable.Row row in table.rows)
        {
            try
            {
                string resource = row.Get("UsableResource").Trim();
                var def = new Def
                {
                    potionNo = row.Get("PotionNo").Trim(),
                    potionName = row.Get("Name").Trim(),
                    effectName = row.Get("Effect").Trim(),
                    validTarget = (ValidTarget)Enum.Parse(typeof(ValidTarget), row.Get("ValidTarget").Trim()),
                    value = row.GetInt("Value"),
                    duration = row.GetInt("Duration"),
                    battleOnly = row.Get("BattleOnly").Trim() == "1",
                    usableResource = resource.Length == 0 || resource == "ANY" ? BattleResourceType.NONE : (BattleResourceType)Enum.Parse(typeof(BattleResourceType), resource),
                    price = row.GetInt("Price"),
                    dropWeight = row.GetInt("DropWeight"),
                    description = row.Get("Description"),
                };
                MethodInfo method = typeof(ConsumableEffects).GetMethod(def.effectName, BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    Debug.LogError($"[ConsumableData] 소모품 효과 메서드가 없습니다: {def.effectName} ({row.lineNumber}행) — ConsumableEffects에 동명의 정적 메서드 필요");
                    continue;
                }
                def.execute = (ExecuteConsumable)Delegate.CreateDelegate(typeof(ExecuteConsumable), method);
                potions[def.potionNo] = def;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConsumableData] ConsumableDB 로드 실패 ({row.lineNumber}행) — {e.Message}");
            }
        }
    }

    public static Def Get(string potionNo)
    {
        EnsureLoaded();
        return potions.TryGetValue(potionNo, out Def def) ? def : null;
    }

    public static List<Def> GetAll()
    {
        EnsureLoaded();
        return new List<Def>(potions.Values);
    }

    /// <summary>DropWeight 가중 랜덤 1개 (0은 제외). 없으면 null</summary>
    public static Def PickWeighted()
    {
        EnsureLoaded();
        int total = 0;
        foreach (Def def in potions.Values) if (def.dropWeight > 0) total += def.dropWeight;
        if (total <= 0) return null;
        int roll = UnityEngine.Random.Range(0, total);
        foreach (Def def in potions.Values)
        {
            if (def.dropWeight <= 0) continue;
            roll -= def.dropWeight;
            if (roll < 0) return def;
        }
        return null;
    }

    /// <summary>사용 가능 여부 — 서버 검증(ServerUseConsumable)과 클라 UI(버튼 비활성·사유)가 같은 규칙을 쓴다. reasonKey = 로컬라이즈 키 (가능하면 null)</summary>
    public static bool CanUse(Def def, GamePlayer user, bool inBattle, out string reasonKey)
    {
        reasonKey = null;
        if (def == null || user == null) { reasonKey = "ui.msg.potion_invalid"; return false; }
        if (def.battleOnly && !inBattle) { reasonKey = "ui.msg.potion_battle_only"; return false; }
        if (def.usableResource != BattleResourceType.NONE)
        {
            CharacterStatData.Entry stat = CharacterStatData.Get(user.character);
            if (stat == null || stat.resource != def.usableResource) { reasonKey = "ui.msg.potion_resource_mismatch"; return false; }
        }
        return true;
    }

    /// <summary>물약 표시 이름 — 로컬라이즈 `potion.&lt;PotionNo&gt;.name`, 없으면 CSV 한국어</summary>
    public static string DisplayName(Def def) => def == null ? "" : M_LanguageManager.Get($"potion.{def.potionNo}.name", def.potionName);

    /// <summary>물약 설명 — 로컬라이즈 `potion.&lt;PotionNo&gt;.desc`, 없으면 CSV 한국어</summary>
    public static string DisplayDescription(Def def) => def == null ? "" : M_LanguageManager.Get($"potion.{def.potionNo}.desc", def.description);

    /// <summary>사용 불가 사유 짧은 표기 (임시 UI)</summary>
    public static string ReasonText(string reasonKey)
    {
        switch (reasonKey)
        {
            case "ui.msg.potion_battle_only": return M_LanguageManager.Get("ui.msg.potion_battle_only_short", "전투 전용");
            case "ui.msg.potion_resource_mismatch": return M_LanguageManager.Get("ui.msg.potion_resource_mismatch_short", "자원 불일치");
            default: return "";
        }
    }
}
