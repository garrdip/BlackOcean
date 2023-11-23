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
    public Image selectorBaseMyLine;
    public Image selectLeft;
    public Image selectMiddle;
    public Image selectRight;
    public Image baseDark;
    public Image characterSelectCompleteImage;

    [Header("ClassLayout")]
    public GameObject classLayout;

    [Header("ClassIconLayout")]
    public GameObject classIconLayout;

    [Header("ProfileLayout")]
    public GameObject profileLayout;
    public RawImage steamAvatar;
    public TextMeshProUGUI steamDisplayName;

    [Header("OutIconLayout")]
    public GameObject outIconLayout;

    [Header("CapIconLayout")]
    public GameObject capIconLayout;
    public List<GameObject> selectableCharacters = new List<GameObject>();
    public Sprite danhyangSprite;
    public Sprite erisSprite;
    public Sprite georkSprite;

    [Header("SwapRequestLayout")]
    public GameObject swapRequestLayout;
    public Button buttonSwapAccept;
    public Button buttonSwapReject;

    // Dotween 참조값
    private Sequence sequence;

    private float positionY;

    [SyncVar]
    public RoomPlayer roomPlayer;

    [SyncVar(hook = nameof(OnChangedSteamID))]
    public ulong steamID;

    [SyncVar]
    public int oldIndex;
    
    [SyncVar]
    public int newIndex;


    void Start()
    {
        positionY = -30f;
        roomPlayer.onSelectCompleteCharacter += OnSelectCompleteCharacter; // 캐릭터 선택 이벤트 수신
        InitLobbyPlayerView(isOwned); // 로비플레이어 뷰 초기화
        buttonSwapAccept.onClick.AddListener(() => HandleClickButtonSwapAccept()); // 교환 수락 버튼
        buttonSwapReject.onClick.AddListener(() => HandleClickButtonSwapReject()); // 교환 거절 버튼
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

    void OnDestroy()
    {
        KillTweenLobbyPlayer(); // 로비플레이어 오브젝트 파괴시 sequence, tween kill
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        OnSelectCompleteCharacter(roomPlayer.character); // 클라 생성 시점에도 캐릭터 선택 정보 세팅(클라이언트가 방에 접속할때 다른 유저가 이미 선택한 상태일 경우 그 값을 수신받아 설정하는 용도)
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

    public void OnSelectCompleteCharacter(Character character)
    {
        if(character != Character.NONE){
            selectLeft.gameObject.SetActive(false);
            selectMiddle.gameObject.SetActive(false);
            selectRight.gameObject.SetActive(false);
            baseDark.gameObject.SetActive(false);
            classLayout.SetActive(false);
            characterSelectCompleteImage.gameObject.SetActive(true);
            selectorBaseLayout.SetActive(true);
            classIconLayout.SetActive(!classLayout.activeSelf);
            
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
        }
    }

    // 로비플레이어 초기 뷰 컴포넌트 설정(부모 오브젝트 설정 및 트랜스폼 값 설정)
    private void InitLobbyPlayerView(bool isOwned)
    {
        int index = M_LobbyMananger.instance.lobbyPlayers.FindIndex((netId) => netId == GetComponent<NetworkIdentity>().netId);
        if(index != -1){
            RoomUI.instance.ChangeSwapButtonsState(GetComponent<NetworkIdentity>().netId, index);
            transform.SetParent(RoomUI.instance.topIcons[index].transform); // 플레이어 Order값에 따라 룸씬의 각 아이콘 순서에 맞춰 자식오브젝트로 설정
            transform.localScale = new Vector3(1f, 1f, 1f); // SetParent 수행시 스케일이 부모에 따라 변동되므로 수동으로 스케일 기본값인 1로 조정
            transform.localPosition = new Vector3(0f, 100f, 0f);
            transform.SetAsFirstSibling(); // TopIcon보다 먼저 그려지도록 SiblingIndex를 맨 처음으로 설정
            GetComponent<CanvasGroup>().DOFade(1.0f, 0.5f);
            transform.DOLocalMoveY(positionY, 0.5f);
            selectorBaseLayout.SetActive(isOwned);
            selectorBaseMyLine.gameObject.SetActive(isOwned);
            classLayout.SetActive(isOwned);
            classIconLayout.SetActive(!classLayout.activeSelf);
            if(isOwned){
                RoomUI.instance.topIconImages[index].sprite = RoomUI.instance.topIconMy;
            }
        }
    }

    // 로비플레이어 뷰 컴포넌트 변경사항 업데이트
    public void ChangeLobbyPlayerView(int index)
    {
        DOTweenLobbyPlayer(index);
        for(int i=0; i<RoomUI.instance.swapButtons.Count; i++){
            RoomUI.instance.topIconImages[i].sprite =  RoomUI.instance.topIconExChange;
        }
        int ownedLobbyPlayerIndex = M_LobbyMananger.instance.lobbyPlayers.FindIndex((netId) => netId == M_LobbyMananger.instance.ownedLobbyPlayer);
        if(ownedLobbyPlayerIndex != -1){
            RoomUI.instance.topIconImages[ownedLobbyPlayerIndex].sprite =  RoomUI.instance.topIconMy;
        }
    }

    // 트위닝, 시퀀스 등록
    private void DOTweenLobbyPlayer(int index)
    {
        Tween fadeInTween = GetComponent<CanvasGroup>().DOFade(0.0f, 0.5f);
        Tween upTween = transform.DOLocalMoveY(100f, 0.5f).OnComplete(() => {
            transform.SetParent(RoomUI.instance.topIcons[index].transform);
            transform.localPosition = new Vector3(0f, 100f, 0f);
            transform.localScale = new Vector3(1f, 1f, 1f);
            transform.SetAsFirstSibling();
        });
        Tween downTween = transform.DOLocalMoveY(positionY, 0.5f);
        Tween fadeOutTween = GetComponent<CanvasGroup>().DOFade(1.0f, 0.5f);
        sequence = DOTween.Sequence();
        sequence.Append(fadeInTween);
        sequence.Join(upTween);
        sequence.Append(downTween);
        sequence.Join(fadeOutTween);
    }

    // 트위닝, 시퀀스 제거
    private void KillTweenLobbyPlayer()
    {
        GetComponent<CanvasGroup>().DOKill(); // 로비플레이어 캔버스그룹 트위닝 제거
        GetComponent<RectTransform>().DOKill(); // 로비플레이어 트랜스폼 트위닝 제거
    }

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

    // SteamID를 이용하여 플레이어 아이디와  프로필사진 데이터를 조회하여 뷰요소에 설정
    private void OnChangedSteamID(ulong oldVal,  ulong newVal)
    {
        var cSteamId = new CSteamID(newVal);
        steamDisplayName.text = SteamFriends.GetFriendPersonaName(cSteamId);
        int imageId = SteamFriends.GetLargeFriendAvatar(cSteamId);
        if(imageId == -1) return;
        steamAvatar.texture = M_SteamManager.instance.GetSteamImageAsTextureByImageId(imageId);
    }

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
}
