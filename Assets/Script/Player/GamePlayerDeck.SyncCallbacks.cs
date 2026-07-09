using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using DG.Tweening;


// GamePlayerDeck partial — SyncVar 훅 및 SyncList 콜백
public partial class GamePlayerDeck
{

    // -------------------------------------------------SyncVar Hooks ---------------------------------------------------//

    public void OnChangeCurrentDeckCount(int oldCount, int newCount)
    {
        Debug.Log("현재 댁 갯수 변경 :" + newCount);
    }


    public void OnChangeMaxRemoveCardCount(int oldCount, int newCount)
    {
        choosedCardOnHands = new CardOnHand[newCount]; // 패 제거 팝업에서 사용할 배열의 크기 초기화
        if(isOwned){
            // 패 제거 팝업에서 카드들의 위치값을 잡아주는 슬롯오브젝트 생성. 갯수 변경되면 모두 지우고 변경된 갯수만큼 생성
            CardOnHandRemovePopUp cardOnHandRemovePopUp = PopUpUIManager.instance.cardOnHandRemovePopUp.GetComponent<CardOnHandRemovePopUp>();
            foreach(GameObject removeCardSlot in cardOnHandRemovePopUp.removeCardSlots){
                Destroy(removeCardSlot);
            }
            cardOnHandRemovePopUp.removeCardSlots.Clear();
            for(int i=0; i<newCount; i++){
                GameObject removeCardSlot = Instantiate(PopUpUIManager.instance.RemoveCardSlotPrefab);
                removeCardSlot.transform.SetParent(cardOnHandRemovePopUp.gridLayoutGroup.transform);
                removeCardSlot.transform.localPosition = Vector3.zero;
                removeCardSlot.transform.localScale = Vector3.one;
                cardOnHandRemovePopUp.removeCardSlots.Add(removeCardSlot);
            }
        }
    }


    public void PreviousCardTypeChanged(CardType oldVal, CardType newVal)
    {
        foreach(CardOnHand cardOnHand in cardOnHands)
            if(!cardOnHand.isMoving)cardOnHand.CardInfoChangedEvent?.Invoke();
    }


    public void OnChangedNumberOfUsedCard(int oldVal, int newVal)
    {
        foreach(CardOnHand cardOnHand in cardOnHands)
            cardOnHand.OnChangeCardData(cardOnHand.card,cardOnHand.card);
    }


    // -------------------------------------------------SyncList Callback ---------------------------------------------------//
    
    // Deck Callback
    void OnDeckUpdated(SyncList<Card>.Operation op, int index, Card oldVal, Card newVal)
    {
        switch (op)
        {
            case SyncList<Card>.Operation.OP_REMOVEAT:
                if(PopUpUIManager.instance.isCardRemovePopUpOpen){
                    CardRemovePopUp cardRemovePopUp = PopUpUIManager.instance.cardRemovePopUp.GetComponent<CardRemovePopUp>();
                    cardRemovePopUp.ClearRemoveableCards();
                    cardRemovePopUp.CreateRemoveableCards();
                }
                break;
            case SyncList<Card>.Operation.OP_SET:
                if(PopUpUIManager.instance.isCardEnhancePopUpOpen){
                    CardEnhancePopUp cardEnhancePopUp = PopUpUIManager.instance.cardEnhancePopUp.GetComponent<CardEnhancePopUp>();
                    cardEnhancePopUp.ClearAllEnhanceableCards();
                    cardEnhancePopUp.CreateEnhanceableCards();
                }
                break;
        }
    }


    // CardOnHand Callback
    void OnCardOnHandsUpdated(SyncList<CardOnHand>.Operation op, int index, CardOnHand oldCardOnHand, CardOnHand newCardOnHand)
    {
        switch (op)
        {
            case SyncList<CardOnHand>.Operation.OP_ADD:
                if(newCardOnHand.isAddtionDrawCard){
                    M_CardManager.instance.CardOnHandAdditionDrawSequence(newCardOnHand, index, this);
                }else{
                    if(newCardOnHand.transform.position.x < 0){
                        M_CardManager.instance.CardOnHandDrawSequence(newCardOnHand, index);
                    }else{
                        StartCoroutine(CardOnHandDrawSequenceFromTrashDeckCoroutine(newCardOnHand, index));
                    }    
                }
                if(newCardOnHand.card.baseCard.cardType == CardType.CURSE)gainCurseCardCount++;
                break;
        }
        M_CardManager.instance.RefreshCardOnHandsSortingOrder(cardOnHands); // CardOnHand 리스트 값이 변경될 때 마다 CardOnHand의 정렬값 재설정
    }


