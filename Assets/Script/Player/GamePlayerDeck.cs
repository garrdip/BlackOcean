using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using DG.Tweening;


public partial class GamePlayerDeck : NetworkBehaviour
{

    [SyncVar (hook = nameof(OnChangeCurrentDeckCount))]
    public int currentDeckCount = 0; // 현재 플레이어의 카드 카운트

    [SyncVar]
    public int maxShopCardCount = 0; // 상점에서 구매 가능한 카드 최대 갯수

    [SyncVar]
    public int maxRewardCardCount = 0; // 전투 보상 팝업에서 선택 가능한 카드 최대 갯수

    [SyncVar (hook = nameof(OnChangeMaxRemoveCardCount))]
    public int maxRemoveCardCount = 0; // 패 제거 팝업에서 선택 가능한 카드 최대 갯수

    [SyncVar]
    public int maxSelectableCardCount = 0; // DeckSelect 팝업에서 선택 가능한 카드 최대 갯수

    [SyncVar]
    public CardPocket cardPocket; // 현재 플레이어 소유의 카드 포켓 오브젝트

    [SyncVar]
    public CardCtrlArrow cardCtrlArrow; // 현재 소환된 카드 화살표

    [SyncVar]
    public AbilityButton abilityButton; // 현재 소환된 어빌리티 버튼

    [SyncVar]
    public AbilityCtrlArrow abilityCtrlArrow; // 현재 소환된 어빌리티 화살표

    [SyncVar]
    public CardOnHand abilityCard; // 현재 소환된 어빌리티 카드

    public readonly SyncList<Card> deck =  new SyncList<Card>(); // 덱 총괄 데이터

    public readonly SyncList<Card> prefareDeck =  new SyncList<Card>(); // 뽑을 덱
    
    public readonly SyncList<Card> trashDeck = new SyncList<Card>(); // 버린 덱

    public readonly SyncList<Card> forgottenDeck = new SyncList<Card>(); // 잊혀진 덱 찰나로 보내진 덱

    public readonly SyncList<CardOnHand> cardOnHands = new SyncList<CardOnHand>(); // 패 카드 오브젝트 리스트

    public readonly SyncList<Reward> rewards = new SyncList<Reward>(); // 전투 보상 전체 목록
    
    public readonly SyncList<Card> rewardCards = new SyncList<Card>(); // 전투 보상 카드

    public readonly SyncList<Card> addtionDrawCards = new SyncList<Card>(); // 추가 드로우 카드

    public readonly SyncList<Card> shopCards = new SyncList<Card>(); // 상점 카드

    public int currentIndex = 0; // 패 제거 팝업에서 삭제하기 위해 선택된 카드들의 인덱스(순환용)

    public CardOnHand[] choosedCardOnHands;  // 패 제거 팝업에서 삭제하기 위해 선택된 카드 오브젝트들을 담을 배열

    public Queue<(CardOnHand,TargetObject)> serverCardPredictQueue = new Queue<(CardOnHand, TargetObject)>();// Server에서 Card Queue 관리를 위한 Queue

    [SyncVar(hook = nameof(PreviousCardTypeChanged))]
    public CardType previousCardType;

    public List<CardOnHand> destroyCardList = new List<CardOnHand>();

    public int numOfUsedIronTeeth = 0;

    [SyncVar(hook = nameof(OnChangedNumberOfUsedCard))]
    public int numOfUsedCard = 0;

    [SyncVar]
    public int AdditionalSizeOfIromDemon; //철귀 영구 크기 증가

    [SyncVar]
    public string usedCardName;

    [SyncVar]
    public int gainCurseCardCount = 0;

    private readonly float duration = 0.05f;
    
    private int prefareDeckCountDelay = 0;
    
    private int trashDeckCountDelay = 0;
    
    private int forgottenDeckCountDelay = 0;

    public enum DeckAction {
        ACTION_DECK_DRAW,
        ACTION_DECK_SELECT,
        ACTION_DECK_MULTIPLE_SELECT,
        ACTION_CARDONHAND_REMOVE
    }


