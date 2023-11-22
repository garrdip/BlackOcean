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

    [SyncVar]
    public RoomPlayer roomPlayer;

    [SyncVar(hook = nameof(OnChangedSteamID))]
    public ulong steamID;

    // Dotween 참조값
    private Sequence sequence;
    private Tween fadeInTween;
    private Tween fadeOutTween;
    private Tween upTween;
    private Tween downTween;


    void Start()
    {
        roomPlayer.onSelectCompleteCharacter += OnSelectCompleteCharacter; // 캐릭터 선택 이벤트 수신
        InitLobbyPlayerView(isOwned); // 로비플레이어 뷰 초기화
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
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if(canvasGroup != null){
            int index = M_LobbyMananger.instance.lobbyPlayers.FindIndex((netId) => netId == GetComponent<NetworkIdentity>().netId);
            if(index != -1){
                transform.SetParent(RoomUI.instance.topIcons[index].transform); // 플레이어 Order값에 따라 룸씬의 각 아이콘 순서에 맞춰 자식오브젝트로 설정
                transform.localScale = new Vector3(1f, 1f, 1f); // SetParent 수행시 스케일이 부모에 따라 변동되므로 수동으로 스케일 기본값인 1로 조정
                transform.localPosition = new Vector3(0f, 200f, 0f);
                transform.SetAsFirstSibling(); // TopIcon보다 먼저 그려지도록 SiblingIndex를 맨 처음으로 설정
                canvasGroup.DOFade(1.0f, 0.5f);
                transform.DOLocalMoveY(0f, 0.5f);
                selectorBaseLayout.SetActive(isOwned);
                selectorBaseMyLine.gameObject.SetActive(isOwned);
                classLayout.SetActive(isOwned);
                classIconLayout.SetActive(!classLayout.activeSelf);
                if(isOwned){
                    RoomUI.instance.topIconImages[index].sprite = RoomUI.instance.topIconMy;
                }
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
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if(canvasGroup != null){
            fadeInTween = canvasGroup.DOFade(0.0f, 0.5f);
            upTween = transform.DOLocalMoveY(200f, 0.5f).OnComplete(() => {
                transform.SetParent(RoomUI.instance.topIcons[index].transform);
                transform.localPosition = new Vector3(0f, 200f, 0f);
                transform.localScale = new Vector3(1f, 1f, 1f);
                transform.SetAsFirstSibling();
            });
            downTween = transform.DOLocalMoveY(0f, 0.5f);
            fadeOutTween = canvasGroup.DOFade(1.0f, 0.5f);
            sequence = DOTween.Sequence();
            sequence.Append(fadeInTween);
            sequence.Join(upTween);
            sequence.Append(downTween);
            sequence.Join(fadeOutTween);
        }
    }

    // 트위닝, 시퀀스 제거
    private void KillTweenLobbyPlayer()
    {
        fadeInTween.Kill();
        fadeOutTween.Kill();
        upTween.Kill();
        downTween.Kill();
        sequence.Kill();
    }

    [Command]
    public void CmdSetSteamId(ulong steamId)
    {
        steamID = steamId;
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
}
