using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

public class NextActionIndicator : MonoBehaviour
{
    public List<Sprite> actionIcons;
    public GameObject actionIcon;
    public GameObject actionTarget;

    public void SetNextTargetAction(ActionType type, bool isTargetable, ActionTarget tar)
    {
        //Action Type
        actionIcon.SetActive(true);
        actionIcon.GetComponent<SpriteRenderer>().sprite = actionIcons[(int)type];
        if(isTargetable)
        {
            actionTarget.SetActive(true);
            actionTarget.transform.GetChild(2).gameObject.SetActive(tar == ActionTarget.FRONT || tar == ActionTarget.FRONT_BACK || tar == ActionTarget.FRONT_BACK || tar == ActionTarget.WHOLE);
            actionTarget.transform.GetChild(1).gameObject.SetActive(tar == ActionTarget.MIDDLE || tar == ActionTarget.MIDDLE_BACK || tar == ActionTarget.FRONT_MIDDLE || tar == ActionTarget.WHOLE);
            actionTarget.transform.GetChild(0).gameObject.SetActive(tar == ActionTarget.BACK || tar == ActionTarget.FRONT_BACK || tar == ActionTarget.MIDDLE_BACK || tar == ActionTarget.WHOLE);
        }
        else
            actionTarget.SetActive(false);

    }
}
