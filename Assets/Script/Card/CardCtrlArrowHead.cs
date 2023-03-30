using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CardCtrlArrowHead : NetworkBehaviour
{
    void Start()
    {
        
    }

    // 화살표 인디케이터 헤드가 TargetObject로 Enter 감지해서 붉은색으로 변경
    private void OnTriggerEnter(Collider collider)
    {
        if(collider != null && collider.tag.Equals("TargetObject")){
            collider.gameObject.transform.GetChild(0).GetComponent<SpriteRenderer>().color = Color.red;
        }
    }

    // 화살표 인디케이터 헤드가 TargetObject로 Exit 감지해서 흰색으로 변경
    private void OnTriggerExit(Collider collider)
    {
        if(collider != null && collider.tag.Equals("TargetObject")){
            collider.gameObject.transform.GetChild(0).GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

}
