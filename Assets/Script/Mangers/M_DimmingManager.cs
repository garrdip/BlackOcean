using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;
using Mirror;
using Spine.Unity.Examples;


public class M_DimmingManager : NetworkSingletonD<M_DimmingManager>
{
    SpriteRenderer dim;

    protected override void Start()
    {
        DontDestroyOnLoad(gameObject);
        dim = GetComponent<SpriteRenderer>();
    }

    [ClientRpc]
    public void StartDimming(List<TargetObject> targets)
    {
        DOTween.Kill(dim);
        Dimming();
        foreach(TargetObject tar in targets)
        {
            if(tar != null)
            {
                SetTargetObjectLayer(tar, "FrontLayer");
            }
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
                SetTargetObjectLayer(tar, "BackLayer");
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

    public void SetTargetObjectLayer(TargetObject tar, string layerName)
    {
        tar.targetObjectUI.GetComponent<SortingGroup>().sortingLayerName = layerName;
        if(tar.player != null){
            tar.avatar.GetComponent<MeshRenderer>().sortingLayerName = layerName;
            if(tar.ironDemon != null){
                tar.ironDemon.GetComponent<MeshRenderer>().sortingLayerName = layerName;
                tar.ironDemon.GetComponent<SkeletonRenderTexture>().quad.GetComponent<MeshRenderer>().sortingLayerName = layerName; // SkeletonRenderTexture의 정렬값 따로 조정 
            }
            tar.playerHpCanvas.sortingLayerName = layerName;
            tar.playerNameCanvas.sortingLayerName = layerName;
            tar.playerShieldCanvas.sortingLayerName = layerName;
        }else{
            tar.monster.GetComponent<MeshRenderer>().sortingLayerName = layerName;
            tar.monsterHpCanvas.sortingLayerName = layerName;
            tar.monsterNameCanvas.sortingLayerName = layerName;
            tar.monsterShieldCanvas.sortingLayerName = layerName;
        }
        foreach(Canvas canvas in tar.GetComponentsInChildren<Canvas>()){
            canvas.sortingLayerName = layerName;
        }
    }
}
