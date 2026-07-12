using System.Collections.Generic;
using UnityEngine;

namespace ProjectD
{
    /// <summary>
    /// 골드버그 다면체(오각형 12개 + 육각형 N개로 구성된 구체)의 기하 정보 생성 유틸리티.
    /// GoldbergSphereBuilder(통합 매쉬)와 SphereMapView(타일별 오브젝트)가 공용으로 사용한다.
    /// 타일 간 인접 그래프(neighbors)를 함께 생성하므로 맵 이동/경로 탐색의 기반 데이터로 쓸 수 있다.
    /// </summary>
    public static class GoldbergSphereGeometry
    {
        /// <summary>타일 하나(구체의 면 하나)의 기하 정보. 좌표는 단위 구 기준(반지름 1).</summary>
        public class Tile
        {
            public int index;
            public bool isPentagon;
            public Vector3 normal;     // 타일 중심 방향 단위 법선
            public Vector3 centerUnit; // 플래튼된 면 중심 (단위 구 스케일)
            public Vector3[] ringUnit; // 플래튼된 꼭짓점 링 (단위 구 스케일)
            public readonly List<int> neighbors = new List<int>(); // 인접 타일 인덱스 (오각형 5개, 육각형 6개)
        }

        // 정이십면체 기본 정점/면
        static readonly Vector3[] s_icoVerts;
        static readonly int[] s_icoFaces =
        {
            0,11,5,  0,5,1,   0,1,7,   0,7,10,  0,10,11,
            1,5,9,   5,11,4,  11,10,2, 10,7,6,  7,1,8,
            3,9,4,   3,4,2,   3,2,6,   3,6,8,   3,8,9,
            4,9,5,   2,4,11,  6,2,10,  8,6,7,   9,8,1
        };

        static GoldbergSphereGeometry()
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            s_icoVerts = new Vector3[]
            {
                new Vector3(-1,  t,  0), new Vector3( 1,  t,  0), new Vector3(-1, -t,  0), new Vector3( 1, -t,  0),
                new Vector3( 0, -1,  t), new Vector3( 0,  1,  t), new Vector3( 0, -1, -t), new Vector3( 0,  1, -t),
                new Vector3( t,  0, -1), new Vector3( t,  0,  1), new Vector3(-t,  0, -1), new Vector3(-t,  0,  1),
            };
            for (int i = 0; i < s_icoVerts.Length; i++)
                s_icoVerts[i] = s_icoVerts[i].normalized;
        }

