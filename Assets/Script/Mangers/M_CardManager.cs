using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DG.Tweening;

public class M_CardManager : NetworkBehaviour
{
    public static M_CardManager Instance = null;

    public static M_CardManager instance
    {
        get
        {
            if (Instance == null)
            {
                Instance = FindObjectOfType<M_CardManager>();
            }
            return Instance;
        }
    }

    // CardOnHand 오브젝트들의 인덱스값에 따라 순차적인 움직임으로 날아오는 애니매이션 + Moving플래그 변수 조정
    public void CardOnHandDrawSequence(CardOnHand cardOnHand, int index)
    {
        cardOnHand.isMoving = true;
        Transform cardTransform = cardOnHand.gameObject.transform;
        cardTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        cardTransform.position = DeckUI.instance.buttonPrefareDeck.GetComponent<RectTransform>().position;

        // Dotween 애니매이션 시퀀스 생성
        Sequence sequence = DOTween.Sequence();
        sequence.Append(cardOnHand.transform.DOScale(new Vector3(0.02f, 0.02f, 0f), 0.2f));
        sequence.Join(cardOnHand.transform.DORotate(new Vector3(0f, 0f, 0f), 0.2f));
        sequence.Join(cardTransform
            .DOMove(cardTransform.position + new Vector3(0f, 5f, 0f), 0.2f)
            .SetDelay(index * 0.1f)
            .SetEase(Ease.OutSine)
            .OnComplete(() => {
                cardTransform
                    .DOMove(cardTransform.position, 0.2f)
                    .SetDelay(index * 0.1f)
                    .SetEase(Ease.OutSine)
                    .OnComplete(() => {
                        cardOnHand.isMoving = false;
                    }
                );
            })
        );
    }

    // CardOnHand 오브젝트 멀어지는 애니매이션 + 오브젝트 파괴 커맨드 호출
    public void CardOnHandThrowAwaySequence(CardOnHand cardOnHand)
    {
        DeckUI.instance.buttonEndTurn.interactable = false;        
        cardOnHand.isMoving = true;
        float duration = 0.3f;
        Vector3 trashDeckPosition = DeckUI.instance.buttonTrashDeck.GetComponent<RectTransform>().position;

        // Dotween 애니매이션 시퀀스 생성
        Sequence sequence = DOTween.Sequence();
        
        // 시퀸스에 사이즈 축소, 현재위치에서 중앙 0.5f위쪽 위치로 이동 애니매이션 추가
        sequence.Append(cardOnHand.transform.DOScale(new Vector3(0.02f, 0.02f, 0f), duration));
        sequence.Join(cardOnHand.transform
                            .DOMove(new Vector3(0f, 0.5f, 0f), duration)
                            .SetEase(Ease.OutSine));

        // 시퀀스에 사이즈 축소, 오른쪽으로 90도 회전, 현재위치에서 화면의 우측하단 방향으로 포물선 이동 애니매이션 추가
        sequence.Append(cardOnHand.transform.DOScale(new Vector3(0.02f, 0.02f, 0f), duration));
        sequence.Join(cardOnHand.transform.DORotate(new Vector3(0f, 0f, -90f), duration));
        sequence.Join(cardOnHand.transform
                            .DOMove(trashDeckPosition, duration)
                            .SetEase(Ease.InOutCirc));
        sequence.OnComplete(() =>
        {
            // 애니매이션 시퀀스 모두 종료 시 카드 삭제 로직 수행   
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if (gamePlayerDeck.isLocalPlayer)
            {
                cardOnHand.isMoving = false;
                gamePlayerDeck.CmdDestroyCardOnHand(cardOnHand);
                DeckUI.instance.buttonEndTurn.interactable = true;
            }
        });
    }

    // CardOnHand 모두 trashDeck으로 버리는 애니매이션(역순으로 크기, 방향, 위치 변경)
    public void CardOnHandAllThrowAwaySequence(CardOnHand cardOnHand)
    {
        DeckUI.instance.buttonEndTurn.interactable = false;
        GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
        if(gamePlayerDeck.isLocalPlayer){
            float delay = (gamePlayerDeck.cardOnHands.Count - cardOnHand.index) * 0.1f;
            Vector3 trashDeckPosition = DeckUI.instance.buttonTrashDeck.GetComponent<RectTransform>().position;
            cardOnHand.isMoving = true;

            cardOnHand.transform.DOScale(new Vector3(0.02f, 0.02f, 0f), 0.3f);
            cardOnHand.transform.DORotate(new Vector3(0f, 0f, -90f), 0.3f);
            cardOnHand.transform
                    .DOMove(trashDeckPosition, 0.3f)
                    .SetEase(Ease.OutCirc)
                    .SetDelay(delay)
                    .OnComplete(() => {
                        cardOnHand.isMoving = false;
                        gamePlayerDeck.CmdDestroyCardOnHand(cardOnHand);
                        DeckUI.instance.buttonEndTurn.interactable = true;
                    });
        }    
    }


    // 현재 타겟팅 카드 화살표가 소환되어 있는지 여부 확인 함수
    public bool IsArrowSpawned()
    {
        if(NetworkClient.connection != null){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            return gamePlayerDeck.isArrowSpawned;
        }
        return false;
    }
}
