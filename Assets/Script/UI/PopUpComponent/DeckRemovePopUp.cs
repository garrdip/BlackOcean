using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DeckRemovePopUp : SingletonD<DeckRemovePopUp>
{
    public CanvasGroup canvasGroup;


    void Start()
    {
        
    }

    void OnEnable()
    {
        canvasGroup.DOFade(1.0f, 1.0f);
    }

    void OnDisable()
    {
        canvasGroup.DOFade(0.0f, 1.0f);
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }
}
