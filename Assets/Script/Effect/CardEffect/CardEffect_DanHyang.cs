using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


// 단향 카드 이펙트용 클래스 : M_EffectManager의 partial 클래스
public partial class M_EffectManager
{
    // 이빨 공격 이펙트
    [ClientRpc]
    public void RpcEffectEatter(Vector3 position)
    {
        StartCoroutine(StartEffect(
            effects[Card_Effect.Effect_Eatter],
            "EffEatter",
            position,
            M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][8],
            "Effect")
        );
    }

    // 손톱 공격 이펙트
    [ClientRpc]
    public void RpcEffectClaw(Vector3 position)
    {
        StartCoroutine(StartEffect(
            effects[Card_Effect.Effect_Scratch],
            "01EffScratch",
            position,
            M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][4],
            "Effect")
        );
    }

    // 단향 카드 실드류 이펙트
    [ClientRpc]
    public void RpcEffectFlowerShield(Vector3 position)
    {
        StartCoroutine(StartEffect(
            effects[Card_Effect.Effect_Shield],
            "Eff6_Shield",
            position,
            M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][12],
            "Effect")
        );
    }

    // 단향 직접 공격류 이펙트
    [ClientRpc]
    public void RpcEffectCutLeafAttack(Vector3 position)
    {
        StartCoroutine(StartEffect(
            effects[Card_Effect.Effect_CutLeaf],
            "Eff1_CutLeaf",
            position,
            M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][101],
            "Effect")
        );
    }

    [ClientRpc]
    public void RpcEffectBodyTurnLeaf(Vector3 position)
    {
        StartCoroutine(StartEffect(
            effects[Card_Effect.Effect_BodyTurnZoomIn],
            "Eff0_BodyZoomIn",
            position,
            M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][0], // 이펙트에 맞는 SFX로 변경 해야함
            "Effect") 
        );
    }

    [ClientRpc]
    public void RpcEffectBackTurnBottomLeaf(Vector3 position)
    {
        StartCoroutine(StartEffect(
            effects[Card_Effect.Effect_BackTurnBottom],
            "Eff2_TurnBottom",
            position,
            M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][0], // 이펙트에 맞는 SFX로 변경 해야함
            "FrontLayer") 
        );
    }

    [ClientRpc]
    public void RpcEffectTurnBottomLeaf(Vector3 position)
    {
        StartCoroutine(StartEffect(
            effects[Card_Effect.Effect_TurnBottom],
            "Eff2_TurnBottom",
            position,
            M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][0], // 이펙트에 맞는 SFX로 변경 해야함
            "Effect") 
        );
    }

    [ClientRpc]
    public void RpcEffectUpLight(Vector3 position)
    {
        StartCoroutine(StartEffect(
            effects[Card_Effect.Effect_UpLight],
            "Eff3_UpLight",
            position,
            M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][0], // 이펙트에 맞는 SFX로 변경 해야함
            "Effect") 
        );
    }

    // 꽃잎 이펙트(오브젝트 뒤쪽)
    [ClientRpc]
    public void RpcEffectBackFallingLeaf(Vector3 position)
    {
        StartCoroutine(StartEffect(
            effects[Card_Effect.Effect_BackFallingLeaf],
            "Eff4_FallingLeaf",
            position,
            M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][0], // 이펙트에 맞는 SFX로 변경 해야함
            "BackLayer") 
        );
    }

    // 꽃잎 이펙트(오브젝트 앞쪽)
    [ClientRpc]
    public void RpcEffectFallingLeaf(Vector3 position)
    {
        StartCoroutine(StartEffect(
            effects[Card_Effect.Effect_FallingLeaf],
            "Eff4_FallingLeaf",
            position,
            M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][0], // 이펙트에 맞는 SFX로 변경 해야함
            "Effect")
        );
    }

    [ClientRpc]
    public void RpcEffectBodyZoomOut(Vector3 position)
    {
        StartCoroutine(StartEffect(
            effects[Card_Effect.Effect_BodyZoomOut],
            "Eff5_BodyZoomOut",
            position,
            M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][0],
            "Effect") // 이펙트에 맞는 SFX로 변경 해야함
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