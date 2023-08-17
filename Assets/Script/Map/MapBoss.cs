using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class MapBoss : NetworkBehaviour
{

    [SyncVar (hook = nameof(OnChangedBossPosition))]
    public Vector3 bossPosition; // 맵에서 보스 위치

    [SyncVar]
    public Vector2Int coordinate; // 고유 좌표계


    void Start()
    {
        transform.SetParent(M_MapManager.instance.MapRooms.transform);
        transform.localPosition = new Vector3(transform.position.x, transform.position.y, 0f);
        transform.localRotation = Quaternion.Euler(0, 0f, 0f);
    }


    // 보스의 위치 변경 수신
    public void OnChangedBossPosition(Vector3 oldPosition, Vector3 newPosition)
    {
        transform.localPosition = newPosition;
    }
}
