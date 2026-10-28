using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Arena3Q.Data;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>Independent browser build. Never changes texture overrides, mobile scenes or build profiles.</summary>
    public static class WebGameBuild
    {
        [Serializable] private sealed class Settings
        {
            public string output, startScene, introSource, introFile;
            public string[] scenes;
        }
        [Serializable] private sealed class BrowserBuild
        {
            public string loaderUrl, dataUrl, frameworkUrl, codeUrl;
            public string streamingAssetsUrl = "Player/StreamingAssets";
            public string companyName, productName, productVersion;
            public string contentHash, startupScene, battleScene;
        }

        [MenuItem("Arena3Q/Build/Web Game (Phone, iPad, PC)")]
        public static void Build()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new InvalidOperationException("Install Unity Web Build Support first.");
            var settings = JsonUtility.FromJson<Settings>(File.ReadAllText("WebGame/web-settings.json"));
            string output = Path.GetFullPath(settings.output);
            string allowed = Path.GetFullPath("Builds/WebGL") + Path.DirectorySeparatorChar;
            if (!output.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Web output must stay within Builds/WebGL.");
            if (settings.scenes == null || settings.scenes.Length == 0 || settings.scenes[0] != settings.startScene || settings.scenes.Any(s => !File.Exists(s)))
                throw new InvalidOperationException("Every configured web scene must exist, with startScene first.");
            CurrentGameRelease.Validate();
            if (!settings.scenes.SequenceEqual(CurrentGameRelease.ScenePaths))
                throw new InvalidOperationException("Web scenes differ from the current mobile/web release manifest.");
            ApprovedSkillIcons.Verify();
            var typography = Resources.Load<UiTypographyConfig>(UiTypographyConfig.ResourceName);
            if (typography == null || typography.browserFont == null)
                throw new InvalidOperationException("Browser UI requires the embedded mobile font.");
            foreach (char glyph in "Ải Công Thủ Đồ họa Chủ tướng Triệu hồi Kỵ Binh Lữ Bố")
                if (!typography.browserFont.HasCharacter(glyph))
                    throw new InvalidOperationException("Browser font lacks Vietnamese glyph: " + glyph);
            Directory.CreateDirectory(output);
            string template = PlayerSettings.WebGL.template;
            var compression = PlayerSettings.WebGL.compressionFormat;
            bool fallback = PlayerSettings.WebGL.decompressionFallback;
            try
            {
                PlayerSettings.WebGL.template = "APPLICATION:Minimal";
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = settings.scenes,
                    locationPathName = Path.Combine(output, "Player"),
                    target = BuildTarget.WebGL,
                    targetGroup = BuildTargetGroup.WebGL,
                    options = BuildOptions.None
                });
                var summary = report.summary;
                if (summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Web game build failed: " + summary.result);
                Package(output, settings);
                long bytes = Directory.GetFiles(output, "*", SearchOption.AllDirectories).Sum(p => new FileInfo(p).Length);
                string evidence = "NghiemThu/WebGame/20261025";
                Directory.CreateDirectory(evidence);
                string result = $"result={summary.result}\noutput={output}\nbytes={bytes}\nMB={bytes / 1048576d:F1}\nseconds={summary.totalTime.TotalSeconds:F1}\nerrors={summary.totalErrors}\nwarnings={summary.totalWarnings}\nstartScene={settings.startScene}\n";
                File.WriteAllText(Path.Combine(evidence, "build.txt"), result);
                Debug.Log("[Arena3Q Web] " + result);
            }
            finally
            {
                PlayerSettings.WebGL.template = template;
                PlayerSettings.WebGL.compressionFormat = compression;
                PlayerSettings.WebGL.decompressionFallback = fallback;
            }
        }

        private static void Package(string output, Settings settings)
        {
            foreach (string file in new[] { "index.html", "style.css", "player.mjs", "device-preview.mjs", "web-settings.json" })
                File.Copy(Path.Combine("WebGame", file), Path.Combine(output, file), true);
            string art = Path.Combine(output, "art");
            Directory.CreateDirectory(art);
            const string source = "Assets/Arena3Q/Art/UI/Startup/";
            foreach (var pair in new[] {
                ("UI/Backgrounds/keyart_3heroes.png", "background.png"),
                ("UI/Logo/logo_arena3q.png", "logo.png"),
                ("UI/Sprites/Buttons/btn_gold.png", "btn_gold.png"),
                ("UI/Sprites/Buttons/btn_blue.png", "btn_blue.png"),
                ("UI/Sprites/Frames/frame_panel.png", "panel.png"),
                ("UI/Sprites/Ornaments/crest_dragon.png", "crest.png"),
                ("Fonts/BeVietnamPro-Regular.ttf", "regular.ttf"),
                ("Fonts/BeVietnamPro-ExtraBold.ttf", "bold.ttf"),
                ("Fonts/OFL.txt", "OFL.txt") })
                File.Copy(source + pair.Item1, Path.Combine(art, pair.Item2), true);
            string streaming = Path.Combine(output, "Player/StreamingAssets");
            Directory.CreateDirectory(streaming);
            File.Copy(settings.introSource, Path.Combine(streaming, settings.introFile), true);
            string build = Path.Combine(output, "Player/Build");
            string Hash(string file)
            {
                using var sha = SHA256.Create(); using var stream = File.OpenRead(file);
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            }
            string Pick(string pattern)
            {
                string file = Directory.GetFiles(build, pattern).Single();
                return "Player/Build/" + Path.GetFileName(file) + "?v=" + Hash(file).Substring(0, 20);
            }
            var browser = new BrowserBuild {
                loaderUrl = Pick("*.loader.js"), dataUrl = Pick("*.data*"),
                frameworkUrl = Pick("*.framework.js*"), codeUrl = Pick("*.wasm*"),
                companyName = PlayerSettings.companyName, productName = PlayerSettings.productName,
                productVersion = Application.version,
                contentHash = Hash(Directory.GetFiles(build, "*.data*").Single()),
                startupScene = settings.startScene,
                battleScene = CurrentGameRelease.Settings.frontend.BattleSceneName
            };
            File.WriteAllText(Path.Combine(output, "build.json"), JsonUtility.ToJson(browser, true));
        }
    }
}
