using System.Collections.Generic;
using UnityEngine;

namespace ProjectD
{
    /// <summary>
    /// 3D 구체 맵의 타일 하나(방 하나). SphereMapView가 생성하고 SphereMapSystem이 방 데이터를 관리한다.
    /// neighbors에 인접 타일 인덱스가 들어 있어 경로 탐색(A*/BFS)의 기반으로 사용한다.
    /// </summary>
    public class SphereMapTile : MonoBehaviour
    {
        public int index;
        public bool isPentagon;             // true면 이동 불가 지역
        public Vector3 normal;              // 구체 로컬 기준 타일 법선 (방사형 상승 방향)
        public Vector3 center;              // 구체 로컬 기준 타일 면 중심 (아이콘 배치용)
        public readonly List<int> neighbors = new List<int>(); // 인접 타일 인덱스 (오각형 5개, 육각형 6개)

        [Header("방 데이터 (SphereMapSystem이 설정)")]
        public RoomType roomType = RoomType.UNDEFINED;
        public bool isActiveRoom;           // 시야에 밝혀진 방인지 (2D의 isActive 대응)
        public int hazard;                  // 위험도 (시작 방 기준 BFS 홉 거리)
        public bool highlight;              // true면 포커스 시에도 어두워지지 않음 (경로 표시용)

        public MeshRenderer Renderer { get; private set; }
        public Color baseColor;
        public SpriteRenderer iconRenderer; // 방 타입 아이콘 (SphereMapSystem이 생성)

        public void Init(int index, bool isPentagon, Vector3 normal, Vector3 center,
            List<int> neighbors, MeshRenderer renderer, Color baseColor)
        {
            this.index = index;
            this.isPentagon = isPentagon;
            this.normal = normal;
            this.center = center;
            this.neighbors.Clear();
            this.neighbors.AddRange(neighbors);
            Renderer = renderer;
            this.baseColor = baseColor;
        }
    }
}
