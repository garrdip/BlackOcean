using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Mirror;
using ProjectD;
using DG.Tweening;
using TMPro;
using Spine.Unity;
using Spine.Unity.Examples;
using System.Linq;

// TargetObject partial — 캐릭터 음성 및 비용 부족 말풍선
public partial class TargetObject
{

    // 남은 코스트 없음 표시하는 말풍선 페이드인 후 페이드아웃
    public void ShowCostNotReaminBubble(GamePlayer gamePlayer)
    {
        Dictionary<string, string> constDict = new Dictionary<string, string>();
        constDict.Add("georg_78", Const.Georg_78);
        constDict.Add("georg_79", Const.Georg_79);
        constDict.Add("georg_80", Const.Georg_80);
        constDict.Add("Eris_116", Const.Eris_116);
        constDict.Add("Eris_117", Const.Eris_117);
        constDict.Add("Eris_118", Const.Eris_118);
        constDict.Add("Eris_119", Const.Eris_119);
        constDict.Add("Hong_66", Const.Hong_66);
        constDict.Add("Hong_67", Const.Hong_67);
        constDict.Add("Hong_68", Const.Hong_68);

        // 캐릭터 별 음성 클립 재생
        switch (gamePlayer.character){
            case Character.GEORK:
                PlayCharacterRequireCostVoice(Character.GEORK, 77, 3, constDict);
                break;
            case Character.ERIS:
                PlayCharacterRequireCostVoice(Character.ERIS, 115, 4, constDict);
                break;
            case Character.HONGDANHYANG:
                PlayCharacterRequireCostVoice(Character.HONGDANHYANG, 65, 3, constDict);
                break;
        }
        // 페이드인 1초 후 페이드아웃 1초 
        CanvasGroup canvasGroup = playerMessageCavnas.GetComponent<CanvasGroup>();
        if(canvasGroup != null){
            if(DOTween.IsTweening(canvasGroup)){
                canvasGroup.DOKill();
            }
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.DOFade(1.0f, 1f).OnComplete(() => {
                canvasGroup.DOFade(0.0f, 1f).OnComplete(() => {
                    canvasGroup.gameObject.SetActive(false);
                }); 
            }); 
        }
    }


    // 캐릭터별 음성 생성 및 팝업창 텍스트 세팅
    private void PlayCharacterRequireCostVoice(Character character, int startClipIndex, int numberOfClips, Dictionary<string, string> constDict)
    {
        List<AudioClip> clips = new List<AudioClip>();
        switch(character){
            case Character.HONGDANHYANG:
                List<AudioClip> danhyangVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.HongDanHyang, startClipIndex, numberOfClips);
                foreach(AudioClip audioClip in danhyangVoices){
                    clips.Add(audioClip);
                }
                break;
            case Character.GEORK:
                List<AudioClip> georkVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Geork, startClipIndex, numberOfClips);
                foreach(AudioClip audioClip in georkVoices){
                    clips.Add(audioClip);
                }
                break;
            case Character.ERIS:
                List<AudioClip> erisVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Eris, startClipIndex, numberOfClips);
                foreach(AudioClip audioClip in erisVoices){
                    clips.Add(audioClip);
                }
            break;
        }
        if(clips.Count > 0){
            int randomIndex = Random.Range(0, clips.Count);
            AudioClip clipToPlay = clips[randomIndex];

            if(constDict.TryGetValue(clipToPlay.name, out string message)){
                playerMessageBubble.text = message;
            }
            M_SoundManager.instance.StopAllVoice();
            M_SoundManager.instance.PlayVoice(clipToPlay, clipToPlay.length);
        }
    }


    // 캐릭터별 피격 음성 재생
    private void PlayCharaterHitVoice()
    {
        AudioClip hitVoice = null;
        switch(player.character){
            case Character.HONGDANHYANG:
                List<AudioClip> danhyangVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.HongDanHyang, 58, 4);
                hitVoice = danhyangVoices[Random.Range(0, danhyangVoices.Count)];
                break;
            case Character.GEORK:
                List<AudioClip> georkVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Geork, 65, 9);
                hitVoice = georkVoices[Random.Range(0, georkVoices.Count)];
                break;
            case Character.ERIS:
                List<AudioClip> erisVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Eris, 99, 6);
                hitVoice = erisVoices[Random.Range(0, erisVoices.Count)];
                break;
        }
        M_SoundManager.instance.PlayVoice(hitVoice, hitVoice.length);
    }


    // 캐릭터별 사망 음성 재생
    private void PlayChararcterDeathVoice()
    {
        AudioClip playerDeathVoice = null;
        switch(player.character){
            case Character.HONGDANHYANG:
                List<AudioClip> danhyangVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.HongDanHyang, 62, 3);
                playerDeathVoice = danhyangVoices[Random.Range(0, danhyangVoices.Count)];
                break;
            case Character.GEORK:
                List<AudioClip> georkVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Geork, 74, 3);
                playerDeathVoice = georkVoices[Random.Range(0, georkVoices.Count)];
                break;
            case Character.ERIS:
                List<AudioClip> erisVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Eris, 112, 3);
                playerDeathVoice = erisVoices[Random.Range(0, erisVoices.Count)];
                break;
        }
        M_SoundManager.instance.PlayVoice(playerDeathVoice, playerDeathVoice.length);
    }
}
