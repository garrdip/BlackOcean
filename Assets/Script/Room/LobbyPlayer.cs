using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectD;
using Mirror;

public class LobbyPlayer : NetworkBehaviour
{

    [Header("UI 컴포넌트")]
    public GameObject selectorBaseLayout;
    public Image characterSelectCompleteImage;
    public GameObject classLayout;
    public GameObject classIconLayout;
    public GameObject profileLayout;
    public GameObject outIconLayout;
    public GameObject capIconLayout;
    public List<GameObject> selectableCharacters = new List<GameObject>();

    public Sprite danhyangSprite;
    public Sprite erisSprite;
    public Sprite georkSprite;

    [SyncVar]
    public RoomPlayer roomPlayer;

    [SyncVar]
    public PlayOrder playOrder;

    void Start()
    {
        roomPlayer.onSelectCompleteCharacter += OnSelectCompleteCharacter; // 캐릭터 선택 이벤트 수신
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        selectorBaseLayout.SetActive(isOwned);
        transform.SetParent(RoomUI.instance.topIconList[(int)playOrder].transform); // 플레이어 Order값에 따라 룸씬의 각 아이콘 순서에 맞춰 자식오브젝트로 설정
        transform.localScale = new Vector3(1f, 1f, 1f); // SetParent 수행시 스케일이 부모에 따라 변동되므로 수동으로 스케일 기본값인 1로 조정
        transform.localPosition = Vector3.zero;
        transform.SetAsFirstSibling(); // TopIcon보다 먼저 그려지도록 SiblingIndex를 맨 처음으로 설정
        OnSelectCompleteCharacter(roomPlayer.character); // 클라 생성 시점에도 캐릭터 선택 정보 세팅(클라이언트가 방에 접속할때 다른 유저가 이미 선택한 상태일 경우 그 값을 수신받아 설정하는 용도)
    }

    public void OnSelectCompleteCharacter(Character character)
    {
        if(character != Character.NONE){
            selectorBaseLayout.SetActive(true);
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
        }
    }
}
