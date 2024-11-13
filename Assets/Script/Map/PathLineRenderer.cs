using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PathLineRenderer : MonoBehaviour
{
    public HexagonMapRoom hexagonMapRoom;
    public uint netId;
    public GameObject startPoint;
    public GameObject startPointLight;
    public GameObject pathArrow;
    private SpriteRenderer pathArrowSpriteRenderer;


    void Start()
    {
        pathArrowSpriteRenderer = pathArrow.GetComponent<SpriteRenderer>();
        DoPathLineMoveLoopTween();
        DoPathLineFadeLoopTwwen();   
    }

    private void OnDestroy()
    {
        pathArrow.transform.DOKill();
        pathArrowSpriteRenderer.DOKill();
    }

    private void DoPathLineMoveLoopTween()
    {
        if(DOTween.IsTweening(pathArrow)){
            pathArrow.transform.DOKill();
        }else{
            pathArrow.transform
                .DOLocalMoveX(0.5f, 3f)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
        }
    }

    private void DoPathLineFadeLoopTwwen()
    {
        if(DOTween.IsTweening(pathArrowSpriteRenderer)){
            pathArrowSpriteRenderer.DOKill();
        }else{
            pathArrowSpriteRenderer.DOFade(1f, 1.5f).OnComplete(() => {
                pathArrowSpriteRenderer.DOFade(0f, 1.5f);
            })
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Yoyo);
        }
    }
}
