using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using DG.Tweening;
using TMPro;

public class PlayerOrder : NetworkBehaviour
{
    [Header("Layout")]
    public GameObject BaseLayout;
    public GameObject TopLayout;
    public GameObject LastCardLayout;

    [Header("BaseLayout Components")]
    public GameObject uLight;
    public GameObject uBase;
    public GameObject uBaseC;
    public GameObject uLine;
    public GameObject uLineLight;
    public GameObject uMyLine;
    public GameObject uMyLineLight;

    [Header("TopLayout Components")]
    public GameObject topBase;
    public GameObject topBaseLight;
    public GameObject topMy;
    public GameObject topMyLight;
    public GameObject topSee;
    public GameObject topSeeLight;
    public GameObject topReady;
    public GameObject topReadyLight;

    [Header("LastCardLayout Components")]
    public Button cardPeekButton;
    public GameObject lastCardBaseLine;
    public GameObject lastCardBaseLingLight;
    public bool isCardPeekLocked = false;

    public TextMeshProUGUI textGold;

    [SyncVar]
    public uint gamePlayerNetId;


    void Awake()
    {
        EventTrigger baseEventTrigger = BaseLayout.AddComponent<EventTrigger>();   
        
        EventTrigger.Entry baseEnterEntry = new EventTrigger.Entry();
        baseEnterEntry.eventID = EventTriggerType.PointerEnter;
        baseEnterEntry.callback.AddListener((data) => { OnPointerEnterBase((PointerEventData)data); });
        baseEventTrigger.triggers.Add(baseEnterEntry);

        EventTrigger.Entry baseExitEntry = new EventTrigger.Entry();
        baseExitEntry.eventID = EventTriggerType.PointerExit;
        baseExitEntry.callback.AddListener((data) => { OnPointerExitBase((PointerEventData)data); });
        baseEventTrigger.triggers.Add(baseExitEntry); 

        EventTrigger cardPeekEventTrigger = cardPeekButton.gameObject.AddComponent<EventTrigger>();
        
        EventTrigger.Entry cardPeekEnterEntry = new EventTrigger.Entry();
        cardPeekEnterEntry.eventID = EventTriggerType.PointerEnter;
        cardPeekEnterEntry.callback.AddListener((data) => { OnPointerEnterCardPeekIcon((PointerEventData)data); });
        cardPeekEventTrigger.triggers.Add(cardPeekEnterEntry);

        EventTrigger.Entry cardPeekExitEntry = new EventTrigger.Entry();
        cardPeekExitEntry.eventID = EventTriggerType.PointerExit;
        cardPeekExitEntry.callback.AddListener((data) => { OnPointerExitCardPeekIcon((PointerEventData)data); });
        cardPeekEventTrigger.triggers.Add(cardPeekExitEntry); 

        cardPeekButton.onClick.AddListener(() => { OnPointerClickCardPeekButton(); });
    }

    public void OnPointerEnterBase(PointerEventData eventData)
    {
        uMyLineLight.SetActive(isOwned && uMyLine.activeSelf);
        topMyLight.SetActive(isOwned && topMy.activeSelf);
        uLineLight.SetActive(true);
        topBaseLight.SetActive(true);
    }

    public void OnPointerExitBase(PointerEventData eventData)
    {
        uMyLineLight.SetActive(false);
        topMyLight.SetActive(false);
        uLineLight.SetActive(false);
        topBaseLight.SetActive(false);
    }

    public void OnPointerEnterCardPeekIcon(PointerEventData eventData)
    {
        lastCardBaseLingLight.SetActive(true);
        if(!isCardPeekLocked){
            uint originNetId = PlayerRegistry.Local.currentGamePlayerNetId;
            topSeeLight.SetActive(true);
            SwapCardPocket(originNetId, gamePlayerNetId);
        }
    }

    public void OnPointerExitCardPeekIcon(PointerEventData eventData)
    {
        lastCardBaseLingLight.SetActive(false);
        if(!isCardPeekLocked){
            uint originNetId = PlayerRegistry.Local.currentGamePlayerNetId;
            topSeeLight.SetActive(false);
            SwapCardPocket(gamePlayerNetId, originNetId);
        }
    }

    public void OnPointerClickCardPeekButton()
    {
        uint originNetId = PlayerRegistry.Local.currentGamePlayerNetId;
        isCardPeekLocked = !isCardPeekLocked;
        if(isCardPeekLocked){
            topSeeLight.GetComponent<SpriteRenderer>().color = Color.red;
            SwapCardPocket(originNetId, gamePlayerNetId);
        }else{
            topSeeLight.GetComponent<SpriteRenderer>().color = Color.white;
            SwapCardPocket(gamePlayerNetId, originNetId);
        }
    }

