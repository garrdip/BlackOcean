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
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "choose_character_mouseover");
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
        M_SoundManager.instance.StopAllVoice();
        switch(character){
            case Character.HONGDANHYANG:
                List<AudioClip> danhyangVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.HongDanHyang, 0, 3);
                AudioClip danhyangEliteBattleVoice = danhyangVoices[Random.Range(0, danhyangVoices.Count)];
                M_SoundManager.instance.PlayVoice(danhyangEliteBattleVoice, danhyangEliteBattleVoice.length);
                break;
            case Character.GEORK:
                List<AudioClip> georkVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Geork, 0, 3);
                AudioClip georkEliteBattleVoice = georkVoices[Random.Range(0, georkVoices.Count)];
                M_SoundManager.instance.PlayVoice(georkEliteBattleVoice, georkEliteBattleVoice.length);
                break;
            case Character.ERIS:
                List<AudioClip> erisVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Eris, 0, 4);
                AudioClip erisEliteBattleVoice = erisVoices[Random.Range(0, erisVoices.Count)];
                M_SoundManager.instance.PlayVoice(erisEliteBattleVoice, erisEliteBattleVoice.length);
                break;
        }
    }
}
