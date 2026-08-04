using UnityEngine;
using TMPro;

/// <summary>
/// 텍스트 내용은 그대로 두고 폰트만 현재 언어 폰트로 바꾸는 컴포넌트 (채팅 메시지처럼 내용이 런타임에 정해지는 텍스트용).
/// Locales.csv에 폰트가 지정되지 않은 언어에서는 아무것도 하지 않는다 — 기존 폰트를 유지한다.
/// </summary>
public class FontUpdater : MonoBehaviour
{
    public TMP_Text thisText;

    void OnEnable()
    {
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

        if (M_LanguageManager.currnetFont != null)
            thisText.font = M_LanguageManager.currnetFont;
    }
}
