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
    }
}
