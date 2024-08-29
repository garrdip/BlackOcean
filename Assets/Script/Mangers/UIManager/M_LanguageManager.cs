using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class M_LanguageManager : SingletonD<M_LanguageManager>
{
    public static Dictionary<string,string> currentLanguage;
    public static TMP_FontAsset currnetFont;

    public delegate void LanguageChanged();
    public static LanguageChanged languageChangedCallback;
    public static bool isLanguageLoadDone = false;

    public void ApplyChangedLanguage()
    {
        languageChangedCallback();
    }

}
