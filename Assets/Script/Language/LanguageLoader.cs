using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LanguageLoader : MonoBehaviour
{
    public List<(string,string)> languageList = new List<(string,string)>();
    public TMP_Dropdown dropdown;

    void Start()
    {
        string[] fileNames = Directory.GetFiles(Directory.GetCurrentDirectory() + "/Language");
        dropdown.options.Clear();
        foreach(string languageFile in fileNames)
        {
            Dictionary<string,string> data = CSVReader.ReadToDictionary(languageFile);
            if( new List<string>(data.Keys)[0] == "Language")
            {
                Debug.Log("Language File!");
                languageList.Add((data["Language"],languageFile));
                dropdown.options.Add(new TMP_Dropdown.OptionData(data["Language"]));
            }
        }
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        SetLanguage(languageList[0].Item1);
    }
    private void OnDropdownValueChanged(int optionIndex)
    {
        string optionText = dropdown.options[optionIndex].text;
        SetLanguage(optionText);
    }

    void SetLanguage(string language)
    {
        Dictionary<string,string> data = CSVReader.ReadToDictionary(languageList.Find(lang => lang.Item1 == language).Item2);
        FontLoader(data["Font"]);

        M_LanguageManager.currentLanguage = data;
    }

    void FontLoader(string fontFileName)
    {
        string fontPath = Directory.GetCurrentDirectory() + "/Language/" + fontFileName;
        Debug.Log(fontPath);
        Font legacyFont = new Font(fontPath);

        M_LanguageManager.currnetFont = TMP_FontAsset.CreateFontAsset(legacyFont);
    }
}

