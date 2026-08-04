using UnityEngine;
using TMPro;

/// <summary>
/// TMP 텍스트를 현재 언어로 채우는 컴포넌트.
///
/// 키 결정 순서: 인스펙터에 입력한 <see cref="key"/> → 비어 있으면 "ui.&lt;게임오브젝트 이름&gt;"
/// (예전에는 게임오브젝트 이름만이 키였다. 이름을 바꾸면 번역이 끊기므로 명시 키 입력을 권장한다.)
///
/// 번역이 없으면 씬에 작성된 원래 문자열을 그대로 둔다 — 화면이 비거나 키가 노출되지 않도록.
/// </summary>
public class TextUpdater : MonoBehaviour
{
    [Tooltip("번역 키. 비워두면 'ui.<게임오브젝트 이름>'을 키로 사용한다")]
    public string key;

    // UI(TextMeshProUGUI)와 월드 공간(TextMeshPro) 텍스트를 모두 다루도록 상위 타입으로 받는다
    public TMP_Text thisText;

    string authoredText; // 번역이 없을 때 유지할 원래 문자열
    bool captured;

    void Awake()
    {
        CaptureAuthoredText();
    }

    void CaptureAuthoredText()
    {
        if (captured) return;
        if (thisText == null) thisText = GetComponent<TMP_Text>();
        authoredText = thisText != null ? thisText.text : "";
        captured = true;
    }

    void OnEnable()
    {
        CaptureAuthoredText();
        M_LanguageManager.languageChangedCallback += LanguageChanged;
        LanguageChanged();
    }

    void OnDisable()
    {
        M_LanguageManager.languageChangedCallback -= LanguageChanged;
    }

    public void LanguageChanged()
    {
        if (thisText == null) thisText = GetComponent<TMP_Text>();
        if (thisText == null) return;

        string lookupKey = string.IsNullOrEmpty(key) ? LocKey.Ui(gameObject.name) : key;
        thisText.text = M_LanguageManager.Get(lookupKey, authoredText);

        if (M_LanguageManager.currnetFont != null)
            thisText.font = M_LanguageManager.currnetFont;
    }
}
