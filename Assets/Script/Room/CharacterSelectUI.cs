using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ProjectD;
using Mirror;

public class CharacterSelectUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("캐릭터")]
    public Character character;

    [Header("UI 컴포넌트")]
    public Image hoverImage;

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        hoverImage.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        hoverImage.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RoomPlayer roomPlayer = NetworkClient.localPlayer.GetComponent<RoomPlayer>();
        if(roomPlayer.isLocalPlayer){
            roomPlayer.character = character;
            roomPlayer.OnChangedCharacter(character, character);
        }
    }
}
