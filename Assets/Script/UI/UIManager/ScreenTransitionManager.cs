using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenTransitionManager : SingletonD<ScreenTransitionManager>
{
    public GameObject screenTransition;
    public Image screen;

    void Start()
    {
        screen = screenTransition.GetComponent<Image>();
        screen.material =  new Material(screen.material); // 머티리얼 인스턴스 복사본을 생성하여 이미지의 머티리얼값에 할당(원본대신 복사본을 사용해 프로퍼티값 변경)
    }

    public void DoTransition(System.Action callback = null)
    {
        StartCoroutine(TransitionCoroutine(() => {
            if(callback != null){
                callback();
            }
        }));
    }
    
    public IEnumerator TransitionCoroutine(System.Action callback = null)
    {
        screen.enabled = true;
        float duration = 2.0f;
        float elapsedTime = 0f;

        float initialScroll = 2.5f; // 진행상태 프로퍼티값의 초기값
        float finalScroll = 0f;     // 진행상태 프로퍼티값의 최종값      

        while (elapsedTime < duration){
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Transition_In 구간 : 0에서 1사이의 t값이 0 ~ 0.5 구간에서는 2.5 -> 0 변경
            // Transition_Out 구간 : 0에서 1사이의 t값이 0.5 ~ 1.0 구간에서는 0 -> 2.5 변경     
            float currentScroll = t < 0.5f
                ? Mathf.Lerp(initialScroll, finalScroll, t * 2)
                : Mathf.Lerp(finalScroll, initialScroll, (t - 0.5f) * 2);
            if (t >= 0.5f && callback != null){ // Transition_In 구간이 끝나고 Transition_Out 구간이 시작될 때 콜백 호출
                callback();
                callback = null; // 콜백이 한 번만 호출되도록 null로 설정
            }
            screen.material.SetFloat("_Progress", currentScroll);

            yield return null;
        }

        screen.material.SetFloat("_Progress", initialScroll);
        screen.enabled = false;
    }
}
