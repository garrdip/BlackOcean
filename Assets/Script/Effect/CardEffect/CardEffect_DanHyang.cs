using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using AYellowpaper.SerializedCollections;
using Spine.Unity;

// 단향 카드 이펙트용 클래스 : M_EffectManager의 partial 클래스
public partial class M_EffectManager
{
    public SerializedDictionary<Card_Effect, SkeletonDataAsset> danhyangCardEffects = new SerializedDictionary<Card_Effect, SkeletonDataAsset>();

    public List<ParticleSystem> danhyangCardEffectParticles = new List<ParticleSystem>();

    // 철귀 이빨 공격 이펙트
    [ClientRpc]
    public void RpcEffectEatter(Vector3 position)
    {
        StartCoroutine(StartEffect(
            danhyangCardEffects[Card_Effect.Effect_Eatter],
            "EffEatter",
            position,
            M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][8],
            "Effect")
        );
    }

    // 철귀 손톱 공격 이펙트
    [ClientRpc]
    public void RpcEffectClaw(Vector3 position)
    {
        StartCoroutine(StartEffect(
            danhyangCardEffects[Card_Effect.Effect_Scratch],
            "01EffScratch",
            position,
            M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][4],
            "Effect")
        );
    }

    // 단향 카드 실드류 이펙트
    [ClientRpc]
    public void RpcEffectFlowerShield(Vector3 position, int index)
    {
        AudioClip audioClip = index == -1 ? null : M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][index];
        StartCoroutine(StartEffect(
            danhyangCardEffects[Card_Effect.Effect_Shield],
            "Eff6_Shield",
            position,
            audioClip,
            "Effect")
        );
    }

    // 단향 직접 공격류 이펙트
    [ClientRpc]
    public void RpcEffectCutLeafAttack(Vector3 position, int index)
    {
        AudioClip audioClip = index == -1 ? null : M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][index];
        StartCoroutine(StartEffect(
            danhyangCardEffects[Card_Effect.Effect_CutLeaf],
            "Eff1_CutLeaf",
            position,
            audioClip,
            "Effect")
        );
    }

    [ClientRpc]
    public void RpcEffectBodyTurnZoomIn(Vector3 position, int index)
    {
        AudioClip audioClip = index == -1 ? null : M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][index];
        StartCoroutine(StartEffect(
            danhyangCardEffects[Card_Effect.Effect_BodyTurnZoomIn],
            "Eff0_BodyZoomIn",
            position,
            audioClip,
            "Effect") 
        );
    }

    // 개화 이펙트
    [ClientRpc]
    public void RpcEffectBackTurnBottomLeaf(Vector3 position, int index)
    {
        AudioClip audioClip = index == -1 ? null : M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][index];
        StartCoroutine(StartEffect(
            danhyangCardEffects[Card_Effect.Effect_BackTurnBottom],
            "Eff2_TurnBottom",
            position,
            audioClip,
            "FrontLayer") 
        );
        Instantiate(danhyangCardEffectParticles[0], position, Quaternion.identity, transform); // 파티클 생성
    }

    [ClientRpc]
    public void RpcEffectTurnBottomLeaf(Vector3 position, int index)
    {
        AudioClip audioClip = index == -1 ? null : M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][index];
        StartCoroutine(StartEffect(
            danhyangCardEffects[Card_Effect.Effect_TurnBottom],
            "Eff2_TurnBottom",
            position,
            audioClip,
            "Effect") 
        );
    }

    // 화합 이펙트
    [ClientRpc]
    public void RpcEffectUpLight(Vector3 position, int index)
    {
        AudioClip audioClip = index == -1 ? null : M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][index];
        StartCoroutine(StartEffect(
            danhyangCardEffects[Card_Effect.Effect_UpLight],
            "Eff3_UpLight",
            position,
            audioClip,
            "Effect") 
        );
    }

    // 꽃가루 이펙트(오브젝트 뒤쪽 꽃잎 )
    [ClientRpc]
    public void RpcEffectBackFallingLeaf(Vector3 position, int index)
    {
        AudioClip audioClip = index == -1 ? null : M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][index];
        StartCoroutine(StartEffect(
            danhyangCardEffects[Card_Effect.Effect_BackFallingLeaf],
            "Eff4_FallingLeaf",
            position,
            audioClip,
            "BackLayer") 
        );
    }

    // 꽃가루 이펙트(오브젝트 앞쪽 꽃잎)
    [ClientRpc]
    public void RpcEffectFallingLeaf(Vector3 position, int index)
    {
        AudioClip audioClip = index == -1 ? null : M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][index];
        StartCoroutine(StartEffect(
            danhyangCardEffects[Card_Effect.Effect_FallingLeaf],
            "Eff4_FallingLeaf",
            position,
            audioClip,
            "Effect")
        );
    }

    // 이치 부여 이펙트
    [ClientRpc]
    public void RpcEffectBodyZoomOut(Vector3 position, int index)
    {
        AudioClip audioClip = index == -1 ? null : M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][index];
        StartCoroutine(StartEffect(
            danhyangCardEffects[Card_Effect.Effect_BodyZoomOut],
            "Eff5_BodyZoomOut",
            position,
            audioClip,
            "Effect")
        );
    }
}

public enum Card_Effect {
    Effect_Eatter,
    Effect_Scratch,
    Effect_BodyTurnZoomIn,
    Effect_CutLeaf,
    Effect_BackTurnBottom,
    Effect_TurnBottom,
    Effect_UpLight,
    Effect_BackFallingLeaf,
    Effect_FallingLeaf,
    Effect_BodyZoomOut,
    Effect_Shield
}