using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 밸런스 수치 테이블 (Resources/DB/BalanceDB.csv).
/// 코드 곳곳에 박혀 있던 매직 넘버를 키 기반 조회로 중앙화한다.
/// 파일/키 누락 시 에러 로그 + 호출부가 넘긴 기본값으로 동작 — 로드 실패가 게임을 멈추지 않는다.
/// </summary>
public static class BalanceData
{
    static Dictionary<string, int> values;

    static void EnsureLoaded()
    {
        if (values != null) return;
        values = new Dictionary<string, int>();
        CsvTable table = CsvTable.LoadFromResources("DB/BalanceDB");
        foreach (CsvTable.Row row in table.rows)
        {
            string key = row.Get("Key").Trim();
            if (key.Length == 0) continue;
            if (int.TryParse(row.Get("Value"), out int value))
                values[key] = value;
            else
                Debug.LogError($"[BalanceData] 값이 정수가 아닙니다: {key} ({row.lineNumber}행)");
        }
    }

    /// <summary>키 조회. 키 누락 시 에러 로그 후 fallback 반환.</summary>
    public static int Get(string key, int fallback)
    {
        EnsureLoaded();
        if (values.TryGetValue(key, out int value)) return value;
        Debug.LogError($"[BalanceData] BalanceDB에 없는 키: {key} — 기본값 {fallback} 사용");
        return fallback;
    }
}
