using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;

public class TargetObject : NetworkBehaviour
{
    public ObjectType objectType;

    // Player 의 경우 
    [SyncVar]
    public GamePlayer player;

    // Monster 의 경우
    [SyncVar]
    public Monster monster;

    public List<GameObject> characters;

    public override void OnStartClient()
    {
        Debug.Log("ON Start Client 호출");
        base.OnStartClient();
        Debug.Log("Base 메소드 종료");
        Debug.Log(player.character);
        switch(player.character)
        {
            case Character.GEORK :
                Instantiate(characters[2],transform.position,Quaternion.identity,transform);
            break;
            case Character.ERIS :
                Instantiate(characters[1],transform.position,Quaternion.identity,transform);
            break;
            case Character.HONGDANHYANG :
                Instantiate(characters[0],transform.position,Quaternion.identity,transform);
            break;
        }

    }
}
