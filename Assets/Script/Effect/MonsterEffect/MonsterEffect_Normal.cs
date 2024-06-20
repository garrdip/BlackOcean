using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using AYellowpaper.SerializedCollections;
using Spine.Unity;

public partial class M_EffectManager
{
    public SerializedDictionary<Monster_Effect, SkeletonDataAsset> normalMonsterEffects = new SerializedDictionary<Monster_Effect, SkeletonDataAsset>();

    // 몬스터 찌르기 공격 이펙트
    [ClientRpc]
    public void RpcEffectNormalMonsterSting(Vector3 position)
    {
        StartCoroutine(StartEffect(
            normalMonsterEffects[Monster_Effect.Eff_Sting],
            "Eff0_Sting",
            position,
            M_SoundManager.instance.sfxClips[SFX_TYPE.Normal_Spear].Find((audioClip) => audioClip.name.Equals("monster_nor_spear_1_1")),
            "Effect")
        );
    }

    // 몬스터 베기 공격 이펙트
    [ClientRpc]
    public void RpcEffectNormalMonsterCut(bool isSoldierAxe, Vector3 position)
    {
        AudioClip attackSFX = isSoldierAxe ? 
            M_SoundManager.instance.sfxClips[SFX_TYPE.Normal_Axe].Find((audioClip) => audioClip.name.Equals("monster_nor_axe_1_3")) :
            M_SoundManager.instance.sfxClips[SFX_TYPE.Normal_Axe].Find((audioClip) => audioClip.name.Equals("monster_nor_sw_shd_1_3"));
        StartCoroutine(StartEffect(
            normalMonsterEffects[Monster_Effect.Eff_Cut],
            "Eff1_Cut",
            position,
            attackSFX,
            "Effect")
        );
    }

    // 몬스터 디버프 이펙트
    [ClientRpc]
    public void RpcEffectNormalMonsterBang(Vector3 position)
    {
        StartCoroutine(StartEffect(
            normalMonsterEffects[Monster_Effect.Eff_Bang],
            "Eff2_bang",
            position,
            null,
            "Effect")
        );
    }

    // 몬스터 마법 공격 이펙트
    [ClientRpc]
    public void RpcEffectNormalMonsterMagicAttack(Vector3 position)
    {
        StartCoroutine(StartEffect(
            normalMonsterEffects[Monster_Effect.Eff_MagicAttack],
            "Eff3_MagicAttack",
            position,
            null,
            "Effect")
        );
    }

    // 몬스터 버프 이펙트
    [ClientRpc]
    public void RpcEffectNormalMonsterBuff(Vector3 position)
    {
        StartCoroutine(StartEffect(
            normalMonsterEffects[Monster_Effect.Eff_Buff],
            "Eff04_Buff",
            position,
            M_SoundManager.instance.sfxClips[SFX_TYPE.Normal_Axe].Find((audioClip) => audioClip.name.Equals("monster_nor_axe_3")),
            "Effect")
        );
    }

    // 몬스터 방어 버프 이펙트
    [ClientRpc]
    public void RpcEffectNormalMonsterShield(Vector3 position)
    {
        StartCoroutine(StartEffect(
            normalMonsterEffects[Monster_Effect.Eff_Shield],
            "Eff05_Shield",
            position,
            M_SoundManager.instance.sfxClips[SFX_TYPE.Normal_Axe].Find((audioClip) => audioClip.name.Equals("monster_nor_axe_4_1")),
            "Effect")
        );
    }
}

public enum Monster_Effect {
    Eff_Bang,
    Eff_Buff,
    Eff_Cut,
    Eff_MagicAttack,
    Eff_Shield,
    Eff_Sting
}