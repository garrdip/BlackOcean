using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;


public class EffectBase : MonoBehaviour
{
    private SkeletonAnimation skeletonAnimation;
    public AudioClip sfx { get; set; } // 이펙트 효과음


    private void Awake()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        skeletonAnimation.state.Start += OnAnimationStart;
        skeletonAnimation.state.Complete += OnAnimationComplete;
    }


    private void OnAnimationStart(Spine.TrackEntry trackEntry)
    {
        StartCoroutine(PlayEffectSFX());
    }

    private void OnAnimationComplete(Spine.TrackEntry trackEntry)
    {
        Destroy(gameObject);
    }

    private IEnumerator PlayEffectSFX()
    {
        yield return new WaitForSeconds(0.5f);
        if(sfx != null){
            M_SoundManager.instance.PlaySFX(sfx, sfx.length);
        }
    }
}
