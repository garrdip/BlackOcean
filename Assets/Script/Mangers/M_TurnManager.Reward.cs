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


// M_TurnManager partial — 전투 보상 및 전투 종료 처리
public partial class M_TurnManager
{

    // 소유한 모든 플레이어가 보상 카드 받았는지 체크
    public void CheckAllPlayerRewarded(GamePlayer gamePlayer)
    {
        if(!M_TurnManager.instance.playerRewardedDic.ContainsValue(false) && gamePlayer.isOwned){ // 소유한 모든 플레이어 보상받았으면 종료
            NetworkClient.localPlayer.GetComponent<PlayerInterface>().isRewardDone = true; 
            gamePlayer.GetComponent<GamePlayerDeck>().CmdClearRewardCards();
        }
    }


    // 보상 목록 오브젝트 모두 제거
    public void ClearRewardListItem()
    {
        foreach(GameObject gameObject in rewardObjects){
            Destroy(gameObject);
        }
        rewardObjects.Clear();
    }


    // 보상 목록 오브젝트 단일 제거
    public void RemoveRewardListItem(GameObject rewardObject)
    {
        M_TurnManager.instance.rewardObjects.Remove(rewardObject);
        Destroy(rewardObject);
    }


    // 보상 카드 오브젝트 제거 및 플레이어 보상 상태 데이터 정리
    public void ClearRewardCardAndPlayer()
    {
        foreach(GameObject gameObject in rewardCardObjects){
            Destroy(gameObject);
        }
        rewardCardObjects.Clear();
    }


    [Server]
    public void BattleEnd()
    {   
        // 전투 종료시 플레이어들의 캐릭터별 보상카드 랜덤추출하여 각 플레이어들에게 전달
        foreach(NetworkConnectionToClient conn in NetworkServer.connections.Values){
            PlayerInterface playerInterface = NetLookup.Server<PlayerInterface>(conn.identity.netId);
            PlayerInterfaceServer playerInterfaceServer = playerInterface.GetComponent<PlayerInterfaceServer>();
            foreach(GamePlayer gamePlayer in playerInterface.ownedPlayers){
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
                
                // TODO : 보상테이블 데이터 DB에서 조회해서 보상아이템 세팅(임시로 골드 + 카드 보상)
                string cardRewardGuid = System.Guid.NewGuid().ToString();
                gamePlayerDeck.rewards.Add(new Reward(){ netId = gamePlayer.netId, guid = cardRewardGuid, reward_Type = Reward_Type.Card });
                gamePlayerDeck.rewards.Add(new Reward(){ netId = gamePlayer.netId, guid = System.Guid.NewGuid().ToString(), reward_Type = Reward_Type.Gold, rewardGold = 10 });
                
                // 카드 보상 데이터 세팅
                int rewardCardCount = gamePlayerDeck.maxRewardCardCount; // 플레이어별로 설정된 보상 카드 최대 갯수
                List<Card> cardsByCharacter = M_CardManager.instance.cards.FindAll(card => card.baseCard.character == gamePlayer.character); // 카드매니저의 카드데이터 Synclist로부터 캐릭터별 카드 목록 추출
                if(cardsByCharacter.Count > 0){
                    for(int i = 0; i < rewardCardCount; i++){
                        int randomIndex = Random.Range(0, cardsByCharacter.Count);
                        Card rewardCard = cardsByCharacter[randomIndex].CardDeepCopy(false);
                        rewardCard.guid = cardRewardGuid;
                        gamePlayerDeck.rewardCards.Add(rewardCard);
                        cardsByCharacter.RemoveAt(randomIndex);
                    }
                }
                // 플레이어 보상 상태 데이터 세팅
                gamePlayerDeck.TargetPlayerRewarded(gamePlayerDeck.GetComponent<NetworkIdentity>().connectionToClient);

                // 플레이어의 모든 카드 데이터 제거
                gamePlayerDeck.trashDeck.Clear();
                gamePlayerDeck.prefareDeck.Clear();
                gamePlayerDeck.forgottenDeck.Clear();
                
                //코스트 리셋
                gamePlayerDeck.maxIchi = 3;
                gamePlayerDeck.currentIchi = 3;

                //해방 카드를 위한 카드 카운팅 종료
                gamePlayerDeck.numOfUsedCard = 0;

                //저주카드 획득량 제거
                gamePlayerDeck.gainCurseCardCount = 0;

                foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands){
                    NetworkServer.Destroy(cardOnHand.gameObject);
                }
                gamePlayerDeck.cardOnHands.Clear();
            }
        }
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
        foreach(PlayerInterface player in FindObjectsByType<PlayerInterface>(FindObjectsSortMode.None)){
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
        AudioClip audioClip_map = M_SoundManager.instance.bgmClips[BGM_TYPE.Map].Find((audioClip) => audioClip.name.Equals(audioName));
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
