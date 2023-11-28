using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using DG.Tweening;
using Steamworks;

public class MapPlayer : NetworkBehaviour
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

    [Header("SwapRequestLayout")]
    public Button buttonSwapAccept;
    public Button buttonSwapReject;

    [Header("Player Info Layout")]
    public TextMeshProUGUI textOrder;
    public TextMeshProUGUI steamDisplayName;

    // Dotween 참조값
    public Sequence sequence;
    private float upPositionY;
    private float downPositionY;

    [SyncVar]
    public GamePlayer gamePlayer;

    [SyncVar]
    public int oldIndex;
    
    [SyncVar]
    public int newIndex;


    void Start()
    {
        upPositionY = -300f;
        downPositionY = -350f;
        InitMapPlayerView();
        if(isOwned){
            buttonSwapAccept.onClick.AddListener(() => HandleClickButtonSwapAccept()); // 교환 수락 버튼
            buttonSwapReject.onClick.AddListener(() => HandleClickButtonSwapReject()); // 교환 거절 버튼
        }
        
    }

    void OnDestroy()
    {
        GetComponent<CanvasGroup>().DOKill();
        GetComponent<RectTransform>().DOKill();
        sequence.Kill();
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        M_MapManager.instance.ownedMapPlayer = GetComponent<NetworkIdentity>().netId;
    }

    // 스왑 승인 버튼 클릭
    private void HandleClickButtonSwapAccept()
    {
        swapRequestLayout.SetActive(false);
        CmdSwapAccept(oldIndex, newIndex);
    }

    // 스왑 거절 버튼 클릭
    private void HandleClickButtonSwapReject()
    {
        swapRequestLayout.SetActive(false);
        CmdSwapReject(oldIndex);
    }

    // ----------------------------------------------------------------- Command Method --------------------------------------------------------------------------------//

    // 교환요청 수락 커맨드
    [Command]
    public void CmdSwapAccept(int oldIndex, int newIndex)
    {
        // 스왑로직 수행
        M_MapManager.instance.CmdSwapMapPlayer(oldIndex, newIndex);

        // 교환요청자와 상대방 모두에게 요청이 수락되었음을 알리는 TargetRpc 이벤트 전달
        uint ownedNetId = M_MapManager.instance.mapPlayers[oldIndex];
        uint targetNetID = M_MapManager.instance.mapPlayers[newIndex];
        MapPlayer ownedMapPlayer = NetworkServer.spawned[ownedNetId].GetComponent<MapPlayer>();
        MapPlayer targetMapPlayer = NetworkServer.spawned[targetNetID].GetComponent<MapPlayer>();
        TargetResponseSwapAccept(ownedMapPlayer.GetComponent<NetworkIdentity>().connectionToClient);
        TargetResponseSwapAccept(targetMapPlayer.GetComponent<NetworkIdentity>().connectionToClient);
    }

    // 교환요청 거절 커맨드
    [Command]
    public void CmdSwapReject(int targetIndex)
    {
        // 교환요청자에게 요청이 거절되었음을 알리는 TargetRpc 이벤트 전달
        uint targetNetID = M_MapManager.instance.mapPlayers[targetIndex];
        MapPlayer mapPlayer = NetworkServer.spawned[targetNetID].GetComponent<MapPlayer>();
        TargetResponseSwapReject(mapPlayer.GetComponent<NetworkIdentity>().connectionToClient);
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
            .Position(ToastPosition.Bottom)
            .MessageBoxColor(Color.green)
            .TextColor(Color.white)
            .Text($"위치 교환 요청이 수락되었습니다.")
            .Show();
    }

    // 교환요청 거절 응답 : 교환요청자에게만 거절 메시지 표시
    [TargetRpc]
    public void TargetResponseSwapReject(NetworkConnectionToClient target)
    {
        M_MessageManager.instance
            .Position(ToastPosition.Bottom)
            .MessageBoxColor(Color.red)
            .TextColor(Color.white)
            .Text($"위치 교환 요청이 거절되었습니다.")
            .Show();
    }

    // ----------------------------------------------------------------- View Update Method --------------------------------------------------------------------------------//

    // 맵플레이어 뷰 컴포넌트 초기화
    private void InitMapPlayerView()
    {
        int index = M_MapManager.instance.mapPlayers.FindIndex((netId) => netId == GetComponent<NetworkIdentity>().netId);
        if(index != -1){    
            ChangeMapPlayerViewByOrder(index);
            p_INFO_M_L.SetActive(isOwned);
            steamDisplayName.text = gamePlayer.objectOwner.steamPersonaName;
            SetOrderTextByPlayerOrder(gamePlayer.selectOrder);
            MapUI.instance.ChangeSwapButtonsState(GetComponent<NetworkIdentity>().netId, index);
            if(isOwned){
                SwapButtonOnMap swapButtonOnMap = MapUI.instance.swapButtons[index].GetComponent<SwapButtonOnMap>();
                swapButtonOnMap.t_M_Icon.SetActive(true);
                swapButtonOnMap.t_Chan_Icon.SetActive(false);
                swapButtonOnMap.t_Ready_Icon.SetActive(false);
            }
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
        for(int i=0; i<MapUI.instance.swapButtons.Count; i++){
            SwapButtonOnMap swapButtonOnMap = MapUI.instance.swapButtons[i].GetComponent<SwapButtonOnMap>();
            swapButtonOnMap.t_Chan_Icon.SetActive(true);
            swapButtonOnMap.t_M_Icon.SetActive(false);
            swapButtonOnMap.t_Ready_Icon.SetActive(false);
        }
        int ownedMapPlayerIndex = M_MapManager.instance.mapPlayers.FindIndex((netId) => netId == M_MapManager.instance.ownedMapPlayer);
        if(ownedMapPlayerIndex != -1){
            SwapButtonOnMap ownedSwapButton = MapUI.instance.swapButtons[ownedMapPlayerIndex].GetComponent<SwapButtonOnMap>();
            ownedSwapButton.t_M_Icon.SetActive(true);
            ownedSwapButton.t_Chan_Icon.SetActive(false);
            ownedSwapButton.t_Ready_Icon.SetActive(false);
        }
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
}
