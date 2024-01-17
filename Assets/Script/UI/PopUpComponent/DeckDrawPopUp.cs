using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;


public class DeckDrawPopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public List<GameObject> addtionDrawCards = new List<GameObject>();
    public List<GameObject> addtionDrawCardPositions = new List<GameObject>();


    void Awake()
    {
        PopUpUIManager.instance.onChangeDeckDrawPopUpShow += OnChangeDeckDrawPopUpShow;
        PopUpUIManager.instance.onChangeDeckDrawPopUpHide += OnChangeDeckDrawPopUpHide;
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // DeckDrawPopUp 활성화 콜백
    public void OnChangeDeckDrawPopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
    }

    // DeckDrawPopUp 비활성화 콜백
    public void OnChangeDeckDrawPopUpHide()
    {
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            foreach(GameObject card in addtionDrawCards){
                Destroy(card);
            }
            addtionDrawCards.Clear();
            gameObject.SetActive(false);
        });
    } 
}
