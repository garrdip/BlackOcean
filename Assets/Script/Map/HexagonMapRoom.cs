using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class HexagonMapRoom : NetworkBehaviour
{
    void Start()
    {
       transform.SetParent(M_MapManager.instance.MapRooms.transform);
       transform.localPosition = new Vector3(transform.position.x, transform.position.y, 0f);
       transform.rotation = Quaternion.Euler(40f, 0f, 0f);
    }

    private void OnMouseDown()
    {
        if(isServer){
            // 클릭된 육각형의 위치 주변에 육각형 생성
            Vector3 position = new Vector3(transform.localPosition.x, transform.localPosition.y, 0f);
            M_MapManager.instance.GenerateHexagonRoom(position);
        }
    }
}
