using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using DG.Tweening;


public enum ToastPosition
{
    Top,
    Bottom
}

public class M_MessageManager : NetworkSingletonD<M_MessageManager>
{
    public CanvasGroup canvasGroup;
    public Image toastMessageContainer;
    public TextMeshProUGUI toastMessageText;
    public float fadeInTime = 1.0f;
    public float fadeOutTime = 1.0f;


    // 토스트 메시지의 외곽 박스 색상 설정
    public M_MessageManager MessageBoxColor(Color color)
    {
        toastMessageContainer.color = color;
        return this;
    }

    // 토스트 메시지 FadeIn Time 설정
    public M_MessageManager FadeInTime(float time)
    {
        fadeInTime = time;
        return this;
    }

    // 토스트 메시지 FadeOut Time 설정
    public M_MessageManager FadeOutTime(float time)
    {
        fadeOutTime = time;
        return this;
    }

    // 토스트 메시지 위치 설정
    public M_MessageManager Position(ToastPosition position)
    {
        RectTransform canvasRectTransform = canvasGroup.GetComponent<RectTransform>();
        switch (position)
        {
            case ToastPosition.Top:
                canvasRectTransform.anchorMin = new Vector2(0.5f, 1);
                canvasRectTransform.anchorMax = new Vector2(0.5f, 1);
                canvasRectTransform.pivot = new Vector2(0.5f, 1);
                canvasRectTransform.anchoredPosition = new Vector2(canvasRectTransform.anchoredPosition.x, -250f);
                break;
            case ToastPosition.Bottom:
                canvasRectTransform.anchorMin = new Vector2(0.5f, 0);
                canvasRectTransform.anchorMax = new Vector2(0.5f, 0);
                canvasRectTransform.pivot = new Vector2(0.5f, 0);
                canvasRectTransform.anchoredPosition = new Vector2(canvasRectTransform.anchoredPosition.x, 250f);
                break;
        }
        return this;
    }

    // 토스트 메시지 텍스트 설정
    public M_MessageManager Text(string text)
    {
        toastMessageText.text = text;
        return this;
    }

    // 토스트 메시지 텍스트 색상설정
    public M_MessageManager TextColor(Color color)
    {
        toastMessageText.color = color;
        return this;
    }

    // 토스트 메시지 출력 후 사라짐
    public M_MessageManager Show()
    {
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.DOFade(1.0f, fadeInTime).OnComplete(() => {
            canvasGroup.DOFade(0.0f, fadeOutTime).OnComplete(() => {
            canvasGroup.gameObject.SetActive(false);
            canvasGroup.transform.DOKill();
            toastMessageText.text = string.Empty;
            });
        });
        return this;
    }

    // ------------------------------------------------------------ ClientRpc Method -------------------------------------------------------------- //

    // 룸씬에서 다른 클라 연결해제 이벤트 수신
    [ClientRpc]
    public void RpcOtherPlayerDisconnectedInRoomScene(string oldOwner, string newOwner)
    {
        M_MessageManager.instance
            .Position(ToastPosition.Bottom)
            .MessageBoxColor(Color.red)
            .TextColor(Color.white)
            .Text($"{oldOwner} 님이 대기방을 나갔습니다.\n{newOwner} 님에게 권한이 이전됩니다.")
            .Show();
    }

    // 게임씬에서 다른 클라 연결해제 이벤트 수신
    [ClientRpc]
    public void RpcOtherPlayerDisconnectedInGameScene(string oldPlayer ,string newPlayer)
    {
        M_MessageManager.instance
            .Position(ToastPosition.Bottom)
            .MessageBoxColor(Color.white)
            .TextColor(Color.red)
            .Text($"{oldPlayer} 님이 게임을 나갔습니다.\n{newPlayer} 님에게 권한이 이전됩니다.")
            .Show();
    }
}
