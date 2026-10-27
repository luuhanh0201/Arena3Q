using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Arena3Q.Editor
{
    /// <summary>Scoped UI-only mobile import policy and evidence. Does not change skill icons or source artwork.</summary>
    public static class LobbyCompressionQa
    {
        public const string Evidence = "NghiemThu/UI/LobbyCompression_20261027";
        private const string AtlasPath = "Assets/Arena3Q/Lobby2D/Unity/SpriteAtlases/LobbyUI.spriteatlas";
        private static readonly string[] TextureRoots = {
            "Assets/Arena3Q/Lobby2D/UI", "Assets/Arena3Q/Lobby2D/Backgrounds",
            "Assets/Arena3Q/Art/UI/Startup", "Assets/Arena3Q/Art/UI/Icons"
        };
        [Serializable] private sealed class PlatformState
        {
            public string name; public int maxTextureSize, format, textureCompression, compressionQuality; public bool overridden;
        }
        [Serializable] private sealed class TextureState
        {
            public string path, guid, pngSha256, importedFormat; public bool mipmapEnabled;
            public int width, height; public long estimatedAstcBytes; public PlatformState[] platforms;
        }
        [Serializable] private sealed class Snapshot
        {
            public string target, label, note; public TextureState[] textures; public PlatformState[] atlas;
        }
        [Serializable] private sealed class PackedEntry { public string path, type, group; public ulong bytes; }
        [Serializable] private sealed class GroupBytes { public string group; public int count; public ulong bytes; }
        [Serializable] private sealed class BuildEvidence
        {
            public string label, output, result, platform; public long apkBytes; public double apkMB, seconds;
            public int errors, warnings; public bool noLegacyLuBu; public string[] forbiddenFound;
            public GroupBytes[] groups; public PackedEntry[] assets;
        }
        [MenuItem("Arena3Q/UI/Apply Mobile UI Compression and Capture")]
        public static void ApplyAndCapture()
        {
            ApplyOptimized(); Capture("after");
        }
        [MenuItem("Arena3Q/UI/Build UI Compression Baseline APK")]
        public static void BuildBaseline()
        {
            CurrentGameRelease.Validate(); ApprovedSkillIcons.Verify();
            try { ApplyBaseline(); Capture("before"); BuildAt("Before"); }
            finally { ApplyOptimized(); }
        }
        [MenuItem("Arena3Q/UI/Build UI Compression Optimized APK")]
        public static void BuildOptimized()
        {
            CurrentGameRelease.Validate(); ApprovedSkillIcons.Verify();
            ApplyOptimized(); Capture("after"); BuildAt("After");
        }
        public static void ApplyOptimized()
        {
            foreach (var path in TexturePaths())
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing UI importer: " + path);
                bool dirty = importer.mipmapEnabled;
                importer.mipmapEnabled = false;
                foreach (var platform in new[] { "Android", "iPhone" })
                {
                    var settings = importer.GetPlatformTextureSettings(platform);
                    int maximum = settings.maxTextureSize > 0 ? settings.maxTextureSize : importer.maxTextureSize;
                    dirty |= !settings.overridden || settings.format != TextureImporterFormat.ASTC_4x4 ||
                        settings.compressionQuality != 100 || settings.crunchedCompression || settings.allowsAlphaSplitting;
                    settings.name = platform; settings.overridden = true; settings.maxTextureSize = maximum;
                    settings.format = TextureImporterFormat.ASTC_4x4; settings.textureCompression = TextureImporterCompression.Compressed;
                    settings.compressionQuality = 100; settings.crunchedCompression = false; settings.allowsAlphaSplitting = false;
                    importer.SetPlatformTextureSettings(settings);
                }
                if (dirty) importer.SaveAndReimport();
            }
            var atlas = RequiredAtlas();
            foreach (var platform in new[] { "Android", "iPhone" })
            {
                var settings = atlas.GetPlatformSettings(platform);
                settings.name = platform; settings.overridden = true; settings.maxTextureSize = 2048;
                settings.format = TextureImporterFormat.ASTC_4x4; settings.textureCompression = TextureImporterCompression.Compressed;
                settings.compressionQuality = 100; settings.crunchedCompression = false; settings.allowsAlphaSplitting = false;
                atlas.SetPlatformSettings(settings);
            }
            EditorUtility.SetDirty(atlas); AssetDatabase.SaveAssetIfDirty(atlas);
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            VerifyOptimized();
        }
        /// <summary>Shared release gate: old art-review scenes and components cannot enter the game dependency graph.</summary>
        public static void ValidateReleaseScenes(IEnumerable<string> scenePaths)
        {
            var scenes = scenePaths.ToArray();
            var legacy = AssetDatabase.GetDependencies(scenes, true).Where(ForbiddenLegacyLobby).Distinct().OrderBy(path => path).ToArray();
            if (legacy.Length != 0)
                throw new InvalidOperationException("Release depends on the legacy lobby art review: " + string.Join(", ", legacy));
            if (Directory.Exists("Assets/Arena3Q/Lobby2D/UI/Resources"))
                throw new InvalidOperationException("Lobby ResourceBar must not be stored in a Resources folder.");
        }
        private static void ApplyBaseline()
        {
            var snapshot = JsonUtility.FromJson<Snapshot>(File.ReadAllText(Evidence + "/before-platforms.json"));
            foreach (var path in TexturePaths())
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var previous = snapshot.textures.FirstOrDefault(row => ResolveRenamedPath(row.path) == path);
                foreach (var platform in new[] { "Android", "iPhone" })
                {
                    string canonical = platform == "iPhone" ? "iOS" : platform;
                    var saved = previous?.platforms?.FirstOrDefault(row => row.name == canonical || row.name == platform);
                    var settings = importer.GetPlatformTextureSettings(platform);
                    settings.name = platform; settings.overridden = saved != null && saved.overridden;
                    settings.format = saved != null ? (TextureImporterFormat)saved.format : TextureImporterFormat.Automatic;
                    settings.maxTextureSize = saved != null && saved.maxTextureSize > 0 ? saved.maxTextureSize : importer.maxTextureSize;
                    settings.textureCompression = saved != null ? (TextureImporterCompression)saved.textureCompression : TextureImporterCompression.Compressed;
                    settings.compressionQuality = saved?.compressionQuality ?? 50;
                    settings.crunchedCompression = false; settings.allowsAlphaSplitting = false;
                    importer.SetPlatformTextureSettings(settings);
                }
                importer.SaveAndReimport();
            }
            // The immutable before snapshot has only DefaultTexturePlatform for this atlas.
            var atlas = RequiredAtlas();
            foreach (var platform in new[] { "Android", "iPhone" })
            {
                var settings = atlas.GetPlatformSettings(platform); settings.name = platform; settings.overridden = false;
                atlas.SetPlatformSettings(settings);
            }
            EditorUtility.SetDirty(atlas); AssetDatabase.SaveAssetIfDirty(atlas);
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
        }
        private static void VerifyOptimized()
        {
            foreach (var path in TexturePaths())
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer.mipmapEnabled) throw new InvalidOperationException("UI mipmaps enabled: " + path);
                foreach (var platform in new[] { "Android", "iPhone" })
                {
                    var settings = importer.GetPlatformTextureSettings(platform);
                    if (!settings.overridden || settings.format != TextureImporterFormat.ASTC_4x4)
                        throw new InvalidOperationException("Missing ASTC4x4 UI override: " + platform + " " + path);
                }
            }
            foreach (var platform in new[] { "Android", "iPhone" })
            {
                var settings = RequiredAtlas().GetPlatformSettings(platform);
                if (!settings.overridden || settings.format != TextureImporterFormat.ASTC_4x4 || settings.maxTextureSize < 2048)
                    throw new InvalidOperationException("Lobby atlas mobile settings invalid: " + platform);
            }
        }
        private static void Capture(string label)
        {
            string run = Evidence + "/" + label + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"); Directory.CreateDirectory(run);
            var states = TexturePaths().Select(path =>
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                return new TextureState { path = path, guid = AssetDatabase.AssetPathToGUID(path), pngSha256 = Hash(path),
                    mipmapEnabled = importer.mipmapEnabled, importedFormat = texture.format.ToString(), width = texture.width, height = texture.height,
                    estimatedAstcBytes = ((texture.width + 3L) / 4) * ((texture.height + 3L) / 4) * 16,
                    platforms = new[] { State(importer.GetPlatformTextureSettings("Android")), State(importer.GetPlatformTextureSettings("iPhone")) } };
            }).ToArray();
            var snapshot = new Snapshot { label = label, target = EditorUserBuildSettings.activeBuildTarget.ToString(), textures = states,
                atlas = new[] { State(RequiredAtlas().GetPlatformSettings("Android")), State(RequiredAtlas().GetPlatformSettings("iPhone")) },
                note = "Unity normalizes iPhone API names to iOS in serialized metadata; existing Lobby2D iOS ASTC4x4 was valid. Imported Editor format is not a substitute for packed BuildReport bytes." };
            File.WriteAllText(run + "/settings.json", JsonUtility.ToJson(snapshot, true));
            File.WriteAllText(Evidence + "/" + label + "-import-settings.json", JsonUtility.ToJson(snapshot, true));
            var samples = new[] {
                "Assets/Arena3Q/Lobby2D/UI/VIP/VIP_12.png", "Assets/Arena3Q/Lobby2D/UI/ResourceBar/Resource_Bar.png",
                "Assets/Arena3Q/Art/UI/Startup/UI/Sprites/Buttons/btn_gold.png", "Assets/Arena3Q/Art/UI/LobbyRecruitDay.png",
                "Assets/Arena3Q/Art/UI/Icons/HeroEnvelope.png", "Assets/Arena3Q/Art/UI/Icons/Star.png"
            };
            foreach (var path in samples)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) throw new InvalidOperationException("Missing sample Sprite: " + path);
                SnapshotSprite(sprite, run + "/" + Path.GetFileName(path));
            }
            File.WriteAllText(Evidence + "/latest-" + label + "-capture.txt", run);
            Debug.Log("[LobbyCompressionQA] " + label + " captured " + states.Length + " textures at " + run);
        }
        private static void SnapshotSprite(Sprite source, string path)
        {
            var rect = source.textureRect; var texture = source.texture;
            var target = RenderTexture.GetTemporary(Mathf.RoundToInt(source.rect.width), Mathf.RoundToInt(source.rect.height), 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active; Texture2D image = null;
            try
            {
                Graphics.Blit(texture, target, new Vector2(rect.width / texture.width, rect.height / texture.height), new Vector2(rect.x / texture.width, rect.y / texture.height));
                RenderTexture.active = target; image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); if (image != null) UnityEngine.Object.DestroyImmediate(image); }
        }
        private static void BuildAt(string label)
        {
            string output = "Builds/Android/Arena3Q-Lobby-UI-" + label + "-20261027.apk";
            if (File.Exists(output)) output = "Builds/Android/Arena3Q-Lobby-UI-" + label + "-20261027-" + DateTime.Now.ToString("HHmmss") + ".apk";
            string cacheRoot = Path.GetFullPath("Library/Bee/Android") + Path.DirectorySeparatorChar;
            string cached = Path.GetFullPath("Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build/outputs/apk/debug/launcher-debug.apk");
            if (!cached.StartsWith(cacheRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Generated APK path escaped build cache.");
            if (File.Exists(cached)) File.Delete(cached);
            Directory.CreateDirectory(Path.GetDirectoryName(output)); bool bundle = EditorUserBuildSettings.buildAppBundle;
            try
            {
                EditorUserBuildSettings.buildAppBundle = false;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = CurrentGameRelease.ScenePaths,
                    locationPathName = output, target = BuildTarget.Android, targetGroup = BuildTargetGroup.Android, options = BuildOptions.Development });
                VerifyBuild(report, label);
                if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("UI compression APK failed: " + report.summary.result);
                SkillIconMobileQa.VerifyBuild(report, Evidence + "/" + label); CurrentGameRelease.VerifyBuild(report);
            }
            finally { EditorUserBuildSettings.buildAppBundle = bundle; }
        }
        public static void VerifyBuild(BuildReport report, string label = "After")
        {
            var assets = report.packedAssets.SelectMany(package => package.contents).Select(entry => new PackedEntry {
                path = entry.sourceAssetPath, type = entry.type?.Name ?? "", bytes = entry.packedSize, group = Group(entry.sourceAssetPath) }).ToArray();
            var forbidden = assets.Where(entry => ForbiddenLegacyLobby(entry.path)).Select(entry => entry.path).Distinct().OrderBy(path => path).ToArray();
            var included = assets.Where(entry => entry.group != "Other").ToArray();
            var groups = included.GroupBy(entry => entry.group).Select(group => new GroupBytes { group = group.Key, count = group.Count(), bytes = group.Aggregate(0UL, (sum, entry) => sum + entry.bytes) }).ToArray();
            var summary = report.summary; long bytes = File.Exists(summary.outputPath) ? new FileInfo(summary.outputPath).Length : 0;
            var evidence = new BuildEvidence { label = label, output = summary.outputPath, result = summary.result.ToString(), platform = summary.platform.ToString(),
                apkBytes = bytes, apkMB = bytes / 1048576d, seconds = summary.totalTime.TotalSeconds, errors = (int)summary.totalErrors, warnings = (int)summary.totalWarnings,
                noLegacyLuBu = forbidden.Length == 0, forbiddenFound = forbidden, groups = groups, assets = included };
            Directory.CreateDirectory(Evidence); File.WriteAllText(Evidence + "/" + label.ToLowerInvariant() + "-build.json", JsonUtility.ToJson(evidence, true));
            Debug.Log($"[LobbyCompressionQA] {label}: {summary.result}, APK {bytes / 1048576d:F1} MB, legacy LuBu assets {forbidden.Length}.");
            if (forbidden.Length != 0) throw new InvalidOperationException("Legacy lobby art review entered the player build: " + string.Join(", ", forbidden));
        }
        private static bool ForbiddenLegacyLobby(string path) => ForbiddenLegacyLuBu(path) ||
            path.StartsWith("Assets/Arena3Q/Lobby2D/Runtime/", StringComparison.Ordinal) ||
            path.StartsWith("Assets/Arena3Q/Lobby2D/Unity/LobbyAndroidTest", StringComparison.Ordinal) ||
            path.StartsWith("Assets/Arena3Q/Lobby2D/Unity/LobbyArtReview", StringComparison.Ordinal) ||
            path == "Assets/Arena3Q/Lobby2D/Unity/LobbyArtCatalog.asset" ||
            path == "Assets/Arena3Q/Lobby2D/Unity/SpriteAtlases/LegacyLobbyReview.spriteatlas" ||
            path.StartsWith("Assets/Arena3Q/Lobby2D/Unity/Prefabs/LobbyArtReview", StringComparison.Ordinal);
        private static bool ForbiddenLegacyLuBu(string path) => path.StartsWith("Assets/Arena3Q/Lobby2D/Characters/LuBu/", StringComparison.Ordinal) ||
            path.StartsWith("Assets/Arena3Q/Lobby2D/Unity/Animators/LuBu_", StringComparison.Ordinal) || path.StartsWith("Assets/Arena3Q/Lobby2D/Unity/Prefabs/LuBu_", StringComparison.Ordinal) ||
            path == "Assets/Arena3Q/Lobby2D/Unity/LuBuCutoutRig.asset" || path == "Assets/Arena3Q/Lobby2D/Unity/LuBuSlash.asset" ||
            path.StartsWith("Assets/Arena3Q/Lobby2D/Effects/Characters/LuBu_Attack/", StringComparison.Ordinal);
        private static string Group(string path)
        {
            if (path == AtlasPath) return "LobbyAtlas";
            if (path.StartsWith("Assets/Arena3Q/Lobby2D/Backgrounds/", StringComparison.Ordinal)) return "LobbyBackgrounds";
            if (path.StartsWith("Assets/Arena3Q/Lobby2D/UI/", StringComparison.Ordinal)) return "LobbyUI";
            if (path.StartsWith("Assets/Arena3Q/Art/UI/Startup/Fonts/", StringComparison.Ordinal)) return "StartupFonts";
            if (path.StartsWith("Assets/Arena3Q/Art/UI/Startup/Video/", StringComparison.Ordinal)) return "StartupVideo";
            if (path.StartsWith("Assets/Arena3Q/Art/UI/Startup/", StringComparison.Ordinal)) return "StartupUI";
            if (path.StartsWith("Assets/Arena3Q/Art/UI/Icons/", StringComparison.Ordinal)) return "SharedIcons";
            if (path == "Assets/Arena3Q/Art/UI/LobbyRecruitDay.png") return "RecruitBanner";
            return "Other";
        }
        private static IEnumerable<string> TexturePaths() => TextureRoots.Where(Directory.Exists).SelectMany(root => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            .Concat(new[] { "Assets/Arena3Q/Art/UI/LobbyRecruitDay.png" }).Select(path => path.Replace('\\', '/'))
            .Where(path => File.Exists(path) && (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)) &&
                !path.Contains("/Fonts/") && !path.Contains("/Video/")).Distinct().OrderBy(path => path, StringComparer.Ordinal);
        private static PlatformState State(TextureImporterPlatformSettings settings) => new PlatformState { name = settings.name, maxTextureSize = settings.maxTextureSize,
            format = (int)settings.format, textureCompression = (int)settings.textureCompression, compressionQuality = settings.compressionQuality, overridden = settings.overridden };
        private static string ResolveRenamedPath(string path) => path.Replace("Lobby2D/UI/Resources/", "Lobby2D/UI/ResourceBar/");
        private static SpriteAtlas RequiredAtlas() => AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath) ?? throw new InvalidOperationException("Missing LobbyUI atlas.");
        private static string Hash(string path) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
    }
}
