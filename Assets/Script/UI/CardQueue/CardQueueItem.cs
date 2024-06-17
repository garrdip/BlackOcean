using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;


public class CardQueueItem : MonoBehaviour
{
    public CardQueue cardQueue;
    public CanvasGroup canvasGroup;

    [Header("Small 카드 큐")]
    public GameObject smallCardQueue;
    public GameObject smallCardQueueEm;
    public Image smallCardQueueIllust;
    
    [Header("Big 카드 큐")]
    public GameObject bigCardQueue;
    public GameObject bigCardQueueEm;
    public Image bigCardQueueIllust;


    void Start()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.5f);
        InitCardQueueItemIllust(cardQueue.card.baseCard);
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

    // 카드 큐 일러스트 이미지에 사용된 카드의 일러스트 세팅
    private void InitCardQueueItemIllust(CardBase cardBase)
    {
        if(!cardBase.cardNumber.Contains("HA")){
            if(cardBase.cardNumber.Contains("_E")){
                int idx = cardBase.cardNumber.IndexOf("_E");
                if(idx != -1){
                    string cardNumber = cardBase.cardNumber.Substring(0, idx);
                    smallCardQueueIllust.sprite = CardData.instance.cardIllustAtlas.GetSprite(cardNumber);
                    bigCardQueueIllust.sprite = CardData.instance.cardIllustAtlas.GetSprite(cardNumber);
                }
            }else{
                smallCardQueueIllust.sprite = CardData.instance.cardIllustAtlas.GetSprite(cardBase.cardNumber);
                bigCardQueueIllust.sprite = CardData.instance.cardIllustAtlas.GetSprite(cardBase.cardNumber);
            }
        }
    }


    // ------------------------------------------------- 이벤트 트리거 함수 --------------------------------------------------- //
    
    public void OnPointerEnter()
    {
        GameUIManager.instance.HandleCardQueuePopUp(cardQueue, true);
        if(smallCardQueue.activeSelf){
            GameUIManager.instance.cardQueuePopUp.transform.position = smallCardQueueEm.transform.position + new Vector3(0f, -150f, 0f);
        }else{
            GameUIManager.instance.cardQueuePopUp.transform.position = bigCardQueueEm.transform.position + new Vector3(0f, -150f, 0f);
        }
    }

    public void OnPointerExit()
    {
        GameUIManager.instance.HandleCardQueuePopUp(cardQueue, false);
    }
}
