using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;


public partial class GamePlayer : NetworkBehaviour
{
    public delegate void OnChangePlayerOrder(int order);
    public OnChangePlayerOrder onChangePlayerOrder;

    public delegate void OnChangeGold(int gold);
    public OnChangeGold onChangeGold;

    public GamePlayerDeck gamePlayerDeck; // GamePlayerDeck 참조값 (인스펙터 할당)

    [SyncVar (hook = nameof(OnChangePlayerGold))]
    public int gold = 0; // 현재 플레이어 보유 골드

    [SyncVar (hook = nameof(OnChangHpValue))]
    public int HP; // 체력
    
    [SyncVar]
    public int MaxHP; // 최대 체력

    [SyncVar]
    public int recoveryValue; // 체력 회복 수치

    [SyncVar]
    public int recoveryLimitCount; // 체력 회복 횟수 제한

    // ---- RPG 스탯 (CharacterStatDB 기반, 레벨업으로 성장) ----
    [SyncVar (hook = nameof(OnChangedLevel))]
    public int level = 1;      // 레벨
    [SyncVar]
    public int exp = 0;        // 현재 레벨에서 쌓은 경험치 (레벨업 시 필요치만큼 차감)
    [SyncVar]
    public int strength;       // 힘 — 물리 공격력
    [SyncVar]
    public int agility;        // 민첩 — TP(턴 게이지) 충전 속도
    [SyncVar]
    public int vitality;       // 체력 — 최대 HP 결정 (MaxHP = PLAYER_INIT_HP + vitality * HP_PER_VITALITY)
    [SyncVar]
    public int intelligence;   // 지능 — 마법 공격력
    [SyncVar]
    public int defense;        // 방어력
    [SyncVar]
    public int magicDefense;   // 마법방어

    // ---- 전투 자원 (게오르크: 분노 / 홍단향: MP / 에리스: HP 소모 — CharacterStatDB의 Resource) ----
    [SyncVar]
    public int currentResource; // 분노는 0에서 시작해 전투 중 충전, MP는 최대치로 시작
    [SyncVar]
    public int maxResource;     // HP형(에리스)은 0 — 자원 대신 자신의 HP를 소모한다

    // ---- 스킬트리 (SkillTreeDB — 습득/검증은 GamePlayer.SkillTree.cs) ----
    [SyncVar]
    public int skillPoints;     // 레벨업당 +1
    public readonly SyncList<string> learnedNodes = new SyncList<string>(); // 습득한 트리 노드 id 목록

    [SyncVar (hook = nameof(OnChangedObjectOwner))]
    public PlayerInterface objectOwner;

    [SyncVar (hook = nameof(OnChangedSelectOrder))]
    public int selectOrder = 0;

    [SyncVar]
    public Character character;

    public ParticleSystem recoverParticle; // 체력 회복 파티클 이펙트

    public bool isSelectable = false; // CharacterSelector 클래스에서 사용되는 플래그 변수(캐릭터 오브젝트의 마우스 오버 및 클릭 가능 상태 변경 용도)

    public override void OnStartServer()
    {
        base.OnStartServer();

        if(M_SaveManager.instance.isSaveGame)
        {
            foreach(SaveDataPlayer saveDataPlayer in M_SaveManager.instance.loadData.players)
            {
                if(saveDataPlayer == null || !saveDataPlayer.isActive)continue; // JSON 역직렬화 시 빈 슬롯은 기본 인스턴스로 채워짐
                if(saveDataPlayer.ownerSteamId == objectOwner.steamID)
                {
                    HP = saveDataPlayer.HP;
                    MaxHP = saveDataPlayer.MaxHP;
                }
            }
        }
    }

    // ------------------------------------------------------------- Command Method ------------------------------------------------------------------//
    
    [Command]
    public void CmdHpRecovery(uint targetPlayerNetId)
    {
        if(NetworkServer.spawned.TryGetValue(targetPlayerNetId, out NetworkIdentity networkIdentity)){
            GamePlayer targetPlayer = networkIdentity.GetComponent<GamePlayer>();
            if(recoveryLimitCount <= 0){
                TargetErrorMessage(Const.ERR_RECOVERY_COUNT_LIMITED);
                return;
            }
            TargetObject targetObject = M_TurnManager.instance.GetCurrentPlayerTargetObject(targetPlayer);
            if(targetObject != null && targetObject.player != null){
                targetObject.playerHP += recoveryValue; // 전투 아바타가 있으면 아바타 HP 경유 (GamePlayer.HP로 동기화됨)
            }else{
                targetPlayer.HP = Mathf.Min(targetPlayer.HP + recoveryValue, targetPlayer.MaxHP); // 거점(아바타 없음): GamePlayer HP 직접 회복
            }
            recoveryLimitCount--;
            RpcHpRecovery(targetPlayerNetId);
        }
    }

