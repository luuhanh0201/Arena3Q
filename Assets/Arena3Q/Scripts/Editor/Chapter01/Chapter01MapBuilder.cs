using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Arena3Q.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;
using Candidate = Arena3Q.Editor.Chapter01.Chapter01ColorReference.Candidate;

namespace Arena3Q.Editor.Chapter01
{
    /// <summary>
    /// Builds the Chapter 1 exploration map (Chuong1.md §9 to §18, stage A step 7) from CH01_MapLayout.json:
    /// a terrain mesh (hills outside the play area, river bed, gentle mounds) painted through the vertex colours
    /// of the battlefield's Blended Ground shader (road, gravel yards and banks, paddy fields, grass), the seven
    /// stage areas with their buildings and props, bridges where the main road crosses the river, scattered
    /// vegetation that keeps clear of roads and buildings, NPC placeholders and stage markers for Codex, and the
    /// release light and camera. Every number and placement lives in the layout file.
    /// </summary>
    public static class Chapter01MapBuilder
    {
        private const string Root = "Assets/Arena3Q/Chapters/Chapter01_YellowTurban";
        private const string LayoutPath = Root + "/Data/CH01_MapLayout.json";
        private const string ScenePath = Root + "/Scenes/CH01_Exploration.unity";
        private const string Local = "Assets/ThirdParty/Chapter01/_Map";
        private const string Renders = "ArtSource/Chapter01/3D/Renders/Map";
        private const string Codex = "ArtSource/Chapter01/2D/Assets/Export/1024";

#pragma warning disable 0649
        [Serializable] private class MapLayout
        {
            public float[] size; public float skirt; public float resolution; public int seed;
            public Line road; public Line[] paths; public Line river;
            public Box[] paddies; public Box[] yards; public Disc[] meadows;
            public AssetDef[] assets; public Area[] areas; public Prop[] props; public Scatter[] scatter; public Npc[] npcs; public View[] views;
        }
        [Serializable] private class Line { public float width, depth; public float[] points; }
        [Serializable] private class Box { public float[] centre, size; }
        [Serializable] private class Disc { public float[] centre; public float radius; }
        [Serializable] private class AssetDef { public string key, source, texture; public float height; }
        [Serializable] private class Area { public int id; public string name; public float[] centre; public float radius; }
        [Serializable] private class Prop { public string asset; public float[] pos; public float yaw, roll, y, scale; public int area; }
        [Serializable] private class Scatter { public string name, zone; public string[] assets; public int count; public float spacing, radius, inner, clearance; public float[] centre, scale; }
        [Serializable] private class Npc { public string kind; public float[] pos; public float y; public int area; }
        [Serializable] private class View { public string name; public float[] focus; public float size; public bool top; }
#pragma warning restore 0649

        private static MapLayout _m;
        private static List<Vector2> _road, _river;
        private static List<List<Vector2>> _paths;
        private static readonly List<(Vector2 c, float r)> Occupied = new();
        private static System.Random _rng;

        /// <summary>Batch entry: kitbash prefabs first (the map uses them), then the map.</summary>
        public static void BuildWithKitbash() { Chapter01Kitbash.Build(); Build(); }

