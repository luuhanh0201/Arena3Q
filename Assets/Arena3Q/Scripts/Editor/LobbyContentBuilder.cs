using System;
using System.Collections.Generic;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Frontend;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Arena3Q.Editor
{
    public static class LobbyContentBuilder
    {
        public const string Path = "Assets/Arena3Q/Content/Frontend/LobbyConfig.asset";
        [Serializable] private sealed class VipMetric { public int tier; public float bodyAnchorY, bodyHeightY; }
        [Serializable] private sealed class VipMetrics { public VipMetric[] frames; }
        [MenuItem("Arena3Q/UI/Apply Integrated Lobby")]
        public static void Apply()
        {
            var frontend = CurrentGameRelease.Settings.frontend;
            ApplyTo(frontend); AssetDatabase.SaveAssets();
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/Arena3Q/Lobby2D/Unity/SpriteAtlases/LobbyUI.spriteatlas");
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            CapturePortraits(CurrentGameRelease.Settings.frontend);
            CaptureSurfaces(CurrentGameRelease.Settings.frontend); AssetDatabase.SaveAssets();
        }
        public static void ApplyForReview()
        {
            Apply();
            LobbyModelSurfaceProbe.Run();
            LobbyCompressionQa.ApplyAndCapture();
            CurrentGameRelease.Apply();
        }
        private static void CaptureSurfaces(FrontendConfig frontend)
        {
            const string output = "NghiemThu/UI/XemTruoc_Lobby2D_SuaTheoMau_20261027";
            System.IO.Directory.CreateDirectory(output);
            foreach (var source in frontend.Lobby.models)
            {
                var materials = new List<string>();
                foreach (var variant in new[] { "LordSurface", "BattleSurface" })
                {
                    var root = new GameObject("SurfaceCapture", typeof(RectTransform), typeof(RawImage), typeof(LobbyLordView));
                    var view = root.GetComponent<LobbyLordView>();
                    var previous = RenderTexture.active;
                    try
                    {
                        BattlePresentation(source.hero, out var battlePrefab, out var battleIdle);
                        var prefab = variant == "BattleSurface" ? battlePrefab : source.hero.PortraitModelPrefab;
                        var idle = variant == "BattleSurface" ? battleIdle : source.hero.PortraitIdle;
                        if (prefab == null || idle == null) continue;
                        view.Initialize(root.GetComponent<RawImage>(), frontend.Lobby, source, prefab, idle);
                        using var isolated = new LobbyCaptureStage(view);
                        view.Sample(idle.length * .25f);
                        foreach (var renderer in view.Model.GetComponentsInChildren<Renderer>())
                            foreach (var material in renderer.sharedMaterials)
                                materials.Add(variant + " " + renderer.name + " " + AssetDatabase.GetAssetPath(material) + " shader=" + material.shader.name);
                        view.CaptureCamera.orthographicSize = .73f;
                        view.CaptureCamera.transform.localPosition = new Vector3(0, .99f, -4);
                        view.CaptureCamera.Render(); RenderTexture.active = view.Texture;
                        var image = new Texture2D(view.Texture.width, view.Texture.height, TextureFormat.RGBA32, false);
                        image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0); image.Apply();
                        System.IO.File.WriteAllBytes(output + "/" + variant + ".png", image.EncodeToPNG());
                        if (variant == "LordSurface")
                        {
                            var pixels = image.GetPixels32(); var alpha = new Color32[pixels.Length];
                            var opaque = 0; var partial = 0; var clear = 0;
                            for (var i = 0; i < pixels.Length; i++)
                            {
                                var value = pixels[i].a; alpha[i] = new Color32(value, value, value, 255);
                                if (value == 255) opaque++; else if (value == 0) clear++; else partial++;
                            }
                            image.SetPixels32(alpha); image.Apply();
                            System.IO.File.WriteAllBytes(output + "/LordSurfaceAlpha.png", image.EncodeToPNG());
                            System.IO.File.WriteAllText(output + "/SurfaceAlpha.json", "{\"opaquePixels\":" + opaque +
                                ",\"transparentPixels\":" + clear + ",\"partialAlphaPixels\":" + partial + "}");
                        }
                        UnityEngine.Object.DestroyImmediate(image);
                    }
                    finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(root); }
                }
                System.IO.File.WriteAllLines(output + "/ModelMaterialSources.txt", materials);
                var portrait = frontend.LordRoster.Find(source.hero.HeroId)?.portrait;
                if (portrait != null) System.IO.File.Copy(AssetDatabase.GetAssetPath(portrait), output + "/LordPortrait.png", true);
            }
        }
        private static void CapturePortraits(FrontendConfig frontend)
        {
            var heroIds = frontend.Lobby.models.Where(source => source.hero != null).Select(source => source.hero.HeroId).ToArray();
            foreach (var heroId in heroIds)
            {
                frontend = CurrentGameRelease.Settings.frontend;
                var entry = frontend.LordRoster.Find(heroId);
                var config = UnityEngine.Object.Instantiate(frontend.Lobby);
                config.renderResolution = config.avatarRenderResolution;
                var source = config.Model(entry?.lord);
                if (entry == null || source.hero.PortraitModelPrefab == null || source.hero.PortraitIdle == null)
                { UnityEngine.Object.DestroyImmediate(config); continue; }
                var path = "Assets/Arena3Q/Art/UI/Icons/LordPortrait_" + source.hero.HeroId + ".png";
                var root = new GameObject("PortraitCapture", typeof(RectTransform), typeof(RawImage), typeof(LobbyLordView));
                var view = root.GetComponent<LobbyLordView>();
                var previous = RenderTexture.active;
                try
                {
                    view.Initialize(root.GetComponent<RawImage>(), config, source);
                    using var isolated = new LobbyCaptureStage(view);
                    view.Sample(source.hero.PortraitIdle.length * .25f);
                    view.CaptureCamera.orthographicSize = .43f;
                    view.CaptureCamera.transform.localPosition = new Vector3(0, 1.12f, -4);
                    view.CaptureCamera.Render(); RenderTexture.active = view.Texture;
                    var image = new Texture2D(view.Texture.width, view.Texture.height, TextureFormat.RGBA32, false);
                    image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0); image.Apply();
                    System.IO.File.WriteAllBytes(path, image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
                }
                finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(config); }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                var roster = CurrentGameRelease.Settings.frontend.LordRoster;
                roster.Find(heroId).portrait = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                EditorUtility.SetDirty(roster); AssetDatabase.SaveAssets();
            }
        }
        public static void ApplyTo(FrontendConfig frontend)
        {
            // Imports may unload native asset instances. Complete imports before loading mutable release data.
            var frontendPath = AssetDatabase.GetAssetPath(frontend);
            PrepareArtImports();
            frontend = AssetDatabase.LoadAssetAtPath<FrontendConfig>(frontendPath);
            var config = AssetDatabase.LoadAssetAtPath<LobbyConfig>(Path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<LobbyConfig>(); AssetDatabase.CreateAsset(config, Path);
                Populate(config, frontend);
                config.heroFeet = new Vector2(.39f, .20f);
            }
            EnsureHeroPresentations(frontend);
            if (config.layout == null)
            {
                const string layoutPath = "Assets/Arena3Q/Content/Frontend/LobbyLayout.asset";
                config.layout = AssetDatabase.LoadAssetAtPath<LobbyLayoutConfig>(layoutPath);
                if (config.layout == null)
                {
                    config.layout = ScriptableObject.CreateInstance<LobbyLayoutConfig>();
                    AssetDatabase.CreateAsset(config.layout, layoutPath);
                }
            }
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/Arena3Q/Lobby2D/Unity/SpriteAtlases/LobbyUI.spriteatlas");
            if (atlas == null) throw new InvalidOperationException("Integrated lobby requires its UI atlas.");
            EditorUtility.SetDirty(config.layout);
            atlas.SetIncludeInBuild(true); EditorUtility.SetDirty(atlas);
            var legacyAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/Arena3Q/Lobby2D/Unity/SpriteAtlases/LegacyLobbyReview.spriteatlas");
            if (legacyAtlas != null) { legacyAtlas.SetIncludeInBuild(false); EditorUtility.SetDirty(legacyAtlas); }
            const string recruitPath = "Assets/Arena3Q/Art/UI/LobbyRecruitDay.png";
            var recruitImporter = AssetImporter.GetAtPath(recruitPath) as TextureImporter;
            if (recruitImporter != null)
            {
                var recruit = config.art.FirstOrDefault(entry => entry.id == "Banner_Recruit");
                if (recruit != null) recruit.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(recruitPath);
            }
            if (!config.uiRefinementsInitialized)
            {
                config.heroFeet.y = .20f;
                foreach (var resource in config.resources)
                {
                    resource.isStamina = resource.id == "Stamina";
                    resource.showAdd = true;
                }
                config.vipFrames = Enumerable.Range(1, 12).Select(tier => new LobbyConfig.VipFrame
                    { tier = tier, sprite = config.Sprite("VIP_" + tier.ToString("D2")) }).ToArray();
                config.defaultPlayerFrame = config.Sprite("Player_Name");
                config.uiRefinementsInitialized = true;
            }
            config.mail = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arena3Q/Art/UI/Icons/HeroEnvelope.png");
            config.settings = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arena3Q/Art/UI/Icons/HeroCog.png");
            if (config.vipFrames.Any(frame => frame.bodyAnchorY <= 0 || frame.bodyHeightY <= 0))
            {
                var metrics = JsonUtility.FromJson<VipMetrics>(System.IO.File.ReadAllText("ArtSource/Lobby2D/vip-layout-metrics.json"));
                foreach (var frame in config.vipFrames)
                {
                    var row = metrics.frames.First(metric => metric.tier == frame.tier);
                    if (frame.bodyAnchorY <= 0) frame.bodyAnchorY = row.bodyAnchorY;
                    if (frame.bodyHeightY <= 0) frame.bodyHeightY = row.bodyHeightY;
                }
            }
            if (!config.lightingConfigured)
            {
                var battleView = AssetDatabase.LoadAssetAtPath<BattleViewConfig>("Assets/Arena3Q/Content/Resources/BattleView.asset");
                config.directionalIntensity = battleView.LightIntensity; config.ambientColor = battleView.AmbientColour;
                config.lightColor = battleView.LightColour; config.fillIntensity = 0;
                config.lightRotation = (Quaternion.Inverse(Quaternion.Euler(-90, 0, 0)) * Quaternion.Euler(battleView.LightEulerAngles)).eulerAngles;
                config.lightingConfigured = true;
            }
            if (config.reflectionEnvironment == null)
            {
                const int size = 32;
                var cube = new Cubemap(size, TextureFormat.RGBAHalf, true) { name = "LobbyWarmEnvironment" };
                for (var face = 0; face < 6; face++)
                {
                    var pixels = new Color[size * size];
                    for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
                    {
                        var u = (x + .5f) / size * 2 - 1; var v = (y + .5f) / size * 2 - 1;
                        var direction = face == 0 ? new Vector3(1, v, -u) : face == 1 ? new Vector3(-1, v, u) :
                            face == 2 ? new Vector3(u, 1, -v) : face == 3 ? new Vector3(u, -1, v) :
                            face == 4 ? new Vector3(u, v, 1) : new Vector3(-u, v, -1);
                        direction.Normalize();
                        pixels[y * size + x] = Color.Lerp(config.reflectionGround, config.reflectionSky,
                            Mathf.SmoothStep(0, 1, direction.y * .5f + .5f)).linear;
                    }
                    cube.SetPixels(pixels, (CubemapFace)face);
                }
                cube.Apply(true, false);
                AssetDatabase.CreateAsset(cube, "Assets/Arena3Q/Content/Frontend/LobbyWarmEnvironment.asset");
                config.reflectionEnvironment = cube;
            }
            if (config.captureComposite == null)
            {
                var composite = new Material(Shader.Find("Arena3Q/UI/Premultiplied Capture")) { name = "LobbyCapture" };
                AssetDatabase.CreateAsset(composite, "Assets/Arena3Q/Content/Frontend/LobbyCapture.mat");
                config.captureComposite = composite;
            }
            if (config.lordHeadingMaterial == null)
            {
                var material = new Material(frontend.StartupUi.extraBold.material) { name = "LobbyLordHeading" };
                material.SetColor("_OutlineColor", frontend.StartupUi.ink); material.SetFloat("_OutlineWidth", .13f);
                material.EnableKeyword("UNDERLAY_ON"); material.SetColor("_UnderlayColor", new Color(.08f, .025f, 0, .65f));
                material.SetFloat("_UnderlayOffsetY", -.35f); material.SetFloat("_UnderlaySoftness", .35f);
                AssetDatabase.CreateAsset(material, "Assets/Arena3Q/Content/Frontend/LobbyLordHeading.mat");
                config.lordHeadingMaterial = material;
            }
            EditorUtility.SetDirty(config);
            EditorUtility.SetDirty(frontend.StartupUi);
            if (frontend.Progression != null) EditorUtility.SetDirty(frontend.Progression);
            var eventBanner = config.banners.FirstOrDefault(entry => entry.id == "Event");
            if (eventBanner != null) eventBanner.title = "Đại Chiến\nQuần Hùng";
            var serialized = new SerializedObject(frontend);
            serialized.FindProperty("lobby").objectReferenceValue = config;
            var order = frontend.Steps.Where(x => x != FrontendStep.Lobby && x != FrontendStep.TutorialBattle).ToList();
            var index = order.IndexOf(FrontendStep.Battle);
            if (index < 0) throw new InvalidOperationException("Frontend requires a battle step.");
            order.Insert(index, FrontendStep.TutorialBattle); order.Insert(index + 1, FrontendStep.Lobby);
            var steps = serialized.FindProperty("steps"); steps.arraySize = order.Count;
            for (var i = 0; i < order.Count; i++) steps.GetArrayElementAtIndex(i).intValue = (int)order[i];
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(frontend);
        }
        private static void Populate(LobbyConfig config, FrontendConfig frontend)
        {
            var catalog = AssetDatabase.LoadMainAssetAtPath("Assets/Arena3Q/Lobby2D/Unity/LobbyArtCatalog.asset");
            if (catalog == null) throw new InvalidOperationException("Missing lobby art catalog.");
            var source = new SerializedObject(catalog);
            var entries = source.FindProperty("entries"); var art = new List<LobbyConfig.ArtEntry>();
            for (var i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var id = entry.FindPropertyRelative("id").stringValue;
                if (!id.StartsWith("LuBu", StringComparison.Ordinal)) art.Add(new LobbyConfig.ArtEntry
                    { id = id, sprite = (Sprite)entry.FindPropertyRelative("sprite").objectReferenceValue });
            }
            config.art = art.ToArray();
            config.left = Menu(source.FindProperty("left"), new[] { true, false, true, false, true });
            config.bottom = Menu(source.FindProperty("bottom"), new[] { false, true, false, false, true, true });
            var hours = new[] { 5, 11, 15, 18, 21 };
            var times = source.FindProperty("times"); config.daylight = new LobbyConfig.Daylight[times.arraySize];
            for (var i = 0; i < times.arraySize; i++)
            {
                var time = times.GetArrayElementAtIndex(i);
                config.daylight[i] = new LobbyConfig.Daylight { startHour = hours[i],
                    label = time.FindPropertyRelative("label").stringValue, sky = time.FindPropertyRelative("skyId").stringValue,
                    tint = time.FindPropertyRelative("environmentTint").colorValue, lightingAlpha = time.FindPropertyRelative("lightingAlpha").floatValue };
            }
            config.resources = new[] {
                new LobbyConfig.ResourceEntry { id = "Banknotes", label = "Ngân Phiếu", icon = "Icon_Banknotes" },
                new LobbyConfig.ResourceEntry { id = "Silver", label = "Bạc", icon = "Icon_Silver" },
                new LobbyConfig.ResourceEntry { id = "Stamina", label = "Thể Lực", icon = "Icon_Stamina", isStamina = true } };
            config.banners = new[] {
                new LobbyConfig.BannerEntry { id = "Event", tag = "Sự Kiện", title = "Đại Chiến Quần Hùng", subtitle = "Tham gia ngay", art = "Banner_Event" },
                new LobbyConfig.BannerEntry { id = "Recruit", title = "Chiêu Mộ", subtitle = "Tụ Anh Hùng Thống Tam Quốc", art = "Banner_Recruit" },
                new LobbyConfig.BannerEntry { id = "Arena", title = "Đấu Trường", subtitle = "So tài chiến thuật", art = "Banner_Arena" },
                new LobbyConfig.BannerEntry { id = "Guild", title = "Bang Hội", subtitle = "Cùng huynh đệ xưng bá Tam Quốc", art = "Banner_Guild" } };
            Sprite Icon(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arena3Q/Art/UI/Icons/" + name + ".png");
            config.plus = Icon("Plus"); config.chevron = Icon("ChevronRight"); config.doubleChevron = Icon("ChevronsRight");
            config.star = Icon("Star"); config.friends = Icon("Users");
            config.mail = config.Sprite("Icon_Mail"); config.settings = config.Sprite("Icon_Settings");
            EnsureHeroPresentations(frontend);
            config.models = frontend.LordRoster.Lords.Where(lord => lord.lord.PortraitModelPrefab != null)
                .Select(lord => new LobbyConfig.LordModel { hero = lord.lord }).ToArray();
            EditorUtility.SetDirty(config);
        }
        private static void PrepareArtImports()
        {
            foreach (var path in new[] { "Assets/Arena3Q/Art/UI/LobbyRecruitDay.png" }.Concat(
                new[] { "Plus", "ChevronRight", "ChevronsRight", "Users", "HeroEnvelope", "HeroCog" }
                    .Select(name => "Assets/Arena3Q/Art/UI/Icons/" + name + ".png")))
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single &&
                    !importer.mipmapEnabled && importer.alphaIsTransparency) continue;
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.SaveAndReimport();
            }
        }
        // Existing hero presentation is authoritative. Seed only missing portrait references from battle data.
        private static void EnsureHeroPresentations(FrontendConfig frontend)
        {
            foreach (var lord in frontend.LordRoster.Lords)
            {
                var hero = lord.lord;
                if (hero == null || (hero.PortraitModelPrefab != null && hero.PortraitIdle != null)) continue;
                BattlePresentation(hero, out var prefab, out var idle);
                if (prefab == null || idle == null) continue;
                var portrait = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(prefab)
                    .Replace("_Battle3D.prefab", "_Portrait3D.prefab"));
                var data = new SerializedObject(hero);
                if (hero.PortraitModelPrefab == null) data.FindProperty("portraitModelPrefab").objectReferenceValue = portrait != null ? portrait : prefab;
                if (hero.PortraitIdle == null) data.FindProperty("portraitIdle").objectReferenceValue = idle;
                data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(hero);
            }
        }
        private static void BattlePresentation(HeroDefinition hero, out GameObject prefab, out AnimationClip idle)
        {
            prefab = hero.Model3D != null ? hero.Model3D.Prefab : null;
            idle = hero.Model3D != null ? hero.Model3D.Find(HeroAnimationState.Idle) : null;
            if (prefab != null && idle != null) return;
            var view = hero.MacAnhPerformance != null ? hero.MacAnhPerformance.Battle3DViewPrefab : null;
            if (view == null) return;
            foreach (var component in view.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) continue;
                var data = new SerializedObject(component); var character = data.FindProperty("characterPrefab");
                var clips = data.FindProperty("clips"); if (character == null || clips == null) continue;
                for (var i = 0; i < clips.arraySize; i++)
                {
                    var clip = clips.GetArrayElementAtIndex(i);
                    if (clip.FindPropertyRelative("action").intValue != (int)MacAnhAction.Idle) continue;
                    prefab = character.objectReferenceValue as GameObject;
                    idle = clip.FindPropertyRelative("clip").objectReferenceValue as AnimationClip;
                    return;
                }
            }
        }
        private static LobbyConfig.MenuEntry[] Menu(SerializedProperty source, bool[] notifications)
        {
            var result = new LobbyConfig.MenuEntry[source.arraySize];
            for (var i = 0; i < result.Length; i++)
            {
                var entry = source.GetArrayElementAtIndex(i); var icon = entry.FindPropertyRelative("iconId").stringValue;
                result[i] = new LobbyConfig.MenuEntry { id = icon, icon = icon,
                    label = entry.FindPropertyRelative("label").stringValue, notification = notifications[i] };
            }
            return result;
        }
    }
}
