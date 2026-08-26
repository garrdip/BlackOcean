using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Shake : MonoBehaviour
{
    public Camera mainCamera;

    public Vector3 originPosition;

    [Range(0f, 3f)]
    public float shakeStrength;

    [Range(0f, 5f)]
    public float shakeDuration;

    [Range(10f, 20f)]
    public int vibrato;

    [Range(0f, 180f)]
    public float randomness;

    public bool isShakeXY = false;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        originPosition = mainCamera.transform.position;
        shakeStrength = 0.5f;
        shakeDuration = 0.1f;
        vibrato = 12;
        randomness = 90f;
    }

 
    public void Shaking()
    {
        // 중첩 셰이크 방지 — 다중 대상 공격 등으로 0.1초 안에 여러 번 호출되면, 나중 트윈이 이미 흔들린 위치를
        // 시작점으로 기록해 진폭이 합산되고(배경 스프라이트 밖 노출 → 배경 깜빡임) 원점 복귀도 어긋난다.
        // 진행 중인 셰이크를 정리하고 원점에서 새로 시작한다. 복귀는 DOShakePosition의 fadeOut(자동 시작점 복귀)이 담당.
        mainCamera.transform.DOKill();
        mainCamera.transform.position = originPosition;
        if(isShakeXY){
            mainCamera.transform.DOShakePosition(shakeDuration, new Vector3(shakeStrength, 0f, 0f), vibrato, randomness, false, true);
        }else{
            mainCamera.transform.DOShakePosition(shakeDuration, new Vector3(shakeStrength, shakeStrength, 0f), vibrato, randomness, false, true); // 2D — Z축 제외
        }
    }
}
