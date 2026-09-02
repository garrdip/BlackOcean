using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectD;

// 스킬트리 팝업 캔버스 관리자 (GameScene 씬 배치 — 옵션창과 같은 Screen Space Overlay 캔버스 방식).
// 표시 트리는 좌상단 파티 배너로 선택한 현재 조작 캐릭터(currentGamePlayer)를 따라간다:
// 게오르크 = GeorkSkillTree(GKA*/GKD*/GKW*), 에리스 = ErisSkillTree(EKA*), 홍단향 = 트리 없음 안내.
// 레이아웃 (2026-09-02): 트리는 우측, 좌측에 설명 패널(옵션창 1-2.M Base 이미지) — 노드를 클릭하면 스킬 이름·설명·코스트·필요 포인트·습득 상태 표시.
// 노드 이미지(SkillTreeNodeButton) 클릭 = 선택 → 하단 작은 '배우기' 버튼 활성화 → 버튼 클릭 시 습득(CmdLearnNode).
// 색상: 습득 = RED, 습득 가능 = 흰색, 잠김(선행 미습득/포인트 부족) = 회색.
// 연결선(SkillTreeLink)은 양 끝 노드를 모두 습득하면 RED — 배운 경로가 중앙에서부터 이어진다.
// 습득/포인트 판정은 노드가 속한 캐릭터의 소유 GamePlayer 기준 (멀티에서 미소유 캐릭터 트리는 회색·습득 불가).
// 우상단 '스킬트리' 디버그 버튼(GamePlayer.SkillTree.cs OnGUI)이 토글한다.
public class SkillTreeUIManager : InstanceD<SkillTreeUIManager>
{
    static readonly Color LearnedColor = Color.red;
    static readonly Color LearnableColor = Color.white;
    static readonly Color LockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    static readonly Color LineDefaultColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    public GameObject skillTreePopUp; // 캔버스 하위 팝업 루트 — SetActive로 표시/숨김 (옵션창의 optionPopUp과 동일 방식)
    public GameObject georkTreeRoot;  // GeorkSkillTree — 게오르크 선택 시 표시
    public GameObject erisTreeRoot;   // ErisSkillTree — 에리스 선택 시 표시
    public TMP_Text skillPointText;   // 트리 상단 — 캐릭터 이름 + 남은 스킬 포인트
    public Button learnButton;        // 트리 하단 작은 버튼 — 노드 선택 시 활성화, 클릭하면 습득
    public TMP_Text learnButtonLabel; // 고정 "배우기" (습득 완료 노드는 "습득 완료")
    public TMP_Text skillNameText;        // 좌측 설명 패널(1-2.M Base) — 선택한 노드의 스킬/스탯 이름
    public TMP_Text skillDescriptionText; // 좌측 설명 패널 — 효과 설명(SkillDB Description) + 코스트 + 필요 포인트 + 습득 상태

    SkillTreeNodeButton[] nodeButtons; // 팝업 안 전체 노드 이미지 목록 (비활성 트리 포함 수집)
    Image[] nodeImages;                // nodeButtons와 같은 순서의 Image 캐시
    SkillTreeLink[] links;             // 노드 사이 연결선 목록
    Image[] linkImages;                // links와 같은 순서의 Image 캐시
    string selectedNodeId;
    Character lastCharacter = Character.NONE; // 캐릭터가 바뀌면 선택을 초기화하기 위한 추적값

    public bool IsOpen => skillTreePopUp != null && skillTreePopUp.activeSelf;

    void Start()
    {
        nodeButtons = skillTreePopUp != null
            ? skillTreePopUp.GetComponentsInChildren<SkillTreeNodeButton>(true)
            : new SkillTreeNodeButton[0];
        nodeImages = new Image[nodeButtons.Length];
        for (int i = 0; i < nodeButtons.Length; i++)
        {
            nodeImages[i] = nodeButtons[i].GetComponent<Image>();
            Button button = nodeButtons[i].GetComponent<Button>();
            if (button == null) continue;
            SkillTreeNodeButton captured = nodeButtons[i];
            button.onClick.AddListener(() => OnClickNode(captured));
        }
        if (learnButton != null) learnButton.onClick.AddListener(OnClickLearn);

        links = skillTreePopUp != null
            ? skillTreePopUp.GetComponentsInChildren<SkillTreeLink>(true)
            : new SkillTreeLink[0];
        linkImages = new Image[links.Length];
        for (int i = 0; i < links.Length; i++)
            linkImages[i] = links[i].GetComponent<Image>();
    }

    public void Show(bool isActive)
    {
        if (skillTreePopUp == null) return;
        if (isActive)
        {
            selectedNodeId = null; // 열 때마다 선택 초기화 — 배우기 버튼은 노드를 클릭해야 나타난다
            Refresh();
        }
        skillTreePopUp.SetActive(isActive);
    }