    public override void OnStartServer()
    {
        SetInitialValue();
        StartCoroutine(EnQueueCardTargetPair());
        StartCoroutine(ServerDestroyCardOnHand()); 
    }

    public override void OnStartClient()
    {
        deck.Callback += OnDeckUpdated;
        cardOnHands.Callback += OnCardOnHandsUpdated;
        prefareDeck.Callback += OnPrefareDeckUpdated;
        trashDeck.Callback += OnTrashDeckUpdated;
        forgottenDeck.Callback += OnForgottenDeckUpdated;
        rewards.Callback += OnRewardUpdated;
        rewardCards.Callback += OnRewardCardUpdated;
        shopCards.Callback += OnShopCardUpdated;
        if(isOwned){
            GameUIManager.instance.currentIchiText.text = currentIchi.ToString(); // 현재 이치값 초기 뷰 세팅
            GameUIManager.instance.maxIchiText.text = maxIchi.ToString(); // 최대 이치값 초기 뷰 세팅
            GameUIManager.instance.textPrefareDeckCount.text = prefareDeck.Count.ToString();
            GameUIManager.instance.textTrashDeckCount.text = trashDeck.Count.ToString();
            GameUIManager.instance.textForgottenDeckCount.text = forgottenDeck.Count.ToString();
        } 
        // 플레이어별 뽑을덱, 버린덱, 잊혀진덱 팝업 오브젝트 생성 및 Synclist Callback 이벤트 등록 (팝업의 활성화 여부와 상관없이 덱 리스트 정보 동기화 목적) 
        prefareDeck.Callback += PopUpUIManager.instance.CreatePrefareDeckListPopUp(netId).OnPrefareDeckUpdated;
        trashDeck.Callback += PopUpUIManager.instance.CreateTrashDeckListPopUp(netId).OnTrashDeckUpdated;
        forgottenDeck.Callback += PopUpUIManager.instance.CreateForgottenDeckListPopUp(netId).OnForgottenDeckUpdated;
    }

    // 화살표 주인 카드 참조값 설정
    public void SetArrowOwnCardOnHand(CardOnHand cardOnHand)
    {
        cardCtrlArrow.arrowOwnedCardOnHand = cardOnHand;
    }

    // choosedCardOnHands 배열에 선택한 카드를 추가
    public void AddChoosedCardOnHands(CardOnHand cardOnHand)
    {
        cardOnHand.isChoosed = true;
        // 인덱스 순환 반복
        currentIndex = (currentIndex + 1) % choosedCardOnHands.Length;
        for(int i=0; i<choosedCardOnHands.Length; i++){
            if(choosedCardOnHands[i] == null){
                currentIndex = i;
                break;
            }
        }
        if(choosedCardOnHands[currentIndex] != null){
            M_CardManager.instance.ResetCardAllState(choosedCardOnHands[currentIndex], false);
        }
        choosedCardOnHands[currentIndex] = cardOnHand;
        M_CardManager.instance.CardOnHandChooseForRemoveSequence(cardOnHand, currentIndex);
    }

    // choosedCardOnHands 배열에 선택한 카드를 제거
    public void RemoveChoosedCardOnHands(CardOnHand cardOnHand)
    {
        for(int i = 0; i < choosedCardOnHands.Length; i++){
            if(choosedCardOnHands[i] == cardOnHand){
                choosedCardOnHands[i] = null;
            }
        }
        cardOnHand.isChoosed = false;
        M_CardManager.instance.ResetCardAllState(cardOnHand, false);
    }

    // ---------------------------------------------------------------------- Server Method -----------------------------------------------------------------//
    
