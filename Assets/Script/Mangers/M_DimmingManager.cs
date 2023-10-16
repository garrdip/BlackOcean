using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;
using Mirror;
public class M_DimmingManager : NetworkSingletonD<M_DimmingManager>
{
    SpriteRenderer dim;

    void Start()
    {
        dim = GetComponent<SpriteRenderer>();
    }
    [ClientRpc]
    public void StartDimming(List<TargetObject> targets)
    {
        DOTween.Kill(dim);
        Dimming();
        foreach(TargetObject tar in targets)
        {
            tar.GetComponent<SortingGroup>().sortingLayerName = "FrontLayer";
            foreach(Canvas canvas in tar.GetComponentsInChildren<Canvas>())
                canvas.sortingLayerName = "FrontLayer";
        }
    }
    [ClientRpc]
    public void StopDimming(List<TargetObject> targets)
    {
        Clear();
        foreach(TargetObject tar in targets)
        {
            if(tar != null)
            {
                tar.GetComponent<SortingGroup>().sortingLayerName = "BackLayer";
                foreach(Canvas canvas in tar.GetComponentsInChildren<Canvas>())
                    canvas.sortingLayerName = "BackLayer";
            }
        }
    }

    public void Dimming()
    {
        dim.DOFade(0.6f,0.4f);
    }

    public void Clear()
    {
        dim.DOFade(0f,0.4f);
    }
}
