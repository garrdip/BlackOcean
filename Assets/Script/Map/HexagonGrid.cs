using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class HexagonGrid : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        transform.parent.SetParent(M_MapManager.instance.gridParent);
        transform.localPosition = new Vector3(transform.position.x, transform.position.y, 0f);
        transform.localRotation = Quaternion.Euler(0f,0,0);
    }
}
