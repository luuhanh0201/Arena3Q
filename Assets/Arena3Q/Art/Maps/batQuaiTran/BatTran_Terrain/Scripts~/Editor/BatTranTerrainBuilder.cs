// Arena3Q — Bat Tran Do terrain builder (Unity Editor tool)
// Put this file in any "Editor" folder, e.g. Assets/Editor/BatTranTerrainBuilder.cs
// Menu: Arena3Q > Bat Tran > Build Terrain
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BatTranTerrainBuilder : EditorWindow
{
    [Serializable] class TerrainInfo { public float sizeX, height, sizeZ, posX, posY, posZ; public int heightRes, alphaRes; }
    [Serializable] class Marker { public string name, kind; public float x, y, rotY; }
    [Serializable] class P2 { public float x, y; }
    [Serializable] class Lane { public string name; public float length; public P2[] points; }
    [Serializable] class Layout { public string map; public TerrainInfo terrain; public Marker[] markers; public Lane[] lanes; }

    TextAsset layoutJson, heightBytes, splatBytes;
    Texture2D painted, grassTex, sandTex, pathTex, rockTex;
    bool usePainted = true;
    float tileSize = 4f;
    string outFolder = "Assets/Art/Maps/BatTran/Generated";

    [MenuItem("Arena3Q/Bat Tran/Build Terrain")]
    static void Open() => GetWindow<BatTranTerrainBuilder>("Bat Tran Terrain");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Data", EditorStyles.boldLabel);
        layoutJson = (TextAsset)EditorGUILayout.ObjectField("BatTran_Layout.json", layoutJson, typeof(TextAsset), false);
        heightBytes = (TextAsset)EditorGUILayout.ObjectField("BatTran_Height.bytes", heightBytes, typeof(TextAsset), false);
        splatBytes = (TextAsset)EditorGUILayout.ObjectField("BatTran_Splat.bytes", splatBytes, typeof(TextAsset), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ground texture", EditorStyles.boldLabel);
        usePainted = EditorGUILayout.ToggleLeft("Use painted texture (looks exactly like the concept)", usePainted);
        if (usePainted)
        {
            painted = (Texture2D)EditorGUILayout.ObjectField("BatTran_Painted.png", painted, typeof(Texture2D), false);
        }
        else
        {
            grassTex = (Texture2D)EditorGUILayout.ObjectField("Dry grass (tile)", grassTex, typeof(Texture2D), false);
            sandTex = (Texture2D)EditorGUILayout.ObjectField("Sand (tile)", sandTex, typeof(Texture2D), false);
            pathTex = (Texture2D)EditorGUILayout.ObjectField("Dirt path (tile)", pathTex, typeof(Texture2D), false);
            rockTex = (Texture2D)EditorGUILayout.ObjectField("Rock / rim (tile)", rockTex, typeof(Texture2D), false);
            tileSize = EditorGUILayout.FloatField("Tile size (m)", tileSize);
        }

        EditorGUILayout.Space();
        outFolder = EditorGUILayout.TextField("Output folder", outFolder);

        bool ready = layoutJson && heightBytes && splatBytes &&
                     (usePainted ? painted != null : (grassTex && sandTex && pathTex && rockTex));
        using (new EditorGUI.DisabledScope(!ready))
        {
            if (GUILayout.Button("Build Terrain + Markers", GUILayout.Height(32))) Build();
        }
        if (!ready) EditorGUILayout.HelpBox("Assign all fields above.", MessageType.Info);
    }

    void Build()
    {
        var L = JsonUtility.FromJson<Layout>(layoutJson.text);
        var t = L.terrain;
        EnsureFolder(outFolder);

        // ---------- heights (uint16 little-endian, rows from SOUTH)
        int n = t.heightRes;
        byte[] hb = heightBytes.bytes;
        if (hb.Length != n * n * 2) { Debug.LogError($"Height file size {hb.Length} != {n * n * 2}"); return; }
        var td = new TerrainData();
        td.heightmapResolution = n;                         // must be set before size
        td.size = new Vector3(t.sizeX, t.height, t.sizeZ);
        var H = new float[n, n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int i = (y * n + x) * 2;
                H[y, x] = (hb[i] | (hb[i + 1] << 8)) / 65535f;
            }
        td.SetHeights(0, 0, H);

        // ---------- layers
        TerrainLayer[] layers = usePainted
            ? new[] { MakeLayer("BatTran_Painted", painted, new Vector2(t.sizeX, t.sizeZ), TextureWrapMode.Clamp) }
            : new[]
            {
                MakeLayer("BatTran_Grass", grassTex, Vector2.one * tileSize, TextureWrapMode.Repeat),
                MakeLayer("BatTran_Sand", sandTex, Vector2.one * tileSize, TextureWrapMode.Repeat),
                MakeLayer("BatTran_Path", pathTex, Vector2.one * tileSize, TextureWrapMode.Repeat),
                MakeLayer("BatTran_Rock", rockTex, Vector2.one * tileSize, TextureWrapMode.Repeat),
            };
        td.terrainLayers = layers;

        // ---------- splat (uint8 x4: grass, sand, path, rock; rows from SOUTH)
        int a = t.alphaRes;
        td.alphamapResolution = a;
        var S = new float[a, a, layers.Length];
        if (usePainted)
        {
            for (int y = 0; y < a; y++) for (int x = 0; x < a; x++) S[y, x, 0] = 1f;
        }
        else
        {
            byte[] sb = splatBytes.bytes;
            if (sb.Length != a * a * 4) { Debug.LogError($"Splat file size {sb.Length} != {a * a * 4}"); return; }
            for (int y = 0; y < a; y++)
                for (int x = 0; x < a; x++)
                {
                    float sum = 0;
                    for (int c = 0; c < 4; c++) { S[y, x, c] = sb[(y * a + x) * 4 + c] / 255f; sum += S[y, x, c]; }
                    if (sum <= 0) { S[y, x, 0] = 1; continue; }
                    for (int c = 0; c < 4; c++) S[y, x, c] /= sum;
                }
        }
        td.SetAlphamaps(0, 0, S);

        string tdPath = AssetDatabase.GenerateUniqueAssetPath($"{outFolder}/BatTran_TerrainData.asset");
        AssetDatabase.CreateAsset(td, tdPath);

        var go = Terrain.CreateTerrainGameObject(td);
        go.name = "BatTran_Terrain";
        go.transform.position = new Vector3(t.posX, t.posY, t.posZ);
        var terrain = go.GetComponent<Terrain>();
        terrain.heightmapPixelError = 8;      // lighter for mobile
        terrain.basemapDistance = 100;
        terrain.drawInstanced = true;
        Undo.RegisterCreatedObjectUndo(go, "Build Bat Tran Terrain");

        BuildMarkers(L, terrain);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = go;
        Debug.Log($"Bat Tran terrain built: {t.sizeX} x {t.sizeZ} m, {L.markers.Length} markers, {L.lanes.Length} lanes.");
    }

    TerrainLayer MakeLayer(string name, Texture2D tex, Vector2 tile, TextureWrapMode wrap)
    {
        var ti = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;
        if (ti != null && (ti.wrapMode != wrap || ti.maxTextureSize < 2048))
        {
            ti.wrapMode = wrap;
            ti.maxTextureSize = 2048;
            ti.SaveAndReimport();
        }
        var layer = new TerrainLayer { diffuseTexture = tex, tileSize = tile, tileOffset = Vector2.zero, smoothness = 0.05f, metallic = 0f };
        AssetDatabase.CreateAsset(layer, AssetDatabase.GenerateUniqueAssetPath($"{outFolder}/{name}.terrainlayer"));
        return layer;
    }

    void BuildMarkers(Layout L, Terrain terrain)
    {
        var root = new GameObject("BatTran_Markers");
        root.AddComponent<BatTranMarkers>();
        Undo.RegisterCreatedObjectUndo(root, "Bat Tran Markers");
        float baseY = terrain.transform.position.y;

        foreach (var m in L.markers)
        {
            var g = new GameObject(m.name);
            g.transform.SetParent(root.transform, false);
            var p = new Vector3(m.x, 0, m.y);                     // logic y -> Unity Z
            p.y = terrain.SampleHeight(p) + baseY;
            g.transform.position = p;
            g.transform.rotation = Quaternion.Euler(0, m.rotY, 0);
        }

        var lanesRoot = new GameObject("Lanes");
        lanesRoot.transform.SetParent(root.transform, false);
        foreach (var lane in L.lanes)
        {
            var lg = new GameObject($"{lane.name} ({lane.length:0.0} m)");
            lg.transform.SetParent(lanesRoot.transform, false);
            for (int i = 0; i < lane.points.Length; i++)
            {
                var q = new GameObject($"P{i:00}");
                q.transform.SetParent(lg.transform, false);
                var p = new Vector3(lane.points[i].x, 0, lane.points[i].y);
                p.y = terrain.SampleHeight(p) + baseY + 0.05f;
                q.transform.position = p;
            }
        }
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
