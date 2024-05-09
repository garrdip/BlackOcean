using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;


public class CardEffectBase : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private SkeletonAnimation skeletonAnimation;
    public string animationName {get; set; }
    public AudioClip sfx { get; set; }

    /*
    - 카드 이펙트는 스파인에서 Opacity값 변경을 통해 애니매이션과 투명도가 조절되는 형태임.
    - 생성시점에 매쉬랜더러가 활성화 되어있는 경우 Opacity값이 변경되지않아 깜빡임 이슈 발생.
    - 프리팹에서 매쉬랜더러 컴포넌트 비활성화 후 애니매이션 시작시점에 활성화 하는 방식으로 해결.
    */

    private void Awake()
    {
        meshRenderer =  GetComponent<MeshRenderer>();
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        skeletonAnimation.state.Start += OnAnimationStart;
        skeletonAnimation.state.Complete += OnAnimationComplete;
    }

    void Start()
    {   
        skeletonAnimation.state.SetAnimation(0, animationName, false);
    }

    private void OnAnimationStart(Spine.TrackEntry trackEntry)
    {
        meshRenderer.enabled = true; // 매쉬랜더러 활성화
        StartCoroutine(PlayEffectSFX());
    }

    private void OnAnimationComplete(Spine.TrackEntry trackEntry)
    {
        Destroy(gameObject);
    }

    private IEnumerator PlayEffectSFX()
    {
        yield return new WaitForSeconds(0.5f);
        M_SoundManager.instance.PlaySFX(sfx, sfx.length);
    }
}
