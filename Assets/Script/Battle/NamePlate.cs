using UnityEngine;
using TMPro;
using DG.Tweening;

public class NamePlate : MonoBehaviour
{
    public GameObject hpBarFiller;
    public GameObject hpBarFillerTrace;
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
        hpBarFillerTrace.transform.DOKill();
        shield.transform.DOKill();
    }

    public void SetHPValue(int value, int max)
    {
        float hpValue = (3.3f * value / max) - 3.3f;
        hpBarFiller.transform.localPosition = new Vector3(hpValue, 0, 0);
        hpText.text = value <= 0 ? ("0 / " + max) : (value + " / " + max);
        DOVirtual.DelayedCall(1f, () => {
            if(hpBarFillerTrace != null){
                hpBarFillerTrace.transform.DOLocalMove(new Vector3(hpValue, 0, 0), 0.5f); // 1초 딜레이 후 임시 체력바 게이지를 현재 체력바 게이지 위치로 이동
            }
        });
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
