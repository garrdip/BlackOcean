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
    void Awake()
    {
        testSprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hitCollider = Physics2D.OverlapPoint(mousePosition);
            if (hitCollider == GetComponent<Collider2D>())
            {
                OnButtonClick();
            }
        }
    }

    void OnButtonClick()
    {
        M_MapManager.instance.MoveToRoom(location);
    }

    [ClientRpc]
    public void SetSprite(Color color)
    {
        testSprite.color = color;
    }

}
