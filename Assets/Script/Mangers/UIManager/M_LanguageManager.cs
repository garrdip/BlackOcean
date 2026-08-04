using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 다국어 관리자 — 키 기반 스트링 테이블 방식.
///
/// 구조
///   Resources/Language/Locales.csv   지원 언어 목록 (code,displayName,font)
///   Resources/Language/&lt;code&gt;.csv    언어별 번역 테이블 (key,text[,source_ko])
///
/// 한국어(ko)가 기준 언어이자 폴백이다. 카드/버프/용어의 한국어 원문은 기존 DB CSV에 그대로 두고
/// 다른 언어만 스트링 테이블로 덮어쓴다. 따라서 번역이 없는 항목은 자동으로 한국어로 표시되며,
/// 언어를 추가할 때 게임 데이터(CardDB 등)를 건드릴 필요가 없다 — CSV 한 장과 Locales.csv 한 줄이면 된다.
///
/// 조회 순서: 현재 언어 → 한국어 테이블 → 호출부가 넘긴 원문(DB CSV 값) → 키 그대로
/// </summary>
public class M_LanguageManager : SingletonD<M_LanguageManager>
{
    public const string FallbackLocaleCode = "ko";
    const string PlayerPrefsKey = "CurrentLanguage";
    const string ResourceFolder = "Language/";

    public class LocaleInfo
    {
        public string code;             // ko, en, ja, zh-Hans, fr, es …
        public string displayName;      // 드롭다운 표기 (해당 언어 표기법으로 — 한국어, English, 日本語 …)
        public string fontResourcePath; // Resources 기준 TMP 폰트 에셋 경로 (비우면 폰트 교체 안 함)
    }

    static readonly List<LocaleInfo> locales = new List<LocaleInfo>();
    static readonly Dictionary<string, string> currentTable = new Dictionary<string, string>();
    static readonly Dictionary<string, string> fallbackTable = new Dictionary<string, string>();
    static readonly HashSet<string> reportedMissingKeys = new HashSet<string>();
    static bool initialized;

    /// <summary>지원 언어 목록 (Locales.csv 순서).</summary>
    public static IReadOnlyList<LocaleInfo> Locales { get { EnsureInitialized(); return locales; } }

    public static string CurrentLocaleCode { get; private set; } = FallbackLocaleCode;

    /// <summary>한국어 여부 — 조사(을/를) 자동 부착 등 한국어 전용 처리의 게이트.</summary>
    public static bool IsKorean => CurrentLocaleCode == FallbackLocaleCode;

    public static bool isLanguageLoadDone => initialized;

    /// <summary>현재 언어 폰트. Locales.csv에 폰트가 지정되지 않았으면 null (각 텍스트의 기존 폰트 유지).</summary>
    public static TMP_FontAsset currnetFont { get; private set; }

    /// <summary>UI 텍스트 갱신용 (TextUpdater/FontUpdater가 구독).</summary>
    public delegate void LanguageChanged();
    public static LanguageChanged languageChangedCallback;

    /// <summary>DB 텍스트 재적용용 (CardData/BuffData가 구독). UI 갱신보다 먼저 호출된다.</summary>
    public static event Action onLocaleChanged;

    protected override void Awake()
    {
        base.Awake();
        EnsureInitialized();
    }

    /// <summary>
    /// 최초 접근 시 1회 로드. 씬에 매니저가 없거나 GameScene부터 실행해도 동작하도록 지연 초기화한다.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true; // 로드 도중의 재진입 방지

