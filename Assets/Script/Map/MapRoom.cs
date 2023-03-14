using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MapRoom : NetworkBehaviour
{
    [SyncVar]
    public int number;
}
