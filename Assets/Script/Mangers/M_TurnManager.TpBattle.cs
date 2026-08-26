using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;

// M_TurnManager partial — TP 기반 턴제 커맨드 전투 (RPG 전환 수직 슬라이스).
//
// 규칙 (RPG_CONVERSION_DESIGN):
// - 유닛별 TP가 민첩에 비례해 차오르고 100이 되면 그 유닛의 턴 (다회 턴 상한 없음 — 민첩 수치로 밸런싱)
// - 1턴 1액션: 공격 / 방어 / 스킬 / 아이템 / 이동(전열·중열·후열)
// - 약점 속성 피격 시 피해 증폭 + 대상 TP 감소 (반복 시 효과 체감)
//
// 진입: BattleInitialize()가 USE_TP_BATTLE이면 카드 페이즈 대신 StartTpBattle() 호출.
// 종료: 몬스터 전멸 시 기존 OnChangedMonsterList → BATTLE_END → 보상/맵 복귀 파이프라인 재사용.
// UI: 정식 전투 UI 전까지 OnGUI 임시 액션 바 사용 (프로젝트의 디버그 UI 관례).
public partial class M_TurnManager
{
    class TpUnit
    {
        public TargetObject target;
        public float tp;
    }

    readonly List<TpUnit> tpUnits = new List<TpUnit>();
    readonly Dictionary<TargetObject, int> tpWeaknessHits = new Dictionary<TargetObject, int>(); // 대상별 약점 피격 횟수 (TP 브레이크 체감용)

    // 지속 턴 디버프 (쇠락 등 — 서버 전용). 대상의 턴이 끝날 때마다 남은 턴이 줄고, 0이 되면 버프를 상쇄 제거한다.
    // 버프 규칙 (RPG_CONVERSION_SKILLS): 중첩되지 않으며 지속 턴만 갱신, 효과는 강한 쪽으로 남는다
    class TpTimedDebuff
    {
        public TargetObject target;
        public BuffType type;
        public int value;
        public int turnsLeft;
    }
    readonly List<TpTimedDebuff> tpTimedDebuffs = new List<TpTimedDebuff>();

    [SyncVar]
    public bool tpBattleActive;

    [SyncVar]
    public uint tpCurrentUnitNetId; // 액션 입력을 기다리는 플레이어 유닛(TargetObject)의 netId. 0 = 대기 없음

    /// <summary>클라이언트 표시용 TP 스냅샷 (유닛 netId + 현재 TP). 게이지 진행/턴/브레이크 시점마다 서버가 갱신</summary>
    public struct TpGaugeSnapshot
    {
        public uint netId;
        public int tp;
    }
    public readonly SyncList<TpGaugeSnapshot> tpGauges = new SyncList<TpGaugeSnapshot>();

    [Tooltip("유닛 머리 위 TP 숫자 표시 높이 (월드 단위)")]
    public float tpLabelHeight = 3.5f;

    bool tpActionSubmitted;                                        // 서버: 현재 턴 액션 접수 플래그
    (TpAction action, string skillNo, uint targetNetId, int row) tpSubmittedAction;

    public static bool UseTpBattle => BalanceData.Get("USE_TP_BATTLE", 1) == 1;

    // ------------------------------------------------------------- 서버: 전투 루프 -------------------------------------------------------------//

    [Server]
    public void StartTpBattle()
    {
        StartCoroutine(TpBattleLoop());
    }

