using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Mirror;
using DG.Tweening;
using ProjectD;

public class M_CardManager : NetworkSingletonD<M_CardManager>
{
    private const int maxCardOnHandCount = 10; // CardOnHand 최대 갯수

    [Header("현재 선택한 플레이어의 GamePlayerDeck 참조변수")]
    public GamePlayerDeck currentGamePlayerDeck;

    [Header("랜덤 시드값")]
    public int seedNumber = 0;

    [Header("DB에서 받아온 카드정보를 저장할 SyncList")]
    public readonly SyncList<Card> cards = new SyncList<Card>();

    [Header("카드 대칭 계산값 변수 범위")]
    [Range(-2.5f, 2.5f)]
    public float symmetryRange;

    [Header("카드 대칭 위치 X값 범위")]
    [Range(0f, 3.0f)]
    public float symmetryPositionX_Range;

    [Header("카드 대칭 위치 Y값 범위")]
    [Range(0.01f, 0.5f)]
    public float symmetryPositionY_Range;

    [Header("카드 그룹 곡률 범위")]
    [Range(0.01f, 0.3f)]
    public float symmetryCurveRange;

    [Header("카드 대칭 회전값 범위")]
    [Range(-20.0f, 20.0f)]
    public float symmetryRotationRange;

    [Header("카드 밀려나는 정도값 범위")]
    [Range(0f, 20.0f)]
    public float cardOnHandShiftedRange;

    [Header("카드 마우스 오버시 Y값")]
    public float hoveredPositionY;

    [Header("카드 원래 사이즈")]
    public Vector3 cardOriginSize;

    [Header("카드 마우스 오버 사이즈")]
    public Vector3 cardOverSize;

    [Header("카드 정렬 순서 최대값")]
    public readonly int maxSortOrder = 999;

    [Header("카드 이동 궤적 표시용 TrailRenderer 오브젝트")]
    public GameObject CardTrail;

    [Header("화살표 활성화 상태 여부")]
    public bool isArrowActive = false;

    [Header("어빌리티 화살표 활성화 상태 여부")]
    public bool isAbilityArrowActive = false;

    public List<(Card,TargetObject)> curseCardQueue = new List<(Card, TargetObject)>();

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

    protected override void Start()
    {
        DontDestroyOnLoad(gameObject);
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.persistentManagers.Add(gameObject.name, gameObject);
        InitCardConfigValue();
    }

    void Update()
    {
        SetCardOnHandPositionSymmetry();
    }

    // 카드 관련 기본값 설정(카드 크기, 위치, 회전과 관련된 값, Range로 조정 가능한 값들의 초기값)
    private void InitCardConfigValue()
    {
        cardOriginSize = new Vector3(1f, 1f, 1f);
        cardOverSize = cardOriginSize + new Vector3(0.45f, 0.45f, 0.45f);
        symmetryRange = 1.6f;
        symmetryPositionX_Range = 2.3f;
        symmetryPositionY_Range = 0.12f;
        symmetryCurveRange = 0.03f;
        symmetryRotationRange = 3.5f;
        cardOnHandShiftedRange = 1.5f;
        hoveredPositionY = 2.1f;
    }

