using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using Spine.Unity;
using Spine.Unity.Examples;
using Gpm.Ui;
using AYellowpaper.SerializedCollections;
using System.Linq;


// M_TurnManager partial — 전투 연출 (BGM/보이스/토스트/애니메이션 RPC)
public partial class M_TurnManager
{

    // 이벤트 방 대화 재생
    public void PlayEventConversation(bool isPositive)
    {
        AudioClip eventVoice = null;
        Character character = PlayerRegistry.Local.currentGamePlayer.character;
        switch(character){
            case Character.HONGDANHYANG:
                List<AudioClip> danhyangVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.HongDanHyang, isPositive ? 86 : 92, 3);
                eventVoice = danhyangVoices[Random.Range(0, danhyangVoices.Count)];
                break;
            case Character.GEORK:
                List<AudioClip> georkVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Geork, isPositive ? 98 : 104, 3);
                eventVoice = georkVoices[Random.Range(0, georkVoices.Count)];
                break;
            case Character.ERIS:
                List<AudioClip> erisVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Eris, isPositive ? 144 : 150, 3);
                eventVoice = erisVoices[Random.Range(0, erisVoices.Count)];
                break;
        }
        M_SoundManager.instance.PlayVoice(eventVoice, eventVoice.length);
    }


    // 페이즈 상태 텍스트 업데이트
    [ClientRpc]
    void RpcChangePhase(BattleTurn phase)
    {
        GameUIManager.instance.textCurrentPhase.text = phase.ToString();
    }


    // 전투에 필요한 카드 준비 요청
    [ClientRpc]
    void RpcCardPrefareForBattle()
    {
        M_CardManager.instance.PrefareCardWithSuffle(); // 카드데이터 셔플 수행후 PrefareDeck에 추가
        M_CardManager.instance.ChangeAbilityButtonActiveState(true); // 어빌리티 버튼 활성화
    }

 
    // 보스전 시작 수신 이벤트
    [ClientRpc]
    public void RpcStartBossBattleEvent()
    {
        AudioClip stageStartAudio = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "stage_start");
        M_SoundManager.instance.PlaySFX(stageStartAudio, stageStartAudio.length);
        M_MessageManager.instance
            .MakeToast()
            .Position(ToastPosition.Top)
            .FadeInTime(1f)
            .FadeOutTime(1f)
            .MessageBoxColor(ColorUtils.HexToColor("#E700FF"))
            .TextColor(Color.white)
            .Text("전투 : 보스")
            .Show();
    }


    // 일반 몬스터 혹은 엘리트전 시작 수신 이벤트
    [ClientRpc]
    public void RpcStartBattleEvent(RoomType roomType)
    {
        AudioClip stageStartAudio = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "stage_start");
        M_SoundManager.instance.PlaySFX(stageStartAudio, stageStartAudio.length);
        Character character = PlayerRegistry.Local.character; // 로컬 플레이어가 선택한 캐릭터 조회
        switch(roomType)
        {
            case RoomType.MONSTER:
                // 토스트 메시지 표시
                M_MessageManager.instance
                    .MakeToast()
                    .Position(ToastPosition.Top)
                    .FadeInTime(1f)
                    .FadeOutTime(1f)
                    .MessageBoxColor(Color.red)
                    .TextColor(Color.white)
                    .Text("전투 : 일반 몬스터")
                    .Show();  
                
                // BGM 재생     
                string audioName = Random.Range(0, 2) == 0 ? "Monster_Battle_N_1" : "Monster_Battle_N_2";
                AudioClip audioClip_monster_n = M_SoundManager.instance.GetBGMClip(BGM_TYPE.Battle, audioName);
                M_SoundManager.instance.PlayBGM(audioClip_monster_n, MusicTransition.Swift, 1.5f);

                // 캐릭터별 일반 몬스터 전투 음성대화 재생
                switch(character){
                    case Character.HONGDANHYANG:
                        List<AudioClip> danhyangVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.HongDanHyang, 98, 3);
                        AudioClip danhyangNormalBattleVoice = danhyangVoices[Random.Range(0, danhyangVoices.Count)];
                        M_SoundManager.instance.PlayVoice(danhyangNormalBattleVoice, danhyangNormalBattleVoice.length);
                        break;
                    case Character.GEORK:
                        List<AudioClip> georkVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Geork, 110, 3);
                        AudioClip georkNormalBattleVoice = georkVoices[Random.Range(0, georkVoices.Count)];
                        M_SoundManager.instance.PlayVoice(georkNormalBattleVoice, georkNormalBattleVoice.length);
                        break;
                    case Character.ERIS:
                        List<AudioClip> erisVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Eris, 156, 3);
                        AudioClip erisNormalBattleVoice = erisVoices[Random.Range(0, erisVoices.Count)];
                        M_SoundManager.instance.PlayVoice(erisNormalBattleVoice, erisNormalBattleVoice.length);
                        break;
                }
                break;

            case RoomType.ELITE:
                // 토스트 메시지 표시
                M_MessageManager.instance
                    .MakeToast()
                    .Position(ToastPosition.Top)
                    .FadeInTime(1f)
                    .FadeOutTime(1f)
                    .MessageBoxColor(Color.red)
                    .TextColor(Color.white)
                    .Text("전투 : 엘리트 몬스터")
                    .Show();
               
                // BGM 재생            
                AudioClip audioClip_monster_e = M_SoundManager.instance.GetBGMClip(BGM_TYPE.Battle, "Monster_Battle_E");
                M_SoundManager.instance.PlayBGM(audioClip_monster_e, MusicTransition.Swift, 1.5f);

                // 캐릭터별 엘리트 몬스터 전투 음성대화 재생
                switch(character){
                    case Character.HONGDANHYANG:
                        List<AudioClip> danhyangVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.HongDanHyang, 12, 3);
                        AudioClip danhyangEliteBattleVoice = danhyangVoices[Random.Range(0, danhyangVoices.Count)];
                        M_SoundManager.instance.PlayVoice(danhyangEliteBattleVoice, danhyangEliteBattleVoice.length);
                        break;
                    case Character.GEORK:
                        List<AudioClip> georkVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Geork, 12, 3);
                        AudioClip georkEliteBattleVoice = georkVoices[Random.Range(0, georkVoices.Count)];
                        M_SoundManager.instance.PlayVoice(georkEliteBattleVoice, georkEliteBattleVoice.length);
                        break;
                    case Character.ERIS:
                        List<AudioClip> erisVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Eris, 13, 3);
                        AudioClip erisEliteBattleVoice = erisVoices[Random.Range(0, erisVoices.Count)];
                        M_SoundManager.instance.PlayVoice(erisEliteBattleVoice, erisEliteBattleVoice.length);
                        break;
                }
                break;
        }
    }


    // 엔피씨 방문 수신 이벤트
    [ClientRpc]
    public void RpcStartNoneBattleEvent(RoomType roomType)
    {
        switch(roomType)
        {
            case RoomType.EVENT_POSITIIVE:
                M_MessageManager.instance
                    .MakeToast()
                    .Position(ToastPosition.Top)
                    .FadeInTime(1f)
                    .FadeOutTime(1f)
                    .MessageBoxColor(Color.yellow)
                    .TextColor(Color.white)
                    .Text("긍정적 이벤트")
                    .Show();
                AudioClip audioClip_event_positive = M_SoundManager.instance.GetBGMClip(BGM_TYPE.Event, "Positive_Event");
                M_SoundManager.instance.PlayBGM(audioClip_event_positive, MusicTransition.Swift, 1.5f);
                PlayEventConversation(true);
                break;
            case RoomType.EVENT_NEGATIVE:
                M_MessageManager.instance
                    .MakeToast()
                    .Position(ToastPosition.Top)
                    .FadeInTime(1f)
                    .FadeOutTime(1f)
                    .MessageBoxColor(Color.yellow)
                    .TextColor(Color.white)
                    .Text("부정적 이벤트")
                    .Show();
                AudioClip audioClip_event_negative = M_SoundManager.instance.GetBGMClip(BGM_TYPE.Event, "Negative_Event");
                M_SoundManager.instance.PlayBGM(audioClip_event_negative, MusicTransition.Swift, 1.5f);
                PlayEventConversation(false);
                break;
            case RoomType.CAMP:
                M_MessageManager.instance
                    .MakeToast()
                    .Position(ToastPosition.Top)
                    .FadeInTime(1f)
                    .FadeOutTime(1f)
                    .MessageBoxColor( Color.green)
                    .TextColor(Color.white)
                    .Text("전초기지")
                    .Show();
                // 전초기지 배경음 재생
                AudioClip audioClip_base_camp = M_SoundManager.instance.GetBGMClip(BGM_TYPE.Event, "Base_Camp");
                M_SoundManager.instance.PlayBGM(audioClip_base_camp, MusicTransition.Swift, 1.5f);
                break;
            case RoomType.CARD_NPC:
                M_MessageManager.instance
                    .MakeToast()
                    .Position(ToastPosition.Top)
                    .FadeInTime(1f)
                    .FadeOutTime(1f)
                    .MessageBoxColor(Color.magenta)
                    .TextColor(Color.white)
                    .Text("카드 상점")
                    .Show();
                // 카드 상점 배경음 재생                    
                AudioClip audioClip_card_hop = M_SoundManager.instance.GetBGMClip(BGM_TYPE.Event, "Card_Shop");
                M_SoundManager.instance.PlayBGM(audioClip_card_hop, MusicTransition.Swift, 1.5f);
                break;
            case RoomType.ITEM_NPC:
                M_MessageManager.instance
                    .MakeToast()
                    .Position(ToastPosition.Top)
                    .FadeInTime(1f)
                    .FadeOutTime(1f)
                    .MessageBoxColor(Color.blue)
                    .TextColor(Color.white)
                    .Text("아이템 상점")
                    .Show();                
                // 아이템 상점 배경음 재생
                AudioClip audioClip_item_hop = M_SoundManager.instance.GetBGMClip(BGM_TYPE.Event, "Item_Shop");
                M_SoundManager.instance.PlayBGM(audioClip_item_hop, MusicTransition.Swift, 1.5f);            
                break;
        }
    }


    [ClientRpc]
    public void StartAnimation(TargetObject tar, int trackIndex,string animationName, bool loop )
    {
        if(tar != null)
        {
            SkeletonAnimation anim = tar.avatar.GetComponent<SkeletonAnimation>();
            Spine.TrackEntry track = anim.state.SetAnimation(trackIndex,animationName,loop);
            track.MixBlend = Spine.MixBlend.Replace;
        }
    }
}
