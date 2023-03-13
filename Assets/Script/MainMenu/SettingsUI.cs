using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    public FullScreenMode fullScreenMode;
    public List<Resolution> resolutions = new List<Resolution>();

    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown languageDropdown;

    public Toggle toggleFullScreen;

    public Button buttonApply;

    public int resolutionNum;

    void Start()
    {
        initDropdown();
        buttonApply.onClick.AddListener(() => HandleClickApply());
        resolutionDropdown.onValueChanged.AddListener(DropdownValueChange);
        toggleFullScreen.onValueChanged.AddListener(HandleFullScreen);
        toggleFullScreen.isOn = Screen.fullScreenMode.Equals(FullScreenMode.FullScreenWindow) ? true : false;
    }

    // 현재 디스플레이에서 지원되는 해상도 목록 드롭다운에 초기화
    public void initDropdown()
    {
        int optionNum = 0;
        resolutions.AddRange(Screen.resolutions);
        resolutionDropdown.options.Clear();
        foreach(Resolution resolution in resolutions){
            if(Application.isEditor){
                resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(resolution.width + " X " + resolution.height ));
            }else{
                resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(resolution.width + " X " + resolution.height + " : "+ resolution.refreshRate + "hz" ));
            }
            if(resolution.width == Screen.width && resolution.height == Screen.height){
                resolutionDropdown.value = optionNum;
            }
            optionNum++;
        }
        resolutionDropdown.RefreshShownValue();
    }

    // 드롭다운 옵션 값 변경이벤트
    public void DropdownValueChange(int optionIndex)
    {
        resolutionNum = optionIndex;
    }

    // 옵션 변경 Apply버튼 이벤트
    public void HandleClickApply()
    {
        if(Application.isEditor){
            Screen.SetResolution(resolutions[resolutionNum].width, resolutions[resolutionNum].height, fullScreenMode);
        }else{
            Screen.SetResolution(resolutions[resolutionNum].width, resolutions[resolutionNum].height, fullScreenMode, resolutions[resolutionNum].refreshRate);
        }
    }

    // FullScreen 체크박스 토글 이벤트
    public void HandleFullScreen(bool isFull)
    {
        fullScreenMode = isFull ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
    }
}
