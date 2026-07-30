using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectD
{
    /// <summary>
    /// 오각형/육각형 타일로 구성된 구체(골드버그 다면체) 통합 매쉬 생성기.
    /// 인스펙터 값 변경이 에디터에서 실시간으로 반영된다.
    /// - subdivision(분할 수)을 올리면 육각형 개수가 늘어난다. 오각형은 항상 12개. (총 면 수 = 10*n*n + 2)
    /// - spacing은 이웃 타일 사이의 간격(월드 단위). 각 타일이 자기 중심으로 수축해 구 형태는 유지된다.
    /// 서브매쉬 0 = 오각형, 서브매쉬 1 = 육각형 (머티리얼 2개로 색 구분 가능)
    /// 기하 계산은 GoldbergSphereGeometry 공용 클래스를 사용한다.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GoldbergSphereBuilder : MonoBehaviour
    {
        [Header("구성")]
        [Tooltip("분할 수. 1 = 오각형 12개(정십이면체), 2 = 42면, 3 = 92면 ... (총 면 수 = 10*n*n + 2). 최대 8.")]
        [Range(1, 8)] public int subdivision = 2;

        [Header("형태")]
        [Tooltip("구체 반지름")]
        [Min(0.01f)] public float radius = 3f;
        [Tooltip("타일 사이 간격 (월드 단위)")]
        [Min(0f)] public float spacing = 0.05f;
        [Tooltip("타일 두께 (구 안쪽 방향)")]
        [Min(0.001f)] public float thickness = 0.1f;

        [Header("정보 (읽기 전용)")]
        public int pentagonCount;
        public int hexagonCount;

        bool _dirty;
        Mesh _mesh;

        void OnEnable()
        {
            Rebuild();
        }

        void OnValidate()
        {
            _dirty = true;
#if UNITY_EDITOR
            // OnValidate 안에서 매쉬를 직접 만지면 경고가 발생하므로 한 프레임 지연
            if (!Application.isPlaying)
                UnityEditor.EditorApplication.delayCall += EditorDelayedRebuild;
#endif
        }

#if UNITY_EDITOR
        void EditorDelayedRebuild()
        {
            UnityEditor.EditorApplication.delayCall -= EditorDelayedRebuild;
            if (this == null || !_dirty)
                return;
            Rebuild();
        }
#endif

        void Update()
        {
            // 플레이 중 코드로 필드를 바꾼 뒤 _dirty를 세우는 경우 대응
            if (_dirty)
                Rebuild();
        }

        /// <summary>런타임에서 필드 변경 후 호출하면 매쉬가 다시 생성된다.</summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            _dirty = false;
            EnsureMesh();

            List<GoldbergSphereGeometry.Tile> tiles = GoldbergSphereGeometry.Generate(subdivision);

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var pentTris = new List<int>();
            var hexTris = new List<int>();

            foreach (GoldbergSphereGeometry.Tile tile in tiles)
            {
                GoldbergSphereGeometry.AppendTile(tile, radius, spacing, thickness,
                    vertices, uvs, tile.isPentagon ? pentTris : hexTris);
            }

            // 매쉬 확정 (서브매쉬 0 = 오각형, 1 = 육각형)
            _mesh.Clear();
            _mesh.indexFormat = IndexFormat.UInt32;
            _mesh.SetVertices(vertices);
            _mesh.SetUVs(0, uvs);
            _mesh.subMeshCount = 2;
            _mesh.SetTriangles(pentTris, 0);
            _mesh.SetTriangles(hexTris, 1);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            pentagonCount = 12;
            hexagonCount = tiles.Count - 12;

#if UNITY_EDITOR
            // 머티리얼 슬롯이 비어 있으면 기본 머티리얼로 2개 채움
            var mr = GetComponent<MeshRenderer>();
            var mats = mr.sharedMaterials;
            if (mats == null || mats.Length < 2 || mats[0] == null || mats[1] == null)
            {
                var def = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
                var newMats = new Material[2];
                newMats[0] = (mats != null && mats.Length > 0 && mats[0] != null) ? mats[0] : def;
                newMats[1] = (mats != null && mats.Length > 1 && mats[1] != null) ? mats[1] : def;
                mr.sharedMaterials = newMats;
            }
#endif
        }

        void EnsureMesh()
        {
            if (_mesh == null)
            {
                _mesh = new Mesh
                {
                    name = "GoldbergSphere",
                    // 씬 파일에 매쉬가 저장되지 않도록 함 (OnEnable에서 항상 재생성)
                    hideFlags = HideFlags.DontSave
                };
            }
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }
    }
}
