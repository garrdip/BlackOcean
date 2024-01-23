using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectD;

public class DeckBookUI : SingletonD<DeckBookUI>
{
    public GameObject cellPrefab;
    public MenuUI menuUI;
    public Button buttonCloseDeckBook;
    public List<Button> tabButtons = new List<Button>();
    public List<GameObject> tabFrames = new List<GameObject>();
    public int currentTabIndex;


    void Start()
    {
        menuUI = GetComponent<MenuUI>();
        buttonCloseDeckBook.onClick.AddListener(() => menuUI.HandleCloseDeckBook());
        for(int i=0; i<tabButtons.Count; i++){
            int buttonIndex = i;
            tabButtons[i].onClick.AddListener(() => ShowTab(buttonIndex));
            SetCardsByTabIndex(i);
        }
    }

    // 탭 인덱스값에 따라 캐릭터 카드데이터 리스트 세팅
    private void SetCardsByTabIndex(int index)
    {
        switch(index){
            case 0:
                foreach(CardBase cardBase in GetCardsByCharacter(Character.GEORK)){
                    tabFrames[0].GetComponent<DeckBookTab>().cardsByCharacter.Add(cardBase);
                }
                break;
            case 1:
                foreach(CardBase cardBase in GetCardsByCharacter(Character.HONGDANHYANG)){
                    tabFrames[1].GetComponent<DeckBookTab>().cardsByCharacter.Add(cardBase);
                }
                break;
            case 2:
                foreach(CardBase cardBase in GetCardsByCharacter(Character.ERIS)){
                    tabFrames[2].GetComponent<DeckBookTab>().cardsByCharacter.Add(cardBase);
                }
                break;
            }
    }

    // 캐릭터별 카드데이터 조회
    private List<CardBase> GetCardsByCharacter(Character character)
    {
        return CardData.instance.cards.FindAll((cardBase) => cardBase.character == character && !cardBase.cardNumber.Contains("_E")); 
    }

    public void ShowTab(int index)
    {
        tabFrames[index].SetActive(true);
        tabButtons[index].image.color = new Color32(255, 255, 255, 255);
        currentTabIndex = index;
        HideOtherTabs(index);
    }

    public void HideOtherTabs(int index)
    {
        for(int i=0; i<tabButtons.Count; i++){
            if(i != index){
                tabButtons[i].image.color = new Color32(255, 255, 255, 70);
                tabFrames[i].SetActive(false);
            }
        }
    }
}
