using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;

public class GamePlayer : NetworkBehaviour
{
    [SyncVar]
    public int HP;

    [SyncVar]
    public int MaxHP = 0;

    [SyncVar]
    public Character character;

    [SyncVar]
    public bool isInitializeDone = false;

    [SyncVar (hook = nameof(OnChangedSelectOrder))]
    public int selectOrder = 0;

    [SyncVar (hook = nameof(OnChangeCurrentDeckCount))]
    public int currentDeckCount = 0;

    [SyncVar]
    public bool isArrowSpawned = false; // 화살표는 한개만 생성되어야하므로 이미 생성되어 있는지 체크용 변수

    public const int arrowNodeNum = 13; // 카드 컨트롤 화살표 몸통 개수

    public const int defaultCardOnHandCount = 10; // 카드 오브젝트 기본 개수

    public const int maxCardOnHandCount = 12; // 카드 오브젝트 최대 개수

    public readonly SyncList<Artifact> artifacts = new SyncList<Artifact>();

    public readonly SyncList<Card> deck =  new SyncList<Card>(); // 카드 총량(시작 8개)

    public readonly  SyncList<Card> prefareDeck =  new SyncList<Card>(); // 뽑을 카드(카드 총량에서 내 손에 있는 카드(5개)를 제외한 그 나머지 개수)
    
    public readonly SyncList<Item> trashDeck = new SyncList<Item>(); // 버릴 카드(사용된 카드 + 턴 종료될때 내 손에 있는 카드)

    public readonly SyncList<CardOnHand> cardOnHands = new SyncList<CardOnHand>(); // 실제 컨트롤 하는 플레이어 소유의 카드 네트워크 오브젝트 리스트

    public void SetOrderByUI(int num)
    {
        if(isLocalPlayer)
            selectOrder = num;
    }

    public void OnChangedSelectOrder(int oldVal,int newVal)
    {
        if(isServer)
            M_TurnManager.instance.OnChangedPlayerOrder();
    }

    public void OnChangeCurrentDeckCount(int oldCount, int newCount)
    {
        Debug.Log("현재 댁 갯수 변경 :" + newCount);
    }
    
    public override void OnStartLocalPlayer()
    {
        // Server Loading 종료 후 1층 데이터 생성
        if(isServer)
        {
            M_MapManager.instance.GenerateFloor();
        }
        if(isLocalPlayer)
        {
            SetInitialValue();
            isInitializeDone = true;
            Debug.Log("다른 플레이어 기다림 시작!");
            StartCoroutine(nameof(WaitPlayerList));
        }
    }

