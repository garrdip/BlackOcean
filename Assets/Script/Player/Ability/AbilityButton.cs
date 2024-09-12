using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DG.Tweening;


public class AbilityButton : NetworkBehaviour
{
    public GameObject leftLine;
    public GameObject leftLineLight;
    public GameObject rightLine;
    public GameObject rightLineLight;
    public GameObject lineLight;
    public GameObject IconLight;
    private Vector3 leftOriginPosition;
    private Vector3 rightOriginPosition;


    public override void OnStartClient()
    {
        if(isOwned){
            transform.position = new Vector3(17.8f, -4.9f, 0);
        }
        gameObject.SetActive(false); // 초기 시점에 버튼 비활성화
        leftOriginPosition = leftLine.transform.localPosition;
        rightOriginPosition = rightLine.transform.localPosition;
    }

    void OnMouseDown()
    {
        if(NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayer>().character == ProjectD.Character.HONGDANHYANG)
            NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().abilityCtrlArrow.InitCardCtrlArrow(this);
        else
            NetworkClient.spawned[NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerTarget>().targetObject].GetComponent<TargetObject>().UsingGoHeng();
    }

    void OnMouseEnter()
    {
        SetAbilityButtonActive(true);
    }

    void OnMouseExit()
    {
        if(!NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().abilityCtrlArrow.isInitialized){
            SetAbilityButtonActive(false);
        }
    }

    public void SetAbilityButtonActive(bool isActive)
    {
        leftLineLight.SetActive(isActive);
        rightLineLight.SetActive(isActive);
        lineLight.SetActive(isActive);
        IconLight.SetActive(isActive);
        if(isActive){
            leftLine.transform.DOLocalMoveX(leftOriginPosition.x - 0.15f, 0.3f);
            rightLine.transform.DOLocalMoveX(rightOriginPosition.x + 0.15f, 0.3f);
        }else{
            leftLine.transform.DOLocalMoveX(leftOriginPosition.x, 0.3f);
            rightLine.transform.DOLocalMoveX(rightOriginPosition.x , 0.3f);
        }
    }
}