    // 플레이어 댁 정보 초기화
    [Server]
    public void SetInitialValue()
    {
        SetInitialIchi();
        currentDeckCount = BalanceData.Get("DRAW_COUNT_PER_TURN", 5);
        maxShopCardCount = BalanceData.Get("SHOP_CARD_MAX", 6);
        maxRewardCardCount = BalanceData.Get("REWARD_CARD_MAX", 3);
        maxRemoveCardCount = BalanceData.Get("REMOVE_CARD_MAX", 3);
        if(M_SaveManager.instance.isSaveGame)
        {
            foreach(SaveDataPlayer saveDataPlayer in M_SaveManager.instance.loadData.players)
            {
                if(saveDataPlayer == null)break;
                if(saveDataPlayer.ownerSteamId == GetComponent<GamePlayer>().objectOwner.steamID)
                {
                    foreach(Card card in saveDataPlayer.cards)
                    {
                        Card savedCard = card;
                        deck.Add(savedCard);
                    }
                }
            }
        }
        else
        {
            Character character = GetComponent<GamePlayer>().character;
            switch(character){
                case Character.GEORK:
                    //for(int i = 0; i < 8; i++)
                    //{
                    //    if(i % 2 == 0){
                    //        Card attackCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("G1_H")));
                    //        deck.Add(attackCard);
                    //    }else{
                    //        Card defenseCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("G0")));
                    //        deck.Add(defenseCard);
                    //    }   
                    //}
                    break;
                case Character.ERIS:
                    //for(int i = 0; i < 8; i++)
                    //{
                    //    if(i % 2 == 0){
                    //        Card attackCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("E0")));
                    //        deck.Add(attackCard);
                    //    }else{
                    //        Card defenseCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("E1")));
                    //        deck.Add(defenseCard);
                    //    }
                    //}
                    break;
                case Character.HONGDANHYANG:
                    //for(int i = 0; i < 10; i++)
                    //{
                        //Card attackCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("H"+(i+2))));
                        //deck.Add(attackCard);
                    //    if(i % 2 == 0){
                    //        Card attackCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("H1")));
                    //        deck.Add(attackCard);
                    //    }else{
                    //        Card defenseCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("H41")));
                    //        deck.Add(defenseCard);
                    //    }
                    //    
                    //   
                    //}
                    // Card additional = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("H34")));
                    //        deck.Add(additional);

                    break;
                default:
                    break;
            }
        }
    }
    
    public int GetTotalCostOfCardOnHand(CardOnHand cardOnHand)
    {
        int totalCost;
        // 고행 III(G2) 디버프의 비용 +1은 "이 카드를 제외한" 모든 카드에 적용 — G2 자신은 제외
        bool isGohang3Card = cardOnHand.card.baseCard.cardNumber == "G2" || cardOnHand.card.baseCard.cardNumber == "G2_E";
        totalCost = cardOnHand.card.baseCard.cost + cardOnHand.card.costAddition + (!isGohang3Card && GetComponent<GamePlayerTarget>().GetTargetObject().buffs.FindIndex(x => x.type == BuffType.GOHANG3_DEBUFF) != -1 ? 1 : 0);

        if(cardOnHand.card.baseCard.cardCharacteristics.Exists(x => x == CardCharacteristic.EUNHASOO)) // 은하수 카드 코스트 계산
        {
            if(cardOnHand.card.baseCard.cardType == previousCardType)
                totalCost -= 1;
            else
                totalCost += 1;
        }
        
        if(cardOnHand.card.baseCard.cardCharacteristics.Exists(x => x == CardCharacteristic.HEBANG))
            totalCost -= numOfUsedCard;
        if(GetComponent<GamePlayerTarget>().GetTargetObject().buffs.FindIndex(x => x.type == BuffType.GOHANG3) != -1)totalCost = 1;
        return (totalCost < 0) ? 0 : totalCost;
    }

