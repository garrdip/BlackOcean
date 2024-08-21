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
        if(isShakeXY){
            mainCamera.transform.DOShakePosition(shakeDuration, new Vector3(shakeStrength, 0f, 0f), vibrato, randomness, false, true);
        }else{
            mainCamera.transform.DOShakePosition(shakeDuration, shakeStrength, vibrato, randomness, false, true);
        }
        DOVirtual.DelayedCall(shakeDuration, () => { 
            mainCamera.transform.position = originPosition;
        });
    }
}
