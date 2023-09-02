using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CardCtrlArrowNode : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnChangeCardCtrlArrow))]
    public CardCtrlArrow cardCtrlArrow; // 화살표 몸통의 부모 오브젝트 클래스


    // --------------------------------------------------------------SyncVar Hook ----------------------------------------------------------------------//

    // 화살표 몸통의 부모 오브젝트 설정
    public void OnChangeCardCtrlArrow(CardCtrlArrow oldCardCtrlArrow, CardCtrlArrow newCarCtrlAroow)
    {
        transform.SetParent(newCarCtrlAroow.transform);
        transform.localPosition = new Vector3(0f, 0f, 0f); // 동적으로 부모 설정시 localPosition이 변경되므로 부모와 같은 위치가 되도록 localPosition 0으로 설정
        newCarCtrlAroow.arrowNodes.Add(GetComponent<Transform>());
    }
}