    [Server]
    IEnumerator EnQueueCardTargetPair()
    {
        // TargetObject List 구조 : 
        /*
            Index : 내용
            0 : 카드 사용한 Player 
            1 : Target Monster
            이후 : 모든 플레이어 및 몬스터
        */
        WaitForSeconds loopTime = new WaitForSeconds(0.01f);
        CardOnHand cardOnHand;
        TargetObject targetObject;

        while(true)
        {
            yield return loopTime; // 0.01s

            if(serverCardPredictQueue.Count == 0) continue; //카드큐가 비어있을경우 스킵 
            
            (cardOnHand,targetObject) = serverCardPredictQueue.Dequeue(); // Command가 왔기때문에 Dequeue하여 판단

            int totalCost = GetTotalCostOfCardOnHand(cardOnHand);

            if(totalCost > currentIchi) // 카드 코스트 계산 하는곳
            {
                ReturnToCardOnHand(cardOnHand);
                continue;
            }
            if(cardOnHand.card.baseCard.isTargetable && targetObject == null)
            {
                ReturnToCardOnHand(cardOnHand);
                continue;
            }
            currentIchi -= totalCost ;

            // 여기부터 카드사용이 확정 되는곳
            previousCardType = cardOnHand.card.baseCard.cardType;
           
            List<TargetObject> targetObjects = new List<TargetObject>();
            targetObjects.Add(M_TurnManager.instance.GetPlayer(this)); // Index 0 
            if(cardOnHand.card.baseCard.isTargetable)targetObjects.Add(targetObject);// Index 1 // TargetAble이 아닐경우 Index1은 비워짐
            targetObjects.AddRange(M_TurnManager.instance.GetPlayerObjects());
            targetObjects.AddRange(M_TurnManager.instance.GetMonsterObjects());

            M_TurnManager.instance.cardTargetPairQueue.Enqueue((this, totalCost, cardOnHand, targetObjects)); // 큐에 데이터 추가

            M_TurnManager.instance.AddCardQueueList(cardOnHand, this.netId); // 카드 큐 Synclist에 데이터 추가
        }
    }

    [Server]
    IEnumerator ServerDestroyCardOnHand()
    {
        while(true)
        {
            PlayerInterface playerInterface = GetComponent<GamePlayer>().objectOwner;
            yield return new WaitForSeconds(0.01f);
            for(int i = 0 ;i < destroyCardList.Count ; i++)
            {
                CardOnHand cardOnHand = destroyCardList[i];
                if(playerInterface.destroyCards.FindIndex(x => x == cardOnHand) != -1)
                { 
                    playerInterface.RemoveDestroyCardList(cardOnHand);
                    bool isChalna = CardData.instance.CheckCardCharacteristic(cardOnHand.card, CardCharacteristic.CHALNA);
                    if(isChalna){
                        forgottenDeck.Add(cardOnHand.card);
                    }else{
                        trashDeck.Add(cardOnHand.card);
                    }
                    cardOnHands.Remove(cardOnHand);
                    destroyCardList.Remove(cardOnHand);
                    while(true)
                    {
                        if(playerInterface.destroyCards.FindIndex(x => x == cardOnHand) == -1)
                            break;
                        yield return new WaitForSeconds(0.01f);
                    }
                    NetworkServer.Destroy(cardOnHand.gameObject);
                    ServerCheckCardPopUpAction(cardOnHand.card);
                }
            }
        }
    }

    // CardOnHand 생성 시 뽑을 덱이 비어있는 경우 버린 덱에서 뽑을 덱으로 충전
    // 주의: 버린덱까지 비어 있으면 충전되지 않으므로, 호출부는 호출 후 prefareDeck.Count == 0 검사 필수
    [Server]
    public void ReChargePrefareDeck()
    {
        if(prefareDeck.Count == 0){
            while(trashDeck.Count != 0){
                Card card = trashDeck[0];
                card.isChargedCard = true;
                trashDeck.RemoveAt(0);
                prefareDeck.Add(card);
            }
        }
    }
    
    IEnumerator ReturnToCardOnHandCoroutine(CardOnHand cardOnHand)
    {
        if(cardOnHand != null){
            PlayerInterface playerInterface = PlayerRegistry.Local;
            while(true)
            {
                if(playerInterface.destroyCards.FindIndex(x => x == cardOnHand) != -1)
                {
                    cardOnHand.isUsed = false; // 요기 널오류
                    M_CardManager.instance.ResetCardAllState(cardOnHand,false);
                    playerInterface.destroyCards.Remove(cardOnHand);
                    break;
                }
                yield return new WaitForSeconds(0.01f);
            }
        }
    }

    IEnumerator CardOnHandDrawSequenceFromTrashDeckCoroutine(CardOnHand cardOnHand, int index)
    {
        while(true)
        {
            yield return new WaitForSeconds(0.01f);
            if(true)
            {
                M_CardManager.instance.CardOnHandDrawSequenceFromTrashDeck(cardOnHand, index);
                break;
            }
        }
    }
}
