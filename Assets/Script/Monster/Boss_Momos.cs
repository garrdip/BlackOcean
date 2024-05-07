using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class Boss_Momos : SpawnedMonster
{
    public override void OnStartClient()
    {
        base.OnStartClient();

        // 모모스 BGM 재생
        AudioClip momosBGM = M_SoundManager.instance.bgmClips[BGM_TYPE.Boss].Find((audioClip) => audioClip.name.Equals("Boss_Momos"));
        M_SoundManager.instance.PlayBGM(momosBGM, MusicTransition.Swift, 1.5f);

        // 플레이어별 모모스 조우 대화 오디오클립 조회
        Character character = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.character;
        AudioClip playerVoice = null;
        AudioClip momosVoice = null;
        switch(character){
            case Character.HONGDANHYANG:
                momosVoice = M_SoundManager.instance.voiceClips[VOICE_TYPE.Momos][11];
                playerVoice = M_SoundManager.instance.voiceClips[VOICE_TYPE.HongDanHyang][157];
                break;
            case Character.GEORK:
                momosVoice = M_SoundManager.instance.voiceClips[VOICE_TYPE.Momos][12];
                playerVoice = M_SoundManager.instance.voiceClips[VOICE_TYPE.Geork][166];
                break;
            case Character.ERIS:
                int index = Random.Range(0, 1);
                momosVoice = M_SoundManager.instance.voiceClips[VOICE_TYPE.Momos][index == 0 ? 13 : 14];
                playerVoice = M_SoundManager.instance.voiceClips[VOICE_TYPE.Eris][index == 0 ? 217 : 218];
                // TODO : 에리스 공허상태인 경우 대화 분기 처리 
                break;
        }
        M_SoundManager.instance.PlayVoice(momosVoice, momosVoice.length, true, () => { // 모모스 대화 재생
            M_SoundManager.instance.PlayVoice(playerVoice, playerVoice.length); // 플레이어 대화 재생
        });
    }

    public override IEnumerator DoAction()
    {
        
        List<TargetObject> highlightTargetObjects = new List<TargetObject>();
        switch(turn)
        {
            case 0 :
                highlightTargetObjects.Add(transform.parent.GetComponent<TargetObject>());
                highlightTargetObjects.AddRange(M_TurnManager.instance.GetTargetObjectFromActionTargetList(nextTarget));
                M_DimmingManager.instance.StartDimming(highlightTargetObjects);
                DoAnimation("Attact0");
                yield return new WaitForSeconds(1f);
                GeneralAttack();
                yield return new WaitForSeconds(0.333f);
                M_DimmingManager.instance.StopDimming(highlightTargetObjects);
                break;
            case 1 :
                highlightTargetObjects.Add(transform.parent.GetComponent<TargetObject>());
                highlightTargetObjects.AddRange(M_TurnManager.instance.GetTargetObjectFromActionTargetList(nextTarget));
                DoAnimation("Attact1");
                yield return new WaitForSeconds(1f);
                GeneralAttack();
                yield return new WaitForSeconds(0.333f);
                M_DimmingManager.instance.StopDimming(highlightTargetObjects);
                break;
            case 2 :
                highlightTargetObjects.Add(transform.parent.GetComponent<TargetObject>());
                highlightTargetObjects.AddRange(M_TurnManager.instance.GetTargetObjectFromActionTargetList(nextTarget));
                DoAnimation("Buff0");
                yield return new WaitForSeconds(1f);
                GeneralAttack();
                yield return new WaitForSeconds(0.333f);
                M_DimmingManager.instance.StopDimming(highlightTargetObjects);
                break;
        }
        ReturnToIdleAnimation();
        yield return new WaitForSeconds(1f);
        turn ++;
        isActive = false;
    }
/*
    [Server]
    public override void GetNextAction()
    {
        if(turn < 2)
            nextAction = monsterData.behavior[0].ActionList[0];
        else
            nextAction = monsterData.behavior[1].ActionList[0];
    }
*/
    [ClientRpc]
    public void DoAnimation(string actionName)
    {
        parent.anim.state.SetAnimation(1,actionName,false);
        if(actionName.Equals("Attact0") || actionName.Equals("Attact1") ){
            List<AudioClip> attackVoices = new List<AudioClip>(M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Momos, 0, 7));
            int randomIndex = Random.Range(0, attackVoices.Count);
            AudioClip attacVoice = attackVoices[randomIndex];
            M_SoundManager.instance.PlayVoice(attacVoice, attacVoice.length);  // 공격 애니매이션 실행시 공격 음성중 하나 랜덤 재생
        }
    }

    [Server]
    public override IEnumerator OnHitAnimation()
    {
        OnHitAnimationRPC();
        yield return new WaitForSeconds(0.667f);
        ReturnToIdleAnimation();
    }

    [ClientRpc]
    public void OnHitAnimationRPC()
    {
        parent.anim.state.SetAnimation(1,"Defense0",false);
        List<AudioClip> hitVoices = new List<AudioClip>(M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Momos, 7, 4));
        int randomIndex = Random.Range(0, hitVoices.Count);
        AudioClip hitVoice = hitVoices[randomIndex];
        M_SoundManager.instance.PlayVoice(hitVoice, hitVoice.length);  // 피격 애니매이션 실행시 피격 음성중 하나 랜덤 재생
    }

    [ClientRpc]
    public override void ReturnToIdleAnimation()
    {
        parent.anim.state.SetAnimation(1,"Idle",true);
    }

    public override void OnChanedNextAction(MonsterAction oldVal, MonsterAction newVal)
    {
        if(newVal.actionName == "")return;
        Debug.Log("정상 입력");
        transform.parent.GetChild(3).localPosition = new Vector3(transform.parent.GetChild(3).localPosition.x, 0, transform.parent.GetChild(3).localPosition.z);
        if(nextAction.actionName == "Enrage")
            parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACKANDDEBUFF,true,newVal.actionTarget,100.ToString());
        else
            parent.nextActionIndicator.SetNextTargetAction(ActionType.ATTACK,true,newVal.actionTarget,100.ToString());
 
    }

    public override void OnChangedHpValue(int oldHpValue, int newHpValue)
    {
        base.OnChangedHpValue(oldHpValue, newHpValue);
        if(newHpValue <= 0){
            int index = Random.Range(0, 1) == 0 ? 45 : 46; // Moon_45, Moon_46중 랜덤재생
            AudioClip momosDeadVoice = M_SoundManager.instance.voiceClips[VOICE_TYPE.MoonGirl][index];
            M_SoundManager.instance.PlayVoice(momosDeadVoice, momosDeadVoice.length); // 모모스 사망시 달의소녀 나레이션 재생
        }
    }
}
