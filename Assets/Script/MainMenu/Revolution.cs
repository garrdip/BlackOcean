using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Revolution : MonoBehaviour
{
    public Transform innerOrbit;
    public Transform middleOrbit;
    public Transform outerOrbit;

    void FixedUpdate()
    {
        innerOrbit.Rotate(new Vector3(0,0,1.35f),Space.Self);
        middleOrbit.Rotate(new Vector3(0,0,0.9f),Space.Self);
        outerOrbit.Rotate(new Vector3(0,0,0.6f),Space.Self);
    }
}
