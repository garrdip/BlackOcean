using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Mirror;


public class GameUIManager : SingletonD<GameUIManager>
{
    [Header("게임 오브젝트")]
    public GameObject RootGameObject;
    public GameObject CardOnHandsPanel;
    public GameObject PrefareDeck;
    public GameObject TrashDeck;
    public GameObject ForgottenDeck;
    public GameObject TestUI;
    public HorizontalLayoutGroup CurrentItchIconLayout;
    public HorizontalLayoutGroup MaxItchIconLayout;
    public GameObject CurrentItchPrefab;
    public GameObject MaxItchPrefab;
    public List<GameObject> currentIchiIcons = new List<GameObject>();
    public List<GameObject> maxIchiIcons = new List<GameObject>();

    [Header("화면 Dim 처리용 이미지")]
    public Image blackCurtain;

    [Header("카드 전투 UI")]
    public Button buttonEndTurn;
    public Button buttonPrefareDeck;
    public Button buttonTrashDeck;
    public Text textPrefareDeckCount;
    public Text textTrashDeckCount;
    public Text textForgottenDeckCount;
    public TextMeshProUGUI textCurrentActionCost;
    public TextMeshProUGUI textMaxActionCost;
    public TextMeshProUGUI textCurrentPhase;
    public TextMeshProUGUI currentIchiText;
    public TextMeshProUGUI maxIchiText;

    [Header("카드 큐 UI")]
    public HorizontalLayoutGroup cardQueueLayout;
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
        scrollSpeed = 500f;
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

    // 스크롤 뷰 내부 컨텐츠요소의 길이에 따라 스크롤 버튼의 활성화 상태 변경
    private void UpdateCardQueueScrollButtonVisibility()
    {
        if(cardQueueScrollRect != null){
            float contentWidth = cardQueueScrollRect.content.rect.width;
            float viewportWidth = cardQueueScrollRect.viewport.rect.width;
            if(contentWidth <= viewportWidth){
                leftScrollButton.gameObject.SetActive(false);
                rightScrollButton.gameObject.SetActive(false);
            }else{
                if(cardQueueScrollRect.horizontalNormalizedPosition <= 0){
                    leftScrollButton.gameObject.SetActive(false); // 스크롤바가 왼쪽 끝에 있는 경우 왼쪽 버튼 비활성화
                }else{
                    leftScrollButton.gameObject.SetActive(true);
                }
                if(cardQueueScrollRect.horizontalNormalizedPosition >= 1){
                    rightScrollButton.gameObject.SetActive(false); // 스크롤바가 오른쪽 끝에 있는 경우 오른쪽 버튼 비활성화
                }else{
                    rightScrollButton.gameObject.SetActive(true);
                }
            }
        }
    }

    // 댁 카운트 텍스트 컴포넌트들의 크기 변경 애니매이션(댁 카운트 변경 시 크기 커졌다 작아지는 애니매이션)
    public void DeckCountTextScaleAnimation(Text textComponent, int count)
    {
        Vector3 chagenScale = new Vector3(2f, 2f, 2f);
        Vector3 originScale = new Vector3(1f, 1f, 1f);
        textComponent.text = count.ToString();
        textComponent.transform.DOScale(chagenScale, 0.1f).SetEase(Ease.OutQuad).OnComplete(() => {
            textComponent.transform.DOScale(originScale, 0.1f).SetEase(Ease.InQuad);
        });
    }

    // 화면 전체 Dim 효과용 이미지 컴포넌트 Fade 애니매이션
    public void FadeBlackCurtain(System.Action<Image> callback = null)
    {
        blackCurtain.gameObject.SetActive(true);
        blackCurtain.DOFade(1.0f, 0.5f).OnComplete(() => {
            if(callback != null){
                callback(blackCurtain);
            }
        }); 
    }

    public void FadeOffBlackCurtain(System.Action<Image> callback = null)
    {
        blackCurtain.DOFade(0.0f, 0.5f).OnComplete(() => {
            if(callback != null){
                callback(blackCurtain);
            }
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
            GamePlayerDeck gamePlayerDeck = NetworkClient.spawned[cardQueue.cardOwnerNetId].GetComponent<NetworkIdentity>().GetComponent<GamePlayerDeck>();
            string playerName = gamePlayerDeck.GetComponent<GamePlayer>().objectOwner.steamPersonaName;
            textCardOwnerName.text = playerName;
            Card card = cardQueue.card;
            textCardQueueName.text = card.baseCard.name.ToString();
            textCardQueueType.text = card.baseCard.cardType.ToString();
            textCardQueueDesc.text = card.baseCard.description.ToString();
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
}