    [Server]
    IEnumerator TpBattleLoop()
    {
        tpBattleActive = true;
        tpCurrentUnitNetId = 0;
        tpUnits.Clear();
        tpWeaknessHits.Clear();
        tpTimedDebuffs.Clear();

        foreach (TargetObject player in spawnedPlayerList)
        {
            tpUnits.Add(new TpUnit { target = player, tp = GetUnitTpGain(player) }); // 1회 획득량만큼 선충전 (첫 턴 순서에 반영)
            // 분노(게오르크)는 매 전투 0에서 시작 — 전투 중 피해를 주고받으며 충전
            CharacterStatData.Entry stat = CharacterStatData.Get(player.player.character);
            if (stat != null && stat.resource == BattleResourceType.RAGE)
                player.player.currentResource = 0;
        }
        foreach (TargetObject monster in spawnedMonsterList)
        {
            if (monster.objectType != ObjectType.ENEMY) continue; // NPC 제외
            tpUnits.Add(new TpUnit { target = monster, tp = GetUnitTpGain(monster) });
            monster.monster.SetNextAction(); // 행동 예고 표시
        }
        SyncTpGauges();

        WaitForSeconds wait = new WaitForSeconds(0.05f);
        while (true)
        {
            // 전투 종료 (몬스터 전멸 시 OnChangedMonsterList가 BATTLE_END로 전환)
            if (phase == BattleTurn.BATTLE_END || phase == BattleTurn.NONE_BATTLE_END)
                break;
            tpUnits.RemoveAll(unit => unit.target == null || !IsUnitAlive(unit.target));
            if (monsterDeathOperating
                || !tpUnits.Exists(unit => unit.target.objectType == ObjectType.ENEMY)
                || !tpUnits.Exists(unit => unit.target.objectType == ObjectType.PLAYER))
            {
                yield return wait; // 사망 정리 중이거나 한쪽 전멸 → 종료 판정(BATTLE_END/RpcGameOver)을 기다린다
                continue;
            }

            // TP 게이지 진행 — 가장 먼저 100에 도달하는 유닛까지의 시간만큼 일괄 충전.
            // 획득량 = TP_GAIN_BASE(오프셋, 비공개) + 민첩 (RPG_CONVERSION_BATTLE)
            TpUnit next = null;
            float minTime = float.MaxValue;
            foreach (TpUnit unit in tpUnits)
            {
                float time = (100f - unit.tp) / Mathf.Max(1, GetUnitTpGain(unit.target));
                if (time < minTime)
                {
                    minTime = time;
                    next = unit;
                }
            }
            if (minTime > 0f)
                foreach (TpUnit unit in tpUnits)
                    unit.tp += GetUnitTpGain(unit.target) * minTime;

            // 턴 실행 — 넘친 TP는 이월된다 (민첩이 높으면 자연히 다회 턴)
            next.tp -= 100f;
            next.target.defense = 0; // 방어(실드)는 자기 다음 턴까지 유지 — 자기 턴 시작에 리셋
            SyncTpGauges();
            if (next.target.objectType == ObjectType.PLAYER)
                yield return ExecutePlayerTpTurn(next);
            else
                yield return ExecuteMonsterTpTurn(next);
            SyncTpGauges(); // 턴 중 TP 변동(브레이크/피의 가속/필살기 페널티) 반영

            yield return wait;
        }

        // 전투 종료 후 MP 회복 — 제어만큼 (RPG_CONVERSION_BATTLE MP 회복 공식)
        foreach (TargetObject playerTarget in spawnedPlayerList)
        {
            if (playerTarget == null || playerTarget.player == null) continue;
            CharacterStatData.Entry endStat = CharacterStatData.Get(playerTarget.player.character);
            if (endStat != null && endStat.resource == BattleResourceType.MP)
                playerTarget.player.currentResource = Mathf.Min(playerTarget.player.maxResource,
                    playerTarget.player.currentResource + playerTarget.player.control);
        }

        tpBattleActive = false;
        tpCurrentUnitNetId = 0;
        tpGauges.Clear();
    }

    // 서버 → 클라이언트 TP 표시 동기화 (유닛 수가 적어 전체 재작성으로 충분)
    [Server]
    void SyncTpGauges()
    {
        tpGauges.Clear();
        foreach (TpUnit unit in tpUnits)
        {
            if (unit.target == null) continue;
            tpGauges.Add(new TpGaugeSnapshot { netId = unit.target.netId, tp = Mathf.RoundToInt(unit.tp) });
        }
    }

    [Server]
    IEnumerator ExecuteMonsterTpTurn(TpUnit unit)
    {
        SpawnedMonster monster = unit.target.monster;
        if (monster == null) yield break;
        monster.isActive = true;
        StartCoroutine(monster.DoAction()); // 기존 몬스터 AI 그대로 (MonsterActionSeuqence와 동일한 대기 규약)
        while (monster != null && monster.isActive)
            yield return null;
        TickTimedDebuffs(unit.target); // 쇠락 등 지속 디버프 턴 감소 (대상의 턴 종료 기준)
        if (monster != null && IsUnitAlive(unit.target))
            monster.SetNextAction(); // 다음 행동 예고 갱신
    }

