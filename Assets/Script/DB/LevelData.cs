using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨 테이블 (Resources/DB/LevelDB.csv).
/// RequiredExp = 해당 레벨에서 다음 레벨로 가는 데 필요한 경험치. 0이면 최대 레벨(성장 정지).
/// BalanceData와 같은 정적 로더 패턴 — 파일/키 누락 시 에러 로그 후 안전값으로 동작한다.
/// </summary>
public static class LevelData
{
    static Dictionary<int, int> requiredExp;
    static int maxLevel = 1;

    static void EnsureLoaded()
    {
        if (requiredExp != null) return;
        requiredExp = new Dictionary<int, int>();
        CsvTable table = CsvTable.LoadFromResources("DB/LevelDB");
        foreach (CsvTable.Row row in table.rows)
        {
            if (!int.TryParse(row.Get("Level"), out int level))
            {
                Debug.LogError($"[LevelData] Level이 정수가 아닙니다 ({row.lineNumber}행)");
                continue;
            }
            requiredExp[level] = row.GetInt("RequiredExp");
            if (level > maxLevel) maxLevel = level;
        }
    }

    public static int MaxLevel
    {
        get { EnsureLoaded(); return maxLevel; }
    }

    /// <summary>해당 레벨에서 다음 레벨까지 필요한 경험치. 최대 레벨이거나 미정의 레벨이면 0 (더 이상 성장하지 않음)</summary>
    public static int GetRequiredExp(int level)
    {
        EnsureLoaded();
        if (requiredExp.TryGetValue(level, out int value)) return value;
        return 0;
    }
}
