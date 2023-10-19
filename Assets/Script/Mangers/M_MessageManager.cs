using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using DG.Tweening;


public class M_MessageManager : NetworkSingletonD<M_MessageManager>
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
            toastMessageText.text = string.Empty;
        });
    }

    // 룸씬에서 다른 클라 연결해제 이벤트 수신
    [ClientRpc]
    public void RpcOtherPlayerDisconnectedInRoomScene(string oldOwner, string newOwner)
    {
        SetToastMessageTest($"{oldOwner} 님이 대기방을 나갔습니다.\n{newOwner} 님에게 권한이 이전됩니다.");
        ShowToastMessage();
    }

    // 게임씬에서 다른 클라 연결해제 이벤트 수신
    [ClientRpc]
    public void RpcOtherPlayerDisconnectedInGameScene(string oldPlayer ,string newPlayer)
    {
        SetToastMessageTest($"{oldPlayer} 님이 게임을 나갔습니다.\n{newPlayer} 님에게 권한이 이전됩니다.");
        ShowToastMessage();
    }
}
