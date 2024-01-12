using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using UnlimitedScrollUI;


public class DeckBookTab : MonoBehaviour
{
    public DeckBookUI deckBookUI;
    public List<CardBase> cardsByCharacter = new List<CardBase>();
    public GridUnlimitedScroller gridUnlimitedScroller;
    public IUnlimitedScroller unlimitedScroller; 


    void Awake()
    {
        unlimitedScroller = gridUnlimitedScroller.GetComponent<IUnlimitedScroller>();
    }

    void OnEnable()
    {
        GetCardDataFromDatabase();
    }

    void OnDisable()
    {
        unlimitedScroller.Clear();
    }

    public void GetCardDataFromDatabase()
    {
        GameObject cardOnBookPrefab = deckBookUI.cellPrefab; // 스크롤뷰에 생성할 Cell 오브젝트(CardOnBook 프리팹)
        int totalCount = cardsByCharacter.Count; // Cell 총 갯수
        unlimitedScroller.Generate(cardOnBookPrefab, totalCount, (index, iCell) => {
            var regularCell = iCell as RegularCell;
            CardOnBook cardOnBook = regularCell.GetComponent<CardOnBook>();
            cardOnBook.cardBase = cardsByCharacter[index];
            if (regularCell != null){
                regularCell.onGenerated?.Invoke(index);
            }
        });
        unlimitedScroller.JumpTo(0, JumpToMethod.Center); // Cell 생성하면 항상 맨 위로 이동
    }
}
