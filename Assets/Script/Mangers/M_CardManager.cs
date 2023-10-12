using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Mirror;
using DG.Tweening;
using ProjectD;

public class M_CardManager : NetworkBehaviour
{
    public static M_CardManager Instance = null;

    [Header("랜덤 시드값")]
    public int seedNumber = 0;

    [Header("현재 플레이어가 선택한 캐릭터의 카드 리스트")]
    public readonly SyncList<Card> cards = new SyncList<Card>();

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

    [Header("카드 마우스 오버시 Y값")]
    public float hoveredPositionY;

    [Header("카드 원래 사이즈")]
    public Vector3 cardOriginSize;

    [Header("카드 마우스 오버 사이즈")]
    public Vector3 cardOverSize;

    [Header("카드 밀려나는 정도값 범위")]
    public float cardOnHandShiftedRange;

    [Header("카드 정렬 순서 최대값")]
    public readonly int maxSortOrder = 999;

    [Header("화살표 활성화 상태 여부")]
    public bool isArrowActive = false;

    [Header("어빌리티 화살표 활성화 상태 여부")]
    public bool isAbilityArrowActive = false;

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

    public override void OnStartServer()
    {
        // 카드 DB 데이터에서 강화, 철귀이동, 고행카드를 제외한 카드 데이터를 추출하여 서버가 관리할 Synclist에 추가
        foreach(CardBase cardBase in CardData.instance.cards){
            if(!cardBase.cardNumber.Contains("_E") && !cardBase.cardNumber.Equals("HA") && !cardBase.cardCharacteristics.Exists((c) => c == CardCharacteristic.GOHENG)){
                Card card = new Card(cardBase);
                cards.Add(card);
            }
        }
    }

    void Start()
    {
        InitCardConfigValue();
    }

    void FixedUpdate()
    {
        SetCardOnHandPositionSymmetry();
    }

    // 카드 관련 기본값 설정(카드 크기, 위치, 회전과 관련된 값, Range로 조정 가능한 값들의 초기값)
    private void InitCardConfigValue()
    {
        cardOriginSize = new Vector3(1f, 1f, 1f);
        cardOverSize = cardOriginSize + new Vector3(0.45f, 0.45f, 0.45f);
        symmetryRange = 1.6f;
        symmetryPositionX_Range = 2.0f;
        symmetryPositionY_Range = 0.35f;
        symmetryRotationRange = 5.0f;
        cardOnHandShiftedRange = 1.7f;
        hoveredPositionY = 2.2f;
    }

