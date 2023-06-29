using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using DG.Tweening;
using TMPro;

public class CardOnDeck : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Card card;
    public TextMeshProUGUI textCardName;
    public TextMeshProUGUI textCardInfo;

    private Vector3 originScale;
    private bool isTweening = false; // Dotween 애니매이션 함수들 실행중인지 여부

    void Start()
    {
        textCardName.text = card.baseCard.name;
        textCardInfo.text = card.baseCard.cardType.ToString();
        originScale = transform.localScale;
    }

    void OnDisable()
    {
        DOTween.Kill(transform); // 비활성화 될 때 DoTween 프로세스 킬
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        if(!isTweening){
            transform.DOScale(originScale * 1.2f, 0.3f);
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
        HandleClickCardOnDeckOnBattleResultPopUp();
    }

    // BattleResultPopUp이 활성화 된 경우의 CardOnDeck 클릭 이벤트 처리
    private void HandleClickCardOnDeckOnBattleResultPopUp()
    {
        if(PopUpUIManager.instance.battleResultPopUp.activeSelf){
            if(NetworkClient.connection != null && NetworkClient.active){
                GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
                if(gamePlayerDeck.isLocalPlayer){
                    // 전투 결과 팝업 비활성화
                    PopUpUIManager.instance.HandleHideBattleResultPopUp();

                    // 애니매이션용 카드 오브젝트 복사본 생성
                    GameObject cardOnDeckChoosed = CreateChoosedCardOnDeck(this.card);
                      
                    // 턴 매니저에 저장된 현재 참가한 플레이어들의 타겟오브젝트 리스트에서 로컬플레이어의 타겟오브젝트 조회
                    GamePlayer gamePlayer = gamePlayerDeck.GetComponent<GamePlayer>();
                    TargetObject currentPlayer = M_TurnManager.instance.spawnedPlayerSyncList.Find((targetObject) => targetObject.player == gamePlayer);

                    // 이동 위치는 현재 플레이어 타겟오브젝트 위치
                    Vector3 targetPosition = currentPlayer.transform.position;
                    StartMoveToTarget(cardOnDeckChoosed.GetComponent<CardOnDeck>(), targetPosition);
                }
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
                M_CardManager.instance.RemoveAllCurrentPlayerPrefareDeckAndTrashDeck();
                M_TurnManager.instance.ClearTargetObject();
                Destroy(cardOnDeckChoosed.gameObject);
                //M_MapManager.instance.SetCameraPosition();
                GameUIManager.instance.FadeBlackCurtain((blackCurtain) => {
                    // 카메라 위치 리셋
                    Vector2 currLoc = M_MapManager.instance.currentLocation;
                    Camera.main.transform.position = M_MapManager.instance.GetMapCameraLocation() + new Vector3(0,0,-8);
                    Camera.main.orthographic = false;

                    // UI 활성화 상태 변경
                    M_MapManager.instance.roommaps.SetActive(true);
                    M_MapManager.instance.game.SetActive(false);
                    GameUIManager.instance.GameUI.gameObject.SetActive(false);
                    GameUIManager.instance.GameBackGround.gameObject.SetActive(false);
                    
                    // Dim배경 상태 변경
                    blackCurtain.gameObject.SetActive(false);
                    blackCurtain.DOFade(0.0f, 0.5f); // 원래 알파값으로 변경
                });
            });
    }
}