    void Update()
    {
        // 습득 결과(SyncList/SyncVar)와 배너 캐릭터 전환은 비동기로 일어나므로 열려 있는 동안 매 프레임 갱신한다
        if (IsOpen) Refresh();
    }

    /// <summary>좌상단 배너로 선택한 현재 조작 캐릭터의 GamePlayer</summary>
    GamePlayer CurrentPlayer => PlayerRegistry.Local != null ? PlayerRegistry.Local.currentGamePlayer : null;

    /// <summary>해당 캐릭터를 소유한 내 GamePlayer (멀티에서 다른 플레이어 소유면 null)</summary>
    GamePlayer GetOwnedPlayer(Character character)
    {
        if (PlayerRegistry.Local == null) return null;
        foreach (GamePlayer owned in PlayerRegistry.Local.ownedPlayers)
            if (owned != null && owned.character == character) return owned;
        return null;
    }

    static string CharacterDisplayName(Character character)
    {
        switch (character)
        {
            case Character.GEORK: return "게오르크";
            case Character.ERIS: return "에리스";
            case Character.HONGDANHYANG: return "홍단향";
            default: return "-";
        }
    }

    void OnClickNode(SkillTreeNodeButton node)
    {
        if (node == null || string.IsNullOrEmpty(node.nodeId)) return;
        selectedNodeId = node.nodeId;
    }

    void OnClickLearn()
    {
        SkillTreeData.Node node = string.IsNullOrEmpty(selectedNodeId) ? null : SkillTreeData.Get(selectedNodeId);
        if (node == null) return;
        GamePlayer owner = GetOwnedPlayer(node.character);
        if (owner == null || !owner.CanLearnNode(node)) return;
        owner.CmdLearnNode(node.nodeId); // 서버 재검증 — 습득되면 learnedNodes 동기화로 색이 RED로 바뀐다
    }

    void Refresh()
    {
        GamePlayer current = CurrentPlayer;
        Character currentCharacter = current != null ? current.character : Character.NONE;

        // 배너로 캐릭터를 바꾸면 표시 트리를 전환하고 노드 선택을 초기화한다
        if (currentCharacter != lastCharacter)
        {
            lastCharacter = currentCharacter;
            selectedNodeId = null;
        }
        if (georkTreeRoot != null) georkTreeRoot.SetActive(currentCharacter == Character.GEORK);
        if (erisTreeRoot != null) erisTreeRoot.SetActive(currentCharacter == Character.ERIS);
        bool hasTree = currentCharacter == Character.GEORK || currentCharacter == Character.ERIS;

        if (skillPointText != null)
            skillPointText.text = current == null ? "-"
                : hasTree ? $"{CharacterDisplayName(currentCharacter)} — 남은 스킬 포인트 : {current.skillPoints}"
                : $"{CharacterDisplayName(currentCharacter)} — 스킬트리 준비 중";

        // 노드 색상 — 노드가 속한 캐릭터의 소유 GamePlayer 기준 (비활성 트리 노드도 같이 갱신해도 무해)
        if (nodeButtons != null)
            for (int i = 0; i < nodeButtons.Length; i++)
            {
                if (nodeImages[i] == null || string.IsNullOrEmpty(nodeButtons[i].nodeId)) continue; // 미연결 노드는 원래 색 유지
                SkillTreeData.Node node = SkillTreeData.Get(nodeButtons[i].nodeId);
                GamePlayer owner = node != null ? GetOwnedPlayer(node.character) : null;
                if (owner == null || node == null) { nodeImages[i].color = LockedColor; continue; }
                if (owner.HasLearnedNode(node.nodeId)) nodeImages[i].color = LearnedColor;
                else if (owner.CanLearnNode(node)) nodeImages[i].color = LearnableColor;
                else nodeImages[i].color = LockedColor;
            }

        // 연결선 — 양 끝 노드를 모두 습득하면 RED (시작이 Center인 선은 도착 노드만 보면 된다)
        if (links != null)
            for (int i = 0; i < links.Length; i++)
            {
                if (linkImages[i] == null || string.IsNullOrEmpty(links[i].toNodeId)) continue;
                SkillTreeData.Node toNode = SkillTreeData.Get(links[i].toNodeId);
                GamePlayer owner = toNode != null ? GetOwnedPlayer(toNode.character) : null;
                bool fromLearned = string.IsNullOrEmpty(links[i].fromNodeId)
                    || (owner != null && owner.HasLearnedNode(links[i].fromNodeId));
                bool toLearned = owner != null && owner.HasLearnedNode(links[i].toNodeId);
                linkImages[i].color = fromLearned && toLearned ? LearnedColor : LineDefaultColor;
            }

        SkillTreeData.Node selected = string.IsNullOrEmpty(selectedNodeId) ? null : SkillTreeData.Get(selectedNodeId);
        if (learnButton != null)
        {
            learnButton.gameObject.SetActive(selected != null); // 노드를 클릭해야 하단 버튼이 나타난다
            if (selected != null)
            {
                GamePlayer owner = GetOwnedPlayer(selected.character);
                bool learned = owner != null && owner.HasLearnedNode(selected.nodeId);
                learnButton.interactable = owner != null && !learned && owner.CanLearnNode(selected);
                if (learnButtonLabel != null) learnButtonLabel.text = learned ? "습득 완료" : "배우기"; // 작은 버튼 — 상세 설명은 좌측 패널
            }
        }
        RefreshDescriptionPanel(current, selected);
    }

