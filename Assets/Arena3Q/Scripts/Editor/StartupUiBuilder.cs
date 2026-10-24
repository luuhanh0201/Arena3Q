using System;
using System.IO;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Arena3Q.Editor
{
    /// <summary>Repeatable, scoped importer. Existing UI values/copy are preserved unless explicitly refreshed.</summary>
    public static class StartupUiBuilder
    {
        public const string ArtRoot = "Assets/Arena3Q/Art/UI/Startup";
        public const string ConfigPath = "Assets/Arena3Q/Content/Frontend/StartupUi.asset";
        private const string Source = "docs/UI/Arena3Q_UI_Handoff/Assets";

        [InitializeOnLoadMethod]
        private static void RegisterFontCleanup()
        {
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }
        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) CompactFonts();
        }
        [MenuItem("Arena3Q/UI/Compact Startup Font Atlases")]
        public static void CompactFonts()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { ArtRoot + "/Fonts" }))
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
                font.ClearFontAssetData(true); EditorUtility.SetDirty(font);
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Arena3Q/UI/Import Startup Handoff")]
        public static void Import()
        {
            if (!Directory.Exists(Source)) throw new DirectoryNotFoundException(Source);
            CopyMissing(Source, ArtRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var path in Directory.GetFiles(ArtRoot, "*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension == ".png" || extension == ".jpg") ConfigureSprite(path.Replace('\\', '/'));
            }
            EnsureTmpSettings();
            var config = AssetDatabase.LoadAssetAtPath<StartupUiConfig>(ConfigPath);
            if (config == null) { config = ScriptableObject.CreateInstance<StartupUiConfig>(); AssetDatabase.CreateAsset(config, ConfigPath); }
            var so = new SerializedObject(config);
            void Bind(string field, string relative)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/" + relative);
                if (sprite == null) throw new InvalidOperationException("Missing UI sprite: " + relative + ". Run docs/UI/Tools/prepare-assets.cjs.");
                so.FindProperty(field).objectReferenceValue = sprite;
            }
            Bind("background", "UI/Backgrounds/keyart_3heroes.png"); Bind("blurredBackground", "Generated/keyart_blur.jpg");
            Bind("logo", "UI/Logo/logo_arena3q.png"); Bind("splashStill", "UI/Backgrounds/splash_lastframe.jpg");
            Bind("goldButton", "UI/Sprites/Buttons/btn_gold.png"); Bind("blueButton", "UI/Sprites/Buttons/btn_blue.png"); Bind("redButton", "UI/Sprites/Buttons/btn_red.png");
            Bind("panel", "UI/Sprites/Frames/frame_panel.png"); Bind("largePanel", "UI/Sprites/Frames/frame_panel_big.png");
            Bind("crest", "UI/Sprites/Ornaments/crest_dragon.png"); Bind("tassel", "UI/Sprites/Ornaments/tassel.png"); Bind("cloud", "UI/Sprites/Ornaments/cloud_swirl.png");
            Bind("backIcon", "UI/Sprites/Icons/icon_back.png"); Bind("soundIcon", "UI/Sprites/Icons/icon_sound.png");
            Bind("settingsIcon", "UI/Sprites/Icons/icon_settings.png"); Bind("language", "UI/Sprites/Icons/lang_selector_vi.png");
            foreach (var name in new[] { "cart", "wheel", "flag", "spear", "gradient", "vignette", "rounded" }) Bind(name, "Generated/" + name + ".png");
            var icons = new[] { ("wifi", "wifi"), ("wifiLow", "wifi-low"), ("wifiOff", "wifi-off"), ("serverOff", "server-off"),
                ("warning", "triangle-alert"), ("download", "arrow-down"), ("check", "check"), ("user", "user-round"), ("gift", "gift"), ("slash", "slash") };
            foreach (var icon in icons) Bind(icon.Item1, "Lucide/" + icon.Item2 + ".png");
            var weights = new[] { ("regular", "Regular"), ("semiBold", "SemiBold"), ("bold", "Bold"), ("extraBold", "ExtraBold") };
            foreach (var weight in weights) so.FindProperty(weight.Item1).objectReferenceValue = Font(weight.Item2);
            var audio = so.FindProperty("sounds"); audio.arraySize = 6;
            var sounds = new[] { "tap", "tab", "confirm", "enter", "back", "error" };
            for (var i = 0; i < sounds.Length; i++) audio.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ArtRoot + "/Audio/SFX/ui_" + sounds[i] + ".wav");
            var musicPath = ArtRoot + "/Video/OGG - in-game loop file.ogg";
            var musicImporter = AssetImporter.GetAtPath(musicPath) as AudioImporter;
            if (musicImporter != null)
            {
                var sample = musicImporter.defaultSampleSettings; sample.loadType = AudioClipLoadType.Streaming;
                sample.compressionFormat = AudioCompressionFormat.Vorbis; sample.quality = .7f;
                musicImporter.defaultSampleSettings = sample; musicImporter.SaveAndReimport();
            }
            so.FindProperty("music").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath);
            if (so.FindProperty("copy").arraySize == 0) FillCopy(so);
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(config);
            // Migrate only the first local draft's panel height, without resetting edited layouts.
            if (Mathf.Approximately(config.loginPanel.height, 566)) config.loginPanel.height = 512;
            var tmpSettings = new SerializedObject(Resources.Load<TMP_Settings>("TMP Settings"));
            tmpSettings.FindProperty("m_defaultFontAsset").objectReferenceValue = config.regular;
            tmpSettings.FindProperty("m_defaultSpriteAsset").objectReferenceValue = null;
            tmpSettings.FindProperty("m_enableEmojiSupport").boolValue = false;
            tmpSettings.ApplyModifiedPropertiesWithoutUndo();

            var frontend = AssetDatabase.LoadAssetAtPath<FrontendConfig>("Assets/Arena3Q/Content/Frontend/FrontendConfig.asset");
            var flow = new SerializedObject(frontend);
            flow.FindProperty("startupUi").objectReferenceValue = config;
            flow.FindProperty("banner").objectReferenceValue = config.background;
            flow.FindProperty("logo").objectReferenceValue = config.logo;
            flow.FindProperty("theme").FindPropertyRelative("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(ArtRoot + "/Fonts/BeVietnamPro-Regular.ttf");
            flow.FindProperty("introVideo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(ArtRoot + "/Video/Arena3Q_Splash_fire_sound.mp4");
            var steps = flow.FindProperty("steps");
            if (!Enumerable.Range(0, steps.arraySize).Any(i => steps.GetArrayElementAtIndex(i).intValue == (int)FrontendStep.Loading))
            {
                steps.InsertArrayElementAtIndex(1); steps.GetArrayElementAtIndex(1).intValue = (int)FrontendStep.Loading;
            }
            flow.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(frontend);
            ExpandSampleServers();
            AssetDatabase.SaveAssets();
            Debug.Log("[Arena3Q UI] Handoff imported, fonts and 9-slice borders bound; Frontend scene uses StartupUi.");
        }

        private static void CopyMissing(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.GetFiles(source))
            {
                var target = Path.Combine(destination, Path.GetFileName(file));
                if (!File.Exists(target)) File.Copy(file, target);
            }
            foreach (var directory in Directory.GetDirectories(source)) CopyMissing(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
        private static void ConfigureSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            var border = Vector4.zero;
            switch (Path.GetFileName(path))
            {
                case "btn_gold.png": border = new Vector4(72, 37, 72, 38); break;
                case "btn_blue.png": border = new Vector4(44, 24, 44, 24); break;
                case "btn_red.png": border = new Vector4(44, 25, 44, 26); break;
                case "frame_panel.png": border = Vector4.one * 44; break;
                case "frame_panel_big.png": border = Vector4.one * 56; break;
                case "rounded.png": border = Vector4.one * 14; break;
            }
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteBorder = border;
            var changed = importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single
                || importer.spriteBorder != border || importer.mipmapEnabled || importer.maxTextureSize != 2048;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.sRGBTexture = true;
            importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ; importer.maxTextureSize = 2048;
            importer.SetTextureSettings(settings); importer.spriteBorder = border;
            // TextureImporterSettings includes textureType: set it after applying the old settings.
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.mipmapEnabled = false;
            if (changed) importer.SaveAndReimport();
        }

        private static TMP_FontAsset Font(string weight)
        {
            var path = ArtRoot + "/Fonts/BeVietnamPro-" + weight + " SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;
            var source = AssetDatabase.LoadAssetAtPath<Font>(ArtRoot + "/Fonts/BeVietnamPro-" + weight + ".ttf");
            if (source == null) throw new InvalidOperationException("Download Be Vietnam Pro " + weight + " from Google Fonts first.");
            var font = TMP_FontAsset.CreateFontAsset(source, 40, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            font.name = "Be Vietnam Pro " + weight + " SDF";
            AssetDatabase.CreateAsset(font, path);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var texture in font.atlasTextures) { texture.name = font.name + " Atlas"; AssetDatabase.AddObjectToAsset(texture, font); }
            // Keep generated atlases empty on disk; runtime SDF populates from the included OFL source font.
            // This avoids megabytes of duplicate atlas data while preserving every Vietnamese glyph.
            EditorUtility.SetDirty(font); return font;
        }

        private static void EnsureTmpSettings()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return;
            throw new InvalidOperationException("Run node docs/UI/Tools/prepare-tmp.cjs before the batch importer. ImportPackage is asynchronous.");
        }

        [MenuItem("Arena3Q/UI/Refresh Startup Copy From TSV")]
        public static void RefreshCopy()
        {
            var asset = AssetDatabase.LoadAssetAtPath<StartupUiConfig>(ConfigPath);
            var so = new SerializedObject(asset); FillCopy(so); so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();
        }
        private static void FillCopy(SerializedObject so)
        {
            var lines = File.ReadAllLines("docs/UI/StartupCopy.tsv").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            var copy = so.FindProperty("copy"); copy.arraySize = lines.Length;
            for (var i = 0; i < lines.Length; i++)
            {
                var parts = lines[i].Split(new[] { '\t' }, 2);
                if (parts.Length != 2) throw new InvalidDataException("Invalid startup copy line: " + lines[i]);
                var entry = copy.GetArrayElementAtIndex(i); entry.FindPropertyRelative("key").stringValue = parts[0]; entry.FindPropertyRelative("value").stringValue = parts[1];
            }
        }
        private static void ExpandSampleServers()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ServerList>("Assets/Arena3Q/Content/Frontend/ServerList.asset");
            var so = new SerializedObject(asset); var servers = so.FindProperty("servers");
            if (servers.arraySize != 3) return; // Do not overwrite a user's/API's expanded directory.
            var names = new[] { "Lạc Dương", "Trường An", "Hứa Xương", "Kiến Nghiệp", "Thành Đô", "Hổ Lao Quan", "Trường Bản", "Xích Bích", "Quan Độ", "Phàn Thành",
                "Kinh Châu", "Ích Châu", "Tương Dương", "Hán Trung", "Nhai Đình", "Hợp Phì", "Di Lăng", "Định Quân Sơn", "Ngũ Trượng Nguyên", "Bạch Đế Thành" };
            servers.arraySize = names.Length;
            for (var i = 3; i < names.Length; i++)
            {
                var entry = servers.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("serverId").stringValue = "s" + (i + 1);
                entry.FindPropertyRelative("displayName").stringValue = "S" + (i + 1) + " · " + names[i];
                var status = i == 11 ? ServerStatus.Maintenance : i == 13 ? ServerStatus.Full : i % 6 == 0 ? ServerStatus.Busy : ServerStatus.Good;
                entry.FindPropertyRelative("status").intValue = (int)status;
                entry.FindPropertyRelative("recommended").boolValue = i == 7 || i == 19;
                entry.FindPropertyRelative("isNew").boolValue = i >= 17;
                entry.FindPropertyRelative("isHot").boolValue = i == 7 || i == 10;
            }
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(asset);
        }
    }
}
