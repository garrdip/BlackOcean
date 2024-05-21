using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using AYellowpaper.SerializedCollections;
using DG.Tweening;
using TMPro;
using ProjectD;

public class M_EffectManager : NetworkSingletonD<M_EffectManager>
{
    public SerializedDictionary<Card_Effect, GameObject> effects = new SerializedDictionary<Card_Effect, GameObject>(); 

    public Canvas EffectCanvas;
    public GameObject FloatingDamageText;

    protected override void Start()
    {
        DontDestroyOnLoad(gameObject);
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.persistentManagers.Add(gameObject.name, gameObject);  
    }

    // 이빨 공격 이펙트
    [ClientRpc]
    public void RpcEffectEatter(Vector3 position)
    {
        GameObject Effect = Instantiate(effects[Card_Effect.Effect_Eatter], position, Quaternion.identity);
        CardEffectBase cardEffectBase = Effect.GetComponent<CardEffectBase>();
        cardEffectBase.animationName = "EffEatter";
        cardEffectBase.sfx = M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][8];
    }

    // 손톱 공격 이펙트
    [ClientRpc]
    public void RpcEffectClaw(Vector3 position)
    {
        GameObject Effect = Instantiate(effects[Card_Effect.Effect_Scratch], position, Quaternion.identity);
        CardEffectBase cardEffectBase = Effect.GetComponent<CardEffectBase>();
        cardEffectBase.animationName = "01EffScratch";
        cardEffectBase.sfx = M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][4];
    }

    // 데미지 표시 트위닝
    public void DisPlayeDamage(TargetObject targetObject, int damage)
    {
        Camera.main.GetComponent<Shake>().Shaking();
        GameObject floatingDamage = Instantiate(FloatingDamageText, Vector3.zero, Quaternion.identity);
        floatingDamage.transform.SetParent(EffectCanvas.transform);
        floatingDamage.transform.position = targetObject.transform.position + new Vector3(0f, 6f, 0f);
        floatingDamage.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
        floatingDamage.GetComponent<TextMeshProUGUI>().text = damage.ToString();

        bool reversePath = Random.Range(0, 2) == 0; // 좌측커브 or 우측커브 랜덤 결정
        Vector3 endPoint = reversePath ? floatingDamage.transform.position + new Vector3(-3f, -12f, 0f) : floatingDamage.transform.position + new Vector3(3f, -12f, 0f);
        Tween curveTween = floatingDamage.transform.DOJump(endPoint, 9f, 1, 0.5f);
        Tween fadeTween = floatingDamage.GetComponent<CanvasGroup>().DOFade(0f, 1f);
        Tween scaleTween = floatingDamage.transform.DOPunchScale(new Vector3(3f, 3f, 3f), 0.5f, 2, 1f).SetEase(Ease.OutCubic);
        Tween scaleReturnTween = floatingDamage.transform.DOScale(1f, 0.5f);
        Sequence sequence = DOTween.Sequence();
        sequence.Append(scaleTween);
        sequence.Join(curveTween);
        sequence.Insert(0.2f, scaleReturnTween);
        sequence.Join(fadeTween)
                .OnComplete(() => {
                    floatingDamage.transform.DOKill();
                    Destroy(floatingDamage);
                });
    }

    // 방어도 표시 트위닝
    public void DisplayDefence(TargetObject targetObject, bool isGain, int value)
    {
        if(isGain){ // 방어력 얻을 때 효과
            GameObject defenceGainText = Instantiate(FloatingDamageText, Vector3.zero, Quaternion.identity);
            defenceGainText.transform.SetParent(EffectCanvas.transform);
            defenceGainText.transform.localScale = Vector3.one;
            defenceGainText.transform.position = targetObject.transform.position + new Vector3(0f, 8f, 0f);
            defenceGainText.GetComponent<TextMeshProUGUI>().color = ColorUtils.HexToColor("#0082FA");
            defenceGainText.GetComponent<TextMeshProUGUI>().text = "+" + value.ToString();
            
            Tween moveTween = defenceGainText.transform.DOMoveY(3f, 1.5f).SetEase(Ease.OutSine);
            Sequence sequence = DOTween.Sequence();
            sequence.Append(moveTween);
            sequence.Join(defenceGainText.GetComponent<CanvasGroup>().DOFade(1f, 0.5f));
            sequence.Append(defenceGainText.GetComponent<CanvasGroup>().DOFade(0f, 0.5f))
                .OnComplete(() => {
                    defenceGainText.transform.DOKill();
                    Destroy(defenceGainText);
                });
        }else{ // 방어력 잃을 때 효과
            GameObject defendText = Instantiate(FloatingDamageText, Vector3.zero, Quaternion.identity);
            defendText.transform.SetParent(EffectCanvas.transform);
            defendText.transform.position = targetObject.transform.position + new Vector3(0f, 6f, 0f);
            defendText.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
            defendText.GetComponent<TextMeshProUGUI>().color = ColorUtils.HexToColor("#808080");
            defendText.GetComponent<TextMeshProUGUI>().fontSize = 60f;
            defendText.GetComponent<TextMeshProUGUI>().text = Const.DEFEND_TEXT;

            Tween moveTween = defendText.transform.DOMoveY(8f, 1.5f).SetEase(Ease.OutSine);
            Tween scaleTween = defendText.transform.DOScale(2f, 0.5f).SetEase(Ease.OutCubic);
            Tween scaleReturnTween = defendText.transform.DOScale(1f, 0.5f);
            Sequence defenceTextsequence = DOTween.Sequence();
            defenceTextsequence.Append(moveTween);
            defenceTextsequence.Join(scaleTween);
            defenceTextsequence.Append(scaleReturnTween)
                .OnComplete(() => {
                    defendText.transform.DOKill();
                    Destroy(defendText);
                });
        }
    }
}

public enum Card_Effect {
    Effect_Eatter,
    Effect_Scratch
}