    // 뽑을 덱 리스트 콜백
    void OnPrefareDeckUpdated(SyncList<Card>.Operation op, int index, Card oldPrefareDeck, Card newPrefareDeck)
    {
        uint currentGamePlayerNetId = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId;
        switch (op)
        {
            case SyncList<Card>.Operation.OP_ADD:
                if(newPrefareDeck.baseCard.cardType == CardType.CURSE){
                    gainCurseCardCount++;
                }
                // 현재 플레이하는 캐릭터의 카드 충전 이펙트 실행
                if(isOwned && currentGamePlayerNetId == GetComponent<GamePlayer>().netId && newPrefareDeck.isChargedCard){
                    Vector3 startPosition = GameUIManager.instance.buttonTrashDeck.transform.position;
                    Vector3 endPosition = GameUIManager.instance.buttonPrefareDeck.transform.position;
                    M_CardManager.instance.CardOnHandChargedSequence(newPrefareDeck, index, startPosition, endPosition);
                }
                if(GetComponent<GamePlayer>().netId == currentGamePlayerNetId){
                    int count = prefareDeck.Count;
                    prefareDeckCountDelay++;
                    GameUIManager.instance.textPrefareDeckCount.transform.DOScale(1.2f, prefareDeckCountDelay * duration).OnComplete(() => {
                        GameUIManager.instance.DeckButtonScaleAnimation(GameUIManager.instance.buttonPrefareDeck);
                        GameUIManager.instance.textPrefareDeckCount.text = count.ToString();
                        prefareDeckCountDelay--;
                    });
                }
                break;
            case SyncList<Card>.Operation.OP_REMOVEAT:
                if(GetComponent<GamePlayer>().netId == currentGamePlayerNetId){
                    int count = prefareDeck.Count;
                    prefareDeckCountDelay++;
                    GameUIManager.instance.textPrefareDeckCount.transform.DOScale(1.2f, prefareDeckCountDelay * duration).OnComplete(() => {
                        GameUIManager.instance.textPrefareDeckCount.text = count.ToString();
                        prefareDeckCountDelay--;
                    });
                }
                break;
            case SyncList<Card>.Operation.OP_CLEAR:
                if(GetComponent<GamePlayer>().netId == currentGamePlayerNetId){        
                    GameUIManager.instance.textPrefareDeckCount.text = "0";
                }
                break;
        }
    }


    // 버린 덱 리스트 콜백
    void OnTrashDeckUpdated(SyncList<Card>.Operation op, int index, Card oldTrashDeck, Card newTrashDeck)
    {
        uint currentGamePlayerNetId = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId;
        switch (op)
        {
            case SyncList<Card>.Operation.OP_ADD:
                if(isOwned && currentGamePlayerNetId == GetComponent<GamePlayer>().netId){
                    int count = trashDeck.Count;
                    trashDeckCountDelay++;
                    GameUIManager.instance.textTrashDeckCount.transform.DOScale(1.2f, trashDeckCountDelay * duration).OnComplete(() => {
                        GameUIManager.instance.DeckButtonScaleAnimation(GameUIManager.instance.buttonTrashDeck);
                        GameUIManager.instance.textTrashDeckCount.text = count.ToString();
                        trashDeckCountDelay--;
                    });
                }
                break;
            case SyncList<Card>.Operation.OP_REMOVEAT:
                 if(isOwned && currentGamePlayerNetId == GetComponent<GamePlayer>().netId){
                    int count = trashDeck.Count;
                    trashDeckCountDelay++;
                    GameUIManager.instance.textTrashDeckCount.transform.DOScale(1.2f, trashDeckCountDelay * duration).OnComplete(() => {
                        GameUIManager.instance.textTrashDeckCount.text = count.ToString();
                        trashDeckCountDelay--;
                    });
                }
                break;
            case SyncList<Card>.Operation.OP_CLEAR:
                if(GetComponent<GamePlayer>().netId == currentGamePlayerNetId){
                    GameUIManager.instance.textTrashDeckCount.text = "0";
                }
                break;
        }
    }


