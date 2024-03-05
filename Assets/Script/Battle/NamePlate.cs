using UnityEngine;
using TMPro;
using DG.Tweening;

public class NamePlate : MonoBehaviour
{
    public GameObject hpBarFiller;
    public TextMeshProUGUI hpText;
    public GameObject shield;
    public TextMeshProUGUI shieldValue;
    public GameObject shieldBase;
    public GameObject shieldIcon;
    public Canvas nameCanvas;
    public Canvas hpCanvas;
    public Canvas shieldCanvas;

    private Vector3 shieldOriginPosition;
    private Vector3 shieldGainPosition;
    private Sequence sequence;

    void Start()
    {
        // 실드 오브젝트의 초기 위치 및 알파값 설정
        shieldOriginPosition = shield.transform.localPosition;
        shieldCanvas.GetComponent<CanvasGroup>().alpha = 0.5f;
        shieldIcon.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
        shieldBase.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
    }

    void OnDestroy()
    {
        // 오브젝트 파괴 시 트위닝 킬
        sequence.Kill();
        shieldCanvas.GetComponent<CanvasGroup>().DOKill();
        shieldIcon.GetComponent<SpriteRenderer>().DOKill();
        shieldBase.GetComponent<SpriteRenderer>().DOKill();
        shield.transform.DOKill();
    }

    public void SetHPValue(int value,int max,int order)
    {
        hpBarFiller.transform.localPosition = new Vector3((3.2f * value / max) - 3.2f, 0, 0);
        hpText.text = value <= 0 ? ("0 / " + max) : (value + " / " + max);
    }

    public void SetShieldValue(int value,bool isGain, bool isEnemy)
    {
        if(value == 0){
            sequence = DOTween.Sequence()
                .Join(shieldCanvas.GetComponent<CanvasGroup>().DOFade(0f, 0.5f))
                .Join(shieldIcon.GetComponent<SpriteRenderer>().DOFade(0f, 0.5f))
                .Join(shieldBase.GetComponent<SpriteRenderer>().DOFade(0f, 0.5f))
                .OnComplete(() => {
                    shield.SetActive(false);
                });
        }else{
            if(isGain){
                shield.transform.localPosition = isEnemy ? shieldOriginPosition + new Vector3(-1f, 0f, 0f) : shieldOriginPosition + new Vector3(1f, 0f, 0f);
                shield.SetActive(true);
                sequence = DOTween.Sequence()
                    .Join(shieldCanvas.GetComponent<CanvasGroup>().DOFade(1f, 0.5f))
                    .Join(shieldIcon.GetComponent<SpriteRenderer>().DOFade(1f, 0.5f))
                    .Join(shieldBase.GetComponent<SpriteRenderer>().DOFade(1f, 0.5f))
                    .Join(shield.transform.DOLocalMove(Vector3.zero, 0.5f));
            }
            shieldValue.text = value.ToString();
        }
    }
}
