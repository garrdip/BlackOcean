using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class OptionUIManager : SingletonD<OptionUIManager>
{
    public delegate void OnChangeOptionPopUpShow(bool isActive);
    public OnChangeOptionPopUpShow onChangeOptionPopUpShow;
    public GameObject optionPopUp;
    public Image dimBacground;
    public Button buttonOption;
    public Button backButton;
    public GameObject backButtonLight;
    public Button okButton;
    public GameObject okButtonLight;
    public Slider bgmVolumeSlider;
    public Toggle bgmToggle;
    public Slider voiceVolumeSlider;
    public Toggle voiceToggle;
    public Slider sfxVolumeSlider;
    public Toggle sfxToggle;
    public bool isOptionPopUpActive = false;
    public TMP_Dropdown languageDropdown;
    public GameObject dropdownLight;
    private const string currentLanguage = "CurrentLanguage";


    void Start()
    {
        DontDestroyOnLoad(gameObject);
        onChangeOptionPopUpShow += OnChangeOptionPopUpActive; // 옵션 팝업 활성화 상태 변경 이벤트 수신
        SceneManager.activeSceneChanged += OnChangedActiveScene; // 씬 변경 이벤트 수신

        backButton.onClick.AddListener(HandleClickBackButton);
        okButton.onClick.AddListener(HandleClickOkButton);

        bgmVolumeSlider.value = M_SoundManager.instance.MusicVolume;
        bgmToggle.isOn = !M_SoundManager.instance.IsMusicOn;
        
        voiceVolumeSlider.value = M_SoundManager.instance.VoiceVolume;
        voiceToggle.isOn = !M_SoundManager.instance.IsVoiceOn;

        sfxVolumeSlider.value = M_SoundManager.instance.SoundVolume;
        sfxToggle.isOn = !M_SoundManager.instance.IsSoundOn;

        bgmVolumeSlider.onValueChanged.AddListener(HandleBgmVolumeChange);
        bgmToggle.onValueChanged.AddListener(HandleBgmToggleChanage);

        voiceVolumeSlider.onValueChanged.AddListener(HandleVoiceVolumeChange);
        voiceToggle.onValueChanged.AddListener(HandleVoiceToggleChanage);

        sfxVolumeSlider.onValueChanged.AddListener(HandleSfxVolumeChange);
        sfxToggle.onValueChanged.AddListener(HandleSfxToggleChanage);
        
        InitDropdownValue();
        languageDropdown.onValueChanged.AddListener(delegate{
            HandleChangeLanguageDropdown(languageDropdown);
        });
    }

    private void OnChangedActiveScene(Scene current, Scene next)
    {
       
        if(next.name.Equals("RoomScene") || next.name.Equals("GameScene") ){
            buttonOption.gameObject.SetActive(true);
        }else{
            buttonOption.gameObject.SetActive(false);
        }
    }

    private void InitDropdownValue()
    {
        string savedLanguage = PlayerPrefs.GetString(currentLanguage);
        int index = languageDropdown.options.FindIndex(option => option.text == savedLanguage);
        if(index != -1){
            languageDropdown.value = index;
        }
    }

    private void OnChangeOptionPopUpActive(bool isActive)
    {
        optionPopUp.SetActive(isActive);
    }

    public void HandShowOptionPopUp(bool isActive)
    {
        if(onChangeOptionPopUpShow != null){
            onChangeOptionPopUpShow.Invoke(isActive);
        }
        isOptionPopUpActive = isActive;
    }

    private void HandleClickBackButton()
    {
        HandShowOptionPopUp(false);
    }

    private void HandleClickOkButton()
    {
        HandShowOptionPopUp(false);
    }

    private void HandleBgmVolumeChange(float value)
    {
        M_SoundManager.instance.MusicVolume = value;
    }

    private void HandleBgmToggleChanage(bool isOn)
    {
        M_SoundManager.instance.IsMusicOn = !isOn;
    }

    private void HandleVoiceVolumeChange(float value)
    {
        M_SoundManager.instance.VoiceVolume = value;
    }

    private void HandleVoiceToggleChanage(bool isOn)
    {
        M_SoundManager.instance.IsVoiceOn = !isOn;
    }


    private void HandleSfxVolumeChange(float value)
    {
        M_SoundManager.instance.SoundVolume = value;
    }

    private void HandleSfxToggleChanage(bool isOn)
    {
        M_SoundManager.instance.IsSoundOn = !isOn;
    }

    private void HandleChangeLanguageDropdown(TMP_Dropdown select)
    {
        string selectLanguage = select.options[select.value].text;
        PlayerPrefs.SetString(currentLanguage, selectLanguage);
        Debug.Log("언어 변경 : " + selectLanguage);
    }

    // -------- 이벤트 트리거에 할당되어있는 함수들 --------- //

    public void OnPointerEnterBackButton()
    {
        backButtonLight.SetActive(true);
    }

    public void OnPointerExitBackButton()
    {
        backButtonLight.SetActive(false);
    }

    public void OnPointerEnterOkButton()
    {
        okButtonLight.SetActive(true);
    }

    public void OnPointerExitOkButton()
    {
        okButtonLight.SetActive(false);
    }

    public void OnPointerEnterDropDown()
    {
        dropdownLight.SetActive(true);
    }

    public void OnPointerExitDropDown()
    {
        dropdownLight.SetActive(false);
    }

    public void OnPointerClickDimBackground()
    {
        HandShowOptionPopUp(false);
    }

    // ----------------------------------------------------- //
}