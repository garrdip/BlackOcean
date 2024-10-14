using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;

// 게오르크 카드 이펙트용 클래스 : M_EffectManager의 partial 클래스
public partial class M_EffectManager
{
    public List<ParticleSystem> georkCardEffectParticles = new List<ParticleSystem>();

    [ClientRpc]
    public void RpcEffectSwordSlash(Vector3 position, bool isRandom)
    {
        float[] angles = { 40f, 75f, -40f, -75f, 90f };
        float randomAngle = angles[Random.Range(0, angles.Length)];

        ParticleSystem particleSystem = Instantiate(georkCardEffectParticles[0], position + new Vector3(0f, 3f, 0f), Quaternion.identity);
        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "Effect";
        ParticleSystem.MainModule mainModule = particleSystem.main;
        mainModule.startRotation = isRandom ? Mathf.Deg2Rad * randomAngle : Mathf.Deg2Rad * 40f;
        
        ParticleSystem subEmitter = particleSystem.subEmitters.GetSubEmitterSystem(0);
        ParticleSystemRenderer subRenderer = subEmitter.GetComponent<ParticleSystemRenderer>();
        subRenderer.sortingLayerName = "Effect";
        ParticleSystem.MainModule subModule = subEmitter.main;
        subModule.startRotation = isRandom ? Mathf.Deg2Rad * randomAngle : Mathf.Deg2Rad * 40f;
    }


    [ClientRpc]
    public void RpcEffectSwordSting(Vector3 position)
    {
        ParticleSystem particleSystem = Instantiate(georkCardEffectParticles[0], position + new Vector3(0f, 3f, 0f), Quaternion.identity);
        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "Effect";
        ParticleSystem.MainModule mainModule = particleSystem.main;
        mainModule.startColor = ColorUtils.HexToColor("#0087F1");
        mainModule.startRotation = Mathf.Deg2Rad * 90f;

        ParticleSystem subEmitter = particleSystem.subEmitters.GetSubEmitterSystem(0);
        ParticleSystemRenderer subRenderer = subEmitter.GetComponent<ParticleSystemRenderer>();
        subRenderer.sortingLayerName = "Effect";
        ParticleSystem.MainModule subModule = subEmitter.main;
        subModule.startColor = ColorUtils.HexToColor("#00F1FF");
        subModule.startRotation = Mathf.Deg2Rad * 90f;
    }
}
