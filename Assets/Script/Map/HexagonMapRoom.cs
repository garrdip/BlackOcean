using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;

public class HexagonMapRoom : NetworkBehaviour
{
    public SpriteRenderer spriteRenderer;

    void Start()
    {
       transform.SetParent(M_MapManager.instance.MapRooms.transform);
       transform.localPosition = new Vector3(transform.position.x, transform.position.y, 0f);
       transform.localRotation = Quaternion.Euler(0, 0f, 0f);
    }

    private void OnMouseDown()
    {
        if(isServer){
            // 클릭된 육각형의 위치 주변에 육각형 생성
            Vector3 position = new Vector3(transform.localPosition.x, transform.localPosition.y, 0f);
            M_MapManager.instance.GenerateHexagonRoom(position);
        }
        // 클릭한 육각형으로 맵플레이어 이동 및 현재 선택된 맵으로 저장
        NetworkClient.localPlayer.GetComponent<GamePlayerMap>().CmdSelectHexagonMapRoom(this, NetworkClient.connection.identity);
        NetworkClient.localPlayer.GetComponent<GamePlayerMap>().CmdChangeCurrentMapPlayerPosition(this, GetComponent<Transform>().position);
    }
}
