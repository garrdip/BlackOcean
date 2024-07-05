using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;
using DG.Tweening;

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
    public GameObject lastCardbase;
    public GameObject lastCardBaseLine;
    public GameObject lastCardBaseLingLight;
    public GameObject cardPeekIcon;

    [SyncVar]
    public uint gamePlayerNetId;


    void Awake()
    {
        EventTrigger baseEventTrigger = uBase.AddComponent<EventTrigger>();   
        
        EventTrigger.Entry baseEnterEntry = new EventTrigger.Entry();
        baseEnterEntry.eventID = EventTriggerType.PointerEnter;
        baseEnterEntry.callback.AddListener((data) => { OnPointerEnterBase((PointerEventData)data); });
        baseEventTrigger.triggers.Add(baseEnterEntry);

        EventTrigger.Entry baseExitEntry = new EventTrigger.Entry();
        baseExitEntry.eventID = EventTriggerType.PointerExit;
        baseExitEntry.callback.AddListener((data) => { OnPointerExitBase((PointerEventData)data); });
        baseEventTrigger.triggers.Add(baseExitEntry); 

        EventTrigger cardPeekEventTrigger = lastCardbase.AddComponent<EventTrigger>();
        
        EventTrigger.Entry cardPeekEnterEntry = new EventTrigger.Entry();
        cardPeekEnterEntry.eventID = EventTriggerType.PointerEnter;
        cardPeekEnterEntry.callback.AddListener((data) => { OnPointerEnterCardPeekIcon((PointerEventData)data); });
        cardPeekEventTrigger.triggers.Add(cardPeekEnterEntry);

        EventTrigger.Entry cardPeekExitEntry = new EventTrigger.Entry();
        cardPeekExitEntry.eventID = EventTriggerType.PointerExit;
        cardPeekExitEntry.callback.AddListener((data) => { OnPointerExitCardPeekIcon((PointerEventData)data); });
        cardPeekEventTrigger.triggers.Add(cardPeekExitEntry); 
    }

    public void OnPointerEnterBase(PointerEventData eventData)
    {
        uMyLineLight.SetActive(isOwned);
        topMyLight.SetActive(isOwned);
        uLineLight.SetActive(true);
        topBaseLight.SetActive(true);
        topSeeLight.SetActive(true);
        lastCardBaseLingLight.SetActive(true);
    }

    public void OnPointerExitBase(PointerEventData eventData)
    {
        uMyLineLight.SetActive(false);
        topMyLight.SetActive(false);
        uLineLight.SetActive(false);
        topBaseLight.SetActive(false);
        topSeeLight.SetActive(false);
        lastCardBaseLingLight.SetActive(false);
    }

    public void OnPointerEnterCardPeekIcon(PointerEventData eventData)
    {
        lastCardBaseLingLight.SetActive(true);
        cardPeekIcon.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);
        uint originNetId = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId;
        if(originNetId != gamePlayerNetId){
            SwapCardPocket(originNetId, gamePlayerNetId);
        }
    }

    public void OnPointerExitCardPeekIcon(PointerEventData eventData)
    {
        lastCardBaseLingLight.SetActive(false);
         cardPeekIcon.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
        uint originNetId = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId;
        if(originNetId != gamePlayerNetId){
            SwapCardPocket(gamePlayerNetId, originNetId);
        }
    }

    // 카드포켓 위치 스왑
    private void SwapCardPocket(uint originNetId, uint targetNetId)
    {
        CardPocket originCardPocket = NetworkClient.spawned[originNetId].GetComponent<GamePlayerDeck>().cardPocket;
        CardPocket targetCardPocket = NetworkClient.spawned[targetNetId].GetComponent<GamePlayerDeck>().cardPocket ;

        // 위치 스왑
        Sequence sequence = DOTween.Sequence();
        sequence.Append(originCardPocket.transform.DOMoveY(-100f, 0.5f));
        sequence.Join(targetCardPocket.transform.DOMoveY(-8f, 0.5f));

        // 현재 선택한 플레이어의 PrefareDeck, TrashDeck 카운트 텍스트 설정
        GamePlayerDeck currentGamePlayerDeck = NetworkClient.spawned[targetNetId].GetComponent<GamePlayerDeck>();
        GameUIManager.instance.DeckCountTextScaleAnimation(GameUIManager.instance.textPrefareDeckCount, currentGamePlayerDeck.prefareDeck.Count);
        GameUIManager.instance.DeckCountTextScaleAnimation(GameUIManager.instance.textTrashDeckCount, currentGamePlayerDeck.trashDeck.Count);
        GameUIManager.instance.currentIchiText.text = currentGamePlayerDeck.currentIchi.ToString();
        GameUIManager.instance.maxIchiText.text = currentGamePlayerDeck.maxIchi.ToString();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if(NetworkClient.spawned.TryGetValue(gamePlayerNetId, out NetworkIdentity networkIdentity)){
            GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
            gamePlayer.onChangePlayerOrder += OnChangePlayerOrder;
            SetParentAndPostion(gamePlayer.selectOrder);
            SetOwnedViewComponent();
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
    }
}
