using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Gpm.Ui;

public class CardQueueItem : InfiniteScrollItem
{
    public CardQueue cardQueue;
    public CanvasGroup canvasGroup;

    [Header("Small 카드 큐")]
    public GameObject smallCardQueue;
    public GameObject smallCardQueueEm;
    public List<GameObject> smallCardQueueLights;
    public Image smallCardQueueIllust;
    
    [Header("Big 카드 큐")]
    public GameObject bigCardQueue;
    public GameObject bigCardQueueEm;
    public List<GameObject> bigCardQueueLights;
    public Image bigCardQueueIllust;


    void Awake()
    {
        M_TurnManager.instance.onCurrentCardQueueUpdated += OnCurrentCardQueueUpdated;
    }

    void Start()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.5f);
    }

    private void OnDestroy()
    {
        transform.DOKill();
        canvasGroup.DOKill();
        bigCardQueue.transform.DOKill();
    }

    // 해당 InfiniteScrollItem이 재사용 되어 InfiniteScrollData가 변경될 경우 호출(초기 생성 및 스크롤 이동 시)
    public override void UpdateData(InfiniteScrollData scrollData)
    {
        base.UpdateData(scrollData);

        cardQueue = (CardQueue)scrollData;
        InitCardQueueItemIllust(cardQueue.card.baseCard);
        bigCardQueue.gameObject.SetActive(cardQueue.isCurrent);
        smallCardQueue.gameObject.SetActive(!cardQueue.isCurrent);
    }

    // 현재 카드 큐 인덱스 변경 이벤트 수신
    public void OnCurrentCardQueueUpdated(int currentCardQueueIndex)
    {
        bigCardQueue.gameObject.SetActive(currentCardQueueIndex == GetItemIndex());
        smallCardQueue.gameObject.SetActive(currentCardQueueIndex != GetItemIndex());
        bigCardQueue.transform.DOScale(1.5f, 0.25f).OnComplete(() => {
            bigCardQueue.transform.DOScale(1f, 0.25f);
        });
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
            GameUIManager.instance.cardQueuePopUp.transform.position = smallCardQueueEm.transform.position + new Vector3(0f, -1.5f, 0f);
            foreach(GameObject lightObject in smallCardQueueLights){
                lightObject.SetActive(true);
            }
        }else{
            GameUIManager.instance.cardQueuePopUp.transform.position = bigCardQueueEm.transform.position + new Vector3(0f, -1.5f, 0f);
            foreach(GameObject lightObject in bigCardQueueLights){
                lightObject.SetActive(true);
            }
        }
    }

    public void OnPointerExit()
    {
        GameUIManager.instance.HandleCardQueuePopUp(cardQueue, false);
        if(smallCardQueue.activeSelf){
            foreach(GameObject lightObject in smallCardQueueLights){
                lightObject.SetActive(false);
            }
        }else{
            foreach(GameObject lightObject in bigCardQueueLights){
                lightObject.SetActive(false);
            }
        }
    }
}
