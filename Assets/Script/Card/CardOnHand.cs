using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using ProjectD;
using Mirror;
using DG.Tweening;
using TMPro;


public class CardOnHand : NetworkBehaviour
{
    [SyncVar (hook = nameof(OnChangeCardData))]
    public Card card;

    [SyncVar (hook = nameof(OnChangeParent))]
    public CardPocket parent;

    [SyncVar]
    public int index; // 추가 드로우된 CardOnHand의 생성 순서 인덱스

    [SyncVar]
    public bool isAddtionDrawCard = false; // 추가 드로우된 카드인지 여부

    [Header("CardOnHand Transform 및 정렬 관련 값들")]
    public SortingGroup sortingGroup;

    public Vector3 originPosition; // 초기 위치값

    public Vector3 arrowSpawnedCardPosition; // 화살표 소환된 카드의 위치값(화면 중앙 하단)

    [Header("CardOnHand 상태 변수값들")]
    public bool isMouseOverBuffer = false; // 마우스가 오브젝트 위에 있는지 여부

    public bool isMouseOver{
        get {return isMouseOverBuffer;}
        set { isMouseOverBuffer = value;
            OnChangedIsChoosedState();
        }
    } 

    public bool isExitComplete = true; // 마우스가 오브젝트에서 벗어난 상태 확인 여부(딜레이를 이용해 이벤트 연속 호출 방지용)

    public bool isDrag = false; // 드래그 상태인지 여부

    public bool isMoving = false; // 움직이는 상태인지 여부

    public bool isShifted = false; // 밀려난 상태인지 여부

    public bool isChoosed = false; // 카드 제거 팝업창에서 선택한 상태인지 여부

    public bool isUsed = false; // 사용된 상태인지 여부

    private bool hasOverPoint = false;

    public int originSortingOrder; // 마우스 오버되지 않은 원래 상태에서의 정렬값

    [Header("CardOnHand UI Canvas 컴포넌트")]
    public Canvas cardOnHandCanvas;

    [Header("CardOnHand SpriteRenderer 컴포넌트")]
    public SpriteRenderer cardBackground;
    public SpriteRenderer cardIllust;
    public SpriteRenderer cardImageFrame;
    public SpriteRenderer cardGradeFrame;
    public SpriteRenderer cardEmblem;

    [Header("CardOnHand Text 컴포넌트")]
    public TextMeshProUGUI textCardName;
    public TextMeshProUGUI textCardDescription;
    public TextMeshProUGUI textCardCost;

    [Header("CardOnHand 경험치 바")]
    public GameObject cardExpBar; // 경험치 바 
    public GameObject expBlockPrefab; // 경험치 바 내부 블록 오브젝트 프리팹
    public VerticalLayoutGroup verticalLayoutGroup;
    public List<GameObject> expBlocks = new List<GameObject>(); // 경험치 바 내부 블록 리스트

    [Header("CardOnHand 이펙트")]
    public ParticleSystem rippleParticle;
    public Material rippleRed;
    public Material rippleYellow;
    public Material rippleBlue;
    public TrailRenderer trailRenderer;

    public delegate void CardInfoChanged();
    public CardInfoChanged CardInfoChangedEvent;
    public Transform popUpWIndowParent;
    public Transform popUpCardParent;
    public GameObject popUpWindow;
    public GameObject popUpCard;

    public List<GameObject> createdPopUpWindow = new List<GameObject>();

    void Start()
    {
        cardOnHandCanvas.worldCamera = Camera.main;
        CardInfoChangedEvent += OnChangedCardInfo;
    }

    void OnDestroy()
    {
        transform.DOKill();
    }

    // 클라이언트에서 생성 시 현재 플레이어 참조값 미리 캐싱
    public override void OnStartClient()
    {
        InitCardTemplateByCharacter(card);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        transform.DOKill();
    }

