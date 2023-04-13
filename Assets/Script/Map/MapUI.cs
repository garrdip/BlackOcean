using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using Steamworks;

public class MapUI : InstanceD<MapUI>
{
    public GameObject[] orderSelected;
    public GameObject[] playerProfiles;

    public Button readyButton;

    public void Start()
    {
        readyButton.onClick.AddListener(() => OnChangeReadyState());
    }

    public void SetOrderIndicator(int order)
    {
        orderSelected[0].SetActive(order == 0 ? true : false);
        orderSelected[1].SetActive(order == 1 ? true : false);
        orderSelected[2].SetActive(order == 2 ? true : false);
    }

    public void UpdateProfile()
    {
        GamePlayer[] users = FindObjectsOfType<GamePlayer>();
        foreach(GamePlayer user in users)
        {
            if(!user.isInitializeDone)return;
            // Avatar
            int imageId = SteamFriends.GetLargeFriendAvatar((CSteamID)user.steamID);
            playerProfiles[user.selectOrder].transform.GetChild(6).GetComponent<RawImage>().texture = M_SteamManager.instance.GetSteamImageAsTexture(imageId);
            playerProfiles[user.selectOrder].transform.GetChild(6).GetComponent<RawImage>().color = new Color(1,1,1,1);
            // Show ID
            playerProfiles[user.selectOrder].transform.GetChild(5).GetComponent<TextMeshProUGUI>().text = SteamFriends.GetFriendPersonaName((CSteamID)user.steamID);
            // Show Ready State
            playerProfiles[user.selectOrder].transform.GetChild(1).gameObject.SetActive(user.isReady == true ? true : false);
            // HP (Right 195 -> 0 : 0 -> Max)
            playerProfiles[user.selectOrder].transform.GetChild(3).GetChild(0).GetComponent<RectTransform>().offsetMax = 
            new Vector2(( 195 * user.HP / user.MaxHP ) - 195,playerProfiles[user.selectOrder].transform.GetChild(3).GetChild(0).GetComponent<RectTransform>().offsetMax.y);
            // Ichi
        }
    }

    public void OnChangeReadyState()
    {
        NetworkClient.localPlayer.GetComponent<GamePlayer>().isReady = !NetworkClient.localPlayer.GetComponent<GamePlayer>().isReady;
        UpdateProfile();
    }

}
