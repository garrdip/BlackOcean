using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;
using TMPro;
using UnityEngine.UI;

public class RoomPlayerForUI : MonoBehaviour
{
    public delegate void OnCharacterSelectRoomPlayer(NetworkIdentity networkIdentity, Character character);
    public event OnCharacterSelectRoomPlayer onCharacterSelectRoomPlayer;

    public delegate void OnReadyRoomPlayer(NetworkIdentity networkIdentity, bool isReady);
    public event OnReadyRoomPlayer onRedyRoomPlayer;

    [Header("RoomPlayer Network Object")]
    public RoomPlayer roomPlayer;

    [Header("Ready State")]
    public bool isReady;


    void Start()
    {
        if(roomPlayer.isLocalPlayer){
            transform.GetComponent<Image>().color = Color.yellow;
        }else{
            transform.GetComponent<Image>().color = Color.white;
        }
        onRedyRoomPlayer += OnChangeReadyRoomPlayer;
        onCharacterSelectRoomPlayer += OnChangeCharacterRoomPlayer;
    }

    // 레디 상태 변경 델리게이트 이벤트 송신
    public void EmitReadyStateRoomPlayerForUI(NetworkIdentity networkIdentity, bool isReady)
    {
        if(onRedyRoomPlayer != null){
            onRedyRoomPlayer.Invoke(networkIdentity, isReady);
        }
    }

    // 레디 상태 변경 델리게이트 이벤트 수신
    public void OnChangeReadyRoomPlayer(NetworkIdentity changedNetworkIdentity, bool changedReady)
    {
        NetworkIdentity playerIdentity = roomPlayer.GetComponent<NetworkIdentity>();
        if(playerIdentity == changedNetworkIdentity){
            isReady = changedReady;
            TextMeshProUGUI readyText = transform.GetChild(2).GetComponent<TextMeshProUGUI>();
            readyText.text = changedReady ? "Ready" : "Not Ready" ;
        }
    }

    // 캐릭터 선택 변경 델리게이트 이벤트 송신
    public void EmitCharacterChangeRoomPlayerForUI(NetworkIdentity networkIdentity, Character character)
    {
         if(onCharacterSelectRoomPlayer != null){
            onCharacterSelectRoomPlayer.Invoke(networkIdentity, character);
        }
    }

    // 캐릭터 선택 변경 델리게이트 이벤트 수신
    public void OnChangeCharacterRoomPlayer(NetworkIdentity changedNetworkIdentity, Character character)
    {
        NetworkIdentity playerIdentity = roomPlayer.GetComponent<NetworkIdentity>();
        if(playerIdentity == changedNetworkIdentity){
            TextMeshProUGUI characterText = transform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>();
            characterText.text = character.ToString();
        }
    }

}
