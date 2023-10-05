using System.Collections;
using System.Collections.Generic;
using ProjectD;

[System.Serializable]
public class Item
{
    public string itemName;

    public string itemNumber;
    
    public int value;
    
    public ItemGrade itemGrade;
   
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
