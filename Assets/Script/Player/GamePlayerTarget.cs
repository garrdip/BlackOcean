using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GamePlayerTarget : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnNetIdUpdate))]
    public uint targetObject;

    void OnNetIdUpdate(uint oldVal, uint newVal)
    {
        Debug.Log("targetObject 업데이트 = "+targetObject);
    }
}
