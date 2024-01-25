using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using UnlimitedScrollUI;


public class DeckBookTab : MonoBehaviour
{
    public int index;
    public List<CardBase> cardsByCharacter = new List<CardBase>();
    public GridUnlimitedScroller gridUnlimitedScroller;
    public IUnlimitedScroller unlimitedScroller; 


    void Awake()
    {
        SetCardsByTabIndex(index);
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

    // 탭 인덱스값에 따라 캐릭터 카드데이터 리스트 세팅
    private void SetCardsByTabIndex(int index)
    {
        switch(index){
            case 0:
                foreach(CardBase cardBase in GetCardsByCharacter(Character.GEORK)){
                    cardsByCharacter.Add(cardBase);
                }
                break;
            case 1:
                foreach(CardBase cardBase in GetCardsByCharacter(Character.HONGDANHYANG)){
                    cardsByCharacter.Add(cardBase);
                }
                break;
            case 2:
                foreach(CardBase cardBase in GetCardsByCharacter(Character.ERIS)){
                    cardsByCharacter.Add(cardBase);
                }
                break;
        }
    }

    // 캐릭터별 카드데이터 조회
    private List<CardBase> GetCardsByCharacter(Character character)
    {
        return CardData.instance.cards.FindAll((cardBase) => cardBase.character == character && !cardBase.cardNumber.Contains("_E")); 
    }

    public void GetCardDataFromDatabase()
    {
        GameObject cardOnBookPrefab = DeckBookUI.instance.cellPrefab; // 스크롤뷰에 생성할 Cell 오브젝트(CardOnBook 프리팹)
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
