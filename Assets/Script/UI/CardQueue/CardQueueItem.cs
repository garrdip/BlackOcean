using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardQueueItem : MonoBehaviour
{
    public CardQueue cardQueue;
    public GameObject smallCardQueue;
    public GameObject smallCardQueueEm;
    public GameObject bigCardQueue;
    public GameObject bigCardQueueEm;
    public GameObject cardQueuePopUpPosition;

    void Start()
    {
        transform.DOScale(1.5f, 0.25f).OnComplete(() => {
            transform.DOScale(1f, 0.25f);
        });
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

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
