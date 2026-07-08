using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;

public class Boss_Geras : SpawnedMonster
{
    public override void OnStartClient()
    {
        base.OnStartClient();

        // 게라스 BGM 재생
        AudioClip momosBGM = M_SoundManager.instance.GetBGMClip(BGM_TYPE.Boss, "Boss_Geras");
        M_SoundManager.instance.PlayBGM(momosBGM, MusicTransition.Swift, 1.5f);

        // 플레이어별 게라스 조우 대화 오디오클립 조회
        if(NetworkClient.localPlayer != null){
            Character character = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.character;
            AudioClip playerVoice = null;
            AudioClip gerasVoice = null;
            switch(character){
                case Character.HONGDANHYANG:
                    gerasVoice = M_SoundManager.instance.GetVoiceClipAt(VOICE_TYPE.Geras, 9);
                    playerVoice = M_SoundManager.instance.GetVoiceClipAt(VOICE_TYPE.HongDanHyang, 158);
                    break;
                case Character.GEORK:
                    gerasVoice = M_SoundManager.instance.GetVoiceClipAt(VOICE_TYPE.Geras, 10);
                    playerVoice = M_SoundManager.instance.GetVoiceClipAt(VOICE_TYPE.Geork, 167);
                    break;
                case Character.ERIS:
                    int index = Random.Range(0, 1);
                    gerasVoice = M_SoundManager.instance.GetVoiceClipAt(VOICE_TYPE.Geras, 11);
                    playerVoice = M_SoundManager.instance.GetVoiceClipAt(VOICE_TYPE.Eris, 220);
                    // TODO : 에리스 공허상태인 경우 대화 분기 처리 
                    break;
            }
            M_SoundManager.instance.PlayVoice(gerasVoice, gerasVoice.length, true, () => { // 게라스 대화 재생
                M_SoundManager.instance.PlayVoice(playerVoice, playerVoice.length); // 플레이어 대화 재생
            });
        }
    }

    public override void OnChangedHpValue(int oldHpValue, int newHpValue)
    {
        base.OnChangedHpValue(oldHpValue, newHpValue);
        if(newHpValue <= 0){
            int index = Random.Range(0, 1) == 0 ? 47 : 48; // Moon_48, Moon_49중 랜덤재생
            AudioClip momosDeadVoice = M_SoundManager.instance.GetVoiceClipAt(VOICE_TYPE.MoonGirl, index);
            M_SoundManager.instance.PlayVoice(momosDeadVoice, momosDeadVoice.length); // 게라스 사망시 달의소녀 나레이션 재생
        }
    }
}