    /// <summary>지속 턴 디버프 부여 (쇠락 등) — 같은 종류가 있으면 지속 턴만 갱신하고 효과는 강한 쪽으로 유지</summary>
    [Server]
    public void ApplyTimedDebuffTo(TargetObject target, BuffType type, int value, int turns, TargetObject from)
    {
        if (target == null || turns <= 0) return;
        TpTimedDebuff existing = tpTimedDebuffs.Find(debuff => debuff.target == target && debuff.type == type);
        if (existing != null)
        {
            existing.turnsLeft = Mathf.Max(existing.turnsLeft, turns);
            if (Mathf.Abs(value) > Mathf.Abs(existing.value))
            {
                target.GainBuff(type, value - existing.value, true, false, false, false, from, null); // 강한 효과로 교체 (차액 반영)
                existing.value = value;
            }
            return;
        }
        target.GainBuff(type, value, true, false, false, false, from, null);
        tpTimedDebuffs.Add(new TpTimedDebuff { target = target, type = type, value = value, turnsLeft = turns });
    }

    // 대상의 턴 종료 시 지속 디버프 감소 — 만료되면 버프를 상쇄 제거
    [Server]
    void TickTimedDebuffs(TargetObject target)
    {
        for (int i = tpTimedDebuffs.Count - 1; i >= 0; i--)
        {
            TpTimedDebuff debuff = tpTimedDebuffs[i];
            if (debuff.target != target) continue;
            if (--debuff.turnsLeft > 0) continue;
            if (debuff.target != null && !debuff.target.isDying)
                debuff.target.GainBuff(debuff.type, -debuff.value, true, false, false, false, debuff.target, null); // 상쇄 → 값 0이 되며 제거
            tpTimedDebuffs.RemoveAt(i);
        }
    }

    [Server]
    IEnumerator ExecutePlayerTpTurn(TpUnit unit)
    {
        // MP(홍단향) 자기 턴 시작 회복 = 제어/2 (RPG_CONVERSION_BATTLE MP 회복 공식 — 전투 종료 후에는 제어 전량)
        CharacterStatData.Entry statEntry = CharacterStatData.Get(unit.target.player.character);
        if (statEntry != null && statEntry.resource == BattleResourceType.MP)
            unit.target.player.currentResource = Mathf.Min(unit.target.player.maxResource,
                unit.target.player.currentResource + unit.target.player.control / Mathf.Max(1, BalanceData.Get("MP_REGEN_CONTROL_DIVISOR", 2)));

        tpActionSubmitted = false;
        tpCurrentUnitNetId = unit.target.netId;
        while (!tpActionSubmitted)
        {
            if (unit.target == null || !IsUnitAlive(unit.target) || phase == BattleTurn.BATTLE_END)
            {
                tpCurrentUnitNetId = 0;
                yield break; // 입력 대기 중 사망/전투 종료 → 턴 스킵
            }
            yield return null;
        }
        tpCurrentUnitNetId = 0;

        (TpAction action, string skillNo, uint targetNetId, int row) = tpSubmittedAction;
        GamePlayer gamePlayer = unit.target.player;
        switch (action)
        {
            case TpAction.ATTACK:
            {
                TargetObject target = ResolveEnemyTarget(targetNetId);
                if (target != null)
                {
                    CharacterStatData.Entry stat = CharacterStatData.Get(gamePlayer.character);
                    AttackAttribute attribute = stat != null ? stat.basicAttack : AttackAttribute.NONE;
                    yield return BattleActions.AttackTarget(unit.target, target, BattleActions.BasicAttackDamage(unit.target), attribute);
                }
                break;
            }
            case TpAction.DEFEND:
            {
                // 방어: 방어력 스탯(장비 합산) + 기본치만큼 실드 획득 (자기 다음 턴 시작까지 유지)
                // ※실드가 깎일 때는 방어력 공식 미적용 — 대열 보정된 원 데미지 그대로 소모된다 (TargetObject.DamageToPlayer)
                unit.target.GainDefense(gamePlayer.TotalDefense + BalanceData.Get("DEFEND_BASE_VALUE", 5));
                break;
            }
            case TpAction.SKILL:
            {
                SkillData.SkillDef skill = SkillData.Get(skillNo);
                if (skill != null && gamePlayer.KnowsSkill(skillNo) && PayCost(unit.target, skill)) // 스킬트리 습득 여부 서버 검증
                    yield return skill.execute(skill, unit.target, ResolveSkillTargets(skill, targetNetId));
                break;
            }
            case TpAction.MOVE:
            {
                int myIndex = playerOrder.IndexOf(gamePlayer.netId);
                if (myIndex >= 0 && row >= 0 && row < playerOrder.Count && myIndex != row)
                    SwapPlayerOrder(myIndex, row);
                break;
            }
            case TpAction.ITEM:
            {
                // 소모품 사용 — skillNo 파라미터에 PotionNo를 실어 보낸다
                ConsumableData.Def potion = ConsumableData.Get(skillNo);
                if (potion != null && gamePlayer.ServerConsumePotion(skillNo))
                {
                    switch (potion.type)
                    {
                        case ConsumableType.HEAL_HP:
                            unit.target.HealPlayer(potion.value);
                            break;
                        case ConsumableType.RESTORE_RESOURCE:
                            gamePlayer.currentResource = Mathf.Min(gamePlayer.maxResource, gamePlayer.currentResource + potion.value);
                            break;
                    }
                    yield return new WaitForSeconds(0.3f);
                }
                break;
            }
        }
    }

