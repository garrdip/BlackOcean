using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Mirror;

public class DeckDrawPopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GridLayoutGroup gridLayoutGroup;
    public List<GameObject> addtionDrawCardObjects = new List<GameObject>();
    public List<GameObject> addtionDrawCardSlots = new List<GameObject>();


    void Awake()
    {
        PopUpUIManager.instance.onChangeDeckDrawPopUpShow += OnChangeDeckDrawPopUpShow;
        PopUpUIManager.instance.onChangeDeckDrawPopUpHide += OnChangeDeckDrawPopUpHide;
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // DeckDrawPopUp 활성화 콜백
    public void OnChangeDeckDrawPopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        GamePlayerDeck gamePlayerDeck = PlayerRegistry.Local.currentGamePlayer.GetComponent<GamePlayerDeck>();
        for(int i=0; i<gamePlayerDeck.addtionDrawCards.Count; i++){
            // 추가 드로우 카드 슬롯 오브젝트 생성
            GameObject cardPosition = Instantiate(
                    PopUpUIManager.instance.AddtionDrawCardSlotPrefab,
                    Vector3.zero,
                    Quaternion.identity,
                    gridLayoutGroup.transform
            );
            addtionDrawCardSlots.Add(cardPosition);

            // 추가 드로우 카드 오브젝트 생성
            GameObject cardOnDeckObject = Instantiate(
                PopUpUIManager.instance.CardOnDeckPrefab,
                GameUIManager.instance.buttonPrefareDeck.transform.position,
                Quaternion.identity,
                PopUpUIManager.instance.deckDrawPopUp.transform
            );
            CardOnDeck cardOnDeck =  cardOnDeckObject.GetComponent<CardOnDeck>();
            cardOnDeck.card = gamePlayerDeck.addtionDrawCards[i];
            addtionDrawCardObjects.Add(cardOnDeckObject);
            cardOnDeck.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            cardOnDeck.transform.SetParent(cardPosition.transform);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(cardOnDeck.transform.DOLocalMove(Vector3.zero, 0.5f).SetDelay(0.1f * i));
            sequence.Join(cardOnDeck.transform.DOScale(Vector3.one, 0.5f));
            sequence.OnComplete(() => {
                sequence.Kill();
            });
        }
    }

    // DeckDrawPopUp 비활성화 콜백
    public void OnChangeDeckDrawPopUpHide()
    {
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            foreach(GameObject card in addtionDrawCardObjects){
                Destroy(card);
            }
            foreach(GameObject cardSlot in addtionDrawCardSlots){
                Destroy(cardSlot);
            }
            addtionDrawCardObjects.Clear();
            addtionDrawCardSlots.Clear();
            gameObject.SetActive(false);
        });
    } 
}
