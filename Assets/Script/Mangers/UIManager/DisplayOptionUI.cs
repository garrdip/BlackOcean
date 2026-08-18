using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 옵션 팝업 좌측 하단(TempSetting 슬롯)을 Display 설정 섹션으로 구성한다.
///
/// 씬을 직접 수정하지 않고 Language 섹션의 기존 UI(헤더 텍스트/드롭다운)를 런타임에 복제해
/// 같은 스타일로 만든다 — 섹션 UI를 씬에서 다시 디자인하게 되면 이 클래스는 복제 대신
/// 인스펙터 참조를 받도록 바꾸면 된다.
///
/// 해상도와 화면 모드(전체 화면/전체 창/창 모드)를 PlayerPrefs에 저장하고 게임 시작 시 적용한다.
/// 에디터에서는 Screen.SetResolution이 게임 뷰에 적용되지 않으므로 빌드에서 확인해야 한다.
/// </summary>
public class DisplayOptionUI : MonoBehaviour
{
    const string PREF_WIDTH = "DISPLAY_WIDTH";
    const string PREF_HEIGHT = "DISPLAY_HEIGHT";
    const string PREF_MODE = "DISPLAY_MODE"; // FullScreenMode enum 값을 그대로 저장

    // 드롭다운 항목 순서와 FullScreenMode 매핑
    static readonly FullScreenMode[] MODES =
    {
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.FullScreenWindow,
        FullScreenMode.Windowed,
    };
    static readonly string[] MODE_KEYS = { "ui.Settings_Full_Screen", "ui.Settings_Borderless_Window", "ui.Settings_Windowed" };
    static readonly string[] MODE_FALLBACKS = { "전체 화면", "전체 창 모드", "창 모드" };

    TMP_Dropdown resolutionDropdown;
    TMP_Dropdown screenModeDropdown;
    readonly List<Vector2Int> resolutionList = new List<Vector2Int>();

    /// <summary>저장된 디스플레이 설정을 적용한다. 게임 시작 시 1회 호출 (저장된 값이 없으면 아무것도 안 함).</summary>
    public static void ApplySavedSettings()
    {
        if(!PlayerPrefs.HasKey(PREF_WIDTH)) return;
        int width = PlayerPrefs.GetInt(PREF_WIDTH, Screen.width);
        int height = PlayerPrefs.GetInt(PREF_HEIGHT, Screen.height);
        FullScreenMode mode = (FullScreenMode)PlayerPrefs.GetInt(PREF_MODE, (int)Screen.fullScreenMode);
        Screen.SetResolution(width, height, mode);
    }

    /// <summary>Language 섹션의 드롭다운을 원본 삼아 Display 섹션 UI를 만든다.</summary>
    public void Init(TMP_Dropdown languageDropdown)
    {
        // 계층: Dropdown → 내용 영역 → LanguageSetting → OptionTabs
        Transform languageSetting = languageDropdown.transform.parent.parent;
        Transform optionTabs = languageSetting.parent;

        // OptionTabs는 그리드(언어/사운드/빈칸/빈칸) — 첫 번째 TempSetting이 좌측 하단 칸이다
        Transform section = null;
        foreach(Transform child in optionTabs)
        {
            if(child.name == "TempSetting"){ section = child; break; }
        }
        if(section == null)
        {
            Debug.LogWarning("[DisplayOptionUI] TempSetting 슬롯을 찾지 못해 Display 섹션을 만들 수 없다");
            return;
        }
        section.name = "DisplaySetting";

        Transform headerSlot = section.GetChild(0); // 섹션 제목 배경
        Transform content = section.GetChild(1);    // 섹션 내용 영역 (1078x720)
        Transform languageHeaderText = languageSetting.GetChild(0).GetChild(0);

        // 섹션 제목 + (라벨, 드롭다운) 2줄 — 드롭다운 x위치는 Language 섹션과 동일하게 맞춘다.
        // 해상도 드롭다운의 펼침 목록이 아래쪽 화면 모드 UI에 가려지지 않도록,
        // 화면(아래쪽) 요소를 먼저 만들고 해상도(위쪽) 요소를 마지막 형제로 만든다 (UI는 뒤 형제가 위에 그려진다)
        CloneText(languageHeaderText, headerSlot, Vector2.zero, "ui.Settings_Display", "디스플레이");

        CloneText(languageHeaderText, content, new Vector2(-159, 64), "ui.Settings_Screen_Mode", "화면 모드");
        screenModeDropdown = CloneDropdown(languageDropdown, content, new Vector2(-159, -30), "ScreenModeDropdown");

        CloneText(languageHeaderText, content, new Vector2(-159, 290), "ui.Settings_Resolution", "해상도");
        resolutionDropdown = CloneDropdown(languageDropdown, content, new Vector2(-159, 196), "ResolutionDropdown");

        SyncUI();

        resolutionDropdown.onValueChanged.AddListener(HandleResolutionChanged);
        screenModeDropdown.onValueChanged.AddListener(HandleScreenModeChanged);
        M_LanguageManager.languageChangedCallback += RefreshScreenModeOptionTexts;
    }

    void OnDestroy()
    {
        M_LanguageManager.languageChangedCallback -= RefreshScreenModeOptionTexts;
    }