        LoadLocaleList();
        LoadTable(FallbackLocaleCode, fallbackTable);
        ApplyLocale(ResolveInitialLocale(), notify: false);
    }

    // ------------------------------------------------------------ 조회 -------------------------------------------------------------- //

    /// <summary>번역 조회. fallbackText는 보통 DB CSV의 한국어 원문을 넘긴다.</summary>
    public static string Get(string key, string fallbackText = null)
    {
        EnsureInitialized();
        if (!string.IsNullOrEmpty(key))
        {
            if (currentTable.TryGetValue(key, out string text) && !string.IsNullOrEmpty(text)) return text;
            if (fallbackTable.TryGetValue(key, out string ko) && !string.IsNullOrEmpty(ko)) return ko;
        }
        if (fallbackText != null) return fallbackText;

        if (!string.IsNullOrEmpty(key) && reportedMissingKeys.Add(key))
            Debug.LogWarning($"[Language] 번역 키 없음: '{key}' ({CurrentLocaleCode}) — Resources/Language/{CurrentLocaleCode}.csv 확인");
        return key;
    }

    public static bool TryGet(string key, out string text)
    {
        EnsureInitialized();
        if (!string.IsNullOrEmpty(key))
        {
            if (currentTable.TryGetValue(key, out text) && !string.IsNullOrEmpty(text)) return true;
            if (fallbackTable.TryGetValue(key, out text) && !string.IsNullOrEmpty(text)) return true;
        }
        text = null;
        return false;
    }

    // ------------------------------------------------------------ 언어 전환 -------------------------------------------------------------- //

    /// <summary>언어 전환 + 저장. DB 텍스트 재적용과 UI 갱신이 이어서 일어난다.</summary>
    public void SetLocale(string code) => SetLocaleStatic(code);

    public static void SetLocaleStatic(string code)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(code) || code == CurrentLocaleCode) return;
        if (FindLocale(code) == null)
        {
            Debug.LogError($"[Language] 지원하지 않는 언어 코드: '{code}' — Resources/Language/Locales.csv 확인");
            return;
        }
        ApplyLocale(code, notify: true);
        PlayerPrefs.SetString(PlayerPrefsKey, code);
        PlayerPrefs.Save();
    }

    /// <summary>기존 호출 규약 유지 — UI 갱신 콜백만 다시 쏜다.</summary>
    public void ApplyChangedLanguage()
    {
        languageChangedCallback?.Invoke();
    }

    static void ApplyLocale(string code, bool notify)
    {
        CurrentLocaleCode = code;
        if (code == FallbackLocaleCode)
        {
            currentTable.Clear();
            foreach (KeyValuePair<string, string> pair in fallbackTable) currentTable[pair.Key] = pair.Value;
        }
        else
        {
            LoadTable(code, currentTable);
        }
        reportedMissingKeys.Clear();
        LoadFont(FindLocale(code));

        if (notify)
        {
            onLocaleChanged?.Invoke();         // DB 텍스트 먼저 재적용 (카드 설명 등)
            languageChangedCallback?.Invoke(); // 그 다음 화면 텍스트 갱신
        }
    }

    static LocaleInfo FindLocale(string code)
    {
        foreach (LocaleInfo locale in locales)
            if (locale.code == code) return locale;
        return null;
    }

    // ------------------------------------------------------------ 로드 -------------------------------------------------------------- //

    static void LoadLocaleList()
    {
        locales.Clear();
        CsvTable table = CsvTable.LoadFromResources(ResourceFolder + "Locales");
        foreach (CsvTable.Row row in table.rows)
        {
            string code = row.Get("code");
            if (string.IsNullOrEmpty(code)) continue;
            locales.Add(new LocaleInfo
            {
                code = code,
                displayName = string.IsNullOrEmpty(row.Get("displayName")) ? code : row.Get("displayName"),
                fontResourcePath = row.Get("font")
            });
        }
        if (locales.Count == 0)
        {
            Debug.LogError("[Language] Locales.csv를 읽지 못했습니다. 한국어 단일 언어로 진행합니다.");
            locales.Add(new LocaleInfo { code = FallbackLocaleCode, displayName = "한국어", fontResourcePath = "" });
        }
    }

    static void LoadTable(string code, Dictionary<string, string> into)
    {
        into.Clear();
        CsvTable table = CsvTable.LoadFromResources(ResourceFolder + code);
        foreach (CsvTable.Row row in table.rows)
        {
            string key = row.Get("key");
            if (string.IsNullOrEmpty(key)) continue;
            // CSV는 한 줄이 한 항목이므로 줄바꿈은 "\n" 두 글자로 적고 여기서 실제 개행으로 바꾼다
            into[key] = row.Get("text").Replace("\\n", "\n");
        }
    }

    static void LoadFont(LocaleInfo locale)
    {
        if (locale == null || string.IsNullOrEmpty(locale.fontResourcePath))
        {
            currnetFont = null; // 폰트 미지정 = 각 텍스트의 기존 폰트 유지
            return;
        }
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>(locale.fontResourcePath);
        if (font == null)
            Debug.LogError($"[Language] 폰트를 찾을 수 없습니다: Resources/{locale.fontResourcePath} ({locale.code})");
        currnetFont = font;
    }

    // ------------------------------------------------------------ 초기 언어 결정 -------------------------------------------------------------- //

    // 저장된 사용자 선택 → 스팀 클라이언트 언어 → 한국어
    static string ResolveInitialLocale()
    {
        string saved = PlayerPrefs.GetString(PlayerPrefsKey, "");
        if (!string.IsNullOrEmpty(saved) && FindLocale(saved) != null) return saved;

        string steam = ResolveSteamLocale();
        if (!string.IsNullOrEmpty(steam) && FindLocale(steam) != null) return steam;

        return FallbackLocaleCode;
    }

    static string ResolveSteamLocale()
    {
        try
        {
            if (!SteamManager.Initialized) return null;
            string steamLanguage = Steamworks.SteamApps.GetCurrentGameLanguage();
            if (string.IsNullOrEmpty(steamLanguage)) return null;
            // 스팀 API 언어명 → 로케일 코드 (https://partner.steamgames.com/doc/store/localization)
            switch (steamLanguage)
            {
                case "koreana":
                case "korean": return "ko";
                case "english": return "en";
                case "japanese": return "ja";
                case "schinese": return "zh-Hans";
                case "tchinese": return "zh-Hant";
                case "french": return "fr";
                case "spanish": return "es";
                case "latam": return "es-419";
                case "german": return "de";
                case "russian": return "ru";
                case "brazilian": return "pt-BR";
                default: return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Language] 스팀 언어 조회 실패 — 기본 언어로 진행합니다: {e.Message}");
            return null;
        }
    }
}
