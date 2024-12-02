using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using ProjectD;
using AYellowpaper.SerializedCollections;
using DG.Tweening;
using Mirror;


public class CardOnBook : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,  IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    private int index;
    private Vector3 originScale;

    public CardBase cardBase;
    public TextMeshProUGUI textCardName;
    public TextMeshProUGUI textCardDescription;
    public TextMeshProUGUI textCardCost;
    public Image cardBackground;
    public Image cardIllust;
    public Image cardImageFrame;
    public Image cardGradeFrame;
    public Image cardEmblem;


    private void Start()
    {
        originScale = Vector3.one;
    }

    void OnDisable()
    {
        DOTween.Kill(transform);
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        transform.DOScale(originScale * 1.2f, 0.3f);
        GraphicRaycaster graphicRaycaster = textCardDescription.GetComponentInParent<GraphicRaycaster>();
        TextDetector.instance.StartTextDetect(graphicRaycaster);
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("event_cardstore_mouseover_1"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        transform.DOScale(originScale, 0.3f);
        TextDetector.instance.StopTextDetect();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        cardBackground.color = Color.gray;
        cardImageFrame.color = Color.gray;
        cardGradeFrame.color = Color.gray;
        cardEmblem.color = Color.gray;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        cardBackground.color = Color.white;
        cardImageFrame.color = Color.white;
        cardGradeFrame.color = Color.white;
        cardEmblem.color = Color.white;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if(!SceneManager.GetActiveScene().name.Equals("MenuScene")){
            PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
            GamePlayerDeck gamePlayerDeck = playerInterface.currentGamePlayer.GetComponent<GamePlayerDeck>();
            Card card = new Card(cardBase);
            if(playerInterface.currentGamePlayer.character == card.baseCard.character){
                gamePlayerDeck.CmdAddDeck(card);
            }
        }
    }

    // 덱북 카드 초기화 : CardOnBook프리팹의 Regular Cell클래스에 있는 OnGenerate 이벤트에 연결되어있음.
    public void initCardOnBook(int newIndex)
    {
        index = newIndex;
        textCardName.text = cardBase.name;
        textCardDescription.text = cardBase.description;
        textCardCost.text = cardBase.cost.ToString();
        InitCardTemplateByCharacter(cardBase);
        initCardData(new Card(cardBase));
    }

    private void InitCardTemplateByCharacter(CardBase cardBase)
    {
        switch(cardBase.character){
            case Character.GEORK:
                SerializedDictionary<string, Sprite> georkCardSprites = CardData.instance.characterCardTemplate[Character.GEORK];
                InitCardTemplateByCardType(cardBase, georkCardSprites);
                InitCardIllust(cardBase, georkCardSprites);

                break;
            case Character.ERIS:
                SerializedDictionary<string, Sprite> erisCardSprites = CardData.instance.characterCardTemplate[Character.ERIS];
                InitCardTemplateByCardType(cardBase, erisCardSprites);
                InitCardIllust(cardBase, erisCardSprites);

                break;
            case Character.HONGDANHYANG:
                SerializedDictionary<string, Sprite> danhyangCardSprites = CardData.instance.characterCardTemplate[Character.HONGDANHYANG];
                InitCardTemplateByCardType(cardBase, danhyangCardSprites);
                InitCardIllust(cardBase, danhyangCardSprites);
                break;
        }
    }

    private void InitCardTemplateByCardType(CardBase cardBase, SerializedDictionary<string, Sprite> sprites)
    {
        if(!cardBase.cardNumber.Equals("HA") || !cardBase.cardNumber.Equals("HA_E")){
            switch(cardBase.cardType){
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
                case CardType.CURSE:
                    cardBackground.sprite = sprites[Const.CURSE_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.CURSE_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.CURSE_EMBLEM];
                    cardGradeFrame.sprite = sprites[Const.NORMAL_GRADE_FRAME];
                    break;
            }
        }
    }

    private void InitCardIllust(CardBase cardBase, SerializedDictionary<string, Sprite> sprites)
    {
        if(!cardBase.cardNumber.Contains("HA")){
            if(cardBase.cardNumber.Contains("_E")){
                // 강화카드의 경우 _E 문자열을 제거하여 아틀라스에서 스프라이트 조회
                int idx = cardBase.cardNumber.IndexOf("_E");
                if(idx != -1){
                    string cardNumber = cardBase.cardNumber.Substring(0, idx);
                    cardIllust.sprite = CardData.instance.cardIllustAtlas.GetSprite(cardNumber);
                }
            }else{
                cardIllust.sprite = CardData.instance.cardIllustAtlas.GetSprite(cardBase.cardNumber);
            }
        }
    }

    public void initCardData(Card card)
    {
        textCardDescription.text = CardData.instance.ReplaceDescription(card.baseCard.description);
        textCardDescription.text += '\n';
        textCardDescription.text += '\n';
        foreach(CardCharacteristic character in card.baseCard.cardCharacteristics)
            textCardDescription.text += "<b><color=yellow>" + character.ToString() + "</color></b>" + '\n';
        foreach(CardCharacteristic character in card.cardCharacteristics)
            textCardDescription.text += "<b><color=yellow>" + character.ToString() + "</color></b>" + '\n';
        
        if(card.baseCard.cardCharacteristics.Exists( x => x == CardCharacteristic.EUNHASOO)) // 은하수 카드 코스트 계산
        {
            textCardCost.text = "<b><color=red>"+ (card.baseCard.cost + card.costAddition + 1).ToString() + "</color></b>";
        }
        else textCardCost.text = (card.baseCard.cost + card.costAddition).ToString();
    }
}