    // ------------------------------------------------------------- 좌측 설명 패널 -------------------------------------------------------------//
    // 노드를 클릭하면 스킬 이름과 설명(SkillDB Description — RPG_CONVERSION_SKILLS 기준)을 보여준다. 스탯 노드는 트리 설명(SkillTreeDB)을 풀어서 표시

    void RefreshDescriptionPanel(GamePlayer current, SkillTreeData.Node selected)
    {
        if (skillNameText == null && skillDescriptionText == null) return;
        if (selected == null)
        {
            if (skillNameText != null) skillNameText.text = current != null ? $"{CharacterDisplayName(current.character)} 스킬트리" : "-";
            if (skillDescriptionText != null) skillDescriptionText.text = "노드를 클릭하면 스킬 이름과 설명이 여기에 표시됩니다.";
            return;
        }

        GamePlayer owner = GetOwnedPlayer(selected.character);
        SkillData.SkillDef skill = string.IsNullOrEmpty(selected.skillNo) ? null : SkillData.Get(selected.skillNo);
        var body = new System.Text.StringBuilder();
        string title;
        switch (selected.nodeType)
        {
            case SkillTreeData.NodeType.STAT:
                title = selected.description;
                body.AppendLine($"{CharacterDisplayName(selected.character)}의 {StatDisplayName(selected.stat)}을(를) {selected.statValue}만큼 상승시킨다.");
                break;
            case SkillTreeData.NodeType.SKILL_LEVEL:
                title = selected.description;
                body.AppendLine(skill != null ? $"{skill.skillName} 강화 — {skill.description}" : selected.description);
                break;
            default: // SKILL / ULTIMATE
                title = skill != null ? skill.skillName : selected.description;
                if (skill != null)
                {
                    body.AppendLine(skill.passive ? "[패시브]" : skill.innate ? "[기본 스킬 — 턴 소모 없음]" : "[액티브]");
                    body.AppendLine(skill.description);
                    if (!skill.passive && skill.costType != BattleResourceType.NONE)
                        body.AppendLine($"소모: {ResourceDisplayName(skill.costType)} {skill.cost}");
                }
                else body.AppendLine(selected.description);
                break;
        }
        body.AppendLine();
        body.AppendLine($"트리: {selected.tree}   필요 스킬 포인트: {selected.cost}");
        body.Append(NodeStatusText(owner, selected));

        if (skillNameText != null) skillNameText.text = title;
        if (skillDescriptionText != null) skillDescriptionText.text = body.ToString();
    }

    // 습득 상태 한 줄 — 습득 완료 / 습득 가능 / 포인트 부족 / 선행 노드 필요 / 다른 플레이어 소유
    static string NodeStatusText(GamePlayer owner, SkillTreeData.Node node)
    {
        if (owner == null) return "상태: 내 캐릭터가 아니라 습득할 수 없음";
        if (owner.HasLearnedNode(node.nodeId)) return "상태: 습득 완료";
        if (owner.CanLearnNode(node)) return "상태: 습득 가능";
        bool parentLearned = node.parents.Length == 0;
        foreach (string parentId in node.parents)
            if (owner.HasLearnedNode(parentId)) { parentLearned = true; break; }
        if (!parentLearned) return "상태: 선행 노드를 먼저 습득해야 함";
        return $"상태: 스킬 포인트 부족 (보유 {owner.skillPoints})";
    }

    static string StatDisplayName(string stat)
    {
        switch (stat)
        {
            case "STR": return "힘";
            case "AGI": return "민첩";
            case "INT": return "지능";
            case "DEF": return "방어";
            case "MDEF": return "마법방어";
            case "CTRL": return "제어";
            case "HP": return "최대 HP";
            default: return stat;
        }
    }

    static string ResourceDisplayName(BattleResourceType type)
    {
        switch (type)
        {
            case BattleResourceType.RAGE: return "분노";
            case BattleResourceType.MP: return "MP";
            case BattleResourceType.HP: return "HP";
            default: return type.ToString();
        }
    }
}