    // 현재 플레이어의 CardOnHands 리스트를 통해 각 카드들의 위치, 회전, 크기 제어
    public void SetCardOnHandPositionSymmetry()
    {   
        if(currentGamePlayerDeck != null){
            List<CardOnHand> cardOnHandsIsNotChoosed = currentGamePlayerDeck.cardOnHands.FindAll(card => !card.isChoosed); // 선택되지 않은 카드 리스트 필터
            int count = cardOnHandsIsNotChoosed.Count;
            if(count > 0){
                for(int i=0; i<count; i++){      
                    CardOnHand cardOnHand = cardOnHandsIsNotChoosed[i];
                    if(cardOnHand != null){
                        if(!cardOnHand.isMoving && !cardOnHand.isDrag && !cardOnHand.isUsed && !cardOnHand.isAddtionDrawCard){
                            if(cardOnHand.isMouseOver){
                                cardOnHand.sortingGroup.sortingOrder = maxSortOrder;
                                cardOnHand.cardOnHandCanvas.sortingOrder = maxSortOrder;

                                // 위치값
                                Vector3 cardOverPosition = new Vector3(cardOnHand.originPosition.x, hoveredPositionY, cardOnHand.transform.localPosition.z);
                                cardOnHand.transform.localPosition = Vector3.Lerp(cardOnHand.transform.localPosition, cardOverPosition, Time.deltaTime * 10f);
                                // 회전값
                                Quaternion cardOverRotation = Quaternion.Euler(0f, 0f, 0f);
                                cardOnHand.transform.localRotation = Quaternion.Lerp(cardOnHand.transform.rotation, cardOverRotation, Time.deltaTime * 10f);
                                // 크기값
                                cardOnHand.transform.localScale = cardOverSize;

                                if(Vector3.Distance(cardOnHand.transform.localPosition,cardOverPosition) < 0.01f && cardOnHand.transform.localRotation.x < 0.01f && cardOnHand.transform.localRotation.y < 0.01f && cardOnHand.createdPopUpWindow.Count == 0)
                                {
                                    foreach(Infomation info in cardOnHand.card.baseCard.info)
                                    {
                                        GameObject newPopUpWindow = Instantiate(cardOnHand.popUpWindow,new Vector3(0,0,0),Quaternion.identity);
                                        newPopUpWindow.GetComponent<PopUpWindow>().SetPopUpWinwdowText(info);
                                        newPopUpWindow.transform.SetParent(cardOnHand.popUpWIndowParent);
                                        newPopUpWindow.transform.localScale = new Vector3(1f,1f,0);
                                        cardOnHand.createdPopUpWindow.Add(newPopUpWindow);
                                    }
                                    foreach(CardCharacteristic cardCharacteristic in cardOnHand.card.cardCharacteristics)
                                    {
                                        GameObject newPopUpWindow = Instantiate(cardOnHand.popUpWindow,new Vector3(0,0,0),Quaternion.identity);
                                        newPopUpWindow.GetComponent<PopUpWindow>().SetPopUpWinwdowText(CardData.instance.cardCharacteristicToString[cardCharacteristic]);
                                        newPopUpWindow.transform.SetParent(cardOnHand.popUpWIndowParent);
                                        newPopUpWindow.transform.localScale = new Vector3(1f,1f,0);
                                        cardOnHand.createdPopUpWindow.Add(newPopUpWindow);
                                    }
                                    foreach(CardCharacteristic cardCharacteristic in cardOnHand.card.baseCard.cardCharacteristics)
                                    {
                                        GameObject newPopUpWindow = Instantiate(cardOnHand.popUpWindow,new Vector3(0,0,0),Quaternion.identity);
                                        newPopUpWindow.GetComponent<PopUpWindow>().SetPopUpWinwdowText(CardData.instance.cardCharacteristicToString[cardCharacteristic]);
                                        newPopUpWindow.transform.SetParent(cardOnHand.popUpWIndowParent);
                                        newPopUpWindow.transform.localScale = new Vector3(1f,1f,0);
                                        cardOnHand.createdPopUpWindow.Add(newPopUpWindow);
                                    }
                                    foreach(string cardNumber in cardOnHand.card.baseCard.cardInfo)
                                    {
                                        Debug.Log("Card  생 성 !");
                                        GameObject popUpCard = Instantiate(cardOnHand.popUpCard);
                                        popUpCard.transform.SetParent(cardOnHand.popUpCardParent);
                                        popUpCard.transform.localScale = new Vector3(0.005f, 0.005f,0.005f);
                                        popUpCard.transform.localPosition = new Vector3(0,0,0);
                                        popUpCard.GetComponent<CardOnDeck>().card = new Card(CardData.instance.cards.Find(card => card.cardNumber == cardNumber));
                                        cardOnHand.createdPopUpWindow.Add(popUpCard);
                                    }
                                }
                            }else{
                                cardOnHand.originSortingOrder = i;
                                cardOnHand.sortingGroup.sortingOrder = i;
                                cardOnHand.cardOnHandCanvas.sortingOrder = i;

                                // 대칭값 계산
                                float symmetryValue = (i - ((count - 1) / 2.0f));
                                
                                // 위치값(카드 개수에 따라 좌우 대칭값 계산하여 각 카드의 x, y 좌표 설정)
                                float xPosition = symmetryValue * (symmetryPositionX_Range + ((maxCardOnHandCount - count) * 0.1f));
                                float yPosition = -0.5f - (Mathf.Pow(Mathf.Abs(symmetryValue), 2f) * (symmetryPositionY_Range + ((maxCardOnHandCount - count) * symmetryCurveRange)));
                                Vector3 symmetryPosition = new Vector3(xPosition , yPosition, 0f);
                                cardOnHand.originPosition = symmetryPosition;

                                // 회전값
                                Quaternion symmetryRotation = Quaternion.Euler(0f, 0f, -symmetryValue * (symmetryRotationRange + ((maxCardOnHandCount - count))));
                                cardOnHand.transform.localRotation = Quaternion.Lerp(cardOnHand.transform.rotation,  symmetryRotation, Time.deltaTime * 10f);

                                // 크기값
                                cardOnHand.transform.localScale = Vector3.Lerp(cardOnHand.transform.localScale, cardOriginSize, Time.deltaTime * 10f);

                                // 마우스 오버되지 않은 나머지 카드들은 shift 되어 밀려남. 마우스 오버된 카드를 기준으로 좌우 대칭으로 멀어질 수록 밀려나는 위치의 정도가 감소.
                                if(cardOnHand.isShifted){
                                    int mouseOveredIndex = currentGamePlayerDeck.cardOnHands.FindIndex((card) =>  card.isMouseOver);
                                    float shiftedValue = 0f;
                                    if(i != mouseOveredIndex){
                                        shiftedValue = cardOnHandShiftedRange / (i - mouseOveredIndex);
                                    }
                                    Vector3 shiftPosition = new Vector3(symmetryPosition.x + shiftedValue, symmetryPosition.y, symmetryPosition.z);
                                    cardOnHand.transform.localPosition = Vector3.Lerp(cardOnHand.transform.localPosition, shiftPosition, Time.deltaTime * 10f);
                                }else{
                                    cardOnHand.transform.localPosition = Vector3.Lerp(cardOnHand.transform.localPosition, symmetryPosition, Time.deltaTime * 10f);
                                } 
                            }
                        }
                    }
                }
            }
        }
    }