    [Command]
    public void CmdAddGoldValue(uint targetPlayerNetId, int giveGold)
    {
        if(giveGold <= 0) return; // 음수/0 전달 방어 (역방향 갈취 방지)
        if(NetworkServer.spawned.TryGetValue(targetPlayerNetId, out NetworkIdentity networkIdentity)){
            GamePlayer targetPlayer = networkIdentity.GetComponent<GamePlayer>();
            // 자기 자신의 netId는 클라가 보낸 값 대신 서버가 아는 이 오브젝트의 netId를 사용 (위장 방지)
            if(targetPlayerNetId == netId){
                TargetErrorMessage(Const.ERR_DENIED_GIVE_GOLD_LOCAL_PLAYER);
            }else{
                int resultGold = gold - giveGold;
                if((resultGold >= 0) && (gold >= resultGold)){
                    gold -= giveGold; // 요청한 플레이어의 골드 감소
                    targetPlayer.gold += giveGold; // 전달한 플레이어의 골드 증가
                    RpcGetGold(targetPlayerNetId, giveGold);
                }else{
                    TargetErrorMessage(Const.ERR_NOT_ENOUGH_GOLD);
                }
            }
        }
    }

    // ------------------------------------------------------------- Server Method --------------------------------------------------------------------//

    /// <summary>경험치 지급 + 레벨업 처리. 필요치(LevelDB)를 넘길 때마다 레벨업하며 캐릭터 성장치(CharacterStatDB)를 반영한다</summary>
    [Server]
    public void AddExp(int amount)
    {
        if(amount <= 0) return;
        int required = LevelData.GetRequiredExp(level);
        if(required <= 0) return; // 최대 레벨(LevelDB RequiredExp 0) — 더 이상 쌓지 않음
        exp += amount;
        int startLevel = level;
        int gainedPoints = 0;
        CharacterStatData.Entry stat = CharacterStatData.Get(character);
        while(required > 0 && exp >= required){ // required 0 = 최대 레벨
            exp -= required;
            level++;
            int points = stat != null ? stat.GetSkillPointsForLevel(level) : 1; // 레벨업당 스킬 포인트 + N레벨 보너스 (CharacterStatDB)
            skillPoints += points;
            gainedPoints += points;
            ApplyLevelUpGrowth();
            required = LevelData.GetRequiredExp(level);
        }
        if(required <= 0) exp = 0; // 최대 레벨 도달 — 잔여 경험치 정리
        if(level > startLevel) RpcLevelUp(startLevel, level, gainedPoints);
    }

    // 레벨업 알림 — 소유자 화면에 토스트
    [ClientRpc]
    void RpcLevelUp(int fromLevel, int toLevel, int gainedPoints)
    {
        if(!isOwned) return;
        string text = M_LanguageManager.Get("ui.msg.level_up", "레벨 업! Lv.{0} → Lv.{1} (스킬 포인트 +{2})")
            .Replace("{0}", fromLevel.ToString()).Replace("{1}", toLevel.ToString()).Replace("{2}", gainedPoints.ToString());
        M_MessageManager.instance
            .MakeToast()
            .Position(ToastPosition.Top)
            .MessageBoxColor(ProjectD.ColorUtils.HexToColor("#DAA520"))
            .TextColor(Color.white)
            .Text(text)
            .FadeInTime(1f)
            .FadeOutTime(1.5f)
            .Show();
    }

    // 레벨업 1회분 성장치 반영. 체력 스탯이 오르면 최대 HP를 다시 계산하고 늘어난 만큼 현재 HP도 회복시킨다
    [Server]
    private void ApplyLevelUpGrowth()
    {
        CharacterStatData.Entry stat = CharacterStatData.Get(character);
        if(stat == null) return;
        strength += stat.growStr;
        agility += stat.growAgi;
        vitality += stat.growVit;
        intelligence += stat.growInt;
        defense += stat.growDef;
        magicDefense += stat.growMdef;

        int newMaxHP = GetMaxHPByVitality(vitality);
        HP += Mathf.Max(0, newMaxHP - MaxHP);
        MaxHP = newMaxHP;
        Debug.Log($"[GamePlayer] {character} 레벨업 → Lv.{level} (힘{strength}/민첩{agility}/체력{vitality}/지능{intelligence}/방어{defense}/마방{magicDefense}, MaxHP {MaxHP})");
    }

    /// <summary>체력 스탯 기준 최대 HP 계산식 — 초기화(PlayerInterface)와 레벨업이 공유한다</summary>
    public static int GetMaxHPByVitality(int vitality)
    {
        return BalanceData.Get("PLAYER_INIT_HP", 50) + vitality * BalanceData.Get("HP_PER_VITALITY", 2);
    }

    [Server]
    public void SetPlayerOrder(int num)
    {
        SetPlayerOrderRPC(num);
    }

