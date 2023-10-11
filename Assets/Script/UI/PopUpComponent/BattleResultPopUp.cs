using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;


public class BattleResultPopUp : SingletonD<BattleResultPopUp>
{
    public CanvasGroup canvasGroup;

    [Header("랜덤으로 추출한 카드 오브젝트 리스트")]
    public List<GameObject> extractCardObjects = new List<GameObject>();

 
    protected override void Awake()
    {
        PopUpUIManager.instance.onChangeBattleResultPopUpShow += OnChangeBattleResultPopUpShow;
        PopUpUIManager.instance.onChangeBattleResultPopUpHide += OnChangeBattleResultPopUpHide;
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    // 보상 카드 오브젝트 생성
    public void CreateResultCard(List<Card> rewardCards)
    {
        //TODO
        //foreach(Card card in rewardCards){
        //    if(card.baseCard.character == NetworkClient.localPlayer.GetComponent<GamePlayer>().character){
        //        GameObject cardOnDeck = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab);
        //        cardOnDeck.transform.SetParent(PopUpUIManager.instance.selectableCardList.transform);
        //        cardOnDeck.transform.localScale = new Vector3(1, 1, 1);
        //        cardOnDeck.GetComponent<CardOnDeck>().card = card;
        //        extractCardObjects.Add(cardOnDeck);
        //    }
        //}
    }

    // 생성되었던 보상 카드들 제거
    private void RemoveResultCard()
    {
        foreach(GameObject gameObject in extractCardObjects){
            Destroy(gameObject);
        }
        extractCardObjects.Clear();
    }

    // 넘기기 버튼 클릭
    public void HandleClickButtonSkip()
    {
        //TODO
        //PopUpUIManager.instance.HandleHideBattleResultPopUp(); // 전투 결과 팝업 비활성화
        //GameUIManager.instance.FadeBlackCurtain((blackCurtain) => {
        //    if(NetworkClient.connection != null && NetworkClient.active){
        //        NetworkClient.connection.identity.GetComponent<GamePlayer>().isRewardDone = true;
        //    }
        //});
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // BattleResultPopUp 활성화 콜백
    public void OnChangeBattleResultPopUpShow(List<Card> rewardCards)
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        CreateResultCard(rewardCards);
        M_CardManager.instance.RemoveAllCurrentPlayerArrow(); // 화살표 제거
        M_CardManager.instance.ChangeCurrentPlayerCardOnHandState(false); // 남아있는 CardOnHand 오브젝트들의 상태값 초기화
    }
    
    // BattleResultPopUp 비활성화 콜백
    public void OnChangeBattleResultPopUpHide()
    {
        RemoveResultCard();
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
        });
    } 
}
