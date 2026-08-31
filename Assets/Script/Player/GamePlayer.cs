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
    /// <summary>레벨업 연출용 스탯 스냅샷 (RpcLevelUp 전/후 값 전달 — Mirror가 구조체 직렬화 자동 생성)</summary>
    public struct LevelUpStats
    {
        public int maxHP, str, agi, intel, def, mdef, ctrl;
        /// <summary>LevelUpPopUp 행 순서: HP / 힘 / 민첩 / 지능 / 방어력 / 마법방어 / 제어</summary>
        public int[] ToArray() => new[]{ maxHP, str, agi, intel, def, mdef, ctrl };
    }

    [SyncVar (hook = nameof(OnChangedLevel))]
    public int level = 1;      // 레벨
    [SyncVar]
    public int exp = 0;        // 현재 레벨에서 쌓은 경험치 (레벨업 시 필요치만큼 차감)
    [SyncVar]
    public int strength;       // 힘 — 물리 공격력
    [SyncVar]
    public int agility;        // 민첩 — TP(턴 게이지) 충전 속도
    [SyncVar]
    public int intelligence;   // 지능 — 마법 공격력
    [SyncVar]
    public int defense;        // 방어력
    [SyncVar]
    public int magicDefense;   // 마법방어
    [SyncVar]
    public int control;        // 제어 — MP 회복(매턴 제어/2, 전투 종료 후 제어)·분노 생성(변환제어 = 제어 + RAGE_CONTROL_OFFSET)에 영향 (RPG_CONVERSION_BATTLE)
    [SyncVar]
    public int growthSeed;     // 레벨업 성장치 랜덤 분배 시드 (LevelGrowthTable) — 생성 시 서버가 부여, 세이브에 보존. 레벨별 상승량은 시드마다 다르지만 만렙 총합은 CharacterStatDB GrowX로 동일

    // ---- 전투 자원 (게오르크: 분노 / 홍단향: MP / 에리스: HP 소모 — CharacterStatDB의 Resource) ----
    [SyncVar]
    public int currentResource; // 분노는 0에서 시작해 전투 중 충전, MP는 최대치로 시작
    [SyncVar]
    public int maxResource;     // HP형(에리스)은 0 — 자원 대신 자신의 HP를 소모한다

    // ---- 스킬트리 (SkillTreeDB — 습득/검증은 GamePlayer.SkillTree.cs) ----
    [SyncVar]
    public int skillPoints;     // 레벨업당 +3 (BalanceDB SKILL_POINTS_PER_LEVEL)
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
        LevelUpStats before = CaptureLevelUpStats(); // 연출용 — 레벨업 전 스탯 스냅샷
        while(required > 0 && exp >= required){ // required 0 = 최대 레벨
            exp -= required;
            level++;
            int points = BalanceData.Get("SKILL_POINTS_PER_LEVEL", 3); // 레벨업당 고정 3 포인트 (RPG_CONVERSION_SKILLS — 구 CharacterStatDB 방식 대체)
            skillPoints += points;
            gainedPoints += points;
            ApplyLevelUpGrowth();
            required = LevelData.GetRequiredExp(level);
        }
        if(required <= 0) exp = 0; // 최대 레벨 도달 — 잔여 경험치 정리
        if(level > startLevel) RpcLevelUp(startLevel, level, gainedPoints, before, CaptureLevelUpStats());
    }

    // 레벨업 연출용 스탯 스냅샷 (최대 HP + 7종 스탯)
    LevelUpStats CaptureLevelUpStats()
    {
        return new LevelUpStats{ maxHP = MaxHP, str = strength, agi = agility, intel = intelligence, def = defense, mdef = magicDefense, ctrl = control };
    }

    // 레벨업 알림 — 소유자 화면에 스탯 변화 팝업 (LevelUpPopUp). SyncVar보다 RPC가 먼저 도착하므로 전/후 값을 명시적으로 전달한다
    [ClientRpc]
    void RpcLevelUp(int fromLevel, int toLevel, int gainedPoints, LevelUpStats before, LevelUpStats after)
    {
        if(!isOwned) return;
        LevelUpPopUp.Show(character, fromLevel, toLevel, gainedPoints, before, after);
    }

    // 레벨업 1회분 성장치 반영 — 도달한 레벨(level)의 칸을 LevelGrowthTable(시드 기반 랜덤 분배)에서 읽는다.
    // 레벨마다 오르는 양은 growthSeed에 따라 다르지만 만렙까지의 합은 CharacterStatDB GrowX로 항상 같다.
    // 최대 HP가 오르면 늘어난 만큼 현재 HP도 회복시킨다
    [Server]
    private void ApplyLevelUpGrowth()
    {
        if(CharacterStatData.Get(character) == null) return;
        AddMaxHP(LevelGrowthTable.GetGrowth(growthSeed, character, LevelGrowthTable.Stat.HP, level));
        strength += LevelGrowthTable.GetGrowth(growthSeed, character, LevelGrowthTable.Stat.STR, level);
        agility += LevelGrowthTable.GetGrowth(growthSeed, character, LevelGrowthTable.Stat.AGI, level);
        intelligence += LevelGrowthTable.GetGrowth(growthSeed, character, LevelGrowthTable.Stat.INT, level);
        defense += LevelGrowthTable.GetGrowth(growthSeed, character, LevelGrowthTable.Stat.DEF, level);
        magicDefense += LevelGrowthTable.GetGrowth(growthSeed, character, LevelGrowthTable.Stat.MDEF, level);
        control += LevelGrowthTable.GetGrowth(growthSeed, character, LevelGrowthTable.Stat.CTRL, level);
        Debug.Log($"[GamePlayer] {character} 레벨업 → Lv.{level} (HP{MaxHP}/힘{strength}/민첩{agility}/지능{intelligence}/방어{defense}/마방{magicDefense}/제어{control})");
    }

    /// <summary>최대 HP 증가 — 레벨업 성장/스킬트리 HP 노드가 공유. 늘어난 만큼 현재 HP도 회복 (감소는 현재 HP를 최대치로 클램프)</summary>
    [Server]
    public void AddMaxHP(int amount)
    {
        if(amount == 0) return;
        MaxHP += amount;
        HP = amount > 0 ? HP + amount : Mathf.Min(HP, MaxHP);
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