    // 현재 플레이어의 CardOnHands 리스트를 통해 각 카드들의 위치, 회전, 크기 제어
    public void SetCardOnHandPositionSymmetry()
    {   
        if(NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId != 0){
            List<CardOnHand> cardOnHandsIsNotChoosed =
                    NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().cardOnHands.FindAll(card => !card.isChoosed); // 선택되지 않은 카드 리스트 필터
            int count = cardOnHandsIsNotChoosed.Count;
            if(count > 0){
                for(int i=0; i<count; i++){      
                    CardOnHand cardOnHand = cardOnHandsIsNotChoosed[i];
                    if(cardOnHand != null){
                        if(!cardOnHand.isMoving && !cardOnHand.isDrag && !cardOnHand.isUsed){
                            if(cardOnHand.isMouseOver){
                                Vector3 targetPosition = new Vector3(cardOnHand.originPosition.x, hoveredPositionY, cardOnHand.transform.localPosition.z);
                                cardOnHand.transform.localPosition = Vector3.Lerp(cardOnHand.transform.localPosition, targetPosition, Time.deltaTime * 10f);
                                cardOnHand.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                                cardOnHand.transform.localScale = cardOverSize;
                            }else{
                                cardOnHand.transform.GetComponent<SortingGroup>().sortingOrder = i; // 스프라이트 정렬 인덱스
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
                                    int mouseOveredIndex = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().cardOnHands.FindIndex((card) =>  card.isMouseOver);
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
            cardOnHand.transform.position = GameUIManager.instance.buttonPrefareDeck.transform.position;
            cardOnHand.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            cardOnHand.transform.localScale = new Vector3(0.02f, 0.02f, 0f);

            // Dotween 애니매이션 시퀀스 생성
            Sequence sequence = DOTween.Sequence();
            sequence.Append(cardOnHand.transform.DOScale(new Vector3(0.02f, 0.02f, 0f), 0.2f));
            sequence.Join(cardOnHand.transform.DORotate(new Vector3(0f, 0f, 0f), 0.2f)
                .SetDelay(index * 0.1f)
                .SetEase(Ease.OutSine)
                .OnComplete(() => {
                    cardOnHand.isMoving = false;
                    sequence.Kill();
                }));      
        }
    }

    public void CardOnHandDrawSequenceFromTrashDeck(CardOnHand cardOnHand, int index)
    {
        if(!cardOnHand.isChoosed){
            cardOnHand.isMoving = true;
            cardOnHand.transform.position = GameUIManager.instance.buttonTrashDeck.transform.position;
            cardOnHand.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            cardOnHand.transform.localScale = new Vector3(0.02f, 0.02f, 0f);

            // Dotween 애니매이션 시퀀스 생성
            Sequence sequence = DOTween.Sequence();
            sequence.Append(cardOnHand.transform.DOScale(new Vector3(0.02f, 0.02f, 0f), 0.2f));
            sequence.Join(cardOnHand.transform.DORotate(new Vector3(0f, 0f, 0f), 0.2f)
                .SetDelay(index * 0.1f)
                .SetEase(Ease.OutSine)
                .OnComplete(() => {
                    cardOnHand.isMoving = false;
                    sequence.Kill();
                }));      
        }
    }

    // CardOnHand 오브젝트 trashDeck으로 버리는 애니매이션 + 오브젝트 파괴 커맨드 호출
    public void CardOnHandThrowAwaySequence(CardOnHand cardOnHand)
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GameUIManager.instance.buttonEndTurn.interactable = false;        
            cardOnHand.isMoving = true;
            float duration = 0.3f;
            Vector3 trashDeckPosition = GameUIManager.instance.buttonTrashDeck.GetComponent<RectTransform>().position;
            GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();

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
                if (gamePlayerDeck.isOwned)
                {
                    cardOnHand.isMoving = false;
                    GameUIManager.instance.buttonEndTurn.interactable = true;
                    sequence.Kill();
                    NetworkClient.connection.identity.GetComponent<PlayerInterface>().destroyCards.Add(cardOnHand);
                }
            });
        }
    }

    // CardOnHand 모두 trashDeck으로 버리는 애니매이션(역순으로 크기, 방향, 위치 변경)
    public void CardOnHandAllThrowAwaySequence(CardOnHand cardOnHand)
    {
        GameUIManager.instance.buttonEndTurn.interactable = false;
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isOwned){
                float delay = (gamePlayerDeck.cardOnHands.Count - cardOnHand.index) * 0.1f;
                Vector3 trashDeckPosition = GameUIManager.instance.buttonTrashDeck.GetComponent<RectTransform>().position;
                cardOnHand.isMoving = true;
                cardOnHand.isUsed = true;

                cardOnHand.transform.DOScale(new Vector3(0.02f, 0.02f, 0f), 0.3f);
                cardOnHand.transform.DORotate(new Vector3(0f, 0f, -90f), 0.3f);
                cardOnHand.transform
                        .DOMove(trashDeckPosition, 0.3f)
                        .SetEase(Ease.OutCirc)
                        .SetDelay(delay)
                        .OnComplete(() => {
                            cardOnHand.isMoving = false;
                            gamePlayerDeck.CmdDestroyCardOnHand(cardOnHand);
                            GameUIManager.instance.buttonEndTurn.interactable = true;
                        });
            }   
        } 
    }

    // 덱 제거를 위해 선택된 카드들의 위치 및 크기 변경
    public void CardOnHandChooseForRemoveSequence(CardOnHand removeCardOnHand, int index)
    {
        // 카드 제거 팝업 위치로 카드 위치, 크기, 회전 변경
        removeCardOnHand.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        removeCardOnHand.transform.localScale = new Vector3(1f, 1f, 1f);

        Vector3 centerPosition = PopUpUIManager.instance.layoutCardOnHandForRemove.GetComponent<RectTransform>().position;
        Vector3 left = centerPosition - new Vector3(3.5f, 0f, 0f);
        Vector3 right = centerPosition + new Vector3(3.5f, 0f, 0f);
        Vector3 targetPosition = (index == 0) ? left : right;
        removeCardOnHand.transform.DOMove(targetPosition, 0.2f).SetEase(Ease.OutSine);
    }

    // 로컬 플레이어의 CardOnHand 오브젝트들의 sortingLayer 변경
    public void ChangeCardOnHandSortingLayerByName(string layerName)
    {
        // 로컬 플레이어의 카드 정렬 순서 변경
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
            foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands){
                cardOnHand.GetComponent<SortingGroup>().sortingLayerName = layerName;
                cardOnHand.cardOnHandCanvas.sortingLayerName = layerName;
            }
        }
    }

    // 로컬 플레이어의 CardOnHand 오브젝트들 중 마우스 오버되지 않은 카드들의 isShifted 변수 값 변경
    public void ChangeCardOnHandShiftState(CardOnHand mouseOveredCardOnHand, bool isShifted)
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
            foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands){
                if(cardOnHand != mouseOveredCardOnHand){
                    cardOnHand.isShifted = isShifted;
                }
            }
        }
    }

    // 로컬 플레이어의 모든 카드 제거
    public void RemoveAllCurrentPlayerCardOnHands()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
            foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands){
                // 영원 타입이 아닌 카드들만 제거
                bool isCardTypeImmortal = CardData.instance.CheckCardCharacteristic(cardOnHand.card, ProjectD.CardCharacteristic.YOUNGWON);
                if(!isCardTypeImmortal){
                    CardOnHandAllThrowAwaySequence(cardOnHand);
                }
            }
        }
    }

    // 로컬 플레이어의 모든 카드 제거(버린댁으로 보내지 않고 제거만 수행)
    public void RemoveAllCurrentPlayerCardOnHandsWithOutTrashDeck()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
            gamePlayerDeck.CmdDestroyAllCardOnHandWithOutTrashDeck();
        }    
    }

    // 로컬 플레이어 소유의 카드 제어 화살표 제거
    public void RemoveAllCurrentPlayerArrow()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
            gamePlayerDeck.cardCtrlArrow.RemoveCardCtrlArrow();
        }
    }

    // 로컬 플레이어의 PrefareDeck과 TrashDeck 데이터 모두 제거
    public void RemoveAllCurrentPlayerPrefareDeckAndTrashDeck()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
            gamePlayerDeck.CmdClearPrefareDeckAndTrashDeck();
        }   
    }

    // 로컬 플레이어의 Deck에 카드 데이터 추가
    public void AddCardDataToCurrentPlayerDeck(Card card)
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
            gamePlayerDeck.CmdAddDeck(card);
        }   
    }

    // 로컬 플레이어의 현재 소환된 CardOnHand의 상태 변수들 변경
    public void ChangeCurrentPlayerCardOnHandState(bool state)
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
            foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands){
                ResetCardAllState(cardOnHand, state);
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

    public string GetAdditionalValueFromDescription(string str)
    {
        TargetObject tar = null;
        if(isServer)
            tar = NetworkServer.spawned[NetworkClient.connection.identity.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerTarget>().targetObject].GetComponent<TargetObject>();
        else
            tar = NetworkClient.spawned[NetworkClient.connection.identity.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerTarget>().targetObject].GetComponent<TargetObject>();

        string[] splitString = str.Trim().Split(" ");
        for(int i = 0 ;i < splitString.Length ; i++)
        {
            if(splitString[i].ToCharArray()[0] == '!')
            {
                splitString[i] = splitString[i].Remove(0,1);
                int result = int.Parse(splitString[i]) + tar.GetBuffValue(BuffType.ICHI_ATTACK) + tar.GetBuffValue(BuffType.FLOWER);
                splitString[i] = "<color=green>" + result.ToString() + "</color>";
            }
            if(splitString[i].ToCharArray()[0] == '#')
            {
                splitString[i] = splitString[i].Remove(0,1);
                int result = int.Parse(splitString[i]) + tar.GetBuffValue(BuffType.ICHI_DEFENSE);
                splitString[i] = "<color=green>" + result.ToString() + "</color>";
            }
        }
        
        return string.Join(" ",splitString);
    }
}
