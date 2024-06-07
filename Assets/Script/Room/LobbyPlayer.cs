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
    [Header("SelectBaseLayout")]
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
    public Image classIcon;
    public Sprite georkIcon;
    public Sprite erisIcon;
    public Sprite dnahyangIcon;

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
    private const float upPositionY = 60f;
    private const float downPositionY = -30f;

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

    protected Callback<AvatarImageLoaded_t> avatarImageLoaded;

    void Start()
    {
        avatarImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
        buttonSwapAccept.onClick.AddListener(() => HandleClickButtonSwapAccept()); // 교환 수락 버튼
        buttonSwapReject.onClick.AddListener(() => HandleClickButtonSwapReject()); // 교환 거절 버튼
        characterSelectCompleteImage.GetComponent<Button>().onClick.AddListener(() => HandleClickSelectCompleteImage()); // 캐릭터 선택 완료된 상태에서 캐릭터 이미지 클릭
        outIconLayout.GetComponent<Button>().onClick.AddListener(() => HandleClickLobbyPlayerKickOut()); // 강퇴 버튼 클릭 이벤트(서버 유저만 호출 가능)
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        roomPlayer.onSelectCompleteCharacter += OnChangeSelectCharacter; // 캐릭터 선택 이벤트 등록
        roomPlayer.onChangeReadyState += OnChangeReadyState; // 레디 상태 변경 이벤트 등록
        InitLobbyPlayerView(isOwned); // 로비플레이어 뷰 초기화
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        CmdSetSteamId((ulong)SteamUser.GetSteamID());// 로컬유저의 스팀아이디를 조회하여 다른 클라이언트들에 공유
        M_LobbyMananger.instance.ownedLobbyPlayer = GetComponent<NetworkIdentity>().netId; // 로비매니저에 로컬플레이어 소유의 로비플레이어 오브젝트 참조값 설정
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        M_LobbyMananger.instance.RemoveLobbyPlayer(GetComponent<NetworkIdentity>().netId); // 로비플레이어가 서버에서 사라질 때 리스트에서 제거
    }

    // 로비플레이어 오브젝트 파괴시 수행중인 트위닝 제거
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
        characterSelectCompleteImage.GetComponent<RectTransform>().DOKill();
        characterSelectCompleteImage.DOKill();
        sequence.Kill(); // FadeIn, FadeOut, Up, Down 트위닝 시퀀스 제거
        RoomUI.instance.KillTweenSwapButtons(); // 로비플레이어의 SetLobbyPlayerFadeEffect 함수에서 작동시킨 스왑버튼 트위닝 제거
        roomPlayer.onSelectCompleteCharacter -= OnChangeSelectCharacter; // 캐릭터 선택 이벤트 제거
        roomPlayer.onChangeReadyState -= OnChangeReadyState; // 레디 상태 변경 이벤트 제거
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

    // 강퇴 버튼 클릭
    public void HandleClickLobbyPlayerKickOut()
    {
        if(((roomPlayer.isServer && roomPlayer.index > 0) || roomPlayer.isServerOnly)){
            M_LobbyMananger.instance.LobbyPlayerKickOut(roomPlayer);
        }
    }

    private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
    {
        if(callback.m_steamID.m_SteamID == steamID){
            steamAvatar.texture = M_SteamManager.instance.GetSteamImageAsTextureByImageId(callback.m_iImage);
        }else{
            return;
        }
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
        // 교환요청자와 상대방 모두에게 요청이 수락되었음을 알리는 TargetRpc 이벤트 전달
        uint ownedNetId = M_LobbyMananger.instance.lobbyPlayers[oldIndex];
        uint targetNetID = M_LobbyMananger.instance.lobbyPlayers[newIndex];
        if( ownedNetId != 0 && NetworkServer.spawned.TryGetValue(ownedNetId, out NetworkIdentity ownedNetIdentity) && 
            targetNetID != 0 && NetworkServer.spawned.TryGetValue(targetNetID, out NetworkIdentity targetNetworkIdentity)){
            
            // 스왑로직 수행
            M_LobbyMananger.instance.CmdSwapLobbyPlayer(oldIndex, newIndex);
            
            // 스왑요청 송신지에게 메시지 전달
            LobbyPlayer ownedlobbyPlayer = ownedNetIdentity.GetComponent<LobbyPlayer>();
            TargetResponseSwapAccept(ownedlobbyPlayer.GetComponent<NetworkIdentity>().connectionToClient);

            // 스왑요청 수신자에게 메시지 전달
            LobbyPlayer targetLobbyPlayer = targetNetworkIdentity.GetComponent<LobbyPlayer>();
            TargetResponseSwapAccept(targetLobbyPlayer.GetComponent<NetworkIdentity>().connectionToClient);
        }
    }

    // 교환요청 거절 커맨드
    [Command]
    public void CmdSwapReject(int targetIndex)
    {
        // 교환요청자에게 요청이 거절되었음을 알리는 TargetRpc 이벤트 전달
        uint targetNetID = M_LobbyMananger.instance.lobbyPlayers[targetIndex];
        if(targetNetID != 0 && NetworkServer.spawned.TryGetValue(targetNetID, out NetworkIdentity networkIdentity)){
            LobbyPlayer lobbyPlayer = networkIdentity.GetComponent<LobbyPlayer>();
            TargetResponseSwapReject(lobbyPlayer.GetComponent<NetworkIdentity>().connectionToClient);
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
            .Position(ToastPosition.Bottom)
            .MessageBoxColor(Color.green)
            .TextColor(Color.white)
            .Text("위치 교환 요청이 수락되었습니다.")
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
            .Text("위치 교환 요청이 거절되었습니다.")
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
        SetLobbyPlayerFadeEffect((int)roomPlayer.order);
        OnChangeSelectCharacter(roomPlayer.character); // 룸플레이어의 캐릭터 값으로 초기 설정
        SetOrderTextByPlayerOrder(roomPlayer.order); // 룸플레이어의 오더값으로 초기 설정
        ChangeClassLayoutFade(); // 캐릭터 클래스 레이아웃 Fade 애니매이션
        SetCapAndOutIconByPermission(); // 로비플레이어 상단 좌측의 방장표시 및 강퇴 아이콘을 권한에 따라 설정
        RoomUI.instance.ChangeSwapButtonsIconState(); // 스왑버튼 아이콘 상태 변경
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
            selectorBaseLineLight.SetActive(isServer);
            profileCoverLight.SetActive(isServer);
            profileLineLight.SetActive(isServer);
            switch(character){
                case Character.HONGDANHYANG:
                    characterSelectCompleteImage.sprite = danhyangSprite;
                    classIcon.sprite = dnahyangIcon;
                    break;
                case Character.ERIS:
                    characterSelectCompleteImage.sprite = erisSprite;
                    classIcon.sprite = erisIcon;
                    break;
                case Character.GEORK:
                    characterSelectCompleteImage.sprite = georkSprite;
                    classIcon.sprite = georkIcon;
                    break;
            }
            classIcon.gameObject.SetActive(true);
            characterSelectCompleteImage.GetComponent<RectTransform>().localPosition = new Vector3(0f, 500f, 0f);
            characterSelectCompleteImage.GetComponent<RectTransform>().DOLocalMoveY(-41f, 0.7f).SetEase(Ease.InOutExpo);
            characterSelectCompleteImage.color = new Color(255f, 255f, 255f, 0);
            characterSelectCompleteImage.DOFade(1f, 0.7f);
            characterSelectCompleteImage.gameObject.SetActive(true);
            classIconLayout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -50f);
            classIconLayout.GetComponent<RectTransform>().DOAnchorPosY(0f, 0.5f);
            AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("choose_character"));
            M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        }else{
            classIcon.gameObject.SetActive(false);
            characterSelectCompleteImage.gameObject.SetActive(false);
            AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("choose_character_cancel"));
            M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        }
    }

    // 로비플레이어 페이드 인,아웃 애니매이션 설정
    private void SetLobbyPlayerFadeEffect(int index)
    {
        RoomUI.instance.ChangeSwapButtonsState(GetComponent<NetworkIdentity>().netId, index);
        SetLobbyPlayerParent(index);
        GetComponent<CanvasGroup>().DOFade(1.0f, 0.5f);
        GetComponent<RectTransform>().DOLocalMoveY(downPositionY, 0.5f);
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

    // 룸플레이어 오더 텍스트 변경
    public void SetOrderTextByPlayerOrder(PlayOrder playOrder)
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
        RoomUI.instance.ChangeSwapButtonsIconState(); // 스왑버튼 아이콘 상태 변경
    }

    // 로비플레이어 뷰 컴포넌트 변경사항 업데이트
    public void ChangeLobbyPlayerViewByOrder(int index)
    {
        Tween fadeInTween = GetComponent<CanvasGroup>().DOFade(0.0f, 0.5f).OnComplete(() => { SetLobbyPlayerParent(index); });
        Tween upTween = GetComponent<RectTransform>().DOLocalMoveY(upPositionY, 0.5f).OnComplete(() => { SetOrderTextByPlayerOrder((PlayOrder)index); });
        Tween downTween = GetComponent<RectTransform>().DOLocalMoveY(downPositionY, 0.5f);
        Tween fadeOutTween = GetComponent<CanvasGroup>().DOFade(1.0f, 0.5f);
        sequence = DOTween.Sequence();
        sequence.Append(fadeInTween);
        sequence.Join(upTween);
        sequence.Append(downTween);
        sequence.Join(fadeOutTween);
        RoomUI.instance.ChangeSwapButtonsIconState();
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("choose_position"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    // 캐릭터 선택 완료 상태에서 이미지 클릭 이벤트
    private void HandleClickSelectCompleteImage()
    {
        if(isOwned){
            roomPlayer.character = Character.NONE; // 캐릭터 선택값 NONE으로 리셋
            roomPlayer.OnChangedCharacter(Character.NONE, Character.NONE);
            roomPlayer.isReady = false; // 레디상태 false로 리셋
            roomPlayer.OnChangeReady(false, false);
            ChangeSelectLayoutState(true); // 캐릭터 다시 선택할수 있도록 선택창 활성화
            ChangeClassLayoutFade();
        }
    }

    // 캐릭터 선택 레이아웃 활성화 상태 변경
    private void ChangeSelectLayoutState(bool isActive)
    {
        selectLeft.SetActive(isActive);
        selectMiddle.SetActive(isActive);
        selectRight.SetActive(isActive);
        baseDark.SetActive(isActive);
        classLayout.SetActive(isActive);
        characterSelectCompleteImage.gameObject.SetActive(!isActive);
        classIconLayout.SetActive(!classLayout.activeSelf);
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
