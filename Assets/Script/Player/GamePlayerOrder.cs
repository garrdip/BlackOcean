using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 게임씬 좌측 상단의 게임 참가자들의 정보
public class GamePlayerOrder : MonoBehaviour
{
    public GameObject playerOwnMenu; // 플레이어별 정보 메뉴
    public GameObject playerCommonMenu; // 플레이어 공통 메뉴

    public GameObject playerItemPrefab; // 플레이어 아이템 프리팹
    public List<GameObject> playerItems; // 플레이어 아이템 목록

    public GridLayoutGroup gridLayoutGroup;
    public Button buttonPlayerOrder;
    public TextMeshProUGUI textPlayerName;
    public TextMeshProUGUI costCount;

   
    void Start()
    {
        InitPlayerItems();
    }

    // [TEMP]플레이어 아이템 목록 초기화
    public void InitPlayerItems()
    {
        for(int i=0; i<10; i++){
            GameObject playerItem = Instantiate(playerItemPrefab, Vector3.zero, Quaternion.identity);
            playerItem.transform.SetParent(gridLayoutGroup.transform);
            playerItem.transform.localScale = new Vector3(1f, 1f, 1f);
        }
    }
}
