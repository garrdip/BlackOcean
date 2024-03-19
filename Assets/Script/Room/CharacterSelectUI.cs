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
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("choose_character_mouseover"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
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
            PlaySelectdCharacterVoice(character);
        }
    }

    // 캐릭터 선택 음성 랜덤 재생
    private void PlaySelectdCharacterVoice(Character character)
    {
        List<AudioClip> clips = M_SoundManager.instance.GetCharacterVoiceClips(character, 0, 3);
        AudioClip characterSelecteVoice = clips[Random.Range(0, clips.Count)];
        M_SoundManager.instance.StopAllVoice();
        M_SoundManager.instance.PlayVoice(characterSelecteVoice, characterSelecteVoice.length);
    }
}
