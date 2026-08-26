using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

/// <summary>
/// 스킬트리 테이블 (Resources/DB/SkillTreeDB.csv) — 드퀘식 트리 (RPG_CONVERSION_DESIGN).
/// 노드 타입: SKILL(액티브 습득) / SKILL_LEVEL(스킬 강화 — 습득 수만큼 피해 +20%) / STAT(스탯 상승) / ULTIMATE(필살기).
/// 습득 상태는 GamePlayer.learnedNodes(SyncList)가 들고, 검증/적용은 GamePlayer.SkillTree.cs가 담당한다.
/// </summary>
public static class SkillTreeData
{
    public enum NodeType { SKILL, SKILL_LEVEL, STAT, ULTIMATE }

    public class Node
    {
        public string nodeId;
        public Character character;
        public string tree;      // 트리 이름 (연격/수호/개화/회복/은하수/별무리)
        public int tier;         // 0부터 시작하는 깊이
        public string parent;    // 선행 노드 id (빈 문자열 = 루트)
        public NodeType nodeType;
        public string skillNo;   // SKILL/SKILL_LEVEL/ULTIMATE의 대상 스킬
        public string stat;      // STAT 노드의 대상 (STR/AGI/VIT/INT/DEF/MDEF/CTRL)
        public int statValue;
        public int cost;         // 소모 스킬 포인트 (RPG_CONVERSION_SKILLS의 [N] — 레벨업당 3포인트 획득)
        public string description;
    }

    static Dictionary<string, Node> nodes;

    static void EnsureLoaded()
    {
        if (nodes != null) return;
        nodes = new Dictionary<string, Node>();
        CsvTable table = CsvTable.LoadFromResources("DB/SkillTreeDB");
        foreach (CsvTable.Row row in table.rows)
        {
            try
            {
                var node = new Node
                {
                    nodeId = row.Get("NodeId").Trim(),
                    character = (Character)Enum.Parse(typeof(Character), row.Get("Character").Trim()),
                    tree = row.Get("Tree").Trim(),
                    tier = row.GetInt("Tier"),
                    parent = row.Get("Parent").Trim(),
                    nodeType = (NodeType)Enum.Parse(typeof(NodeType), row.Get("NodeType").Trim()),
                    skillNo = row.Get("SkillNo").Trim(),
                    stat = row.Get("Stat").Trim(),
                    statValue = row.Get("StatValue").Trim().Length == 0 ? 0 : row.GetInt("StatValue"),
                    cost = row.GetInt("Cost"),
                    description = row.Get("Description"),
                };
                nodes[node.nodeId] = node;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SkillTreeData] SkillTreeDB 로드 실패 ({row.lineNumber}행) — {e.Message}");
            }
        }
    }

    public static Node Get(string nodeId)
    {
        EnsureLoaded();
        return nodes.TryGetValue(nodeId, out Node node) ? node : null;
    }

    /// <summary>캐릭터의 전체 노드 (트리명 → 티어 순 정렬)</summary>
    public static List<Node> GetNodesByCharacter(Character character)
    {
        EnsureLoaded();
        var result = new List<Node>();
        foreach (Node node in nodes.Values)
            if (node.character == character) result.Add(node);
        result.Sort((a, b) => a.tree != b.tree ? string.Compare(a.tree, b.tree, StringComparison.Ordinal) : a.tier.CompareTo(b.tier));
        return result;
    }
}
