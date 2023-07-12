using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using ProjectD;
public class SpawnedMonster : NetworkBehaviour
{
    public string monsterName;

    [SyncVar]
    public int MAXHP;

    [SyncVar (hook = nameof(OnChangedHpValue))]
    public int HP;

    [SyncVar (hook = nameof(OnChangedSheild))]
    public int sheild;

    [SyncVar (hook = nameof(OnChangedAggro))]
    public int[] aggro = new int[3];

    [SyncVar]
    public MonsterAction nextAction;

    public MonsterActionList currentBehavior;

    [SyncVar]
    public TargetObject nextTargetPlayer;
    [SyncVar]
    public PlayOrder nextTarget;
    
    [SyncVar (hook = nameof(OnChangedMonsterData))]
    public MonsterData monsterData;

    [SyncVar (hook = nameof(OnChangeParent))]
    public TargetObject parent;

    public MeshRenderer meshRenderer;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();   
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if(collider != null && collider.tag.Equals("CardArrowHead")){
            meshRenderer.material = M_MonsterManager.instance.outLineMaterial;
        }
    }

    // 화살표 인디케이터 헤드가 TargetObject로 Exit 감지해서 흰색으로 변경
    private void OnTriggerExit2D(Collider2D collider)
    {
        if(collider != null && collider.tag.Equals("CardArrowHead")){
            meshRenderer.material = M_MonsterManager.instance.defaultMaterial;
        }
    }

    [Server]
    public void SetNextTarget()
    {
        nextTarget = PlayOrder.FIRST;
    }

    [Server]
    public void SetNextAction()
    {
        nextAction = GetNextAction();
    }

    MonsterAction GetNextAction()
    {
        // 다음 액션 찾는 알고리즘 추가 부분
        return monsterData.behavior[0].ActionList[0];
    }

    [Server]
    public virtual void DoAction()
    {

    }

    [ClientRpc]
    public virtual void DoAnimation()
    {
        
    }

    [Server]
    public virtual void OnChangedSheild()
    {

    }

    [Server]
    public virtual void OnChangedAggro()
    {

    }

    // ------------------------------------------------------------------ SyncVar Hook ------------------------------------------------------------------------//

    public void OnChangedMonsterData(MonsterData oldVal , MonsterData newVal)
    {
        monsterName = monsterData.name;
        MAXHP = monsterData.MAXHP;
        HP = monsterData.MAXHP;
        sheild = 0;
        //SyncVar Data는 서버에서 관리
        if(isServer)
        {
            HP = monsterData.MAXHP;
        }
    }

    public void OnChangedHpValue(int oldHpValue, int newHpValue)
    {
        if(HP <= 0)
        {
            if(isServer)NetworkServer.Destroy(this.gameObject);
            return;
        }
        if(transform.parent != null){
            transform.parent.GetComponent<TargetObject>().hpbar.value = newHpValue;
        }
    }

    public void OnChangeParent(TargetObject oldPrent, TargetObject newParent)
    {
        transform.SetParent(newParent.transform);
        Slider hpbar = newParent.transform.GetChild(0).GetChild(3).GetComponent<Slider>();
        TextMeshProUGUI textMonsterName = newParent.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>();
        textMonsterName.text = monsterName;
        hpbar.maxValue = MAXHP;;
        hpbar.value = MAXHP;
    }
    
    //-------------------------------------- Battle Method ----------------------------------//
    public void GeneralSingleAttack()
    {
        M_TurnManager.instance.GetTargetObjectFromOrder(nextTarget)[0].playerHP -= nextAction.actionValue;
        M_TurnManager.instance.GetTargetObjectFromOrder(nextTarget)[1].playerHP -= nextAction.actionValue; // Clone도 데미지 적용(TBD)
    }


    public void GeneralFullScaleAttack()
    {
        foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
            target.playerHP -= nextAction.actionValue;
        foreach(TargetObject target in M_TurnManager.instance.clonePlayerList)
            target.playerHP -= nextAction.actionValue;
    }

}
