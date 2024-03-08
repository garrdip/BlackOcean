using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;
using Mirror;
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
            tar.targetObjectUI.GetComponent<SortingGroup>().sortingLayerName = "FrontLayer";
            if(tar.player != null && tar.ironDemon != null){
                tar.avatar.GetComponent<MeshRenderer>().sortingLayerName = "FrontLayer";
                tar.ironDemon.GetComponent<MeshRenderer>().sortingLayerName = "FrontLayer";
                tar.playerHpCanvas.sortingLayerName = "FrontLayer";
                tar.playerNameCanvas.sortingLayerName = "FrontLayer";
                tar.playerShieldCanvas.sortingLayerName = "FrontLayer";
            }else{
                tar.monster.GetComponent<MeshRenderer>().sortingLayerName = "FrontLayer";
                tar.monsterHpCanvas.sortingLayerName = "FrontLayer";
                tar.monsterNameCanvas.sortingLayerName = "FrontLayer";
                tar.monsterShieldCanvas.sortingLayerName = "FrontLayer";
            }
            foreach(Canvas canvas in tar.GetComponentsInChildren<Canvas>()){
                canvas.sortingLayerName = "FrontLayer";
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
                tar.targetObjectUI.GetComponent<SortingGroup>().sortingLayerName = "BackLayer";
                if(tar.player != null && tar.ironDemon != null){
                    tar.avatar.GetComponent<MeshRenderer>().sortingLayerName = "BackLayer";
                    tar.ironDemon.GetComponent<MeshRenderer>().sortingLayerName = "BackLayer";
                    tar.playerHpCanvas.sortingLayerName = "BackLayer";
                    tar.playerNameCanvas.sortingLayerName = "BackLayer";
                    tar.playerShieldCanvas.sortingLayerName = "BackLayer";
                }else{
                    tar.monster.GetComponent<MeshRenderer>().sortingLayerName = "BackLayer";
                    tar.monsterHpCanvas.sortingLayerName = "BackLayer";
                    tar.monsterNameCanvas.sortingLayerName = "BackLayer";
                    tar.monsterShieldCanvas.sortingLayerName = "BackLayer";
                }
                foreach(Canvas canvas in tar.GetComponentsInChildren<Canvas>()){
                    canvas.sortingLayerName = "BackLayer";
                }
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