    // CardData의 스프라이트 데이터로부터 선택한 캐릭터의 카드 이미지 세팅
    private void InitCardTemplateByCharacter(Card card)
    {
        switch(card.baseCard.character){
            case Character.GEORK:
                AYellowpaper.SerializedCollections.SerializedDictionary<string, Sprite> georkCardSprites = CardData.instance.characterCardTemplate[Character.GEORK];
                InitCardTemplateByCardType(card, georkCardSprites);
                InitCardIllust(card);
                InitCardTemplateByCardEnhanced(card, georkCardSprites);
                InitCardExpBar(card, georkCardSprites);
                InitCardTrailColor(ProjectD.ColorUtils.HexToColor("#FF6400"));
                rippleParticle.GetComponent<ParticleSystemRenderer>().material = rippleYellow;
                break;
            case Character.ERIS:
                AYellowpaper.SerializedCollections.SerializedDictionary<string, Sprite> erisCardSprites = CardData.instance.characterCardTemplate[Character.ERIS];
                InitCardTemplateByCardType(card, erisCardSprites);
                InitCardIllust(card);
                InitCardTemplateByCardEnhanced(card, erisCardSprites);
                InitCardExpBar(card, erisCardSprites);
                InitCardTrailColor(ProjectD.ColorUtils.HexToColor("#0068A1"));
                rippleParticle.GetComponent<ParticleSystemRenderer>().material = rippleBlue;
                break;
            case Character.HONGDANHYANG:
                AYellowpaper.SerializedCollections.SerializedDictionary<string, Sprite> danhyangCardSprites = CardData.instance.characterCardTemplate[Character.HONGDANHYANG];
                InitCardTemplateByCardType(card, danhyangCardSprites);
                InitCardIllust(card);
                InitCardTemplateByCardEnhanced(card, danhyangCardSprites);
                InitCardExpBar(card, danhyangCardSprites);
                InitCardTrailColor(ProjectD.ColorUtils.HexToColor("#FF0000"));
                rippleParticle.GetComponent<ParticleSystemRenderer>().material = rippleRed;
                break;
        }
    }

