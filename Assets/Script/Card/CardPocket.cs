using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CardPocket : NetworkBehaviour
{
    private Vector3 hidePosition;
    private Vector3 showPosition;
    public Collider dragTarget;
    public GamePlayerDeck currentPlayerDeck;


    void Start()
    {
        transform.SetParent(DeckUI.instance.DeckListPanel.transform);
        transform.localPosition = new Vector3(0f, 1f, 0f);
        if(NetworkClient.connection != null){
            currentPlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
        }
    }

    void Update()
    {
        SetCardOfHandPositionSymmetry();
    }

    // 현재 플레이어의 CardOnHands 리스트를 통해 각 카드들의 위치, 회전, 크기 제어
    public void SetCardOfHandPositionSymmetry()
    {
        if(NetworkClient.connection != null){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            int count = gamePlayerDeck.cardOnHands.Count;
            if(count > 0){
                for(int i=0; i<count; i++){      
                    CardOnHand cardOnHand =  gamePlayerDeck.cardOnHands[i];
                    if(cardOnHand != null && !cardOnHand.isMoving){
                        if(cardOnHand.isMouseOver){
                            Vector3 targetPosition = new Vector3(cardOnHand.transform.localPosition.x, cardOnHand.hoveredPositionY, cardOnHand.transform.localPosition.z);
                            cardOnHand.transform.localPosition = Vector3.Lerp(cardOnHand.transform.localPosition, targetPosition, Time.deltaTime * 10f);
                            cardOnHand.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                            cardOnHand.transform.localScale = cardOnHand.targetScale;
                        }else{
                            // 대칭 위치값 계산
                            int leftCount = (count - 1) / 2;
                            int rightCount = count - leftCount - 1;
                            float symmetryPosition = (count % 2 == 0) ? ((i - leftCount) * 1.5f - 0.75f) : ((i - leftCount) * 1.5f + 0f);

                            // 위치값(카드 개수에 따라 좌우 대칭값 계산하여 각 카드의 x, y 좌표 설정)
                            Vector3 position = new Vector3(symmetryPosition, -Mathf.Abs(symmetryPosition) * 0.15f, 0f);
                            cardOnHand.transform.localPosition = Vector3.Lerp(cardOnHand.transform.localPosition, position, Time.deltaTime * 10f);

                            // 회전값
                            cardOnHand.transform.localRotation = Quaternion.Euler(0f, 0f, -symmetryPosition * 1.8f);

                            // 크기값
                            cardOnHand.transform.localScale = Vector3.Lerp(cardOnHand.transform.localScale, cardOnHand.originScale, Time.deltaTime * 10f);  
                        }
                    }
                }
            }
        }
    }
}