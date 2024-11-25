using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckBookUI : SingletonD<DeckBookUI>
{
    public delegate void OnChangeDeckBookOpenState(bool isOpen);
    public OnChangeDeckBookOpenState onChangeDeckBookOpenState;

    public GameObject dekcBookMenu;
    public GameObject cellPrefab;
    public Button buttonCloseDeckBook;
    public GameObject buttonCloseLight;
    public List<Button> tabButtons = new List<Button>();
    public List<TextMeshProUGUI> tabTexts = new List<TextMeshProUGUI>();
    public List<GameObject> tabFrames = new List<GameObject>();
    public int currentTabIndex;


    void Start()
    {
        DontDestroyOnLoad(gameObject);
        buttonCloseDeckBook.onClick.AddListener(() => HandleCloseDeckBook());
        for(int i=0; i<tabButtons.Count; i++){
            int buttonIndex = i;
            tabButtons[i].onClick.AddListener(() => ShowTab(buttonIndex));
        }
    }

    public void HandleCloseDeckBook()
    {
        dekcBookMenu.SetActive(false);
        onChangeDeckBookOpenState?.Invoke(false);
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    public void HandleOpenDeckBook()
    {
        dekcBookMenu.SetActive(true);
        onChangeDeckBookOpenState?.Invoke(true);
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    public void ShowTab(int index)
    {
        tabFrames[index].SetActive(true);
        tabButtons[index].image.color = new Color32(255, 255, 255, 255);
        tabTexts[index].color = new Color32(255, 255, 255, 255);
        currentTabIndex = index;
        HideOtherTabs(index);
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    public void HideOtherTabs(int index)
    {
        for(int i=0; i<tabButtons.Count; i++){
            if(i != index){
                tabButtons[i].image.color = new Color32(255, 255, 255, 70);
                tabTexts[i].color = new Color32(255, 255, 255, 70);
                tabFrames[i].SetActive(false);
            }
        }
    }

    public void OnPointerEnterCloseDeckBookButton()
    {
        buttonCloseLight.SetActive(true);
    }

    public void OnPointerExitCloseDeckBookButton()
    {
        buttonCloseLight.SetActive(false);
    }
}
