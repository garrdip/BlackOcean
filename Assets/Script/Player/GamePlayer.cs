using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    public SyncList<Artifact> artifacts = new SyncList<Artifact>();

    public SyncList<Card> deck =  new SyncList<Card>();
    
    public SyncList<Item> items = new SyncList<Item>();


    public override void OnStartLocalPlayer()
    {
        // Server Loading 종료 후 1층 데이터 생성
        if(isServer)
        {
            Debug.Log("Generate Floor");
            M_MapManager.instance.GenerateFloor();
        }
    }

    // Host, Client 시작 시 맵 UI 사용될 커스텀 MapPlayer생성해서 플레이어 참가 목록UI에 세팅
    public override void OnStartClient()
    {
        base.OnStartClient();

        if(CharacterInfoUI.instance.gamePlayerListLayout.transform != null){
            M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
            GameObject user = Instantiate(M_MapManager.instance.mapPlayerForUI);
            user.transform.SetParent(CharacterInfoUI.instance.gamePlayerListLayout.transform);
            user.transform.localScale = new Vector3(1, 1, 1);
            user.GetComponent<MapPlayerForUI>().netID =  GetComponent<NetworkIdentity>();
            user.GetComponent<MapPlayerForUI>().gamePlayer = this;

            // 카트 네트워크 오브젝트 6개 생성
            for(int i=0; i<6; i++){
                GameObject card = Instantiate(M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("Card")));
                NetworkServer.Spawn(card);
                card.GetComponent<Card>().index = i;
                card.transform.SetParent(DeckUI.instance.DeckListPanel.transform);
                card.transform.localScale = new Vector3(1, 1, 1);
            }
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
