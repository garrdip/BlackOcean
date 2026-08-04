using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 현지화 운영 도구 (에디터 전용).
///
/// - 번역 스켈레톤 내보내기: 게임 안의 모든 번역 키를 한국어 원문과 함께 CSV로 뽑는다.
///   번역가에게는 이 파일 한 장만 전달하면 되고, 채워진 파일을 Assets/Resources/Language/에 넣으면 끝난다.
/// - 번역 누락 리포트: 언어별로 비어 있는 키를 집계한다.
/// - 마크업 검증: CardDB 설명문의 중괄호 짝과 @{용어} 키가 Description.csv에 있는지 확인한다.
/// </summary>
public class LocalizationTools : EditorWindow
{
    const string LanguageFolder = "Assets/Resources/Language/";

    string newLocaleCode = "ja";
    Vector2 scroll;
    string report = "";

    [MenuItem("Tools/현지화/현지화 도구")]
    static void Open()
    {
        GetWindow<LocalizationTools>("현지화 도구").minSize = new Vector2(520, 360);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("번역 스켈레톤 내보내기", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "모든 번역 키 + 한국어 원문을 담은 CSV를 만든다.\n" +
            "text 열을 채워 Assets/Resources/Language/ 에 두고, Locales.csv에 한 줄 추가하면 언어가 늘어난다.",
            MessageType.Info);
        using (new EditorGUILayout.HorizontalScope())
        {
            newLocaleCode = EditorGUILayout.TextField("로케일 코드", newLocaleCode);
            if (GUILayout.Button("내보내기", GUILayout.Width(120))) ExportSkeleton(newLocaleCode);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("점검", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("번역 누락 리포트")) report = BuildMissingReport();
            if (GUILayout.Button("마크업 검증")) report = BuildMarkupReport();
            if (GUILayout.Button("언어 다시 읽기"))
            {
                // 번역 테이블은 최초 1회만 로드된다 — CSV를 고쳤거나 언어를 추가했으면 눌러서 반영
                M_LanguageManager.Reload();
                report = "다시 읽음 — 현재 언어: " + M_LanguageManager.CurrentLocaleCode
                       + " / 지원 언어 " + M_LanguageManager.Locales.Count + "종";
            }
        }

        EditorGUILayout.Space();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    // ------------------------------------------------------------ 키 수집 -------------------------------------------------------------- //

    /// <summary>번역 키 → 한국어 원문. DB CSV(카드/버프/용어/특성) + ko.csv(UI)를 모두 훑는다.</summary>
    static List<KeyValuePair<string, string>> CollectKeys()
    {
        List<KeyValuePair<string, string>> keys = new List<KeyValuePair<string, string>>();

        foreach (CsvTable.Row row in CsvTable.LoadFromResources("Language/ko").rows)
        {
            string key = row.Get("key");
            if (!string.IsNullOrEmpty(key)) keys.Add(new KeyValuePair<string, string>(key, row.Get("text")));
        }

        foreach (CsvTable.Row row in CsvTable.LoadFromResources("DB/CardDB").rows)
        {
            string cardNo = row.Get("CardNo");
            if (string.IsNullOrEmpty(cardNo)) continue;
            keys.Add(new KeyValuePair<string, string>(LocKey.CardName(cardNo), row.Get("Name")));
            keys.Add(new KeyValuePair<string, string>(LocKey.CardDesc(cardNo), row.Get("Description")));
        }

        foreach (CsvTable.Row row in CsvTable.LoadFromResources("DB/BuffDB").rows)
        {
            string buffEnum = row.Get("enum");
            if (string.IsNullOrEmpty(buffEnum)) continue;
            keys.Add(new KeyValuePair<string, string>(LocKey.BuffName(buffEnum), row.Get("name")));
            keys.Add(new KeyValuePair<string, string>(LocKey.BuffDesc(buffEnum), row.Get("description")));
        }

        foreach (CsvTable.Row row in CsvTable.LoadFromResources("DB/Description").rows)
        {
            string term = row.Get("info");
            if (string.IsNullOrEmpty(term)) continue;
            keys.Add(new KeyValuePair<string, string>(LocKey.TermName(term), row.Get("name")));
            keys.Add(new KeyValuePair<string, string>(LocKey.TermDesc(term), row.Get("description")));
        }

        foreach (CsvTable.Row row in CsvTable.LoadFromResources("DB/CardCharacteristic").rows)
        {
            string characteristic = row.Get("enum");
            if (string.IsNullOrEmpty(characteristic)) continue;
            keys.Add(new KeyValuePair<string, string>(LocKey.Characteristic(characteristic), row.Get("name")));
        }

        return keys;
    }

    // ------------------------------------------------------------ 내보내기 -------------------------------------------------------------- //

    static void ExportSkeleton(string localeCode)
    {
        if (string.IsNullOrEmpty(localeCode))
        {
            EditorUtility.DisplayDialog("현지화", "로케일 코드를 입력하세요 (ja, zh-Hans, fr, es …).", "확인");
            return;
        }

        string path = LanguageFolder + localeCode + ".csv";
        Dictionary<string, string> existing = new Dictionary<string, string>();
        if (File.Exists(path))
        {
            // 이미 번역한 내용은 보존한다 (카드가 추가되어 다시 뽑을 때 덮어쓰지 않도록)
            foreach (CsvTable.Row row in CsvTable.LoadFromResources("Language/" + localeCode).rows)
            {
                string key = row.Get("key");
                if (!string.IsNullOrEmpty(key)) existing[key] = row.Get("text");
            }
        }

        List<KeyValuePair<string, string>> keys = CollectKeys();
        StringBuilder builder = new StringBuilder();
        builder.Append("key,text,source_ko\n");
        int translated = 0;
        foreach (KeyValuePair<string, string> pair in keys)
        {
            existing.TryGetValue(pair.Key, out string text);
            if (!string.IsNullOrEmpty(text)) translated++;
            builder.Append(Escape(pair.Key)).Append(',').Append(Escape(text)).Append(',').Append(Escape(pair.Value)).Append('\n');
        }

        Directory.CreateDirectory(LanguageFolder);
        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        AssetDatabase.Refresh();
        Debug.Log($"[현지화] {path} — 키 {keys.Count}개 (기존 번역 {translated}개 유지)");
        EditorUtility.DisplayDialog("현지화",
            $"{path}\n\n키 {keys.Count}개를 내보냈습니다. (기존 번역 {translated}개 유지)\n\n" +
            $"Locales.csv에 '{localeCode}' 행이 없으면 추가해야 게임에 노출됩니다.", "확인");
    }

    // CSV 한 필드 이스케이프 — 콤마/따옴표/줄바꿈이 있으면 따옴표로 감싼다
    static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.IndexOf(',') < 0 && value.IndexOf('"') < 0 && value.IndexOf('\n') < 0) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    // ------------------------------------------------------------ 리포트 -------------------------------------------------------------- //

