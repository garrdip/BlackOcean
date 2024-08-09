using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using Spine.Unity;
using Spine;

public class Boss_Apates : SpawnedMonster
{
    private List<Skin> apatesSkins = new List<Skin>(); // 아파테스 스파인 스킨 목록

    public override void Start()
    { 
        base.Start();
        skeletonAnimation.skeleton.SetSkin(GetApatesFaceSkins(1)); // 1번 얼굴 스킨 적용
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // 아파테스 BGM 재생
        AudioClip momosBGM = M_SoundManager.instance.bgmClips[BGM_TYPE.Boss].Find((audioClip) => audioClip.name.Equals("Boss_Apates"));
        M_SoundManager.instance.PlayBGM(momosBGM, MusicTransition.Swift, 1.5f);

        // 플레이어별 아파테스 조우 대화 오디오클립 조회
        if(NetworkClient.localPlayer != null){
            Character character = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.character;
            AudioClip playerVoice = null;
            AudioClip apatesVoice = null;
            switch(character){
                case Character.HONGDANHYANG:
                    apatesVoice = M_SoundManager.instance.voiceClips[VOICE_TYPE.Apates][9];
                    playerVoice = M_SoundManager.instance.voiceClips[VOICE_TYPE.HongDanHyang][156];
                    break;
                case Character.GEORK:
                    apatesVoice = M_SoundManager.instance.voiceClips[VOICE_TYPE.Apates][10];
                    playerVoice = M_SoundManager.instance.voiceClips[VOICE_TYPE.Geork][165];
                    break;
                case Character.ERIS:
                    int index = Random.Range(0, 1);
                    apatesVoice = M_SoundManager.instance.voiceClips[VOICE_TYPE.Apates][11];
                    playerVoice = M_SoundManager.instance.voiceClips[VOICE_TYPE.Eris][216];
                    // TODO : 에리스 공허상태인 경우 대화 분기 처리 
                    break;
            }
            M_SoundManager.instance.PlayVoice(apatesVoice, apatesVoice.length, true, () => { // 아파테스 대화 재생
                M_SoundManager.instance.PlayVoice(playerVoice, playerVoice.length); // 플레이어 대화 재생
            });
        }
    }

    public override void OnChangedHpValue(int oldHpValue, int newHpValue)
    {
        base.OnChangedHpValue(oldHpValue, newHpValue);
        if(newHpValue <= 0){
            int index = Random.Range(0, 1) == 0 ? 43 : 44; // Moon_44, Moon_45중 랜덤재생
            AudioClip momosDeadVoice = M_SoundManager.instance.voiceClips[VOICE_TYPE.MoonGirl][index];
            M_SoundManager.instance.PlayVoice(momosDeadVoice, momosDeadVoice.length); // 아파테스 사망시 달의소녀 나레이션 재생
        }
    }

    // [ 아파테스 얼굴 스킨 3종 중에서 해당 인덱스의 스킨 반환 ]
    // 1번 스킨 : Default(얼굴 없음)
    // 2번 스킨 : Skin1
    // 3번 스킨 : Skin2
    // 4번 스킨 : Skin3
    public Skin GetApatesFaceSkins(int index)
    {
        ExposedList<Skin> apaptesFaceSkins = skeletonAnimation.skeleton.Data.Skins;
        foreach(Skin skin in apaptesFaceSkins){
            apatesSkins.Add(skin);
        }
        return apatesSkins[index];
    }
}
