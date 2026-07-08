using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using Spine.Unity;
using Spine.Unity.Examples;
using Gpm.Ui;
using AYellowpaper.SerializedCollections;
using System.Linq;


// M_TurnManager partial — 철귀(홍단향 소환수) 전용 연출/이동 로직
public partial class M_TurnManager
{

    public IEnumerator IronDemonReturnProcess(TargetObject target)
    {
        M_TurnManager.instance.AnimIronDemon("TeleportGo",target); // 철귀 사라짐
        yield return new WaitForSeconds(0.333f); // 철귀 완전히 사라지는 시간
        M_TurnManager.instance.MoveIronDemon(target,target); // 철귀 적으로 이동
        M_TurnManager.instance.AnimIronDemon("TeleportBack",target); // 철귀 나타나기 시작
        yield return new WaitForSeconds(0.2f); // 적당히 나타날때까지 기다림
        M_TurnManager.instance.AnimIronDemon("Idle",target); // 철귀 나타나기 시작
    }


    [ClientRpc]
    public void MoveIronDemon(TargetObject tar, TargetObject target)
    {
        if(tar != null && target != null){
            tar.ironDemon.GetComponent<SkeletonRenderTexture>().enabled = false;
            if(target.objectType == ObjectType.PLAYER){
                tar.ironDemon.GetComponent<MeshRenderer>().sortingOrder = target.avatar.GetComponent<MeshRenderer>().sortingOrder - 1;
            }else{
                tar.ironDemon.GetComponent<MeshRenderer>().sortingOrder = -1;
            }
            tar.ironDemon.GetComponent<SkeletonAnimation>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            int transformOffset = CalcOffset(tar); 
            if(target.monster != null)
                if(target.monster.monsterName == "Boss_Momos") // 모모스 키 적용 TODO: 몬스터 키적용 코드 추가
                    tar.ironDemon.transform.position = target.transform.position + new Vector3(transformOffset,5,0);
                else
                    tar.ironDemon.transform.position = target.transform.position + new Vector3(transformOffset,0,0);
            else
                tar.ironDemon.transform.position = target.transform.position + new Vector3(transformOffset,0,0);
            if(target.objectType == ObjectType.PLAYER) tar.ironDemon.GetComponent<SkeletonAnimation>().skeletonDataAsset = tar.ironDemonData[0];
            else tar.ironDemon.GetComponent<SkeletonAnimation>().skeletonDataAsset = tar.ironDemonData[1];
            tar.ironDemon.GetComponent<SkeletonAnimation>().Initialize(true);
            tar.ironDemon.GetComponent<SkeletonRenderTexture>().enabled = true;
        }
    }


    // 영웅능력으로 철귀 이동 시 음성 재생
    [ClientRpc]
    public void PlayIronDemonCommandVoice(TargetObject tar, TargetObject target)
    {
        if(target.player != null){
            if(target.player.objectOwner.isLocalPlayer){
                AudioClip abilitySound = M_SoundManager.instance.GetVoiceClipAt(VOICE_TYPE.HongDanHyang, 55); // 이리 오거라
                M_SoundManager.instance.PlayVoice(abilitySound, abilitySound.length);
            }else{
                List<AudioClip> clips = new List<AudioClip>();
                AudioClip abilitySound1 = M_SoundManager.instance.GetVoiceClipAt(VOICE_TYPE.HongDanHyang, 53); // 도와 주거라
                AudioClip abilitySound2 = M_SoundManager.instance.GetVoiceClipAt(VOICE_TYPE.HongDanHyang, 56); // 저리 가주거라
                clips.Add(abilitySound1);
                clips.Add(abilitySound2);
                AudioClip abilitySound = clips[Random.Range(0, clips.Count)];
                M_SoundManager.instance.PlayVoice(abilitySound, abilitySound.length);
            }
        }else{
            List<AudioClip> clips = new List<AudioClip>();
            AudioClip abilitySound1 = M_SoundManager.instance.GetVoiceClipAt(VOICE_TYPE.HongDanHyang, 52); // 부탁하마
            AudioClip abilitySound2 = M_SoundManager.instance.GetVoiceClipAt(VOICE_TYPE.HongDanHyang, 54); // 융융아 가거라
            AudioClip abilitySound3 = M_SoundManager.instance.GetVoiceClipAt(VOICE_TYPE.HongDanHyang, 57); // 물어 뜯어 주거라
            clips.Add(abilitySound1);
            clips.Add(abilitySound2);
            clips.Add(abilitySound3);
            AudioClip abilitySound = clips[Random.Range(0, clips.Count)];
            M_SoundManager.instance.PlayVoice(abilitySound, abilitySound.length);
        }
    }


    int CalcOffset(TargetObject tar)
    {
        int retVal = 0;
        if(tar.player == (NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer)) retVal = 0;
        else
        {
            int addval = 0;
            foreach(uint netId in playerOrder){
                if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                    if(tar.player == networkIdentity.GetComponent<GamePlayer>())
                        break;
                    if(tar.player == (NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer))
                        continue;
                    else
                        addval++;
                }
            }
            if(addval == 0) retVal = -1;
            else retVal = 1;
        }
        return retVal;
    }


    [ClientRpc]
    public void AnimIronDemon(string anim ,TargetObject tar)
    {
        if(tar != null){
            bool isLoop = anim == "Idle" ? true : false;
            if(anim == "TeleportBack")tar.ironDemon.GetComponent<SkeletonAnimation>().maskInteraction = SpriteMaskInteraction.None;
            tar.ironDemon.GetComponent<SkeletonAnimation>().state.SetAnimation(0,anim,isLoop);
            tar.ApllyIronDemonAnimationCallbackFunction();
        }
    }
}
