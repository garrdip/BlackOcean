using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Boss_Geras : SpawnedMonster
{
    public override void OnStartClient()
    {
        base.OnStartClient();

        // 게라스 BGM 재생
        AudioClip momosBGM = M_SoundManager.instance.bgmClips[BGM_TYPE.Boss].Find((audioClip) => audioClip.name.Equals("Boss_Geras"));
        M_SoundManager.instance.PlayBGM(momosBGM, MusicTransition.Swift, 1.5f);
    }
}
