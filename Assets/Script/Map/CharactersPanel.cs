using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CharactersPanel : MonoBehaviour
{
    public GameObject prefabMapPlayer;

    void Awake()
    {
        StartCoroutine(nameof(LoadMapPlayer));
    }

    IEnumerator LoadMapPlayer()
    {
        // Player 정보 업데이트 끝날때까지 기다림 임의로 1초 !!수정필요!!
        yield return new WaitForSeconds(1f);
        GamePlayer[] players = FindObjectsOfType<GamePlayer>();
        foreach(GamePlayer gamePlayer in players)
        {
            GameObject user = Instantiate(prefabMapPlayer,transform);
            user.GetComponent<MapPlayerLoader>().netID = gamePlayer.netIdentity;
        }

    }

}
