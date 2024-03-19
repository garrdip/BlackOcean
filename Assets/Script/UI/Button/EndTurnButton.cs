using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using DG.Tweening;


public class EndTurnButton : MonoBehaviour
{
    public GameObject endTurnBaseLight;
    public GameObject endTurnBaseLight2;
    public GameObject endTurnBaseLight3;
    public GameObject endTurnSBase;
    public GameObject endTurnS1;
    public GameObject endTurnS2;

    void Start()
    {
        GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
    }

    void OnDestroy()
    {
        endTurnS1.GetComponent<RectTransform>().DOKill();
        endTurnS2.GetComponent<RectTransform>().DOKill();
    }

    public void OnPointerClick()
    {
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("stage_ready"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
        playerInterface.endTurnActive = !playerInterface.endTurnActive;
        playerInterface.OnEndTurnStateChanged(playerInterface.endTurnActive,  playerInterface.endTurnActive);
    }

    public void OnPointerEnter()
    {
        endTurnBaseLight.SetActive(true);
    }

    public void OnPointerExit()
    {
        endTurnBaseLight.SetActive(false);
    }

    public void SetEndTurnButtonActiveState(bool isActive)
    {
        endTurnBaseLight.SetActive(isActive);
        endTurnBaseLight2.SetActive(isActive);
        endTurnBaseLight3.SetActive(isActive);
        endTurnSBase.SetActive(isActive);
        endTurnS1.SetActive(isActive);
        endTurnS2.SetActive(isActive);
        if(isActive){
            SetEndTurnCircleRotateInfinite();
        }
    }

    private void SetEndTurnCircleRotateInfinite()
    {
        if(!DOTween.IsTweening(endTurnS1.GetComponent<RectTransform>()) && !DOTween.IsTweening(endTurnS2.GetComponent<RectTransform>())){
            endTurnS1.GetComponent<RectTransform>().DORotate(new Vector3(0, 0, 360), 4.5f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
            endTurnS2.GetComponent<RectTransform>().DORotate(new Vector3(0, 0, -360), 4.5f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }
    }
}
