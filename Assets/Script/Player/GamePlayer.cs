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
    public Character character;

    public SyncList<Artifact> artifacts = new SyncList<Artifact>();

    public SyncList<Card> deck =  new SyncList<Card>();


}
