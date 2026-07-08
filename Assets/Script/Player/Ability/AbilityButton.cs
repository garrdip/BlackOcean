using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DG.Tweening;
using ProjectD;


public class AbilityButton : NetworkBehaviour
{
    [SyncVar]
    public Character character;

    public GameObject leftLine;
    public GameObject leftLineLight;
    public GameObject rightLine;
    public GameObject rightLineLight;
    public GameObject lineLight;
    public GameObject icon;
    public GameObject iconLight;
    private Vector3 leftOriginPosition;
    private Vector3 rightOriginPosition;
    public Sprite danhyangIcon;
    public Sprite danhyangIconLight;
    public Sprite georkIcon;
    public Sprite georkIconLight;

    public override void OnStartClient()
    {
        transform.position = new Vector3(17.8f, -4.9f, 0);
        gameObject.SetActive(false); // 초기 시점에 버튼 비활성화
        leftOriginPosition = leftLine.transform.localPosition;
        rightOriginPosition = rightLine.transform.localPosition;
        SetAbilityButtonIcon(character);
    }

    void OnMouseDown()
    {
        if(NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayer>().character == ProjectD.Character.HONGDANHYANG)
            NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().abilityCtrlArrow.InitCardCtrlArrow(this);
        else
            NetLookup.Client<TargetObject>(NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerTarget>().targetObject).UsingGoHeng();
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

    private void SetAbilityButtonIcon(Character character)
    {
        switch(character){
            case Character.HONGDANHYANG:
                icon.GetComponent<SpriteRenderer>().sprite = danhyangIcon;
                iconLight.GetComponent<SpriteRenderer>().sprite = danhyangIconLight;
                break;
            case Character.GEORK:
                icon.GetComponent<SpriteRenderer>().sprite = georkIcon;
                iconLight.GetComponent<SpriteRenderer>().sprite = georkIconLight;
                break;
        }
    }

    public void SetAbilityButtonActive(bool isActive)
    {
        leftLineLight.SetActive(isActive);
        rightLineLight.SetActive(isActive);
        lineLight.SetActive(isActive);
        iconLight.SetActive(isActive);
        if(isActive){
            leftLine.transform.DOLocalMoveX(leftOriginPosition.x - 0.15f, 0.3f);
            rightLine.transform.DOLocalMoveX(rightOriginPosition.x + 0.15f, 0.3f);
        }else{
            leftLine.transform.DOLocalMoveX(leftOriginPosition.x, 0.3f);
            rightLine.transform.DOLocalMoveX(rightOriginPosition.x , 0.3f);
        }
    }
}
