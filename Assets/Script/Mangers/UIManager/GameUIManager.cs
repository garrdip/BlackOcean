using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Mirror;
using Gpm.Ui;

public class GameUIManager : SingletonD<GameUIManager>
{
    [Header("게임 오브젝트")]
    public GameObject RootGameObject;
    public GameObject TestUI;

    [Header("카메라 사이즈값")]
    public static float battelSceneCameraSize = 10.8f; // 전투씬에서 카메라 크기값
    public static float mapSceneCameraSize = 6.0f; // 맵씬에서 카메라 크기값

    [Header("화면 전환 UI")]
    public Image screenTransition;
    public Image screenFade;
    public enum ScreenTransitionMode {
        Fade,
        Transition
    }
    public ScreenTransitionMode screenTransitionMode; // 스크린 전환 모드(페이드 인 아웃, 블록트랜지션 인 아웃)

    [Header("카드 전투 UI")]
    public GameObject CardOnHandsPanel;
    public HorizontalLayoutGroup CurrentItchIconLayout;
    public HorizontalLayoutGroup MaxItchIconLayout;
    public GameObject CurrentItchPrefab;
    public GameObject MaxItchPrefab;
    public List<GameObject> currentIchiIcons = new List<GameObject>();
    public List<GameObject> maxIchiIcons = new List<GameObject>();
    public Button buttonEndTurn;

    public GameObject PrefareDeck;
    public Button buttonPrefareDeck;
    public GameObject iconPrefareDeckLight;
    public Text textPrefareDeckCount;

    public GameObject TrashDeck;
    public Button buttonTrashDeck;
    public GameObject iconTrashDeckLight;
    public Text textTrashDeckCount;

    public GameObject ForgottenDeck;
    public Button buttonForgottenDeck;
    public GameObject iconForgottenDeckLight;
    public Text textForgottenDeckCount;

    public TextMeshProUGUI textCurrentActionCost;
    public TextMeshProUGUI textMaxActionCost;
    public TextMeshProUGUI textCurrentPhase;
    public TextMeshProUGUI currentIchiText;
    public TextMeshProUGUI maxIchiText;

    [Header("카드 큐 UI")]
    public InfiniteScroll infiniteScroll;
    public ScrollRect cardQueueScrollRect;
    public GameObject cardQueuePopUp;
    public TextMeshProUGUI textCardQueueName;
    public TextMeshProUGUI textCardQueueType;
    public TextMeshProUGUI textCardQueueDesc;
    public TextMeshProUGUI textCardOwnerName;
    private Coroutine cardQueueScrollCoroutine;
    public ScrollButtonDirection direction;
    public Button leftScrollButton;
    public Button rightScrollButton;
    public float scrollSpeed;
    private bool isPointerDown = false;
    public enum ScrollButtonDirection
    {
        NONE,
        LEFT,
        RIGHT
    }


    void Start()
    {
        ConfigScreenChangeMode(screenTransitionMode);
        screenTransition.material =  new Material(screenTransition.material); // 머티리얼 인스턴스 복사본을 생성하여 이미지의 머티리얼값에 할당(원본대신 복사본을 사용해 프로퍼티값 변경)
        scrollSpeed = 500f;
        buttonPrefareDeck.onClick.AddListener(() => {
            PopUpUIManager.instance.HandleShowPrefareDeckListPopUp(PlayerRegistry.Local.currentGamePlayerNetId);
        });
        buttonTrashDeck.onClick.AddListener(() => {
            PopUpUIManager.instance.HandleShowTrashDeckListPopUp(PlayerRegistry.Local.currentGamePlayerNetId);
        });
        buttonForgottenDeck.onClick.AddListener(() => {
            PopUpUIManager.instance.HandShowForgottenDeckListPopUp(PlayerRegistry.Local.currentGamePlayerNetId);
        });
    }

    void Update()
    {
        UpdateCardQueueScrollButtonVisibility();
        HandleCardQueueScrollViewByButton();   
    }

    // 버튼으로 스크롤 뷰 제어
    private void HandleCardQueueScrollViewByButton()
    {
        if(isPointerDown && cardQueueScrollRect != null){
            float contentWidth = cardQueueScrollRect.content.rect.width;
            float viewportWidth = cardQueueScrollRect.viewport.rect.width;
            float maxScrollWidth = contentWidth - viewportWidth;
            if(maxScrollWidth <= 0){
                return;
            }
            float scrollAmount = (scrollSpeed / maxScrollWidth) * Time.deltaTime;
            if(direction == ScrollButtonDirection.LEFT){
                cardQueueScrollRect.horizontalNormalizedPosition = Mathf.Clamp01(cardQueueScrollRect.horizontalNormalizedPosition - scrollAmount);
            }else{
                cardQueueScrollRect.horizontalNormalizedPosition = Mathf.Clamp01(cardQueueScrollRect.horizontalNormalizedPosition + scrollAmount);
            }
        }
    }

