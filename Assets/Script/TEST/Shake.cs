using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Shake : MonoBehaviour
{
    public Camera mainCamera;
    public float shakeStrength;
    public float shakeDuration;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        shakeStrength = 0.3f;
        shakeDuration = 0.2f;
    }

 
    public void Shaking()
    {
        Vector3 originalPos = mainCamera.transform.position;
        mainCamera.transform.DOShakePosition(shakeDuration, shakeStrength);
        DOVirtual.DelayedCall(shakeDuration, () =>
        {
            mainCamera.transform.position = originalPos;
        });
    }
}