    [Server]
    public void CheckAllPlayersIsDead()
    {
        int gamePlayerCount = M_TurnManager.instance.playerOrder.FindAll((netId) => netId != 0).Count; // 현재 게임에 참가한 플레이어 수
        int deadPlayerCount = M_TurnManager.instance.playerOrder.FindAll((netId) => netId != 0 && IsPlayerHpZero(netId) && !CanErisRevive(netId)).Count; // HP가 0이고 광기 변신으로 사망을 회피할 수 없는 플레이어 수
        if(deadPlayerCount == gamePlayerCount){
            RpcGameOver();
        }
    }

    // netId로 GamePlayer 조회하여 HP가 0 이하면 trun, 아니면 false 반환
    [Server]
    private bool IsPlayerHpZero(uint netId)
    {
        if(NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
           return networkIdentity.GetComponent<GamePlayer>().HP <= 0;
        }
        return false;
    }

    // 에리스가 아직 광기 변신으로 사망을 회피할 수 있는 상태면 true — 광기(MAD) 상태에서 죽었거나 타겟오브젝트가 이미 소멸했으면 false (전멸 집계에 포함)
    [Server]
    private bool CanErisRevive(uint netId)
    {
        if(NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
            GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
            if(gamePlayer.character != Character.ERIS) return false;
            TargetObject targetObject = M_TurnManager.instance.GetCurrentPlayerTargetObject(gamePlayer);
            return targetObject != null && targetObject.erisMode != ErisMode.MAD;
        }
        return false;
    }

    // ---------------------------------------------------------------- ClientRpc Method -------------------------------------------------------------//

    [ClientRpc]
    void SetPlayerOrderRPC(int num)
    {
        if(isLocalPlayer)
        {
            selectOrder = num;
        }
    }

    [ClientRpc]
    public void RpcGameOver()
    {
        PopUpUIManager.instance.HandleShowGameOverPopUp();
    }

    [ClientRpc]
    public void RpcHpRecovery(uint targetPlayerNetId)
    { 
        if(NetworkClient.spawned.TryGetValue(targetPlayerNetId, out NetworkIdentity networkIdentity)){
            GamePlayer targetPlayer = networkIdentity.GetComponent<GamePlayer>();
            TargetObject targetObject = M_TurnManager.instance.GetCurrentPlayerTargetObject(targetPlayer);
            if(targetObject != null){ // 거점(아바타 없음)에서는 파티클 생략
                ParticleSystem particleSystem = Instantiate(recoverParticle, targetObject.transform.position, Quaternion.identity);
                ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                renderer.sortingLayerName = "Effect";
            }
            if(targetPlayer.isOwned){
                M_MessageManager.instance
                    .MakeToast()
                    .Position(ToastPosition.Bottom)
                    .MessageBoxColor(Color.green)
                    .TextColor(Color.white)
                    .Text(M_LanguageManager.Get("ui.msg.hub_healed", "체력을 회복했습니다."))
                    .FadeInTime(1f)
                    .FadeOutTime(1f)
                    .Show();
            }
        }
    }

    [ClientRpc]
    public void RpcGetGold(uint targetPlayerNetId, int giveGold)
    {
        if(NetworkClient.spawned.TryGetValue(targetPlayerNetId, out NetworkIdentity networkIdentity)){
            GamePlayer targetPlayer = networkIdentity.GetComponent<GamePlayer>();
            TargetObject targetObject = M_TurnManager.instance.GetCurrentPlayerTargetObject(targetPlayer);
            M_EffectManager.instance.DisplayGold(targetObject, giveGold);
        }
    }

    // 커맨드 요청한 플레이어에게 전달되는 오류 메시지 수신
    [TargetRpc]
    public void TargetErrorMessage(string err)
    {
        M_MessageManager.instance
            .MakeToast()
            .Position(ToastPosition.Bottom)
            .MessageBoxColor(Color.red)
            .TextColor(Color.white)
            .Text(err)
            .FadeInTime(2f)
            .FadeOutTime(2f)
            .Show();
    }

    // ---------------------------------------------------------------- SyncVar Hook Method ----------------------------------------------------------//

    public void OnChangePlayerGold(int oldVal, int newVal)
    {
        onChangeGold?.Invoke(newVal);
    }

    public void OnChangHpValue(int oldVal, int newVal)
    {
        if(isServer){
            CheckAllPlayersIsDead();
        }
    }

    public void OnChangedObjectOwner(PlayerInterface oldVal, PlayerInterface newVal)
    {
        transform.SetParent(newVal.transform);
    }

    public void OnChangedSelectOrder(int oldVal,int newVal)
    {
        onChangePlayerOrder?.Invoke(newVal);
    }

    public void OnChangedLevel(int oldVal, int newVal)
    {
        if(oldVal < newVal && oldVal > 0){ // 초기 동기화(0→1)는 제외하고 실제 레벨업만 로그
            Debug.Log($"[GamePlayer] {character} 레벨 {oldVal} → {newVal}");
        }
    }
}
