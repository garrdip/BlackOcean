using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectD;
using TMPro;
using Steamworks;

public class RoomProfile : MonoBehaviour
{
    public RoomPlayer player;
    public RoomPlayer prePlayer;

    [Header("This Position Order")]
    public PlayOrder playOrder;

    [Header("Character Image List")]
    public List<Sprite> characterImages;

    public Image characterImage;
    public GameObject readyState;
    public GameObject changeOrderButton;
    public GameObject kickButton;
    public Button changeCharacterButton;
    public GameObject RoomUIObject;
    public GameObject CharacterSelectUIObject;
    public TextMeshProUGUI steamID;
    public RawImage steamAvatar;
    bool isPlayerChanged = false;

    void Awake()
    {
        SetEmpty();
        changeCharacterButton.onClick.AddListener(() => ChangeCharacter());
    }
    public void SetEmpty()
    {
        readyState.SetActive(false);
        changeOrderButton.SetActive(false);
        kickButton.SetActive(false);
        characterImage.sprite = characterImages[4];
        steamID.text = "";
        steamAvatar.color = new Color(0,0,0,0);
        
    }
    public void EnableButton()
    {
        changeOrderButton.SetActive(true);
        kickButton.SetActive(true);
    }

    public void Update()
    {
        prePlayer = player;
        RoomPlayer[] players = FindObjectsOfType<RoomPlayer>();
        player = null;
        foreach(RoomPlayer user in players)
        {
            if(user.order == playOrder)
            {
                player = user;
            }
        }

        if(!isPlayerChanged)
            isPlayerChanged = (prePlayer == player)? false : true;

        if(player == null)
            SetEmpty();
        else
        {
            UpdateCharacter();
            UpdateReadyState();
            if(isPlayerChanged && player.steamID != 0){
                ChangeSteamProfile(player);
                isPlayerChanged = false;
            }
            if(!player.isLocalPlayer)
            {
                EnableButton();
            }
        }
    }
    
    public void ChangeCharacter()
    {
        if(player != null)
        if(player.isLocalPlayer)
        {
            RoomUIObject.SetActive(false);
            CharacterSelectUIObject.SetActive(true);
        }
    }

    public void UpdateCharacter()
    {
        switch(player.character)
        {
            case Character.HONGDANHYANG :
                characterImage.sprite = characterImages[0];
                break;
            case Character.GEORK :
                characterImage.sprite = characterImages[1];
                break;
            case Character.ERIS :
                characterImage.sprite = characterImages[2];
                break;
            case Character.NONE :
                characterImage.sprite = characterImages[3];
                break;
        }
    }

    public void UpdateReadyState()
    {
        readyState.SetActive(player.isReady);
    }

    public void ChangeSteamProfile(RoomPlayer roomPlayer)
    {
        // Name 
        steamID.text = roomPlayer.steamPersonaName;
        // Avatar
        if(roomPlayer.isValidAvatar)
        {
            byte[] avatarImage = new byte[roomPlayer.imageWidth * roomPlayer.imageHeight * 4];
            for(int i = 0 ;i < roomPlayer.image.Count ; i++)
                avatarImage[i] = roomPlayer.image[i];
            steamAvatar.texture = M_SteamManager.instance.GetSteamImageAsTexture(avatarImage,(int)roomPlayer.imageWidth, (int)roomPlayer.imageHeight);
            steamAvatar.color = new Color(1,1,1,1);
        }
    }


}
