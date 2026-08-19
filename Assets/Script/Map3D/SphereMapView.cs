using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace ProjectD
{
    /// <summary>
    /// 3D 구체 맵 뷰 (2D 육각형 맵의 3D 리뉴얼 테스트).
    /// - 골드버그 구체를 타일별 오브젝트(MeshCollider 포함)로 생성
    /// - 마우스 왼쪽 드래그: 지구본처럼 회전
    /// - 육각형 타일 클릭: 방사형(법선) 방향으로 상승 + 포커싱, 나머지 타일은 어둡게 (2D 맵의 확장+블러 연출 대응)
    /// - 오각형 타일: 이동 불가 지역 (어두운 색, 클릭 무시)
    /// - 빈 공간 클릭: 포커스 해제
    /// 타일 인접 그래프는 각 SphereMapTile.neighbors에 들어 있다.
    /// </summary>
    [ExecuteAlways]
    public class SphereMapView : MonoBehaviour
    {
        [Header("구체 구성")]
        [Tooltip("분할 수. 총 면 수 = 10*n*n + 2 (오각형 12개는 이동 불가 지역). 최대 16 고정.")]
        [Range(1, 16)] public int subdivision = 16;
        [Min(0.01f)] public float radius = 5f;
        [Tooltip("타일 사이 간격 (월드 단위)")]
        [Min(0f)] public float spacing = 0.06f;
        [Min(0.001f)] public float thickness = 0.15f;

        [Header("타일 색상")]
        [Tooltip("비워두면 Standard 셰이더 머티리얼을 자동 생성")]
        public Material tileMaterial;
        public Color hexagonColor = new Color(0.85f, 0.85f, 0.85f);
        public Color pentagonColor = new Color(0.16f, 0.16f, 0.2f);
        [Tooltip("모든 타일 사이 홈을 채우는 테두리 색 (거점지역 외곽선과 같은 상감 방식)")]
        public Color tileBorderColor = new Color(0.10f, 0.10f, 0.12f);
        [Tooltip("타일 테두리를 표면에서 가라앉히는 깊이 (월드 단위). 0이면 표면과 같은 높이가 되어 거점지역 외곽선과 겹칠 수 있다")]
        [Min(0f)] public float tileBorderInset = 0.045f;
        [Tooltip("타일 사이 홈의 발광 색 (HDR). 카메라 블룸과 함께 홈이 붉게 빛나는 네온 효과를 만든다. 검정이면 발광 없음")]
        [ColorUsage(false, true)] public Color tileBorderEmission = new Color(4.5f, 0.12f, 0.08f, 1f);

        [Header("포커스 연출")]
        [Tooltip("선택된 타일이 방사형으로 상승하는 높이")]
        [Min(0f)] public float focusHeight = 0.6f;
        [Tooltip("상승/복귀 애니메이션 시간 (2D 맵의 expandDuration과 동일값 권장)")]
        [Min(0.01f)] public float focusDuration = 0.5f;
        [Tooltip("포커스 시 나머지 타일이 어두워지는 정도 (2D 맵의 블러 패널 대응)")]
        [Range(0f, 1f)] public float unfocusedDim = 0.75f;

        [Header("회전")]
        [Tooltip("드래그 1픽셀당 회전 각도")]
        public float rotateSpeed = 0.25f;
        [Tooltip("이 픽셀 이상 움직이면 클릭이 아니라 드래그(회전)로 판정")]
        public float dragThreshold = 6f;

        [Header("입력")]
        [Tooltip("uGUI 위에서 시작된 클릭을 무시할지 여부. 전체 화면을 덮는 캔버스가 있는 씬(GameScene 등)에서는 꺼야 입력이 동작한다.")]
        public bool blockWhenPointerOverUI = false;

        [Header("줌 (마우스 스크롤)")]
        [Tooltip("스크롤 1틱당 줌 변화량")]
        public float zoomSpeed = 0.15f;
        [Min(0.1f)] public float zoomMin = 1f;
        [Min(0.1f)] public float zoomMax = 3f;

        [Header("조명")]
        [Tooltip("씬에 조명이 없어도 보이도록 전용 디렉셔널 라이트 생성 (회전과 무관하게 고정)")]
        public bool createLight = true;

        public IReadOnlyList<SphereMapTile> Tiles => _tiles;
        public int FocusedIndex => _focusedIndex;
        /// <summary>타일들이 담긴 회전 루트 (보스 말 등 구체 위 오브젝트의 부모로 사용)</summary>
        public Transform TileRoot => _tileRoot;

        /// <summary>타일 클릭 시 호출. 지정하면 기본 동작(포커스 토글) 대신 이 콜백이 처리한다. (SphereMapSystem이 사용)</summary>
        public System.Action<SphereMapTile> OnTileClicked;
        /// <summary>빈 공간 클릭 시 호출. 지정하면 기본 동작(포커스 해제) 대신 이 콜백이 처리한다.</summary>
        public System.Action OnEmptySpaceClicked;
        /// <summary>마우스가 올라간 타일이 바뀔 때 호출. 타일에서 벗어나면 null 전달. (SphereMapSystem이 거점지역 팝업에 사용)</summary>
        public System.Action<SphereMapTile> OnTileHovered;
        /// <summary>Rebuild 완료 후 호출 (타일이 모두 새로 생성됨)</summary>
        public event System.Action OnRebuilt;

        readonly List<SphereMapTile> _tiles = new List<SphereMapTile>();
        Transform _tileRoot;
        Material _runtimeMaterial;
        Material _borderMaterial;                      // 발광(_EMISSION) 켠 테두리 전용 머티리얼
        List<GoldbergSphereGeometry.Tile> _geoTiles;   // 타일 테두리 재생성용 기하 캐시
        GameObject _tileBordersGo;                     // 타일 사이 홈을 채우는 테두리 메쉬 오브젝트
        HashSet<long> _borderExcludedEdges;            // 거점지역 외곽선이 덮어 테두리에서 제외할 변
        HashSet<int> _borderExcludedTris;              // 거점지역 외곽선이 덮어 테두리에서 제외할 접합부
        int _focusedIndex = -1;
        float _dimValue; // 0 = 전체 밝음, 1 = 포커스 외 타일 어두움
        Tween _dimTween;
        Tween _alignTween; // 선택 타일을 화면 중앙(카메라 정면)으로 정렬하는 회전/이동 트윈

        bool _dirty;
        Vector3 _mouseDownPos;
        Vector3 _lastMousePos;
        bool _mouseHeld;
        bool _dragging;
        int _hoveredIndex = -1;
        float _zoom = 1f;
        Vector3 _basePosition;

        static MaterialPropertyBlock s_mpb;

        // ------------------------------------------------------------ Unity Lifecycle --------------------------------------------------------------- //

        void OnEnable()
        {
            Rebuild();
            // 맵 페이즈 진입 등으로 다시 활성화될 때마다 카메라 정면에 재배치 (줌/위치 초기화 포함)
            if (Application.isPlaying)
                PlaceInFrontOfCamera();
        }

        void OnDisable()
        {
            _dimTween?.Kill();
            _alignTween?.Kill();
            foreach (SphereMapTile tile in _tiles)
            {
                if (tile != null)
                    tile.transform.DOKill();
            }
            // 맵이 닫힐 때 호버 상태 해제 (거점지역 팝업이 남지 않도록)
            if (_hoveredIndex != -1)
            {
                _hoveredIndex = -1;
                OnTileHovered?.Invoke(null);
            }
        }

        void OnValidate()
        {
            _dirty = true;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorApplication.delayCall += EditorDelayedRebuild;
#endif
        }

#if UNITY_EDITOR
        void EditorDelayedRebuild()
        {
            UnityEditor.EditorApplication.delayCall -= EditorDelayedRebuild;
            if (this == null || !_dirty || !isActiveAndEnabled)
                return;
            Rebuild();
        }
#endif

        void Update()
        {
            if (_dirty)
                Rebuild();
            if (Application.isPlaying)
                HandleInput();
        }

        // ------------------------------------------------------------ Build --------------------------------------------------------------- //

        /// <summary>구체 타일들을 다시 생성한다. (포커스/회전 상태 초기화)</summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            _dirty = false;
            _focusedIndex = -1;
            _dimValue = 0f;
            _dimTween?.Kill();
            _alignTween?.Kill();

            EnsureTileRoot();
            EnsureLight();
            Material material = EnsureMaterial();

            // 기존 타일 제거 (생성한 매쉬도 함께 파괴하여 누수 방지)
            for (int i = _tileRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = _tileRoot.GetChild(i).gameObject;
                var meshFilter = child.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                    DestroyImmediate(meshFilter.sharedMesh);
                DestroyImmediate(child);
            }
            _tiles.Clear();

            List<GoldbergSphereGeometry.Tile> tiles = GoldbergSphereGeometry.Generate(subdivision);
            _geoTiles = tiles;
            foreach (GoldbergSphereGeometry.Tile tileData in tiles)
            {
                var go = new GameObject((tileData.isPentagon ? "Pent_" : "Hex_") + tileData.index.ToString("D3"));
                go.hideFlags = HideFlags.DontSave; // 씬 저장에서 제외 (OnEnable에서 항상 재생성)
                go.transform.SetParent(_tileRoot, false);

                Mesh mesh = GoldbergSphereGeometry.BuildTileMesh(tileData, radius, spacing, thickness);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var meshRenderer = go.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = material;
                go.AddComponent<MeshCollider>().sharedMesh = mesh;

                var tile = go.AddComponent<SphereMapTile>();
                tile.Init(tileData.index, tileData.isPentagon, tileData.normal, tileData.centerUnit * radius,
                    tileData.neighbors, meshRenderer, tileData.isPentagon ? pentagonColor : hexagonColor);
                _tiles.Add(tile);
            }
            BuildTileBorders(tiles);
            ApplyDimToAll();
            OnRebuilt?.Invoke();
        }

        // 모든 타일 사이 홈을 채우는 테두리 메쉬 생성. tileBorderInset만큼 가라앉혀
        // 표면 높이에 그려지는 거점지역 외곽선이 위에 보이게 한다. (콜라이더 없음 — 입력에 영향 없음)
        // 거점지역 외곽선이 덮는 변·접합부(_borderExcluded*)에는 그리지 않는다.
        void BuildTileBorders(List<GoldbergSphereGeometry.Tile> tiles)
        {
            Mesh mesh = GoldbergSphereGeometry.BuildAllBordersMesh(tiles, radius, spacing, tileBorderInset,
                _borderExcludedEdges, _borderExcludedTris);
            if (mesh == null)
                return;
            var go = new GameObject("TileBorders");
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(_tileRoot, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = EnsureBorderMaterial();
            if (s_mpb == null)
                s_mpb = new MaterialPropertyBlock();
            s_mpb.Clear();
            s_mpb.SetColor("_Color", tileBorderColor);
            s_mpb.SetColor("_EmissionColor", tileBorderEmission); // HDR 발광 — 블룸이 이 값을 받아 홈이 빛난다
            meshRenderer.SetPropertyBlock(s_mpb);
            _tileBordersGo = go;
        }

        // 테두리 전용 머티리얼: 타일과 달리 _EMISSION 키워드가 켜져 있어야 MPB의 _EmissionColor가 적용된다.
        // (키워드는 MaterialPropertyBlock으로 제어할 수 없으므로 머티리얼을 분리)
        Material EnsureBorderMaterial()
        {
            if (_borderMaterial == null)
            {
                _borderMaterial = new Material(Shader.Find("Standard"))
                {
                    name = "SphereMapBorderMaterial",
                    hideFlags = HideFlags.DontSave,
                };
                _borderMaterial.EnableKeyword("_EMISSION");
                _borderMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                // 스페큘러/반사가 흰색 하이라이트로 발광 색을 씻어내지 않게 차단 (홈은 순수 발광 색만 보이게)
                _borderMaterial.SetFloat("_Glossiness", 0f);
                _borderMaterial.SetFloat("_SpecularHighlights", 0f);
                _borderMaterial.SetFloat("_GlossyReflections", 0f);
                _borderMaterial.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                _borderMaterial.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            }
            return _borderMaterial;
        }

        /// <summary>
        /// 거점지역 외곽선이 덮는 변·접합부를 타일 테두리에서 제외하고 테두리 메쉬만 다시 만든다.
        /// (SphereMapSystem이 거점지역 외곽선을 만들 때 호출. 겹쳐서 삐져나와 보이는 것 방지)
        /// </summary>
        public void SetTileBorderExclusions(HashSet<long> excludedEdges, HashSet<int> excludedTris)
        {
            _borderExcludedEdges = excludedEdges;
            _borderExcludedTris = excludedTris;
            if (_geoTiles == null || _tileRoot == null)
                return;
            if (_tileBordersGo != null)
            {
                var meshFilter = _tileBordersGo.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                    DestroyImmediate(meshFilter.sharedMesh);
                DestroyImmediate(_tileBordersGo);
                _tileBordersGo = null;
            }
            BuildTileBorders(_geoTiles);
        }

        void EnsureTileRoot()
        {
            if (_tileRoot != null)
                return;
            Transform existing = transform.Find("TileRoot");
            if (existing != null)
            {
                _tileRoot = existing;
                return;
            }
            var go = new GameObject("TileRoot");
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(transform, false);
            _tileRoot = go.transform;
        }

        void EnsureLight()
        {
            if (!createLight)
                return;
            // 라이트는 회전하는 TileRoot가 아니라 루트에 직접 붙여 드래그 회전과 무관하게 고정
            if (transform.Find("SphereLight") != null)
                return;
            var lightGo = new GameObject("SphereLight");
            lightGo.hideFlags = HideFlags.DontSave;
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.98f, 0.94f);
            light.shadows = LightShadows.None;
        }

        Material EnsureMaterial()
        {
            if (tileMaterial != null)
                return tileMaterial;
            if (_runtimeMaterial == null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    return UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
#endif
                _runtimeMaterial = new Material(Shader.Find("Standard")) { name = "SphereMapTileMaterial" };
            }
            return _runtimeMaterial;
        }

        // ------------------------------------------------------------ Input --------------------------------------------------------------- //

        void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                // OnGUI 테스트 버튼 영역에서 시작된 클릭은 무시
                bool overTestButton = Map3DGuiArea.Contains(Input.mousePosition);
                // uGUI 차단은 옵션 (전체 화면을 덮는 캔버스가 있으면 모든 입력이 막히므로 기본 꺼짐)
                bool overUI = blockWhenPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (!overTestButton && !overUI)
                {
                    _mouseHeld = true;
                    _dragging = false;
                    _mouseDownPos = Input.mousePosition;
                    _lastMousePos = Input.mousePosition;
                }
            }

            if (_mouseHeld && Input.GetMouseButton(0))
            {
                Vector3 delta = Input.mousePosition - _lastMousePos;
                if (!_dragging && (Input.mousePosition - _mouseDownPos).magnitude > dragThreshold)
                    _dragging = true;
                // 방 선택(포커스) 중에는 구체를 돌릴 수 없다 (드래그 판정은 유지해 릴리즈가 클릭으로 오인되지 않게 함)
                if (_dragging && _focusedIndex < 0)
                    RotateBy(delta);
                _lastMousePos = Input.mousePosition;
            }

            if (_mouseHeld && Input.GetMouseButtonUp(0))
            {
                if (!_dragging)
                    TryPickTile(Input.mousePosition);
                _mouseHeld = false;
                _dragging = false;
            }

            UpdateHover();
            HandleZoom();
        }

        // 마우스가 올라간 타일 추적 — 바뀔 때만 OnTileHovered 통지 (2D 맵의 OnMouseEnter/Exit 대응)
        void UpdateHover()
        {
            if (OnTileHovered == null)
                return;

            SphereMapTile hoveredTile = null;
            // 드래그 회전 중이거나 테스트 버튼 영역 위에서는 호버 없음 처리
            if (!_dragging && !Map3DGuiArea.Contains(Input.mousePosition))
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                    {
                        var tile = hit.collider.GetComponent<SphereMapTile>();
                        if (tile != null && tile.transform.IsChildOf(_tileRoot))
                            hoveredTile = tile;
                    }
                }
            }

            int newIndex = hoveredTile != null ? hoveredTile.index : -1;
            if (newIndex == _hoveredIndex)
                return;
            _hoveredIndex = newIndex;
            OnTileHovered(hoveredTile);
        }

        // 구체 위에 마우스를 올린 채 스크롤하면 커서 지점을 중심으로 줌인/줌아웃
        // (카메라는 게임 공용이므로 건드리지 않고 구체 스케일을 조절하는 방식)
        void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.0001f)
                return;
            Camera cam = Camera.main;
            if (cam == null)
                return;

            // 마우스가 구체 타일 위에 있을 때만 줌
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f))
                return;
            var tile = hit.collider.GetComponent<SphereMapTile>();
            if (tile == null || !tile.transform.IsChildOf(_tileRoot))
                return;

            float newZoom = Mathf.Clamp(_zoom + scroll * zoomSpeed * 10f, zoomMin, Mathf.Max(zoomMin, zoomMax));
            if (Mathf.Approximately(newZoom, _zoom))
                return;

            if (newZoom <= zoomMin + 0.0001f)
            {
                // 최소 줌으로 돌아오면 원래 위치로 복귀
                transform.position = _basePosition;
            }
            else
            {
                // 커서 아래 지점이 화면상 고정되도록 중심을 반대 방향으로 이동
                float ratio = newZoom / _zoom;
                transform.position = hit.point - (hit.point - transform.position) * ratio;
            }
            _zoom = newZoom;
            transform.localScale = Vector3.one * _zoom;
        }

        // 카메라 기준 축으로 회전시켜 지구본처럼 모든 면을 볼 수 있게 함
        void RotateBy(Vector3 delta)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;
            _tileRoot.Rotate(cam.transform.up, -delta.x * rotateSpeed, Space.World);
            _tileRoot.Rotate(cam.transform.right, delta.y * rotateSpeed, Space.World);
        }

        void TryPickTile(Vector3 screenPos)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;
            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                var tile = hit.collider.GetComponent<SphereMapTile>();
                if (tile != null && tile.transform.IsChildOf(_tileRoot))
                {
                    // 게임 로직 레이어(SphereMapSystem)가 연결되어 있으면 클릭 처리를 위임
                    if (OnTileClicked != null)
                    {
                        OnTileClicked(tile);
                        return;
                    }
                    if (tile.isPentagon)
                        return; // 이동 불가 지역
                    FocusTile(tile.index);
                    return;
                }
            }
            // 빈 공간 클릭
            if (OnEmptySpaceClicked != null)
            {
                OnEmptySpaceClicked();
                return;
            }
            UnfocusTile();
        }

        // ------------------------------------------------------------ Focus --------------------------------------------------------------- //

        /// <summary>타일을 방사형으로 상승시키며 포커싱. 이미 포커스된 타일이면 해제.</summary>
        public void FocusTile(int index)
        {
            if (index < 0 || index >= _tiles.Count)
                return;
            if (index == _focusedIndex)
            {
                UnfocusTile();
                return;
            }

            ReturnFocusedTile();
            _focusedIndex = index;

            SphereMapTile tile = _tiles[index];
            tile.transform.DOKill();
            if (Application.isPlaying)
                tile.transform.DOLocalMove(tile.normal * focusHeight, focusDuration).SetEase(Ease.OutCubic);
            else
                tile.transform.localPosition = tile.normal * focusHeight;

            AlignFocusedTileToCamera(tile); // 선택한 방이 화면 중앙에서 정면을 보도록 구체 정렬
            ApplyDimToAll(); // 포커스 대상이 바뀌었을 때 즉시 밝기 재적용
            SetDim(1f);
        }

        /// <summary>해당 타일이 화면 중앙에서 정면을 보도록 구체만 정렬한다 (포커스 상승/딤 없음 — 이동 후 현재 위치 정렬용)</summary>
        public void CenterOnTile(SphereMapTile tile)
        {
            if (tile != null)
                AlignFocusedTileToCamera(tile);
        }

        /// <summary>포커스 해제 (타일 복귀 + 전체 밝기 복원 + 정렬 트윈 중단)</summary>
        public void UnfocusTile()
        {
            _alignTween?.Kill(); // 정렬 중 해제되면 그 자리에서 멈춰 사용자가 바로 회전할 수 있게 한다
            ReturnFocusedTile();
            SetDim(0f);
        }

        /// <summary>
        /// 선택된 타일이 화면 중앙에서 정면을 보도록 구체를 정렬한다.
        /// - 구체 중심을 카메라 시선 축 위로 이동 (줌 등으로 어긋난 중심 보정)
        /// - 타일 법선이 카메라를 향하고, 타일 위 장식(아이콘·투표 창)의 위쪽이 화면 위와 일치하도록 TileRoot 회전
        /// </summary>
        void AlignFocusedTileToCamera(SphereMapTile tile)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            // 현재 카메라와의 거리를 유지한 채 중심만 시선 축으로 이동 (줌 상태가 튀지 않게)
            Vector3 camPos = cam.transform.position;
            float distance = Vector3.Distance(camPos, _tileRoot.position);
            Vector3 targetPos = camPos + cam.transform.forward * distance;
            // 장식(아이콘·투표 창)은 TileRoot 로컬에서 LookRotation(-normal, upHint)로 놓인다 (SphereMapSystem 참조).
            // 그 장식 기준 좌표계가 정확히 "카메라 정면 + 화면 위쪽"이 되는 회전을 역산하면
            // 법선 정렬과 UI 수직 정렬(롤 제거)이 한 번에 결정된다.
            Vector3 upHint = Mathf.Abs(Vector3.Dot(tile.normal, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            Quaternion decoLocalRot = Quaternion.LookRotation(-tile.normal, upHint);
            Quaternion targetRot = Quaternion.LookRotation(cam.transform.forward, cam.transform.up) * Quaternion.Inverse(decoLocalRot);

            _alignTween?.Kill();
            if (Application.isPlaying)
            {
                _alignTween = DOTween.Sequence()
                    .Join(transform.DOMove(targetPos, focusDuration).SetEase(Ease.OutCubic))
                    .Join(_tileRoot.DORotateQuaternion(targetRot, focusDuration).SetEase(Ease.OutCubic));
            }
            else
            {
                transform.position = targetPos;
                _tileRoot.rotation = targetRot;
            }
        }

        void ReturnFocusedTile()
        {
            if (_focusedIndex < 0)
                return;
            SphereMapTile tile = _tiles[_focusedIndex];
            _focusedIndex = -1;
            if (tile == null)
                return;
            tile.transform.DOKill();
            if (Application.isPlaying)
                tile.transform.DOLocalMove(Vector3.zero, focusDuration).SetEase(Ease.OutCubic);
            else
                tile.transform.localPosition = Vector3.zero;
        }

        void SetDim(float target)
        {
            _dimTween?.Kill();
            if (Application.isPlaying)
            {
                _dimTween = DOTween.To(() => _dimValue, v =>
                {
                    _dimValue = v;
                    ApplyDimToAll();
                }, target, focusDuration);
            }
            else
            {
                _dimValue = target;
                ApplyDimToAll();
            }
        }

        void ApplyDimToAll()
        {
            if (s_mpb == null)
                s_mpb = new MaterialPropertyBlock();
            for (int i = 0; i < _tiles.Count; i++)
            {
                SphereMapTile tile = _tiles[i];
                if (tile == null || tile.Renderer == null)
                    continue;
                // 포커스된 타일과 하이라이트(경로 표시) 타일은 어두워지지 않음
                float dim = (i == _focusedIndex || tile.highlight) ? 0f : _dimValue;
                Color color = Color.Lerp(tile.baseColor, tile.baseColor * (1f - unfocusedDim), dim);
                s_mpb.SetColor("_Color", color);
                tile.Renderer.SetPropertyBlock(s_mpb);
            }
        }

        // ------------------------------------------------------------ Utility --------------------------------------------------------------- //

        /// <summary>타일 baseColor/highlight 변경 후 호출하면 현재 dim 상태를 유지한 채 색을 다시 적용한다.</summary>
        public void RefreshColors()
        {
            ApplyDimToAll();
        }

        /// <summary>메인 카메라 정면에 구체를 배치하고 줌 상태를 초기화 (토글/맵 진입 시 호출)</summary>
        public void PlaceInFrontOfCamera(float extraDistance = 6f)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;
            transform.position = cam.transform.position + cam.transform.forward * (radius + extraDistance);
            _basePosition = transform.position;
            _zoom = 1f;
            transform.localScale = Vector3.one;
        }
    }
}