    // 스킬 코스트 지불. 실패(자원 부족) 시 false — 액션은 소모되지 않은 것으로 하고 싶지만
    // 슬라이스에서는 이미 턴이 확정된 뒤라 아무 일도 하지 않고 턴이 지나간다 (클라이언트가 사전 검증)
    [Server]
    bool PayCost(TargetObject user, SkillData.SkillDef skill)
    {
        GamePlayer gamePlayer = user.player;
        switch (skill.costType)
        {
            case BattleResourceType.HP: // 에리스 — 자신의 HP 소모. 코스트로는 빈사(HP 1 이하)까지 내려갈 수 없다
                if (user.playerHP <= skill.cost) return false;
                user.LosePlayerHP(skill.cost);
                return true;
            case BattleResourceType.MP:
            case BattleResourceType.RAGE:
                if (gamePlayer.currentResource < skill.cost) return false;
                gamePlayer.currentResource -= skill.cost;
                return true;
            default:
                return true;
        }
    }

    [Server]
    TargetObject ResolveEnemyTarget(uint targetNetId)
    {
        foreach (TargetObject monster in spawnedMonsterList)
        {
            if (monster.netId == targetNetId && monster.objectType == ObjectType.ENEMY && !monster.isDying)
                return monster;
        }
        // 지정 대상이 이미 죽었으면 살아있는 첫 몬스터로 폴백 (카드 큐의 타겟 사망 재검증 대응)
        foreach (TargetObject monster in spawnedMonsterList)
        {
            if (monster.objectType == ObjectType.ENEMY && !monster.isDying)
                return monster;
        }
        return null;
    }

    [Server]
    List<TargetObject> ResolveSkillTargets(SkillData.SkillDef skill, uint targetNetId)
    {
        var targets = new List<TargetObject>();
        switch (skill.validTarget)
        {
            case ValidTarget.ENEMY:
            {
                TargetObject target = ResolveEnemyTarget(targetNetId);
                if (target != null) targets.Add(target);
                break;
            }
            case ValidTarget.ENEMY_ALL:
            {
                foreach (TargetObject monster in spawnedMonsterList)
                    if (monster.objectType == ObjectType.ENEMY && !monster.isDying)
                        targets.Add(monster);
                break;
            }
            case ValidTarget.MEMBER: // 아군 단일 (회복/실드)
            {
                TargetObject ally = ResolvePlayerTarget(targetNetId);
                if (ally != null) targets.Add(ally);
                break;
            }
            case ValidTarget.TEAM: // 아군 전원
            {
                foreach (TargetObject player in spawnedPlayerList)
                    if (!player.isDying && player.playerHP > 0)
                        targets.Add(player);
                break;
            }
        }
        return targets; // NONE(자신) 계열은 빈 목록 — 효과 메서드가 user를 직접 사용
    }

    [Server]
    TargetObject ResolvePlayerTarget(uint targetNetId)
    {
        foreach (TargetObject player in spawnedPlayerList)
        {
            if (player.netId == targetNetId && !player.isDying && player.playerHP > 0)
                return player;
        }
        foreach (TargetObject player in spawnedPlayerList) // 폴백: 살아있는 첫 아군
        {
            if (!player.isDying && player.playerHP > 0)
                return player;
        }
        return null;
    }

