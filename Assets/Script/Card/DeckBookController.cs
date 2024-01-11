using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnlimitedScrollUI;

public class DeckBookController : MonoBehaviour
{
    public GameObject cell;
    public IUnlimitedScroller unlimitedScroller;
    public ScrollRect scrollRect;

    void Awake()
    {
        unlimitedScroller = GetComponent<IUnlimitedScroller>();
    }

    public void GetCardDataFromDatabase()
    {
        int totalCount = CardData.instance.cards.Count;
        unlimitedScroller.Generate(cell, totalCount, (index, iCell) => {
            var regularCell = iCell as RegularCell;
            if (regularCell != null){
                regularCell.onGenerated?.Invoke(index);
            }
        });
        unlimitedScroller.JumpTo(0, JumpToMethod.Center); // Cell 생성하면 항상 맨 위로 이동
    }
}
