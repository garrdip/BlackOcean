using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;
using TMPro;
using ProjectD;


public class CharacterSelector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Character Selector Object Size")]
    public float originScale; // 원래의 오브젝트 사이즈 값
    public float hoverScale; // 마우스 Enter 또는  Exit시 변화되는 사이즈 값

    [Header("View Components")]
    public TextMeshProUGUI characterLabel;

    [Header("Character")]
    public Character character;


    void Start()
    {
        originScale = 1f;
        hoverScale = 0.1f;
        RoomUI.Instance.onCharacterSelect += OnCharacterSelected;
        RoomUI.Instance.onCharacterSelectcancel += OnCharacterSelectCanceled;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        gameObject.transform.localScale = new Vector3(originScale + hoverScale, originScale + hoverScale, 0);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        gameObject.transform.localScale = new Vector3(originScale - hoverScale, originScale - hoverScale, 0);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(!CheckCharacterIsAlreadySelected()){
            RoomUI.Instance.EmitCharacterSelectEvent(character);
        }
    }

    // 현재 방에 참가한 플레이어들의 character값들을 순회하여 로컬플레이어가 선택하려하는 캐릭터를 이미 다른 플레이어가 선택했는지 체크
    public bool CheckCharacterIsAlreadySelected()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        List<NetworkRoomPlayer> players = M_NetworkRoomManager.roomSlots;
        foreach(RoomPlayer roomPlayer in players){
            if(!roomPlayer.isLocalPlayer && roomPlayer.character.Equals(character)){
                Debug.Log("이미 다른사람이 선택한 캐릭터 입니다");
                return true;
            }
        }
        return false;
    }

    // 캐릭터 선택 델리게이트 수신(선택된 캐릭터 이름 텍스트 붉은색 변경)
    public void OnCharacterSelected(Character selectedCharacter)
    {
        if(character.Equals(selectedCharacter)){
            characterLabel.color = Color.red;
        }else{
            characterLabel.color = Color.white;
        }
    }

    // 캐릭터 선택 취소 델리게이트 수신(취소된 캐릭터 이름 텍스트 원상태로 변경)
    public void OnCharacterSelectCanceled()
    {
        characterLabel.color = Color.white;
    }
}