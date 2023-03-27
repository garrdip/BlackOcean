using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CardPocket : NetworkBehaviour
{
    private Vector3 hidePosition;
    private Vector3 showPosition;

    public Vector3 mousePosition;
    public Vector3 targetPosition;

    public Vector3 targetScale;
    public float hoveredPositionY;
    public Camera mainCamera;

    public CardOnHand currentCardOnHand;

    public readonly SyncList<CardOnHand> cards = new SyncList<CardOnHand>();



    void Start()
    {
        transform.SetParent(DeckUI.instance.DeckListPanel.transform);
        hidePosition = transform.localPosition + new Vector3(-20f, -3.5f, 0f);
        showPosition = transform.localPosition + new Vector3(20f, -3.5f, 0f);
        targetScale = new Vector3(3f, 4f, 0f) + new Vector3(1f, 1.5f, 0f);
        mainCamera = Camera.main;
        hoveredPositionY = 0.8f;
    }

    void FixedUpdate()
    {
        if(M_TurnManager.instance.isMyTurn){
            transform.localPosition = showPosition;
        }else{
            transform.localPosition = hidePosition;
        }
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(mousePos + new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, 1f));

        Debug.DrawRay(mousePos + new Vector3(0f, 0f, -0.01f), new Vector3(0f, 0f, 100f), Color.red);
        float minDistance = float.MaxValue;
        Collider closestCollider = null;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && hit.collider.GetComponent<CardOnHand>() != null)
            {
                float distance = Vector3.Distance(mousePos, hit.collider.GetComponent<CardOnHand>().originPosition);
                if (distance < minDistance){
                    minDistance = distance;
                    closestCollider = hit.collider;
                }
            }
        }

        if (closestCollider != null){
            GameObject collisionGameObject = closestCollider.gameObject;
            collisionGameObject.GetComponent<CardOnHand>().isMouseOver = true;
            foreach(CardOnHand cardOnHand in cards){
                if(collisionGameObject.GetComponent<CardOnHand>() != cardOnHand){
                    cardOnHand.isMouseOver = false;
                }
            }
        }else{
            foreach(CardOnHand cardOnHand in cards){
                cardOnHand.isMouseOver = false;
            }
        }
 
    }
}
