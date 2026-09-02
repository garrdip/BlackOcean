using UnityEngine;

// 스킬트리 노드 사이를 잇는 선 하나 (Image와 함께 사용).
// fromNodeId가 빈 문자열이면 시작점이 Center(트리 시작 허브). 색상 처리는 SkillTreeUIManager가 담당:
// 양 끝 노드를 모두 습득하면(시작이 Center면 도착 노드만) RED, 아니면 기본 회색.
public class SkillTreeLink : MonoBehaviour
{
    public string fromNodeId; // 선행 쪽 SkillTreeDB NodeId (빈 문자열 = Center)
    public string toNodeId;   // 후행 쪽 SkillTreeDB NodeId
}
