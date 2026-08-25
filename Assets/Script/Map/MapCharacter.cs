using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectD
{
    /// <summary>
    /// 2D 맵 탐험용 파티 캐릭터 비주얼 (클라이언트 전용 — 네트워크 오브젝트 아님).
    /// 빈땅 즉시 이동 시 서버가 보낸 경로(RpcMovePartyAlongPath)를 따라 타일을 하나씩 걸어가고,
    /// 걷는 중이 아닐 때는 항상 현재 방(currentRoom) 위치로 스냅해 투표 이동/전투 복귀/로드와도 동기화된다.
    /// 스프라이트는 임시로 에리스 전신 일러스트(Resources/Map/MapCharacter) 사용 — 정식 맵 캐릭터 아트 나오면 교체.
    /// </summary>
    public class MapCharacter : MonoBehaviour
    {
        public static MapCharacter instance;

        public const float MoveSpeed = 2.6f;  // 초당 이동 거리 (월드 단위) — 서버의 이동 잠금 시간 계산에도 사용
        public const float StepPause = 0.08f; // 타일 하나 이동(홉)이 끝난 뒤 다음 타일로 가기 전 정지 시간
        const float HopHeight = 0.07f;        // 타일 간 이동 홉 높이
        static readonly Vector3 FaceOffset = new Vector3(0f, 0.05f, 0f); // 타일 면 위에 서는 오프셋

        Coroutine walkRoutine;
        public bool IsWalking => walkRoutine != null;

        /// <summary>맵 캐릭터 비주얼을 찾거나 생성 (MapScene 하위 — 전투 전환 시 함께 숨겨진다)</summary>
        public static MapCharacter EnsureExists()
        {
            if (instance != null) return instance;
            GameObject characterObject = new GameObject("MapCharacter");
            if (M_MapManager.instance != null && M_MapManager.instance.MapScene != null)
                characterObject.transform.SetParent(M_MapManager.instance.MapScene.transform, false);
            instance = characterObject.AddComponent<MapCharacter>();
            return instance;
        }

        void OnDisable()
        {
            // 전투 전환 등으로 비활성화되면 코루틴이 중단되므로 상태를 정리 — 복귀 시 LateUpdate가 스냅 동기화
            walkRoutine = null;
        }

        void Awake()
        {
            instance = this;
            SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Resources.Load<Sprite>("Map/MapCharacter");
            // 타일보다 위에 그려지는 기존 말 레이어 재사용.
            // 주의: MapDimCanvas(블러)의 planeDistance가 1로 당겨지면 Z기록 때문에 이 레이어 이상의
            // 월드 스프라이트가 전부 잘린다 — 딤 캔버스는 반드시 planeDistance 100 유지 (GameScene)
            spriteRenderer.sortingLayerName = "MapPlayerPiece";
            spriteRenderer.sortingOrder = 100;
            if (spriteRenderer.sprite == null)
                Debug.LogError("[MapCharacter] 스프라이트 로드 실패 — Resources/Map/MapCharacter");
        }

        int mismatchFrames; // currentRoom과 위치가 어긋난 채 지난 프레임 수

        void LateUpdate()
        {
            // 걷는 중이 아니면 현재 방 위치로 동기화 (투표 이동/전투 복귀/이어서 하기 대응).
            // 단, 즉시 이동 시 호스트에서는 currentRoom(SyncVar)이 걷기 RPC보다 먼저 반영되므로
            // 몇 프레임 기다려 걷기가 시작되지 않을 때만 스냅한다 (순간이동 방지)
            if (walkRoutine != null || M_MapManager.instance == null || M_MapManager.instance.currentRoom == null)
            {
                mismatchFrames = 0;
                return;
            }
            Vector3 targetPosition = M_MapManager.instance.currentRoom.transform.position + FaceOffset;
            if ((transform.position - targetPosition).sqrMagnitude < 0.0004f)
            {
                mismatchFrames = 0;
                return;
            }
            if (++mismatchFrames >= 10)
            {
                transform.position = targetPosition;
                mismatchFrames = 0;
            }
        }

        /// <summary>경로(타일 위치 목록)를 따라 순서대로 걸어간다. 걷는 중 재호출되면 현재 위치에서 새 경로로 갱신</summary>
        public void MoveAlong(List<Vector3> waypoints)
        {
            if (waypoints == null || waypoints.Count == 0) return;
            if (walkRoutine != null) StopCoroutine(walkRoutine);
            walkRoutine = StartCoroutine(Walk(waypoints));
        }

        // 타일 하나당 홉(점프) 하나 — 홉이 완전히 끝나야 다음 타일로 이동한다
        IEnumerator Walk(List<Vector3> waypoints)
        {
            Vector3 from = transform.position;
            foreach (Vector3 rawTarget in waypoints)
            {
                Vector3 to = rawTarget + FaceOffset;
                float distance = Vector3.Distance(from, to);
                if (!Mathf.Approximately(to.x, from.x))
                {
                    // 진행 방향으로 좌우 반전
                    Vector3 scale = transform.localScale;
                    scale.x = Mathf.Abs(scale.x) * (to.x >= from.x ? 1f : -1f);
                    transform.localScale = scale;
                }
                float progress = 0f;
                while (progress < 1f)
                {
                    progress = Mathf.Min(1f, progress + Time.deltaTime * MoveSpeed / Mathf.Max(0.0001f, distance));
                    Vector3 position = Vector3.Lerp(from, to, progress);
                    position.y += Mathf.Sin(progress * Mathf.PI) * HopHeight; // 타일당 홉 1회
                    transform.position = position;
                    yield return null;
                }
                transform.position = to; // 착지 확정
                from = to;
                yield return new WaitForSeconds(StepPause); // 이동(홉) 완료 후 다음 타일로
            }
            walkRoutine = null;
        }
    }
}
