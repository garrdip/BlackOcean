using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DG.Tweening;


public class BattleResultPopUp : SingletonD<BattleResultPopUp>
{
    public CanvasGroup canvasGroup;

    [Header("랜덤으로 추출한 카드 리스트")]
    public List<Card> extractCards = new List<Card>();

    [Header("랜덤으로 추출한 카드 오브젝트 리스트")]
    public List<GameObject> extractCardObjects = new List<GameObject>();

 
    void OnEnable()
    {
        DOTween.KillAll(); // 전투 결과 팝업 호출 시 모든 Tweening 제거(카드 수행도중 전투종료로 팝업이 호출되어, 네트워크 오브젝트가 제거되었으나 트위닝에 의해 접근을 하려는 경우를 방지하기 위함.)
        canvasGroup.DOFade(1.0f, 1.0f);
        M_CardManager.instance.RemoveAllCurrentPlayerCardOnHandsWithOutTrashDeck(); // 현재 플레이어 손에 있던 카드들을 소멸
        CreateResultCard(3); // 랜덤 보상 카드 3개 생성
    }

    void OnDisable()
    {
        canvasGroup.DOFade(0.0f, 1.0f);
        RemoveResultCard();
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    // 랜덤 보상 카드 N개 생성
    private void CreateResultCard(int count)
    {
        List<Card> randomCards = M_CardManager.instance.ExtractRandomCards(count);
        foreach(Card card in randomCards){
            extractCards.Add(card);
            GameObject cardOnDeck = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab);
            cardOnDeck.transform.SetParent(PopUpUIManager.instance.selectableCardList.transform);
            cardOnDeck.transform.localScale = new Vector3(1, 1, 1);
            cardOnDeck.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 500);
            cardOnDeck.GetComponent<CardOnDeck>().card = card;
            extractCardObjects.Add(cardOnDeck);
        }
    }

    // 생성되었던 보상 카드들 제거
    private void RemoveResultCard()
    {
        foreach(GameObject gameObject in extractCardObjects){
            Destroy(gameObject);
        }
        extractCards.Clear();
        extractCardObjects.Clear();
    }
}