    // 스크롤 뷰 내부 컨텐츠요소의 길이에 따라 스크롤 버튼의 활성화 상태 변경 (상태가 바뀔 때만 SetActive 호출)
    private void UpdateCardQueueScrollButtonVisibility()
    {
        if(cardQueueScrollRect != null){
            float contentWidth = cardQueueScrollRect.content.rect.width;
            float viewportWidth = cardQueueScrollRect.viewport.rect.width;
            bool leftVisible;
            bool rightVisible;
            if(contentWidth <= viewportWidth){
                leftVisible = false;
                rightVisible = false;
            }else{
                float scrollPosition = cardQueueScrollRect.horizontalNormalizedPosition;
                leftVisible = scrollPosition > 0.01f;
                rightVisible = scrollPosition < 0.99f;
            }
            if(leftScrollButton.gameObject.activeSelf != leftVisible){
                leftScrollButton.gameObject.SetActive(leftVisible);
            }
            if(rightScrollButton.gameObject.activeSelf != rightVisible){
                rightScrollButton.gameObject.SetActive(rightVisible);
            }
        }
    }

    // 뽑을덱, 버린덱, 잊혀진덱 버튼의 크기 변경 애니매이션
    public void DeckButtonScaleAnimation(Button button)
    {
        Vector3 chagenScale = new Vector3(1.5f, 1.5f, 1.5f);
        Vector3 originScale = new Vector3(1f, 1f, 1f);
        button.GetComponent<RectTransform>().DOScale(chagenScale, 0.1f).OnComplete(() => {
            button.GetComponent<RectTransform>().DOScale(originScale, 0.1f);
        });
    }

    public void CardQueueScrollToEnd()
    {
        if(cardQueueScrollCoroutine != null){
            StopCoroutine(cardQueueScrollCoroutine);
        }
        cardQueueScrollCoroutine = StartCoroutine(MoveScrollToEnd());
    }

