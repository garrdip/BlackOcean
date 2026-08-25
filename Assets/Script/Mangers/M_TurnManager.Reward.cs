using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;


// M_TurnManager partial — 전투 종료 흐름 제어 및 관련 RPC.
// 보상 분배/보상 UI 상태는 RewardService로 분리됨.
public partial class M_TurnManager
{
    [Server]
    public void BattleEnd()
    {
        RewardService.instance.DistributeBattleRewards(); // 보상 분배 (서버)
        RpcShowBattleResultPopUp(); // 전투 종료 팝업 호출
        ResetEndTurnState(); // 턴종료 상태 리셋
        cardQueueList.Clear(); // 카드 큐 Synclist 클리어
        currentCardQueueIndex = currentCardQueueInitalValue; // 카드 큐 Synclist에 사용하는 index 값 초기화
    }


    // 전투/보상 정리 후 거점 또는 스테이지(미로) 화면 복귀
    // 타겟오브젝트(아바타/몬스터) 정리는 화면이 검어진 뒤 전환 코루틴(M_TurnManager.Spawner)이 수행한다 — 여기서 지우면 페이드 전에 사라지는 과도현상이 생김
    [Server]
    public void NoneBattleEnd()
    {
        foreach(PlayerInterface player in PlayerRegistry.All){
            player.SetEndTurnActiveStateDefault(); // 앤드 턴 상태 모두 확인후 다시 false 되돌림
            player.SetCompleteRewardStateDefault();
        }
        foreach(GamePlayer gamePlayer in FindObjectsByType<GamePlayer>(FindObjectsSortMode.None)){
            gamePlayer.GetComponent<GamePlayerDeck>().rewards.Clear();
            gamePlayer.GetComponent<GamePlayerDeck>().rewardCards.Clear();
        }
        EachPlayerNoneBattleEnd();
        M_HubManager.instance.OnBattleVictory(); // 스테이지 진행 중이면 방 클리어(다음 방/스테이지 클리어 판정 + 저장), 아니면 저장 후 거점 복귀
    }


    // -------------------------------------------------------------------- ClientRpc Method -----------------------------------------------------------------//

    // 전투 종료 보상 카드 팝업 호출
    [ClientRpc]
    public void RpcShowBattleResultPopUp()
    {
        // 전투 종료 음성 재생
        Character character = PlayerRegistry.Local.currentGamePlayer.character;
        switch(character){
            case Character.HONGDANHYANG:
                List<AudioClip> battleWinVoicesDanhyang = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.HongDanHyang, 68, 3);
                AudioClip audioClipDanhyang = battleWinVoicesDanhyang[Random.Range(0, battleWinVoicesDanhyang.Count)];
                M_SoundManager.instance.PlayVoice(audioClipDanhyang, audioClipDanhyang.length);
                break;
            case Character.GEORK:
                List<AudioClip> battleWinVoicesGeork = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Geork, 80, 3);
                AudioClip audioClipGeork = battleWinVoicesGeork[Random.Range(0, battleWinVoicesGeork.Count)];
                M_SoundManager.instance.PlayVoice(audioClipGeork, audioClipGeork.length);
                break;
            case Character.ERIS:
                List<AudioClip> battleWinVoicesEris = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Eris, 123, 3);
                AudioClip audioClipEris = battleWinVoicesEris[Random.Range(0, battleWinVoicesEris.Count)];
                M_SoundManager.instance.PlayVoice(audioClipEris, audioClipEris.length);
                break;
        }
        // 전투 종료 팝업 호출
        PopUpUIManager.instance.HandleShowBattleResultPopUp();
    }


    // 전투 종료 시 클라이언트 카드 잔재 정리 (화면 전환 연출은 M_TurnManager.Spawner 전환 코루틴이 담당)
    [ClientRpc]
    public void EachPlayerNoneBattleEnd()
    {
        M_CardManager.instance.RemoveAllCurrentPlayerCardOnHandsWithOutTrashDeck(); // 현재 플레이어 손에 있던 카드들을 삭제, 삭제 시 Trash Deck에 추가하지 않음.
        M_CardManager.instance.RemoveAllCurrentPlayerPrefareDeckAndTrashDeck(); // 플레이어의 PrefareDeck, TrashDeck 삭제
        M_CardManager.instance.ChangeAbilityButtonActiveState(false); // 어빌리티 버튼 비활성화
    }
}
