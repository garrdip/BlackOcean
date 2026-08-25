/// <summary>
/// 스테이지 미로의 방 하나 — 격자 좌표 + 종류(RoomType) + 클리어 여부.
/// 서버가 입장 시 생성(StageData.Entry.GenerateLayout)해 M_HubManager.stageRooms(SyncList)로 동기화한다.
/// Mirror 위버가 직렬화할 수 있도록 public 필드만 가진 단순 구조체로 유지할 것.
/// </summary>
[System.Serializable]
public struct StageRoomInfo
{
    public int x;       // 격자 X (입구 = 0,0)
    public int y;       // 격자 Y
    public int type;    // ProjectD.RoomType
    public bool cleared; // 방문/클리어 여부 — 클리어한 방은 계속 보이고 자유롭게 되돌아갈 수 있다

    public StageRoomInfo(int x, int y, ProjectD.RoomType type, bool cleared)
    {
        this.x = x;
        this.y = y;
        this.type = (int)type;
        this.cleared = cleared;
    }

    public ProjectD.RoomType RoomType => (ProjectD.RoomType)type;

    /// <summary>상하좌우로 붙어 있는 방인지 (맨해튼 거리 1)</summary>
    public bool IsAdjacentTo(StageRoomInfo other)
    {
        return UnityEngine.Mathf.Abs(x - other.x) + UnityEngine.Mathf.Abs(y - other.y) == 1;
    }
}
