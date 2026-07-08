using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Mirror;
using ProjectD;
using DG.Tweening;

public class ReadyButtonOnRoom : ButtonBase, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public GameObject readyBaseL1;
    public GameObject readyBaseL2;
    public GameObject readyBaseL3;
    public GameObject readySBase;
    public GameObject readyS1;
    public GameObject readyS2;
    public TextMeshProUGUI textReady;

    void Start()
    {
        GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        readyS1.GetComponent<RectTransform>().DOKill();
        readyS2.GetComponent<RectTransform>().DOKill();
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        HandleRadeyState();
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        readyBaseL1.SetActive(true);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        readyBaseL1.SetActive(false);
    }

    // 레디 상태 제어 
    public void HandleRadeyState()
    {
        RoomPlayer roomPlayer = NetworkClient.localPlayer.gameObject.GetComponent<RoomPlayer>();
        if(roomPlayer.character != Character.NONE){
            if(roomPlayer.isServer){ //서버케이스
                if(textReady.text == "START" )ChangeGameScene();
            }else{ //클라이언트만 레디
                roomPlayer.isReady = !roomPlayer.isReady;
            }
            SetReadyButtonViewByReadyState(roomPlayer.isReady);
            RoomUI.instance.ChangeSwapButtonsIconState();
            AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "game_start");
            M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        }
    }

    // 레디버튼 상태 변경
    public void SetReadyButtonViewByReadyState(bool isReady)
    {
        readyBaseL1.SetActive(isReady);
        readyBaseL2.SetActive(isReady);
        readyBaseL3.SetActive(isReady);
        readySBase.SetActive(isReady);
        readyS1.SetActive(isReady);
        readyS2.SetActive(isReady);
        if(isReady){
            SetReadyCircleRotateInfinite();
        }else{
            readyS1.GetComponent<RectTransform>().DOKill();
            readyS2.GetComponent<RectTransform>().DOKill();
        }
    }

    // 레디버튼 원형 컴포넌트 회전루프 트위닝
    private void SetReadyCircleRotateInfinite()
    {
        if(!DOTween.IsTweening(readyS1.GetComponent<RectTransform>()) && !DOTween.IsTweening(readyS2.GetComponent<RectTransform>())){
            readyS1.GetComponent<RectTransform>().DORotate(new Vector3(0, 0, 360), 4.5f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetRelative(true)
                .SetLoops(-1, LoopType.Yoyo);
            readyS2.GetComponent<RectTransform>().DORotate(new Vector3(0, 0, -360), 4.5f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetRelative(true)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    // 게임씬 이동
    public void ChangeGameScene()
    {
        M_LoadingManager.instance.SetLoadingScreen(true);
        M_LoadingManager.instance.state = LOADING_STATE.SCENE_LOADING;
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        M_NetworkRoomManager.ServerChangeScene(M_NetworkRoomManager.GameplayScene);
    }
}
