using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectD;
using Mirror;
using TMPro;
using Steamworks;
using DG.Tweening;

public class LobbyPlayer : NetworkBehaviour
{
    [Header("SelectorBaseLayout")]
    public GameObject selectorBaseLayout;
    public GameObject selectorBaseLineLight;
    public GameObject selectorBaseMyLine;
    public GameObject selectLeft;
    public GameObject selectLeftLight;
    public GameObject selectMiddle;
    public GameObject selectMiddleLight;
    public GameObject selectRight;
    public GameObject selectRightLight;
    public GameObject baseDark;
    public Image characterSelectCompleteImage;

    [Header("ClassLayout")]
    public GameObject classLayout;
    public GameObject classDanhyang;
    public GameObject classEris;
    public GameObject classGeork;

    [Header("ClassIconLayout")]
    public GameObject classIconLayout;

    [Header("ProfileLayout")]
    public GameObject profileLayout;
    public GameObject profileCoverLight;
    public GameObject profileLineLight;
    public RawImage steamAvatar;
    public TextMeshProUGUI steamDisplayName;

    [Header("OutIconLayout")]
    public GameObject outIconLayout;
    public GameObject outIcon;
    public GameObject outIconLight;

    [Header("CapIconLayout")]
    public GameObject capIconLayout;
    public Sprite danhyangSprite;
    public Sprite erisSprite;
    public Sprite georkSprite;
    public GameObject capIcon;
    public GameObject capIconLight;

    [Header("SwapRequestLayout")]
    public GameObject swapRequestLayout;
    public Button buttonSwapAccept;
    public Button buttonSwapReject;
    
    [Header("OrderLayout")]
    public TextMeshProUGUI textOrder;

    // Dotween 참조값
    private Sequence sequence;
    private float upPositionY;
    private float downPositionY;

    [SyncVar]
    public RoomPlayer roomPlayer;

    [SyncVar(hook = nameof(OnChangedSteamID))]
    public ulong steamID;

    [SyncVar]
    public int oldIndex;
    
    [SyncVar]
    public int newIndex;

    [SyncVar]
    public bool isHostLobbyPlayer;

    void Start()
    {
        upPositionY = 60f;
        downPositionY = -30f;
        roomPlayer.onSelectCompleteCharacter += OnChangeSelectCharacter; // 캐릭터 선택 이벤트 등록
        roomPlayer.onChangeRoomPlayerOrder += OnChangePlayerOrder; // 오더 변경 이벤트 등록
        roomPlayer.onChangeReadyState += OnChangeReadyState; // 레디 상태 변경 이벤트 등록
        InitLobbyPlayerView(isOwned); // 로비플레이어 뷰 초기화
        if(isOwned){
            buttonSwapAccept.onClick.AddListener(() => HandleClickButtonSwapAccept()); // 교환 수락 버튼
            buttonSwapReject.onClick.AddListener(() => HandleClickButtonSwapReject()); // 교환 거절 버튼
            characterSelectCompleteImage.GetComponent<Button>().onClick.AddListener(() => HandleClickSelectCompleteImage()); // 캐릭터 선택 완료된 상태에서 캐릭터 이미지 클릭
        }
        if(isServer){
            outIconLayout.GetComponent<Button>().onClick.AddListener(() => HandleClickLobbyPlayerOut()); // 강퇴 버튼 클릭 이벤트(서버 유저만 이벤트 등록)
        }
    }

