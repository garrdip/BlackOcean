using System.Collections.Generic;
using UnityEngine;
using ProjectD;

/// <summary>
/// 스테이지 테이블 (Resources/DB/StageDB.csv) — 거점 "출정"에서 선택하는 스테이지 목록과 미로 생성 규칙.
/// 행 순서 = 해금 순서 (1-1 → 1-2 → …). 방 배치는 미리 정해져 있지 않고 **입장할 때마다 GenerateLayout으로 랜덤 생성**한다.
/// 미로: 입구(0,0)에서 격자를 랜덤 확장(트리 위주, 가끔 고리) → 입구에서 가장 먼 막다른 방 = 출구(EXIT) 또는 보스(BOSS, Type=BOSS일 때).
/// - RoomCount: 입구/출구(보스)를 제외한 내용 방 개수 / EliteCount: 엘리트 방 개수(막다른 방 우선) / EmptyPercent: 나머지 방이 빈 방일 확률 %
/// - Hazard: 전투 방(MONSTER/ELITE/BOSS)의 MonsterGroupDB 선택 위험도
/// LevelData와 같은 정적 로더 패턴 — 파일/행 오류 시 에러 로그 후 해당 행만 건너뛴다.
/// </summary>
public static class StageData
{
    public class Entry
    {
        public string stageNo;
        public string name;
        public RoomType roomType;   // BOSS = 보스 스테이지 (가장 먼 방이 보스), 그 외 = 출구(EXIT)
        public int hazard;
        public int roomCount = 5;
        public int eliteCount = 0;
        public int emptyPercent = 40;
        public string description;

        public bool IsBossStage => roomType == RoomType.BOSS;

        const int GridHalfWidth = 4;  // 격자 범위 제한 (미니맵에 맞추기 위해)
        const int GridHalfHeight = 3;
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        /// <summary>입장 시 미로 생성 (서버). 인덱스 0 = 입구(빈 방, 클리어 상태)</summary>
        public List<StageRoomInfo> GenerateLayout()
        {
            int contentCount = Mathf.Max(1, roomCount);
            int targetCount = contentCount + 2; // 입구 + 내용 방 + 출구/보스

            // 1. 격자 랜덤 확장 — 기존 방 하나를 골라 한 칸 뻗는다. 이미 이웃이 2개 이상인 칸은 대체로 피해 미로(트리) 형태를 유지, 25%는 허용해 고리를 만든다
            List<Vector2Int> cells = new List<Vector2Int> { Vector2Int.zero };
            HashSet<Vector2Int> occupied = new HashSet<Vector2Int> { Vector2Int.zero };
            int guard = 0;
            while (cells.Count < targetCount && guard++ < 10000)
            {
                Vector2Int from = cells[Random.Range(0, cells.Count)];
                Vector2Int to = from + Directions[Random.Range(0, Directions.Length)];
                if (occupied.Contains(to)) continue;
                if (Mathf.Abs(to.x) > GridHalfWidth || Mathf.Abs(to.y) > GridHalfHeight) continue;
                if (CountNeighbors(to, occupied) > 1 && Random.value > 0.25f) continue;
                cells.Add(to);
                occupied.Add(to);
            }

            // 2. 입구에서의 거리(BFS) — 가장 먼 막다른 방을 출구/보스로
            Dictionary<Vector2Int, int> distance = new Dictionary<Vector2Int, int> { { Vector2Int.zero, 0 } };
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(Vector2Int.zero);
            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int next = cell + direction;
                    if (!occupied.Contains(next) || distance.ContainsKey(next)) continue;
                    distance[next] = distance[cell] + 1;
                    queue.Enqueue(next);
                }
            }
            int exitIndex = 0;
            int bestScore = -1;
            for (int i = 1; i < cells.Count; i++)
            {
                int score = distance[cells[i]] * 2 + (CountNeighbors(cells[i], occupied) == 1 ? 1 : 0); // 거리 우선, 막다른 방 가산
                if (score > bestScore) { bestScore = score; exitIndex = i; }
            }

            // 3. 종류 배정
            RoomType[] types = new RoomType[cells.Count];
            types[0] = RoomType.EMPTY; // 입구
            if (cells.Count > 1) types[exitIndex] = IsBossStage ? RoomType.BOSS : RoomType.EXIT;