    // 현재 플레이어의 GamePlayerDeck 참조값 설정
    public void SetCurrentGamePlayerDeck(GamePlayerDeck gamePlayerDeck)
    {
        currentGamePlayerDeck = gamePlayerDeck;
    }

    // CardOnHand 오브젝트들의 인덱스값에 따라 순차적인 움직임으로 날아오는 애니매이션 + Moving플래그 변수 조정
    public void CardOnHandDrawSequence(CardOnHand cardOnHand, int index)
    {
        if(!cardOnHand.isChoosed){
            cardOnHand.isMoving = true;
            cardOnHand.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            cardOnHand.transform.localScale = new Vector3(0.02f, 0.02f, 0f);
            Sequence sequence = DOTween.Sequence();
            sequence.InsertCallback(0.5f, () => {
                cardOnHand.transform
                    .DORotate(new Vector3(0f, 0f, 0f), 0.2f)
                    .SetDelay(index * 0.1f)
                    .SetEase(Ease.OutSine)
                    .OnComplete(() => {
                        cardOnHand.rippleParticle.gameObject.SetActive(true);
                        cardOnHand.isMoving = false;
                        sequence.Kill();
                        cardOnHand.transform.DOKill();
                        if(cardOnHand.isOwned){
                            AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("combat_card_draw"));
                            M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
                        }
                    });
            });
        }
    }

    // 버린덱에서 뽑을덱으로 TrailRenderer 오브젝트 이동 시퀀스(뽑을덱 없어서 버린덱에서 충전할 때)
    public void CardOnHandChargedSequence(Card card, int index, Vector3 startPosition, Vector3 endPosition, System.Action callback)
    {
        // Card Trail 오브젝트 생성
        GameObject cardTrail = Instantiate(CardTrail, startPosition, Quaternion.identity);
        TrailRenderer trailRenderer = cardTrail.GetComponent<TrailRenderer>();
        
        // 캐릭터별 Card Trail 색상 설정
        float alpha = 1.0f;
        Gradient gradient = new Gradient();
        switch(card.baseCard.character){
            case Character.GEORK:
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(ProjectD.ColorUtils.HexToColor("#FF6400"), 0f), new GradientColorKey(Color.white, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(alpha, 1f), new GradientAlphaKey(alpha, 0f) }
                );    
                break;
            case Character.ERIS:
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(ProjectD.ColorUtils.HexToColor("#0068A1"), 0f), new GradientColorKey(Color.white, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(alpha, 1f), new GradientAlphaKey(alpha, 0f) }
                );   
                break;
            case Character.HONGDANHYANG:
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(ProjectD.ColorUtils.HexToColor("#FF0000"), 0f), new GradientColorKey(Color.white, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(alpha, 1f), new GradientAlphaKey(alpha, 0f) }
                );
                break;
        }
        trailRenderer.colorGradient = gradient;

        // Card Trail 오브젝트 버린덱에서 뽑을덱으로 포물선 이동 애니매이션
        Vector3 midPoint = ((startPosition + endPosition) / 2) + new Vector3(0f, 2f, 0f);
        Vector3[] path = new Vector3[] { startPosition, midPoint, endPosition };
        cardTrail.transform
            .DOPath(path, 0.5f, PathType.CatmullRom)
            .SetEase(Ease.Linear)
            .SetDelay(0.1f * index)
            .OnComplete(() => {
                callback();
                Destroy(cardTrail);
            });
    }

    // 추가 드로우된 CardOnHand 이동 시퀀스
    public void CardOnHandAdditionDrawSequence(CardOnHand cardOnHand, int index)
    {
        if(!cardOnHand.isChoosed){
            cardOnHand.isMoving = true;
            DeckDrawPopUp deckDrawPopUp = PopUpUIManager.instance.deckDrawPopUp.GetComponent<DeckDrawPopUp>();
            cardOnHand.transform.position = cardOnHand.isOwned ? deckDrawPopUp.addtionDrawCardSlots[cardOnHand.index].transform.position : new Vector3(0f, -100f, 0f);
            Sequence sequence = DOTween.Sequence();
            sequence.Append(cardOnHand.transform.DORotate(new Vector3(0f, 0f, 0f), 0.2f)
                .SetDelay(index * 0.1f)
                .SetEase(Ease.OutSine)
                .OnComplete(() => {
                    cardOnHand.rippleParticle.gameObject.SetActive(true);
                    cardOnHand.isMoving = false;
                    currentGamePlayerDeck.CmdChangeCardOnHandIsAddtionDraw(cardOnHand, false);
                    sequence.Kill();
                    if(cardOnHand.isOwned){
                        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("combat_card_draw"));
                        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
                    }
                })
            );      
        }
    }

    // CardOnHand가 버린덱에서 패로 되돌아올 때 애니매이션
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
                    cardOnHand.rippleParticle.gameObject.SetActive(true);
                    cardOnHand.isMoving = false;
                    sequence.Kill();
                    if(cardOnHand.isOwned){
                        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("combat_card_draw"));
                        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
                    }
                }));      
        }
    }

    // CardOnHand 오브젝트 버리는 애니매이션(카드 사용시 호출) - CHALNA 특성이 있는 경우는 잊혀진 덱, 없는 경우는 버려진 덱
    public void CardOnHandThrowAwaySequence(CardOnHand cardOnHand)
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GameUIManager.instance.buttonEndTurn.interactable = false;        
            cardOnHand.isMoving = true;
            cardOnHand.isUsed = true;
            float duration = 0.5f;
            bool isChalna = CardData.instance.CheckCardCharacteristic(cardOnHand.card, CardCharacteristic.CHALNA);
            Vector3 position = isChalna ? GameUIManager.instance.ForgottenDeck.GetComponent<RectTransform>().position : GameUIManager.instance.buttonTrashDeck.GetComponent<RectTransform>().position;
            GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();

            // Dotween 애니매이션 시퀀스 생성
            Sequence sequence = DOTween.Sequence();
            
            // 시퀸스에 회전 초기화, 현재위치에서 중앙 0.5f위쪽 위치로 이동 애니매이션 추가
            sequence.Prepend(cardOnHand.transform.DORotate(new Vector3(0f, 0f, 0f), 0.5f).OnComplete(() => {
                cardOnHand.trailRenderer.enabled = true;
            }));
            sequence.Join(cardOnHand.transform
                                .DOMove(new Vector3(0f, 0.5f, 0f), 0.5f)
                                .SetEase(Ease.OutSine));

            // 시퀀스에 사이즈 축소, 오른쪽으로 90도 회전, 현재위치에서 화면의 우측하단 방향으로 포물선 이동 애니매이션 추가
            sequence.Append(cardOnHand.transform.DOScale(new Vector3(0f, 0f, 0f), duration));
            sequence.Join(cardOnHand.transform.DORotate(new Vector3(0f, 0f, -90f), duration));
            sequence.Join(cardOnHand.transform.DOMove(position, duration).SetEase(Ease.InOutCirc));
            sequence.OnComplete(() =>
            {
                // 애니매이션 시퀀스 모두 종료 시 카드 삭제 로직 수행
                if(gamePlayerDeck.isOwned){
                    GameUIManager.instance.buttonEndTurn.interactable = true;
                    sequence.Kill();
                    cardOnHand.isUsed = true;
                    NetworkClient.connection.identity.GetComponent<PlayerInterface>().destroyCards.Add(cardOnHand);
                    ChangeCurrentPlayerCardOnHandState(false);
                }
            });
        }
    }

    // CardOnHand 오브젝트 잊혀진 덱으로 버리는 애니매이션
    public void CardOnHandThrowAwaySequenceToForgotenDeck(CardOnHand cardOnHand)
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GameUIManager.instance.buttonEndTurn.interactable = false;        
            cardOnHand.isMoving = true;
            cardOnHand.isUsed = true;
            float duration = 0.5f;
            Vector3 position = GameUIManager.instance.ForgottenDeck.GetComponent<RectTransform>().position;
            GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();

            // Dotween 애니매이션 시퀀스 생성
            Sequence sequence = DOTween.Sequence();
            
            // 시퀸스에 회전 초기화, 현재위치에서 중앙 0.5f위쪽 위치로 이동 애니매이션 추가
            sequence.Prepend(cardOnHand.transform.DORotate(new Vector3(0f, 0f, 0f), 0.5f).OnComplete(() => {
                cardOnHand.trailRenderer.enabled = true;
            }));
            sequence.Join(cardOnHand.transform
                                .DOMove(new Vector3(0f, 0.5f, 0f), 0.5f)
                                .SetEase(Ease.OutSine));

            // 시퀀스에 사이즈 축소, 오른쪽으로 90도 회전, 현재위치에서 화면의 우측하단 방향으로 포물선 이동 애니매이션 추가
            sequence.Append(cardOnHand.transform.DOScale(new Vector3(0f, 0f, 0f), duration));
            sequence.Join(cardOnHand.transform.DORotate(new Vector3(0f, 0f, -90f), duration));
            sequence.Join(cardOnHand.transform.DOMove(position, duration).SetEase(Ease.InOutCirc));
            sequence.OnComplete(() =>
            {
                // 애니매이션 시퀀스 모두 종료 시 카드 삭제 로직 수행
                if(gamePlayerDeck.isOwned){
                    GameUIManager.instance.buttonEndTurn.interactable = true;
                    sequence.Kill();
                    cardOnHand.isUsed = true;
                    NetworkClient.connection.identity.GetComponent<PlayerInterface>().destroyCards.Add(cardOnHand);
                    ChangeCurrentPlayerCardOnHandState(false);
                }
            });
        }
    }

    // CardOnHand 모두 trashDeck으로 버리는 애니매이션(턴 종료시 호출)
    public void CardOnHandAllThrowAwaySequence(CardOnHand cardOnHand, GamePlayerDeck gamePlayerDeck)
    {
        GameUIManager.instance.buttonEndTurn.interactable = false;
        float duration = 0.5f;
        float delay = (gamePlayerDeck.cardOnHands.Count - cardOnHand.sortingGroup.sortingOrder) * 0.1f;
        Vector3 position = GameUIManager.instance.buttonTrashDeck.GetComponent<RectTransform>().position;
        
        Sequence sequence = DOTween.Sequence();
        sequence.PrependCallback(() => {
            cardOnHand.trailRenderer.enabled = true;
            cardOnHand.isMoving = true;
            cardOnHand.isUsed = true;
            if(cardOnHand.isOwned){
                AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("combat_card_discard"));
                M_SoundManager.instance.PlaySFX(audioClip, audioClip.length, 0.5f);
            }
        });
        sequence.Append(cardOnHand.transform.DOScale(new Vector3(0.02f, 0.02f, 0f), duration));
        sequence.Join(cardOnHand.transform.DORotate(new Vector3(0f, 0f, -90f), duration));
        sequence.Join(cardOnHand.transform.DOMove(position, duration).SetEase(Ease.OutCirc).SetDelay(delay));
        sequence.OnComplete(() => {
                    GameUIManager.instance.buttonEndTurn.interactable = true;
                    gamePlayerDeck.CmdDestroyCardOnHandToTrash(cardOnHand);
                    ChangeCurrentPlayerCardOnHandState(false);
                    sequence.Kill();
                });
    }

    // 패 제거를 위해 선택된 카드들의 위치 변경
    public void CardOnHandChooseForRemoveSequence(CardOnHand removeCardOnHand, int index)
    {
        removeCardOnHand.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        removeCardOnHand.transform.localScale = new Vector3(1f, 1f, 1f);
        CardOnHandRemovePopUp cardOnHandRemovePopUp = PopUpUIManager.instance.cardOnHandRemovePopUp.GetComponent<CardOnHandRemovePopUp>();
        Vector3 targetPosition = cardOnHandRemovePopUp.removeCardSlots[index].gameObject.transform.position;
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

    // 로컬 플레이어 소유의 카드 제어 화살표 제거
    public void RemoveAllCurrentPlayerArrow()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
            foreach(GamePlayer gamePlayer in playerInterface.ownedPlayers){
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
                gamePlayerDeck.cardCtrlArrow.RemoveCardCtrlArrow();
            }
        }
    }

    // 로컬 플레이어의 모든 카드 제거
    public void RemoveAllCurrentPlayerCardOnHands()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
            foreach(GamePlayer gamePlayer in playerInterface.ownedPlayers){
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
                foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands){
                    // 영원 타입이 아닌 카드들만 제거
                    bool isCardTypeImmortal = CardData.instance.CheckCardCharacteristic(cardOnHand.card, ProjectD.CardCharacteristic.YOUNGWON);
                    if(cardOnHand.card.baseCard.cardType == CardType.CURSE) // 게오르크 고행길 효과
                    {
                        CMDCurseCardEffect(cardOnHand.card,NetworkClient.spawned[gamePlayerDeck.GetComponent<GamePlayerTarget>().targetObject].GetComponent<TargetObject>());
                    }
                    if(!isCardTypeImmortal){
                        CardOnHandAllThrowAwaySequence(cardOnHand, gamePlayerDeck);
                    }
                }
                playerInterface.cardThrowAwayDone = true;
            }
        }
    }

    [Command(requiresAuthority = false)]
    void CMDCurseCardEffect(Card card, TargetObject tar)
    {
        curseCardQueue.Add((card,tar));
    }

    // 로컬 플레이어의 모든 카드 제거(버린댁으로 보내지 않고 제거만 수행)
    public void RemoveAllCurrentPlayerCardOnHandsWithOutTrashDeck()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
            foreach(GamePlayer gamePlayer in playerInterface.ownedPlayers){
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
                gamePlayerDeck.CmdDestroyAllCardOnHandWithOutTrashDeck();
            }
        }    
    }
    
    // 로컬 플레이어의 PrefareDeck과 TrashDeck 데이터 모두 제거
    public void RemoveAllCurrentPlayerPrefareDeckAndTrashDeck()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
            foreach(GamePlayer gamePlayer in playerInterface.ownedPlayers){
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
                gamePlayerDeck.CmdClearPrefareDeckAndTrashDeck();
            }
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

    // 로컬 플레이어 카드 셔플 수행후 PrefareDeck에 추가 요청
    public void PrefareCardWithSuffle()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            foreach(GamePlayer gamePlayer in NetworkClient.localPlayer.GetComponent<PlayerInterface>().ownedPlayers){
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
                gamePlayerDeck.CmdAddPrefareDeckWithShuffle();
            }
        }
    }

    // CardOnHand 정렬값 갱신
    public void RefreshCardOnHandsSortingOrder(SyncList<CardOnHand> cardOnHands)
    {
        for(int i=0; i<cardOnHands.Count; i++){
            CardOnHand cardOnHand = cardOnHands[i];
            cardOnHand.originSortingOrder = i; // 마우스 오버되기 이전의 원래 정렬 인덱스
            cardOnHand.sortingGroup.sortingOrder = i; // 스프라이트 정렬 인덱스
            cardOnHand.cardOnHandCanvas.sortingOrder = i;  // 카드 UI 요소의 정렬 인덱스
        }
    }

    // 어빌리티 버튼 활성화 상태 변경
    public void ChangeAbilityButtonActiveState(bool isActive)
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            foreach(GamePlayer gamePlayer in NetworkClient.localPlayer.GetComponent<PlayerInterface>().ownedPlayers){
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
                if(gamePlayerDeck.abilityButton != null){
                    gamePlayerDeck.abilityButton.gameObject.SetActive(isActive);
                }
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
        if(!NetworkClient.spawned.ContainsKey(NetworkClient.connection.identity.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerTarget>().targetObject))return str;
        if(isServer)
            tar = NetworkServer.spawned[NetworkClient.connection.identity.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerTarget>().targetObject].GetComponent<TargetObject>();
        else
            tar = NetworkClient.spawned[NetworkClient.connection.identity.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerTarget>().targetObject].GetComponent<TargetObject>();
        int totalFlower = 0;
        string[] splitString = str.Trim().Split(" ");
        for(int i = 0 ;i < splitString.Length ; i++)
        {
            foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
                totalFlower += tar.GetBuffValue(BuffType.FLOWER,target);
            if(splitString[i].ToCharArray()[0] == '!') // !피해량
            {
                splitString[i] = splitString[i].Remove(0,1);
                int parseValue;
                if(int.TryParse(splitString[i], out parseValue)){
                    int result = parseValue + tar.GetBuffValue(BuffType.ICHI_ATTACK) + totalFlower;
                    splitString[i] = "<color=green>" + result.ToString() + "</color>";
                }else{
                    // TODO : splitString[i]가 정수로 변경 불가능한 문자열인 경우 처리
                }
            }
            if(splitString[i].ToCharArray()[0] == '#') // #방어도
            {
                splitString[i] = splitString[i].Remove(0,1);
                int result = int.Parse(splitString[i]) + tar.GetBuffValue(BuffType.ICHI_DEFENSE);
                splitString[i] = "<color=green>" + result.ToString() + "</color>";
            }
            if(splitString[i].ToCharArray()[0] == '^') // ^체력
            {
                splitString[i] = splitString[i].Remove(0,1);
                int result = int.Parse(splitString[i]);
                splitString[i] = "<#FF7F00>" + result.ToString() + "</color>";
            }
            if(splitString[i].ToCharArray()[0] == '&') // &크기
            {
                splitString[i] = splitString[i].Remove(0,1);
                int result = int.Parse(splitString[i]);
                splitString[i] = "<color=purple>" + result.ToString() + "</color>";
            }
            if(splitString[i].ToCharArray()[0] == '$') // $피해량$타수
            {
                splitString[i] = splitString[i].Remove(0,1);
                string[] data = splitString[i].Trim().Split("$");
                int result = int.Parse(data[0]) + tar.GetBuffValue(BuffType.ICHI_ATTACK) + totalFlower;
                string color = CardData.instance.colorList[2];
                splitString[i] = "<color=green>" + result.ToString() + "</color>" + " 를 " + color + data[1] + "</color>" + "번";
            }
        }
        
        return string.Join(" ",splitString);
    }

    public IEnumerator CurseCardOperation()
    {
        Debug.Log("Start CurseCardEffect!");
        Debug.Log(curseCardQueue.Count);
        for (int i = 0; i < curseCardQueue.Count; i++)
        {
            yield return CardData.instance.CurseCardEffect(curseCardQueue[i].Item1, curseCardQueue[i].Item2);
        }
        curseCardQueue.Clear();
        M_TurnManager.instance.phase = BattleTurn.MONSTER_ORDERSELECT;
        yield return null;
    }
}
