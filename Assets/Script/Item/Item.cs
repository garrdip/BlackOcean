using System.Collections;
using System.Collections.Generic;
using ProjectD;
public class Item
{
    public string name;
    public string itemNumber;
    public int value;
    public ItemGrade itemGrade;
    public ItemEffectTime effectTime;

    public Item(){}
    public Item(string nameIn, string itemNumberIn, ItemGrade itemGradeIn, ItemEffectTime itemEffectTimeIn)
    {
        name = nameIn;
        itemNumber = itemNumberIn;
        itemGrade = itemGradeIn;
        effectTime = itemEffectTimeIn;
    }
}
