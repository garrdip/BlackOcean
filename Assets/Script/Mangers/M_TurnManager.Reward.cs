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


    [Server]
    public void NoneBattleEnd()
    {
        ClearTargetObject(); // 타겟오브젝트 정리
        M_MapManager.instance.ClearPlayerVoteHexagonMapRooms(); // 방 투표 목록 비움
        M_MapManager.instance.SetRoomStateComplete(); // 방 완료상태로 변경
        M_MapManager.instance.DecreaseTotalActionCost(); // 행동비용 감소
        M_MapManager.instance.ApproachBossToPlayer(); // 보스가 플레이어에게로 이동
        StopCoroutine(ProcessMonsterDeathCoroutine());
        foreach(PlayerInterface player in PlayerRegistry.All){
            player.SetIsReadyStateDefault(); // 레디 상태 모두 확인후 다시 false 되돌림 (여러군데서 사용 예정)
            player.SetEndTurnActiveStateDefault(); // 앤드 턴 상태 모두 확인후 다시 false 되돌림
            player.SetCompleteRewardStateDefault();
        }
        foreach(HexagonMapRoom hexagonMapRoom in M_MapManager.instance.hexagonMapRooms){
            hexagonMapRoom.isSelected = false; // 맵 선택상태 모두 false 초기화
        }
        foreach(GamePlayer gamePlayer in FindObjectsByType<GamePlayer>(FindObjectsSortMode.None)){
            gamePlayer.GetComponent<GamePlayerDeck>().rewards.Clear();
            gamePlayer.GetComponent<GamePlayerDeck>().rewardCards.Clear();
        }
        EachPlayerNoneBattleEnd();
    }


    // -------------------------------------------------------------------- ClientRpc Method -----------------------------------------------------------------//

    // 전투 종료 보상 카드 팝업 호출
    [ClientRpc]
    public void RpcShowBattleResultPopUp()
    {
        // 전투 종료 음성 재생
        Character character = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.character;
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


    [ClientRpc]
    public void EachPlayerNoneBattleEnd()
    {
        M_CardManager.instance.RemoveAllCurrentPlayerCardOnHandsWithOutTrashDeck(); // 현재 플레이어 손에 있던 카드들을 삭제, 삭제 시 Trash Deck에 추가하지 않음.
        M_CardManager.instance.RemoveAllCurrentPlayerPrefareDeckAndTrashDeck(); // 플레이어의 PrefareDeck, TrashDeck 삭제
        M_CardManager.instance.ChangeAbilityButtonActiveState(false); // 어빌리티 버튼 비활성화
        ReturnToMap();
    }


    public void ReturnToMap()
    {
        string audioName = M_MapManager.instance.mapBoss == null ? "Stage_1_Map" : "Stage_1_Map_Boss_Spawn";
        AudioClip audioClip_map = M_SoundManager.instance.GetBGMClip(BGM_TYPE.Map, audioName);
        M_SoundManager.instance.PlayBGM(audioClip_map, MusicTransition.CrossFade, 2f);
        GameUIManager.instance.DoScreenChangeIn(() => {
            // 카메라 위치 리셋
            Camera.main.orthographicSize = GameUIManager.mapSceneCameraSize;

            // UI 활성화 상태 변경
            M_MapManager.instance.MapScene.SetActive(true);
            M_MapManager.instance.BattleScene.SetActive(false);
            M_MapManager.instance.BackgroundLight.GetComponent<MeshRenderer>().sortingLayerName = "Default"; // 배경 플레어 정렬 오더 변경

            // Dim배경 상태 변경
            MapUI.instance.ChangeMapDimBackground(false);
            MapUI.instance.RemoveAllMapInfoPopUps();

            GameUIManager.instance.DoScreenChangeOut();
        });
    }
}