    // 잊혀진 덱 리스트 콜백
    void OnForgottenDeckUpdated(SyncList<Card>.Operation op, int index, Card oldVal, Card newVal)
    {
        uint currentGamePlayerNetId = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId;
        switch (op)
        {
            case SyncList<Card>.Operation.OP_ADD:
                if(isOwned && currentGamePlayerNetId == GetComponent<GamePlayer>().netId){
                    int count = forgottenDeck.Count;
                    forgottenDeckCountDelay++;
                    GameUIManager.instance.textForgottenDeckCount.transform.DOScale(1.2f, forgottenDeckCountDelay * duration).OnComplete(() => {
                        GameUIManager.instance.DeckButtonScaleAnimation(GameUIManager.instance.buttonForgottenDeck);
                        GameUIManager.instance.textForgottenDeckCount.text = count.ToString();
                        forgottenDeckCountDelay--;
                    });
                }
                break;
            case SyncList<Card>.Operation.OP_REMOVEAT:
                if(isOwned && currentGamePlayerNetId == GetComponent<GamePlayer>().netId){
                    int count = forgottenDeck.Count;
                    forgottenDeckCountDelay++;
                    GameUIManager.instance.textForgottenDeckCount.transform.DOScale(1.2f, forgottenDeckCountDelay * duration).OnComplete(() => {
                        GameUIManager.instance.textForgottenDeckCount.text = count.ToString();
                        forgottenDeckCountDelay--;
                    });
                }
                break;
            case SyncList<Card>.Operation.OP_CLEAR:
                if(GetComponent<GamePlayer>().netId == currentGamePlayerNetId){
                    GameUIManager.instance.textForgottenDeckCount.text = "0";
                }
                break;
        }
    }


    // 전체 보상 리스트 콜백
    void OnRewardUpdated(SyncList<Reward>.Operation op, int index, Reward oldVal, Reward newVal)
    {
        switch (op)
        {
            case SyncList<Reward>.Operation.OP_ADD:
                BattleResultPopUp battleResultPopUp = PopUpUIManager.instance.battleResultPopUp.GetComponent<BattleResultPopUp>();
                GamePlayer gamePlayer = GetComponent<GamePlayer>();
                int orderIndex = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == gamePlayer.netId);          
                GameObject rewardListItemObject = Instantiate(PopUpUIManager.instance.RewardListItemPrefab);
                RewardListItem rewardListItem = rewardListItemObject.GetComponent<RewardListItem>();
                rewardListItem.reward = newVal;
                rewardListItem.rewardOwner = gamePlayer;
                rewardListItem.transform.SetParent(battleResultPopUp.rewardLayoutGroups[orderIndex].transform);
                rewardListItem.transform.localScale = new Vector3(1, 1, 1);
                RewardService.instance.rewardObjects.Add(rewardListItemObject);
                break;
            case SyncList<Reward>.Operation.OP_REMOVEAT:
                if(isOwned && rewards.Count <= 0){
                    // 더 보상받을 데이터 없는 경우 보상완료상태 세팅
                    RewardService.instance.playerRewardedDic[GetComponent<GamePlayer>()] = true;
                    RewardService.instance.CheckAllPlayerRewarded(GetComponent<GamePlayer>());
                }
                break;
        }
    }


    // 보상 카드 리스트 콜백
    void OnRewardCardUpdated(SyncList<Card>.Operation op, int index, Card oldVal, Card newVal)
    {
        switch (op)
        {
            case SyncList<Card>.Operation.OP_ADD:
                BattleResultPopUp battleResultPopUp = PopUpUIManager.instance.battleResultPopUp.GetComponent<BattleResultPopUp>();
                GamePlayer gamePlayer = GetComponent<GamePlayer>();

                // 보상 카드 오브젝트 생성
                GameObject cardOnDeck = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab);
                cardOnDeck.GetComponent<CardOnDeck>().card = newVal;
                cardOnDeck.GetComponent<CardOnDeck>().cardOwner = gamePlayer;
                
                int orderIndex = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == gamePlayer.netId);
                if(orderIndex != -1){
                    cardOnDeck.transform.SetParent(battleResultPopUp.rewardCardLayoutGroups[orderIndex].transform);
                    cardOnDeck.transform.localScale = new Vector3(1, 1, 1);
                    battleResultPopUp.SetTabButtonIconByClass(gamePlayer.character, orderIndex);
                    if(isOwned){
                        battleResultPopUp.ChangeTab(orderIndex);
                        battleResultPopUp.tabButtons[orderIndex].gameObject.SetActive(isOwned);
                    }
                }
                RewardService.instance.rewardCardObjects.Add(cardOnDeck);
                break;
        }
    }


    // 상점 카드 리스트 콜백
    void OnShopCardUpdated(SyncList<Card>.Operation op, int index, Card oldVal, Card newVal)
    {
        switch (op)
        {
            case SyncList<Card>.Operation.OP_SET:
                MercuriusPopUp mercuriusPopUp = PopUpUIManager.instance.mercuriusPopUp.GetComponent<MercuriusPopUp>();
                if(PopUpUIManager.instance.isMercuriusPopUpOpen){
                    mercuriusPopUp.RemoveShopCards();
                    mercuriusPopUp.CreateShopCards();
                }
                break;
        }
    }
}
