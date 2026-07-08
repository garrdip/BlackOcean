using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// CSV 공통 파서.
/// - 첫 줄을 헤더로 읽어 컬럼명 기반 접근(Get("Name"))을 지원한다 — 컬럼 순서 변경/삽입에 안전.
/// - 따옴표 필드("a,b" / 이중따옴표 "" 이스케이프)를 지원한다 — 설명 텍스트에 콤마가 들어가도 안전.
/// - 인덱서(row[i])는 범위를 벗어나면 빈 문자열을 반환한다 — 반복 컬럼(MonsterDB 등) 위치 접근용.
/// - 빈 줄은 건너뛴다. 행별 파싱 오류 처리는 호출부에서 try/catch로 격리할 것.
/// </summary>
public class CsvTable
{
    public class Row
    {
        readonly CsvTable table;
        readonly string[] fields;
        public readonly int lineNumber; // 파일 기준 1-base 라인 번호 (오류 로그용)

        public Row(CsvTable table, string[] fields, int lineNumber)
        {
            this.table = table;
            this.fields = fields;
            this.lineNumber = lineNumber;
        }

        public int Count => fields.Length;

        /// <summary>위치 기반 접근. 범위를 벗어나면 빈 문자열 반환.</summary>
        public string this[int index] => (index >= 0 && index < fields.Length) ? fields[index] : "";

        /// <summary>헤더 컬럼명 기반 접근. 컬럼이 없으면 빈 문자열 반환(오류는 테이블에서 1회 로그).</summary>
        public string Get(string column) => this[table.GetColumnIndex(column)];

        public int GetInt(string column) => int.Parse(Get(column));

        public T GetEnum<T>(string column) where T : struct => Enum.Parse<T>(Get(column));
    }

    public readonly string sourcePath;
    public readonly List<Row> rows = new List<Row>();

    readonly Dictionary<string, int> columnIndexByName = new Dictionary<string, int>();
    readonly HashSet<string> reportedMissingColumns = new HashSet<string>();

    CsvTable(string sourcePath)
    {
        this.sourcePath = sourcePath;
    }

    /// <summary>Resources 경로에서 CSV를 로드. 파일이 없으면 빈 테이블 반환 + 에러 로그.</summary>
    public static CsvTable LoadFromResources(string resourcePath)
    {
        CsvTable table = new CsvTable(resourcePath);
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset == null)
        {
            Debug.LogError($"[CsvTable] CSV 파일을 찾을 수 없습니다: Resources/{resourcePath}");
            return table;
        }

        string[] lines = textAsset.text.Split('\n');
        bool headerParsed = false;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimEnd('\r').Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] fields = SplitCsvLine(line);
            if (!headerParsed)
            {
                for (int col = 0; col < fields.Length; col++)
                {
                    string name = fields[col].Trim();
                    if (name.Length > 0 && !table.columnIndexByName.ContainsKey(name))
                        table.columnIndexByName.Add(name, col);
                }
                headerParsed = true;
                continue;
            }
            table.rows.Add(new Row(table, fields, i + 1));
        }
        return table;
    }

    /// <summary>컬럼명 → 인덱스. 없으면 -1 반환 + 최초 1회 에러 로그.</summary>
    public int GetColumnIndex(string column)
    {
        if (columnIndexByName.TryGetValue(column, out int index)) return index;
        if (reportedMissingColumns.Add(column))
            Debug.LogError($"[CsvTable] '{sourcePath}'에 '{column}' 컬럼이 없습니다. 헤더: {string.Join(",", columnIndexByName.Keys)}");
        return -1;
    }

    public bool HasColumn(string column) => columnIndexByName.ContainsKey(column);

    // 따옴표 필드를 지원하는 한 줄 분리 ("a,b" → 하나의 필드, "" → 따옴표 문자)
    static string[] SplitCsvLine(string line)
    {
        List<string> fields = new List<string>();
        StringBuilder current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; } // "" 이스케이프
                    else inQuotes = false;
                }
                else current.Append(c);
            }
            else
            {
                if (c == '"' && current.Length == 0) inQuotes = true;
                else if (c == ',') { fields.Add(current.ToString()); current.Clear(); }
                else current.Append(c);
            }
        }
        fields.Add(current.ToString());
        return fields.ToArray();
    }
}
