using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using ProjectD;
using UnlimitedScrollUI;
using TMPro;


public class DeckBookTab : MonoBehaviour
{
    public int index;
    public List<CardBase> cardsByCharacter = new List<CardBase>();
    public GridUnlimitedScroller gridUnlimitedScroller;
    public IUnlimitedScroller unlimitedScroller; 
    public TMP_InputField inputField;
    public Button buttonSearch;

    void Awake()
    {
        SetCardsByTabIndex(index);
        unlimitedScroller = gridUnlimitedScroller.GetComponent<IUnlimitedScroller>();
    }

    void Start()
    {
        inputField.onSubmit.AddListener(OnSubmitInputField);
        buttonSearch.onClick.AddListener(OnClickButtonSearch);
    }

    void OnEnable()
    {
        CreateDeckBookCard(cardsByCharacter);
    }

    void OnDisable()
    {
        unlimitedScroller.Clear();
        inputField.text = string.Empty;
    }

    private void OnClickButtonSearch()
    {
        OnSubmitInputField(inputField.text);
    }

    private void OnSubmitInputField(string text)
    {
        unlimitedScroller.Clear();
        string regexPattern = Regex.Escape(text);
        List<CardBase> serachedList = FullTextSearchByCardName(regexPattern);
        CreateDeckBookCard(serachedList);
        inputField.ActivateInputField();
    }

    // 정규표현식을 이용해 카드 이름을 검색하여, 해당하는 카드를 담은 리스트 반환
    private List<CardBase> FullTextSearchByCardName(string pattern)
    {
        List<CardBase> serachedList = new List<CardBase>();
        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
        for(int i = 0; i < cardsByCharacter.Count; i++){
            if(regex.IsMatch(cardsByCharacter[i].name)){
                serachedList.Add(cardsByCharacter[i]);
            }
        }
        return serachedList;
    }

    // 탭 인덱스값에 따라 캐릭터 카드데이터 리스트 세팅
    private void SetCardsByTabIndex(int index)
    {
        switch(index){
            case 0:
                foreach(CardBase cardBase in GetCardsByCharacter(Character.GEORK)){
                    if(!cardBase.cardNumber.Contains("G6")){ // 흔들리는 신념, 굳건한 신념 카드 제외
                        cardsByCharacter.Add(cardBase);
                    }
                }
                break;
            case 1:
                foreach(CardBase cardBase in GetCardsByCharacter(Character.HONGDANHYANG)){
                    if(!cardBase.cardNumber.Contains("HA")){ // 철귀이동 카드 제외
                        cardsByCharacter.Add(cardBase);
                    }
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

    private void CreateDeckBookCard(List<CardBase> cards)
    {
        GameObject cardOnBookPrefab = DeckBookUI.instance.cellPrefab; // 스크롤뷰에 생성할 Cell 오브젝트(CardOnBook 프리팹)
        int totalCount = cards.Count; // Cell 총 갯수
        unlimitedScroller.Generate(cardOnBookPrefab, totalCount, (index, iCell) => {
            var regularCell = iCell as RegularCell;
            CardOnBook cardOnBook = regularCell.GetComponent<CardOnBook>();
            cardOnBook.cardBase = cards[index];
            if (regularCell != null){
                regularCell.onGenerated?.Invoke(index);
            }
        });
        unlimitedScroller.JumpTo(0, JumpToMethod.Center); // Cell 생성하면 항상 맨 위로 이동
    }
}