        /// <summary>
        /// 분할 수에 따른 타일 목록 생성. 총 타일 수 = 10*n*n + 2 (오각형 12개 + 육각형 10*n*n - 10개).
        /// 인덱스 0~11이 오각형(정이십면체 꼭짓점 위치)이다.
        /// </summary>
        public static List<Tile> Generate(int subdivision)
        {
            int n = Mathf.Max(1, subdivision);

            // 1) 정이십면체를 n분할한 측지 구(geodesic sphere) 정점/삼각형 생성
            var geoVerts = new List<Vector3>();
            var keyToIndex = new Dictionary<Vector3Int, int>();
            var geoTris = new List<int>();

            int GetVert(Vector3 p)
            {
                p = p.normalized;
                var key = new Vector3Int(
                    Mathf.RoundToInt(p.x * 100000f),
                    Mathf.RoundToInt(p.y * 100000f),
                    Mathf.RoundToInt(p.z * 100000f));
                if (!keyToIndex.TryGetValue(key, out int idx))
                {
                    idx = geoVerts.Count;
                    geoVerts.Add(p);
                    keyToIndex.Add(key, idx);
                }
                return idx;
            }

            for (int f = 0; f < s_icoFaces.Length; f += 3)
            {
                Vector3 a = s_icoVerts[s_icoFaces[f]];
                Vector3 b = s_icoVerts[s_icoFaces[f + 1]];
                Vector3 c = s_icoVerts[s_icoFaces[f + 2]];

                int[][] grid = new int[n + 1][];
                for (int i = 0; i <= n; i++)
                {
                    grid[i] = new int[n - i + 1];
                    for (int j = 0; j <= n - i; j++)
                        grid[i][j] = GetVert(a + (b - a) * ((float)i / n) + (c - a) * ((float)j / n));
                }
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n - i; j++)
                    {
                        geoTris.Add(grid[i][j]); geoTris.Add(grid[i + 1][j]); geoTris.Add(grid[i][j + 1]);
                        if (j < n - i - 1)
                        {
                            geoTris.Add(grid[i + 1][j]); geoTris.Add(grid[i + 1][j + 1]); geoTris.Add(grid[i][j + 1]);
                        }
                    }
                }
            }

            // 2) 삼각형 무게중심(=쌍대 다면체 타일의 꼭짓점)과 측지 정점별 인접 삼각형 수집
            int triCount = geoTris.Count / 3;
            var centroids = new Vector3[triCount];
            var vertTris = new List<int>[geoVerts.Count];
            for (int v = 0; v < vertTris.Length; v++)
                vertTris[v] = new List<int>(6);

            for (int ti = 0; ti < triCount; ti++)
            {
                int i0 = geoTris[ti * 3], i1 = geoTris[ti * 3 + 1], i2 = geoTris[ti * 3 + 2];
                centroids[ti] = ((geoVerts[i0] + geoVerts[i1] + geoVerts[i2]) / 3f).normalized;
                vertTris[i0].Add(ti);
                vertTris[i1].Add(ti);
                vertTris[i2].Add(ti);
            }

            // 3) 타일 간 인접 그래프: 측지 구의 간선(edge) = 두 타일이 변을 공유한다는 의미
            var edgeSet = new HashSet<long>();
            var edges = new List<Vector2Int>();
            for (int ti = 0; ti < triCount; ti++)
            {
                for (int e = 0; e < 3; e++)
                {
                    int a = geoTris[ti * 3 + e];
                    int b = geoTris[ti * 3 + (e + 1) % 3];
                    long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    if (edgeSet.Add(key))
                        edges.Add(new Vector2Int(a, b));
                }
            }

            // 4) 측지 정점 하나당 타일(오각형 또는 육각형) 하나 생성
            var tiles = new List<Tile>(geoVerts.Count);
            for (int v = 0; v < geoVerts.Count; v++)
            {
                Vector3 nrm = geoVerts[v];
                List<int> adj = vertTris[v];
                int m = adj.Count; // 5 또는 6

                // 접평면 기저
                Vector3 t1 = Vector3.Cross(nrm, Vector3.up);
                if (t1.sqrMagnitude < 1e-6f)
                    t1 = Vector3.Cross(nrm, Vector3.right);
                t1.Normalize();
                Vector3 t2 = Vector3.Cross(nrm, t1);

                // 타일 꼭짓점(무게중심들)을 법선 기준 각도순 정렬
                adj.Sort((x, y) =>
                {
                    float ax = Mathf.Atan2(Vector3.Dot(centroids[x] - nrm, t2), Vector3.Dot(centroids[x] - nrm, t1));
                    float ay = Mathf.Atan2(Vector3.Dot(centroids[y] - nrm, t2), Vector3.Dot(centroids[y] - nrm, t1));
                    return ax.CompareTo(ay);
                });

                // 꼭짓점들을 같은 평면으로 플래튼(납작하게)
                float dAvg = 0f;
                for (int k = 0; k < m; k++)
                    dAvg += Vector3.Dot(centroids[adj[k]], nrm);
                dAvg /= m;

                var ring = new Vector3[m];
                Vector3 center = Vector3.zero;
                for (int k = 0; k < m; k++)
                {
                    Vector3 c = centroids[adj[k]];
                    ring[k] = c + nrm * (dAvg - Vector3.Dot(c, nrm));
                    center += ring[k];
                }
                center /= m;

                tiles.Add(new Tile
                {
                    index = v,
                    isPentagon = (m == 5),
                    normal = nrm,
                    centerUnit = center,
                    ringUnit = ring,
                });
            }

            // 5) 인접 정보 기록
            foreach (Vector2Int edge in edges)
            {
                tiles[edge.x].neighbors.Add(edge.y);
                tiles[edge.y].neighbors.Add(edge.x);
            }

            return tiles;
        }

        /// <summary>
        /// 타일 하나의 납작한 판 형태 매쉬 데이터를 리스트에 덧붙인다. (플랫 셰이딩을 위해 면마다 정점 분리)
        /// spacing은 이웃 타일과의 간격(월드 단위)이며 아포템 기준으로 절반씩 수축시킨다.
        /// </summary>
        public static void AppendTile(Tile tile, float radius, float spacing, float thickness,
            List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
        {
            int m = tile.ringUnit.Length;
            Vector3 nrm = tile.normal;

            Vector3 t1 = Vector3.Cross(nrm, Vector3.up);
            if (t1.sqrMagnitude < 1e-6f)
                t1 = Vector3.Cross(nrm, Vector3.right);
            t1.Normalize();
            Vector3 t2 = Vector3.Cross(nrm, t1);

            var outer = new Vector3[m];
            for (int k = 0; k < m; k++)
                outer[k] = tile.ringUnit[k] * radius;
            Vector3 fc = tile.centerUnit * radius;

            // 아포템(중심-변 거리) 기준으로 spacing의 절반만큼 안쪽으로 수축 → 이웃 타일과 spacing만큼 간격
            float apothem = float.MaxValue;
            for (int k = 0; k < m; k++)
            {
                Vector3 mid = (outer[k] + outer[(k + 1) % m]) * 0.5f;
                apothem = Mathf.Min(apothem, (mid - fc).magnitude);
            }
            apothem = Mathf.Max(apothem, 1e-5f);
            // 최소 5%는 유지: 0까지 수축하면 퇴화 매쉬가 되어 MeshCollider 쿠킹이 실패한다
            float scale = Mathf.Clamp(1f - (spacing * 0.5f) / apothem, 0.05f, 1f);

            int baseIdx = vertices.Count;
            Vector3 inward = -nrm * thickness;

            // 바깥면: 중심 + 링
            vertices.Add(fc);
            uvs.Add(new Vector2(0.5f, 0.5f));
            for (int k = 0; k < m; k++)
            {
                Vector3 p = fc + (outer[k] - fc) * scale;
                vertices.Add(p);
                Vector3 d = p - fc;
                uvs.Add(new Vector2(Vector3.Dot(d, t1), Vector3.Dot(d, t2)) * (0.5f / apothem) + new Vector2(0.5f, 0.5f));
            }
            // 안쪽면: 중심 + 링
            vertices.Add(fc + inward);
            uvs.Add(new Vector2(0.5f, 0.5f));
            for (int k = 0; k < m; k++)
            {
                vertices.Add(vertices[baseIdx + 1 + k] + inward);
                uvs.Add(uvs[baseIdx + 1 + k]);
            }

            int oc = baseIdx;          // 바깥면 중심
            int or0 = baseIdx + 1;     // 바깥면 링 시작
            int ic = baseIdx + m + 1;  // 안쪽면 중심
            int ir0 = baseIdx + m + 2; // 안쪽면 링 시작

            for (int k = 0; k < m; k++)
            {
                int k1 = (k + 1) % m;
                // 바깥면 캡 (법선: 구 바깥 방향)
                triangles.Add(oc); triangles.Add(or0 + k); triangles.Add(or0 + k1);
                // 안쪽면 캡 (법선: 구 중심 방향)
                triangles.Add(ic); triangles.Add(ir0 + k1); triangles.Add(ir0 + k);
            }

            // 옆면 (변마다 독립 사각형)
            for (int k = 0; k < m; k++)
            {
                int k1 = (k + 1) % m;
                int sv = vertices.Count;
                Vector3 ok0 = vertices[or0 + k];
                Vector3 ok1 = vertices[or0 + k1];
                vertices.Add(ok0);           // sv+0: 바깥 k
                vertices.Add(ok1);           // sv+1: 바깥 k+1
                vertices.Add(ok0 + inward);  // sv+2: 안쪽 k
                vertices.Add(ok1 + inward);  // sv+3: 안쪽 k+1
                float u0 = (float)k / m;
                float u1 = (float)(k + 1) / m;
                uvs.Add(new Vector2(u0, 1f));
                uvs.Add(new Vector2(u1, 1f));
                uvs.Add(new Vector2(u0, 0f));
                uvs.Add(new Vector2(u1, 0f));

                triangles.Add(sv); triangles.Add(sv + 2); triangles.Add(sv + 3);
                triangles.Add(sv); triangles.Add(sv + 3); triangles.Add(sv + 1);
            }
        }

        /// <summary>타일 하나짜리 독립 매쉬 생성 (SphereMapView의 타일별 오브젝트용, 구체 로컬 좌표 기준)</summary>
        public static Mesh BuildTileMesh(Tile tile, float radius, float spacing, float thickness)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            AppendTile(tile, radius, spacing, thickness, vertices, uvs, triangles);

            var mesh = new Mesh
            {
                name = (tile.isPentagon ? "PentagonTile_" : "HexagonTile_") + tile.index,
                hideFlags = HideFlags.DontSave
            };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
