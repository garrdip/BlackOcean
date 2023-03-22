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
    public int defaultCardOnHandCount = 10;

    public int maxCardOnHandCount = 12;

    [SyncVar (hook = nameof(OnChangedSelectOrder))]
    public int selectOrder = 0;

    public readonly SyncList<Artifact> artifacts = new SyncList<Artifact>();

    public readonly SyncList<Card> deck =  new SyncList<Card>();
    
    public readonly SyncList<Item> items = new SyncList<Item>();

    public readonly SyncList<CardOnHand> cardOnHands = new SyncList<CardOnHand>();

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
        for(int i = 0 ; i < 8 ;i++)
        {
            Card initialCard = new Card(){name  = "i"};
            deck.Add(initialCard);
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
        for(int i=0; i<maxCardOnHandCount; i++){
            // 네트워크 오브젝트 생성. 초기 위치는 화면에서 벗어난 x좌표 -20
            M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
            GameObject cardOnHand = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")),
                new Vector3(-20f, 0f, 0f),
                Quaternion.identity
            );
            //cardOnHand.GetComponent<CardOnHand>().isTargetAble = maxCardOnHandCount % 2 == 0 ? true : false;
            cardOnHand.GetComponent<CardOnHand>().index = i;
            NetworkServer.Spawn(cardOnHand, connectionToClient);
            RpcSpawnCardOnHand(cardOnHand.GetComponent<CardOnHand>());
        }
    }

    // 카드가 생성되면 자신의 권한을 가진 카드 오브젝트들 syncList에 추가
    [ClientRpc]
    public void RpcSpawnCardOnHand(CardOnHand cardOnHand)
    {
        if(cardOnHand.isOwned){
            cardOnHands.Add(cardOnHand);
        }
    }

    // 카드 컨트롤 화살표 인디케이터 생성(네트워크 오브젝트)
    [Command]
    public void CmdSpawnArrowEmitter(Vector3 cardPosition)
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        GameObject cardEmitter = Instantiate(M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("ArrowEmitter")));
        NetworkServer.Spawn(cardEmitter);
        cardEmitter.transform.SetParent(DeckUI.instance.DeckListPanel.transform);
        cardEmitter.transform.localScale = new Vector3(1, 1, 1);
        cardEmitter.transform.position = cardPosition;
    }

    // 카드 컨트롤 화살표 인디케이터 제거(네트워크 오브젝트)
    [Command]
    public void CmdDestroyArrowEmitter(GameObject cardEmitter)
    {
        NetworkServer.Destroy(cardEmitter);
    }
}
