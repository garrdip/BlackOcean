using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class CharactorSelector : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public Material defaultMaterial;
    public Material outLineMaterial;


    void Start()
    {
       
    }

    void OnMouseEnter()
    {
        meshRenderer.material = outLineMaterial;
    }

    void OnMouseExit()
    {
        meshRenderer.material = defaultMaterial;
    }

    void OnMouseDown()
    {
        PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
        TargetObject targetObject =  transform.parent.GetComponent<TargetObject>();
        playerInterface.currentGamePlayerNetId = targetObject.player.netId;
    }
}
