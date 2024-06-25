using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Mirror;
using DG.Tweening;
using Steamworks;

public class MapPlayer : NetworkBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("맵 플레이어 뷰 컴포넌트 레이아웃")]
    public GameObject swapRequestLayout;
    public GameObject p_INFO_Base;
    public GameObject p_INFO_B;
    public GameObject p_INFO_L;
    public GameObject p_INFO_L_L;
    public GameObject p_INFO_M_L;
    public GameObject p_INFO_M_L_L;
    public GameObject p_INFO_C_B;
    public GameObject p_INFO_C_B_L;
    public GameObject hoverLine;

    [Header("SwapRequestLayout")]
    public Button buttonSwapAccept;
    public Button buttonSwapReject;

    [Header("DeckInfoLayout")]
    public Button buttonDecKInfo;

    [Header("Player Info Layout")]
    public TextMeshProUGUI textOrder;
    public TextMeshProUGUI steamDisplayName;

    // Dotween 참조값
    public Sequence sequence;
    private const float upPositionY = -300f;
    private const float downPositionY = -350f;

    [SyncVar]
    public GamePlayer gamePlayer;

    [SyncVar]
    public int oldIndex;
    
    [SyncVar]
    public int newIndex;


    void Start()
    {
        buttonSwapAccept.onClick.AddListener(() => HandleClickButtonSwapAccept()); // 교환 수락 버튼
        buttonSwapReject.onClick.AddListener(() => HandleClickButtonSwapReject()); // 교환 거절 버튼
        buttonDecKInfo.gameObject.SetActive(isOwned);
        buttonDecKInfo.onClick.AddListener(() => HandleOpenDeckInfoPopUp());
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // 서버에서 맵플레이어 생성되면 참조된 게임플레이어의 오더값과 netId값을 전달하여 오더값에 해당하는 인덱스에 netId 추가(룸에서 설정된 오더값은 게임플레이어가 갖고 있음)
        M_MapManager.instance.AddMapPlayer((int)gamePlayer.selectOrder, gamePlayer.netId);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        gamePlayer.objectOwner.onChangeReady += OnChangeReadyState;
        gamePlayer.onChangePlayerOrder += OnChangePlayerOrder;
        InitMapPlayerView(gamePlayer.selectOrder);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        gamePlayer.onChangePlayerOrder -= OnChangePlayerOrder;
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        M_MapManager.instance.ownedGamePlayer = gamePlayer.netId;
    }

    void OnDestroy()
    {
        GetComponent<CanvasGroup>().DOKill();
        GetComponent<RectTransform>().DOKill();
        sequence.Kill();
    }

    // 스왑 승인 버튼 클릭
    private void HandleClickButtonSwapAccept()
    {
        if(isOwned){
            swapRequestLayout.SetActive(false);
            CmdSwapAccept(oldIndex, newIndex);
        }
    }

    // 스왑 거절 버튼 클릭
    private void HandleClickButtonSwapReject()
    {
        if(isOwned){
            swapRequestLayout.SetActive(false);
            CmdSwapReject(oldIndex);
        }
    }

    private void HandleOpenDeckInfoPopUp()
    {
        MapUI.instance.CreatDeckInfoPopUpItem(gamePlayer.GetComponent<GamePlayerDeck>());
        MapUI.instance.deckInfoPopUp.SetActive(true);
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        hoverLine.GetComponent<Image>().color = Color.red;
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        hoverLine.GetComponent<Image>().color = Color.white;
    }

    // ----------------------------------------------------------------- Command Method --------------------------------------------------------------------------------//

       // 교환요청 수락 커맨드
    [Command]
    public void CmdSwapAccept(int oldIndex, int newIndex)
    {
        uint ownedNetId = M_TurnManager.instance.playerOrder[oldIndex];
        uint targetNetID = M_TurnManager.instance.playerOrder[newIndex];
        if( ownedNetId != 0 && NetworkServer.spawned.TryGetValue(ownedNetId, out NetworkIdentity ownedNetIdentity) && 
            targetNetID != 0 && NetworkServer.spawned.TryGetValue(targetNetID, out NetworkIdentity targetNetworkIdentity)){
            
            GamePlayer ownedGamePlayer = ownedNetIdentity.GetComponent<GamePlayer>();
            GamePlayer targetGamePlayer = targetNetworkIdentity.GetComponent<GamePlayer>();
            uint ownedMapPlyerNetId = ownedGamePlayer.mapPlayerNetId;
            uint targetMapPlayerNetId = targetGamePlayer.mapPlayerNetId;
            
            if( ownedMapPlyerNetId != 0 && NetworkServer.spawned.TryGetValue(ownedMapPlyerNetId, out NetworkIdentity ownedMapPlayerNetIdentity) && 
                targetMapPlayerNetId != 0 && NetworkServer.spawned.TryGetValue(targetMapPlayerNetId, out NetworkIdentity targetMapPlayerNetIdentity)){
                
                // 스왑로직 수행
                M_MapManager.instance.CmdSwapMapPlayer(oldIndex, newIndex);

                // 스왑요청 송신지에게 메시지 전달
                MapPlayer ownedMapPlayer = ownedMapPlayerNetIdentity.GetComponent<MapPlayer>();
                TargetResponseSwapAccept(ownedMapPlayer.GetComponent<NetworkIdentity>().connectionToClient);
                
                // 스왑요청 수신자에게 메시지 전달
                MapPlayer targetMapPlayer = targetMapPlayerNetIdentity.GetComponent<MapPlayer>();
                TargetResponseSwapAccept(targetMapPlayer.GetComponent<NetworkIdentity>().connectionToClient);
            }
        }
    }

    // 교환요청 거절 커맨드
    [Command]
    public void CmdSwapReject(int targetIndex)
    {
        // 교환요청자에게 요청이 거절되었음을 알리는 TargetRpc 이벤트 전달
        uint targetNetID = M_TurnManager.instance.playerOrder[targetIndex];
        if(targetNetID != 0 && NetworkServer.spawned.TryGetValue(targetNetID, out NetworkIdentity networkIdentity)){
            GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
            if(gamePlayer.mapPlayerNetId != 0 && NetworkServer.spawned.TryGetValue(gamePlayer.mapPlayerNetId, out NetworkIdentity mapPlayerNetIdentity)){
                MapPlayer mapPlayer = mapPlayerNetIdentity.GetComponent<MapPlayer>();
                TargetResponseSwapReject(mapPlayer.GetComponent<NetworkIdentity>().connectionToClient);
            }
        }  
    }

    // ----------------------------------------------------------------- Rpc Method --------------------------------------------------------------------------------//

    // 교환요청 응답 : 승인, 거절 버튼 UI 활성화
    [TargetRpc]
    public void TargetResponseSwap(NetworkConnectionToClient target)
    {
        swapRequestLayout.SetActive(true);
    }

    // 교환요청 수락 응답 : 교환요청자와 상대방 모두에게 수락 메시지 표시
    [TargetRpc]
    public void TargetResponseSwapAccept(NetworkConnectionToClient target)
    {
        M_MessageManager.instance
            .MakeToast()
            .Position(ToastPosition.Bottom)
            .MessageBoxColor(Color.green)
            .TextColor(Color.white)
            .Text("위치 교환 요청이 수락 되었습니다.")
            .Show();
    }

    // 교환요청 거절 응답 : 교환요청자에게만 거절 메시지 표시
    [TargetRpc]
    public void TargetResponseSwapReject(NetworkConnectionToClient target)
    {
        M_MessageManager.instance
            .MakeToast()
            .Position(ToastPosition.Bottom)
            .MessageBoxColor(Color.red)
            .TextColor(Color.white)
            .Text("위치 교환 요청이 거절 되었습니다.")
            .Show();
    }

    // ----------------------------------------------------------------- View Update Method --------------------------------------------------------------------------------//

    // 맵플레이어 뷰 컴포넌트 초기화
    public void InitMapPlayerView(int index)
    {
        ChangeMapPlayerViewByOrder(index);
        p_INFO_M_L.SetActive(isOwned);
        steamDisplayName.text = gamePlayer.objectOwner.steamPersonaName;
        SetOrderTextByPlayerOrder(index);
        MapUI.instance.ChangeSwapButtonsState(index);
        MapUI.instance.ChangeSwapButtonsIconState();
        if(isOwned){
            SwapButtonOnMap swapButtonOnMap = MapUI.instance.swapButtons[index].GetComponent<SwapButtonOnMap>();
            swapButtonOnMap.t_M_Icon.SetActive(true);
            swapButtonOnMap.t_Chan_Icon.SetActive(false);
            swapButtonOnMap.t_Ready_Icon.SetActive(false);
        }
    }

    // 맵플레이어 뷰 컴포넌트 변경사항 업데이트
    public void ChangeMapPlayerViewByOrder(int index)
    {
        Tween fadeInTween = GetComponent<CanvasGroup>().DOFade(0.0f, 0.5f).OnComplete(() => { SetMapPlayerParent(index); });
        Tween upTween = GetComponent<RectTransform>().DOLocalMoveY(upPositionY, 0.5f).OnComplete(() => { SetOrderTextByPlayerOrder(index); });
        Tween downTween = GetComponent<RectTransform>().DOLocalMoveY(downPositionY, 0.5f);
        Tween fadeOutTween = GetComponent<CanvasGroup>().DOFade(1.0f, 0.5f);
        sequence = DOTween.Sequence();
        sequence.Append(fadeInTween);
        sequence.Join(upTween);
        sequence.Append(downTween);
        sequence.Join(fadeOutTween);
    }

    // 맵플레이어 부모오브젝트 설정
    private void SetMapPlayerParent(int index)
    {
        transform.SetParent(MapUI.instance.topIcons[index].transform);
        transform.localScale = new Vector3(1f, 1f, 1f);
        transform.localPosition = new Vector3(0f, upPositionY, 0f);
        transform.SetAsFirstSibling();
    }

    // 맵플레이어 오더 텍스트 변경
    public void SetOrderTextByPlayerOrder(int playOrder)
    {
        switch(playOrder){
            case 0:
                textOrder.text = "후열";
                break;
            case 1:
                textOrder.text = "중열";
                break;
            case 2:
                textOrder.text = "전열";
                break;     
        }  
    }
 
    // 맵플레이어 레디 변경 수신
    public void OnChangeReadyState(bool isReady)
    {
        MapUI.instance.ChangeSwapButtonsIconState();
    }

    // 맵플레이어 오더 변경 수신
    public void OnChangePlayerOrder(int order)
    {
        ChangeMapPlayerViewByOrder(order);
        MapUI.instance.ChangeSwapButtonsIconState();
        MapUI.instance.ChangeSwapButtonsState(order);
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("choose_position"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }
}
