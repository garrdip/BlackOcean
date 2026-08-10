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
        public string priorityFontName; // 이 언어에서 TMP 전역 폴백 맨 앞으로 올릴 폰트 에셋 이름
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

    /// <summary>
    /// 언어 목록과 번역 테이블을 디스크에서 다시 읽는다.
    /// 테이블은 최초 1회만 로드되므로, 에디터에서 CSV를 고치거나 언어를 추가한 뒤 확인할 때 쓴다.
    /// </summary>
    public static void Reload()
    {
        string keep = CurrentLocaleCode;
        initialized = false;
        EnsureInitialized();
        if (FindLocale(keep) == null) return; // 목록에서 사라진 언어면 새로 결정된 언어를 그대로 둔다
        ApplyLocale(keep, notify: true);
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
        ApplyFallbackPriority(FindLocale(code));

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
                fontResourcePath = row.Get("font"),
                priorityFontName = row.Get("priorityFont")
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

    /// <summary>
    /// 현재 언어의 폰트를 TMP 전역 폴백 목록 맨 앞으로 올린다.
    ///
    /// 일본어와 중국어는 같은 유니코드 코드포인트라도 자형이 다른 한자가 있다(直·骨·今 등).
    /// 폴백은 앞에서부터 찾으므로, 순서를 그대로 두면 중국어 화면에 일본어 자형이 섞인다.
    /// 폰트 자체를 바꾸는 게 아니라 '없는 글리프를 어디서 먼저 가져올지'만 조정하므로
    /// 각 텍스트의 디자인 폰트는 그대로 유지된다.
    /// </summary>
    static void ApplyFallbackPriority(LocaleInfo locale)
    {
        if (locale == null || string.IsNullOrEmpty(locale.priorityFontName)) return;

        List<TMP_FontAsset> fallbacks = TMP_Settings.fallbackFontAssets;
        if (fallbacks == null || fallbacks.Count < 2) return;

        int index = fallbacks.FindIndex(f => f != null && f.name == locale.priorityFontName);
        if (index < 0)
        {
            Debug.LogWarning($"[Language] 폴백 폰트 '{locale.priorityFontName}'를 찾을 수 없습니다 ({locale.code}) — TMP Settings의 Fallback 목록 확인");
            return;
        }
        if (index == 0) return; // 이미 맨 앞

        TMP_FontAsset font = fallbacks[index];
        fallbacks.RemoveAt(index);
        fallbacks.Insert(0, font);
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

    /// <summary>
    /// 스팀 초기화가 끝난 뒤 호출 (M_SteamManager.Start).
    /// 씬 로드 초기에는 SteamManager가 아직 준비 전이라 스팀 언어를 읽지 못하므로,
    /// 사용자가 언어를 직접 고른 적이 없으면 이 시점에 스팀 클라이언트 언어를 적용한다.
    /// </summary>
    public static void ApplySteamLocaleIfNoUserChoice()
    {
        EnsureInitialized();
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString(PlayerPrefsKey, ""))) return; // 사용자 선택이 우선
        string steam = ResolveSteamLocale();
        if (!string.IsNullOrEmpty(steam) && steam != CurrentLocaleCode && FindLocale(steam) != null)
            ApplyLocale(steam, notify: true);
    }

    static string ResolveSteamLocale()
    {
        try
        {
            // Initialized는 SteamManager가 없으면 자동 생성해 씬의 SteamManager와 중복을 만들므로
            // 부작용 없는 InitializedSafe로 확인한다
            if (!SteamManager.InitializedSafe) return null;
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
