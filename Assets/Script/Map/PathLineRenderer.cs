using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathLineRenderer : MonoBehaviour
{
    public uint netId;
    public float rotationZ;

    void Start()
    {
        transform.SetParent(M_MapManager.instance.MapPathLines.transform);
        transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0f);
    }
}
