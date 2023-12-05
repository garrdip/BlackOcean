using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class Boss_Momos : SpawnedMonster
{
    public override IEnumerator DoAction()
    {
        
        List<TargetObject> highlightTargetObjects = new List<TargetObject>();
        switch(turn)
        {
            case 0 :
                highlightTargetObjects.Add(transform.parent.GetComponent<TargetObject>());
                highlightTargetObjects.AddRange(M_TurnManager.instance.GetTargetObjectFromActionTargetList(nextTarget));
                M_DimmingManager.instance.StartDimming(highlightTargetObjects);
                DoAnimation("Attact0");
                yield return new WaitForSeconds(1f);
                GeneralAttack();
                yield return new WaitForSeconds(0.333f);
                M_DimmingManager.instance.StopDimming(highlightTargetObjects);
                break;
            case 1 :
                highlightTargetObjects.Add(transform.parent.GetComponent<TargetObject>());
                highlightTargetObjects.AddRange(M_TurnManager.instance.GetTargetObjectFromActionTargetList(nextTarget));
                DoAnimation("Attact1");
                yield return new WaitForSeconds(1f);
                GeneralAttack();
                yield return new WaitForSeconds(0.333f);
                M_DimmingManager.instance.StopDimming(highlightTargetObjects);
                break;
            case 2 :
                highlightTargetObjects.Add(transform.parent.GetComponent<TargetObject>());
                highlightTargetObjects.AddRange(M_TurnManager.instance.GetTargetObjectFromActionTargetList(nextTarget));
                DoAnimation("Buff0");
                yield return new WaitForSeconds(1f);
                GeneralAttack();
                yield return new WaitForSeconds(0.333f);
                M_DimmingManager.instance.StopDimming(highlightTargetObjects);
                break;
        }
        ReturnToIdleAnimation();
        yield return new WaitForSeconds(1f);
        turn ++;
        isActive = false;
    }
/*
    [Server]
    public override void GetNextAction()
    {
        if(turn < 2)
            nextAction = monsterData.behavior[0].ActionList[0];
        else
            nextAction = monsterData.behavior[1].ActionList[0];
    }
*/
    [ClientRpc]
    public void DoAnimation(string actionName)
    {
        parent.anim.state.SetAnimation(1,actionName,false);
    }

    [Server]
    public override IEnumerator OnHitAnimation()
    {
        OnHitAnimationRPC();
        yield return new WaitForSeconds(0.667f);
        ReturnToIdleAnimation();
    }

    [ClientRpc]
    public void OnHitAnimationRPC()
    {
        parent.anim.state.SetAnimation(1,"Defense0",false);
    }

    [ClientRpc]
    public override void ReturnToIdleAnimation()
    {
        parent.anim.state.SetAnimation(1,"Idle",true);
    }

    public override void OnChanedNextAction(MonsterAction oldVal, MonsterAction newVal)
    {
        if(newVal.actionName == "")return;
        Debug.Log("정상 입력");
        transform.parent.GetChild(3).localPosition = new Vector3(transform.parent.GetChild(3).localPosition.x, 11, transform.parent.GetChild(3).localPosition.z);
        if(nextAction.actionName == "Enrage")
            parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACKANDDEBUFF,true,newVal.actionTarget,100.ToString());
        else
            parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACK,true,newVal.actionTarget,100.ToString());
 
    }
}
