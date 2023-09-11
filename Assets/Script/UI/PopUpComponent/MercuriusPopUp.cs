using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MercuriusPopUp : SingletonD<MercuriusPopUp>, IPointerClickHandler
{
    public CanvasGroup canvasGroup;
    public GridLayoutGroup gridLayoutGroup;
    public GameObject frame;
    public TextMeshProUGUI textCardEnhancePrice;
    public TextMeshProUGUI textCardRemovePrice;
    public bool isMouseOnFrame = false;


    protected override void Awake()
    {
        PopUpUIManager.instance.onMercuriusPopUpShow += OnMercuriusPopUpShow;
        PopUpUIManager.instance.onMercuriusPopUpHide += OnMercuriusPopUpHide;
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(!isMouseOnFrame){
            PopUpUIManager.instance.HandleMercuriusPopUp(false);
        }
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // MercuriusPopUp 활성화 콜백
    public void OnMercuriusPopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        GameUIManager.instance.GameUI.gameObject.SetActive(false);
    }

    // MercuriusPopUp 비활성화 콜백
    public void OnMercuriusPopUpHide()
    {
        GameUIManager.instance.GameUI.gameObject.SetActive(true);
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
        });
    }
}
