using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectD;
using AYellowpaper.SerializedCollections;


public class CardOnBook : MonoBehaviour
{
    private int index;
    public TextMeshProUGUI textCardName;
    public TextMeshProUGUI textCardDescription;
    public TextMeshProUGUI textCardCost;
    public Image cardBackground;
    public Image cardIllust;
    public Image cardImageFrame;
    public Image cardGradeFrame;
    public Image cardEmblem;


    // 덱북 카드 초기화 : CardOnBook프리팹의 Regular Cell클래스에 있는 OnGenerate 이벤트에 연결되어있음.
    public void initCardOnBook(int newIndex)
    {
        index = newIndex;
        Card card = new Card(CardData.instance.cards[index]);
        textCardName.text = card.baseCard.name;
        textCardDescription.text = card.baseCard.description;
        textCardCost.text = card.baseCard.cost.ToString();
        InitCardTemplateByCharacter(card);
    }

    private void InitCardTemplateByCharacter(Card card)
    {
        switch(card.baseCard.character){
            case Character.GEORK:
                SerializedDictionary<string, Sprite> georkCardSprites = CardData.instance.characterCardTemplate[Character.GEORK];
                InitCardTemplateByCardType(card, georkCardSprites);
                InitCardIllust(card, georkCardSprites);

                break;
            case Character.ERIS:
                SerializedDictionary<string, Sprite> erisCardSprites = CardData.instance.characterCardTemplate[Character.ERIS];
                InitCardTemplateByCardType(card, erisCardSprites);
                InitCardIllust(card, erisCardSprites);

                break;
            case Character.HONGDANHYANG:
                SerializedDictionary<string, Sprite> danhyangCardSprites = CardData.instance.characterCardTemplate[Character.HONGDANHYANG];
                InitCardTemplateByCardType(card, danhyangCardSprites);
                InitCardIllust(card, danhyangCardSprites);
                break;
        }
    }

    private void InitCardTemplateByCardType(Card card, SerializedDictionary<string, Sprite> sprites)
    {
        if(!card.baseCard.cardNumber.Equals("HA") || !card.baseCard.cardNumber.Equals("HA_E")){
            switch(card.baseCard.cardType){
                case CardType.ATTACK:
                    cardBackground.sprite = sprites[Const.ATTACK_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.ATTACK_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.ATTACK_EMBLEM];
                    cardGradeFrame.sprite = sprites[Const.NORMAL_GRADE_FRAME];
                    break;
                case CardType.BLESS:
                    cardBackground.sprite = sprites[Const.BLESS_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.BLESS_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.BLESS_EMBLEM];
                    cardGradeFrame.sprite = sprites[Const.NORMAL_GRADE_FRAME];
                    break;
                case CardType.STRATEGY:
                    cardBackground.sprite = sprites[Const.STRATEGY_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.STRATEGY_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.STRATEGY_EMBLEM];
                    cardGradeFrame.sprite = sprites[Const.NORMAL_GRADE_FRAME];
                    break;
                case CardType.HERO:
                    cardBackground.sprite = sprites[Const.HERO_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.HERO_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.HERO_EMBLEM];
                    cardGradeFrame.sprite = sprites[Const.NORMAL_GRADE_FRAME];
                    break;
            }
        }
    }

    private void InitCardIllust(Card card, SerializedDictionary<string, Sprite> sprites)
    {
        if(!string.IsNullOrEmpty(card.baseCard.cardImage)){
            cardIllust.sprite = Resources.Load<Sprite>(card.baseCard.cardImage);
        }
    }
}