    /// <summary>약점 피격 시 대상 TP 감소 — 같은 대상에게 반복될수록 효과가 절반씩 줄어든다 (BattleActions가 호출)</summary>
    [Server]
    public void ApplyTpBreakTo(TargetObject target)
    {
        TpUnit unit = tpUnits.Find(u => u.target == target);
        if (unit == null) return;
        tpWeaknessHits.TryGetValue(target, out int hits);
        tpWeaknessHits[target] = hits + 1;
        int amount = Mathf.Max(BalanceData.Get("TP_BREAK_MIN", 5), BalanceData.Get("TP_BREAK_BASE", 30) >> hits);
        unit.tp = Mathf.Max(0f, unit.tp - amount);
        Debug.Log($"[TpBattle] 약점 브레이크! {target.monster?.monsterName} TP -{amount}");
    }

    /// <summary>TP 즉시 증가 (에리스 '피의 가속' 등 스킬 효과용)</summary>
    [Server]
    public void AddTpTo(TargetObject target, int amount)
    {
        TpUnit unit = tpUnits.Find(u => u.target == target);
        if (unit != null) unit.tp += amount;
    }

    int GetUnitAgility(TargetObject target)
    {
        if (target.player != null) return Mathf.Max(1, target.player.TotalAgility); // 장비 합산 민첩
        if (target.monster != null && target.monster.monster != null) return Mathf.Max(1, target.monster.monster.agility);
        return 5;
    }

    // TP 획득량 = 오프셋(BalanceDB TP_GAIN_BASE — 비공개 밸런스 값) + 민첩 (RPG_CONVERSION_BATTLE)
    int GetUnitTpGain(TargetObject target)
    {
        return BalanceData.Get("TP_GAIN_BASE", 50) + GetUnitAgility(target);
    }

    bool IsUnitAlive(TargetObject target)
    {
        if (target == null || target.isDying) return false;
        if (target.objectType == ObjectType.PLAYER) return target.player != null && target.playerHP > 0;
        return target.monster != null && target.monster.HP > 0;
    }

    // ------------------------------------------------------------- 클라이언트: 액션 제출 -------------------------------------------------------------//

    [Command(requiresAuthority = false)]
    public void CmdSubmitTpAction(uint gamePlayerNetId, int action, string skillNo, uint targetNetId, int moveRow, NetworkConnectionToClient sender = null)
    {
        if (!tpBattleActive || tpActionSubmitted || tpCurrentUnitNetId == 0) return;
        TargetObject currentUnit = NetLookup.Server<TargetObject>(tpCurrentUnitNetId);
        if (currentUnit == null || currentUnit.player == null) return;
        if (currentUnit.player.netId != gamePlayerNetId) return; // 자기 턴에만 제출 가능
        // 제출자가 해당 플레이어의 소유 커넥션인지 검증 (호스트는 sender가 로컬 커넥션)
        if (sender != null && currentUnit.player.connectionToClient != null && currentUnit.player.connectionToClient != sender) return;

        tpSubmittedAction = ((TpAction)action, skillNo, targetNetId, moveRow);
        tpActionSubmitted = true;
    }

    // ------------------------------------------------------------- 클라이언트: 임시 전투 UI (OnGUI) -------------------------------------------------------------//

    int guiSelectedAction = -1;      // -1 없음 / 0 공격 / 1 스킬 (대상 선택 대기)
    string guiSelectedSkillNo;
    GUIStyle guiTpLabelStyle;        // TP 숫자 스타일 (가운데 정렬 + 굵게, OnGUI에서 지연 생성)

    // 모든 유닛(플레이어·몬스터) 머리 위에 현재 TP를 숫자로 표시. 현재 턴 유닛은 ▶ 표시
    void DrawTpGaugeLabels()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        if (guiTpLabelStyle == null)
        {
            guiTpLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
        }