        [MenuItem("Arena3Q/Chapter 01/Build Exploration Map")]
        public static void Build()
        {
            _m = JsonUtility.FromJson<MapLayout>(File.ReadAllText(LayoutPath));
            _rng = new System.Random(_m.seed); Occupied.Clear();
            _road = Smooth(_m.road.points); _river = Smooth(_m.river.points); _paths = _m.paths.Select(p => Smooth(p.points)).ToList();
            var library = AssetDatabase.LoadAssetAtPath<BattlefieldAssetLibrary>("Assets/Arena3Q/Content/Battlefield/BattlefieldAssetLibrary.asset");
            var config = AssetDatabase.LoadAssetAtPath<BattlefieldConfig>("Assets/Arena3Q/Content/Battlefield/BattlefieldConfig.asset");
            PrepareFolders();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = Chapter01ColorReference.Stage(config, out var stageGround); Object.DestroyImmediate(stageGround);
            camera.name = "Exploration camera";
            var world = new GameObject("CH01 Exploration").transform;
            Chapter01ColorReference._materialFolder = Local + "/Materials";
            Chapter01ColorReference.SharedMaterials = new Dictionary<string, Material>();
            var report = new StringBuilder();
            try
            {
                Terrain(world);
                Water(world);
                var assets = _m.assets.ToDictionary(a => a.key);
                var groups = new Dictionary<int, Transform>();
                foreach (var area in _m.areas)
                {
                    var g = new GameObject($"Area {area.id:00} {area.name}").transform; g.SetParent(world, false); groups[area.id] = g;
                    var marker = new GameObject($"StageMarker_{area.id:00}").transform; marker.SetParent(g, false);
                    marker.position = Ground(area.centre[0], area.centre[1]);
                }
                var shared = new GameObject("Shared (roads, bridges)").transform; shared.SetParent(world, false);
                int placed = 0;
                foreach (var p in _m.props)
                {
                    var parent = p.area > 0 && groups.TryGetValue(p.area, out var g) ? g : shared;
                    if (Place(p.asset, assets, library, new Vector2(p.pos[0], p.pos[1]), p.yaw, p.roll, p.y, p.scale > 0 ? p.scale : 1f, parent, true) != null) placed++;
                    else report.AppendLine("missing asset " + p.asset);
                }
                foreach (var (pos, yaw) in Crossings())
                {
                    Place("bridge", assets, library, pos, yaw, 0f, 0f, 1f, shared, true, keepHeight: true); placed++;
                }
                int scattered = 0;
                var nature = new GameObject("Nature (scattered)").transform; nature.SetParent(world, false);
                foreach (var s in _m.scatter) scattered += ScatterRule(s, assets, library, nature);
                Npcs(world, groups);
                report.Insert(0, $"props {placed}, scattered {scattered}, npc placeholders {_m.npcs.Length}\n");
                EditorSceneManager.SaveScene(scene, ScenePath);
                Directory.CreateDirectory(Renders);
                float lodBias = QualitySettings.lodBias; QualitySettings.lodBias = 4f;
                try { foreach (var v in _m.views) Shot(camera, v); }
                finally { QualitySettings.lodBias = lodBias; }
                File.WriteAllText(Renders + "/build-report.txt", report.ToString());
                Debug.Log("[Chapter01] Exploration map: " + report.ToString().Replace('\n', ' '));
            }
            finally { Chapter01ColorReference._materialFolder = null; Chapter01ColorReference.SharedMaterials = null; }
        }

        private static void PrepareFolders()
        {
            if (AssetDatabase.IsValidFolder(Local + "/Materials")) AssetDatabase.DeleteAsset(Local + "/Materials");
            foreach (var f in new[] { Local, Local + "/Materials", Local + "/Textures", Root + "/Scenes", Root + "/Data" }) Directory.CreateDirectory(f);
            foreach (var t in new[] { "01_ground-earth.png", "02_ground-path.png", "03_ground-grass.png", "04_paddy-ground.png", "05_water.png" })
                if (!File.Exists(Local + "/Textures/" + t)) File.Copy(Codex + "/" + t, Local + "/Textures/" + t);
            AssetDatabase.Refresh();
            Chapter01ColorReference.PrepareTextures();
        }

        // ------------------------------------------------------------------------------------------ geometry

        private static List<Vector2> Smooth(float[] flat)
        {
            var p = new List<Vector2>(); for (int i = 0; i + 1 < flat.Length; i += 2) p.Add(new Vector2(flat[i], flat[i + 1]));
            if (p.Count < 3) return p;
            var o = new List<Vector2>();
            for (int i = 0; i < p.Count - 1; i++)
            {
                Vector2 a = p[Mathf.Max(0, i - 1)], b = p[i], c = p[i + 1], d = p[Mathf.Min(p.Count - 1, i + 2)];
                for (int k = 0; k < 8; k++)
                {
                    float t = k / 8f, t2 = t * t, t3 = t2 * t;
                    o.Add(.5f * (2 * b + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t2 + (-a + 3 * b - 3 * c + d) * t3));
                }
            }
            o.Add(p[^1]); return o;
        }

