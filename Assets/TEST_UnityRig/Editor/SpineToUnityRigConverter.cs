// Spine → Unity 2D Animation 변환 테스트 도구 (Soldier_Axe / S1_M01 전용 파이프라인 검증)
// Phase1: 아틀라스 언팩 → Phase2: 스프라이트 스킨 데이터 주입 → Phase3: 리그 프리팹 → Phase4: 애니메이션 베이크 → Phase5: 비교 씬
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

namespace TestUnityRig
{
    public static class SpineToUnityRigConverter
    {
        const string SrcDir = "Assets/Art_Resource/SPINE_Export/01_Monster/Stage1/Normal/01_S1_Normal_Soldier_Axe/";
        const string JsonPath = SrcDir + "S1_M01.json";
        const string AtlasPath = SrcDir + "S1_M01.atlas.txt";
        const string PngPath = SrcDir + "S1_M01.png";
        const string OutDir = "Assets/TEST_UnityRig/";
        const string OutPngPath = OutDir + "SoldierAxe_Parts.png";
        const string PrefabPath = OutDir + "TEST_SoldierAxe_UnityRig.prefab";
        const string ControllerPath = OutDir + "TEST_SoldierAxe_UnityRig.controller";
        const string ScenePath = OutDir + "UnityRigCompare.unity";
        const float PPU = 100f;
        const float FPS = 30f;
        const int CanvasWidth = 1024;
        const int Pad = 4;
        // 아틀라스 rotate:90 리전 블리팅 방향. true/false는 서로 180도 차이 —
        // 틀리면 실루엣은 맞아 보여도 파츠 아트의 위아래(관절 연결부)가 반대가 된다.
        const bool RotateVariantA = false;
        // IK 진단용: 0 = IK 끔, 1/-1 = bend 방향 강제, 2 = JSON 값 사용
        public static int IkMode = 2;

        // ---------------------------------------------------------------- entry points

        [MenuItem("Tools/TEST UnityRig/Run All")]
        public static void RunAll()
        {
            Phase1_UnpackAtlas();
            Phase2_InjectSpriteData();
            Phase3_BuildRigPrefab();
            Phase4_BakeAnimations();
            Phase5_BuildCompareScene();
        }

        // ---------------------------------------------------------------- data model

        class AtlasRegion { public string name; public int x, y, w, h; public int ox, oy, ow, oh; public bool rot; public RectInt cell; }
        class SpBone { public string name; public int parent = -1; public float x, y, rot, sx = 1, sy = 1, length; public string inherit = "normal"; }
        class SpSlot { public string name; public int boneIndex; public string attachment; public float alpha = 1f; public string blend; }
        class SpMesh
        {
            public string slot; public string regionName;
            public float[] uvs; public int[] tris; public int hull;
            public bool weighted;
            public float[] plainVerts;                    // 비웨이트: 본 로컬 x,y 목록
            public List<List<float[]>> weightedVerts;     // 웨이트: 정점별 [boneIdx, x, y, w] 목록
        }
        class SpIk
        {
            public string[] bones; public string target; public int bend = 1;
            // EnsureIkOffsets에서 채움: 발목(이펙터) 기반 솔브 데이터
            public Vector2 ankleFromTarget; // 셋업에서 (발목 위치 - 타깃 위치): 발목 목표 = 타깃 + 이 값
            public float l2 = -1, delta2;   // 무릎→발목 거리 / 정강이 로컬 각 보정
            public int bendAuto = 1;        // 셋업 기하에서 판정한 굽힘 방향
            public float off0, off1;        // 잔차 보정 (구성상 ~0)
            public int footIndex = -1;      // 발 본(정강이 첫 자식) 인덱스 — 발바닥 핀 고정에 사용
        }
        class SpPathConstraint { public string name; public int boneIndex; public string targetSlot; public float position; }
        class SpPathAttachment
        {
            public bool closed; public float[] vertices; // 트리플: cpIn, anchor, cpOut
            float[] sx, sy, cum; float total;             // 조밀 샘플 폴리라인 캐시
            public Vector2 Eval(float p)
            {
                if (sx == null) Build();
                if (total < 1e-4f) return new Vector2(sx[0], sy[0]);
                float target = closed ? Mathf.Repeat(p, 1f) * total : Mathf.Clamp01(p) * total;
                int lo = 0, hi = cum.Length - 1;
                while (lo < hi) { int mid = (lo + hi) / 2; if (cum[mid] < target) lo = mid + 1; else hi = mid; }
                int i = Mathf.Max(1, lo);
                float t = Mathf.InverseLerp(cum[i - 1], cum[i], target);
                return new Vector2(Mathf.Lerp(sx[i - 1], sx[i], t), Mathf.Lerp(sy[i - 1], sy[i], t));
            }
            void Build()
            {
                int n = vertices.Length / 6; // 앵커 수
                var px = new List<float>(); var py = new List<float>();
                int segs = closed ? n : n - 1;
                for (int i = 0; i < segs; i++)
                {
                    int j = (i + 1) % n;
                    float ax = vertices[i * 6 + 2], ay = vertices[i * 6 + 3];          // anchor_i
                    float c1x = vertices[i * 6 + 4], c1y = vertices[i * 6 + 5];        // cpOut_i
                    float c2x = vertices[j * 6 + 0], c2y = vertices[j * 6 + 1];        // cpIn_j
                    float bx = vertices[j * 6 + 2], by = vertices[j * 6 + 3];          // anchor_j
                    for (int k = 0; k < 24; k++)
                    {
                        float t = k / 24f, u = 1 - t;
                        px.Add(u * u * u * ax + 3 * u * u * t * c1x + 3 * u * t * t * c2x + t * t * t * bx);
                        py.Add(u * u * u * ay + 3 * u * u * t * c1y + 3 * u * t * t * c2y + t * t * t * by);
                    }
                }
                px.Add(vertices[(closed ? 0 : (n - 1)) * 6 + 2]); py.Add(vertices[(closed ? 0 : (n - 1)) * 6 + 3]);
                sx = px.ToArray(); sy = py.ToArray();
                cum = new float[sx.Length]; cum[0] = 0;
                for (int i = 1; i < sx.Length; i++)
                    cum[i] = cum[i - 1] + Mathf.Sqrt((sx[i] - sx[i - 1]) * (sx[i] - sx[i - 1]) + (sy[i] - sy[i - 1]) * (sy[i] - sy[i - 1]));
                total = cum[cum.Length - 1];
            }
        }

        struct XForm
        {
            public float px, py, rot, sx, sy;
            public Vector2 Apply(float vx, float vy)
            {
                float x = vx * sx, y = vy * sy;
                float c = Mathf.Cos(rot * Mathf.Deg2Rad), s = Mathf.Sin(rot * Mathf.Deg2Rad);
                return new Vector2(px + c * x - s * y, py + s * x + c * y);
            }
        }

        class Model
        {
            public List<SpBone> bones = new List<SpBone>();
            public Dictionary<string, int> boneIndex = new Dictionary<string, int>();
            public List<SpSlot> slots = new List<SpSlot>();
            public Dictionary<string, SpMesh> meshBySlot = new Dictionary<string, SpMesh>();
            public List<SpIk> iks = new List<SpIk>();
            public Dictionary<string, object> animations;
            public List<AtlasRegion> regions = new List<AtlasRegion>();
            public Dictionary<string, AtlasRegion> regionByName = new Dictionary<string, AtlasRegion>();
            public int canvasW, canvasH;
            public Dictionary<int, float> ikRotOffsets; // 델타 IK 보정: 셋업에서 (원본 회전 - 솔버 회전)
            public List<SpPathConstraint> pathConstraints = new List<SpPathConstraint>();
            public Dictionary<string, SpPathAttachment> pathBySlot = new Dictionary<string, SpPathAttachment>();
            public Dictionary<string, Vector2> pathOffsets; // 델타 보정: 셋업에서 (원본 위치 - 경로 위치)
            public Dictionary<string, int> slotBoneIndex = new Dictionary<string, int>();
        }

