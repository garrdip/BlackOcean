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
        GetComponent<RectTransform>().DOKill();
    }

    private void AddEventTriggers()
    {
        EventTrigger eventTrigger = gameObject.AddComponent<EventTrigger>();
            
        EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry();
        pointerEnterEntry.eventID = EventTriggerType.PointerDown;
        pointerEnterEntry.callback.AddListener((data) => { OnPointerDownExpandableButton((PointerEventData)data); });
        eventTrigger.triggers.Add(pointerEnterEntry);

        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry();
        pointerExitEntry.eventID = EventTriggerType.PointerUp;
        pointerExitEntry.callback.AddListener((data) => { OnPointerUpExpandableButton((PointerEventData)data); });
        eventTrigger.triggers.Add(pointerExitEntry); 
    }

    private void OnPointerDownExpandableButton(PointerEventData pointerEventData)
    {
        GetComponent<RectTransform>().DOScale(0.8f, 0.25f);
    }

    private void OnPointerUpExpandableButton(PointerEventData pointerEventData)
    {
        GetComponent<RectTransform>().DOScale(1f, 0.25f);
    }
}
