using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerOrder : NetworkBehaviour
{
    [Header("Layout")]
    public GameObject BaseLayout;
    public GameObject TopLayout;
    public GameObject LastCardLayout;

    [Header("BaseLayout Components")]
    public GameObject uLight;
    public GameObject uBase;
    public GameObject uBaseC;
    public GameObject uLine;
    public GameObject uLineLight;
    public GameObject uMyLine;
    public GameObject uMyLineLight;

    [Header("TopLayout Components")]
    public GameObject topBase;
    public GameObject topBaseLight;
    public GameObject topMy;
    public GameObject topMyLight;
    public GameObject topSee;
    public GameObject topSeeLight;
    public GameObject topReady;
    public GameObject topReadyLight;

    [Header("TopLayout Components")]
    public GameObject lastCardbase;
    public GameObject lastCardBaseLine;
    public GameObject lastCardBaseLingLight;

    [SyncVar]
    public GamePlayer gamePlayer;

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    void Start()
    {
        gamePlayer.onChangePlayerOrder += OnChangePlayerOrder;
        SetParentAndPostion(gamePlayer.selectOrder);
        SetOwnedViewComponent();
    }


    void OnMouseEnter()
    {
        uMyLineLight.SetActive(isOwned);
        topMyLight.SetActive(isOwned);
        uLineLight.SetActive(true);
        topBaseLight.SetActive(true);
        topSeeLight.SetActive(true);
        lastCardBaseLingLight.SetActive(true);
    }

    void OnMouseExit()
    {
        uMyLineLight.SetActive(false);
        topMyLight.SetActive(false);
        uLineLight.SetActive(false);
        topBaseLight.SetActive(false);
        topSeeLight.SetActive(false);
        lastCardBaseLingLight.SetActive(false);
    }

    public void OnChangePlayerOrder(int order)
    {
        SetParentAndPostion(gamePlayer.selectOrder);
    }

    // 참조된 게임플레이어 클래스로부터 오더값 조회하여 값에 맞춰 뷰 컴포넌트 세팅
    private void SetParentAndPostion(int order)
    {
        transform.position = new Vector3(M_TurnManager.instance.targetObjectPosition[gamePlayer.selectOrder].x, 8f, 0f);
        transform.localScale = new Vector3(1f, 1f, 1f);
    }

    // 본인 소유임을 구분하는 뷰 컴포넌트 세팅
    private void SetOwnedViewComponent()
    {
        uLight.SetActive(isOwned);
        uMyLine.SetActive(isOwned);
    }
}
