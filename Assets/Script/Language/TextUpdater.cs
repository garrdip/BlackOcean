using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextUpdater : MonoBehaviour
{
    public TextMeshProUGUI thisText;
    void Start()
    {
        thisText = GetComponent<TextMeshProUGUI>();       
        M_LanguageManager.languageChangedCallback += LanguageChanged;
    }

    void LanguageChanged()
    {
        thisText = GetComponent<TextMeshProUGUI>();
        thisText.text = M_LanguageManager.currentLanguage[thisText.name];
        thisText.font = M_LanguageManager.currnetFont;
    }

}