    private IEnumerator MoveScrollToEnd()
    {
        yield return null;
        float startPosition = cardQueueScrollRect.horizontalNormalizedPosition;
        float targetPosition = 1f;
        float elapsedTime = 0f;
        float scrollDuration = 0.5f;
        while (elapsedTime < scrollDuration)
        {
            cardQueueScrollRect.horizontalNormalizedPosition = Mathf.Lerp(startPosition, targetPosition, elapsedTime / scrollDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    public void HandleCardQueuePopUp(CardQueue cardQueue, bool isOpen)
    {
        if(isOpen){
            GamePlayerDeck gamePlayerDeck = NetLookup.Client<GamePlayerDeck>(cardQueue.cardOwnerNetId);
            string playerName = gamePlayerDeck.GetComponent<GamePlayer>().objectOwner.steamPersonaName;
            textCardOwnerName.text = playerName;
            Card card = cardQueue.card;
            textCardQueueName.text = card.baseCard.name.ToString();
            textCardQueueType.text = card.baseCard.cardType.ToString();
            textCardQueueDesc.text = CardData.instance.ReplaceDescription(card.baseCard.description);
            cardQueuePopUp.gameObject.SetActive(true);
            cardQueuePopUp.GetComponent<CanvasGroup>().DOFade(1f, 0.25f);
        }else{
            textCardOwnerName.text = string.Empty;
            textCardQueueName.text = string.Empty;
            textCardQueueType.text = string.Empty;
            textCardQueueDesc.text = string.Empty;
            cardQueuePopUp.GetComponent<CanvasGroup>().alpha = 0f;
            cardQueuePopUp.gameObject.SetActive(false);
        }
    }

    // 화면 전환 모드에 따라 사용할 오브젝트 활성화 설정
    public void ConfigScreenChangeMode(ScreenTransitionMode screenTransitionMode)
    {
        switch(screenTransitionMode){
            case ScreenTransitionMode.Fade:
                screenFade.gameObject.SetActive(true);
                screenTransition.gameObject.SetActive(false);
                break;
            case ScreenTransitionMode.Transition:
                screenFade.gameObject.SetActive(false);
                screenTransition.gameObject.SetActive(true);
                break;
        }
    }

    // 화면 전환 모드에 따라 스크린 IN 시퀀스 수행
    public void DoScreenChangeIn(System.Action callback = null)
    {
        switch(screenTransitionMode){
            case ScreenTransitionMode.Fade:
                DoScreenFadeIn(() => callback());
                break;
            case ScreenTransitionMode.Transition:
                DoScreenTransitionIn(() => callback());
                break;
        }
    }

    // 화면 전환 모드에 따라 스크린 OUT 시퀀스 수행
    public void DoScreenChangeOut()
    {
         switch(screenTransitionMode){
            case ScreenTransitionMode.Fade:
                DoScreenFadeOut();
                break;
            case ScreenTransitionMode.Transition:
                DoScreenTransitionOut();
                break;
        }
    }

    // 스크린 Fade In 시퀀스 
    private void DoScreenFadeIn(System.Action callback = null)
    {
        screenFade.DOFade(1f, 1.0f).OnComplete(() => {
            if(callback != null){
                callback();
            }
        });
    }

    // 스크린 Fade Out 시퀀스
    private void DoScreenFadeOut()
    {
        screenFade.DOFade(0f, 1.0f);
    }

    // 스크린 Block Transition In 시퀀스
    private void DoScreenTransitionIn(System.Action callback = null)
    {
        StartCoroutine(TransitionInCoroutine(() => {
            if(callback != null){
                callback();
            }
        }));
    }

    // 스크린 Block Transition Out 시퀀스
    private void DoScreenTransitionOut()
    {
        StartCoroutine(TransitionOutCoroutine());
    }

    private IEnumerator TransitionInCoroutine(System.Action callback = null)
    {
        screenTransition.enabled = true;
        float duration = 1.0f; // TransitionIn의 지속 시간
        float elapsedTime = 0f;

        float initialScroll = 2.5f; // 진행상태 프로퍼티값의 초기값
        float finalScroll = 0f;     // 진행상태 프로퍼티값의 최종값      

        while (elapsedTime < duration){
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Transition_In 구간 : 0에서 1사이의 t값이 0 ~ 1 구간에서는, 프로퍼티값의 초기값 -> 0 변경
            float currentScroll = Mathf.Lerp(initialScroll, finalScroll, t);

            screenTransition.material.SetFloat("_Progress", currentScroll);

            yield return null;
        }
        screenTransition.material.SetFloat("_Progress", finalScroll);
        if(callback != null){
            callback();
        }
    }

    private IEnumerator TransitionOutCoroutine()
    {
        screenTransition.enabled = true;
        float duration = 1.0f; // TransitionOut의 지속 시간
        float elapsedTime = 0f;

        float initialScroll = 0f;     // TransitionIn에서 최종적으로 설정된 값
        float finalScroll = 2.5f; // TransitionOut에서 되돌아갈 초기값      

        while (elapsedTime < duration){
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Transition_Out 구간 : 0에서 1사이의 t값이 0 ~ 1 구간에서는, 0 -> 프로퍼티값의 초기값 변경
            float currentScroll = Mathf.Lerp(initialScroll, finalScroll, t);

            screenTransition.material.SetFloat("_Progress", currentScroll);

            yield return null;
        }
        screenTransition.material.SetFloat("_Progress", finalScroll);
        screenTransition.enabled = false;
    }

    // ------------------------------------ 스크롤 버튼 이벤트 트리거 컴포넌트에 할당된 함수 -------------------------------------//
    public void OnPointerEnterScrollView()
    {
        isPointerDown = false;
        direction = ScrollButtonDirection.NONE;
    }
    
    public void OnPointerDownLeftScrollButton()
    {
        isPointerDown = true;
        direction = ScrollButtonDirection.LEFT;
    }

    public void OnPointerUpLeftScrollButton()
    {
        isPointerDown = false;
        direction = ScrollButtonDirection.NONE;
    }

    public void OnPointerDownRightScrollButton()
    {
        isPointerDown = true;
        direction = ScrollButtonDirection.RIGHT;
    }

    public void OnPointerUpRightScrollButton()
    {
        isPointerDown = false;
        direction = ScrollButtonDirection.NONE;
    }

    public void OnPointerEnterButtonPrefareDeck()
    {
        iconPrefareDeckLight.SetActive(true);
    }

    public void OnPointerExitButtonPrefareDeck()
    {
        iconPrefareDeckLight.SetActive(false);
    }

    public void OnPointerEnterButtonTrashDeck()
    {
        iconTrashDeckLight.SetActive(true);
    }

    public void OnPointerExitButtonTrashDeck()
    {
        iconTrashDeckLight.SetActive(false);
    }
     
    public void OnPointerEnterButtonForgottenDeck()
    {
        iconForgottenDeckLight.SetActive(true);
    }

    public void OnPointerExitButtonForgottenDeck()
    {
        iconForgottenDeckLight.SetActive(false);
    }
}