using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using DG.Tweening;


// GamePlayerDeck partial — 카드 드로우 및 핸드/어빌리티 카드 스폰
public partial class GamePlayerDeck
{

    [Server]
    public void ServerSpawnCardOnHand(int cardCount, System.Action<Card> cardSpawnCallback = null)
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        if(prefareDeck.Count == 0 && trashDeck.Count == 0)
        {
            CmdAddPrefareDeckWithShuffle();
        }
        // 카드 생성 초기 위치는 화면 밖
        Vector3 cardSpawnPosition = new Vector3(-100f, 0f, 0f);

        for(int i=0; i<cardCount; i++){
            ReChargePrefareDeck();
            if(prefareDeck.Count == 0){
                // 뽑을덱·버린덱이 모두 빈 극단 상황 — 그대로 인덱싱하면 예외로 Mirror가 연결을 끊으므로 남은 드로우를 건너뛴다
                Debug.LogWarning($"[GamePlayerDeck] 뽑을덱과 버린덱이 모두 비어 드로우를 중단합니다. (요청 {cardCount}장 중 {i}장 드로우됨)");
                break;
            }
            GameObject cardOnHandObject = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")),
                cardSpawnPosition,
                Quaternion.identity
            );

            int randomIndex = Random.Range(0, prefareDeck.Count);
            CardOnHand cardOnHand = cardOnHandObject.GetComponent<CardOnHand>();
            cardOnHand.card = prefareDeck[randomIndex]; // prefareDeck에서 랜덤으로 뽑아서 CardOnHand의 카드데이터에 추가
            cardOnHand.card.isChargedCard = false; // prefareDeck에 카드 충전한 이후이기 때문에 충전 플래그 변수 false로 설정
            prefareDeck.RemoveAt(randomIndex);
            if(cardPocket != null){
                cardOnHand.parent = cardPocket.GetComponent<CardPocket>(); // 소환된 CardOnHand를 CardPocket의 자식오브젝트로 설정
            }
            NetworkServer.Spawn(cardOnHandObject, connectionToClient);
            if(cardOnHand.card.baseCard.cardNumber.Contains("G57"))currentIchi++;
            cardOnHands.Add(cardOnHand); // 카드가 생성되면 자신의 권한을 가진 카드 오브젝트들 syncList에 추가
            if(cardSpawnCallback != null){
                cardSpawnCallback(cardOnHand.card);
            }
        }
    }


    [Server]
    public void GenerateCardOnHand(Card card,int count)
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        Vector3 cardSpawnPosition = new Vector3(0f, -100f, 0f);
        for(int i = 0 ; i < count ; i ++)
        {
            GameObject cardOnHandObject = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")),
                cardSpawnPosition,
                Quaternion.identity
            );
            CardOnHand cardOnHand = cardOnHandObject.GetComponent<CardOnHand>();
            cardOnHand.card = card;
            if(cardPocket != null){
                cardOnHand.parent = cardPocket.GetComponent<CardPocket>(); // 소환된 CardOnHand를 CardPocket의 자식오브젝트로 설정
            }
            NetworkServer.Spawn(cardOnHandObject, connectionToClient);
            cardOnHands.Add(cardOnHand); // 카드가 생성되면 자신의 권한을 가진 카드 오브젝트들 syncList에 추가
        }
    }


    // 뽑을 덱에서 랜덤으로 카드 뽑아 addtionDrawCards에 추가
    [Server]
    public void AddDrawCard(int cardCount)
    {
        for(int i=0; i<cardCount; i++){
            ReChargePrefareDeck();
            if(prefareDeck.Count == 0){
                Debug.LogWarning($"[GamePlayerDeck] 뽑을덱과 버린덱이 모두 비어 추가 드로우를 중단합니다. (요청 {cardCount}장 중 {i}장 드로우됨)");
                break;
            }
            int randomIndex = Random.Range(0, prefareDeck.Count);
            addtionDrawCards.Add(prefareDeck[randomIndex]);
            prefareDeck.RemoveAt(randomIndex);
        }
    }


    // 현재 플레이어의 CardOnHand 오브젝트 생성
    // prefareDeck에서 랜덤으로 가져옴. prefareDeck이 0개일 경우 trashDeck에서 가져온뒤 뽑음
    [Command]
    public void CmdSpawnCardOnHand()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        if(prefareDeck.Count == 0 && trashDeck.Count == 0)
        {
            CmdAddPrefareDeckWithShuffle();
        }
        // 카드 생성 초기 위치는 화면 밖
        Vector3 cardSpawnPosition = new Vector3(-100f, 0f, 0f);

        for(int i=0; i<currentDeckCount; i++){
            ReChargePrefareDeck();
            if(prefareDeck.Count == 0){
                // 뽑을덱·버린덱이 모두 빈 극단 상황 — 그대로 인덱싱하면 예외로 Mirror가 호스트 연결을 끊어 이후 드로우가 영구 중단되므로 남은 드로우를 건너뛴다
                Debug.LogWarning($"[GamePlayerDeck] 뽑을덱과 버린덱이 모두 비어 드로우를 중단합니다. (요청 {currentDeckCount}장 중 {i}장 드로우됨)");
                break;
            }
            GameObject cardOnHandObject = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")),
                cardSpawnPosition,
                Quaternion.identity
            );

            int randomIndex = Random.Range(0, prefareDeck.Count);
            CardOnHand cardOnHand = cardOnHandObject.GetComponent<CardOnHand>();
            cardOnHand.card = prefareDeck[randomIndex]; // prefareDeck에서 랜덤으로 뽑아서 CardOnHand의 카드데이터에 추가
            cardOnHand.card.isChargedCard = false; // prefareDeck에 카드 충전한 이후이기 때문에 충전 플래그 변수 false로 설정
            prefareDeck.RemoveAt(randomIndex);
            if(cardPocket != null){
                cardOnHand.parent = cardPocket.GetComponent<CardPocket>(); // 소환된 CardOnHand를 CardPocket의 자식오브젝트로 설정
            }
            NetworkServer.Spawn(cardOnHandObject, connectionToClient);
            if(cardOnHand.card.baseCard.cardNumber.Contains("G57"))currentIchi++;
            cardOnHands.Add(cardOnHand); // 카드가 생성되면 자신의 권한을 가진 카드 오브젝트들 syncList에 추가
        }
    }


    // 추가 드로우 카드들을 생성하여 패로 이동. 인자값인 card는 팝업창에서 선택한 카드(중력 부여할 카드), index는 선택한 카드의 인덱스값
    [Command]
    public void CmdSpawnAddtionDrawCard(Card card, int index)
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        List<CardOnHand> cards = new List<CardOnHand>();
        for(int i=0; i<addtionDrawCards.Count; i++){
            GameObject cardOnHandObject = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")),
                Vector3.zero,
                Quaternion.identity
            );
            CardOnHand cardOnHand = cardOnHandObject.GetComponent<CardOnHand>();
            cardOnHand.card = addtionDrawCards[i];
            cardOnHand.index = i;
            cardOnHand.isAddtionDrawCard = true;
            if(cardPocket != null){
                cardOnHand.parent = cardPocket.GetComponent<CardPocket>();
            }
            NetworkServer.Spawn(cardOnHandObject, connectionToClient);
            if(cardOnHand.card.baseCard.cardNumber.Contains("G57"))currentIchi++;
            cardOnHands.Add(cardOnHand);
            if(i != index) cards.Add(cardOnHand);
            else cards.Insert(0,cardOnHand);
        }
        CardData.instance.cardSelectCallBack(this,cards);
        addtionDrawCards.Clear();
    }


    // 추가 드로우된 카드의 isAddtionDrawCard 상태값을 변경(패에 있는 CardOnHand와 추가 드로우된 CardOnHand를 구분하기 위한 상태값)
    [Command]
    public void CmdChangeCardOnHandIsAddtionDraw(CardOnHand cardOnHand, bool isAddtion)
    {
        cardOnHand.isAddtionDrawCard = isAddtion;
    }


    [Command]
    public void CmdSpawnAbilityCard()
    {
        Vector3 cardSpawnPosition = new Vector3(-100f, 0f, 0f);
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        Card abilityCardBase = new Card();
        switch(GetComponent<GamePlayer>().character)
        {
            case Character.HONGDANHYANG :
                abilityCardBase = new Card(CardData.instance.cards.Find(c => c.cardNumber.Equals("HA"))); 
                break;
            case Character.ERIS :
                abilityCardBase = new Card(CardData.instance.cards.Find(c => c.cardNumber.Equals("HA"))); 
                break;
            case Character.GEORK :
                abilityCardBase = new Card(CardData.instance.cards.Find(c => c.cardNumber.Equals("HA"))); 
                break;
        }
        GameObject abilityCardObject = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")),
                cardSpawnPosition,
                Quaternion.identity
        );
        NetworkServer.Spawn(abilityCardObject, connectionToClient);
        abilityCardObject.GetComponent<CardOnHand>().card = abilityCardBase;
        abilityCardObject.GetComponent<CardOnHand>().RpcCardOnHandSetParent(gameObject);
        abilityCard = abilityCardObject.GetComponent<CardOnHand>();
    }


    // ------------------------------------------------- Rpc Method ---------------------------------------------------//

    [ClientRpc]
    public void SpawnAbilityCardRPC()
    {
        if(isOwned)CmdSpawnAbilityCard();
    }


    [ClientRpc]
    public void ReturnToCardOnHand(CardOnHand cardOnHand)
    {
        if(isOwned)
            StartCoroutine(ReturnToCardOnHandCoroutine(cardOnHand));
    }
}
