using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;


public class ToastMessageManager : SingletonD<ToastMessageManager>
{
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI toastMessageText;

    public void SetToastMessageTest(string text)
    {
        toastMessageText.text = text;
    }

    public void ShowToastMessage()
    {
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.DOFade(1.0f, 1.0f).OnComplete(() => {
            HideToastMessage();
        });
    }

    public void HideToastMessage()
    {
        canvasGroup.DOFade(0.0f, 1.0f).OnComplete(() => {
            canvasGroup.gameObject.SetActive(false);
            canvasGroup.transform.DOKill();
        });
    }
}
