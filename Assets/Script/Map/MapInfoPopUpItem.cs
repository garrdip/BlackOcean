using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class MapInfoPopUpItem : MonoBehaviour
{
    public uint netId;
    public TextMeshProUGUI textRoomType;
    public CanvasGroup canvasGroup;
  
    void Start()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.5f);
    }

    private void OnDestroy()
    {
        canvasGroup.DOKill();
    }
}
