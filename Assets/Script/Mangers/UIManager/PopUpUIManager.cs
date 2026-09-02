using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;
using DG.Tweening;


// 팝업 창 관리 — 전투 보상 / 전초기지(기도·회복) / 아이템 상점 / 게임오버.
// 카드 팝업(덱 목록 3종·패 제거·덱 드로우·카드 강화/제거/선택·카드 상점 메르크리우스)은 카드 시스템 제거로 삭제됨 (2026-09-01).
public class PopUpUIManager : SingletonD<PopUpUIManager>
{
    // BattleResultPopUp Delegate
    public delegate void OnChangeBattleResultPopUpShow();
    public OnChangeBattleResultPopUpShow onChangeBattleResultPopUpShow;
    public delegate void OnChangeBattleResultPopUpHide();
    public OnChangeBattleResultPopUpHide onChangeBattleResultPopUpHide;


    // ItemShop PopUp Delegate
    public delegate void OnItemShopPopUpShow();
    public OnItemShopPopUpShow onItemShopPopUpShow;
    public delegate void OnItemShopPopUpHide();
    public OnItemShopPopUpHide onItemShopPopUpHide;


    // Camp PopUp Delegate
    public delegate void OnCampPopUpShow(CampAction campAction);
    public OnCampPopUpShow onCampPopUpShow;
    public delegate void OnCampPopUpHide();
    public OnCampPopUpHide onCampPopUpHide;

    [Header("팝업 활성화 상태값")]
    public bool isBattleResultPopUpOpen = false;
    public bool isGameoverPopUpOpen = false;
    public bool isCampPopUpOpen = false;
    public bool isItemShopPopUpOpen = false;

    [Header("팝업 UI 오브젝트")]
    public List<GameObject> popUpList = new List<GameObject>();
    public GameObject battleResultPopUp; // 전투 보상 팝업
    public GameObject gameOverPopUp; // 게임 오버 팝업
    public GameObject campPopUp; // 전초 기지 팝업
    public GameObject itemShopPopUp; // 아이템 상점 팝업

    [Header("보상 목록 아이템 프리팹")]
    public GameObject RewardListItemPrefab;


    private void Start()
    {
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.persistentComponents.Add(gameObject.name, gameObject); // DDOL 관리 컴포넌트에 등록
    }

    #region 전투보상 팝업
        // 전투보상 팝업창 활성화
        public void HandleShowBattleResultPopUp()
        {
            isBattleResultPopUpOpen = true;
            battleResultPopUp.SetActive(true);
            onChangeBattleResultPopUpShow?.Invoke();
        }

        // 전투보상 팝업창 비활성화
        public void HandleHideBattleResultPopUp()
        {
            isBattleResultPopUpOpen = false;
            onChangeBattleResultPopUpHide?.Invoke();
        }
    #endregion

    #region 전초기지 팝업
        // 전초기지 팝업 활성화
        public void HandleCampPopUpShow(CampAction campAction)
        {
            isCampPopUpOpen = true;
            campPopUp.SetActive(true);
            onCampPopUpShow?.Invoke(campAction);
        }

        // 전초기지 팝업 비활성화
        public void HandleCampPopUpHide()
        {
            isCampPopUpOpen = false;
            onCampPopUpHide?.Invoke();
        }
    #endregion

    #region 아이템 상점 팝업
        // 아이템 상점 팝업 활성화/비활성화
        public void HandleItemShopPopUp(bool isPopUp)
        {
            isItemShopPopUpOpen = isPopUp;
            if(isPopUp){
                itemShopPopUp.SetActive(true);
                onItemShopPopUpShow?.Invoke();
            }else{
                onItemShopPopUpHide?.Invoke();
            }
        }
    #endregion

    #region 게임오버 팝업
        // 게임오버 팝업 활성화
        public void HandleShowGameOverPopUp()
        {
            isGameoverPopUpOpen = true;
            gameOverPopUp.SetActive(true);
            gameOverPopUp.GetComponent<CanvasGroup>().DOFade(1.0f, 0.5f);
        }

        // 게임오버 팝업 비활성화
        public void HandleHideGameOverPopUp()
        {
            isGameoverPopUpOpen = false;
            gameOverPopUp.GetComponent<CanvasGroup>().DOFade(0.0f, 0.5f).OnComplete(() => {
                gameOverPopUp.SetActive(false);
                // 호스트로 시작한 경우(싱글플레이 포함) StopClient만 하면 서버가 살아남아 mode가 ServerOnly로 남는다.
                // 그 상태에서는 StartHost가 "이미 시작됨" 경고만 남기고 아무 일도 하지 않아 싱글플레이 버튼이 죽는다.
                // RoomUI.HandleBackToMainScene / M_NetworkRoomManager의 메인 복귀와 동일하게 서버까지 내린다.
                UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
                NetworkServer.Shutdown();
                NetworkClient.Disconnect();
                M_SteamManager.LeaveLobby();
            });
            AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
            M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        }
    #endregion
}
