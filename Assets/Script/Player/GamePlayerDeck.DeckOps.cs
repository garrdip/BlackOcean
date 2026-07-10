using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using DG.Tweening;


// GamePlayerDeck partial — 덱 간 카드 이동/추가/제거/강화 조작
public partial class GamePlayerDeck
{

    // 카드 중 팝업을 요구하는 카드들의 경우 분기 처리하여, 해당 카드에 필요한 팝업 활성화 하는 RPC 호출
    [Server]
    public void ServerCheckCardPopUpAction(Card card)
    {
        switch(card.baseCard.cardNumber){
            case "G53": case "G53_E":
            case "H26": case "H26_E":
                TargetPopUpShowByCard(card, DeckAction.ACTION_CARDONHAND_REMOVE);
                break;
            case "H17": case "H17_E":
            case "E8": case "E8_E":
                TargetPopUpShowByCard(card, DeckAction.ACTION_DECK_DRAW);
                break;
            case "E22": case "E22_E":
            case "E25": case "E25_E":
            case "E26": case "E26_E":
            case "E32": case "E32_E":
            case "E37": case "E37_E":
            case "E40": case "E40_E":
            case "E48": case "E48_E":
            case "E52": case "E52_E":
                TargetPopUpShowByCard(card, DeckAction.ACTION_DECK_SELECT);
                break;
            case "E28": case "E28_E":
                TargetRemoveAllCardOnHands();
                TargetPopUpShowByCard(card, DeckAction.ACTION_DECK_SELECT);
            break;
            case "E44": case "E44_E":
                foreach(TargetObject targetObject in M_TurnManager.instance.spawnedPlayerList){
                    GamePlayerDeck gamePlayerDeck = targetObject.player.GetComponent<GamePlayerDeck>();
                    gamePlayerDeck.TargetPopUpShowByCard(card, DeckAction.ACTION_DECK_MULTIPLE_SELECT);
                }
                break;
        }
    }


    // from 과 to 의 DeckListType 따라 어떤 덱의 Synclist에 추가 및 제거할지 분기 처리 하여 서버 함수 호출
    [Server]
    public void ServerSendDeck(List<Card> cards, DeckListType from, DeckListType to)
    {
        switch(from){
            case DeckListType.PREFARE_DECK:
                switch (to)
                {
                    case DeckListType.TRASH_DECK:
                        foreach(Card card in cards){
                            SendDeckFromTo(prefareDeck, trashDeck, card);
                            ServerCallBackFromPrefareDeckToTrashDeck(card);
                        }
                        break;
                    case DeckListType.FORGOTTEN_DECK:
                        foreach(Card card in cards){
                            SendDeckFromTo(prefareDeck, forgottenDeck, card);
                        }
                        break;
                }
                break;

            case DeckListType.TRASH_DECK:
                switch (to)
                {
 
                    case DeckListType.PREFARE_DECK:
                        foreach(Card card in cards){
                            SendDeckFromTo(trashDeck, prefareDeck, card);
                        }
                        break;
                    case DeckListType.FORGOTTEN_DECK:
                        foreach(Card card in cards){
                            SendDeckFromTo(trashDeck, forgottenDeck, card);
                        }
                        break;
                }
                break;

            case DeckListType.FORGOTTEN_DECK:
                switch (to)
                {
                    case DeckListType.PREFARE_DECK:
                        foreach(Card card in cards){
                            SendDeckFromTo(forgottenDeck, prefareDeck, card);
                        }
                        break;
                    case DeckListType.TRASH_DECK:
                        foreach(Card card in cards){
                            SendDeckFromTo(forgottenDeck, trashDeck, card);
                        }
                        break;
                }
                break;
        }
        TargetSendDeck(cards, from, to);
    }


    // from 덱에서 특정 카드를 선택하여 제거 후 to 덱에 추가
    [Server]
    private void SendDeckFromTo(SyncList<Card> from, SyncList<Card> to, Card selectCard)
    {
        for(int i=from.Count-1; i>=0; i--){
            Card card = from[i];
            if(card.guid.Equals(selectCard.guid)){
                from.Remove(card);
                to.Add(card);
            }
        }
    }