            List<int> candidates = new List<int>();
            for (int i = 1; i < cells.Count; i++) if (i != exitIndex) candidates.Add(i);

            // 엘리트 — 막다른 방 우선, 부족하면 랜덤
            List<int> deadEnds = candidates.FindAll(i => CountNeighbors(cells[i], occupied) == 1);
            int elites = Mathf.Min(eliteCount, candidates.Count);
            for (int e = 0; e < elites; e++)
            {
                List<int> pool = deadEnds.Count > 0 ? deadEnds : candidates;
                int pick = pool[Random.Range(0, pool.Count)];
                types[pick] = RoomType.ELITE;
                candidates.Remove(pick);
                deadEnds.Remove(pick);
            }

            // 나머지 — 빈 방 또는 몬스터 방 (전부 빈 방이면 하나는 몬스터로 보정)
            bool hasMonster = false;
            foreach (int i in candidates)
            {
                types[i] = Random.Range(0, 100) < emptyPercent ? RoomType.EMPTY : RoomType.MONSTER;
                if (types[i] == RoomType.MONSTER) hasMonster = true;
            }
            if (!hasMonster && elites == 0 && !IsBossStage && candidates.Count > 0)
                types[candidates[Random.Range(0, candidates.Count)]] = RoomType.MONSTER;

            List<StageRoomInfo> rooms = new List<StageRoomInfo>(cells.Count);
            for (int i = 0; i < cells.Count; i++)
                rooms.Add(new StageRoomInfo(cells[i].x, cells[i].y, types[i], i == 0));
            return rooms;
        }

        static int CountNeighbors(Vector2Int cell, HashSet<Vector2Int> occupied)
        {
            int count = 0;
            foreach (Vector2Int direction in Directions)
                if (occupied.Contains(cell + direction)) count++;
            return count;
        }
    }

    static List<Entry> stages;

    static void EnsureLoaded()
    {
        if (stages != null) return;
        stages = new List<Entry>();
        CsvTable table = CsvTable.LoadFromResources("DB/StageDB");
        foreach (CsvTable.Row row in table.rows)
        {
            string stageNo = row.Get("StageNo");
            if (string.IsNullOrEmpty(stageNo)) continue;
            if (!System.Enum.TryParse(row.Get("Type"), out RoomType roomType))
            {
                Debug.LogError($"[StageData] {stageNo}: Type이 RoomType 이름이 아닙니다 ({row.lineNumber}행)");
                continue;
            }
            if (!int.TryParse(row.Get("Hazard"), out int hazard))
            {
                Debug.LogError($"[StageData] {stageNo}: Hazard가 정수가 아닙니다 ({row.lineNumber}행)");
                continue;
            }
            Entry entry = new Entry {
                stageNo = stageNo,
                name = row.Get("Name"),
                roomType = roomType,
                hazard = hazard,
                description = row.Get("Description"),
            };
            if (table.HasColumn("RoomCount") && int.TryParse(row.Get("RoomCount"), out int roomCount)) entry.roomCount = roomCount;
            if (table.HasColumn("EliteCount") && int.TryParse(row.Get("EliteCount"), out int eliteCount)) entry.eliteCount = eliteCount;
            if (table.HasColumn("EmptyPercent") && int.TryParse(row.Get("EmptyPercent"), out int emptyPercent)) entry.emptyPercent = emptyPercent;
            stages.Add(entry);
        }
        if (stages.Count == 0)
            Debug.LogError("[StageData] StageDB에 유효한 스테이지가 없습니다");
    }

    /// <summary>해금 순서대로 정렬된 전체 스테이지 목록 (읽기 전용)</summary>
    public static IReadOnlyList<Entry> Stages
    {
        get { EnsureLoaded(); return stages; }
    }

    public static int Count
    {
        get { EnsureLoaded(); return stages.Count; }
    }

    public static Entry Get(string stageNo)
    {
        EnsureLoaded();
        return stages.Find(stage => stage.stageNo == stageNo);
    }

    /// <summary>해금 순서 인덱스 (0-base). 없으면 -1</summary>
    public static int IndexOf(string stageNo)
    {
        EnsureLoaded();
        return stages.FindIndex(stage => stage.stageNo == stageNo);
    }
}