    static string BuildMissingReport()
    {
        List<KeyValuePair<string, string>> keys = CollectKeys();
        StringBuilder builder = new StringBuilder();
        builder.Append($"전체 번역 키: {keys.Count}개\n\n");

        foreach (CsvTable.Row localeRow in CsvTable.LoadFromResources("Language/Locales").rows)
        {
            string code = localeRow.Get("code");
            if (string.IsNullOrEmpty(code) || code == M_LanguageManager.FallbackLocaleCode) continue;

            Dictionary<string, string> table = new Dictionary<string, string>();
            foreach (CsvTable.Row row in CsvTable.LoadFromResources("Language/" + code).rows)
            {
                string key = row.Get("key");
                if (!string.IsNullOrEmpty(key)) table[key] = row.Get("text");
            }

            List<string> missing = new List<string>();
            foreach (KeyValuePair<string, string> pair in keys)
                if (!table.TryGetValue(pair.Key, out string text) || string.IsNullOrEmpty(text))
                    missing.Add(pair.Key);

            builder.Append($"[{code}] 번역 {keys.Count - missing.Count} / {keys.Count}  (미번역 {missing.Count})\n");
            for (int i = 0; i < missing.Count && i < 20; i++) builder.Append("    ").Append(missing[i]).Append('\n');
            if (missing.Count > 20) builder.Append($"    … 외 {missing.Count - 20}개\n");
            builder.Append('\n');
        }
        return builder.ToString();
    }

