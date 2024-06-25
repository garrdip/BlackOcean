using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class ToastMessage : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public Image toastMessageContainer;
    public TextMeshProUGUI toastMessageText;
    public float fadeInTime;
    public float fadeOutTime;

    void Start()
    {
        fadeInTime = 1.0f;
        fadeOutTime = 1.0f;
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    // 토스트 메시지의 외곽 박스 색상 설정
    public ToastMessage MessageBoxColor(Color color)
    {
        toastMessageContainer.color = color;
        return this;
    }

    // 토스트 메시지 FadeIn Time 설정
    public ToastMessage FadeInTime(float time)
    {
        fadeInTime = time;
        return this;
    }

    // 토스트 메시지 FadeOut Time 설정
    public ToastMessage FadeOutTime(float time)
    {
        fadeOutTime = time;
        return this;
    }

    // 토스트 메시지 위치 설정
    public ToastMessage Position(ToastPosition position)
    {
        RectTransform canvasRectTransform = M_MessageManager.instance.toastMessageLayout.GetComponent<RectTransform>();
        switch (position)
        {
            case ToastPosition.Top:
                canvasRectTransform.anchorMin = new Vector2(0.5f, 1);
                canvasRectTransform.anchorMax = new Vector2(0.5f, 1);
                canvasRectTransform.pivot = new Vector2(0.5f, 1);
                break;
            case ToastPosition.Bottom:
                canvasRectTransform.anchorMin = new Vector2(0.5f, 0);
                canvasRectTransform.anchorMax = new Vector2(0.5f, 0);
                canvasRectTransform.pivot = new Vector2(0.5f, 0);
                break;
        }
        return this;
    }

    // 토스트 메시지 텍스트 설정
    public ToastMessage Text(string text)
    {
        toastMessageText.text = text;
        return this;
    }

    // 토스트 메시지 텍스트 색상설정
    public ToastMessage TextColor(Color color)
    {
        toastMessageText.color = color;
        return this;
    }

    // 토스트 메시지 출력 후 사라짐
    public void Show()
    {
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.DOFade(1.0f, fadeInTime).OnComplete(() => {
            canvasGroup.DOFade(0.0f, fadeOutTime).OnComplete(() => {
                canvasGroup.gameObject.SetActive(false);
                canvasGroup.transform.DOKill();
                toastMessageText.text = string.Empty;
                Destroy(gameObject);
            });
        });
    }
}
