using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

public class CardOnDeck : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Card card;
    public TextMeshProUGUI textCardName;
    public TextMeshProUGUI textCardInfo;

    private Vector3 originScale;

    void Start()
    {
        textCardName.text = card.baseCard.name;
        //textCardInfo.text = card.effect.ToString();
        originScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        transform.DOScale(originScale * 1.2f, 0.3f);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        transform.DOScale(originScale, 0.3f);
    }
}