        static Model Load()
        {
            var m = new Model();
            var root = (Dictionary<string, object>)MiniJson.Deserialize(File.ReadAllText(JsonPath));

            foreach (Dictionary<string, object> b in (List<object>)root["bones"])
            {
                var bone = new SpBone { name = (string)b["name"] };
                if (b.ContainsKey("parent")) bone.parent = m.boneIndex[(string)b["parent"]];
                bone.x = F(b, "x"); bone.y = F(b, "y"); bone.rot = F(b, "rotation");
                bone.sx = F(b, "scaleX", 1); bone.sy = F(b, "scaleY", 1);
                bone.length = F(b, "length");
                if (b.ContainsKey("inherit")) bone.inherit = (string)b["inherit"];
                if (b.ContainsKey("transform")) bone.inherit = (string)b["transform"];
                m.boneIndex[bone.name] = m.bones.Count;
                m.bones.Add(bone);
            }

            foreach (Dictionary<string, object> s in (List<object>)root["slots"])
            {
                var slot = new SpSlot { name = (string)s["name"], boneIndex = m.boneIndex[(string)s["bone"]] };
                if (s.ContainsKey("attachment")) slot.attachment = (string)s["attachment"];
                if (s.ContainsKey("blend")) slot.blend = (string)s["blend"];
                if (s.ContainsKey("color"))
                {
                    string hex = (string)s["color"];
                    if (hex.Length >= 8) slot.alpha = Convert.ToInt32(hex.Substring(6, 2), 16) / 255f;
                }
                m.slotBoneIndex[slot.name] = slot.boneIndex;
                m.slots.Add(slot);
            }

            if (root.ContainsKey("ik"))
                foreach (Dictionary<string, object> ik in (List<object>)root["ik"])
                {
                    var c = new SpIk { target = (string)ik["target"] };
                    c.bones = ((List<object>)ik["bones"]).Select(o => (string)o).ToArray();
                    // spine bendPositive=true(기본) ↔ 본 솔버 부호 규약상 -1
                    c.bend = -1;
                    if (ik.ContainsKey("bendPositive") && ik["bendPositive"] is bool bp && !bp) c.bend = 1;
                    m.iks.Add(c);
                }

            if (root.ContainsKey("path"))
            {
                var rawList = root["path"] is List<object> pl ? pl : new List<object> { root["path"] };
                foreach (Dictionary<string, object> pcRaw in rawList)
                {
                    var boneNames = ((List<object>)pcRaw["bones"]).Select(o => (string)o).ToArray();
                    if (boneNames.Length > 1) Debug.LogWarning($"[UnityRigTest] path constraint {pcRaw["name"]}: 다중 본 미지원, 첫 본만 적용");
                    m.pathConstraints.Add(new SpPathConstraint
                    {
                        name = (string)pcRaw["name"],
                        boneIndex = m.boneIndex[boneNames[0]],
                        targetSlot = (string)pcRaw["target"],
                        position = F(pcRaw, "position")
                    });
                }
            }

            // skins.default: 슬롯별 메시 어태치먼트
            var skins = (List<object>)root["skins"];
            var defaultSkin = (Dictionary<string, object>)skins.Cast<Dictionary<string, object>>().First(sk => (string)sk["name"] == "default")["attachments"];
            foreach (var slotKv in defaultSkin)
            {
                var atts = (Dictionary<string, object>)slotKv.Value;
                foreach (var attKv in atts)
                {
                    var att = (Dictionary<string, object>)attKv.Value;
                    string type = att.ContainsKey("type") ? (string)att["type"] : "region";
                    if (type == "path")
                    {
                        var pv = FA(att["vertices"]);
                        int vc = att.ContainsKey("vertexCount") ? (int)Convert.ToDouble(att["vertexCount"]) : pv.Length / 2;
                        if (pv.Length == vc * 2) // 비웨이트 경로만 지원
                            m.pathBySlot[slotKv.Key] = new SpPathAttachment
                            {
                                closed = att.ContainsKey("closed") && att["closed"] is bool cb && cb,
                                vertices = pv
                            };
                        else Debug.LogWarning($"[UnityRigTest] weighted path 미지원: {slotKv.Key}");
                        continue;
                    }
                    if (type != "mesh") continue; // 그 외 타입은 렌더링 안함
                    var mesh = new SpMesh { slot = slotKv.Key, regionName = att.ContainsKey("path") ? (string)att["path"] : attKv.Key };
                    mesh.uvs = FA(att["uvs"]);
                    mesh.tris = ((List<object>)att["triangles"]).Select(o => (int)Convert.ToDouble(o)).ToArray();
                    mesh.hull = att.ContainsKey("hull") ? (int)Convert.ToDouble(att["hull"]) : 0;
                    var raw = FA(att["vertices"]);
                    int vCount = mesh.uvs.Length / 2;
                    if (raw.Length == vCount * 2) { mesh.weighted = false; mesh.plainVerts = raw; }
                    else
                    {
                        mesh.weighted = true;
                        mesh.weightedVerts = new List<List<float[]>>();
                        int i = 0;
                        while (i < raw.Length)
                        {
                            int n = (int)raw[i++];
                            var infl = new List<float[]>();
                            for (int k = 0; k < n; k++) { infl.Add(new[] { raw[i], raw[i + 1], raw[i + 2], raw[i + 3] }); i += 4; }
                            mesh.weightedVerts.Add(infl);
                        }
                    }
                    m.meshBySlot[slotKv.Key] = mesh;
                }
            }

            m.animations = (Dictionary<string, object>)root["animations"];

            ParseAtlas(m);
            PackCells(m);
            return m;
        }

        static float F(Dictionary<string, object> d, string k, float def = 0) => d.ContainsKey(k) ? (float)Convert.ToDouble(d[k]) : def;
        static float[] FA(object o) => ((List<object>)o).Select(x => (float)Convert.ToDouble(x)).ToArray();

        static void ParseAtlas(Model m)
        {
            var lines = File.ReadAllLines(AtlasPath).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            AtlasRegion cur = null;
            for (int i = 1; i < lines.Count; i++) // line0 = 페이지 이름
            {
                var line = lines[i];
                int colon = line.IndexOf(':');
                if (colon < 0)
                {
                    cur = new AtlasRegion { name = line };
                    m.regions.Add(cur); m.regionByName[cur.name] = cur;
                    continue;
                }
                string key = line.Substring(0, colon).Trim();
                var vals = line.Substring(colon + 1).Split(',').Select(v => v.Trim()).ToArray();
                if (cur == null) continue; // 페이지 속성
                switch (key)
                {
                    case "bounds": cur.x = int.Parse(vals[0]); cur.y = int.Parse(vals[1]); cur.w = int.Parse(vals[2]); cur.h = int.Parse(vals[3]); break;
                    case "offsets": cur.ox = int.Parse(vals[0]); cur.oy = int.Parse(vals[1]); cur.ow = int.Parse(vals[2]); cur.oh = int.Parse(vals[3]); break;
                    case "rotate": cur.rot = vals[0] == "90" || vals[0] == "true"; break;
                }
            }
            foreach (var r in m.regions) if (r.ow == 0) { r.ow = r.w; r.oh = r.h; }
        }

        static void PackCells(Model m)
        {
            var order = m.regions.OrderByDescending(r => r.oh).ThenBy(r => r.name).ToList();
            int cx = Pad, cyTop = Pad, rowH = 0;
            foreach (var r in order)
            {
                if (cx + r.ow + Pad > CanvasWidth) { cx = Pad; cyTop += rowH + Pad; rowH = 0; }
                r.cell = new RectInt(cx, cyTop, r.ow, r.oh); // 일단 top-down으로 기록
                cx += r.ow + Pad;
                rowH = Mathf.Max(rowH, r.oh);
            }
            int totalH = cyTop + rowH + Pad;
            totalH = ((totalH + 3) / 4) * 4;
            m.canvasW = CanvasWidth; m.canvasH = totalH;
            foreach (var r in m.regions) r.cell = new RectInt(r.cell.x, totalH - r.cell.y - r.cell.height, r.cell.width, r.cell.height); // bottom-left 기준으로 변환
        }

        // ---------------------------------------------------------------- Phase 1

