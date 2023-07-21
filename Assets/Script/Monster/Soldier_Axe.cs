using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class Soldier_Axe : SpawnedMonster
{
    [Header("몬스터 MeshRenderer")]
    public MeshRenderer meshRenderer;

    [Header("몬스터 기본 Material")]
    public Material defaultMaterial;

    [Header("몬스터 외곽선 Material")]
    public Material outLineMaterial;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();   
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if(collider != null && collider.tag.Equals("CardArrowHead")){
            meshRenderer.material = outLineMaterial;
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if(collider != null && collider.tag.Equals("CardArrowHead")){
            meshRenderer.material = defaultMaterial;
        }
    }

    public override void DoAction()
    {
        switch(nextAction.actionNumber){
            case 0 :
                GeneralAttack();
                DoAnimation();
                break;
            case 1 :
                parent.GainBuff(BuffType.ICHI_ATTACK,nextAction.actionValue);
                DoAnimation();
                break;
        }
    }

    [ClientRpc]
    public override void DoAnimation()
    {
        parent.anim.state.SetAnimation(1,"01Attack",false);
    }
}
