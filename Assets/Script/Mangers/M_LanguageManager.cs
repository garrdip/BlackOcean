using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class M_LanguageManager : MonoBehaviour
{
    public static Dictionary<string,string> currentLanguage;
    public static TMP_FontAsset currnetFont;

    public delegate void LanguageChanged();
    public static LanguageChanged languageChangedCallback;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    public void ApplyChangedLanguage()
    {
        languageChangedCallback();
    }

}
