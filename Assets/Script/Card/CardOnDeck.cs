using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;
using DG.Tweening;
using TMPro;

public class CardOnDeck : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Card card;
    public TextMeshProUGUI textCardName;
    public TextMeshProUGUI textCardInfo;

    private Vector3 originScale;

    void Start()
    {
        textCardName.text = card.baseCard.name;
        textCardInfo.text = card.baseCard.cardType.ToString();
        originScale = transform.localScale;
    }

    void OnDisable()
    {
        DOTween.Kill(transform); // 비활성화 될 때 DoTween 프로세스 킬
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        transform.DOScale(originScale * 1.2f, 0.3f);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        transform.DOScale(originScale, 0.3f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(PopUpUI.instance.BattleResultPopUp.activeSelf){
            if(NetworkClient.connection != null && NetworkClient.active){
                GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
                if(gamePlayerDeck.isLocalPlayer){
                    gamePlayerDeck.CmdAddPrefareDeck(this.card);
                }
            }
        }
    }
}
