using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;

// GamePlayer partial — 스킬트리 습득/조회 (RPG 전환 Phase 3).
// 데이터: SkillTreeDB.csv (SkillTreeData), 습득 상태: learnedNodes SyncList, 재화: skillPoints (레벨업당 +1).
// UI는 정식 팝업 전까지 OnGUI 임시 창 (우상단 '스킬트리' 버튼으로 토글 — 소유 클라이언트에만 표시).
public partial class GamePlayer
{
    // ------------------------------------------------------------- 조회 (클라/서버 공용 — SyncList 기반) -------------------------------------------------------------//

    /// <summary>스킬 사용 가능 여부 — 기본 스킬(innate)이거나 트리에서 습득(SKILL/ULTIMATE 노드)한 경우</summary>
    public bool KnowsSkill(string skillNo)
    {
        SkillData.SkillDef skill = SkillData.Get(skillNo);
        if (skill == null || skill.character != character) return false;
        if (skill.innate) return true;
        foreach (string nodeId in learnedNodes)
        {
            SkillTreeData.Node node = SkillTreeData.Get(nodeId);
            if (node != null && node.skillNo == skillNo
                && (node.nodeType == SkillTreeData.NodeType.SKILL || node.nodeType == SkillTreeData.NodeType.ULTIMATE))
                return true;
        }
        return false;
    }

    /// <summary>스킬 강화 레벨 — 습득한 SKILL_LEVEL 노드 수 + 장비 '스킬레벨' 옵션 (피해 계수에 레벨당 +20%)</summary>
    public int GetSkillLevel(string skillNo)
    {
        int count = EquipSkillLevelBonus;
        foreach (string nodeId in learnedNodes)
        {
            SkillTreeData.Node node = SkillTreeData.Get(nodeId);
            if (node != null && node.nodeType == SkillTreeData.NodeType.SKILL_LEVEL && node.skillNo == skillNo)
                count++;
        }
        return count;
    }

    /// <summary>현재 사용 가능한 액티브 스킬 목록 (기본 스킬 + 습득 스킬) — 전투 UI가 사용. 패시브(기사도 등)는 제외</summary>
    public List<SkillData.SkillDef> GetUsableSkills()
    {
        var result = new List<SkillData.SkillDef>();
        foreach (SkillData.SkillDef skill in SkillData.GetSkillsByCharacter(character))
            if (KnowsSkill(skill.skillNo) && !skill.passive) result.Add(skill);
        return result;
    }

    public bool HasLearnedNode(string nodeId) => learnedNodes.Contains(nodeId);

    /// <summary>습득 가능 여부 — 미습득 + 포인트 충분 + 선행 노드 습득</summary>
    public bool CanLearnNode(SkillTreeData.Node node)
    {
        if (node == null || node.character != character) return false;
        if (HasLearnedNode(node.nodeId)) return false;
        if (skillPoints < node.cost) return false;
        if (node.parent.Length > 0 && !HasLearnedNode(node.parent)) return false;
        return true;
    }

    // ------------------------------------------------------------- 습득 (서버 검증) -------------------------------------------------------------//

    [Command]
    public void CmdLearnNode(string nodeId)
    {
        SkillTreeData.Node node = SkillTreeData.Get(nodeId);
        if (!CanLearnNode(node)) return; // 서버 상태로 재검증 (포인트/선행/중복)
        skillPoints -= node.cost;
        learnedNodes.Add(node.nodeId);
        if (node.nodeType == SkillTreeData.NodeType.STAT)
            ApplyStatNode(node);
        Debug.Log($"[SkillTree] {character} 노드 습득: {node.nodeId} ({node.description}) — 잔여 포인트 {skillPoints}");
        // 거점에서의 습득은 전투 없이 종료해도 유실되지 않도록 즉시 자동 저장 (전투 중 습득은 전투 종료 자동 저장이 담당)
        if (M_HubManager.instance != null && M_HubManager.instance.isInHub)
            GameSaveService.SaveGame();
    }

    // 디버그 — 다음 레벨까지 남은 경험치를 즉시 지급해 레벨업 (테스트 전용, 우상단 OnGUI 버튼)
    [Command]
    public void CmdDebugLevelUp()
    {
        int required = LevelData.GetRequiredExp(level);
        if (required <= 0) return; // 최대 레벨
        AddExp(Mathf.Max(1, required - exp));
    }

