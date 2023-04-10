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
    public string test;

    [SerializeField]
    float moveDuration = 0.7f;

    public (int,int,int)[] verticalLocation = {(400,-200,-400),(400,200,-400),(400,200,0)};

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
        levels[0].transform.GetChild(0).gameObject.SetActive(gameLevel == GameLevel.EASY ? false : true);
        levels[1].transform.GetChild(0).gameObject.SetActive(gameLevel == GameLevel.NORMAL ? false : true);
        levels[2].transform.GetChild(0).gameObject.SetActive(gameLevel == GameLevel.HARD ? false : true);
        levels[(int)oldVal].transform.GetChild(1).gameObject.SetActive(false);
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
    }
}
