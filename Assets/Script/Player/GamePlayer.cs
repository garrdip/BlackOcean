using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;

public class GamePlayer : NetworkBehaviour
{
    [SyncVar]
    int HP;
    [SyncVar]
    Character character;
    SyncList<Artifact> artifacts;
    SyncList<Card> deck;


}