        private static float Distance(List<Vector2> line, Vector2 q)
        {
            float best = float.MaxValue;
            for (int i = 0; i < line.Count - 1; i++)
            {
                Vector2 a = line[i], ab = line[i + 1] - a; float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(1e-5f, ab.sqrMagnitude));
                best = Mathf.Min(best, (a + ab * t - q).sqrMagnitude);
            }
            return Mathf.Sqrt(best);
        }

        private static float PathDistance(Vector2 q, out float width)
        {
            float best = float.MaxValue; width = 0f;
            for (int i = 0; i < _paths.Count; i++) { float d = Distance(_paths[i], q); if (d < best) { best = d; width = _m.paths[i].width; } }
            return best;
        }

        private static float Smoothstep(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); }

        private static float Outside(Vector2 q) => Mathf.Max(0f, Mathf.Max(Mathf.Abs(q.x) - _m.size[0] / 2, Mathf.Abs(q.y) - _m.size[1] / 2));

        private static float BoxMask(Box b, Vector2 q, float soft)
        {
            float dx = Mathf.Abs(q.x - b.centre[0]) - b.size[0] / 2, dz = Mathf.Abs(q.y - b.centre[1]) - b.size[1] / 2;
            return 1f - Smoothstep(-soft, soft, Mathf.Max(dx, dz));
        }

        /// <summary>Ground height: flat under roads and stage areas, gentle mounds, hills outside the play area, river bed.</summary>
        private static float Height(Vector2 q)
        {
            float dRoad = Distance(_road, q), dRiver = Distance(_river, q);
            float flat = Smoothstep(3f, 12f, dRoad);
            foreach (var a in _m.areas) flat = Mathf.Min(flat, Smoothstep(a.radius * .7f, a.radius * 1.2f, Vector2.Distance(q, new Vector2(a.centre[0], a.centre[1]))));
            foreach (var p in _m.paddies) flat = Mathf.Min(flat, 1f - BoxMask(p, q, 3f));
            float h = (Mathf.PerlinNoise(q.x * .045f + 31f, q.y * .045f + 7f) - .5f) * 1.4f * flat;
            float outside = Outside(q);
            // Hills outside the play area, opened into a valley where the river leaves the map.
            h += (Smoothstep(0f, 24f, outside) * 7f + (Mathf.PerlinNoise(q.x * .06f, q.y * .06f) - .3f) * 3f * Smoothstep(2f, 12f, outside)) * Smoothstep(4f, 16f, dRiver);
            float bed = 1f - Smoothstep(_m.river.width / 2 - .2f, _m.river.width / 2 + 1.8f, dRiver);
            return Mathf.Lerp(h, -_m.river.depth, bed);
        }

        private static Vector3 Ground(float x, float z) => new(x, Height(new Vector2(x, z)), z);

        private static void Terrain(Transform world)
        {
            float halfX = _m.size[0] / 2 + _m.skirt, halfZ = _m.size[1] / 2 + _m.skirt, step = _m.resolution;
            int nx = Mathf.CeilToInt(halfX * 2 / step) + 1, nz = Mathf.CeilToInt(halfZ * 2 / step) + 1;
            var vertices = new Vector3[nx * nz]; var colours = new Color[nx * nz]; var dry = new Vector2[nx * nz];
            for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                var q = new Vector2(-halfX + i * step, -halfZ + j * step); int k = j * nx + i;
                vertices[k] = new Vector3(q.x, Height(q), q.y);
                float dRoad = Distance(_road, q), dPath = PathDistance(q, out float pathWidth), dRiver = Distance(_river, q);
                float road = Mathf.Max(1f - Smoothstep(_m.road.width / 2 - .5f, _m.road.width / 2 + .7f, dRoad), 1f - Smoothstep(pathWidth / 2 - .4f, pathWidth / 2 + .6f, dPath));
                float gravel = (1f - Smoothstep(_m.river.width / 2 + .3f, _m.river.width / 2 + 2.4f, dRiver)) * .85f;
                foreach (var y in _m.yards) gravel = Mathf.Max(gravel, BoxMask(y, q, .8f));
                float paddy = 0f; foreach (var p in _m.paddies) paddy = Mathf.Max(paddy, BoxMask(p, q, .35f));
                float meadow = 0f; foreach (var d in _m.meadows) meadow = Mathf.Max(meadow, 1f - Smoothstep(d.radius * .6f, d.radius, Vector2.Distance(q, new Vector2(d.centre[0], d.centre[1]))));
                float wild = Smoothstep(.42f, .62f, Mathf.PerlinNoise(q.x * .07f + 3f, q.y * .07f + 11f));
                float grass = Mathf.Max(meadow, wild * .9f, Smoothstep(1f, 8f, Outside(q)));
                grass *= Smoothstep(1f, 3.5f, Mathf.Min(dRoad - _m.road.width / 2, dPath - pathWidth / 2)) * (1f - paddy) * (1f - gravel);
                colours[k] = new Color(road, gravel * (1f - road), paddy * (1f - road), grass);
                // East and north (Khăn Vàng land) turn drier.
                dry[k] = new Vector2(Mathf.Clamp01(.2f + .6f * Smoothstep(-50f, 60f, q.x + q.y * .5f) + (Mathf.PerlinNoise(q.x * .03f, q.y * .03f) - .5f) * .4f), 0f);
            }
            var tris = new List<int>((nx - 1) * (nz - 1) * 6);
            for (int j = 0; j < nz - 1; j++)
            for (int i = 0; i < nx - 1; i++)
            {
                int a = j * nx + i, b = a + 1, c = a + nx, d = c + 1;
                tris.AddRange(new[] { a, c, b, b, c, d });
            }
            var mesh = new Mesh { name = "CH01_Terrain", indexFormat = IndexFormat.UInt32, vertices = vertices, colors = colours };
            mesh.SetTriangles(tris, 0); mesh.SetUVs(1, dry); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.DeleteAsset(Local + "/CH01_Terrain.asset"); AssetDatabase.CreateAsset(mesh, Local + "/CH01_Terrain.asset");
            var m = new Material(Shader.Find("Arena3Q/Battlefield/Blended Ground")) { name = "CH01_Ground" };
            m.SetTexture("_Sand", Tex("01_ground-earth.png")); m.SetTexture("_Road", Tex("02_ground-path.png"));
            m.SetTexture("_Gravel", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Arena3Q/Art/Battlefield/Textures/GravelSoil.png"));
            m.SetTexture("_Paving", Tex("04_paddy-ground.png")); m.SetTexture("_Dry", Tex("03_ground-grass.png")); m.SetTexture("_Green", Tex("03_ground-grass.png"));
            m.SetFloat("_TileScale", 4f); m.SetFloat("_MacroVariation", .14f);
            AssetDatabase.CreateAsset(m, Local + "/Materials/CH01_Ground.mat");
            var go = new GameObject("Terrain", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            go.transform.SetParent(world, false); go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = m;
            go.GetComponent<MeshCollider>().sharedMesh = mesh; GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic);
        }

        private static Texture2D Tex(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Local + "/Textures/" + name);

        /// <summary>River surface: a strip along the river line just under the bank, Codex's 05 water tile.</summary>
        private static void Water(Transform world)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>(); float half = _m.river.width / 2 + 1.6f, along = 0f;
            for (int i = 0; i < _river.Count; i++)
            {
                Vector2 dir = (_river[Mathf.Min(i + 1, _river.Count - 1)] - _river[Mathf.Max(i - 1, 0)]).normalized, n = new(-dir.y, dir.x);
                if (i > 0) along += Vector2.Distance(_river[i], _river[i - 1]);
                foreach (float s in new[] { -1f, 1f }) { var p = _river[i] + n * half * s; v.Add(new Vector3(p.x, -_m.river.depth * .45f, p.y)); uv.Add(new Vector2(s * half / 4f, along / 4f)); }
                if (i > 0) { int b = v.Count - 4; t.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 }); }
            }
            var mesh = new Mesh { name = "CH01_River" }; mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0); mesh.RecalculateNormals();
            // The strip winding depends on which side the river turns; the surface must face up.
            if (mesh.normals.Length > 0 && mesh.normals[0].y < 0f) { t.Reverse(); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); }
            AssetDatabase.DeleteAsset(Local + "/CH01_River.asset"); AssetDatabase.CreateAsset(mesh, Local + "/CH01_River.asset");
            var m = Chapter01ColorReference.Lit(Tex("05_water.png"), new Color(.85f, .92f, .95f)); m.SetFloat("_Smoothness", .75f); m.name = "CH01_Water";
            AssetDatabase.CreateAsset(m, Local + "/Materials/CH01_Water.mat");
            var go = new GameObject("River", typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(world, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh; var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>Bridges where the main road crosses the river, laid along the road.</summary>
        private static IEnumerable<(Vector2, float)> Crossings()
        {
            for (int i = 0; i < _road.Count - 1; i++)
            for (int j = 0; j < _river.Count - 1; j++)
            {
                if (!Intersect(_road[i], _road[i + 1], _river[j], _river[j + 1], out var p)) continue;
                var d = _road[i + 1] - _road[i];
                yield return (p, -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
        }

        private static bool Intersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 p)
        {
            p = default; Vector2 r = b - a, s = d - c; float den = r.x * s.y - r.y * s.x; if (Mathf.Abs(den) < 1e-6f) return false;
            float t = ((c.x - a.x) * s.y - (c.y - a.y) * s.x) / den, u = ((c.x - a.x) * r.y - (c.y - a.y) * r.x) / den;
            if (t < 0 || t > 1 || u < 0 || u > 1) return false; p = a + r * t; return true;
        }

        // ------------------------------------------------------------------------------------------ placement

        private static GameObject Place(string key, Dictionary<string, AssetDef> assets, BattlefieldAssetLibrary library, Vector2 pos, float yaw, float roll, float y, float scale, Transform parent, bool occupy, bool keepHeight = false)
        {
            Candidate c;
            if (key.StartsWith("lib:")) c = new Candidate("map", key, key, 0f);
            else if (assets.TryGetValue(key, out var a)) c = new Candidate("map", a.key, a.source.StartsWith("kit:") ? Chapter01Kitbash.Prefabs + "/" + a.source.Substring(4) + ".prefab" : a.source, a.height, string.IsNullOrEmpty(a.texture) ? null : a.texture);
            else return null;
            var go = Chapter01ColorReference.Spawn(c, library, out _, out _, out _);
            if (go == null) return null;
            go.name = key.Replace("lib:", "");
            go.transform.localScale *= scale;
            go.transform.rotation = Quaternion.Euler(0f, yaw, roll) * go.transform.rotation;
            // Re-ground after rotation and scale: the lowest point sits on the terrain (or at y above it).
            var b = Chapter01ColorReference.Bounds(Chapter01ColorReference.Visible(go));
            float ground = keepHeight ? 0f : Height(pos);
            go.transform.position += new Vector3(pos.x - b.center.x, ground + y - b.min.y, pos.y - b.center.z);
            go.transform.SetParent(parent, true);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            if (occupy) Occupied.Add((pos, Mathf.Max(b.extents.x, b.extents.z) * .85f));
            return go;
        }

        private static int ScatterRule(Scatter s, Dictionary<string, AssetDef> assets, BattlefieldAssetLibrary library, Transform parent)
        {
            var group = new GameObject(s.name).transform; group.SetParent(parent, false);
            var mine = new List<Vector2>(); int placed = 0, tries = 0;
            float halfX = _m.size[0] / 2, halfZ = _m.size[1] / 2;
            while (placed < s.count && tries++ < s.count * 60)
            {
                Vector2 q;
                if (s.zone == "disc")
                {
                    float a = (float)(_rng.NextDouble() * Mathf.PI * 2), r = Mathf.Lerp(s.inner, s.radius, Mathf.Sqrt((float)_rng.NextDouble()));
                    q = new Vector2(s.centre[0], s.centre[1]) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                }
                else if (s.zone == "skirt")
                {
                    q = new Vector2((float)(_rng.NextDouble() * 2 - 1) * (halfX + _m.skirt * .8f), (float)(_rng.NextDouble() * 2 - 1) * (halfZ + _m.skirt * .8f));
                    if (Outside(q) < 1.5f) continue;
                }
                else
                {
                    q = new Vector2((float)(_rng.NextDouble() * 2 - 1) * (halfX - 2f), (float)(_rng.NextDouble() * 2 - 1) * (halfZ - 2f));
                    if (_m.areas.Any(a => Vector2.Distance(q, new Vector2(a.centre[0], a.centre[1])) < a.radius * .8f)) continue;
                }
                if (Distance(_road, q) < _m.road.width / 2 + s.clearance) continue;
                if (PathDistance(q, out float pw) < pw / 2 + s.clearance * .6f) continue;
                if (Distance(_river, q) < _m.river.width / 2 + s.clearance) continue;
                if (_m.paddies.Any(p => BoxMask(p, q, .1f) > .01f) || _m.yards.Any(p => BoxMask(p, q, .1f) > .01f)) continue;
                if (Occupied.Any(o => Vector2.Distance(o.c, q) < o.r + s.clearance)) continue;
                if (mine.Any(o => Vector2.Distance(o, q) < s.spacing)) continue;
                string key = s.assets[_rng.Next(s.assets.Length)];
                float scale = s.scale != null && s.scale.Length == 2 ? Mathf.Lerp(s.scale[0], s.scale[1], (float)_rng.NextDouble()) : 1f;
                if (Place(key, assets, library, q, (float)(_rng.NextDouble() * 360), 0f, 0f, scale, group, false) == null) continue;
                mine.Add(q); placed++;
            }
            if (s.clearance >= 1f) foreach (var q in mine) Occupied.Add((q, s.spacing * .4f));
            return placed;
        }

        /// <summary>NPC placeholders (capsules by role) for Codex and the character pass; not final art.</summary>
        private static void Npcs(Transform world, Dictionary<int, Transform> groups)
        {
            var colours = new Dictionary<string, Color>
            {
                ["villager"] = new(.85f, .78f, .62f), ["chief"] = new(.95f, .9f, .75f), ["militia"] = new(.35f, .5f, .8f),
                ["yellow"] = new(.95f, .78f, .15f), ["guardian"] = new(.8f, .45f, .1f), ["boss"] = new(.85f, .15f, .12f),
            };
            var materials = new Dictionary<string, Material>();
            var root = new GameObject("NPC placeholders").transform; root.SetParent(world, false);
            foreach (var n in _m.npcs)
            {
                if (!materials.TryGetValue(n.kind, out var mat))
                {
                    mat = Chapter01ColorReference.Lit(null, colours.TryGetValue(n.kind, out var c) ? c : Color.grey); mat.name = "NPC_" + n.kind;
                    AssetDatabase.CreateAsset(mat, Local + "/Materials/NPC_" + n.kind + ".mat"); materials[n.kind] = mat;
                }
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule); Object.DestroyImmediate(go.GetComponent<Collider>());
                float height = n.kind == "boss" ? 2.4f : n.kind == "guardian" ? 2.1f : 1.7f;
                go.name = "NPC_" + n.kind; go.transform.localScale = new Vector3(height * .27f, height / 2, height * .27f);
                go.transform.position = Ground(n.pos[0], n.pos[1]) + Vector3.up * (n.y + height / 2);
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
                go.transform.SetParent(n.area > 0 && groups.TryGetValue(n.area, out var g) ? g : root, true);
            }
        }

        private static void Shot(Camera camera, View v)
        {
            var focus = Ground(v.focus[0], v.focus[1]);
            var previous = camera.transform.rotation;
            if (v.top) camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            camera.orthographicSize = v.size;
            camera.transform.position = focus - camera.transform.forward * 70f;
            Chapter01ColorReference.Capture(camera, Renders + "/" + v.name + ".png", 2560, 1440);
            camera.transform.rotation = previous;
        }
    }
}
