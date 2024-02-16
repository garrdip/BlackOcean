using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionUIManager : SingletonD<OptionUIManager>
{
    public GameObject optionPopUp;
    public Button buttonOption;
    public Slider bgmVolumeSlider;
    public Toggle bgmToggle;
    public Slider voiceVolumeSlider;
    public Toggle voiceToggle;
    public Slider sfxVolumeSlider;
    public Toggle sfxToggle;


    void Start()
    {
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

}
