using UnityEngine;
using TMPro;
using DG.Tweening;
using ProjectD;

public class NamePlate : MonoBehaviour
{
    public GameObject hpBarFiller;
    public GameObject hpBarFillerTrace;
    public GameObject hpBarFillerPoison;
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
    private readonly float fillRate = 3.25f; // Hp Bar 위치 계산 비율


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

    // Hp값 텍스트 초기화(초기의 체력바 오브젝트의 위치는 기본값이므로, 텍스트값만 설정)
    public void InitHpValue(int hp, int maxHp)
    {
        hpText.text = hp <= 0 ? ("0 / " + maxHp) : (hp + " / " + maxHp);
    }

    // Hp값 변경에 따라 Hp Bar의 요소들의 위치 설정 및 텍스트 설정
    public void SetHpValue(int hp, int maxHp, TargetObject tar)
    {        
        float hpValue = (fillRate * hp / maxHp) - fillRate;
        if(tar.objectType == ObjectType.ENEMY){ // 몬스터인 경우의 꽃가루 버프에 대한 도트뎀 처리
            int index = tar.buffs.FindIndex((buff) => buff.type == BuffType.FLOWERPOWDER);
            if(index != -1){ // 꽃가루 상태일때 체력바 위치 변경
                Buff buff = tar.buffs.Find(buff => buff.type == BuffType.FLOWERPOWDER);
                int poisonValue = hp - buff.value;
                float poisonHpValue = (fillRate * poisonValue / maxHp) - fillRate;
                hpBarFiller.transform.localPosition = new Vector3(poisonHpValue, 0, 0);
                hpBarFillerPoison.transform.localPosition = new Vector3(hpValue, 0, 0);
            }else{ // 일반 상태일때 체력바 위치 변경
                hpBarFiller.transform.localPosition = new Vector3(hpValue, 0, 0);
            }
        }else{
            // TODO : 플레이어인 경우의 도트뎀 처리
            hpBarFiller.transform.localPosition = new Vector3(hpValue, 0, 0);
        }    
        DOVirtual.DelayedCall(1f, () => {
            if(hpBarFillerTrace != null){
                hpBarFillerTrace.transform.DOLocalMove(new Vector3(hpValue, 0, 0), 0.5f); // 1초 딜레이 후 임시 체력바 게이지를 현재 체력바 게이지 위치로 이동
            }
        }); 
        hpText.text = hp <= 0 ? ("0 / " + maxHp) : (hp + " / " + maxHp);
    }

    // 꽃가루 중독 상태를 나타내는 Hp Bar 활성화 및 위치 설정(꽃가루 중독 데미지에 의한 Hp Bar 후속 처리는 Hp값이 감소될때 SetHpValue 함수에서 수행)
    public void SetHpBarByFlowerPowderState(int buffValue, int hp, int maxHp, bool isNewBuff)
    {
        float value = hp - buffValue;
        float hpValue = (fillRate * value / maxHp) - fillRate;
        hpBarFillerPoison.gameObject.SetActive(true);
        if(isNewBuff){
            hpBarFillerPoison.transform.localPosition = hpBarFiller.transform.localPosition;
        }
        hpBarFiller.transform.localPosition = new Vector3(hpValue, 0, 0);
    }

    // 일반 상태의 Hp bar로 설정(중독 상태를 나타내는 Hp Bar를 비활성화 하고, Hp bar의 위치를 최대 체력과 현재 체력에 비례해서 설정)
    public void SetHpBarByNoneFlowerPowderState(int hp, int maxHp)
    {
        hpBarFillerPoison.gameObject.SetActive(false);
        float hpValue = (fillRate * hp /  maxHp) - fillRate;
        hpBarFiller.transform.localPosition = new Vector3(hpValue, 0, 0);
    }

    public void SetShieldValue(int value,bool isGain, bool isEnemy)
    {
        if(value == 0){
            sequence = DOTween.Sequence()
                .Join(shieldCanvas.GetComponent<CanvasGroup>().DOFade(0f, 0.5f))
                .Join(shieldIcon.GetComponent<SpriteRenderer>().DOFade(0f, 0.5f))
                .Join(shieldBase.GetComponent<SpriteRenderer>().DOFade(0f, 0.5f));
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
