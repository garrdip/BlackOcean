using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using DG.Tweening;
using TMPro;
using ProjectD;

public class CardOnHandChoosed : MonoBehaviour
{
    public Card card;

    [Header("CardOnDeck Image 컴포넌트")]
    public SpriteRenderer cardBackground;
    public SpriteRenderer cardIllust;
    public SpriteRenderer cardImageFrame;
    public SpriteRenderer cardGradeFrame;
    public SpriteRenderer cardEmblem;

    [Header("CardOnDeck Text 컴포넌트")]
    public TextMeshProUGUI textCardName;
    public TextMeshProUGUI textCardDescription;
    public TextMeshProUGUI textCardCost;

    [Header("CardOnHand 배경 이미지")]
    public Sprite attackCardBackground;
    public Sprite blessCardBackground;
    public Sprite strategyCardBackground;

    [Header("CardOnHand 내부 일러스트 액자 틀")]
    public Sprite attackCardImageFrame;
    public Sprite blessCardImageFrame;
    public Sprite strategyCardImageFrame;

    [Header("CardOnHand 등급 및 강화 틀")]
    public Sprite enhancedLegendCardGradFrame;
    public Sprite legendCardGradeFrame;
    public Sprite enhancedNormalCardGradFrame;
    public Sprite normalCardGradeFrame;
    public Sprite enhancedRareCardGradFrame;
    public Sprite rareCardGradeFrame;

    [Header("CardOnHand 앰블럼")]
    public Sprite attackEmblem;
    public Sprite blessEmblem;
    public Sprite strategyEmblem;

    [Header("CardOnHand 경험치 바")]
    public Sprite activeExpbar;
    public Sprite inActiveExpbar;
    public GameObject expBlockPrefab; // 경험치 바 내부 블록 오브젝트 프리팹
    public VerticalLayoutGroup verticalLayoutGroup;
    public List<GameObject> expBlocks = new List<GameObject>(); // 경험치 바 내부 블록 리스트

    private Vector3 originScale;
    public bool isTweening = false; // Dotween 애니매이션 함수들 실행중인지 여부

    void Start()
    {
        originScale = transform.localScale;
        initCardData();
        InitCardIllust(card);
        InitCardTemplateByCardType(card);
        InitCardTemplateByCardEnhanced(card);
        InitCardExpBar(card);
    }

    private void InitCardTemplateByCardType(Card card)
    {
        if(!card.baseCard.cardNumber.Equals("HA")){
            switch(card.baseCard.cardType){
                case CardType.ATTACK:
                    cardBackground.sprite = attackCardBackground;
                    cardImageFrame.sprite = attackCardImageFrame;
                    cardEmblem.sprite = attackEmblem;
                    break;
                case CardType.BLESS:
                    cardBackground.sprite = blessCardBackground;
                    cardImageFrame.sprite = blessCardImageFrame;
                    cardEmblem.sprite = blessEmblem;
                    break;
                case CardType.STRATEGY:
                    cardBackground.sprite = strategyCardBackground;
                    cardImageFrame.sprite = strategyCardImageFrame;
                    cardEmblem.sprite = strategyEmblem;
                    break;
            }
        }
    }

    // 카드 이미지 세팅
    private void InitCardIllust(Card card)
    {
        if(!string.IsNullOrEmpty(card.baseCard.cardImage)){
            cardIllust.sprite = Resources.Load<Sprite>(card.baseCard.cardImage);
        }
    }

    // 카드 강화 상태 프레임 세팅
    private void InitCardTemplateByCardEnhanced(Card card)
    {
        if(card.isEnhanced){
            cardGradeFrame.sprite = enhancedNormalCardGradFrame;
        }else{
            cardGradeFrame.sprite = normalCardGradeFrame;
        }
    }

    // 카드 경험치 바 초기화 : card 데이터에서 최대 경험치 정보를 가져와 해당 숫자 만큼의 경험치 바 내부 블록 생성
    private void InitCardExpBar(Card card)
    {
        // 철귀 이동카드는 경험치 오브젝트 초기화 제외
        if(!card.baseCard.cardNumber.Equals("HA")){
            // 최대 경험치 만큼 내부 블록 생성
            for(int i=0; i<card.baseCard.maxExperience; i++){
                GameObject expBlock = Instantiate(expBlockPrefab);
                expBlock.transform.SetParent(verticalLayoutGroup.transform, false);
                expBlock.GetComponent<Image>().sprite = inActiveExpbar;
                expBlock.GetComponent<Image>().SetNativeSize();
                expBlocks.Add(expBlock);
            }
            // expBlocks 역순으로 전환(블록이 아래부터 쌓이도록)
            expBlocks.Reverse();
            // 경험치 블록 리스트에서 현재 카드의 경험치 숫자 만큼 블록 생상을 변경
            for(int j=0; j<card.experience; j++){
                expBlocks[j].GetComponent<Image>().sprite = activeExpbar;
                expBlocks[j].GetComponent<Image>().SetNativeSize();
            }
        }   
    }

    void OnDisable()
    {
        DOTween.Kill(transform); // 비활성화 될 때 DoTween 프로세스 킬
    }

 
    // 카드 정보 뷰 설정
    private void initCardData()
    {
        if(card.experience >= card.baseCard.maxExperience)
        {
            textCardName.text = CardData.instance.cards.Find(x => x.cardNumber == card.baseCard.cardNumber + "_E").name;
            textCardDescription.text = M_CardManager.instance.GetAdditionalValueFromDescription(CardData.instance.cards.Find(x => x.cardNumber == card.baseCard.cardNumber + "_E").description);
        }
        else
        {
            textCardName.text = card.baseCard.name;
            textCardDescription.text = M_CardManager.instance.GetAdditionalValueFromDescription(card.baseCard.description);
        }

        textCardDescription.text += '\n';
        textCardDescription.text += '\n';
        foreach(CardCharacteristic character in card.baseCard.cardCharacteristics)
            textCardDescription.text += "<b><color=yellow>" + character.ToString() + "</color></b>";
        
        if(card.baseCard.cardCharacteristics.Exists( x => x == CardCharacteristic.EUNHASOO)) // 은하수 카드 코스트 계산
        {
            if(card.baseCard.cardType == NetworkClient.connection.identity.GetComponent<GamePlayerDeck>().previousCardType)
            {
                textCardCost.text = "<b><color=green>" +((card.baseCard.cost + card.costAddition - 1) <= 0 ? "0" : (card.baseCard.cost + card.costAddition - 1).ToString()) + "</color></b>";
            }
            else
            {
                textCardCost.text = "<b><color=red>"+ (card.baseCard.cost + card.costAddition + 1).ToString() + "</color></b>";
            }
        }
        else textCardCost.text = (card.baseCard.cost + card.costAddition).ToString();
    }
}
