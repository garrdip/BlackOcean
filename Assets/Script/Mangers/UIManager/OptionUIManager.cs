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

        SyncSoundOptionUI();

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
            isOptionPopUpActive = !isOptionPopUpActive;
            HandShowOptionPopUp(isOptionPopUpActive);
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
        // 팝업을 열 때마다 실제 사운드 상태로 UI를 다시 맞춘다.
        // Start()에서 1회만 맞추면 이후 코드 경로가 볼륨을 바꿨을 때 UI와 실제 소리가 어긋난다.
        if(isActive){
            SyncSoundOptionUI();
        }else{
            // 닫을 때 PlayerPrefs를 디스크에 flush (변경 자체는 세터에서 이미 기록됨)
            M_SoundManager sound = M_SoundManager.instance;
            if(sound != null) sound.SaveAllPreferences();
        }
        optionPopUp.SetActive(isActive);
    }

    /// <summary>
    /// 사운드 매니저의 현재 값으로 슬라이더/토글을 갱신한다.
    /// 갱신이 다시 onValueChanged로 되먹임되지 않도록 알림 없는 세터를 사용한다.
    /// </summary>
    private void SyncSoundOptionUI()
    {
        M_SoundManager sound = M_SoundManager.instance;
        if(sound == null) return;

        bgmVolumeSlider.SetValueWithoutNotify(sound.MusicVolume);
        bgmToggle.SetIsOnWithoutNotify(!sound.IsMusicOn);

        voiceVolumeSlider.SetValueWithoutNotify(sound.VoiceVolume);
        voiceToggle.SetIsOnWithoutNotify(!sound.IsVoiceOn);

        sfxVolumeSlider.SetValueWithoutNotify(sound.SoundVolume);
        sfxToggle.SetIsOnWithoutNotify(!sound.IsSoundOn);
    }

    public void HandShowOptionPopUp(bool isActive)
    {
        onChangeOptionPopUpShow?.Invoke(isActive);
        isOptionPopUpActive = isActive;
    }

    private void HandleClickBackButton()
    {
        HandShowOptionPopUp(false);
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    private void HandleClickOkButton()
    {
        HandShowOptionPopUp(false);
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
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
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
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
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
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
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    private void HandleChangeLanguageDropdown(TMP_Dropdown select)
    {
        string selectLanguage = select.options[select.value].text;
        PlayerPrefs.SetString(currentLanguage, selectLanguage);
        Debug.Log("언어 변경 : " + selectLanguage);
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
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