    // 카드포켓 위치 스왑
    private void SwapCardPocket(uint originNetId, uint targetNetId)
    {
        CardPocket originCardPocket = NetLookup.Client<GamePlayerDeck>(originNetId).cardPocket;
        CardPocket targetCardPocket = NetLookup.Client<GamePlayerDeck>(targetNetId).cardPocket ;

        // 위치 스왑
        Sequence sequence = DOTween.Sequence();
        sequence.Append(originCardPocket.transform.DOMoveY(-100f, 0.5f));
        sequence.Join(targetCardPocket.transform.DOMoveY(-8f, 0.5f));

        // 현재 선택한 플레이어의 PrefareDeck, TrashDeck, ForgottenDeck 카운트 텍스트 설정
        GamePlayerDeck currentGamePlayerDeck = NetLookup.Client<GamePlayerDeck>(targetNetId);
        GameUIManager.instance.DeckButtonScaleAnimation(GameUIManager.instance.buttonPrefareDeck);
        GameUIManager.instance.DeckButtonScaleAnimation(GameUIManager.instance.buttonTrashDeck);
        GameUIManager.instance.DeckButtonScaleAnimation(GameUIManager.instance.buttonForgottenDeck);
        GameUIManager.instance.textPrefareDeckCount.text = currentGamePlayerDeck.prefareDeck.Count.ToString();
        GameUIManager.instance.textTrashDeckCount.text = currentGamePlayerDeck.trashDeck.Count.ToString();
        GameUIManager.instance.textForgottenDeckCount.text = currentGamePlayerDeck.forgottenDeck.Count.ToString();
        GameUIManager.instance.currentIchiText.text = currentGamePlayerDeck.currentIchi.ToString();
        GameUIManager.instance.maxIchiText.text = currentGamePlayerDeck.maxIchi.ToString();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if(NetworkClient.spawned.TryGetValue(gamePlayerNetId, out NetworkIdentity networkIdentity)){
            GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
            gamePlayer.onChangePlayerOrder += OnChangePlayerOrder;
            gamePlayer.onChangeGold += OnChangeGold;
            SetParentAndPostion(gamePlayer.selectOrder);
            SetOwnedViewComponent();
            textGold.text = gamePlayer.gold.ToString();
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if(NetworkClient.spawned.TryGetValue(gamePlayerNetId, out NetworkIdentity networkIdentity)){
            GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
            gamePlayer.onChangePlayerOrder -= OnChangePlayerOrder;
        }
    }

    void OnDestroy()
    {
        transform.DOKill();
    }

    void OnMouseEnter()
    {
        uMyLineLight.SetActive(isOwned);
        topMyLight.SetActive(isOwned);
        uLineLight.SetActive(true);
        topBaseLight.SetActive(true);
        topSeeLight.SetActive(true);
        lastCardBaseLingLight.SetActive(true);
    }

    void OnMouseExit()
    {
        uMyLineLight.SetActive(false);
        topMyLight.SetActive(false);
        uLineLight.SetActive(false);
        topBaseLight.SetActive(false);
        topSeeLight.SetActive(false);
        lastCardBaseLingLight.SetActive(false);
    }

    public void OnChangePlayerOrder(int order)
    {
        SetParentAndPostion(order);
    }

    public void OnChangeGold(int gold)
    {
        textGold.text = gold.ToString();
    }

    // 참조된 게임플레이어 클래스로부터 오더값 조회하여 값에 맞춰 뷰 컴포넌트 세팅
    private void SetParentAndPostion(int order)
    {
        transform.DOMove(new Vector3(M_TurnManager.instance.targetObjectPosition[order].x, 8f, 0f), 0.5f);
        transform.localScale = new Vector3(1f, 1f, 1f);
    }

    // 본인 소유임을 구분하는 뷰 컴포넌트 세팅
    private void SetOwnedViewComponent()
    {
        uLight.SetActive(isOwned);
        uMyLine.SetActive(isOwned);
        topMy.SetActive(isOwned);
        topSee.SetActive(!isOwned);
        topSeeLight.SetActive(!isOwned);
        LastCardLayout.SetActive(!isOwned);
        cardPeekButton.gameObject.SetActive(!isOwned); 
    }
}
