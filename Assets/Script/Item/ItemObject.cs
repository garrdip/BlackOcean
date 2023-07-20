using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;

public class ItemObject : NetworkBehaviour
{
    [SyncVar]
    public ItemType itemType;
    [SyncVar]
    public int value;
    [SyncVar]
    public Item baseItem;
}
