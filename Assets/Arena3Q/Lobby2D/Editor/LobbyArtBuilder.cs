using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Arena3Q.Lobby2D.Editor
{
    public static class LobbyArtBuilder
    {
        private const string Root = "Assets/Arena3Q/Lobby2D";
        private const string Source = "ArtSource/Lobby2D";
        [Serializable] private sealed class Delivery { public Entry[] assets; }
        [Serializable] private sealed class Entry
        {
            public string id, path, role; public int width, height;
            public float[] pivot, border; public bool technicalPassed;
        }
        [Serializable] private sealed class Layout
        {
            public LobbyArtCatalog.Node[] nodes;
            public LobbyArtCatalog.TimeOfDay[] times;
            public LobbyArtCatalog.NavigationItem[] left, bottom;
        }

        [MenuItem("Arena3Q/Lobby 2D/Import Art and Build Review Prefab")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            var delivery = JsonUtility.FromJson<Delivery>(File.ReadAllText(Source + "/delivery.json"));
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(Source + "/layout.json"));
            if (delivery?.assets == null || layout?.nodes == null) throw new InvalidDataException("Missing art delivery/layout.");
            foreach (var entry in delivery.assets)
            {
                var importer = AssetImporter.GetAtPath(entry.path) as TextureImporter;
                if (importer == null) throw new InvalidDataException("Missing PNG: " + entry.path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048; importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = entry.role.StartsWith("environment") ? SpriteMeshType.Tight : SpriteMeshType.FullRect; settings.spriteGenerateFallbackPhysicsShape = false;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(entry.pivot[0], entry.pivot[1]);
                settings.spriteBorder = new Vector4(entry.border[0], entry.border[1], entry.border[2], entry.border[3]);
                importer.SetTextureSettings(settings);
                foreach (var platform in new[] { "Android", "iPhone" })
                    importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                        name = platform, overridden = true, maxTextureSize = 2048,
                        format = TextureImporterFormat.ASTC_4x4, compressionQuality = 100
                    });
                importer.SaveAndReimport();
            }
            var catalogPath = Root + "/Unity/LobbyArtCatalog.asset";
            Directory.CreateDirectory(Root + "/Unity/Prefabs");
            Directory.CreateDirectory(Root + "/Unity/SpriteAtlases");
            Directory.CreateDirectory(Root+"/UI/Fonts");
            AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<LobbyArtCatalog>(catalogPath);
            if (!catalog) { catalog = ScriptableObject.CreateInstance<LobbyArtCatalog>(); AssetDatabase.CreateAsset(catalog, catalogPath); }
            var used=new System.Collections.Generic.HashSet<string>(layout.nodes.Where(n=>!string.IsNullOrEmpty(n.spriteId)).Select(n=>n.spriteId));
            catalog.entries = delivery.assets.Where(e=>used.Contains(e.id)||e.role=="button"||e.role=="vip"||e.role=="sky"||e.role=="banner").Select(e => new LobbyArtCatalog.Entry {
                id = e.id, role = e.role, sprite = AssetDatabase.LoadAssetAtPath<Sprite>(e.path)
            }).ToArray();
            catalog.nodes = layout.nodes; catalog.times = layout.times; catalog.left = layout.left; catalog.bottom = layout.bottom;
            var fontPath=Root+"/UI/Fonts/BeVietnamPro-Lobby-Bold SDF.asset";
            if(!File.Exists(fontPath))AssetDatabase.CopyAsset("Assets/Arena3Q/Art/UI/Startup/Fonts/BeVietnamPro-Bold SDF.asset",fontPath);
            catalog.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            catalog.characterRig=AssetDatabase.LoadAssetAtPath<LuBuCutoutDefinition>(Root+"/Unity/LuBuCutoutRig.asset");
            EditorUtility.SetDirty(catalog);
            var atlasPath = Root + "/Unity/SpriteAtlases/LobbyUI.spriteatlas";
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (!atlas) { atlas = new SpriteAtlas(); AssetDatabase.CreateAsset(atlas, atlasPath); }
            atlas.Remove(atlas.GetPackables());
            atlas.Add(catalog.entries.Where(e => new[]{"icon","vip","button","frame","banner","effect"}.Contains(e.role))
                .Select(e => (UnityEngine.Object)e.sprite).ToArray());
            atlas.SetPackingSettings(new SpriteAtlasPackingSettings { enableRotation = false, enableTightPacking = false, padding = 4 });
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings { readable = false, generateMipMaps = false, sRGB = true, filterMode = FilterMode.Bilinear });
            atlas.SetPlatformSettings(new TextureImporterPlatformSettings { name = "DefaultTexturePlatform", maxTextureSize = 2048, format = TextureImporterFormat.RGBA32 });
            // Review-only content must not silently increase the current release's download size.
            atlas.SetIncludeInBuild(false);
            EditorUtility.SetDirty(atlas);
            var root = new GameObject("Arena3Q_Lobby2D_ArtReview", typeof(RectTransform));
            root.AddComponent<LobbyArtPreview>().catalog = catalog;
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/Unity/Prefabs/LobbyArtReview.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Verify(catalog, delivery);
            Debug.Log("LOBBY2D_IMPORT_OK sprites=" + catalog.entries.Length + "; isolated art review prefab.");
        }

        [MenuItem("Arena3Q/Lobby 2D/Build and Capture Art Review")]
        public static void BuildAndCapture()
        {
            Build();
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("ArtReviewCamera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Unity/Prefabs/LobbyArtReview.prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var view = instance.GetComponent<LobbyArtPreview>(); view.previewCamera = camera;
            EditorSceneManager.SaveScene(scene, Root + "/Unity/LobbyArtReview.unity");
            view.Build(); Canvas.ForceUpdateCanvases();
            foreach (var light in view.GetComponentsInChildren<LobbyArtLight>()) light.Sample(0, 1);
            var output = Path.GetFullPath("NghiemThu/UI/XemTruoc_Lobby2D_20261026"); Directory.CreateDirectory(output);
            for (var i = 0; i < view.catalog.times.Length; i++)
            {
                view.SetTime(i); Canvas.ForceUpdateCanvases();
                Capture(camera, view, 1920, 1080, new Rect(0, 0, 1920, 1080), Path.Combine(output, "Lobby_" + i + ".png"));
            }
            var widths = new[] { 2160, 2340, 2400 };
            foreach (var width in widths)
                Capture(camera, view, width, 1080, new Rect(72, 16, width - 96, 1064), Path.Combine(output, "Lobby_safe_" + width + ".png"));
            File.WriteAllText(Path.Combine(output, "unity-review.json"), JsonUtility.ToJson(new CaptureReport {
                spriteCount = view.catalog.entries.Length, textCount = instance.GetComponentsInChildren<TextMeshProUGUI>().Length,
                timeCount = view.catalog.times.Length, animationApproved = false, devicePerformanceMeasured = false
            }, true));
            Debug.Log("LOBBY2D_CAPTURE_OK " + output);
            File.Copy("Assets/Arena3Q/Art/UI/Startup/Fonts/OFL.txt", Root+"/UI/Fonts/OFL.txt", true);
            File.Copy(Source+"/Native/Lucide_LICENSE.txt", Root+"/UI/Lucide_LICENSE.txt", true);
            AssetDatabase.Refresh();
            Directory.CreateDirectory("Builds/Art");
            // Asset archive only. Does not invoke BuildPipeline or change player/build settings.
            AssetDatabase.ExportPackage(Root, "Builds/Art/Arena3Q_Lobby2D.unitypackage",
                ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
            Debug.Log("LOBBY2D_ASSET_PACKAGE_OK Builds/Art/Arena3Q_Lobby2D.unitypackage");
        }
        [Serializable] private sealed class CaptureReport
        {
            public int spriteCount, textCount, timeCount;
            public bool animationApproved, devicePerformanceMeasured;
        }
        private static void Capture(Camera camera, LobbyArtPreview view, int width, int height, Rect safeArea, string path)
        {
            var previous = RenderTexture.active;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try {
                camera.targetTexture = target; camera.aspect = (float)width / height;
                view.SafeAreaOverride = safeArea;
                var scaler = view.GetComponent<CanvasScaler>(); scaler.enabled = false; scaler.enabled = true;
                Canvas.ForceUpdateCanvases(); view.ApplySafeArea(); view.AlignBackgrounds(); Canvas.ForceUpdateCanvases();
                camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            } finally {
                camera.targetTexture = null; RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image);
            }
        }
        private static void Verify(LobbyArtCatalog catalog, Delivery delivery)
        {
            if (!catalog.font) throw new InvalidDataException("Missing Vietnamese TMP font.");
            foreach (var entry in delivery.assets)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entry.path);
                if (!sprite || sprite.rect.width != entry.width || sprite.rect.height != entry.height)
                    throw new InvalidDataException("Sprite dimensions/identity mismatch: " + entry.id);
            }
            foreach (var node in catalog.nodes.Where(n => n.kind == LobbyArtCatalog.NodeKind.Image))
                if (!catalog.Sprite(node.spriteId.Replace("{vip}", catalog.vip.ToString("D2"))))
                    throw new InvalidDataException("Layout references absent sprite: " + node.id);
            if (catalog.nodes.Count(n => n.action == LobbyArtCatalog.NodeAction.Navigate && n.destination == "Chiêu Mộ") != 1 ||
                catalog.nodes.Count(n => n.action == LobbyArtCatalog.NodeAction.Navigate && n.destination == "Thư") != 1)
                throw new InvalidDataException("Duplicate recruitment/mail.");
            if (catalog.entries.Count(e => e.role == "vip") != 12) throw new InvalidDataException("Expected twelve separate VIP badges.");
        }
    }
}