    // 로비플레이어 오브젝트 파괴시 tween kill
    void OnDestroy()
    {
        GetComponent<CanvasGroup>().DOKill(); // 로비플레이어 캔버스그룹 트위닝 제거
        GetComponent<RectTransform>().DOKill(); // 로비플레이어 트랜스폼 트위닝 제거
        classIconLayout.GetComponent<RectTransform>().DOKill(); // 캐릭터 클래스 트랜스폼 아이콘 트위닝 제거
        classDanhyang.GetComponent<CanvasGroup>().DOKill();
        classEris.GetComponent<CanvasGroup>().DOKill();
        classGeork.GetComponent<CanvasGroup>().DOKill();
        classDanhyang.GetComponent<RectTransform>().DOKill();
        classEris.GetComponent<RectTransform>().DOKill();
        classGeork.GetComponent<RectTransform>().DOKill();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        OnChangeSelectCharacter(roomPlayer.character); // 클라 생성 시점에도 캐릭터 선택 정보 세팅(클라이언트가 방에 접속할때 다른 유저가 이미 선택한 상태일 경우 그 값을 수신받아 설정하는 용도)
        if(isOwned){
            CmdSetSteamId((ulong)SteamUser.GetSteamID());// 로컬유저의 스팀아이디를 조회하여 다른 클라이언트들에 공유
        }
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        M_LobbyMananger.instance.ownedLobbyPlayer = GetComponent<NetworkIdentity>().netId; // 로비매니저에 로컬플레이어 소유의 로비플레이어 오브젝트 참조값 설정
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        M_LobbyMananger.instance.RemoveLobbyPlayer(GetComponent<NetworkIdentity>().netId); // 로비플레이어가 서버에서 사라질 때 리스트에서 제거
    }

    private void HandleClickButtonSwapAccept()
    {
        swapRequestLayout.SetActive(false);
        CmdSwapAccept(oldIndex, newIndex);
    }

    private void HandleClickButtonSwapReject()
    {
        swapRequestLayout.SetActive(false);
        CmdSwapReject(oldIndex);
    }

    public void HandleClickLobbyPlayerOut()
    {
        Debug.Log("강퇴 버튼 클릭");
    }

    // ----------------------------------------------------------------- Command Method --------------------------------------------------------------------------------//

    [Command]
    public void CmdSetSteamId(ulong steamId)
    {
        steamID = steamId;
    }

    // 교환요청 수락 커맨드
    [Command]
    public void CmdSwapAccept(int oldIndex, int newIndex)
    {
        // 스왑로직 수행
        M_LobbyMananger.instance.CmdSwapLobbyPlayer(oldIndex, newIndex);

        // 교환요청자와 상대방 모두에게 요청이 수락되었음을 알리는 TargetRpc 이벤트 전달
        uint ownedNetId = M_LobbyMananger.instance.lobbyPlayers[oldIndex];
        uint targetNetID = M_LobbyMananger.instance.lobbyPlayers[newIndex];
        LobbyPlayer ownedlobbyPlayer = NetworkServer.spawned[ownedNetId].GetComponent<LobbyPlayer>();
        LobbyPlayer targetLobbyPlayer = NetworkServer.spawned[targetNetID].GetComponent<LobbyPlayer>();
        TargetResponseSwapAccept(ownedlobbyPlayer.GetComponent<NetworkIdentity>().connectionToClient);
        TargetResponseSwapAccept(targetLobbyPlayer.GetComponent<NetworkIdentity>().connectionToClient);
    }

