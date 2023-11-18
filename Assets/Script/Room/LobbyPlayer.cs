using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectD;
using Mirror;
using TMPro;
using Steamworks;

public class LobbyPlayer : NetworkBehaviour
{

    [Header("SelectorBaseLayout")]
    public GameObject selectorBaseLayout;
    public Image selectLeft;
    public Image selectMiddle;
    public Image selectRight;
    public Image baseDark;
    public Image characterSelectCompleteImage;
    public Sprite topIconMy;
    public Sprite topIconMyLight;
    public Sprite topIconExChange;
    public Sprite topIconExChangeLight;

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


    void Start()
    {
        roomPlayer.onSelectCompleteCharacter += OnSelectCompleteCharacter; // 캐릭터 선택 이벤트 수신
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        SetLobbyPlayerParent();
        InitLobbyPlayerView(isOwned);
        OnSelectCompleteCharacter(roomPlayer.character); // 클라 생성 시점에도 캐릭터 선택 정보 세팅(클라이언트가 방에 접속할때 다른 유저가 이미 선택한 상태일 경우 그 값을 수신받아 설정하는 용도)
        if(isOwned){
            CmdSetSteamId((ulong)SteamUser.GetSteamID());// 로컬유저의 스팀아이디를 조회하여 다른 클라이언트들에 공유
        }
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

    // LobbyPlayer 부모 오브젝트 설정 및 트랜스폼 값 설정
    private void SetLobbyPlayerParent()
    {
        transform.SetParent(RoomUI.instance.topIcons[(int)roomPlayer.order].transform); // 플레이어 Order값에 따라 룸씬의 각 아이콘 순서에 맞춰 자식오브젝트로 설정
        transform.localScale = new Vector3(1f, 1f, 1f); // SetParent 수행시 스케일이 부모에 따라 변동되므로 수동으로 스케일 기본값인 1로 조정
        transform.localPosition = Vector3.zero;
        transform.SetAsFirstSibling(); // TopIcon보다 먼저 그려지도록 SiblingIndex를 맨 처음으로 설정
    }

    // LobbyPlayer의 초기화 시점의 뷰요소 활성화, 비활성화 상태값 설정
    private void InitLobbyPlayerView(bool isOwned)
    {
        RoomUI.instance.topIconImages[(int)roomPlayer.order].sprite = isOwned ? topIconMy : topIconExChange;
        selectorBaseLayout.SetActive(isOwned);
        classLayout.SetActive(isOwned);
        classIconLayout.SetActive(!classLayout.activeSelf);
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