        foreach (TpGaugeSnapshot snapshot in tpGauges)
        {
            if (!NetworkClient.spawned.TryGetValue(snapshot.netId, out NetworkIdentity gaugeIdentity) || gaugeIdentity == null) continue;
            TargetObject unitTarget = gaugeIdentity.GetComponent<TargetObject>();
            if (unitTarget == null || unitTarget.isDying || !unitTarget.gameObject.activeSelf) continue;

            Vector3 screenPos = cam.WorldToScreenPoint(unitTarget.transform.position + Vector3.up * tpLabelHeight);
            if (screenPos.z < 0f) continue; // 카메라 뒤

            bool isCurrentTurn = snapshot.netId == tpCurrentUnitNetId;
            guiTpLabelStyle.normal.textColor = isCurrentTurn ? Color.yellow : Color.white;
            var labelRect = new Rect(screenPos.x - 50f, Screen.height - screenPos.y - 11f, 100f, 22f);
            GUI.Label(labelRect, isCurrentTurn ? $"▶ TP {snapshot.tp}" : $"TP {snapshot.tp}", guiTpLabelStyle);
        }
    }

    void OnGUI()
    {
        if (!Application.isPlaying || !tpBattleActive || !NetworkClient.active) return;
        if (PlayerRegistry.Local == null || PlayerRegistry.Local.currentGamePlayer == null) return;
        GamePlayer myPlayer = PlayerRegistry.Local.currentGamePlayer;

        float lineHeight = 34f;
        float x = Screen.width * 0.5f - 320f;
        float y = Screen.height - 150f;

        // 상태 표시줄: 내 HP/자원 + 현재 턴
        TargetObject currentUnit = null;
        if (tpCurrentUnitNetId != 0 && NetworkClient.spawned.TryGetValue(tpCurrentUnitNetId, out NetworkIdentity identity) && identity != null)
            currentUnit = identity.GetComponent<TargetObject>();
        string turnOwner = currentUnit == null ? "게이지 진행 중..."
            : (currentUnit.player != null ? $"{currentUnit.player.character} 턴" : "몬스터 턴");
        GUI.Label(new Rect(x, y - lineHeight, 640f, lineHeight),
            $"[TP 전투] {turnOwner}  |  내 HP {myPlayer.HP}/{myPlayer.MaxHP}  자원 {myPlayer.currentResource}/{myPlayer.maxResource}");

        DrawTpGaugeLabels(); // 유닛(플레이어·몬스터) 머리 위 TP 숫자

        bool isMyTurn = currentUnit != null && currentUnit.player != null && currentUnit.player.isOwned;
        if (!isMyTurn)
        {
            guiSelectedAction = -1;
            return;
        }

        if (guiSelectedAction < 0)
        {
            // 1단계: 액션 선택
            float buttonX = x;
            if (GUI.Button(new Rect(buttonX, y, 90f, lineHeight), "공격")) guiSelectedAction = 0;
            buttonX += 95f;
            if (GUI.Button(new Rect(buttonX, y, 90f, lineHeight), "방어"))
                CmdSubmitTpAction(myPlayer.netId, (int)TpAction.DEFEND, "", 0, 0);
            buttonX += 95f;
            foreach (SkillData.SkillDef skill in myPlayer.GetUsableSkills()) // 기본 스킬 + 스킬트리 습득분만 표시
            {
                if (GUI.Button(new Rect(buttonX, y, 130f, lineHeight), $"{skill.skillName}({skill.cost})"))
                {
                    if (skill.validTarget == ValidTarget.ENEMY)
                    {
                        guiSelectedAction = 1; // 적 단일 대상 → 대상 선택 단계로
                        guiSelectedSkillNo = skill.skillNo;
                    }
                    else if (skill.validTarget == ValidTarget.MEMBER)
                    {
                        guiSelectedAction = 2; // 아군 단일 대상 → 아군 선택 단계로
                        guiSelectedSkillNo = skill.skillNo;
                    }
                    else
                    {
                        CmdSubmitTpAction(myPlayer.netId, (int)TpAction.SKILL, skill.skillNo, 0, 0); // 전체/자신 — 대상 불필요
                    }
                }
                buttonX += 135f;
            }
            // 이동 (전열/중열/후열) + 아이템 — 전열 = playerOrder[2] (몬스터와 가까운 위치)
            string[] rowNames = { "전열", "중열", "후열" };
            int[] rowTargetIndex = { 2, 1, 0 };
            for (int row = 0; row < rowNames.Length; row++)
            {
                if (GUI.Button(new Rect(x + row * 65f, y + lineHeight + 4f, 60f, lineHeight - 6f), rowNames[row]))
                    CmdSubmitTpAction(myPlayer.netId, (int)TpAction.MOVE, "", 0, rowTargetIndex[row]);
            }
            if (myPlayer.inventoryConsumables.Count > 0
                && GUI.Button(new Rect(x + rowNames.Length * 65f + 10f, y + lineHeight + 4f, 100f, lineHeight - 6f), $"아이템({myPlayer.inventoryConsumables.Count})"))
                guiSelectedAction = 3;
        }
        else if (guiSelectedAction == 3)
        {
            // 2단계: 소모품 선택
            GUI.Label(new Rect(x, y - lineHeight * 2f, 400f, lineHeight), "사용할 아이템:");
            var potionCounts = new Dictionary<string, int>();
            foreach (string potionNo in myPlayer.inventoryConsumables)
            {
                potionCounts.TryGetValue(potionNo, out int count);
                potionCounts[potionNo] = count + 1;
            }
            float buttonX = x;
            foreach (KeyValuePair<string, int> entry in potionCounts)
            {
                ConsumableData.Def potion = ConsumableData.Get(entry.Key);
                if (potion == null) continue;
                if (GUI.Button(new Rect(buttonX, y, 170f, lineHeight), $"{potion.potionName} x{entry.Value}"))
                {
                    CmdSubmitTpAction(myPlayer.netId, (int)TpAction.ITEM, entry.Key, 0, 0);
                    guiSelectedAction = -1;
                }
                buttonX += 175f;
            }
            if (GUI.Button(new Rect(buttonX, y, 70f, lineHeight), "취소"))
                guiSelectedAction = -1;
        }
        else if (guiSelectedAction == 2)
        {
            // 2단계: 아군 대상 선택 (회복/실드 스킬)
            GUI.Label(new Rect(x, y - lineHeight * 2f, 400f, lineHeight), "아군을 선택하세요:");
            float buttonX = x;
            foreach (uint playerNetId in spawnedPlayerSyncList)
            {
                if (playerNetId == 0) continue; // 빈 슬롯
                if (!NetworkClient.spawned.TryGetValue(playerNetId, out NetworkIdentity playerIdentity) || playerIdentity == null) continue;
                TargetObject ally = playerIdentity.GetComponent<TargetObject>();
                if (ally == null || ally.player == null || ally.playerHP <= 0) continue;
                if (GUI.Button(new Rect(buttonX, y, 170f, lineHeight), $"{ally.player.character} ({ally.playerHP}/{ally.playerMaxHP})"))
                {
                    CmdSubmitTpAction(myPlayer.netId, (int)TpAction.SKILL, guiSelectedSkillNo, playerNetId, 0);
                    guiSelectedAction = -1;
                }
                buttonX += 175f;
            }
            if (GUI.Button(new Rect(buttonX, y, 70f, lineHeight), "취소"))
                guiSelectedAction = -1;
        }
        else
        {
            // 2단계: 적 대상 선택 (공격/단일 스킬) — 약점 속성 힌트 표시 (MonsterStatDB 기반, 클라이언트 로컬 조회)
            GUI.Label(new Rect(x, y - lineHeight * 2f, 400f, lineHeight), "대상을 선택하세요:");
            float buttonX = x;
            foreach (uint monsterNetId in spawnedMonsterSyncList)
            {
                if (!NetworkClient.spawned.TryGetValue(monsterNetId, out NetworkIdentity monsterIdentity) || monsterIdentity == null) continue;
                TargetObject monster = monsterIdentity.GetComponent<TargetObject>();
                if (monster == null || monster.objectType != ObjectType.ENEMY || monster.isDying || monster.monster == null) continue;
                string weaknessHint = "";
                Monster monsterData = MonsterData.instance.monsterDataList.Find(m => m.name == monster.monster.monsterName);
                if (monsterData != null && monsterData.weaknesses.Count > 0)
                {
                    var names = new List<string>();
                    foreach (AttackAttribute weakness in monsterData.weaknesses)
                        names.Add(BattleActions.AttributeName(weakness));
                    weaknessHint = $" 약점:{string.Join("/", names)}";
                }
                if (GUI.Button(new Rect(buttonX, y, 190f, lineHeight), $"{monster.monster.monsterName} ({monster.monster.HP}){weaknessHint}"))
                {
                    if (guiSelectedAction == 0)
                        CmdSubmitTpAction(myPlayer.netId, (int)TpAction.ATTACK, "", monsterNetId, 0);
                    else
                        CmdSubmitTpAction(myPlayer.netId, (int)TpAction.SKILL, guiSelectedSkillNo, monsterNetId, 0);
                    guiSelectedAction = -1;
                }
                buttonX += 195f;
            }
            if (GUI.Button(new Rect(buttonX, y, 70f, lineHeight), "취소"))
                guiSelectedAction = -1;
        }
    }
}
