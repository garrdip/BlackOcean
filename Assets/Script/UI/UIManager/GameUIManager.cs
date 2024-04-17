using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using ProjectD;


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
        Camera.main.GetComponent<Shake>().Shaking();
        GameObject floatingDamageText = Instantiate(FloatingDamageText, Vector3.zero, Quaternion.identity);
        floatingDamageText.transform.SetParent(EffectCanvas.transform);
        floatingDamageText.transform.position = targetObject.transform.position + new Vector3(0f, 6f, 0f);
        floatingDamageText.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
        floatingDamageText.GetComponent<TextMeshProUGUI>().text = damage.ToString();

        bool reversePath = Random.Range(0, 2) == 0; // 좌측커브 or 우측커브 랜덤 결정
        Vector3 endPoint = reversePath ? floatingDamageText.transform.position + new Vector3(-3f, -12f, 0f) : floatingDamageText.transform.position + new Vector3(3f, -12f, 0f);
        Tween curveTween = floatingDamageText.transform.DOJump(endPoint, 9f, 1, 0.5f);
        Tween fadeTween = floatingDamageText.GetComponent<CanvasGroup>().DOFade(0f, 1f);
        Tween scaleTween = floatingDamageText.transform.DOPunchScale(new Vector3(3f, 3f, 3f), 0.5f, 2, 1f).SetEase(Ease.OutCubic);
        Tween scaleReturnTween = floatingDamageText.transform.DOScale(1f, 0.5f);
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

    // 방어도 표시 트위닝
    public void DisplayDefence(TargetObject targetObject, bool isGain, int value)
    {
        GameObject floatingDamageText = Instantiate(FloatingDamageText, Vector3.zero, Quaternion.identity);
        floatingDamageText.transform.SetParent(EffectCanvas.transform);
        floatingDamageText.transform.localScale = Vector3.one;
        floatingDamageText.transform.position = isGain ? targetObject.transform.position + new Vector3(0f, 8f, 0f) : targetObject.transform.position + new Vector3(0f, 5f, 0f);
        floatingDamageText.GetComponent<TextMeshProUGUI>().color = ColorUtils.HexToColor("#0082FA");
        floatingDamageText.GetComponent<TextMeshProUGUI>().text = isGain ? "+" + value.ToString() : value.ToString();
        
        Tween moveTween = isGain ? floatingDamageText.transform.DOMoveY(3f, 1.5f).SetEase(Ease.OutSine) : floatingDamageText.transform.DOMoveY(5f, 1.5f).SetEase(Ease.OutSine);
        Sequence sequence = DOTween.Sequence();
        sequence.Append(moveTween);
        sequence.Join(floatingDamageText.GetComponent<CanvasGroup>().DOFade(1f, 0.5f));
        sequence.Append(floatingDamageText.GetComponent<CanvasGroup>().DOFade(0f, 0.5f))
            .OnComplete(() => {
                floatingDamageText.transform.DOKill();
                Destroy(floatingDamageText);
            });
    }
}