    static string BuildMarkupReport()
    {
        HashSet<string> terms = new HashSet<string>();
        foreach (CsvTable.Row row in CsvTable.LoadFromResources("DB/Description").rows)
        {
            string term = row.Get("info");
            if (!string.IsNullOrEmpty(term)) terms.Add(term);
        }
        HashSet<string> cardNumbers = new HashSet<string>();
        foreach (CsvTable.Row row in CsvTable.LoadFromResources("DB/CardDB").rows)
        {
            string cardNo = row.Get("CardNo");
            if (!string.IsNullOrEmpty(cardNo)) cardNumbers.Add(cardNo);
        }

        StringBuilder builder = new StringBuilder();
        int problems = 0;

        // 검사 대상: 한국어 원문(CardDB) + 각 언어 CSV의 card.*.desc
        problems += CheckMarkupSource(builder, "DB/CardDB(한국어 원문)", CollectCardDescriptions(), terms, cardNumbers);
        foreach (CsvTable.Row localeRow in CsvTable.LoadFromResources("Language/Locales").rows)
        {
            string code = localeRow.Get("code");
            if (string.IsNullOrEmpty(code) || code == M_LanguageManager.FallbackLocaleCode) continue;

            List<KeyValuePair<string, string>> descriptions = new List<KeyValuePair<string, string>>();
            foreach (CsvTable.Row row in CsvTable.LoadFromResources("Language/" + code).rows)
            {
                string key = row.Get("key");
                if (!string.IsNullOrEmpty(key) && key.EndsWith(".desc") && !string.IsNullOrEmpty(row.Get("text")))
                    descriptions.Add(new KeyValuePair<string, string>(key, row.Get("text")));
            }
            problems += CheckMarkupSource(builder, "Language/" + code, descriptions, terms, cardNumbers);
        }

        if (problems == 0) builder.Append("문제 없음 — 중괄호 짝·용어 키·카드 참조 모두 정상\n");
        return builder.ToString();
    }

    static List<KeyValuePair<string, string>> CollectCardDescriptions()
    {
        List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
        foreach (CsvTable.Row row in CsvTable.LoadFromResources("DB/CardDB").rows)
        {
            string cardNo = row.Get("CardNo");
            if (!string.IsNullOrEmpty(cardNo)) result.Add(new KeyValuePair<string, string>(cardNo, row.Get("Description")));
        }
        return result;
    }

    static int CheckMarkupSource(StringBuilder builder, string label,
                                 List<KeyValuePair<string, string>> entries, HashSet<string> terms, HashSet<string> cardNumbers)
    {
        int problems = 0;
        foreach (KeyValuePair<string, string> entry in entries)
        {
            string text = entry.Value;
            if (string.IsNullOrEmpty(text)) continue;

            int open = 0, close = 0;
            foreach (char c in text) { if (c == '{') open++; else if (c == '}') close++; }
            if (open != close)
            {
                builder.Append($"[{label}] {entry.Key} — 중괄호 짝 불일치 ({open}/{close}): {text}\n");
                problems++;
            }

            foreach (System.Text.RegularExpressions.Match match in
                     System.Text.RegularExpressions.Regex.Matches(text, @"@\{([^{}]*)\}"))
            {
                if (!terms.Contains(match.Groups[1].Value))
                {
                    builder.Append($"[{label}] {entry.Key} — Description.csv에 없는 용어 '{match.Groups[1].Value}'\n");
                    problems++;
                }
            }

            foreach (System.Text.RegularExpressions.Match match in
                     System.Text.RegularExpressions.Regex.Matches(text, @"\*\{([^{}]*)\}"))
            {
                if (!cardNumbers.Contains(match.Groups[1].Value))
                {
                    builder.Append($"[{label}] {entry.Key} — 존재하지 않는 카드 참조 '{match.Groups[1].Value}'\n");
                    problems++;
                }
            }
        }
        return problems;
    }
}
