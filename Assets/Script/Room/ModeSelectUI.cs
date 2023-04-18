using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;
using DG.Tweening;

public class ModeSelectUI : NetworkBehaviour
{
    [SyncVar (hook = nameof(OnChangedLevel))]
    public GameLevel gameLevel = GameLevel.EASY;

    //각 레벨 셀렉트 모델의 최상위 오브젝트
    public List<GameObject> levels;

    public List<GameObject> levelSliders;

    public List<GameObject> levelIcons;

    public string test;

    [SerializeField]
    float moveDuration = 0.7f;

    public (int,int,int)[] verticalLocation = {(400,-200,-400),(400,200,-400),(400,200,0)};
    readonly Vector3 SMALLPOSITION = new Vector3(57,5,0);
    readonly Vector3 SMALLSCALE = new Vector3(0.3f,0.3f,0.3f);
    readonly Vector3 LARGESCALE = new Vector3(1f,1f,1f);
    readonly Vector3 LARGEPOSITION = new Vector3(215,49,0);

    void Start()
    {
        levels[0].transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => SelectLevel(GameLevel.EASY));
        levels[1].transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => SelectLevel(GameLevel.NORMAL));
        levels[2].transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => SelectLevel(GameLevel.HARD));
    }
    public void SelectLevel(GameLevel level)
    {
        if(isServer)
        {
            gameLevel = level;
        }
    }

    public override void OnStartClient()
    {
        Debug.Log("스타트");
        OnChangedLevel(GameLevel.EASY, GameLevel.EASY);
        base.OnStartClient();
    }

    public void OnChangedLevel(GameLevel oldVal, GameLevel newVal)
    {

        levels[(int)oldVal].transform.GetChild(1).gameObject.SetActive(false);
        ShiftIconScale();
        LightEffectAdjust(false);
        switch(gameLevel)
        {
            case GameLevel.EASY :
                OnCompleteMove();
                levels[0].transform.DOLocalMove(new Vector3(0,verticalLocation[0].Item1,0),moveDuration);
                levels[1].transform.DOLocalMove(new Vector3(0,verticalLocation[0].Item2,0),moveDuration);
                levels[2].transform.DOLocalMove(new Vector3(0,verticalLocation[0].Item3,0),moveDuration);
                break;
            case GameLevel.NORMAL :
                if(oldVal == GameLevel.HARD) OnCompleteMove();
                levels[0].transform.DOLocalMove(new Vector3(0,verticalLocation[1].Item1,0),moveDuration);
                levels[1].transform.DOLocalMove(new Vector3(0,verticalLocation[1].Item2,0),moveDuration).OnComplete(() => OnCompleteMove());
                levels[2].transform.DOLocalMove(new Vector3(0,verticalLocation[1].Item3,0),moveDuration);
                break;
            case GameLevel.HARD :
                levels[0].transform.DOLocalMove(new Vector3(0,verticalLocation[2].Item1,0),moveDuration);
                levels[1].transform.DOLocalMove(new Vector3(0,verticalLocation[2].Item2,0),moveDuration);
                levels[2].transform.DOLocalMove(new Vector3(0,verticalLocation[2].Item3,0),moveDuration).OnComplete(() => OnCompleteMove());
                break;
        }
    }

    public void OnCompleteMove()
    {
        levels[0].transform.GetChild(1).gameObject.SetActive(gameLevel == GameLevel.EASY ? true : false);
        levels[1].transform.GetChild(1).gameObject.SetActive(gameLevel == GameLevel.NORMAL ? true : false);     
        levels[2].transform.GetChild(1).gameObject.SetActive(gameLevel == GameLevel.HARD ? true : false);
        levelSliders[0].transform.DOLocalMove(gameLevel == GameLevel.EASY ? new Vector3(-489,212,0) : new Vector3(-1000,212,0),0.5f).OnComplete(() => LightEffectAdjust(true));
        levelSliders[1].transform.DOLocalMove(gameLevel == GameLevel.NORMAL ? new Vector3(-489,212,0) : new Vector3(-1000,212,0),0.5f);
        levelSliders[2].transform.DOLocalMove(gameLevel == GameLevel.HARD ? new Vector3(-489,212,0) : new Vector3(-1000,212,0),0.5f);
    }

    public void ShiftIconScale()
    {
        levelIcons[0].transform.DOScale(gameLevel == GameLevel.EASY ? LARGESCALE : SMALLSCALE,0.5f);
        levelIcons[1].transform.DOScale(gameLevel == GameLevel.NORMAL ? LARGESCALE : SMALLSCALE,0.5f);
        levelIcons[2].transform.DOScale(gameLevel == GameLevel.HARD ? LARGESCALE : SMALLSCALE,0.5f);
        levelIcons[0].transform.DOLocalMove(gameLevel == GameLevel.EASY ? LARGEPOSITION : SMALLPOSITION,0.5f);
        levelIcons[1].transform.DOLocalMove(gameLevel == GameLevel.NORMAL ? LARGEPOSITION : SMALLPOSITION,0.5f);
        levelIcons[2].transform.DOLocalMove(gameLevel == GameLevel.HARD ? LARGEPOSITION : SMALLPOSITION,0.5f);
    }
    public void LightEffectAdjust(bool onOff)
    {
        if(onOff)
        {
            levels[0].transform.GetChild(1).GetChild(0).gameObject.SetActive(gameLevel == GameLevel.EASY ? true : false);
            levels[1].transform.GetChild(1).GetChild(0).gameObject.SetActive(gameLevel == GameLevel.NORMAL ? true : false);
            levels[2].transform.GetChild(1).GetChild(0).gameObject.SetActive(gameLevel == GameLevel.HARD ? true : false);
            levels[0].transform.GetChild(1).GetChild(0).DOScale(new Vector3(1,1,1),0.5f);
            levels[1].transform.GetChild(1).GetChild(0).DOScale(new Vector3(1,1,1),0.5f);
            levels[2].transform.GetChild(1).GetChild(0).DOScale(new Vector3(1,1,1),0.5f);
        }
        else
        {
            levels[0].transform.GetChild(1).GetChild(0).gameObject.SetActive(false);
            levels[1].transform.GetChild(1).GetChild(0).gameObject.SetActive(false);
            levels[2].transform.GetChild(1).GetChild(0).gameObject.SetActive(false);
            levels[0].transform.GetChild(1).GetChild(0).localScale = new Vector3(1,0,0);
            levels[1].transform.GetChild(1).GetChild(0).localScale = new Vector3(1,0,0);
            levels[2].transform.GetChild(1).GetChild(0).localScale = new Vector3(1,0,0);
        }
    }

}
