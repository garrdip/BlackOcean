using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using DG.Tweening;


public class TrashDeckListPopUp : SingletonD<TrashDeckListPopUp>, IPointerClickHandler
{
    [Header("해당 플레이어 netId")]
    public uint netId;

    [Header("댁 리스트")]
    public List<GameObject> deckList;

    [Header("UI 컴포넌트")]
    public CanvasGroup canvasGroup;
    public GameObject scrollViewLayout;
    public GridLayoutGroup gridLayoutGroup;
    public Button buttonExit;

    protected override void Awake()
    {
        PopUpUIManager.instance.onChangeTrashDeckPopUpShow += OnChangeTrashDeckPopUpShow;
        PopUpUIManager.instance.onChangeTrashDeckPopUpHide += OnChangeTrashDeckPopUpHide;
    }

    void Start()
    {
        buttonExit.onClick.AddListener(() => {
            PopUpUIManager.instance.HandleHideTrashDeckListPopUp();
        });
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(!PopUpUIManager.instance.isMouseOnCardOnDeck){
            PopUpUIManager.instance.HandleHideTrashDeckListPopUp();
        }
    }

    public void CreateDeck(Card card)
    {
        GameObject cardOnDeck = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab);
        cardOnDeck.transform.SetParent(gridLayoutGroup.transform);
        cardOnDeck.transform.localPosition = Vector3.zero;
        cardOnDeck.transform.localScale = Vector3.one;
        cardOnDeck.GetComponent<CardOnDeck>().card = card.CardDeepCopy(false);
        deckList.Add(cardOnDeck);
    }
    
    public void RemoveDeck(Card card)
    {
        for(int i=deckList.Count-1;i>=0;i--){
            CardOnDeck cardOnDeck = deckList[i].GetComponent<CardOnDeck>();
            if(cardOnDeck.card.guid.Equals(card.guid)){
                Destroy(deckList[i]);
                deckList.RemoveAt(i);
            }
        }
    }

    public void ClearDeckList()
    {
        for(int i=deckList.Count-1; i >=0; i--){
            Destroy(deckList[i]);
            deckList.RemoveAt(i);
        }
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    public void OnTrashDeckUpdated(SyncList<Card>.Operation op, int index, Card oldTrashDeck, Card newTrashDeck)
    {
        switch (op)
        {
            case SyncList<Card>.Operation.OP_ADD:
                CreateDeck(newTrashDeck);
                break;
            case SyncList<Card>.Operation.OP_INSERT:
                
                break;
            case SyncList<Card>.Operation.OP_REMOVEAT:
                RemoveDeck(oldTrashDeck);
                break;
            case SyncList<Card>.Operation.OP_SET:
                
                break;
            case SyncList<Card>.Operation.OP_CLEAR:
                ClearDeckList();
                break;
        }
    }

    public void OnChangeTrashDeckPopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);   
    }

    public void OnChangeTrashDeckPopUpHide()
    {
        if(PopUpUIManager.instance.cardOnHandRemovePopUp.activeSelf){
            M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHandOverPopUp");
        }else{
            M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHand");
        }
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
        });
    }

}
