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

    [Header("화면 Dim 처리용 이미지")]
    public Image blackCurtain;


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
}