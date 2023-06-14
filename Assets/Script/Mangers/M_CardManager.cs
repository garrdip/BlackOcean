using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DG.Tweening;

public class M_CardManager : NetworkBehaviour
{
    public static M_CardManager Instance = null;

    [Header("랜덤 시드값")]
    public int seedNumber = 0;

    [Header("GamePlayerDeck 참조값 캐싱")]
    public GamePlayerDeck gamePlayerDeck;

    [Header("카드 모음 패널 오브젝트")]
    public GameObject cardOnHandsPanel;

    [Header("충돌 판정이 가능한 원래의 충돌체 크기값")]
    public Vector3 cardCollidableSize;

    [Header("충돌 판정이 되지 않도록 크기를 줄인 충돌체 크기값")]
    public Vector3 cardNoneCollidableSize;

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

    [Header("카드 밀려나는 정도값 범위")]
    public float cardOnHandShiftedRange;

    [Header("카드 정렬 순서 최대값")]
    public readonly int maxSortOrder = 999;


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
        if(NetworkClient.connection != null && NetworkClient.active){
            gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
        }
        InitSymmetryValue();
        cardCollidableSize = new Vector3(22f, 30f, 1f);
        cardNoneCollidableSize = new Vector3(0f, 0f, 0f);
        cardOriginSize = new Vector3(0.1f, 0.1f, 0.1f);
        cardOverSize = cardOriginSize + new Vector3(0.05f, 0.05f, 0.05f);
    }

    void FixedUpdate()
    {
        SetCardOnHandPositionSymmetry();
    }

    // Range로 변경가능한 값들 초기화
    private void InitSymmetryValue()
    {
        cardOnHandsPanelPositionY_Range = -4.0f;
        symmetryRange = 1.5f;
        symmetryPositionX_Range = 1.15f;
        symmetryPositionY_Range = 0.2f;
        symmetryRotationRange = 5.0f;
        cardOnHandShiftedRange = 1f;
    }

    // 현재 플레이어의 CardOnHands 리스트를 통해 각 카드들의 위치, 회전, 크기 제어
    public void SetCardOnHandPositionSymmetry()
    {
        cardOnHandsPanel.transform.position = new Vector3(0f, cardOnHandsPanelPositionY_Range, 0f); // 카드 모음 패널의 위치       
        if(gamePlayerDeck != null){
            int count = gamePlayerDeck.cardOnHands.Count;
            if(count > 0){
                for(int i=0; i<count; i++){      
                    CardOnHand cardOnHand =  gamePlayerDeck.cardOnHands[i];
                    if(cardOnHand != null){
                        if(!cardOnHand.isMoving && !cardOnHand.isDrag && !cardOnHand.isChoosed){
                            if(cardOnHand.isMouseOver){
                                Vector3 targetPosition = new Vector3(cardOnHand.originPosition.x, cardOnHand.hoveredPositionY, cardOnHand.transform.localPosition.z);
                                cardOnHand.transform.localPosition = Vector3.Lerp(cardOnHand.transform.localPosition, targetPosition, Time.deltaTime * 10f);
                                cardOnHand.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                                cardOnHand.transform.localScale = cardOverSize;
                            }else{
                                cardOnHand.transform.GetComponent<SpriteRenderer>().sortingOrder = i; // 스프라이트 정렬 인덱스
                                cardOnHand.cardOnHandCanvas.sortingOrder = i; // 카드 이름 및 설명 텍스트 요소의 정렬 인덱스
                                cardOnHand.transform.SetSiblingIndex(i); // 오브젝트 스택 순서 인덱스

                                // 대칭값 계산
                                int leftCount = (count - 1) / 2;
                                int rightCount = count - leftCount - 1;
                                float symmetryValue = (count % 2 == 0) ? ((i - leftCount) * symmetryRange - 0.75f) : ((i - leftCount) * symmetryRange);

                                // 위치값(카드 개수에 따라 좌우 대칭값 계산하여 각 카드의 x, y 좌표 설정)
                                Vector3 symmetryPosition = new Vector3(symmetryValue * symmetryPositionX_Range, -Mathf.Abs(symmetryValue) * symmetryPositionY_Range, 0f);
                                cardOnHand.transform.localPosition = Vector3.Lerp(cardOnHand.transform.localPosition, symmetryPosition, Time.deltaTime * 10f);
                                cardOnHand.originPosition = symmetryPosition;

                                // 회전값
                                cardOnHand.transform.localRotation = Quaternion.Euler(0f, 0f, -symmetryValue * symmetryRotationRange);

                                // 크기값
                                cardOnHand.transform.localScale = Vector3.Lerp(cardOnHand.transform.localScale, cardOriginSize, Time.deltaTime * 10f);

                                // 마우스 오버되지 않은 나머지 카드들은 shift 되어 밀려남. 마우스 오버된 카드를 기준으로 좌우 대칭으로 멀어질 수록 밀려나는 위치의 정도가 감소.
                                if(cardOnHand.isShifted){
                                    int mouseOveredIndex = gamePlayerDeck.cardOnHands.FindIndex((card) =>  card.isMouseOver);
                                    float shiftedValue = 0f;
                                    if(i != mouseOveredIndex){
                                        shiftedValue = cardOnHandShiftedRange / (i - mouseOveredIndex);
                                    }
                                    Vector3 shiftPosition = new Vector3(symmetryPosition.x + shiftedValue, symmetryPosition.y, symmetryPosition.z);
                                    cardOnHand.transform.localPosition = Vector3.Lerp(cardOnHand.transform.localPosition, shiftPosition, Time.deltaTime * 10f);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    // CardOnHand 오브젝트들의 인덱스값에 따라 순차적인 움직임으로 날아오는 애니매이션 + Moving플래그 변수 조정
    public void CardOnHandDrawSequence(CardOnHand cardOnHand, int index)
    {
        if(!cardOnHand.isChoosed){
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
    }

    // CardOnHand 오브젝트 trashDeck으로 버리는 애니매이션 + 오브젝트 파괴 커맨드 호출
    public void CardOnHandThrowAwaySequence(CardOnHand cardOnHand)
    {
        DeckUI.instance.buttonEndTurn.interactable = false;        
        cardOnHand.isMoving = true;
        float duration = 0.3f;
        Vector3 trashDeckPosition = DeckUI.instance.buttonTrashDeck.GetComponent<RectTransform>().position;

        // Dotween 애니매이션 시퀀스 생성
        Sequence sequence = DOTween.Sequence();
        
        // 시퀸스에 회전 초기화, 현재위치에서 중앙 0.5f위쪽 위치로 이동 애니매이션 추가
        sequence.Prepend(cardOnHand.transform.DORotate(new Vector3(0f, 0f, 0f), 0.5f));
        sequence.Join(cardOnHand.transform
                            .DOMove(new Vector3(0f, 0.5f, 0f), 0.5f)
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

    // 덱 제거를 위해 선택된 카드들의 위치 및 크기 변경
    public void CardOnHandChooseForRemoveSequence(SyncList<CardOnHand> removeCardOnHands)
    {
        // 카드 제거 팝업 위치로 카드 위치 변경 및 크기, 회전 변경
        Vector3 centerPosition = DeckUI.instance.LayoutCardOnHandForRemove.GetComponent<RectTransform>().position;
        Vector3 left = centerPosition - new Vector3(2f, 0f, 0f);
        Vector3 right = centerPosition + new Vector3(2f, 0f, 0f);

        for (int i = 0; i < removeCardOnHands.Count; i++)
        {
            CardOnHand cardOnHand = removeCardOnHands[i];
            cardOnHand.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            cardOnHand.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);

            Vector3 targetPosition = (i == 0) ? left : right;
            cardOnHand.transform.DOMove(targetPosition, 0.2f).SetEase(Ease.OutSine);
        }
    }


    // 로컬 플레이어의 CardOnHand 오브젝트의 충돌체 크기 조정(마우스 오버되지 않은 카드들의 충돌체 사이즈를 줄여서 충돌판정을 받지 않도록 함)
    public void ChangeCardOnHandColliderSize(CardOnHand mouseOveredCardOnHand, Vector3 size)
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands){
                    if(cardOnHand != mouseOveredCardOnHand){
                        cardOnHand.GetComponent<BoxCollider>().size = size;
                    }
                }
            }
        }
    }

    // 로컬 플레이어의 CardOnHand 오브젝트들의 sortingLayer 변경
    public void ChangeCardOnHandSortingLayerByName(string layerName)
    {
        // 로컬 플레이어의 카드 정렬 순서 변경
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands){
                    cardOnHand.GetComponent<SpriteRenderer>().sortingLayerName = layerName;
                    cardOnHand.cardOnHandCanvas.sortingLayerName = layerName;
                }
                if(gamePlayerDeck.cardOnHandForRemove != null){
                    // 버릴카드 정렬 순서 변경
                    gamePlayerDeck.cardOnHandForRemove.GetComponent<SpriteRenderer>().sortingLayerName = layerName;
                    gamePlayerDeck.cardOnHandForRemove.cardOnHandCanvas.sortingLayerName = layerName;
                }
            }
        }
    }

    // 선택된 제거용 카드 있을경우 카드 제거
    public void RemoveChoosedCardOnHand()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer && gamePlayerDeck.cardOnHandForRemove != null){
                CardOnHandThrowAwaySequence(gamePlayerDeck.cardOnHandForRemove);
            }
        }
    }

    // 로컬 플레이어의 CardOnHand 오브젝트들 중 마우스 오버되지 않은 카드들의 isShifted 변수 값 변경
    public void ChangeCardOnHandShiftState(CardOnHand mouseOveredCardOnHand, bool isShifted)
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands){
                    if(cardOnHand != mouseOveredCardOnHand){
                        cardOnHand.isShifted = isShifted;
                    }
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
                gamePlayerDeck.CmdAddPrefareDeckWithShuffle();
            }
        }
    }

    // 로컬 플레이어의 모든 카드 제거
    public void RemoveAllCurrentPlayerCardOnHands()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands){
                    // 영원 타입이 아닌 카드들만 제거
                    bool isCardTypeImmortal = CardData.instance.CheckCardCharacteristic(cardOnHand.card, ProjectD.CardCharacteristic.YOUNGWON);
                    if(!isCardTypeImmortal){
                        CardOnHandAllThrowAwaySequence(cardOnHand);
                    }
                }
            }
        }
    }

    // 로컬 플레이어 소유의 카드 제어 화살표 제거
    public void RemoveAllCurrentPlayerArrow()
    {
         if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                gamePlayerDeck.cardCtrlArrow.RemoveCardCtrlArrow();
            }
        }
    }

    // 카드와 타겟 데이터 큐에 저장
    public void EnQueueCardTargetPair(Card card, TargetObject targetObject, NetworkIdentity conn, CardCtrlArrow cardCtrlArrow)
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if (gamePlayerDeck.isLocalPlayer){
                gamePlayerDeck.CmdEnQueueCardTargetPair(card, targetObject, conn, cardCtrlArrow);
            }
        }
    }

    // CardOnHand의 상태변수값 모두 변경
    public void ResetCardAllState(CardOnHand cardOnHand, bool state)
    {
        cardOnHand.isDrag = state;
        cardOnHand.isMouseOver = state;
        cardOnHand.isMoving = state;
        cardOnHand.isShifted = state;
        cardOnHand.isChoosed = state;
    }

    // 피셔 예이츠 셔플 알고리즘 함수
    public void Shuffle<T>(SyncList<T> list)
    {
        System.Random random = new System.Random(seedNumber); // 시드값을 이용한 랜덤(시드값 같을 시 동일한 값 산출)

        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