    [Server]
    private void ApplyStatNode(SkillTreeData.Node node)
    {
        switch (node.stat)
        {
            case "STR": strength += node.statValue; break;
            case "AGI": agility += node.statValue; break;
            case "INT": intelligence += node.statValue; break;
            case "DEF": defense += node.statValue; break;
            case "MDEF": magicDefense += node.statValue; break;
            case "CTRL": control += node.statValue; break; // 제어 — MP 자연 회복·분노 생성량 증가
            case "HP": AddMaxHP(node.statValue); break; // 최대 HP — 레벨업과 동일 규칙(증가분만큼 현재 HP도 회복). 구 VIT 노드 대체
            default:
                Debug.LogError($"[SkillTree] 알 수 없는 스탯: {node.stat} ({node.nodeId})");
                break;
        }
    }

    // ------------------------------------------------------------- 임시 UI (OnGUI — 정식 팝업 전까지) -------------------------------------------------------------//

    bool guiTreeOpen;
    Vector2 guiTreeScroll;

    void OnGUI()
    {
        if (!Application.isPlaying || !isOwned) return;
        if (PlayerRegistry.Local == null || PlayerRegistry.Local.currentGamePlayer != this) return;

        DrawEquipmentGUI(); // 장비/인벤토리 창 (GamePlayer.Equipment.cs — 같은 컴포넌트라 OnGUI는 여기 하나로 합친다)

        // 디버그 위험도 상승 버튼 — 전역 위험도 +1 (레벨업 버튼 왼쪽)
        if (M_HubManager.instance != null
            && GUI.Button(new Rect(Screen.width - 450f, 10f, 140f, 30f), $"위험도 +1 ({M_HubManager.instance.hazardLevel})"))
            M_HubManager.instance.CmdDebugRaiseHazard();

        // 디버그 레벨업 버튼 — 다음 레벨까지 경험치 즉시 지급 (스킬트리 버튼 왼쪽)
        if (GUI.Button(new Rect(Screen.width - 300f, 10f, 140f, 30f), $"레벨업 (Lv.{level})"))
            CmdDebugLevelUp();

        Rect toggleRect = new Rect(Screen.width - 150f, 10f, 140f, 30f);
        if (GUI.Button(toggleRect, guiTreeOpen ? "스킬트리 닫기" : $"스킬트리 (P:{skillPoints})"))
        {
            guiTreeOpen = !guiTreeOpen;
            if (guiTreeOpen) guiEquipOpen = false; // 장비 창과 상호 배타 (같은 자리 사용)
        }
        if (!guiTreeOpen) return;

        float windowWidth = 560f;
        float windowHeight = 420f;
        Rect windowRect = new Rect(Screen.width - windowWidth - 10f, 85f, windowWidth, windowHeight);
        GUI.Box(windowRect, $"{character} 스킬트리  |  Lv.{level}  포인트 {skillPoints}");

        guiTreeScroll = GUI.BeginScrollView(
            new Rect(windowRect.x + 10f, windowRect.y + 30f, windowWidth - 20f, windowHeight - 40f),
            guiTreeScroll, new Rect(0, 0, windowWidth - 40f, 800f));

        float y = 0f;
        string currentTree = null;
        foreach (SkillTreeData.Node node in SkillTreeData.GetNodesByCharacter(character))
        {
            if (node.tree != currentTree)
            {
                currentTree = node.tree;
                GUI.Label(new Rect(0, y, 200f, 24f), $"◆ {currentTree} 트리");
                y += 26f;
            }

            bool learned = HasLearnedNode(node.nodeId);
            bool canLearn = CanLearnNode(node);
            string label = $"{node.description} (비용 {node.cost}P)";
            if (learned)
            {
                GUI.Label(new Rect(20f, y, 480f, 24f), $"✔ {label}");
            }
            else
            {
                GUI.enabled = canLearn;
                if (GUI.Button(new Rect(20f, y, 480f, 24f), canLearn ? label : $"{label} — 잠김"))
                    CmdLearnNode(node.nodeId);
                GUI.enabled = true;
            }
            y += 28f;
        }
        GUI.EndScrollView();
    }
}
