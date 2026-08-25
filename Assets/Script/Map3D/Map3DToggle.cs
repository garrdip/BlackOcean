using System.Collections;
using UnityEngine;

namespace ProjectD
{
    /// <summary>
    /// 2D 육각형 맵 ↔ 3D 구체 맵 전환 테스트 버튼.
    /// 3D 구체 맵(SphereMapView)은 Map(MapScene) 오브젝트 하위에 있으므로 맵 페이즈 표시/숨김을 그대로 따라가고,
    /// 이 토글은 Map 하위의 2D 전용 요소들(Grid, MapRooms, MapPathLines 등)만 3D 뷰와 교대로 켜고 끈다.
    /// 2D 요소들의 활성 상태는 게임 로직이 관리하므로, 3D로 전환할 때 상태를 기억했다가 복원한다.
    /// </summary>
    public class Map3DToggle : MonoBehaviour
    {
        [Tooltip("3D 구체 맵 뷰 (Map 오브젝트 하위)")]
        public SphereMapView sphereView;

        [Tooltip("3D 모드에서 숨길 2D 맵 전용 요소들 (Grid, MapRooms, MapPathLines, MapUI 등)")]
        public GameObject[] legacy2DObjects;

        [Tooltip("시작 시 3D 맵으로 표시할지 여부")]
        public bool startWith3D = false;

        bool _showing3D;
        bool[] _legacyWasActive;

        IEnumerator Start()
        {
            // USE_3D_MAP=0(BalanceDB)이면 씬 설정과 무관하게 2D 육각형 맵으로 시작
            if (!SphereMapNetwork.Use3DMap)
                startWith3D = false;
            if (sphereView != null && !startWith3D)
                sphereView.gameObject.SetActive(false);
            if (startWith3D)
                Show3D();

            // M_MapManager는 네트워크 싱글톤이라 씬 로드 직후에는 아직 준비되지 않았을 수 있으므로 대기
            while (M_MapManager.instance == null || M_MapManager.instance.BattleScene == null)
                yield return null;

            // 게임 시작은 맵 화면이므로 전투 화면(Game)은 꺼진 상태로 시작.
            // 원래 씬 파일에는 Map/Game 둘 다 활성으로 저장되어 있는데, 예전에는 2D 맵 스프라이트가
            // 화면을 덮어 가려져 있었지만 3D 맵에서는 뒤가 드러나므로 여기서 정리한다.
            // (첫 전투 진입 시 ChangeBattleScene이 다시 켜준다)
            M_MapManager.instance.BattleScene.SetActive(false);
        }

        // 우상단 "3D 맵 보기 (테스트)" OnGUI 버튼은 제거됨 — 3D 맵이 기본(startWith3D)이 되었고
        // 해당 자리를 스킬트리 버튼이 사용한다. 2D 복귀가 필요하면 인스펙터에서 Toggle()을 직접 호출할 것.
        public void Toggle()
        {
            if (_showing3D)
                Show2D();
            else
                Show3D();
        }

        void Show3D()
        {
            if (legacy2DObjects != null)
            {
                _legacyWasActive = new bool[legacy2DObjects.Length];
                for (int i = 0; i < legacy2DObjects.Length; i++)
                {
                    if (legacy2DObjects[i] == null)
                        continue;
                    _legacyWasActive[i] = legacy2DObjects[i].activeSelf; // 복원을 위해 현재 상태 기억
                    legacy2DObjects[i].SetActive(false);
                }
            }
            if (sphereView != null)
            {
                sphereView.gameObject.SetActive(true);
                sphereView.PlaceInFrontOfCamera();
            }
            _showing3D = true;
        }

        void Show2D()
        {
            if (legacy2DObjects != null && _legacyWasActive != null)
            {
                for (int i = 0; i < legacy2DObjects.Length && i < _legacyWasActive.Length; i++)
                {
                    if (legacy2DObjects[i] != null)
                        legacy2DObjects[i].SetActive(_legacyWasActive[i]); // 숨기기 전 상태 그대로 복원
                }
            }
            if (sphereView != null)
                sphereView.gameObject.SetActive(false);
            _showing3D = false;
        }
    }
}
