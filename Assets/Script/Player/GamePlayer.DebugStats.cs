using UnityEngine;
using ProjectD;

// GamePlayer partial — 디버그 스탯 창 (OnGUI 임시 UI, 테스트 전용).
// 우상단 '스탯' 버튼으로 토글 — 스킬트리/장비 창과 같은 자리를 쓰므로 상호 배타. GamePlayer.SkillTree.cs의 OnGUI가 호출한다.
// 표시: 레벨/EXP/포인트/골드, HP, 전투 자원, 스탯 6종(기본/장비/합계), 다음 레벨 성장치(LevelGrowthTable 확정값), 성장 시드/만렙 총 성장.
public partial class GamePlayer
{
    bool guiStatsOpen;

    void DrawStatsGUI()
    {
        if (GUI.Button(new Rect(Screen.width - 600f, 10f, 140f, 30f), guiStatsOpen ? "스탯 닫기" : "스탯"))
        {
            guiStatsOpen = !guiStatsOpen;
            if (guiStatsOpen) { SkillTreeUIManager.instance?.Show(false); guiEquipOpen = false; } // 같은 자리 — 상호 배타 (스킬트리는 캔버스 팝업)
        }
        if (!guiStatsOpen) return;

        float windowWidth = 560f;
        float windowHeight = 420f;
        Rect windowRect = new Rect(Screen.width - windowWidth - 10f, 85f, windowWidth, windowHeight);
        GUI.Box(windowRect, $"{character} 스탯");

        float x = windowRect.x + 16f;
        float y = windowRect.y + 32f;
        const float lineHeight = 24f;
        void Line(string text)
        {
            GUI.Label(new Rect(x, y, windowWidth - 32f, lineHeight), text);
            y += lineHeight;
        }
        // 스탯 행 — 이름 / 기본 / 장비 보정 / 합계 (컬럼 정렬)
        void Row(string name, string baseText, string equipText, string totalText)
        {
            GUI.Label(new Rect(x, y, 110f, lineHeight), name);
            GUI.Label(new Rect(x + 110f, y, 90f, lineHeight), baseText);
            GUI.Label(new Rect(x + 200f, y, 90f, lineHeight), equipText);
            GUI.Label(new Rect(x + 290f, y, 90f, lineHeight), totalText);
            y += lineHeight;
        }
        void StatRow(string name, int baseValue, int total)
        {
            int bonus = total - baseValue;
            Row(name, baseValue.ToString(), bonus == 0 ? "-" : (bonus > 0 ? $"+{bonus}" : bonus.ToString()), total.ToString());
        }

        int required = LevelData.GetRequiredExp(level);
        Line($"Lv.{level} / {LevelData.MaxLevel}    EXP {exp} / {(required > 0 ? required.ToString() : "MAX")}    스킬 포인트 {skillPoints}    골드 {gold}");
        Line($"HP {HP} / {MaxHP}");
        CharacterStatData.Entry stat = CharacterStatData.Get(character);
        string resourceName = stat != null ? stat.resource.ToString() : "자원";
        Line(maxResource > 0 ? $"{resourceName} {currentResource} / {maxResource}" : $"{resourceName} — 자원 대신 HP 소모");
        y += 8f;

        Row("스탯", "기본", "장비", "합계");
        StatRow("힘", strength, TotalStrength);
        StatRow("민첩", agility, TotalAgility);
        StatRow("지능", intelligence, TotalIntelligence);
        StatRow("방어력", defense, TotalDefense);
        StatRow("마법방어", magicDefense, TotalMagicDefense);
        StatRow("제어", control, control); // 제어는 장비 옵션 없음
        y += 8f;

        // 다음 레벨 성장치 — 시드 기반 분배라 미리 확정되어 있다 (LevelGrowthTable)
        if (required > 0)
        {
            int next = level + 1;
            Line($"다음 레벨(Lv.{next}) 성장:  HP+{Growth(LevelGrowthTable.Stat.HP, next)}  힘+{Growth(LevelGrowthTable.Stat.STR, next)}  민첩+{Growth(LevelGrowthTable.Stat.AGI, next)}"
               + $"  지능+{Growth(LevelGrowthTable.Stat.INT, next)}  방어+{Growth(LevelGrowthTable.Stat.DEF, next)}  마방+{Growth(LevelGrowthTable.Stat.MDEF, next)}  제어+{Growth(LevelGrowthTable.Stat.CTRL, next)}");
        }
        Line($"성장 시드 {growthSeed}    만렙 총 성장 (HP/힘/민첩/지능/방어/마방/제어): "
           + $"{Total(LevelGrowthTable.Stat.HP)}/{Total(LevelGrowthTable.Stat.STR)}/{Total(LevelGrowthTable.Stat.AGI)}/{Total(LevelGrowthTable.Stat.INT)}"
           + $"/{Total(LevelGrowthTable.Stat.DEF)}/{Total(LevelGrowthTable.Stat.MDEF)}/{Total(LevelGrowthTable.Stat.CTRL)}");
        y += 8f;
        Line($"장착 {equippedItems.Count}개    인벤토리 장비 {inventoryEquips.Count}개 / 소모품 {inventoryConsumables.Count}개    습득 노드 {learnedNodes.Count}개");
    }

    int Growth(LevelGrowthTable.Stat stat, int reachedLevel) => LevelGrowthTable.GetGrowth(growthSeed, character, stat, reachedLevel);
    int Total(LevelGrowthTable.Stat stat) => LevelGrowthTable.GetTotal(character, stat);
}
