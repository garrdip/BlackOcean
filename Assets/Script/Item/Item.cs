using System.Collections;
using System.Collections.Generic;
using ProjectD;
using Mirror;
public class Item : NetworkBehaviour
{
    [SyncVar]
    public string itemName;
    [SyncVar]
    public string itemNumber;
    [SyncVar]
    public int value;
    [SyncVar]
    public ItemGrade itemGrade;
    [SyncVar]
    public ItemEffectTime effectTime;

    public Item(){}
    public Item(string nameIn, string itemNumberIn, ItemGrade itemGradeIn, ItemEffectTime itemEffectTimeIn)
    {
        itemName = nameIn;
        itemNumber = itemNumberIn;
        itemGrade = itemGradeIn;
        effectTime = itemEffectTimeIn;
    }
}
