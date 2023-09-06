using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using DG.Tweening;
using TMPro;
using ProjectD;

public class CardOnDeck : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Card card;
    public TextMeshProUGUI textCardName;
    public TextMeshProUGUI textCardType;
    public TextMeshProUGUI textCardDescription;
    public TextMeshProUGUI textCardCost;

    private Vector3 originScale;
    private bool isTweening = false; // Dotween 애니매이션 함수들 실행중인지 여부

    void Start()
    {
        originScale = transform.localScale;
        initCardData();
    }

    void OnDisable()
    {
        DOTween.Kill(transform); // 비활성화 될 때 DoTween 프로세스 킬
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        if(!isTweening){
            transform.DOScale(originScale * 1.3f, 0.3f);
        }
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        if(!isTweening){
            transform.DOScale(originScale, 0.3f);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 전투 결과 팝업 활성화 상태에서 카드 클릭 이벤트
        if(PopUpUIManager.instance.battleResultPopUp.activeSelf){
            HandleClickCardOnDeckOnPopUp(() => {
                PopUpUIManager.instance.HandleHideBattleResultPopUp();
            });
        }
        // MercuriusPopUp이 팝업 활성화 상태에서 카드 클릭 이벤트
        if(PopUpUIManager.instance.mercuriusPopUp.activeSelf){
            HandleClickCardOnDeckOnPopUp(() => {
                PopUpUIManager.instance.HandleMercuriusPopUp(false);
            });
        }
    }

    // 팝업이 활성화된 상태에서 CardOnDeck 공통 클릭 이벤트
    private void HandleClickCardOnDeckOnPopUp(System.Action callback)
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                // 애니매이션용 카드 오브젝트 복사본 생성
                GameObject cardOnDeckChoosed = CreateChoosedCardOnDeck(this.card);
                    
                // 턴 매니저에 저장된 현재 참가한 플레이어들의 타겟오브젝트 리스트에서 로컬플레이어의 타겟오브젝트 조회
                GamePlayer gamePlayer = gamePlayerDeck.GetComponent<GamePlayer>();
                TargetObject currentPlayer;
                if(NetworkServer.activeHost){
                    currentPlayer = NetworkServer.spawned[M_TurnManager.instance.spawnedPlayerSyncList.Find(netId => NetworkServer.spawned[netId].GetComponent<TargetObject>().player == gamePlayer)].GetComponent<TargetObject>();
                }else{
                    currentPlayer = NetworkClient.spawned[M_TurnManager.instance.spawnedPlayerSyncList.Find(netId => NetworkClient.spawned[netId].GetComponent<TargetObject>().player == gamePlayer)].GetComponent<TargetObject>();
                }
                //TargetObject currentPlayer = M_TurnManager.instance.spawnedPlayerSyncList.Find((targetObject) => targetObject.player == gamePlayer);

                // 이동 위치는 현재 플레이어 타겟오브젝트 위치
                Vector3 targetPosition = currentPlayer.transform.position;
                StartMoveToTarget(cardOnDeckChoosed.GetComponent<CardOnDeck>(), targetPosition);
                callback();
            }
        }
    }

    // 애니매이션용으로 사용될 선택된 보상카드의 복사 오브젝트 생성
    private GameObject CreateChoosedCardOnDeck(Card card)
    {
        GameObject cardOnDeckChoosed = Instantiate(PopUpUIManager.instance.CardOnDeckChoosedPrefab);
        cardOnDeckChoosed.GetComponent<CardOnDeck>().card = card;
        cardOnDeckChoosed.GetComponent<CardOnDeck>().isTweening = true;
        cardOnDeckChoosed.transform.SetParent(GameUIManager.instance.RootGameObject.transform);
        cardOnDeckChoosed.transform.position = new Vector3(0f, 0f, 0f);

        return cardOnDeckChoosed;
    }

    // 포물선을 그리며 타겟 위치로 이동
    private void StartMoveToTarget(CardOnDeck cardOnDeckChoosed, Vector3 targetPosition)
    {
        float height = 2f;
        float duration = 1f;
        Vector3 startPos = cardOnDeckChoosed.transform.position;
        Vector3 midPos = (startPos + targetPosition) / 2f;
        midPos.y += height;
        Vector3[] path = new Vector3[] { startPos, midPos, targetPosition };
        
        // DOTween을 사용하여 포물선 이동 애니메이션 생성
        cardOnDeckChoosed.transform.DOScale(new Vector3(0.02f, 0.02f, 0f), 0.5f);
        cardOnDeckChoosed.transform.DOPath(path, duration, PathType.CatmullRom)
            .SetEase(Ease.OutQuint)
            .OnComplete(() => {
                cardOnDeckChoosed.GetComponent<CardOnDeck>().isTweening = false;
                M_CardManager.instance.AddCardDataToCurrentPlayerDeck(cardOnDeckChoosed.card);
                Destroy(cardOnDeckChoosed.gameObject);
                NetworkClient.connection.identity.GetComponent<GamePlayer>().isRewardDone = true;
            });
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

        textCardType.text = card.baseCard.cardType.ToString();
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
