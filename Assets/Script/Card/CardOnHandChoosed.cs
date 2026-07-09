using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using DG.Tweening;
using TMPro;
using AYellowpaper.SerializedCollections;
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

    [Header("CardOnHand 경험치 바")]
    public GameObject expBlockPrefab; // 경험치 바 내부 블록 오브젝트 프리팹
    public VerticalLayoutGroup verticalLayoutGroup;
    public List<GameObject> expBlocks = new List<GameObject>(); // 경험치 바 내부 블록 리스트

    private Vector3 originScale;
    public bool isTweening = false; // Dotween 애니매이션 함수들 실행중인지 여부

    void Start()
    {
        originScale = transform.localScale;
        initCardData();
        InitCardTemplateByCharacter(card);
    }

    // CardData의 스프라이트 데이터로부터 선택한 캐릭터의 카드 이미지 세팅
    private void InitCardTemplateByCharacter(Card car)
    {
        switch(card.baseCard.character){
            case Character.GEORK:
                SerializedDictionary<string, Sprite> georkCardSprites = CardData.instance.characterCardTemplate[Character.GEORK];
                InitCardTemplateByCardType(card, georkCardSprites);
                InitCardIllust(card, georkCardSprites);
                InitCardTemplateByCardEnhanced(card, georkCardSprites);
                InitCardExpBar(card, georkCardSprites);
                break;
            case Character.ERIS:
                SerializedDictionary<string, Sprite> erisCardSprites = CardData.instance.characterCardTemplate[Character.ERIS];
                InitCardTemplateByCardType(card, erisCardSprites);
                InitCardIllust(card, erisCardSprites);
                InitCardTemplateByCardEnhanced(card, erisCardSprites);
                InitCardExpBar(card, erisCardSprites);
                break;
            case Character.HONGDANHYANG:
                SerializedDictionary<string, Sprite> danhyangCardSprites = CardData.instance.characterCardTemplate[Character.HONGDANHYANG];
                InitCardTemplateByCardType(card, danhyangCardSprites);
                InitCardIllust(card, danhyangCardSprites);
                InitCardTemplateByCardEnhanced(card, danhyangCardSprites);
                InitCardExpBar(card, danhyangCardSprites);
                break;
        }
    }

    // 카드 타입에 따라 외형 틀 세팅
    private void InitCardTemplateByCardType(Card card, SerializedDictionary<string, Sprite> sprites)
    {
        if(!card.baseCard.cardNumber.Equals("HA")){
            switch(card.baseCard.cardType){
                case CardType.ATTACK:
                    cardBackground.sprite = sprites[Const.ATTACK_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.ATTACK_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.ATTACK_EMBLEM];
                    break;
                case CardType.BLESS:
                    cardBackground.sprite = sprites[Const.BLESS_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.BLESS_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.BLESS_EMBLEM];
                    break;
                case CardType.STRATEGY:
                    cardBackground.sprite = sprites[Const.STRATEGY_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.STRATEGY_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.STRATEGY_EMBLEM];
                    break;
                case CardType.HERO:
                    cardBackground.sprite = sprites[Const.HERO_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.HERO_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.HERO_EMBLEM];
                    break;
            }
        }
    }

    // 카드 일러스트 세팅
    private void InitCardIllust(Card card, SerializedDictionary<string, Sprite> sprites)
    {
        if(!card.baseCard.cardNumber.Contains("HA")){
            if(card.baseCard.cardNumber.Contains("_E")){
                // 강화카드의 경우 _E 문자열을 제거하여 아틀라스에서 스프라이트 조회
                int idx = card.baseCard.cardNumber.IndexOf("_E");
                if(idx != -1){
                    string cardNumber = card.baseCard.cardNumber.Substring(0, idx);
                    cardIllust.sprite = CardData.instance.cardIllustAtlas.GetSprite(cardNumber);
                }
            }else{
                cardIllust.sprite = CardData.instance.cardIllustAtlas.GetSprite(card.baseCard.cardNumber);
            }
        }
    }

    // 카드 강화 상태 프레임 세팅
    private void InitCardTemplateByCardEnhanced(Card card, SerializedDictionary<string, Sprite> sprites)
    {
        if(card.isEnhanced){
            cardGradeFrame.sprite = sprites[Const.ENHANCE_NORMAL_GRADE_FRAME];
        }else{
            cardGradeFrame.sprite = sprites[Const.NORMAL_GRADE_FRAME];
        }
    }

    // 카드 경험치 바 초기화 : card 데이터에서 최대 경험치 정보를 가져와 해당 숫자 만큼의 경험치 바 내부 블록 생성
    private void InitCardExpBar(Card card, SerializedDictionary<string, Sprite> sprites)
    {
        // 철귀 이동카드는 경험치 오브젝트 초기화 제외
        if(!card.baseCard.cardNumber.Equals("HA")){
            // 최대 경험치 만큼 내부 블록 생성
            for(int i=0; i<card.baseCard.maxExperience; i++){
                GameObject expBlock = Instantiate(expBlockPrefab);
                expBlock.transform.SetParent(verticalLayoutGroup.transform, false);
                expBlock.GetComponent<Image>().sprite = sprites[Const.EXP_BAR_INACTIVE];
                expBlocks.Add(expBlock);
            }
            // expBlocks 역순으로 전환(블록이 아래부터 쌓이도록)
            expBlocks.Reverse();
            // 경험치 블록 리스트에서 현재 카드의 경험치 숫자 만큼 블록 생상을 변경
            for(int j=0; j<card.experience; j++){
                expBlocks[j].GetComponent<Image>().sprite = sprites[Const.EXP_BAR_ACTIVE];
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
            if(card.baseCard.cardType == PlayerRegistry.Local.currentGamePlayer.GetComponent<GamePlayerDeck>().previousCardType)
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
