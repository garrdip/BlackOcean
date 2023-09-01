using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class AbilityCtrlArrowNode : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnChangeCardCtrlArrow))]
    public AbilityCtrlArrow abilityCtrlArrow; // 화살표 몸통의 부모 오브젝트 클래스


    // --------------------------------------------------------------SyncVar Hook ----------------------------------------------------------------------//

    // 화살표 몸통의 부모 오브젝트 설정
    public void OnChangeCardCtrlArrow(AbilityCtrlArrow oldCardCtrlArrow, AbilityCtrlArrow newCarCtrlAroow)
    {
        transform.SetParent(newCarCtrlAroow.transform);
        newCarCtrlAroow.arrowNodes.Add(GetComponent<Transform>());
    }
}
