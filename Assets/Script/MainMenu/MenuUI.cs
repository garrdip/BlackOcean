
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

// 메인 메뉴 — 싱글 플레이를 누르면 메뉴 전체가 "처음부터 시작 / 이어하기" 두 선택지로 바뀐다 (RPG_CONVERSION_DESIGN 메뉴 변경).
// 처음부터 = 기존 싱글 시작 루틴(호스트 시작), 이어하기 = 저장 파일 로드 예약(GameSaveService.pendingLoad) 후 같은 루틴. Esc로 메인 메뉴 복귀
// 세이브 슬롯(2026-08-31): 둘 다 SaveSlotPanel로 슬롯(3개)을 먼저 고른다 — 처음부터 = BeginNewGame(슬롯), 이어하기 = BeginContinue(슬롯)
public class MenuUI : MonoBehaviour
{
    public GameObject multiplayCanvas;
    public GameObject menuCanvas;
    public GameObject debris;
    public GameObject logoGroup;

    public Button buttonSinglePlay;
    public Button buttonMultiPlay;
    public Button buttonDeckBook; // (구 덱북 — 카드 시스템 제거로 폐기, 버튼은 Start에서 숨긴다)
    public Button buttonSettings;
    public Button buttonQuit;

    bool singleModeOpen; // 싱글 플레이 하위 메뉴(처음부터 시작 / 이어하기) 표시 중 — 기존 버튼 2개를 라벨만 바꿔 재사용

    void Start()
    {
        buttonSinglePlay.onClick.AddListener(() => { if(singleModeOpen) StartSingle(false); else EnterSingleMode(); });
        buttonMultiPlay.onClick.AddListener(() => { if(singleModeOpen) StartSingle(true); else HandleMultiPlay(); });
        if(buttonDeckBook != null) buttonDeckBook.gameObject.SetActive(false);
        buttonSettings.onClick.AddListener(() => HandleSettings());
        buttonQuit.onClick.AddListener(() => HandleQuit());
    }

    void Update()
    {
        if(singleModeOpen && !SaveSlotPanel.IsOpen && Input.GetKeyDown(KeyCode.Escape)) ExitSingleMode(); // 하위 메뉴에서 Esc → 메인 메뉴 (슬롯 패널이 열려 있으면 패널이 Esc를 소비)
    }

    // ---------------------------------------------------------------- 싱글 플레이 하위 메뉴 ---------------------------------------------------------------- //

    // 싱글 플레이 클릭 → 메뉴 전체를 "처음부터 시작 / 이어하기"로 전환 (나머지 항목 숨김). 저장 파일이 없으면 이어하기 비활성
    void EnterSingleMode()
    {
        PlayClickSfx();
        singleModeOpen = true;
        SetButtonLabel(buttonSinglePlay, "ui.Main_New_Game", "처음부터 시작");
        SetButtonLabel(buttonMultiPlay, "ui.Main_Continue", "이어하기");
        buttonMultiPlay.interactable = GameSaveService.HasAnySaveFile(); // 슬롯 중 하나라도 저장이 있으면 이어하기 가능
        buttonSettings.gameObject.SetActive(false);
        buttonQuit.gameObject.SetActive(false);
    }

    void ExitSingleMode()
    {
        singleModeOpen = false;
        SetButtonLabel(buttonSinglePlay, "ui.Main_Single_Play", "싱글 플레이");
        SetButtonLabel(buttonMultiPlay, "ui.Main_Multi_Play", "멀티 플레이");
        buttonMultiPlay.interactable = true;
        buttonSettings.gameObject.SetActive(true);
        buttonQuit.gameObject.SetActive(true);
    }

    // 버튼 라벨 교체 — TextUpdater 키까지 바꿔 언어 변경 시에도 유지
    void SetButtonLabel(Button button, string key, string fallback)
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if(label == null) return;
        TextUpdater updater = label.GetComponent<TextUpdater>();
        if(updater != null) updater.key = key;
        label.text = M_LanguageManager.Get(key, fallback);
    }

    // 처음부터 시작(continueFromSave=false) / 이어하기(true) — 슬롯 패널에서 슬롯을 고른 뒤 둘 다 기존 싱글 시작 루틴(HandleSinglePlay)을 탄다.
    // 이어하기: 슬롯 로드 예약 → M_HubManager.OnStartServer(TryLoad) → GenerateGamePlayer(FindProfile)가 프로필 복원
    void StartSingle(bool continueFromSave)
    {
        PlayClickSfx();
        SaveSlotPanel.Open(continueFromSave ? SaveSlotPanel.Mode.Continue : SaveSlotPanel.Mode.NewGame, slot =>
        {
            if(continueFromSave){
                if(!GameSaveService.BeginContinue(slot)) return; // 파일이 사라졌으면 무시
            }else{
                GameSaveService.BeginNewGame(slot);
            }
            HandleSinglePlay();
        });
    }

    public void HandleSinglePlay()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        M_NetworkRoomManager.maxConnections = 1; // 방 최대 인원 1명으로 설정
        M_NetworkRoomManager.StartHost(); // 호스트로 시작
        PlayClickSfx();
    }

    public void HandleMultiPlay()
    {
        menuCanvas.SetActive(false);
        multiplayCanvas.SetActive(true);
        PlayClickSfx();
    }

    void PlayClickSfx()
    {
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    public void HandleSettings()
    {
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        OptionUIManager.instance.HandShowOptionPopUp(true);
    }

    public void HandleQuit()
    {
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        Application.Quit();
    }
}
