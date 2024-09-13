using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonBase : MonoBehaviour
{
    public virtual void Awake()
    {
        AddEventTriggers();
    }

    public virtual void OnDestroy()
    {
        transform.DOKill();
    }

    private void AddEventTriggers()
    {
        EventTrigger eventTrigger = gameObject.AddComponent<EventTrigger>();
            
        EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry();
        pointerEnterEntry.eventID = EventTriggerType.PointerDown;
        pointerEnterEntry.callback.AddListener((data) => { OnPointerDownButtonBase((PointerEventData)data); });
        eventTrigger.triggers.Add(pointerEnterEntry);

        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry();
        pointerExitEntry.eventID = EventTriggerType.PointerUp;
        pointerExitEntry.callback.AddListener((data) => { OnPointerUpButtonBase((PointerEventData)data); });
        eventTrigger.triggers.Add(pointerExitEntry); 
    }

    private void OnPointerDownButtonBase(PointerEventData pointerEventData)
    {
        transform.DOScale(0.9f, 0.25f);
    }

    private void OnPointerUpButtonBase(PointerEventData pointerEventData)
    {
        transform.DOScale(1f, 0.25f);
    }
}