    public void SetInitialValue()
    {
        if(isOwned){
            currentDeckCount = 5;
            switch(character){
            case Character.GEORK:
                for(int i = 0 ; i <8 ;i++)
                {
                    if(i % 2 == 0){
                        Card attackCard = CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("Normal_Attack"));
                        deck.Add(attackCard);
                        prefareDeck.Add(attackCard);
                    }else{
                        Card defenseCard = CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("Normal_Defense"));
                        deck.Add(defenseCard);
                        prefareDeck.Add(defenseCard);
                    }
                    
                }
                break;
            case Character.ERIS:
                for(int i = 0 ; i <8 ;i++)
                {
                    if(i % 2 == 0){
                        Card attackCard = CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("Normal_Attack"));
                        deck.Add(attackCard);
                        prefareDeck.Add(attackCard);
                    }else{
                        Card defenseCard = CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("Normal_Heal"));
                        deck.Add(defenseCard);
                        prefareDeck.Add(defenseCard);
                    }
                    
                }
                break;
            case Character.HONGDANHYANG:
                for(int i = 0 ; i <8 ;i++)
                {
                    if(i % 2 == 0){
                        Card attackCard = CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("Normal_Attack"));
                        deck.Add(attackCard);
                        prefareDeck.Add(attackCard);
                    }else{
                        Card defenseCard = CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("Normal_Defense"));
                        deck.Add(defenseCard);
                        prefareDeck.Add(defenseCard);
                    }
                    
                }
                break;
            }
        }
    }

    IEnumerator WaitPlayerList()
    {
        M_NetworkRoomManager netManger = NetworkRoomManager.singleton as M_NetworkRoomManager;
        //GamePlayer가 모두 로드 될때까지 기다림
        while(true)
        {
            GamePlayer[] users = FindObjectsOfType<GamePlayer>();
            if(users.Length == netManger.roomSlots.Count) break;
            yield return new WaitForSeconds(0.01f);
        }
        //GamePlayer가 모두 Initial Value 초기화 될때까지 기다림
        while(true)
        {
            int cnt = 0;
            GamePlayer[] users = FindObjectsOfType<GamePlayer>();
            foreach(GamePlayer user in users)
            {
                if(user.isInitializeDone) cnt++;
            }
            if(cnt == netManger.roomSlots.Count) break;
            yield return new WaitForSeconds(0.01f);
        }
        SetUserStatusUI();
        M_TurnManager.instance.SetOrderButtonListener();
        // 플레이어 로딩이 끝나면 턴매니저로 플레이어 리스트를 전달함
        if(isServer)
            M_TurnManager.instance.InitiateGamePlayerList();
    }

    public void SetUserStatusUI()
    {
        GamePlayer[] users = FindObjectsOfType<GamePlayer>();
        //자신의 UI를 최상단에 표시
        foreach( GamePlayer user in users )
        {
            if(user.isLocalPlayer)
            {
                GameObject userUI = Instantiate(M_MapManager.instance.mapPlayerForUI);
                userUI.transform.SetParent(CharacterInfoUI.instance.gamePlayerListLayout.transform);
                userUI.transform.localScale = new Vector3(1, 1, 1);
                userUI.GetComponent<MapPlayerForUI>().netID =  user.GetComponent<NetworkIdentity>();
                userUI.GetComponent<MapPlayerForUI>().gamePlayer = user;
            }
        }
        foreach( GamePlayer user in users )
        {
            if(!user.isLocalPlayer)
            {
                GameObject userUI = Instantiate(M_MapManager.instance.mapPlayerForUI);
                userUI.transform.SetParent(CharacterInfoUI.instance.gamePlayerListLayout.transform);
                userUI.transform.localScale = new Vector3(1, 1, 1);
                userUI.GetComponent<MapPlayerForUI>().netID =  user.GetComponent<NetworkIdentity>();
                userUI.GetComponent<MapPlayerForUI>().gamePlayer = user;
            }
        }
    }

    // 카드 생성 요청
    [Command]
    public void CmdSpawnCardOnHand()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        
        GameObject cardPocket = Instantiate(
            M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardPocket")),
            new Vector3(-20f, 0f, 0f),
            Quaternion.identity
        );
        NetworkServer.Spawn(cardPocket, connectionToClient);

        for(int i=0; i<currentDeckCount; i++){
            // 대칭 위치값 계산
            int leftCount = (currentDeckCount - 1) / 2;
            int rightCount = currentDeckCount - leftCount - 1;
            float symmetryPosition = (currentDeckCount % 2 == 0) ? ((i - leftCount) * 1.5f - 0.75f) : ((i - leftCount) * 1.5f + 0f);
            
            // 위치값(카드 개수에 따라 좌우 대칭값 계산하여 각 카드의 x, y 좌표 설정)
            Vector3 position = new Vector3(symmetryPosition - 20f, -Mathf.Abs(symmetryPosition) * 0.15f, 0f);

            // 회전값
            Quaternion rotation = Quaternion.Euler(0f, 0f, -symmetryPosition);

            // 네트워크 오브젝트 생성. 초기 위치는 화면에서 벗어난 x좌표 -20
            GameObject cardOnHand = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")),
                position,
                rotation
            );

            NetworkServer.Spawn(cardOnHand, connectionToClient);
            cardOnHand.GetComponent<CardOnHand>().index = i;

            cardPocket.GetComponent<CardPocket>().cards.Add(cardOnHand.GetComponent<CardOnHand>());
            cardOnHand.GetComponent<CardOnHand>().card = prefareDeck[i];
            cardOnHand.GetComponent<CardOnHand>().card.isTargetable = i % 2 == 0 ? true : false;

            RpcSpawnCardOnHand(
                cardOnHand.GetComponent<CardOnHand>(),
                cardPocket.GetComponent<CardPocket>()
            );
        }
    }

    // 카드가 생성되면 자신의 권한을 가진 카드 오브젝트들 syncList에 추가
    [ClientRpc]
    public void RpcSpawnCardOnHand(CardOnHand cardOnHand, CardPocket cardPocket)
    {
        if(cardOnHand.isOwned){
            cardOnHands.Add(cardOnHand);
        }
        cardOnHand.gameObject.transform.SetParent(cardPocket.transform);
    }

    // 카드 컨트롤 화살표 인디케이터 생성(네트워크 오브젝트)
    [Command]
    public void CmdSpawnArrowEmitter(Vector3 cardPosition)
    {
        if(!isArrowSpawned){
            M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
            // 화살표 인디케이터 오브젝트 생성
            GameObject cardEmitter = Instantiate(M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("ArrowEmitter")));
            NetworkServer.Spawn(cardEmitter, connectionToClient);
            cardEmitter.GetComponent<CardCtrlArrow>().RpcArrowInit(cardPosition);

            // 화살표 인디케이터 몸체 생성
            for(int i=0; i<arrowNodeNum; i++){
                GameObject arrowNode = Instantiate(M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("ArrowNode")));
                NetworkServer.Spawn(arrowNode, connectionToClient);
                cardEmitter.GetComponent<CardCtrlArrow>().RpcSetArrowNode(arrowNode);
            }

            // 화살표 인디케이터 머리 생성
            GameObject arrowHead = Instantiate(M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("ArrowHead")));
            NetworkServer.Spawn(arrowHead, connectionToClient);
            cardEmitter.GetComponent<CardCtrlArrow>().RpcSetArrowHead(arrowHead);
            isArrowSpawned = true;
        }
    }

    // 카드 컨트롤 화살표 인디케이터 제거(네트워크 오브젝트)
    [Command]
    public void CmdDestroyArrowEmitter(GameObject cardEmitter)
    {
        NetworkServer.Destroy(cardEmitter);
        isArrowSpawned = false;
    }
}