    // 교환요청 거절 커맨드
    [Command]
    public void CmdSwapReject(int targetIndex)
    {
        // 교환요청자에게 요청이 거절되었음을 알리는 TargetRpc 이벤트 전달
        uint targetNetID = M_LobbyMananger.instance.lobbyPlayers[targetIndex];
        LobbyPlayer lobbyPlayer = NetworkServer.spawned[targetNetID].GetComponent<LobbyPlayer>();
        TargetResponseSwapReject(lobbyPlayer.GetComponent<NetworkIdentity>().connectionToClient);
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

    // ----------------------------------------------------------------- SyncVar Hook --------------------------------------------------------------------------------//

    // SteamID를 이용하여 플레이어 아이디와  프로필사진 데이터를 조회하여 뷰요소에 설정
    private void OnChangedSteamID(ulong oldVal,  ulong newVal)
    {
        var cSteamId = new CSteamID(newVal);
        steamDisplayName.text = SteamFriends.GetFriendPersonaName(cSteamId);
        int imageId = SteamFriends.GetLargeFriendAvatar(cSteamId);
        if(imageId == -1) return;
        steamAvatar.texture = M_SteamManager.instance.GetSteamImageAsTextureByImageId(imageId);
    }

    // ----------------------------------------------------------------- View Update Method --------------------------------------------------------------------------------//
 
    // 로비플레이어 초기 뷰 컴포넌트 설정(부모 오브젝트 설정 및 트랜스폼 값 설정)
    private void InitLobbyPlayerView(bool isOwned)
    {
        int index = M_LobbyMananger.instance.lobbyPlayers.FindIndex((netId) => netId == GetComponent<NetworkIdentity>().netId);
        if(index != -1){
            SetLobbyPlayerFadeEffect(index);
            OnChangeSelectCharacter(roomPlayer.character); // 룸플레이어의 캐릭터 값으로 초기 설정
            OnChangePlayerOrder(roomPlayer.order); // 룸플레이어의 오더값으로 초기 설정
            ChangeClassLayoutFade();
            SetCapAndOutIconByPermission(); // 로비플레이어 상단 좌측의 방장표시 및 강퇴 아이콘을 권한에 따라 설정
            if(isOwned){
                RoomUI.instance.topIconImages[index].sprite = RoomUI.instance.topIconMy;
            }
        }
    }

    // 룸플레이어 캐릭터 선택 변경 이벤트 수신
    public void OnChangeSelectCharacter(Character character)
    {
        if(character != Character.NONE){
            selectorBaseLayout.SetActive(true);
            classLayout.SetActive(false);
            classIconLayout.SetActive(!classLayout.activeSelf);
            selectLeft.SetActive(false);
            selectMiddle.SetActive(false);
            selectRight.SetActive(false);
            selectLeftLight.SetActive(false);
            selectMiddleLight.SetActive(false);
            selectRightLight.SetActive(false);
            baseDark.SetActive(false);
            characterSelectCompleteImage.gameObject.SetActive(true);
            switch(character){
                case Character.HONGDANHYANG:
                    characterSelectCompleteImage.sprite = danhyangSprite;
                    break;
                case Character.ERIS:
                    characterSelectCompleteImage.sprite = erisSprite;
                    break;
                case Character.GEORK:
                    characterSelectCompleteImage.sprite = georkSprite;
                    break;
            }
            selectorBaseLineLight.SetActive(isServer);
            profileCoverLight.SetActive(isServer);
            profileLineLight.SetActive(isServer);
            classIconLayout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -50f);
            classIconLayout.GetComponent<RectTransform>().DOAnchorPosY(0f, 0.5f);
        }
    }

    // 로비플레이어 페이드 인,아웃 애니매이션 설정
    private void SetLobbyPlayerFadeEffect(int index)
    {
        RoomUI.instance.ChangeSwapButtonsState(GetComponent<NetworkIdentity>().netId, index);
        SetLobbyPlayerParent(index);
        GetComponent<CanvasGroup>().DOFade(1.0f, 0.5f);
        transform.DOLocalMoveY(downPositionY, 0.5f);
        selectorBaseLayout.SetActive(isOwned);
        selectorBaseMyLine.SetActive(isOwned);
        classLayout.SetActive(isOwned);
        classIconLayout.SetActive(!classLayout.activeSelf);
    }

    // 로비플레이어 부모 오브젝트 설정
    private void SetLobbyPlayerParent(int index)
    {
        transform.SetParent(RoomUI.instance.topIcons[index].transform); // 플레이어 Order값에 따라 룸씬의 각 아이콘 순서에 맞춰 자식오브젝트로 설정
        transform.localScale = new Vector3(1f, 1f, 1f); // SetParent 수행시 스케일이 부모에 따라 변동되므로 수동으로 스케일 기본값인 1로 조정
        transform.localPosition = new Vector3(0f, upPositionY, 0f);
        transform.SetAsFirstSibling(); // TopIcon보다 먼저 그려지도록 SiblingIndex를 맨 처음으로 설정
    }

    // 플레이어 권한에 따라 좌측상단 아이콘 설정
    private void SetCapAndOutIconByPermission()
    {
        if(isServer){ // 서버 유저인 경우
            capIcon.SetActive(isOwned);
            outIcon.SetActive(!isOwned);
        }else{ // 클라 유저인 경우
            if(isHostLobbyPlayer){
                capIcon.SetActive(true);
                outIcon.SetActive(false);
            }
        }
    }

