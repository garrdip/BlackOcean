using UnityEngine;

namespace ProjectD
{
    /// <summary>
    /// 납작한 정다각형(정오각형/정육각형) 매쉬 생성 유틸리티.
    /// 구체(깎은 정이십면체, 축구공 형태) 조립을 위해 변 길이를 기준으로 크기를 통일한다.
    /// 피벗은 다각형 중심이며, 윗면이 +Y 방향을 향한다.
    /// </summary>
    public static class FlatPolygonMeshGenerator
    {
        /// <summary>변 길이 기준으로 납작한 정육각형 매쉬 생성</summary>
        public static Mesh CreateHexagon(float edgeLength, float thickness)
        {
            return Create(6, edgeLength, thickness);
        }

        /// <summary>변 길이 기준으로 납작한 정오각형 매쉬 생성</summary>
        public static Mesh CreatePentagon(float edgeLength, float thickness)
        {
            return Create(5, edgeLength, thickness);
        }

        /// <param name="sides">변의 개수 (5 = 정오각형, 6 = 정육각형)</param>
        /// <param name="edgeLength">변 길이</param>
        /// <param name="thickness">두께 (Y축 방향)</param>
        public static Mesh Create(int sides, float edgeLength, float thickness)
        {
            // 정다각형 외접원 반지름 = 변길이 / (2 * sin(PI / n))
            float radius = edgeLength / (2f * Mathf.Sin(Mathf.PI / sides));
            float halfT = thickness * 0.5f;

            // XZ 평면상의 꼭짓점 링 (첫 꼭짓점이 +Z 방향)
            Vector3[] ring = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float angle = (Mathf.PI * 0.5f) + (Mathf.PI * 2f * i / sides);
                ring[i] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }

            // 플랫 셰이딩을 위해 윗면/아랫면/옆면 정점을 모두 분리
            int capVerts = sides + 1;                       // 중심 1 + 링
            Vector3[] vertices = new Vector3[capVerts * 2 + sides * 4];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[sides * 6 + sides * 6]; // 캡 2개 + 옆면

            int topBase = 0;
            int bottomBase = capVerts;
            int sideBase = capVerts * 2;

            // 윗면 정점
            vertices[topBase] = new Vector3(0f, halfT, 0f);
            uvs[topBase] = new Vector2(0.5f, 0.5f);
            // 아랫면 정점
            vertices[bottomBase] = new Vector3(0f, -halfT, 0f);
            uvs[bottomBase] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < sides; i++)
            {
                Vector2 capUV = new Vector2(ring[i].x / (radius * 2f) + 0.5f, ring[i].z / (radius * 2f) + 0.5f);
                vertices[topBase + 1 + i] = ring[i] + Vector3.up * halfT;
                uvs[topBase + 1 + i] = capUV;
                vertices[bottomBase + 1 + i] = ring[i] + Vector3.down * halfT;
                uvs[bottomBase + 1 + i] = capUV;
            }

            int t = 0;
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;

                // 윗면 (법선 +Y, 위에서 볼 때 시계 방향)
                triangles[t++] = topBase;
                triangles[t++] = topBase + 1 + next;
                triangles[t++] = topBase + 1 + i;

                // 아랫면 (법선 -Y)
                triangles[t++] = bottomBase;
                triangles[t++] = bottomBase + 1 + i;
                triangles[t++] = bottomBase + 1 + next;
            }

            // 옆면 (엣지마다 독립된 사각형)
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                int v = sideBase + i * 4;

                Vector3 botA = ring[i] + Vector3.down * halfT;
                Vector3 botB = ring[next] + Vector3.down * halfT;
                Vector3 topA = ring[i] + Vector3.up * halfT;
                Vector3 topB = ring[next] + Vector3.up * halfT;

                vertices[v + 0] = botA;
                vertices[v + 1] = botB;
                vertices[v + 2] = topA;
                vertices[v + 3] = topB;

                float u0 = (float)i / sides;
                float u1 = (float)(i + 1) / sides;
                uvs[v + 0] = new Vector2(u0, 0f);
                uvs[v + 1] = new Vector2(u1, 0f);
                uvs[v + 2] = new Vector2(u0, 1f);
                uvs[v + 3] = new Vector2(u1, 1f);

                // 바깥쪽을 향하는 두 삼각형
                triangles[t++] = v + 0; // botA
                triangles[t++] = v + 2; // topA
                triangles[t++] = v + 3; // topB

                triangles[t++] = v + 0; // botA
                triangles[t++] = v + 3; // topB
                triangles[t++] = v + 1; // botB
            }

            Mesh mesh = new Mesh();
            mesh.name = sides + "gonFlatMesh";
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
