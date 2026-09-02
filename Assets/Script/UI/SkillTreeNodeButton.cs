using UnityEngine;

// 스킬트리 팝업의 노드 이미지 하나 — SkillTreeDB.csv의 NodeId와 매칭한다 (예: C Tree = GKA1).
// 클릭/색상 처리는 SkillTreeUIManager가 자식에서 수집해 담당한다. nodeId가 비어 있으면 미연결(장식) 노드.
public class SkillTreeNodeButton : MonoBehaviour
{
    public string nodeId; // SkillTreeDB.csv NodeId (예: GKA1)
}
