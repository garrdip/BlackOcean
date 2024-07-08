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
    public Image background;
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
        background.alphaHitTestMinimumThreshold = 0.1f;
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

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape)){
            HandShowOptionPopUp(true);
        }
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
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    private void HandleClickOkButton()
    {
        HandShowOptionPopUp(false);
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    private void HandleBgmVolumeChange(float value)
    {
        M_SoundManager.instance.MusicVolume = value;
        if(value > 0){
            bgmToggle.isOn = false;
        }
    }

    private void HandleBgmToggleChanage(bool isOn)
    {
        M_SoundManager.instance.IsMusicOn = !isOn;
        if(isOn){
            bgmVolumeSlider.value = 0f;
        }
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    private void HandleVoiceVolumeChange(float value)
    {
        M_SoundManager.instance.VoiceVolume = value;
        if(value > 0){
            voiceToggle.isOn = false;
        }
    }

    private void HandleVoiceToggleChanage(bool isOn)
    {
        M_SoundManager.instance.IsVoiceOn = !isOn;
        if(isOn){
            voiceVolumeSlider.value = 0f;
        }
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }


    private void HandleSfxVolumeChange(float value)
    {
        M_SoundManager.instance.SoundVolume = value;
        if(value > 0){
            sfxToggle.isOn = false;
        }
    }

    private void HandleSfxToggleChanage(bool isOn)
    {
        M_SoundManager.instance.IsSoundOn = !isOn;
        if(isOn){
            sfxVolumeSlider.value = 0f;
        }
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    private void HandleChangeLanguageDropdown(TMP_Dropdown select)
    {
        string selectLanguage = select.options[select.value].text;
        PlayerPrefs.SetString(currentLanguage, selectLanguage);
        Debug.Log("언어 변경 : " + selectLanguage);
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
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