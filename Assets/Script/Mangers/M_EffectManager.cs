using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using AYellowpaper.SerializedCollections;

public class M_EffectManager : NetworkSingletonD<M_EffectManager>
{
    public SerializedDictionary<Card_Effect, GameObject> effects = new SerializedDictionary<Card_Effect, GameObject>(); 

    protected override void Start()
    {
        DontDestroyOnLoad(gameObject);
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.persistentManagers.Add(gameObject.name, gameObject);  
    }

    [ClientRpc]
    public void RpcEffectEatter(Vector3 position)
    {
        GameObject Effect = Instantiate(effects[Card_Effect.Effect_Eatter], position, Quaternion.identity);
        CardEffectBase cardEffectBase = Effect.GetComponent<CardEffectBase>();
        cardEffectBase.animationName = "EffEatter";
        cardEffectBase.sfx = M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][8];
    }

    [ClientRpc]
    public void RpcEffectClaw(Vector3 position)
    {
        GameObject Effect = Instantiate(effects[Card_Effect.Effect_Scratch], position, Quaternion.identity);
        CardEffectBase cardEffectBase = Effect.GetComponent<CardEffectBase>();
        cardEffectBase.animationName = "01EffScratch";
        cardEffectBase.sfx = M_SoundManager.instance.sfxClips[SFX_TYPE.Card_Danhyang][4];
    }
}

public enum Card_Effect {
    Effect_Eatter,
    Effect_Scratch
}
