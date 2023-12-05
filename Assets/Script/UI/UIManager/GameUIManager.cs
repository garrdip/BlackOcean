using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;
using TMPro;
using ProjectD;
using Steamworks;

public class GameUIManager : SingletonD<GameUIManager>
{
    [Header("게임 오브젝트")]
    public GameObject RootGameObject;
    public GameObject GameUI;
    public GameObject GameBackGround;
    public GameObject DeckListPanel;
    public GameObject CardOnHandsPanel;
    public GameObject PrefareDeck;
    public GameObject TrashDeck;
    public GameObject TestUI;

    [Header("UI 컴포넌트")]

    // 카드 전투 UI
    public Button buttonEndTurn;
    public Button buttonPrefareDeck;
    public Button buttonTrashDeck;
    public Text textPrefareDeckCount;
    public Text textTrashDeckCount;
    public TextMeshProUGUI textCurrentActionCost;
    public TextMeshProUGUI textMaxActionCost;


    [Header("화면 Dim 처리용 이미지")]
    public Image blackCurtain;

    [Header("플레이어 리스트(플레이어 정보 및 턴 정보)")]
    public List<GameObject> playerOrderList;

    [Header("플레이어 코스트 (현재이치/최대이치) 디스플레이")]
    public TextMeshProUGUI ichiText;


    // 턴 종료
    public void HandleEndTurn()
    {
        NetworkClient.localPlayer.GetComponent<PlayerInterface>().endTurnActive = !NetworkClient.localPlayer.GetComponent<PlayerInterface>().endTurnActive;
    }


    // 댁 카운트 텍스트 컴포넌트들의 크기 변경 애니매이션(댁 카운트 변경 시 크기 커졌다 작아지는 애니매이션)
    public void DeckCountTextScaleAnimation(Text textComponent, int count)
    {
        Vector3 chagenScale = new Vector3(2f, 2f, 2f);
        Vector3 originScale = new Vector3(1f, 1f, 1f);
        textComponent.text = count.ToString();
        textComponent.transform.DOScale(chagenScale, 0.1f).SetEase(Ease.OutQuad)
        .OnComplete(() =>
        {
            textComponent.transform.DOScale(originScale, 0.1f).SetEase(Ease.InQuad);
        });
    }

    // 화면 전체 Dim 효과용 이미지 컴포넌트 Fade 애니매이션
    public void FadeBlackCurtain(System.Action<Image> callback = null)
    {
        blackCurtain.gameObject.SetActive(true);
        blackCurtain.DOFade(1.0f, 0.5f).OnComplete(() => {
            if(callback != null){
                callback(blackCurtain);
            }
        }); 
    }

    public void FadeOffBlackCurtain(System.Action<Image> callback = null)
    {
        blackCurtain.DOFade(0.0f, 0.5f).OnComplete(() => {
            if(callback != null){
                callback(blackCurtain);
            }
        }); 
    }
}