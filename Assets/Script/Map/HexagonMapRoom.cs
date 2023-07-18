using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;

public class HexagonMapRoom : NetworkBehaviour
{
    [SyncVar (hook = nameof(OnChangeRegion))]
    public Region region;

    public SpriteRenderer spriteRenderer;

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

    public void OnChangeRegion(Region oldValue, Region newValue)
    {
        switch(newValue.regionGrade)
        {
            case RegionGrade.NONE:
                spriteRenderer.color = Color.white;
                break;
            case RegionGrade.NORMAL:
                spriteRenderer.color = Color.red;
                break;
            case RegionGrade.RARE:
                spriteRenderer.color = Color.green;
                break;
            case RegionGrade.UNIQUE:
                spriteRenderer.color = Color.blue;
                break;
            case RegionGrade.LEGEND:
                spriteRenderer.color = Color.yellow;
                break;
        }
    }
}
