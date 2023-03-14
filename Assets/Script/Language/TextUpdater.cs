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
        StartCoroutine(nameof(InitialLanguageUpdate));
    }

    public void LanguageChanged()
    {
        thisText = GetComponent<TextMeshProUGUI>();
        thisText.text = M_LanguageManager.currentLanguage[thisText.name];
        thisText.font = M_LanguageManager.currnetFont;
    }

    IEnumerator InitialLanguageUpdate()
    {
        while(M_LanguageManager.isLanguageLoadDone == false)
        {
            yield return new WaitForSeconds(0.1f);
        }
        LanguageChanged();
    }
}