    /// <summary>실제 화면 상태(해상도/모드)에 맞춰 드롭다운 목록과 선택값을 다시 채운다. 팝업이 열릴 때마다 호출된다.</summary>
    public void SyncUI()
    {
        if(resolutionDropdown == null || screenModeDropdown == null) return;
        BuildResolutionOptions();
        BuildScreenModeOptions();
    }

    // ------------------------------------------------------------ UI 복제 -------------------------------------------------------------- //

    TMP_Text CloneText(Transform source, Transform parent, Vector2 anchoredPosition, string localeKey, string fallbackText)
    {
        GameObject clone = Instantiate(source.gameObject, parent, false);
        clone.name = localeKey;
        RectTransform rect = clone.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(534, 60); // 드롭다운 폭에 맞춘 라벨 영역

        TMP_Text text = clone.GetComponent<TMP_Text>();
        text.text = fallbackText;

        // 언어 변경 대응 — TextUpdater가 키 조회/폰트 교체를 처리한다
        TextUpdater updater = clone.GetComponent<TextUpdater>();
        if(updater == null) updater = clone.AddComponent<TextUpdater>();
        updater.key = localeKey;
        updater.thisText = text;
        updater.LanguageChanged();
        return text;
    }

    TMP_Dropdown CloneDropdown(TMP_Dropdown source, Transform parent, Vector2 anchoredPosition, string name)
    {
        GameObject clone = Instantiate(source.gameObject, parent, false);
        clone.name = name;
        clone.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;

        // 원본에 붙어 있던 호버 이벤트(언어 드롭다운의 DropdownLight를 켜는)가 따라오지 않도록 제거
        EventTrigger trigger = clone.GetComponent<EventTrigger>();
        if(trigger != null) Destroy(trigger);
        Transform light = clone.transform.Find("DropdownLight");
        if(light != null) light.gameObject.SetActive(false);

        TMP_Dropdown dropdown = clone.GetComponent<TMP_Dropdown>();
        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.ClearOptions();
        return dropdown;
    }

    // ------------------------------------------------------------ 항목 구성 -------------------------------------------------------------- //

    void BuildResolutionOptions()
    {
        resolutionList.Clear();
        foreach(Resolution res in Screen.resolutions)
        {
            Vector2Int size = new Vector2Int(res.width, res.height);
            if(!resolutionList.Contains(size)) resolutionList.Add(size); // 주사율 차이로 인한 중복 제거
        }
        Vector2Int current = new Vector2Int(Screen.width, Screen.height);
        if(!resolutionList.Contains(current)) resolutionList.Add(current);
        resolutionList.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

        resolutionDropdown.ClearOptions();
        foreach(Vector2Int size in resolutionList)
        {
            resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(size.x + " x " + size.y));
        }
        resolutionDropdown.SetValueWithoutNotify(resolutionList.IndexOf(current));
        resolutionDropdown.RefreshShownValue();
    }

    void BuildScreenModeOptions()
    {
        screenModeDropdown.ClearOptions();
        for(int i = 0; i < MODES.Length; i++)
        {
            screenModeDropdown.options.Add(new TMP_Dropdown.OptionData(M_LanguageManager.Get(MODE_KEYS[i], MODE_FALLBACKS[i])));
        }
        screenModeDropdown.SetValueWithoutNotify(CurrentModeIndex());
        screenModeDropdown.RefreshShownValue();
    }

    void RefreshScreenModeOptionTexts()
    {
        if(screenModeDropdown == null) return;
        for(int i = 0; i < MODES.Length && i < screenModeDropdown.options.Count; i++)
        {
            screenModeDropdown.options[i].text = M_LanguageManager.Get(MODE_KEYS[i], MODE_FALLBACKS[i]);
        }
        screenModeDropdown.RefreshShownValue();
    }

    static int CurrentModeIndex()
    {
        for(int i = 0; i < MODES.Length; i++)
        {
            if(Screen.fullScreenMode == MODES[i]) return i;
        }
        return 1; // MaximizedWindow 등 그 외 상태는 전체 창 모드로 취급
    }

    // ------------------------------------------------------------ 변경 적용 -------------------------------------------------------------- //

    void HandleResolutionChanged(int index)
    {
        Apply();
        PlayClickSound();
    }

    void HandleScreenModeChanged(int index)
    {
        Apply();
        PlayClickSound();
    }

    void Apply()
    {
        if(resolutionDropdown.value < 0 || resolutionDropdown.value >= resolutionList.Count) return;
        Vector2Int size = resolutionList[resolutionDropdown.value];
        FullScreenMode mode = MODES[Mathf.Clamp(screenModeDropdown.value, 0, MODES.Length - 1)];

        Screen.SetResolution(size.x, size.y, mode);

        PlayerPrefs.SetInt(PREF_WIDTH, size.x);
        PlayerPrefs.SetInt(PREF_HEIGHT, size.y);
        PlayerPrefs.SetInt(PREF_MODE, (int)mode);
        PlayerPrefs.Save();
    }

    static void PlayClickSound()
    {
        M_SoundManager sound = M_SoundManager.instance;
        if(sound == null) return;
        AudioClip audioClip = sound.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
        sound.PlaySFX(audioClip, audioClip.length);
    }
}
