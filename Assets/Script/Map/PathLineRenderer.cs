using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathLineRenderer : MonoBehaviour
{
    public HexagonMapRoom hexagonMapRoom;
    public uint netId;
    public float rotationZ;

    void Start()
    {
        transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
    }
}
