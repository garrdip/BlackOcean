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

    public string description;

    public Item(){}

    public Item(string nameIn, string itemNumberIn, ItemGrade itemGradeIn, ItemEffectTime itemEffectTimeIn, int valueIn, string descriptionIn)
    {
        itemName = nameIn;
        itemNumber = itemNumberIn;
        itemGrade = itemGradeIn;
        effectTime = itemEffectTimeIn;
        value = valueIn;
        description = descriptionIn;
    }

    // DB의 원본은 템플릿으로 유지하고, 플레이어에게 지급할 때는 복사본을 만든다 (SyncList 직렬화·개별 수치 변동 대비)
    public Item(Item src)
    {
        itemName = src.itemName;
        itemNumber = src.itemNumber;
        itemGrade = src.itemGrade;
        effectTime = src.effectTime;
        value = src.value;
        description = src.description;
    }
}
