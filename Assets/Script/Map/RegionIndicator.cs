using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegionIndicator : MonoBehaviour
{
    public Vector2Int coordinate; // 거점지역 라인의 Axial 좌표계

    public int index; // 거점지역 라인의 인덱스값 (0 = 12시, 1 = 2시, 2 = 4시, 3 = 6시, 4 = 8시, 5 = 10시)

    void Start()
    {
        SetTransformByIsometricRoom(index);
    }

    // Isometric 형태의 육각형 방에 대한 거점지역 라인의 Transform값 재설정
    private void SetTransformByIsometricRoom(int index)
    {
        switch(index)
        {
            case 0 : // 12시
                transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                transform.localPosition = transform.localPosition + new Vector3(0f, -0.06f, 0f);
                break;
            case 1 : // 2시
                transform.localScale = new Vector3(0.7f, 1f, 1f);
                transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
                transform.localPosition = transform.localPosition + new Vector3(0.07f, -0.06f, 0f);
                break;
            case 2 : // 4시
                transform.localScale = new Vector3(0.7f, 1f, 1f);
                transform.localRotation = Quaternion.Euler(0f, 0f, -135f);
                transform.localPosition = transform.localPosition + new Vector3(0.07f, 0.35f, 0f);
                break;
            case 3 :// 6시
                transform.localRotation = Quaternion.Euler(0f, 0f, -180f);
                transform.localPosition = transform.localPosition + new Vector3(0f, 0.35f, 0f);
                break;
            case 4 :// 8시
                transform.localScale = new Vector3(0.7f, 1f, 1f);
                transform.localRotation = Quaternion.Euler(0f, 0f, 135f);
                transform.localPosition = transform.localPosition + new Vector3(-0.07f, 0.35f, 0f);
                break;
            case 5 :// 10시
                transform.localScale = new Vector3(0.7f, 1f, 1f);
                transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                transform.localPosition = transform.localPosition + new Vector3(-0.07f, -0.05f, 0f);
                break;
        }
    }
}