    // 특정 덱에서 해당 카드 추출하여 제거 + 카드 생성 - Multiple Card Version
    [Server]
    public void ServerSpawnCardOnHandExtractFromDeck(List<Card> selectCards, DeckListType deckListType)
    {
        foreach(Card selectCard in selectCards){
            switch (deckListType)
            {
                case DeckListType.PREFARE_DECK:
                    for(int i=prefareDeck.Count-1; i>=0; i--){
                        Card card = prefareDeck[i];
                        if(card.guid.Equals(selectCard.guid)){
                            prefareDeck.Remove(card);
                        }
                    }
                    break;
                case DeckListType.TRASH_DECK:
                    for(int i=trashDeck.Count-1; i>=0; i--){
                        Card card = trashDeck[i];
                        if(card.guid.Equals(selectCard.guid)){
                            trashDeck.Remove(card);
                        }
                    }
                    break;
                case DeckListType.FORGOTTEN_DECK:
                    for(int i=forgottenDeck.Count-1; i>=0; i--){
                        Card card = forgottenDeck[i];
                        if(card.guid.Equals(selectCard.guid)){
                            forgottenDeck.Remove(card);
                        }
                    }
                    break;
            }
            M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
            GameObject cardOnHandObject = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")),
                Vector3.zero,
                Quaternion.identity
            );
            CardOnHand cardOnHand = cardOnHandObject.GetComponent<CardOnHand>();
            cardOnHand.card = selectCard;
            cardOnHand.card.isChargedCard = false;
            if(cardPocket != null){
                cardOnHand.parent = cardPocket.GetComponent<CardPocket>();
            }
            NetworkServer.Spawn(cardOnHandObject, connectionToClient);
            cardOnHands.Add(cardOnHand);
        }
    }


    // 특정 덱에서 해당 카드 추출하여 제거 + 카드 생성 - Single Card Version
    [Server]
    public void ServerSpawnCardOnHandExtractFromDeck(Card selectCard, DeckListType deckListType)
    {
        switch (deckListType)
        {
            case DeckListType.PREFARE_DECK:
                for(int i=prefareDeck.Count-1; i>=0; i--){
                    Card card = prefareDeck[i];
                    if(card.guid.Equals(selectCard.guid)){
                        prefareDeck.Remove(card);
                    }
                }
                break;
            case DeckListType.TRASH_DECK:
                for(int i=trashDeck.Count-1; i>=0; i--){
                    Card card = trashDeck[i];
                    if(card.guid.Equals(selectCard.guid)){
                        trashDeck.Remove(card);
                    }
                }
                break;
            case DeckListType.FORGOTTEN_DECK:
                for(int i=forgottenDeck.Count-1; i>=0; i--){
                    Card card = forgottenDeck[i];
                    if(card.guid.Equals(selectCard.guid)){
                        forgottenDeck.Remove(card);
                    }
                }
                break;
        }
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        GameObject cardOnHandObject = Instantiate(
            M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")),
            Vector3.zero,
            Quaternion.identity
        );
        CardOnHand cardOnHand = cardOnHandObject.GetComponent<CardOnHand>();
        cardOnHand.card = selectCard;
        cardOnHand.card.isChargedCard = false;
        if(cardPocket != null){
            cardOnHand.parent = cardPocket.GetComponent<CardPocket>();
        }
        NetworkServer.Spawn(cardOnHandObject, connectionToClient);
        cardOnHands.Add(cardOnHand);
    }


    // 뽑을덱에서 버린덱으로 가는 경우 카드 추가 기능 발현
    [Server]
    public void ServerCallBackFromPrefareDeckToTrashDeck(Card card)
    {
        TargetObject playerTargtObject = M_TurnManager.instance.GetPlayer(this);
        switch(card.baseCard.cardNumber){
            case "E13": case "E13_E":
                CardData.instance.E13_CallBack(playerTargtObject);
                break;
            case "E14": case "E14_E":
                CardData.instance.E14_CallBack(playerTargtObject);
                break;
            case "E26": case "E26_E":
                CardData.instance.E26_CallBack(playerTargtObject);
                break;
            case "E30": case "E30_E":
                CardData.instance.E30_CallBack(playerTargtObject);
                break;
            case "E31": case "E31_E":
                CardData.instance.E31_CallBack(playerTargtObject);
                break;
            case "E32": case "E32_E":
                CardData.instance.E32_CallBack(playerTargtObject);
                break;
            case "E37": case "E37_E":
                StartCoroutine(CardData.instance.E37_CallBack(playerTargtObject));
                break;
            case "E40": case "E40_E":
                CardData.instance.E40_CallBack(playerTargtObject, card);
                break;
            case "E48": case "E48_E":
                // TODO : 이 카드가 뽑을덱 에서 버린덱 으로 가면 피해는 N배가 증가합니다.
                break;
            case "E52": case "E52_E":
                StartCoroutine(CardData.instance.E52_CallBack(playerTargtObject));
                break;
            case "E56": case "E56_E":
                CardData.instance.E56_CallBack(playerTargtObject, card);
                break;
        }

        // 종말의 징조(SIGNOFEND): 카드가 뽑을덱→버린덱으로 갈 때마다 적 전체에게 피해 2
        if(playerTargtObject != null && playerTargtObject.HasBuff(BuffType.SIGNOFEND))
        {
            foreach(TargetObject enemy in M_TurnManager.instance.spawnedMonsterList)
                enemy.DamageToMonster(2, playerTargtObject);
        }
    }


    // ---------------------------------------------------------------------- Command Method ----------------------------------------------------------------//

    // deck에 추가
    [Command]
    public void CmdAddDeck(Card card)
    {
        deck.Add(card);
    }


    // deck에서 선택한 카드의 guid와 동일한 카드정보 조회하여 강화 카드로 교체 -> deck update callback의 OP_SET 호출됨
    [Command]
    public void CmdEnhanceDeck(string guid)
    {
        int index = deck.FindIndex(card => card.guid == guid);
        if(index != -1){
            Card card = deck[index].CardDeepCopy(false);
            card.isEnhanced = true;
            deck[index] = card;
        }
    }


    // deck에서 선택한 카드의 guid와 동일한 카드정보 조회하여 제거 -> deck update callback의 OP_REMOVEAT 호출됨
    [Command]
    public void CmdRemoveDeck(string guid)
    {
        int index = deck.FindIndex(card => card.guid == guid);
        if(index != -1){
            deck.RemoveAt(index);
        }
    }


    // prefareDeck에 추가
    [Command]
    public void CmdAddPrefareDeck(Card card)
    {
        prefareDeck.Add(card);
    }


    // prefareDeck과 TrashDeck의 모든 데이터 제거
    [Command]
    public void CmdClearPrefareDeckAndTrashDeck()
    {
        foreach(Card card in prefareDeck)
            deck.Add(card.CardDeepCopy(true));
        foreach(Card card in trashDeck)
            deck.Add(card.CardDeepCopy(true));
        prefareDeck.Clear();
        trashDeck.Clear();
    }


    // 전투 시작시 deck -> prefareDeck 으로 Card 데이터를 깊은복사 후 랜덤 셔플 수행
    [Command]
    public void CmdAddPrefareDeckWithShuffle()
    {
        foreach(Card card in deck){
            Card copyCard = card.CardDeepCopy(false);
            prefareDeck.Add(copyCard);
        }
        M_CardManager.instance.Shuffle(prefareDeck);
    }


    [Command]
    public void CmdCheckRequestCard(string requestCardNumber, List<Card> selectCards)
    {
        switch(requestCardNumber){
            case "E22": case "E22_E":
                ServerSpawnCardOnHandExtractFromDeck(selectCards, DeckListType.TRASH_DECK); // 버린 덱에서 선택하여 패로 생성
                break;
            case "E25": case "E25_E":
            case "E26": case "E26_E":
            case "E28": case "E28_E":
            case "E32": case "E32_E":
            case "E37": case "E37_E":
            case "E40": case "E40_E":
            case "E48": case "E48_E":
            case "E52": case "E52_E":
                ServerSendDeck(selectCards, DeckListType.PREFARE_DECK, DeckListType.TRASH_DECK); // 뽑을 덱에서 선택하여 버린 덱으로
                break;
        }
    }


    [Command]
    public void CmdCheckRequestCard(Card card, DeckListType deckListType)
    {
        ServerSpawnCardOnHandExtractFromDeck(card, deckListType);
    }


    // 선택 가능 카드 갯수 초기화
    [Command]
    public void CmdResetMaxCardSelectableCount()
    {
        maxSelectableCardCount = 0;
    }


    // 카드 제거(버려진 덱으로 보내는 경우)
    [Command]
    public void CmdDestroyCardOnHandToTrash(CardOnHand cardOnHand)
    {
        trashDeck.Add(cardOnHand.card);
        cardOnHands.Remove(cardOnHand);
        NetworkServer.Destroy(cardOnHand.gameObject);
    }


    // 카드 제거(잊혀진 덱으로 보내는 경우)
    [Command]
    public void CmdDestroyCardOnHandToForgotten(CardOnHand cardOnHand)
    {
        forgottenDeck.Add(cardOnHand.card);
        cardOnHands.Remove(cardOnHand);
        NetworkServer.Destroy(cardOnHand.gameObject);
    }


    // 카드 제거(버려진 or 잊혀진 댁으로 보내지 않고 제거만 수행)
    [Command]
    public void CmdDestroyAllCardOnHandWithOutTrashDeck()
    {
        foreach(CardOnHand cardOnHand in cardOnHands){
            deck.Add(cardOnHand.card.CardDeepCopy(true));
            NetworkServer.Destroy(cardOnHand.gameObject);
        }
        cardOnHands.Clear();
    }


    // 재조물 카드 콜백 커맨드
    [Command]
    public void Cmd_H26_CallBack(GamePlayer gamePlayer, int choosedCardCount)
    {
        TargetObject targetObject = M_TurnManager.instance.GetCurrentPlayerTargetObject(gamePlayer);
        CardData.instance.H26_CallBack(targetObject, choosedCardCount); // 패 제거 팝업에서 선택한 카드 갯수 넘겨줌
    }


    // 잊혀진 덱으로 카드 보내는 RPC 수신
    [TargetRpc]
    public void TargetCardOnHandRemoveToForgotenDeck(CardOnHand cardOnHand)
    {
        M_CardManager.instance.CardOnHandThrowAwaySequenceToForgotenDeck(cardOnHand);
    }


    // 플레이어의 패를 모두 버린 덱으로 보내는 트위닝 수행 후 제거 커맨드 호출
    [TargetRpc]
    public void TargetRemoveAllCardOnHands()
    {
        foreach(CardOnHand cardOnHand in cardOnHands){
            M_CardManager.instance.CardOnHandAllThrowAwaySequence(cardOnHand, this);
        }
    }


    // PopUpAction과 카드 종류에 따라 필요한 팝업 호출
    [TargetRpc]
    public void TargetPopUpShowByCard(Card card, DeckAction popUpAction)
    {
        switch(popUpAction)
        {
            case DeckAction.ACTION_DECK_DRAW:
                PopUpUIManager.instance.HandleShowDeckDrawPopUp();
                break;
            case DeckAction.ACTION_CARDONHAND_REMOVE:
                PopUpUIManager.instance.HandleShowCardOnHandRemovePopUp();
                break;
            case DeckAction.ACTION_DECK_SELECT:
                if(card.baseCard.cardNumber.Equals("E22") || card.baseCard.cardNumber.Equals("E22_E")){
                    PopUpUIManager.instance.HandleShowDeckSelectPopUp(DeckListType.TRASH_DECK, card.baseCard.cardNumber);
                }else{
                    PopUpUIManager.instance.HandleShowDeckSelectPopUp(DeckListType.PREFARE_DECK, card.baseCard.cardNumber);
                }
                break;
            case DeckAction.ACTION_DECK_MULTIPLE_SELECT:
                PopUpUIManager.instance.HandleShowDeckMultipleSelectPopUp();
                break;
        }
    }


    // from 덱에서 to 덱으로 가는 카드 충전 이펙트 수행 RPC
    [TargetRpc]
    public void TargetSendDeck(List<Card> cards, DeckListType from, DeckListType to)
    {
        Vector3 startPosition = Vector3.zero;
        Vector3 endPosition = Vector3.zero;
        switch(from){
            case DeckListType.NONE:
                startPosition = GameUIManager.instance.CardOnHandsPanel.transform.position;
                switch (to)
                {
                    case DeckListType.PREFARE_DECK:
                        endPosition = GameUIManager.instance.buttonPrefareDeck.transform.position;
                        break;
                    case DeckListType.TRASH_DECK:
                        endPosition = GameUIManager.instance.buttonTrashDeck.transform.position;
                        break;
                    case DeckListType.FORGOTTEN_DECK:
                        endPosition = GameUIManager.instance.buttonForgottenDeck.transform.position;
                        break;
                }
                break;
            case DeckListType.PREFARE_DECK:
                startPosition = GameUIManager.instance.buttonPrefareDeck.transform.position;
                switch (to)
                {
                    case DeckListType.TRASH_DECK:
                        endPosition = GameUIManager.instance.buttonTrashDeck.transform.position;
                        break;
                    case DeckListType.FORGOTTEN_DECK:
                        endPosition = GameUIManager.instance.buttonForgottenDeck.transform.position;
                        break;
                }
                break;
            case DeckListType.TRASH_DECK:
                startPosition = GameUIManager.instance.buttonTrashDeck.transform.position;
                switch (to)
                {
                    case DeckListType.PREFARE_DECK:
                        endPosition = GameUIManager.instance.buttonPrefareDeck.transform.position;
                        break;
                    case DeckListType.FORGOTTEN_DECK:
                        endPosition = GameUIManager.instance.buttonForgottenDeck.transform.position;
                        break;
                }
                break;
            case DeckListType.FORGOTTEN_DECK:
                startPosition = GameUIManager.instance.buttonForgottenDeck.transform.position;
                switch (to)
                {
                    case DeckListType.PREFARE_DECK:
                        endPosition = GameUIManager.instance.buttonPrefareDeck.transform.position;
                        break;
                    case DeckListType.TRASH_DECK:
                        endPosition = GameUIManager.instance.buttonTrashDeck.transform.position;
                        break;
                }
                break;
        }
        for(int i=0; i<cards.Count; i++){
            M_CardManager.instance.CardOnHandChargedSequence(cards[i], i, startPosition, endPosition);
        }
    }


    // 영웅 변신(위대한 자): 이치의저주(GEORK CURSE, G0~G7 계열) ↔ 영웅(Gn_H) 카드 전환 — 전투 덱(패·뽑을덱·버린덱)만 대상, 총괄 덱은 유지
    [Server]
    public void ConvertCurseHeroCards(bool toHero)
    {
        for(int i = 0; i < prefareDeck.Count; i++)
        {
            Card converted = GetConvertedCurseHeroCard(prefareDeck[i], toHero);
            if(converted != null)prefareDeck[i] = converted;
        }
        for(int i = 0; i < trashDeck.Count; i++)
        {
            Card converted = GetConvertedCurseHeroCard(trashDeck[i], toHero);
            if(converted != null)trashDeck[i] = converted;
        }
        foreach(CardOnHand cardOnHand in cardOnHands)
        {
            Card converted = GetConvertedCurseHeroCard(cardOnHand.card, toHero);
            if(converted != null)cardOnHand.card = converted; // SyncVar 재할당 — 클라이언트 카드 뷰 갱신은 훅이 처리
        }
    }


    // 전환 대상이면 baseCard만 교체한 복사본을, 아니면 null 반환 (경험치·강화 상태·특성 보존)
    private Card GetConvertedCurseHeroCard(Card card, bool toHero)
    {
        if(card == null || card.baseCard == null || card.baseCard.cardNumber == null)return null;
        string cardNumber = card.baseCard.cardNumber;
        string targetNumber;
        if(toHero)
        {
            if(card.baseCard.cardType != CardType.CURSE || !cardNumber.StartsWith("G"))return null;
            targetNumber = cardNumber.EndsWith("_E") ? cardNumber.Substring(0, cardNumber.Length - 2) + "_H_E" : cardNumber + "_H";
        }
        else
        {
            if(card.baseCard.cardType != CardType.HERO || !cardNumber.Contains("_H"))return null;
            targetNumber = cardNumber.Replace("_H", "");
        }
        CardBase targetBase = CardData.instance.cards.Find(c => c.cardNumber == targetNumber);
        if(targetBase == null)return null; // 대응 카드가 없으면 전환하지 않음
        Card converted = new Card(card);
        converted.baseCard = targetBase;
        return converted;
    }
}
