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

    public override IEnumerator DoAction()
    {
        switch(nextAction.actionName){
            case "두번찍기" :
                GeneralAttack();
                DoAnimation("1Attack");
                break;
            case "힘증가" :
                parent.GainBuff(BuffType.ICHI_ATTACK,nextAction.actionValue);
                DoAnimation("1Buff");
                break;
        }
        yield return new WaitForSeconds(1f);
        isActive = false;
    }
    public void DoAnimation(string actionName)
    {
        parent.anim.state.SetAnimation(1,actionName,false);
    }

    public override void OnHitAnimation()
    {
        parent.anim.state.SetAnimation(1,"1Defence",false);
    }
}
