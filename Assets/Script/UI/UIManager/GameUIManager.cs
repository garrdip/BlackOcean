using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;


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
    public GameObject ForgottenDeck;
    public GameObject TestUI;
    public GameObject CostIconLayout; // 코스트 아이콘 리스트 레이아웃
    public GameObject CostIocnPrefab; // 코스트 아이콘 프리팹
    public List<GameObject> costIconList = new List<GameObject>(); // 코스트 아이콘 리스트

    [Header("UI 컴포넌트")]

    // 카드 전투 UI
    public Button buttonEndTurn;
    public Button buttonPrefareDeck;
    public Button buttonTrashDeck;
    public Text textPrefareDeckCount;
    public Text textTrashDeckCount;
    public Text textForgottenDeckCount;
    public TextMeshProUGUI textCurrentActionCost;
    public TextMeshProUGUI textMaxActionCost;
    public TextMeshProUGUI textCurrentPhase;
    public TextMeshProUGUI currentIchiText;
    public TextMeshProUGUI maxIchiText;


    [Header("화면 Dim 처리용 이미지")]
    public Image blackCurtain;


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