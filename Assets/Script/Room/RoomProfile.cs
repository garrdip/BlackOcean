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
    }
    public void EnableButton()
    {
        changeOrderButton.SetActive(true);
        kickButton.SetActive(true);
    }

    public void Update()
    {
        RoomPlayer[] players = FindObjectsOfType<RoomPlayer>();
        player = null;
        foreach(RoomPlayer user in players)
        {
            if(user.order == playOrder)
            {
                player = user;
            }
        }
        if(player == null)
            SetEmpty();
        else
        {
            UpdateCharacter();
            UpdateReadyState();
            ChangeSteamProfile();
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

    public void ChangeSteamProfile()
    {
        // Name 
        steamID.text = SteamFriends.GetFriendPersonaName((CSteamID)player.steamID);
        // Avatar
        int imageId = SteamFriends.GetLargeFriendAvatar((CSteamID)player.steamID);
        steamAvatar.texture = GetSteamImageAsTexture(imageId);
    }

    public Texture2D GetSteamImageAsTexture(int iImage)
    {
        Texture2D texture = null;
        bool isValid = SteamUtils.GetImageSize(iImage, out uint width, out uint height);

        if(isValid)
        {
            byte[] image = new byte[width * height * 4];

            isValid = SteamUtils.GetImageRGBA(iImage, image, (int)(width * height * 4));

            if(isValid)
            {
                texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
                texture.LoadRawTextureData(image);
                FlipTextureVertically(texture);
                texture.Apply();
            }
        }
        return texture;
    }

    public void FlipTextureVertically(Texture2D original)
    {
        var originalPixels = original.GetPixels();
        var newPixels = new Color[originalPixels.Length];

        var width = original.width;
        var rows = original.height;
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < rows; y++)
            {
                newPixels[x + y * width] = originalPixels[x + (rows - y - 1) * width];
            }
        }

        original.SetPixels(newPixels);
        original.Apply();
    }
}
