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
        if(NetworkClient.active && NetworkClient.localPlayer != null){
            RoomPlayer roomPlayer = NetworkClient.localPlayer.GetComponent<RoomPlayer>();
            roomPlayer.character = character;
            roomPlayer.OnChangedCharacter(character, character);
            PlaySelectdCharacterVoice();
        }
    }

    // 캐릭터 선택 음성 랜덤 재생
    private void PlaySelectdCharacterVoice()
    {
        List<AudioClip> clips = M_SoundManager.instance.GetCharacterVoiceClips(character, 0, 3);
        AudioClip characterSelecteVoice = clips[Random.Range(0, clips.Count)];
        M_SoundManager.instance.StopAllSFX();
        M_SoundManager.instance.PlaySFX(characterSelecteVoice, characterSelecteVoice.length);
    }
}