    // 카드 TrailRenderer 색상 설정
    private void InitCardTrailColor(Color color)
    {
        float alpha = 1.0f;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(alpha, 1f), new GradientAlphaKey(alpha, 0f) }
        );
        trailRenderer.colorGradient = gradient;
    }

    // 카드 타입에 따라 외형 틀 세팅
    private void InitCardTemplateByCardType(Card card, AYellowpaper.SerializedCollections.SerializedDictionary<string, Sprite> sprites)
    {
        if(!card.baseCard.cardNumber.Equals("HA")){
            switch(card.baseCard.cardType){
                case CardType.ATTACK:
                    cardBackground.sprite = sprites[Const.ATTACK_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.ATTACK_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.ATTACK_EMBLEM];
                    break;
                case CardType.BLESS:
                    cardBackground.sprite = sprites[Const.BLESS_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.BLESS_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.BLESS_EMBLEM];
                    break;
                case CardType.STRATEGY:
                    cardBackground.sprite = sprites[Const.STRATEGY_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.STRATEGY_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.STRATEGY_EMBLEM];
                    break;
                case CardType.HERO:
                    cardBackground.sprite = sprites[Const.HERO_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.HERO_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.HERO_EMBLEM];
                    break;
                case CardType.CURSE:
                    cardBackground.sprite = sprites[Const.CURSE_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.CURSE_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.CURSE_EMBLEM];
                    break;
            }
        }
    }

    // 카드 일러스트 세팅
    private void InitCardIllust(Card card)
    {
        if(!card.baseCard.cardNumber.Contains("HA")){
            if(card.baseCard.cardNumber.Contains("_E")){
                // 강화카드의 경우 _E 문자열을 제거하여 아틀라스에서 스프라이트 조회
                int idx = card.baseCard.cardNumber.IndexOf("_E");
                if(idx != -1){
                    string cardNumber = card.baseCard.cardNumber.Substring(0, idx);
                    cardIllust.sprite = CardData.instance.cardIllustAtlas.GetSprite(cardNumber);
                }
            }else{
                cardIllust.sprite = CardData.instance.cardIllustAtlas.GetSprite(card.baseCard.cardNumber);
            }
        }
    }

    // 카드 강화 상태 프레임 세팅
    private void InitCardTemplateByCardEnhanced(Card card, AYellowpaper.SerializedCollections.SerializedDictionary<string, Sprite> sprites)
    {
        if(card.isEnhanced){
            cardGradeFrame.sprite = sprites[Const.ENHANCE_NORMAL_GRADE_FRAME];
        }else{
            cardGradeFrame.sprite = sprites[Const.NORMAL_GRADE_FRAME];
        }
    }

    // 카드 경험치 바 초기화 : card 데이터에서 최대 경험치 정보를 가져와 해당 숫자 만큼의 경험치 바 내부 블록 생성
    private void InitCardExpBar(Card card, AYellowpaper.SerializedCollections.SerializedDictionary<string, Sprite> sprites)
    {
        // 철귀 이동카드는 경험치 오브젝트 초기화 제외
        if(!card.baseCard.cardNumber.Equals("HA")){
            // 최대 경험치 만큼 내부 블록 생성
            for(int i=0; i<card.baseCard.maxExperience; i++){
                GameObject expBlock = Instantiate(expBlockPrefab);
                expBlock.transform.SetParent(verticalLayoutGroup.transform, false);
                expBlock.GetComponent<Image>().sprite = sprites[Const.EXP_BAR_INACTIVE];
                expBlocks.Add(expBlock);
            }
            // expBlocks 역순으로 전환(블록이 아래부터 쌓이도록)
            expBlocks.Reverse();
            // 경험치 블록 리스트에서 현재 카드의 경험치 숫자 만큼 블록 생상을 변경
            for(int j=0; j<card.experience; j++){
                expBlocks[j].GetComponent<Image>().sprite = sprites[Const.EXP_BAR_ACTIVE];
            }
        }   
    }

    // 오브젝트에 마우스 포인터 진입할 때 이벤트
    void OnMouseEnter()
    {
        if(isOwned && M_TurnManager.instance.IsActivePhase()){
            if(!isUsed && !isMoving && !isChoosed && !IsArrowActive() && !IsCardControllableByPopUpActive() && isExitComplete){
                isMouseOver = true;
                transform.GetComponent<SortingGroup>().sortingOrder =  M_CardManager.instance.maxSortOrder;
                cardOnHandCanvas.sortingOrder =  M_CardManager.instance.maxSortOrder;
                M_CardManager.instance.ChangeCardOnHandShiftState(this, true);
                AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("event_cardstore_mouseover_3"));
                M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
                CmdChangeMouseOverdState(true);
                M_TurnManager.instance.CandidatedTargetIndicatorByCard(card.baseCard.validTarget);
            }
            TextDetector.instance.StartTextDetect();
        }
    }

    // 오브젝트에서 마우스 포인터 나갈 때 이벤트
    void OnMouseExit()
    {
        if(isOwned && M_TurnManager.instance.IsActivePhase()){
            if(!isUsed && !isMoving && !IsArrowActive() && !IsCardControllableByPopUpActive()){
                isMouseOver = false;
                M_CardManager.instance.ChangeCardOnHandShiftState(this, false);
                StartCoroutine(MouseExitDelay());
                CmdChangeMouseOverdState(false);
                M_TurnManager.instance.DisableTargetIndicator();
            }
            TextDetector.instance.StopTextDetect();
        }
    }

    // MouseExit 이벤트 딜레이 함수
    IEnumerator MouseExitDelay()
    {
        isExitComplete = false;
        yield return new WaitForSeconds(0.1f);
        isExitComplete = true;
    }

    // 오브젝트에 마우스 왼쪽버튼 누를 때 이벤트
    void OnMouseDown()
    {
        if(isOwned && M_TurnManager.instance.IsActivePhase()){
            if(!isUsed && !isMoving && !IsArrowActive()){
                // 일반적인 경우에 마우스 왼쪽 버튼 클릭 시
                if(!IsCardControllableByPopUpActive()){
                    isDrag = true;
                    arrowSpawnedCardPosition = transform.position; // 드래그 시작전 마우스 클릭 시점에 카드의 절대 위치값 저장(이 시점의 카드 위치는 중앙 하단). 화살표 소환 시 카드를 다시 중앙 하단으로 이동시키기 위함.
                }
                // 덱 [제거] 팝업창이 뜬 경우에 마우스 왼쪽 버튼 클릭 시
                if(IsCardOnHandRemovePopUpActive()){
                    if(isChoosed){
                        NetworkClient.connection.identity.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().RemoveChoosedCardOnHands(this); // 클릭한 카드를 제거용 카드 배열에서 제거
                    }else{
                        NetworkClient.connection.identity.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().AddChoosedCardOnHands(this); // 클릭한 카드를 제거용 카드 배열에 추가
                    }  
                }

                // 카드 설명 팝업창 삭제
                for(int i = createdPopUpWindow.Count -1 ; i >= 0 ; i --)
                {
                    Destroy(createdPopUpWindow[i]);
                }
                createdPopUpWindow.Clear();
            }
        }
    }

    // 오브젝트를 마우스로 드래그 할 때 이벤트
    void OnMouseDrag()
    {
        if(!isUsed &&isOwned && M_TurnManager.instance.IsActivePhase()){
            if(isDrag && !IsCardControllableByPopUpActive() && !IsCardOnHandRemovePopUpActive()){
                DragCardOnHand(this);
                MovePositionArrowSpawnedCardOnHand(this);
            }
        }
    }

    // 오브젝트에서 마우스 왼쪽버튼 뗄 때 이벤트
    void OnMouseUp()
    {
        if(isOwned && M_TurnManager.instance.IsActivePhase()){
            if(isDrag && !IsCardControllableByPopUpActive() && !IsCardOnHandRemovePopUpActive()){
                // Targetable 카드가 아닌 경우 마우스 뗄 때 위치가 화면 중앙을 넘어갈 경우 액션 수행
                if(!card.baseCard.isTargetable && (Input.mousePosition.y > Screen.height / 2)){
                    int totalCost = 0;
                    if(card.baseCard.cardCharacteristics.Exists(x => x == CardCharacteristic.EUNHASOO)) // 은하수 카드 코스트 계산
                    {
                        if(card.baseCard.cardType == NetworkClient.connection.identity.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().previousCardType)
                        {
                            totalCost = ( card.baseCard.cost + card.costAddition - 1 );
                            if(totalCost < 0)totalCost = 0;
                        }
                        else
                            totalCost = ( card.baseCard.cost + card.costAddition + 1 );
                    }
                    else
                        totalCost = card.baseCard.cost + card.costAddition ;
                    if(totalCost > NetworkClient.connection.identity.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().currentIchi) // 카드 코스트 계산 하는곳
                    {
                        isDrag = false;
                        isMoving = false;
                        isMouseOver = false;
                        return;
                    }

                    GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
                    CmdEnQueueCardData(gamePlayerDeck);
                    M_CardManager.instance.CardOnHandThrowAwaySequence(this);
                    M_TurnManager.instance.DisableTargetIndicator();
                }
                else
                {
                    isDrag = false;
                    isMoving = false;
                    isMouseOver = false;
                }
            }
        }
    }

    // 마우스 좌표에 따라 카드 오브젝트 드래그
    private void DragCardOnHand(CardOnHand cardOnHand)
    {
        // 드래그 중 오브젝트의 정렬값은 최대값. 항상 맨 위에 랜더링
        cardOnHand.transform.GetComponent<SortingGroup>().sortingOrder =  M_CardManager.instance.maxSortOrder;
        cardOnHand.cardOnHandCanvas.sortingOrder = M_CardManager.instance.maxSortOrder;
        // 오브젝트 위치는 마우스 커서 위치
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cardOnHand.transform.position = new Vector2(mousePosition.x, mousePosition.y);
        cardOnHand.transform.localScale = M_CardManager.instance.cardOverSize;
        cardOnHand.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

        // 드래그 위치가 화면 3분의 1을 넘어갈 때 유효한 타겟 오브젝트에 인디케이터 활성화. 화살표 생성되는 카드는 드래그 위치가 화살표 생성시 자동으로 중앙하단으로 이동하므로 호출되지 않고, 논 타겟 카드만 해당되는 케이스.
        float screenPositionY = Screen.height / 3;
        Vector3 cardOnHandPosition = Camera.main.WorldToScreenPoint(cardOnHand.transform.position);
        if(cardOnHandPosition.y > screenPositionY && !hasOverPoint){
            M_TurnManager.instance.EnableTargetIndiCatorByArrow(cardOnHand.card.baseCard.validTarget, true, null);
            hasOverPoint = true; 
        }else if(cardOnHandPosition.y <= screenPositionY && hasOverPoint){
            M_TurnManager.instance.EnableTargetIndiCatorByArrow(cardOnHand.card.baseCard.validTarget, false, null);
            hasOverPoint = false; 
        }
    }

    // 타겟팅 카드일 경우, 드래그중 위치가 화면 하단부 3분의1을 넘어가면 화살표 생성 후 카드의 위치를 중앙으로 이동
    private void MovePositionArrowSpawnedCardOnHand(CardOnHand cardOnHand)
    {
        if( Input.mousePosition.y > Screen.height / 3 ){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.GetTotalCostOfCardOnHand(cardOnHand) > gamePlayerDeck.currentIchi) 
            {
                cardOnHand.isMoving = false;
                cardOnHand.isDrag = false;
                GamePlayer gamePlayer = gamePlayerDeck.GetComponent<GamePlayer>();
                TargetObject currentPlayer = M_TurnManager.instance.GetCurrentPlayerTargetObject(gamePlayer);
                currentPlayer.ShowCostNotReaminBubble(gamePlayer);
            }
            else if(cardOnHand.card.baseCard.isTargetable)
            {
                cardOnHand.isMoving = true;
                cardOnHand.isDrag = false;
                cardOnHand.transform.GetComponent<SortingGroup>().sortingOrder = M_CardManager.instance.maxSortOrder;
                NetworkClient.connection.identity.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().cardCtrlArrow.InitCardCtrlArrow(cardOnHand);
                NetworkClient.connection.identity.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().SetArrowOwnCardOnHand(cardOnHand);
                cardOnHand.transform
                    .DOMove(new Vector3(0f, arrowSpawnedCardPosition.y, arrowSpawnedCardPosition.z), 0.4f)
                    .SetEase(Ease.OutSine);
            }
        }
    }

    // 팝업 활성화 상태일 때 카드 제어가 가능한 팝업의 활성화 여부 확인 함수
    private bool IsCardControllableByPopUpActive()
    {
        int index = PopUpUIManager.instance.popUpList.FindIndex((popUp) => popUp.activeSelf);
        bool isOpenPopUpExist = (index != -1) ? true : false; // 활성화 되어있는 팝업이 존재하는지 유무
        bool isOpenOptionMenu = OptionUIManager.instance.optionPopUp.activeSelf; // 옵션메뉴창 활성화 되어있는지 유무
        return isOpenPopUpExist || isOpenOptionMenu;
    }

    // CardOnHandRemove PopUp 활성화 여부 확인 함수
    private bool IsCardOnHandRemovePopUpActive()
    {
        return PopUpUIManager.instance.cardOnHandRemovePopUp.activeSelf;
    }

    // 화살표 활성화 여부 확인 함수
    private bool IsArrowActive()
    {
        return M_CardManager.instance.isArrowActive;
    }


    void OnChangedIsChoosedState()
    {
        if(isMouseOverBuffer == true)
        {
            return;
        }
        else
        {
            for(int i = createdPopUpWindow.Count -1 ; i >= 0 ; i --)
            {
                Destroy(createdPopUpWindow[i]);
            }
            createdPopUpWindow.Clear();
        }
    }

    // --------------------------------------------------------------- Command Method -----------------------------------------------------------------//
    
    [Command]
    void CmdEnQueueCardData(GamePlayerDeck gamePlayerDeck)
    {
        gamePlayerDeck.serverCardPredictQueue.Enqueue((this, null));
    }

    // CardOnHand 마우스 오버상태 변경 커맨드
    [Command]
    public void CmdChangeMouseOverdState(bool isMouseOvered)
    {
        RpcMouseOveredState(isMouseOvered);
    }

    // --------------------------------------------------------------- Rpc Method -----------------------------------------------------------------//
    
    // CardOnHand 마우스 오버상태 변경 수신 RPC 
    [ClientRpc]
    public void RpcMouseOveredState(bool isMouseOvered)
    {
        sortingGroup.sortingOrder = isMouseOvered ?  M_CardManager.instance.maxSortOrder : originSortingOrder;
        cardOnHandCanvas.sortingOrder = isMouseOvered ? M_CardManager.instance.maxSortOrder : originSortingOrder;
    }

    // 소환된 CardOnHand를 CardPocket의 자식오브젝트로 설정
    [ClientRpc]
    public void RpcCardOnHandSetParent(GameObject cardPocket)
    {
        transform.SetParent(cardPocket.transform);
    }

    // --------------------------------------------------------------- SyncVar Hook -----------------------------------------------------------------//

    // 카드 정보 뷰 업데이트
    public void OnChangeCardData(Card oldCard, Card newCard)
    {
        // 정상적으로 Card가 동기화 되지 않았을경우 업데이트 취소
        if(newCard == null)return;
        if(newCard.baseCard == null)return;
        if(newCard.baseCard.name == null)return;
        if(newCard.baseCard.name == "")return;

        if(newCard.experience >= newCard.baseCard.maxExperience || newCard.isEnhanced || newCard.tempEnhanced)
        {
            textCardName.color = Color.green;
            textCardName.text = CardData.instance.cards.Find(x => x.cardNumber == newCard.baseCard.cardNumber + "_E").name;
            textCardDescription.text = M_CardManager.instance.GetAdditionalValueFromDescription(CardData.instance.cards.Find(x => x.cardNumber == newCard.baseCard.cardNumber + "_E").description);
        }
        else
        {
            textCardName.text = newCard.baseCard.name;
            textCardDescription.text = M_CardManager.instance.GetAdditionalValueFromDescription(newCard.baseCard.description);
        }

        textCardDescription.text += '\n';
        textCardDescription.text += '\n';
        foreach(CardCharacteristic character in newCard.baseCard.cardCharacteristics)
            textCardDescription.text += "<b><color=yellow>" + CardData.instance.cardCharacteristicToString[character] + "</color></b>  ";
        foreach(CardCharacteristic character in newCard.cardCharacteristics)
            textCardDescription.text += "<b><color=yellow>" + CardData.instance.cardCharacteristicToString[character] + "</color></b>  ";

        int totalCost = NetworkClient.connection.identity.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().GetTotalCostOfCardOnHand(this);
        if( totalCost == card.baseCard.cost )
        {
            textCardCost.text = totalCost.ToString();
        } 
        else
        {
            if( totalCost > card.baseCard.cost )
                textCardCost.text = "<b><color=red>" + totalCost.ToString() + "</color></b>";
            else
                textCardCost.text = "<b><color=green>"+ totalCost.ToString() + "</color></b>";
        }
    }

    void OnChangedCardInfo()
    {
        OnChangeCardData(card,card);
    }

    // 카드 부모오브젝트인 CardPocket 참조값 변경 이벤트 수신
    public void OnChangeParent(CardPocket oldCardPocket, CardPocket newCardPocket)
    {
        transform.SetParent(newCardPocket.transform);
        transform.position = GameUIManager.instance.buttonPrefareDeck.transform.position; // 부모 설정 후 위치를 뽑을 덱 버튼 위치로 재설정
    }
}