using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class MapRoom : NetworkBehaviour
{
    [SyncVar]
    public Vector2 location;
    [SyncVar]
    public int hazard;
    [SyncVar]
    public bool isComplete = false;
    SpriteRenderer testSprite;

    Camera mainCamera;

    void Awake()
    {
        testSprite = GetComponent<SpriteRenderer>();
        gameObject.transform.SetParent(Floor.instance.transform);
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = -mainCamera.transform.position.z;
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
            Collider2D hitCollider = Physics2D.OverlapPoint(worldPos);
            if (hitCollider == GetComponent<Collider2D>())
            {
                //OnButtonClick();
            }
        }
    }

    void  OnMouseDown()
    {
        M_MapManager.instance.MoveToRoom(location,transform.position);
    }


    [ClientRpc]
    public void SetSprite(Color color)
    {
        testSprite.color = color;
    }

}
