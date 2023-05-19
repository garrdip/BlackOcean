using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DG.Tweening;

public class M_CardManager : NetworkBehaviour
{
    public static M_CardManager Instance = null;

    public GameObject cardOnHandsPanel; // 카드 모음 패널 오브젝트

    public Vector3 cardCollidableSize; // 충돌 판정이 가능한 원래의 충돌체 크기값

    public Vector3 cardNoneCollidableSize; // 충돌 판정이 되지 않도록 크기를 줄인 충돌체 크기값

    [Header("cardOnHandsPanel의 위치 Y값 범위")]
    [Range(-5.0f, 2.0f)]
    public float cardOnHandsPanelPositionY_Range;

    [Header("카드 대칭 계산값 변수 범위")]
    [Range(-2.5f, 2.5f)]
    public float symmetryRange;

    [Header("카드 대칭 위치 X값 범위")]
    [Range(0f, 3.0f)]
    public float symmetryPositionX_Range;

    [Header("카드 대칭 위치 Y값 범위")]
    [Range(-0.5f, 0.5f)]
    public float symmetryPositionY_Range;

    [Header("카드 대칭 회전값 범위")]
    [Range(-20.0f, 20.0f)]
    public float symmetryRotationRange;

    [Header("카드 원래 사이즈")]
    public Vector3 cardOriginSize;

    [Header("카드 마우스 오버 사이즈")]
    public Vector3 cardOverSize;


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

    void Start()
    {
        InitSymmetryValue();
        cardCollidableSize = new Vector3(22f, 30f, 1f);
        cardNoneCollidableSize = new Vector3(0f, 0f, 0f);
        cardOriginSize = new Vector3(0.1f, 0.1f, 0.1f);
        cardOverSize = cardOriginSize + new Vector3(0.05f, 0.05f, 0.05f);
    }

    void FixedUpdate()
    {
        SetCardOfHandPositionSymmetry();
    }

    // Range로 변경가능한 값들 초기화
    private void InitSymmetryValue()
    {
        cardOnHandsPanelPositionY_Range = -4.0f;
        symmetryRange = 1.5f;
        symmetryPositionX_Range = 1.15f;
        symmetryPositionY_Range = 0.2f;
        symmetryRotationRange = 5.0f;
    }

    // 현재 플레이어의 CardOnHands 리스트를 통해 각 카드들의 위치, 회전, 크기 제어
    public void SetCardOfHandPositionSymmetry()
    {
        cardOnHandsPanel.transform.position = new Vector3(0f, cardOnHandsPanelPositionY_Range, 0f); // 카드 모음 패널의 위치       
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            int count = gamePlayerDeck.cardOnHands.Count;
            if(count > 0){
                for(int i=0; i<count; i++){      
                    CardOnHand cardOnHand =  gamePlayerDeck.cardOnHands[i];
                    if(cardOnHand != null && !cardOnHand.isMoving && !cardOnHand.isDrag){
                        if(cardOnHand.isMouseOver){
                            Vector3 targetPosition = new Vector3(cardOnHand.transform.localPosition.x, cardOnHand.hoveredPositionY, cardOnHand.transform.localPosition.z);
                            cardOnHand.transform.localPosition = Vector3.Lerp(cardOnHand.transform.localPosition, targetPosition, Time.deltaTime * 10f);
                            cardOnHand.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                            cardOnHand.transform.localScale = cardOverSize;
                        }else{
                            // 대칭값 계산
                            int leftCount = (count - 1) / 2;
                            int rightCount = count - leftCount - 1;
                            float symmetryValue = (count % 2 == 0) ? ((i - leftCount) * symmetryRange - 0.75f) : ((i - leftCount) * symmetryRange);

                            // 위치값(카드 개수에 따라 좌우 대칭값 계산하여 각 카드의 x, y 좌표 설정)
                            Vector3 position = new Vector3(symmetryValue * symmetryPositionX_Range, -Mathf.Abs(symmetryValue) * symmetryPositionY_Range, 0f);
                            cardOnHand.transform.localPosition = Vector3.Lerp(cardOnHand.transform.localPosition, position, Time.deltaTime * 10f);

                            // 회전값
                            cardOnHand.transform.localRotation = Quaternion.Euler(0f, 0f, -symmetryValue * symmetryRotationRange);

                            // 크기값
                            cardOnHand.transform.localScale = Vector3.Lerp(cardOnHand.transform.localScale, cardOriginSize, Time.deltaTime * 10f);
                        }
                    }
                }
            }
        }
    }

    // CardOnHand 오브젝트들의 인덱스값에 따라 순차적인 움직임으로 날아오는 애니매이션 + Moving플래그 변수 조정
    public void CardOnHandDrawSequence(CardOnHand cardOnHand, int index)
    {
        cardOnHand.isMoving = true;
        Transform cardTransform = cardOnHand.gameObject.transform;
        cardTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);

        // Dotween 애니매이션 시퀀스 생성
        Sequence sequence = DOTween.Sequence();
        sequence.Append(cardTransform.DOScale(new Vector3(0.02f, 0.02f, 0f), 0.2f));
        sequence.Join(cardTransform.DORotate(new Vector3(0f, 0f, 0f), 0.2f)
            .SetDelay(index * 0.1f)
            .SetEase(Ease.OutSine)
            .OnComplete(() => {
                cardOnHand.isMoving = false;
            }));
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
            if(NetworkClient.connection != null && NetworkClient.active){   
                GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
                if (gamePlayerDeck.isLocalPlayer)
                {
                    cardOnHand.isMoving = false;
                    gamePlayerDeck.CmdDestroyCardOnHand(cardOnHand);
                    DeckUI.instance.buttonEndTurn.interactable = true;
                }
            }
        });
    }

    // CardOnHand 모두 trashDeck으로 버리는 애니매이션(역순으로 크기, 방향, 위치 변경)
    public void CardOnHandAllThrowAwaySequence(CardOnHand cardOnHand)
    {
        DeckUI.instance.buttonEndTurn.interactable = false;
        if(NetworkClient.connection != null && NetworkClient.active){
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
    }


    // 현재 플레이어의 CardOnHand 오브젝트의 충돌체 크기 조정(마우스 오버되지 않은 카드들의 충돌체 사이즈를 줄여서 충돌판정을 받지 않도록 함)
    public void ChangeCardOnHandColliderSize(CardOnHand mouseOveredCardOnHand, Vector3 size)
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands){
                if(cardOnHand != mouseOveredCardOnHand){
                    cardOnHand.GetComponent<BoxCollider>().size = size;
                }
            }
        }
    }

    // 각 플레이어들 소유의 카드와 화살표 생성
    public void SpawnPlayerOwnedCardAndArrow()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                gamePlayerDeck.CmdSpawnCardPocket();
                gamePlayerDeck.CmdSpawnArrowEmitter();
            }
        }
    }
}