        public static void Phase1_UnpackAtlas()
        {
            var m = Load();
            var pageBytes = File.ReadAllBytes(PngPath);
            var page = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            page.LoadImage(pageBytes);
            var src = page.GetPixels32();
            int pw = page.width, ph = page.height;

            var canvas = new Color32[m.canvasW * m.canvasH];
            foreach (var r in m.regions)
            {
                int wpg = r.rot ? r.h : r.w, hpg = r.rot ? r.w : r.h;
                int x0 = r.x, y0 = ph - r.y - hpg; // bounds y는 top-left 기준
                for (int i = 0; i < r.w; i++)
                    for (int j = 0; j < r.h; j++)
                    {
                        Color32 c;
                        if (!r.rot) c = src[(y0 + j) * pw + (x0 + i)];
                        else if (RotateVariantA) c = src[(y0 + (r.w - 1 - i)) * pw + (x0 + j)];
                        else c = src[(y0 + i) * pw + (x0 + (r.h - 1 - j))];
                        // PMA 해제
                        if (c.a > 0 && c.a < 255)
                        {
                            c.r = (byte)Mathf.Min(255, c.r * 255 / c.a);
                            c.g = (byte)Mathf.Min(255, c.g * 255 / c.a);
                            c.b = (byte)Mathf.Min(255, c.b * 255 / c.a);
                        }
                        int dx = r.cell.x + r.ox + i, dy = r.cell.y + r.oy + j;
                        if (dx >= 0 && dx < m.canvasW && dy >= 0 && dy < m.canvasH) canvas[dy * m.canvasW + dx] = c;
                    }
            }
            UnityEngine.Object.DestroyImmediate(page);

            var outTex = new Texture2D(m.canvasW, m.canvasH, TextureFormat.RGBA32, false);
            outTex.SetPixels32(canvas);
            Directory.CreateDirectory(OutDir);
            File.WriteAllBytes(OutPngPath, outTex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(outTex);
            AssetDatabase.Refresh();
            Debug.Log($"[UnityRigTest] Phase1 완료: {OutPngPath} ({m.canvasW}x{m.canvasH}, {m.regions.Count} regions)");
        }

        // ---------------------------------------------------------------- setup pose / transforms

        static XForm[] ComputeSetupWorld(Model m)
        {
            // 셋업 포즈도 스파인 런타임 기준 (컨스트레인트 포함 원본과 동일)
            return SpineWorldAt(m, null, 0f);
        }

        // ---------------------------------------------------------------- 스파인 런타임 기반 포즈 계산
        // 원본 애니메이션(Idle 등)은 수제 솔버 대신 프로젝트에 내장된 spine-csharp로 포즈를 계산한다.
        // IK/패스/트랜스폼 컨스트레인트/상속이 원본과 100% 동일 → 다리 위치·연결 오차가 원천 제거됨.
        static Spine.Skeleton _spineSkel;
        static Dictionary<string, Spine.Bone> _spineBones;
        static float _spineScale = 0.01f;

        static void EnsureSpineSkeleton()
        {
            if (_spineSkel != null) return;
            var dataAsset = AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>(SrcDir + "S1_M01_SkeletonData.asset");
            _spineScale = dataAsset.scale;
            var data = dataAsset.GetSkeletonData(true);
            _spineSkel = new Spine.Skeleton(data);
            _spineBones = new Dictionary<string, Spine.Bone>();
            foreach (var b in _spineSkel.Bones) _spineBones[b.Data.Name] = b;
        }

        static XForm[] SpineWorldAt(Model m, string animName, float t) // animName null = 셋업 포즈
        {
            EnsureSpineSkeleton();
            _spineSkel.SetToSetupPose();
            if (animName != null)
            {
                var anim = _spineSkel.Data.FindAnimation(animName);
                anim.Apply(_spineSkel, 0, t, false, null, 1f, Spine.MixBlend.Setup, Spine.MixDirection.In);
            }
            _spineSkel.UpdateWorldTransform(Spine.Skeleton.Physics.Update);
            var world = new XForm[m.bones.Count];
            float inv = 1f / _spineScale;
            for (int i = 0; i < m.bones.Count; i++)
            {
                var sb = _spineBones[m.bones[i].name];
                world[i] = new XForm { px = sb.WorldX * inv, py = sb.WorldY * inv, rot = sb.WorldRotationX, sx = sb.WorldScaleX, sy = sb.WorldScaleY };
            }
            return world;
        }

        static XForm Compose(XForm p, XForm l, string inherit)
        {
            var w = new XForm();
            var pos = p.Apply(l.px, l.py);
            w.px = pos.x; w.py = pos.y;
            switch (inherit)
            {
                case "onlyTranslation":
                case "noRotationOrReflection":
                    w.rot = l.rot; w.sx = l.sx; w.sy = l.sy; break;
                case "noScale":
                case "noScaleOrReflection":
                    w.rot = p.rot + l.rot; w.sx = l.sx; w.sy = l.sy; break;
                default:
                    w.rot = p.rot + l.rot; w.sx = p.sx * l.sx; w.sy = p.sy * l.sy; break;
            }
            return w;
        }

        static XForm[] ComputeWorld(Model m, XForm[] locals, bool applyIk, Dictionary<string, float> pathPos = null)
        {
            var world = new XForm[m.bones.Count];
            for (int i = 0; i < m.bones.Count; i++)
            {
                var b = m.bones[i];
                world[i] = b.parent < 0 ? locals[i] : Compose(world[b.parent], locals[i], b.inherit);
            }
            if (!applyIk || IkMode == 0) return world;
            EnsureIkOffsets(m);

            // 패스 컨스트레인트 (IK보다 먼저): 대상 본 위치를 경로 위 지점으로 이동 → 하위 재계산 → IK가 무릎을 굽힘
            foreach (var pc in m.pathConstraints)
            {
                if (!m.pathBySlot.TryGetValue(pc.targetSlot, out var pa)) continue;
                int sb = m.slotBoneIndex[pc.targetSlot];
                float p = pathPos != null && pathPos.TryGetValue(pc.name, out var pv) ? pv : pc.position;
                var lp = pa.Eval(p);
                var wp = world[sb].Apply(lp.x, lp.y);
                var off = m.pathOffsets != null && m.pathOffsets.TryGetValue(pc.name, out var o) ? o : Vector2.zero;
                int pbi = pc.boneIndex;
                world[pbi].px = wp.x + off.x; world[pbi].py = wp.y + off.y;
                var pdirty = new bool[m.bones.Count];
                pdirty[pbi] = true;
                for (int i = 0; i < m.bones.Count; i++)
                {
                    var b = m.bones[i];
                    if (b.parent < 0 || i == pbi) continue;
                    if (pdirty[b.parent]) { world[i] = Compose(world[b.parent], locals[i], b.inherit); pdirty[i] = true; }
                }
            }

            foreach (var ik in m.iks)
            {
                if (ik.l2 < 0) continue;
                int bend = IkMode == 2 ? ik.bendAuto : IkMode;
                int i0 = m.boneIndex[ik.bones[0]], i1 = m.boneIndex[ik.bones[1]], it = m.boneIndex[ik.target];
                // 이펙터 = 발목(발 본 원점). 타깃이 안 움직이면 발목도 그대로 → 발바닥 고정
                var p = new Vector2(world[i0].px, world[i0].py);
                var A = new Vector2(world[it].px, world[it].py) + ik.ankleFromTarget;
                float lx = locals[i1].px, ly = locals[i1].py;
                float l1 = Mathf.Sqrt(lx * lx + ly * ly);
                float d1 = Mathf.Atan2(ly, lx) * Mathf.Rad2Deg;
                SolveChain(p, A, l1, ik.l2, bend, out float upperDir, out _);
                world[i0].rot = upperDir - d1 + ik.off0;
                var c = world[i0].Apply(lx, ly);
                world[i1].px = c.x; world[i1].py = c.y;
                world[i1].rot = Mathf.Atan2(A.y - c.y, A.x - c.x) * Mathf.Rad2Deg - ik.delta2 + ik.off1;

                // 체인 하위 본 재계산
                var dirty = new bool[m.bones.Count];
                dirty[i0] = true; dirty[i1] = true;
                for (int i = 0; i < m.bones.Count; i++)
                {
                    var b = m.bones[i];
                    if (b.parent < 0 || i == i0 || i == i1) continue;
                    if (dirty[b.parent]) { world[i] = Compose(world[b.parent], locals[i], b.inherit); dirty[i] = true; }
                }

                // 발바닥 핀 고정: 클램프/잔차로 정강이 끝이 발목 목표에 못 미치면 발이 끌려 올라간다 →
                // 그 오차(A - 정강이 끝점)만큼 발과 하위 본을 통째로 되밀어 발목을 목표에 정확히 고정.
                // (발 로컬 애니메이션 오프셋은 평행이동이라 보존됨)
                if (ik.footIndex >= 0)
                {
                    var toA = new Vector2(A.x - c.x, A.y - c.y);
                    float dl = toA.magnitude;
                    if (dl > 1e-4f)
                    {
                        var E = c + toA / dl * ik.l2; // 정강이 끝(발목이 실제로 도달한 지점)
                        var corr = new Vector2(A.x - E.x, A.y - E.y);
                        if (corr.sqrMagnitude > 1e-6f)
                        {
                            var inSub = new bool[m.bones.Count];
                            inSub[ik.footIndex] = true;
                            world[ik.footIndex].px += corr.x; world[ik.footIndex].py += corr.y;
                            for (int i = 0; i < m.bones.Count; i++)
                            {
                                var b = m.bones[i];
                                if (i == ik.footIndex || b.parent < 0 || !inSub[b.parent]) continue;
                                inSub[i] = true;
                                world[i].px += corr.x; world[i].py += corr.y;
                            }
                        }
                    }
                }
            }
            return world;
        }

        // 2본 IK: 시작점 p에서 무릎(l1)을 거쳐 발목 목표 A(거리 l2)에 도달하는 상단 방향각과 무릎 위치
        static void SolveChain(Vector2 p, Vector2 A, float l1, float l2, int bend, out float upperDir, out Vector2 knee)
        {
            Vector2 d = A - p;
            float dist = Mathf.Clamp(d.magnitude, Mathf.Abs(l1 - l2) + 0.001f, l1 + l2 - 0.001f);
            float baseAng = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            float cosA = Mathf.Clamp((l1 * l1 + dist * dist - l2 * l2) / (2 * l1 * dist), -1f, 1f);
            upperDir = baseAng + bend * Mathf.Acos(cosA) * Mathf.Rad2Deg;
            knee = p + new Vector2(Mathf.Cos(upperDir * Mathf.Deg2Rad), Mathf.Sin(upperDir * Mathf.Deg2Rad)) * l1;
        }

        // 셋업 포즈(원본 데이터 = 정답)와 솔버 해의 차이를 오프셋으로 보관 → 셋업에서 원본과 정확히 일치
        static void EnsureIkOffsets(Model m)
        {
            if (m.ikRotOffsets != null) return;
            m.ikRotOffsets = new Dictionary<int, float>();
            var setupLocals = m.bones.Select(b => new XForm { px = b.x, py = b.y, rot = b.rot, sx = b.sx, sy = b.sy }).ToArray();
            // 정답 기준 = 스파인 런타임 셋업 포즈 (컨스트레인트 포함) → 커스텀 애니메이션도 원본 포즈와 정확히 정합
            var truth = SpineWorldAt(m, null, 0f);

            // 패스 델타 보정: 셋업에서 (원본 본 위치 - 경로 위 지점)
            m.pathOffsets = new Dictionary<string, Vector2>();
            foreach (var pc in m.pathConstraints)
            {
                if (!m.pathBySlot.TryGetValue(pc.targetSlot, out var pa)) continue;
                int sb = m.slotBoneIndex[pc.targetSlot];
                var lp = pa.Eval(pc.position);
                var wp = truth[sb].Apply(lp.x, lp.y);
                m.pathOffsets[pc.name] = new Vector2(truth[pc.boneIndex].px - wp.x, truth[pc.boneIndex].py - wp.y);
            }
            foreach (var ik in m.iks)
            {
                int i0 = m.boneIndex[ik.bones[0]], i1 = m.boneIndex[ik.bones[1]], it = m.boneIndex[ik.target];
                // 이펙터 = 정강이의 첫 자식(발) 원점 = 발목. 없으면 본 길이 팁으로 대체
                int footIdx = -1;
                for (int i = 0; i < m.bones.Count; i++) if (m.bones[i].parent == i1) { footIdx = i; break; }
                ik.footIndex = footIdx;
                Vector2 A;
                if (footIdx >= 0)
                {
                    A = new Vector2(truth[footIdx].px, truth[footIdx].py);
                    ik.l2 = Mathf.Sqrt(m.bones[footIdx].x * m.bones[footIdx].x + m.bones[footIdx].y * m.bones[footIdx].y);
                    ik.delta2 = Mathf.Atan2(m.bones[footIdx].y, m.bones[footIdx].x) * Mathf.Rad2Deg;
                }
                else
                {
                    ik.l2 = m.bones[i1].length; ik.delta2 = 0;
                    float wr = truth[i1].rot * Mathf.Deg2Rad;
                    A = new Vector2(truth[i1].px + Mathf.Cos(wr) * ik.l2, truth[i1].py + Mathf.Sin(wr) * ik.l2);
                }
                var p = new Vector2(truth[i0].px, truth[i0].py);
                var knee = new Vector2(truth[i1].px, truth[i1].py);
                var tpos = new Vector2(truth[it].px, truth[it].py);
                ik.ankleFromTarget = A - tpos;
                float cross = (A.x - p.x) * (knee.y - p.y) - (A.y - p.y) * (knee.x - p.x);
                ik.bendAuto = cross >= 0 ? 1 : -1;
                // 잔차 보정 (셋업 기하 기준이라 ~0이어야 정상)
                float lx = setupLocals[i1].px, ly = setupLocals[i1].py;
                float l1 = Mathf.Sqrt(lx * lx + ly * ly);
                float d1 = Mathf.Atan2(ly, lx) * Mathf.Rad2Deg;
                SolveChain(p, A, l1, ik.l2, ik.bendAuto, out float upperDir, out Vector2 kneeS);
                ik.off0 = Mathf.DeltaAngle(upperDir - d1, truth[i0].rot);
                float lowerSolved = Mathf.Atan2(A.y - kneeS.y, A.x - kneeS.x) * Mathf.Rad2Deg - ik.delta2;
                ik.off1 = Mathf.DeltaAngle(lowerSolved, truth[i1].rot);
                if (Mathf.Abs(ik.off0) > 3f || Mathf.Abs(ik.off1) > 3f)
                    Debug.LogWarning($"[UnityRigTest] IK 잔차 큼: {ik.target} off0={ik.off0:F1} off1={ik.off1:F1}");
            }
        }

        // Unity 계층(항상 normal 상속) 기준 로컬값 역산
        static void UnityLocal(Model m, XForm[] world, int i, out Vector2 pos, out float rot, out Vector2 scale)
        {
            var b = m.bones[i];
            if (b.parent < 0)
            {
                pos = new Vector2(world[i].px, world[i].py); rot = world[i].rot; scale = new Vector2(world[i].sx, world[i].sy);
                return;
            }
            var pw = world[b.parent];
            float c = Mathf.Cos(-pw.rot * Mathf.Deg2Rad), s = Mathf.Sin(-pw.rot * Mathf.Deg2Rad);
            float dx = world[i].px - pw.px, dy = world[i].py - pw.py;
            float rx = c * dx - s * dy, ry = s * dx + c * dy;
            pos = new Vector2(rx / pw.sx, ry / pw.sy);
            rot = world[i].rot - pw.rot;
            scale = new Vector2(world[i].sx / pw.sx, world[i].sy / pw.sy);
        }

        // 셀(원본 픽셀) 공간 좌표: uv → bottom-left 기준
        static Vector2 CellPos(SpMesh mesh, AtlasRegion r, int vi) => new Vector2(mesh.uvs[vi * 2] * r.ow, (1f - mesh.uvs[vi * 2 + 1]) * r.oh);

        static Vector2 VertexWorld(Model m, XForm[] world, SpMesh mesh, SpSlot slot, int vi)
        {
            if (!mesh.weighted) return world[slot.boneIndex].Apply(mesh.plainVerts[vi * 2], mesh.plainVerts[vi * 2 + 1]);
            Vector2 acc = Vector2.zero;
            foreach (var inf in mesh.weightedVerts[vi]) acc += world[(int)inf[0]].Apply(inf[1], inf[2]) * inf[3];
            return acc;
        }

        // 셀 평면 좌표 → 스켈레톤 좌표 최소제곱 유사변환 (복소수 회귀)
        static void FitSimilarity(List<Vector2> P, List<Vector2> Q, out float theta, out float s, out Vector2 trans)
        {
            Vector2 mp = Vector2.zero, mq = Vector2.zero;
            foreach (var p in P) mp += p; mp /= P.Count;
            foreach (var q in Q) mq += q; mq /= Q.Count;
            float numRe = 0, numIm = 0, den = 0;
            for (int i = 0; i < P.Count; i++)
            {
                var p = P[i] - mp; var q = Q[i] - mq;
                numRe += p.x * q.x + p.y * q.y;
                numIm += p.x * q.y - p.y * q.x;
                den += p.sqrMagnitude;
            }
            if (den < 1e-6f) { theta = 0; s = 1; trans = mq - mp; return; }
            float aRe = numRe / den, aIm = numIm / den;
            theta = Mathf.Atan2(aIm, aRe) * Mathf.Rad2Deg;
            s = Mathf.Sqrt(aRe * aRe + aIm * aIm);
            if (s < 1e-6f) s = 1;
            float c = Mathf.Cos(theta * Mathf.Deg2Rad) * s, sn = Mathf.Sin(theta * Mathf.Deg2Rad) * s;
            trans = new Vector2(mq.x - (c * mp.x - sn * mp.y), mq.y - (sn * mp.x + c * mp.y));
        }

        static Vector2 InverseSimilarity(Vector2 q, float theta, float s, Vector2 trans)
        {
            var d = q - trans;
            float c = Mathf.Cos(-theta * Mathf.Deg2Rad), sn = Mathf.Sin(-theta * Mathf.Deg2Rad);
            return new Vector2((c * d.x - sn * d.y) / s, (sn * d.x + c * d.y) / s);
        }

        // 본 1개짜리 메시의 (본 로컬) 인플루언스 좌표 평탄화 — deform 델타 배열과 순서 정렬됨
        static float[] SingleBoneInfluenceCoords(SpMesh mesh)
        {
            if (!mesh.weighted) return mesh.plainVerts;
            var list = new List<float>();
            foreach (var v in mesh.weightedVerts)
                foreach (var inf in v) { list.Add(inf[1]); list.Add(inf[2]); }
            return list.ToArray();
        }

        static List<int> InfluencingBones(Model m, SpMesh mesh, SpSlot slot)
        {
            if (!mesh.weighted) return new List<int> { slot.boneIndex };
            var set = new List<int>();
            foreach (var v in mesh.weightedVerts)
                foreach (var inf in v)
                    if (!set.Contains((int)inf[0])) set.Add((int)inf[0]);
            return set;
        }

        // ---------------------------------------------------------------- Phase 2

        public static void Phase2_InjectSpriteData()
        {
            var m = Load();
            var setup = ComputeSetupWorld(m);

            var importer = (TextureImporter)AssetImporter.GetAtPath(OutPngPath);
            if (importer == null) { Debug.LogError("[UnityRigTest] unpacked png 없음 — Phase1 먼저 실행"); return; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PPU;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var dp = factories.GetSpriteEditorDataProviderFromObject(importer);
            dp.InitSpriteEditorDataProvider();

            // 슬롯별 스킨 데이터 계산 결과를 리전(스프라이트) 단위로 저장 (EyeLight는 두 슬롯이 공유 → 첫번째 것 사용)
            var spriteRects = new List<SpriteRect>();
            var boneData = new Dictionary<GUID, List<UnityEngine.U2D.SpriteBone>>();
            var meshVerts = new Dictionary<GUID, Vertex2DMetaData[]>();
            var meshIndices = new Dictionary<GUID, int[]>();
            var meshEdges = new Dictionary<GUID, Vector2Int[]>();
            var spriteBoneNames = new Dictionary<string, List<string>>(); // regionName → 스켈레톤 본 이름 순서 (Phase3에서 사용)

            var doneRegions = new HashSet<string>();
            foreach (var slot in m.slots)
            {
                if (!m.meshBySlot.TryGetValue(slot.name, out var mesh)) continue;
                if (doneRegions.Contains(mesh.regionName)) continue;
                doneRegions.Add(mesh.regionName);
                var region = m.regionByName[mesh.regionName];

                int vCount = mesh.uvs.Length / 2;
                var flat = new List<Vector2>(); var world = new List<Vector2>();
                for (int vi = 0; vi < vCount; vi++)
                {
                    flat.Add(CellPos(mesh, region, vi));
                    world.Add(VertexWorld(m, setup, mesh, slot, vi));
                }
                FitSimilarity(flat, world, out float theta, out float scl, out Vector2 trans);
                if (Mathf.Abs(scl - 1f) > 0.15f)
                    Debug.LogWarning($"[UnityRigTest] {mesh.regionName}: 셀↔스켈레톤 스케일 편차 {scl:F3} (텍스처 왜곡 가능)");

                var rect = new SpriteRect
                {
                    name = mesh.regionName,
                    spriteID = GUID.Generate(),
                    rect = new Rect(region.cell.x, region.cell.y, region.cell.width, region.cell.height),
                    alignment = SpriteAlignment.BottomLeft,
                    pivot = new Vector2(0f, 0f)
                };
                spriteRects.Add(rect);

                // 스프라이트 본: 영향 본을 rect 공간으로 역매핑 (플랫 루트)
                var infBones = InfluencingBones(m, mesh, slot);
                spriteBoneNames[mesh.regionName] = infBones.Select(bi => m.bones[bi].name).ToList();
                var sbList = new List<UnityEngine.U2D.SpriteBone>();
                foreach (var bi in infBones)
                {
                    var bw = setup[bi];
                    var rectPos = InverseSimilarity(new Vector2(bw.px, bw.py), theta, scl, trans);
                    sbList.Add(new UnityEngine.U2D.SpriteBone
                    {
                        name = m.bones[bi].name,
                        guid = GUID.Generate().ToString(),
                        position = new Vector3(rectPos.x, rectPos.y, 0),
                        rotation = Quaternion.Euler(0, 0, bw.rot - theta),
                        length = Mathf.Max(m.bones[bi].length / Mathf.Max(scl, 0.001f), 5f),
                        parentId = -1,
                        color = new Color32(255, 128, 0, 255)
                    });
                }
                boneData[rect.spriteID] = sbList;

                // 정점: 셀 평면 좌표 + 웨이트 (rect-local)
                var verts = new Vertex2DMetaData[vCount];
                for (int vi = 0; vi < vCount; vi++)
                {
                    var bwgt = new BoneWeight();
                    if (mesh.weighted)
                    {
                        var top = mesh.weightedVerts[vi].OrderByDescending(x => x[3]).Take(4).ToList();
                        float sum = top.Sum(x => x[3]);
                        for (int k = 0; k < top.Count; k++)
                        {
                            int localIdx = infBones.IndexOf((int)top[k][0]);
                            float wgt = sum > 0 ? top[k][3] / sum : 0;
                            if (k == 0) { bwgt.boneIndex0 = localIdx; bwgt.weight0 = wgt; }
                            else if (k == 1) { bwgt.boneIndex1 = localIdx; bwgt.weight1 = wgt; }
                            else if (k == 2) { bwgt.boneIndex2 = localIdx; bwgt.weight2 = wgt; }
                            else { bwgt.boneIndex3 = localIdx; bwgt.weight3 = wgt; }
                        }
                    }
                    else { bwgt.boneIndex0 = 0; bwgt.weight0 = 1f; }
                    verts[vi] = new Vertex2DMetaData { position = flat[vi], boneWeight = bwgt };
                }
                meshVerts[rect.spriteID] = verts;
                meshIndices[rect.spriteID] = mesh.tris;
                var edges = new List<Vector2Int>();
                int hull = Mathf.Max(mesh.hull, 3);
                for (int e = 0; e < hull && hull <= vCount; e++) edges.Add(new Vector2Int(e, (e + 1) % hull));
                meshEdges[rect.spriteID] = edges.ToArray();
            }

            dp.SetSpriteRects(spriteRects.ToArray());
            var nameFileId = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameFileId != null)
                nameFileId.SetNameFileIdPairs(spriteRects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
            var boneDp = dp.GetDataProvider<ISpriteBoneDataProvider>();
            var meshDp = dp.GetDataProvider<ISpriteMeshDataProvider>();
            foreach (var r in spriteRects)
            {
                boneDp.SetBones(r.spriteID, boneData[r.spriteID]);
                meshDp.SetVertices(r.spriteID, meshVerts[r.spriteID]);
                meshDp.SetIndices(r.spriteID, meshIndices[r.spriteID]);
                meshDp.SetEdges(r.spriteID, meshEdges[r.spriteID]);
            }
            dp.Apply();
            importer.SaveAndReimport();

            // Phase3에서 쓸 본 매핑 저장
            File.WriteAllText(OutDir + "sprite_bone_map.json", MiniJson.Serialize(spriteBoneNames.ToDictionary(kv => kv.Key, kv => (object)kv.Value.Cast<object>().ToList())));
            AssetDatabase.Refresh();
            Debug.Log($"[UnityRigTest] Phase2 완료: {spriteRects.Count} sprites (bones+mesh+weights 주입)");
        }

        // ---------------------------------------------------------------- Phase 3

        public static void Phase3_BuildRigPrefab()
        {
            var m = Load();
            var setup = ComputeSetupWorld(m);
            var boneMapJson = (Dictionary<string, object>)MiniJson.Deserialize(File.ReadAllText(OutDir + "sprite_bone_map.json"));

            var sprites = AssetDatabase.LoadAllAssetsAtPath(OutPngPath).OfType<Sprite>().ToDictionary(s => s.name);
            if (sprites.Count == 0) { Debug.LogError("[UnityRigTest] 스프라이트 없음 — Phase2 먼저"); return; }

            GameObject old;
            while ((old = GameObject.Find("TEST_SoldierAxe_UnityRig")) != null) UnityEngine.Object.DestroyImmediate(old);

            var root = new GameObject("TEST_SoldierAxe_UnityRig");
            var bonesRoot = new GameObject("Bones"); bonesRoot.transform.SetParent(root.transform, false);
            var partsRoot = new GameObject("Parts"); partsRoot.transform.SetParent(root.transform, false);

            var boneGos = new Transform[m.bones.Count];
            for (int i = 0; i < m.bones.Count; i++)
            {
                var b = m.bones[i];
                var go = new GameObject(b.name);
                go.transform.SetParent(b.parent < 0 ? bonesRoot.transform : boneGos[b.parent], false);
                UnityLocal(m, setup, i, out var pos, out var rot, out var scl);
                go.transform.localPosition = new Vector3(pos.x / PPU, pos.y / PPU, 0);
                go.transform.localRotation = Quaternion.Euler(0, 0, rot);
                go.transform.localScale = new Vector3(scl.x, scl.y, 1);
                boneGos[i] = go.transform;
            }
            var boneByName = new Dictionary<string, Transform>();
            for (int i = 0; i < m.bones.Count; i++) boneByName[m.bones[i].name] = boneGos[i];

            int order = 0;
            foreach (var slot in m.slots)
            {
                if (!m.meshBySlot.TryGetValue(slot.name, out var mesh)) { order++; continue; }
                if (!sprites.TryGetValue(mesh.regionName, out var sprite)) { order++; continue; }
                var go = new GameObject(slot.name);
                go.transform.SetParent(partsRoot.transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = order++;
                sr.color = new Color(1, 1, 1, slot.alpha);

                var skin = go.AddComponent<SpriteSkin>();
                var expectedNames = ((List<object>)boneMapJson[mesh.regionName]).Select(o => (string)o).ToList();
                var transforms = expectedNames.Select(n => boneByName[n]).ToArray();
                var so = new SerializedObject(skin);
                so.FindProperty("m_RootBone").objectReferenceValue = transforms[0];
                var arr = so.FindProperty("m_BoneTransforms");
                arr.arraySize = transforms.Length;
                for (int k = 0; k < transforms.Length; k++) arr.GetArrayElementAtIndex(k).objectReferenceValue = transforms[k];
                so.ApplyModifiedPropertiesWithoutUndo();
                skin.enabled = false; skin.enabled = true;
            }

            Directory.CreateDirectory(OutDir);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
            Debug.Log($"[UnityRigTest] Phase3 완료: {PrefabPath} (bones:{m.bones.Count})");
        }

        // ---------------------------------------------------------------- Phase 4

        class Sampler
        {
            public Model m;
            public Dictionary<string, object> anim;
            public float duration;
            public Dictionary<string, List<object>> deformTl = new Dictionary<string, List<object>>(); // slot → deform 키 목록

            public Sampler(Model m, Dictionary<string, object> anim)
            {
                this.m = m; this.anim = anim;
                duration = 0.001f;
                CollectDuration(anim);
                if (anim.ContainsKey("attachments"))
                    foreach (var skinKv in (Dictionary<string, object>)anim["attachments"])
                        foreach (var slotKv in (Dictionary<string, object>)skinKv.Value)
                            foreach (var attKv in (Dictionary<string, object>)slotKv.Value)
                            {
                                var attTl = (Dictionary<string, object>)attKv.Value;
                                if (attTl.ContainsKey("deform")) deformTl[slotKv.Key] = (List<object>)attTl["deform"];
                            }
            }

            // FFD deform 델타 배열 샘플 (컴포넌트 선형보간, 키의 offset/부분배열 처리)
            public float[] DeformAt(string slot, float t, int compCount)
            {
                if (!deformTl.TryGetValue(slot, out var keys) || keys.Count == 0) return null;
                float[] Materialize(Dictionary<string, object> k)
                {
                    var full = new float[compCount];
                    if (k.ContainsKey("vertices"))
                    {
                        var fa = FA(k["vertices"]);
                        int off = k.ContainsKey("offset") ? (int)Convert.ToDouble(k["offset"]) : 0;
                        for (int i = 0; i < fa.Length && off + i < compCount; i++) full[off + i] = fa[i];
                    }
                    return full;
                }
                float prevT = 0; Dictionary<string, object> prevK = null;
                foreach (Dictionary<string, object> k in keys)
                {
                    float kt = k.ContainsKey("time") ? (float)Convert.ToDouble(k["time"]) : 0;
                    if (t <= kt)
                    {
                        var cur = Materialize(k);
                        if (prevK == null) return cur;
                        var prev = Materialize(prevK);
                        float a = (kt - prevT) < 1e-5f ? 1f : (t - prevT) / (kt - prevT);
                        for (int i = 0; i < compCount; i++) prev[i] = Mathf.Lerp(prev[i], cur[i], a);
                        return prev;
                    }
                    prevT = kt; prevK = k;
                }
                return Materialize(prevK);
            }
            void CollectDuration(object node)
            {
                if (node is Dictionary<string, object> d) foreach (var v in d.Values) CollectDuration(v);
                else if (node is List<object> l)
                    foreach (var item in l)
                        if (item is Dictionary<string, object> key && key.ContainsKey("time"))
                            duration = Mathf.Max(duration, (float)Convert.ToDouble(key["time"]));
            }

            // 보간: 키의 "curve" 문자열이 구간(해당 키→다음 키)의 이징을 결정
            // stepped=계단 / easeIn=가속(낙하) / easeOut=감속(반동) / ease=스무스 / 없으면 선형
            static float SampleCurve(List<object> keys, float t, string valueKey, float def)
            {
                float prevT = 0, prevV = def; bool first = true; string prevCurve = null;
                foreach (Dictionary<string, object> k in keys)
                {
                    float kt = k.ContainsKey("time") ? (float)Convert.ToDouble(k["time"]) : 0;
                    float kv = k.ContainsKey(valueKey) ? (float)Convert.ToDouble(k[valueKey]) : def;
                    if (t <= kt)
                    {
                        if (first || kt <= prevT) return kv;
                        if (prevCurve == "stepped") return prevV;
                        float f = (t - prevT) / (kt - prevT);
                        switch (prevCurve)
                        {
                            case "easeIn": f = f * f; break;
                            case "easeOut": f = 1f - (1f - f) * (1f - f); break;
                            case "ease": f = f * f * (3f - 2f * f); break;
                        }
                        return Mathf.Lerp(prevV, kv, f);
                    }
                    prevT = kt; prevV = kv; first = false;
                    prevCurve = k.ContainsKey("curve") && k["curve"] is string cs ? cs : null;
                }
                return prevV;
            }

            // 패스 컨스트레인트 position 타임라인 샘플
            public float PathPosAt(string name, float def, float t)
            {
                if (!anim.ContainsKey("path")) return def;
                var d = (Dictionary<string, object>)anim["path"];
                if (!d.ContainsKey(name)) return def;
                var tl = (Dictionary<string, object>)d[name];
                if (!tl.ContainsKey("position")) return def;
                return SampleCurve((List<object>)tl["position"], t, "value", def);
            }

            public XForm[] LocalsAt(float t)
            {
                var locals = m.bones.Select(b => new XForm { px = b.x, py = b.y, rot = b.rot, sx = b.sx, sy = b.sy }).ToArray();
                if (!anim.ContainsKey("bones")) return locals;
                foreach (var kv in (Dictionary<string, object>)anim["bones"])
                {
                    if (!m.boneIndex.TryGetValue(kv.Key, out int bi)) continue;
                    var tl = (Dictionary<string, object>)kv.Value;
                    var b = m.bones[bi];
                    foreach (var tkv in tl)
                    {
                        var keys = (List<object>)tkv.Value;
                        switch (tkv.Key)
                        {
                            case "rotate": locals[bi].rot = b.rot + SampleCurve(keys, t, "value", 0); break;
                            case "translate":
                                locals[bi].px = b.x + SampleCurve(keys, t, "x", 0);
                                locals[bi].py = b.y + SampleCurve(keys, t, "y", 0); break;
                            case "translatex": locals[bi].px = b.x + SampleCurve(keys, t, "value", 0); break;
                            case "translatey": locals[bi].py = b.y + SampleCurve(keys, t, "value", 0); break;
                            case "scale":
                                locals[bi].sx = b.sx * SampleCurve(keys, t, "x", 1);
                                locals[bi].sy = b.sy * SampleCurve(keys, t, "y", 1); break;
                            case "scalex": locals[bi].sx = b.sx * SampleCurve(keys, t, "value", 1); break;
                            case "scaley": locals[bi].sy = b.sy * SampleCurve(keys, t, "value", 1); break;
                        }
                    }
                }
                return locals;
            }

            public float SlotAlphaAt(string slotName, float t, float def)
            {
                if (!anim.ContainsKey("slots")) return def;
                var slots = (Dictionary<string, object>)anim["slots"];
                if (!slots.ContainsKey(slotName)) return def;
                var tl = (Dictionary<string, object>)slots[slotName];
                if (tl.ContainsKey("alpha")) return SampleCurve((List<object>)tl["alpha"], t, "value", def);
                if (tl.ContainsKey("rgba"))
                {
                    // 색상 키에서 알파만 추출 (마지막 2자리)
                    var keys = (List<object>)tl["rgba"];
                    float prevT = 0, prevV = def; bool first = true;
                    foreach (Dictionary<string, object> k in keys)
                    {
                        float kt = k.ContainsKey("time") ? (float)Convert.ToDouble(k["time"]) : 0;
                        string hex = (string)k["color"];
                        float kv = hex.Length >= 8 ? Convert.ToInt32(hex.Substring(6, 2), 16) / 255f : 1f;
                        if (t <= kt) return first ? kv : Mathf.Lerp(prevV, kv, (t - prevT) / Mathf.Max(kt - prevT, 1e-4f));
                        prevT = kt; prevV = kv; first = false;
                    }
                    return prevV;
                }
                return def;
            }
        }

        public static void Phase4_BakeAnimations()
        {
            var m = Load();
            var paths = BonePaths(m);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var clips = new Dictionary<string, AnimationClip>();
            foreach (var animKv in m.animations)
                clips[animKv.Key] = BakeClip(m, paths, animKv.Key, (Dictionary<string, object>)animKv.Value);
            FinishController(controller, clips);
        }

        static string[] BonePaths(Model m)
        {
            var paths = new string[m.bones.Count];
            for (int i = 0; i < m.bones.Count; i++)
            {
                var sb = new List<string> { m.bones[i].name };
                int p = m.bones[i].parent;
                while (p >= 0) { sb.Insert(0, m.bones[p].name); p = m.bones[p].parent; }
                paths[i] = "Bones/" + string.Join("/", sb);
            }
            return paths;
        }

        static AnimationClip BakeClip(Model m, string[] paths, string animName, Dictionary<string, object> animData)
        {
            {
                var sampler = new Sampler(m, animData);
                int frames = Mathf.CeilToInt(sampler.duration * FPS) + 1;

                var clip = new AnimationClip { frameRate = FPS, name = animName };

                var posX = new AnimationCurve[m.bones.Count]; var posY = new AnimationCurve[m.bones.Count];
                var rotZ = new AnimationCurve[m.bones.Count];
                var sclX = new AnimationCurve[m.bones.Count]; var sclY = new AnimationCurve[m.bones.Count];
                for (int i = 0; i < m.bones.Count; i++)
                {
                    posX[i] = new AnimationCurve(); posY[i] = new AnimationCurve(); rotZ[i] = new AnimationCurve();
                    sclX[i] = new AnimationCurve(); sclY[i] = new AnimationCurve();
                }
                var prevRot = new float[m.bones.Count];
                var slotAlphaCurves = new Dictionary<string, AnimationCurve>();

                for (int f = 0; f < frames; f++)
                {
                    float t = Mathf.Min(f / FPS, sampler.duration);
                    XForm[] world;
                    if (m.animations.ContainsKey(animName))
                        world = SpineWorldAt(m, animName, t); // 원본 애니메이션은 스파인 런타임으로 100% 동일하게
                    else
                    {
                        // 커스텀(Death 등)은 수제 파이프라인 (패스 → IK → 발 핀)
                        var pathPos = new Dictionary<string, float>();
                        foreach (var pc in m.pathConstraints) pathPos[pc.name] = sampler.PathPosAt(pc.name, pc.position, t);
                        world = ComputeWorld(m, sampler.LocalsAt(t), applyIk: true, pathPos);
                    }

                    // FFD 근사: 본 1개짜리 메시의 deform을 그 본의 유사변환(회전·이동·스케일)으로 피팅해 본에 합성.
                    // 자식 본 월드는 그대로라 UnityLocal 역산에서 자동 보상됨(발이 다리 deform에 끌려가지 않음).
                    var deformedBones = new HashSet<int>();
                    foreach (var dslot in m.slots)
                    {
                        if (!m.meshBySlot.TryGetValue(dslot.name, out var dmesh)) continue;
                        var infl = InfluencingBones(m, dmesh, dslot);
                        if (infl.Count != 1 || deformedBones.Contains(infl[0])) continue;
                        var coords = SingleBoneInfluenceCoords(dmesh);
                        var delta = sampler.DeformAt(dslot.name, t, coords.Length);
                        if (delta == null) continue;
                        var P = new List<Vector2>(); var Q = new List<Vector2>();
                        for (int vi = 0; vi * 2 + 1 < coords.Length; vi++)
                        {
                            P.Add(new Vector2(coords[vi * 2], coords[vi * 2 + 1]));
                            Q.Add(new Vector2(coords[vi * 2] + delta[vi * 2], coords[vi * 2 + 1] + delta[vi * 2 + 1]));
                        }
                        FitSimilarity(P, Q, out float dTheta, out float dS, out Vector2 dT);
                        int bi = infl[0];
                        var wpos = world[bi].Apply(dT.x, dT.y);
                        world[bi].px = wpos.x; world[bi].py = wpos.y;
                        world[bi].rot += dTheta;
                        world[bi].sx *= dS; world[bi].sy *= dS;
                        deformedBones.Add(bi);
                    }

                    for (int i = 0; i < m.bones.Count; i++)
                    {
                        UnityLocal(m, world, i, out var pos, out var rot, out var scl);
                        if (f > 0) { float dr = Mathf.DeltaAngle(prevRot[i], rot); rot = prevRot[i] + dr; }
                        prevRot[i] = rot;
                        posX[i].AddKey(new Keyframe(t, pos.x / PPU));
                        posY[i].AddKey(new Keyframe(t, pos.y / PPU));
                        rotZ[i].AddKey(new Keyframe(t, rot));
                        sclX[i].AddKey(new Keyframe(t, scl.x));
                        sclY[i].AddKey(new Keyframe(t, scl.y));
                    }
                    foreach (var slot in m.slots)
                    {
                        if (!m.meshBySlot.ContainsKey(slot.name)) continue;
                        float a = sampler.SlotAlphaAt(slot.name, t, slot.alpha);
                        if (!slotAlphaCurves.TryGetValue(slot.name, out var cur))
                        {
                            if (Mathf.Approximately(a, slot.alpha) && f == 0) { }
                            cur = new AnimationCurve(); slotAlphaCurves[slot.name] = cur;
                        }
                        cur.AddKey(new Keyframe(t, a));
                    }
                }

                for (int i = 0; i < m.bones.Count; i++)
                {
                    Smooth(posX[i]); Smooth(posY[i]); Smooth(rotZ[i]); Smooth(sclX[i]); Smooth(sclY[i]);
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "m_LocalPosition.x"), posX[i]);
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "m_LocalPosition.y"), posY[i]);
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "localEulerAnglesRaw.z"), rotZ[i]);
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "m_LocalScale.x"), sclX[i]);
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "m_LocalScale.y"), sclY[i]);
                }
                foreach (var kv in slotAlphaCurves)
                {
                    bool constant = true; float v0 = kv.Value.keys.Length > 0 ? kv.Value.keys[0].value : 1f;
                    foreach (var k in kv.Value.keys) if (!Mathf.Approximately(k.value, v0)) { constant = false; break; }
                    if (constant) continue;
                    Smooth(kv.Value);
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Parts/" + kv.Key, typeof(SpriteRenderer), "m_Color.a"), kv.Value);
                }

                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = animName == "Idle";
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                AssetDatabase.CreateAsset(clip, OutDir + "SoldierAxe_" + animName + ".anim");
                return clip;
            }
        }

        static void FinishController(AnimatorController controller, Dictionary<string, AnimationClip> clips)
        {
            // 스테이트 구성: Idle 기본, 나머지는 종료 후 Idle 복귀
            var sm = controller.layers[0].stateMachine;
            var stateByName = new Dictionary<string, AnimatorState>();
            foreach (var kv in clips)
            {
                var st = sm.AddState(kv.Key);
                st.motion = kv.Value;
                stateByName[kv.Key] = st;
            }
            sm.defaultState = stateByName["Idle"];
            foreach (var kv in stateByName)
            {
                if (kv.Key == "Idle") continue;
                var tr = kv.Value.AddTransition(stateByName["Idle"]);
                tr.hasExitTime = true; tr.exitTime = 1f; tr.duration = 0.1f;
            }

            // 프리팹에 Animator 연결
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null)
            {
                using (var scope = new PrefabUtility.EditPrefabContentsScope(PrefabPath))
                {
                    var animator = scope.prefabContentsRoot.GetComponent<Animator>();
                    if (animator == null) animator = scope.prefabContentsRoot.AddComponent<Animator>();
                    animator.runtimeAnimatorController = controller;
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[UnityRigTest] Phase4 완료: {clips.Count} clips + controller");
        }

        static void Smooth(AnimationCurve c)
        {
            for (int i = 0; i < c.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(c, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(c, i, AnimationUtility.TangentMode.ClampedAuto);
            }
        }

        // IK bend 방향 판정: 셋업 포즈(=IK 미적용 = 정답)와 bend ±1 해의 다리 본 회전 차이 비교
        public static void DiagnoseIk()
        {
            var m = Load();
            int saved = IkMode;
            IkMode = 0;
            var truth = ComputeSetupWorld(m);
            IkMode = 1;
            var plus = ComputeSetupWorld(m);
            IkMode = -1;
            var minus = ComputeSetupWorld(m);
            IkMode = saved;
            var sb = new StringBuilder();
            foreach (var ik in m.iks)
                foreach (var bn in ik.bones)
                {
                    int bi = m.boneIndex[bn];
                    sb.AppendLine($"{bn}: truth={truth[bi].rot:F1} bend+1={plus[bi].rot:F1} (d{Mathf.DeltaAngle(plus[bi].rot, truth[bi].rot):F1}) bend-1={minus[bi].rot:F1} (d{Mathf.DeltaAngle(minus[bi].rot, truth[bi].rot):F1})");
                }
            Debug.Log("[UnityRigTest] IK diag\n" + sb.ToString());
        }

        // ---------------------------------------------------------------- 커스텀 애니메이션 (스파인 레퍼런스 없이 글 설명으로 제작)

        // 키 빌더: (time, value) 쌍 나열 — 기본 이징은 스무스(ease)
        static List<object> RK(params float[] tv)
        {
            var l = new List<object>();
            for (int i = 0; i < tv.Length; i += 2)
                l.Add(new Dictionary<string, object> { { "time", (double)tv[i] }, { "value", (double)tv[i + 1] }, { "curve", "ease" } });
            return l;
        }
        // 키 빌더: (time, x, y) 트리플 나열 — 기본 이징은 스무스(ease)
        static List<object> TK(params float[] txy)
        {
            var l = new List<object>();
            for (int i = 0; i < txy.Length; i += 3)
                l.Add(new Dictionary<string, object> { { "time", (double)txy[i] }, { "x", (double)txy[i + 1] }, { "y", (double)txy[i + 2] }, { "curve", "ease" } });
            return l;
        }
        // 특정 키의 구간 이징 지정 (keyIndex 키 → 다음 키 구간)
        static List<object> Curve(List<object> keys, int keyIndex, string type)
        {
            ((Dictionary<string, object>)keys[keyIndex])["curve"] = type;
            return keys;
        }
        static Dictionary<string, object> Bone(string timeline, List<object> keys) => new Dictionary<string, object> { { timeline, keys } };

        // Death: 고개를 들어 하늘을 보고 → 무릎을 꿇고 → 앞으로 꼬꾸라짐 (약 1.8초, 비루프)
        // 골반 하강/전진은 Center 이동으로 표현 → 패스 컨스트레인트가 Body1을 따라오게 하고
        // IK가 발바닥을 고정한 채 무릎을 접는다. 회전 부호는 스크린샷 반복 검증으로 조정.
        static Dictionary<string, object> BuildDeathAnim()
        {
            // 컨셉: Buff0 참고 — 상체·머리를 쭉 펴고 하늘 응시(0~0.75s) → 척추는 편 채로 골반만 크게 회전해
            // 앞으로 쭉 엎드림(0.75~1.35s) → 착지 바운스 후 정지(~2.0s). 팔은 키 없음(기본 자세 유지).
            // 부호 규약: Head/Body 음수=들기·젖힘, 양수=숙임. 발 타깃은 y=0 유지(바닥 고정), x만 슬라이드.
            var bones = new Dictionary<string, object>
            {
                // 골반: 응시 중 살짝 상승 → 가속 낙하(easeIn) → 착지 반동 감속(easeOut)
                ["Center"] = Bone("translate", Curve(Curve(TK(
                    0.00f, 0, 0,
                    0.40f, 0, 8,
                    0.75f, 0, 5,
                    1.30f, -120, -174,
                    1.42f, -114, -166,
                    1.55f, -118, -172,
                    2.00f, -118, -172), 2, "easeIn"), 3, "easeOut")),
                // 상체 곧게 펴기(Buff0 값 참고) — 엎드릴 때도 척추는 편 상태 유지, 회전은 골반(Body1)이 낙하와 같이 가속
                ["Body1"] = Bone("rotate", Curve(RK(0.00f, 0, 0.75f, 0, 1.30f, 95, 2.00f, 95), 1, "easeIn")),
                ["Body2"] = Bone("rotate", RK(0.00f, 0, 0.40f, -26, 2.00f, -26)),
                ["Body3"] = Bone("rotate", RK(0.00f, 0, 0.40f, -22, 2.00f, -22)),
                ["neck"] = Bone("rotate", RK(0.00f, 0, 0.40f, -13, 2.00f, -13)),
                // 고개: 하늘 응시 → 엎드리며 낙하와 같이 가속으로 떨굼
                ["Head"] = Bone("rotate", Curve(RK(0.00f, 0, 0.40f, -50, 0.85f, -45, 1.35f, -5, 2.00f, -5), 2, "easeIn")),
                // 발(IK 타깃): 왼발(뒷발) 고정, 오른발은 몸에 끌려 가속으로 뒤로 쓸림
                ["RightLeg"] = Bone("translate", Curve(TK(0.00f, 0, 0, 0.75f, 0, 0, 1.35f, 200, 0, 2.00f, 200, 0), 1, "easeIn"))
            };
            var slots = new Dictionary<string, object>
            {
                // 눈 발광: 응시에 점등 → 엎드리며 소등
                ["EyeLight"] = Bone("alpha", RK(0.00f, 0, 0.35f, 1, 0.90f, 0.9f, 1.35f, 0, 2.00f, 0)),
                ["EyeLight2"] = Bone("alpha", RK(0.00f, 0, 0.35f, 1, 0.90f, 0.9f, 1.35f, 0, 2.00f, 0))
            };
            return new Dictionary<string, object> { { "bones", bones }, { "slots", slots } };
        }

        [MenuItem("Tools/TEST UnityRig/Bake Death Animation")]
        public static void BakeDeathAnimation()
        {
            var m = Load();
            var clip = BakeClip(m, BonePaths(m), "Death", BuildDeathAnim());
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) { Debug.LogError("[UnityRigTest] controller 없음 — Phase4 먼저"); return; }
            var sm = controller.layers[0].stateMachine;
            foreach (var st in sm.states)
                if (st.state.name == "Death") { sm.RemoveState(st.state); break; }
            var state = sm.AddState("Death");
            state.motion = clip; // 비루프, 전환 없음 — 마지막 프레임(쓰러진 자세) 유지
            AssetDatabase.SaveAssets();
            Debug.Log("[UnityRigTest] Death 애니메이션 베이크 완료");
        }

        // 발바닥 고정 검증: 애니메이션 전체 구간에서 발/골반 월드 좌표 출력 ("Death"는 커스텀 정의 사용)
        public static void DiagnoseFeet(string animName = "Idle")
        {
            var m = Load();
            var anim = animName == "Death" ? BuildDeathAnim() : (Dictionary<string, object>)m.animations[animName];
            var sampler = new Sampler(m, anim);
            var sb = new StringBuilder();
            foreach (var name in new[] { "RightFoot", "LeftFoot", "Body1" })
            {
                int bi = m.boneIndex[name];
                sb.Append(name + ": ");
                for (float t = 0; t <= sampler.duration + 0.01f; t += sampler.duration / 6f)
                {
                    float tc = Mathf.Min(t, sampler.duration);
                    XForm[] world;
                    if (animName != "Death")
                        world = SpineWorldAt(m, animName, tc);
                    else
                    {
                        var pathPos = new Dictionary<string, float>();
                        foreach (var pc in m.pathConstraints) pathPos[pc.name] = sampler.PathPosAt(pc.name, pc.position, tc);
                        world = ComputeWorld(m, sampler.LocalsAt(tc), true, pathPos);
                    }
                    sb.Append($"({world[bi].px:F0},{world[bi].py:F0}) ");
                }
                sb.AppendLine();
            }
            Debug.Log($"[UnityRigTest] {animName} 발/골반 월드 좌표 (px)\n" + sb.ToString());
        }

        // ---------------------------------------------------------------- Phase 5

        public static void Phase5_BuildCompareScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 2.2f;
            cam.transform.position = new Vector3(0, 1.4f, -10);
            cam.backgroundColor = new Color(0.16f, 0.16f, 0.2f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();

            // 왼쪽: 원본 Spine
            var dataAsset = AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>(SrcDir + "S1_M01_SkeletonData.asset");
            Spine.Unity.SkeletonAnimation spineObj = null;
            if (dataAsset != null)
            {
                spineObj = Spine.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(dataAsset);
                spineObj.gameObject.name = "Spine_SoldierAxe";
                spineObj.transform.position = new Vector3(-1.6f, 0, 0);
                spineObj.Initialize(false);
                spineObj.AnimationState.SetAnimation(0, "Idle", true);
                spineObj.Update(0);
            }
            else Debug.LogError("[UnityRigTest] SkeletonDataAsset 로드 실패");

            // 오른쪽: Unity 리그
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject rigInstance = null;
            if (prefab != null)
            {
                rigInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                rigInstance.transform.position = new Vector3(1.6f, 0, 0);
            }

            // 비교 드라이버
            var driverGo = new GameObject("CompareDriver");
            var driver = driverGo.AddComponent<UnityRigCompareDriver>();
            driver.spine = spineObj;
            driver.unityRig = rigInstance != null ? rigInstance.GetComponent<Animator>() : null;

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[UnityRigTest] Phase5 완료: {ScenePath}");
        }
    }

    // ---------------------------------------------------------------- 최소 JSON 파서/직렬화
    public static class MiniJson
    {
        public static object Deserialize(string json) { int i = 0; return ParseValue(json, ref i); }

        static object ParseValue(string s, ref int i)
        {
            SkipWs(s, ref i);
            char c = s[i];
            if (c == '{') return ParseObject(s, ref i);
            if (c == '[') return ParseArray(s, ref i);
            if (c == '"') return ParseString(s, ref i);
            if (c == 't') { i += 4; return true; }
            if (c == 'f') { i += 5; return false; }
            if (c == 'n') { i += 4; return null; }
            return ParseNumber(s, ref i);
        }
        static Dictionary<string, object> ParseObject(string s, ref int i)
        {
            var d = new Dictionary<string, object>(); i++;
            SkipWs(s, ref i);
            if (s[i] == '}') { i++; return d; }
            while (true)
            {
                SkipWs(s, ref i);
                string key = ParseString(s, ref i);
                SkipWs(s, ref i); i++; // ':'
                d[key] = ParseValue(s, ref i);
                SkipWs(s, ref i);
                if (s[i] == ',') { i++; continue; }
                i++; return d; // '}'
            }
        }
        static List<object> ParseArray(string s, ref int i)
        {
            var l = new List<object>(); i++;
            SkipWs(s, ref i);
            if (s[i] == ']') { i++; return l; }
            while (true)
            {
                l.Add(ParseValue(s, ref i));
                SkipWs(s, ref i);
                if (s[i] == ',') { i++; continue; }
                i++; return l; // ']'
            }
        }
        static string ParseString(string s, ref int i)
        {
            var sb = new StringBuilder(); i++;
            while (s[i] != '"')
            {
                if (s[i] == '\\')
                {
                    i++;
                    switch (s[i])
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; break;
                        default: sb.Append(s[i]); break;
                    }
                }
                else sb.Append(s[i]);
                i++;
            }
            i++;
            return sb.ToString();
        }
        static object ParseNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '-' || s[i] == '+' || s[i] == '.' || s[i] == 'e' || s[i] == 'E')) i++;
            return double.Parse(s.Substring(start, i - start), System.Globalization.CultureInfo.InvariantCulture);
        }
        static void SkipWs(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        public static string Serialize(object o)
        {
            var sb = new StringBuilder();
            Write(o, sb);
            return sb.ToString();
        }
        static void Write(object o, StringBuilder sb)
        {
            if (o == null) { sb.Append("null"); return; }
            if (o is string s) { sb.Append('"').Append(s.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"'); return; }
            if (o is bool b) { sb.Append(b ? "true" : "false"); return; }
            if (o is Dictionary<string, object> d)
            {
                sb.Append('{'); bool first = true;
                foreach (var kv in d) { if (!first) sb.Append(','); Write(kv.Key, sb); sb.Append(':'); Write(kv.Value, sb); first = false; }
                sb.Append('}'); return;
            }
            if (o is List<object> l)
            {
                sb.Append('['); bool first = true;
                foreach (var v in l) { if (!first) sb.Append(','); Write(v, sb); first = false; }
                sb.Append(']'); return;
            }
            sb.Append(Convert.ToString(o, System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
