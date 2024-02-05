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
    public HorizontalLayoutGroup CurrentItchIconLayout;
    public HorizontalLayoutGroup MaxItchIconLayout;
    public GameObject CurrentItchPrefab;
    public GameObject MaxItchPrefab;
    public List<GameObject> currentIchiIcons = new List<GameObject>();
    public List<GameObject> maxIchiIcons = new List<GameObject>();
    public Canvas EffectCanvas;
    public GameObject FloatingDamageText;
    public Sequence sequence;

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

    // 데미지 표시 트위닝
    public void DisPlayeDamage(TargetObject targetObject, int damage)
    {
        GameObject floatingDamageText = Instantiate(FloatingDamageText, Vector3.zero, Quaternion.identity);
        floatingDamageText.transform.SetParent(EffectCanvas.transform);
        floatingDamageText.transform.position = targetObject.transform.position + new Vector3(0f, 5f, 0f);
        floatingDamageText.transform.localScale = Vector3.one;
        floatingDamageText.GetComponent<TextMeshProUGUI>().text = damage.ToString();

        bool reversePath = Random.Range(0, 2) == 0; // 좌측커브 or 우측커브 랜덤 결정
        Vector3 firstPos = floatingDamageText.transform.position;
        Vector3 secondPos = reversePath ? firstPos + new Vector3(-1.5f, 10f, 0) : firstPos + new Vector3(1.5f, 10f, 0);
        Vector3 thirdPos = reversePath ? firstPos + new Vector3(-3f, -7f, 0) : firstPos + new Vector3(3f, -7f, 0);
        Vector3[] path = new Vector3[]
        {
            firstPos,
            firstPos + Vector3.up * 5f,
            reversePath ? secondPos + Vector3.left * 2f : secondPos + Vector3.right * 2f,
            thirdPos, 
            reversePath ? secondPos + Vector3.left * 2f : secondPos + Vector3.right * 2f,
            thirdPos + Vector3.up
        };
        Tween scaleTween = floatingDamageText.transform.DOScale(2f, 0.3f).SetEase(Ease.OutElastic);
        Tween scaleReturnTween = floatingDamageText.transform.DOScale(1f, 0.3f);
        Tween curveTween = floatingDamageText.transform.DOPath(path, 1.5f, PathType.CubicBezier).SetEase(Ease.OutBounce);
        Tween fadeTween = floatingDamageText.GetComponent<CanvasGroup>().DOFade(0.3f, 1.5f);
        Sequence sequence = DOTween.Sequence();
        sequence.Append(scaleTween);
        sequence.Join(curveTween);
        sequence.Insert(0.2f, scaleReturnTween);
        sequence.Join(fadeTween)
                .OnComplete(() => {
                    floatingDamageText.transform.DOKill();
                    Destroy(floatingDamageText);
                });
    }
}