    // 룸플레이어 오더 변경 수신
    public void OnChangePlayerOrder(PlayOrder playOrder)
    {
        switch(playOrder){
            case PlayOrder.FIRST:
                textOrder.text = "후열";
                break;
            case PlayOrder.SECOND:
                textOrder.text = "중열";
                break;
            case PlayOrder.THIRD:
                textOrder.text = "전열";
                break;     
        }      
    }

    // 룸플레이어 레디상태 변경 수신
    public void OnChangeReadyState(bool isReady)
    {
        selectorBaseLineLight.SetActive(isReady);
        profileCoverLight.SetActive(isReady);
        profileLineLight.SetActive(isReady);
    }

    // 로비플레이어 뷰 컴포넌트 변경사항 업데이트
    public void ChangeLobbyPlayerView(int index)
    {
        Tween fadeInTween = GetComponent<CanvasGroup>().DOFade(0.0f, 0.5f);
        Tween upTween = transform.DOLocalMoveY(upPositionY, 0.5f).OnComplete(() => { SetLobbyPlayerParent(index); });
        Tween downTween = transform.DOLocalMoveY(downPositionY, 0.5f);
        Tween fadeOutTween = GetComponent<CanvasGroup>().DOFade(1.0f, 0.5f);
        sequence = DOTween.Sequence();
        sequence.Append(fadeInTween);
        sequence.Join(upTween);
        sequence.Append(downTween);
        sequence.Join(fadeOutTween);
        for(int i=0; i<RoomUI.instance.swapButtons.Count; i++){
            RoomUI.instance.topIconImages[i].sprite =  RoomUI.instance.topIconExChange;
        }
        int ownedLobbyPlayerIndex = M_LobbyMananger.instance.lobbyPlayers.FindIndex((netId) => netId == M_LobbyMananger.instance.ownedLobbyPlayer);
        if(ownedLobbyPlayerIndex != -1){
            RoomUI.instance.topIconImages[ownedLobbyPlayerIndex].sprite =  RoomUI.instance.topIconMy;
        }
    }
    
    // 캐릭터 선택 완료 상태 뷰 업데이트
    private void HandleClickSelectCompleteImage()
    {
        selectLeft.SetActive(true);
        selectMiddle.SetActive(true);
        selectRight.SetActive(true);
        baseDark.SetActive(true);
        classLayout.SetActive(true);
        characterSelectCompleteImage.gameObject.SetActive(false);
        classIconLayout.SetActive(!classLayout.activeSelf);
        ChangeClassLayoutFade();
    }

    // 캐릭터 클래스 레이아웃 Fade 애니매이션
    private void ChangeClassLayoutFade()
    {
        classDanhyang.GetComponent<CanvasGroup>().DOKill();
        classEris.GetComponent<CanvasGroup>().DOKill();
        classGeork.GetComponent<CanvasGroup>().DOKill();
        classDanhyang.GetComponent<RectTransform>().DOKill();
        classEris.GetComponent<RectTransform>().DOKill();
        classGeork.GetComponent<RectTransform>().DOKill();

        classDanhyang.GetComponent<RectTransform>().anchoredPosition = new Vector2(classDanhyang.GetComponent<RectTransform>().anchoredPosition.x, 50f);
        classEris.GetComponent<RectTransform>().anchoredPosition = new Vector2(classEris.GetComponent<RectTransform>().anchoredPosition .x, 50f);
        classGeork.GetComponent<RectTransform>().anchoredPosition = new Vector2(classGeork.GetComponent<RectTransform>().anchoredPosition .x, 50f);
        classDanhyang.GetComponent<RectTransform>().DOAnchorPosY(0f, 1f);
        classEris.GetComponent<RectTransform>().DOAnchorPosY(0f, 1f);
        classGeork.GetComponent<RectTransform>().DOAnchorPosY(0f, 1f);

        classDanhyang.GetComponent<CanvasGroup>().alpha = 0f;
        classEris.GetComponent<CanvasGroup>().alpha = 0f;
        classGeork.GetComponent<CanvasGroup>().alpha = 0f;
        classDanhyang.GetComponent<CanvasGroup>().DOFade(1f, 1f);
        classEris.GetComponent<CanvasGroup>().DOFade(1f, 1f);
        classGeork.GetComponent<CanvasGroup>().DOFade(1f, 1f);
    }

}
