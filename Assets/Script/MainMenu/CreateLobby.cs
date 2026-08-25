using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectD;
using TMPro;

public class CreateLobby : MonoBehaviour
{
    public TextMeshProUGUI lobbyName;
    public TextMeshProUGUI password;
    public Button btnCreateRoom;

    bool continueFromSave; // '이어서 하기' 선택 상태 (저장 파일이 있을 때만 표시)

    void Awake()
    {
        btnCreateRoom.onClick.AddListener(()=>HandleCreateRoom());
        continueFromSave = GameSaveService.HasSaveFile(); // 저장이 있으면 기본값 ON
    }

    void HandleCreateRoom()
    {
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        // '이어서 하기' — 호스트가 저장 파일 소유. 프로필 복원은 M_HubManager.OnStartServer(TryLoad) → GenerateGamePlayer(FindProfile)가 소비
        GameSaveService.pendingLoad = continueFromSave && GameSaveService.HasSaveFile();
        M_SteamManager.instance.HostLobby(lobbyName.text,StringUtils.RemoveZWSP(password.text));
    }

    // 임시 UI — 정식 메뉴 개편(싱글/멀티 x 처음부터/이어서) 전까지 방 만들기 화면에 토글로 노출
    void OnGUI()
    {
        if(!GameSaveService.HasSaveFile()) return;
        var rect = new Rect(20f, Screen.height - 60f, 280f, 40f);
        if(GUI.Button(rect, continueFromSave ? "이어서 하기: ON (저장된 모험 계속)" : "이어서 하기: OFF (처음부터)"))
            continueFromSave = !continueFromSave;
    }
}
