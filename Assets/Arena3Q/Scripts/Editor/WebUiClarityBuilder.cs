using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Arena3Q.Editor
{
    /// <summary>Web-only UI quality policy. Preserves source GUIDs and both mobile overrides.</summary>
    public static class WebUiClarityBuilder
    {
        private static readonly string[] Roots = { "Assets/Arena3Q/Art/UI/Startup", "Assets/Arena3Q/Art/UI/Icons",
            "Assets/Arena3Q/Lobby2D/UI", "Assets/Arena3Q/Lobby2D/Backgrounds" };
        private const string AtlasPath = "Assets/Arena3Q/Lobby2D/Unity/SpriteAtlases/LobbyUI.spriteatlas";
        private const string Evidence = "NghiemThu/WebGame/Clarity_20261028";
        [Serializable] private sealed class Row
        {
            public string path, guid, sourceSha256, androidBefore, androidAfter, iosBefore, iosAfter, webFormat;
            public int sourceWidth, sourceHeight, importedWidth, importedHeight;
        }
        [Serializable] private sealed class Report { public Row[] textures; public bool mobileOverridesUnchanged; public string atlasWebFormat; }
        [MenuItem("Arena3Q/UI/Apply Web UI Clarity")]
        public static void Apply()
        {
            Directory.CreateDirectory(Evidence);
            const string source = "docs/UI/Arena3Q_UI_Handoff/Assets/UI/Backgrounds/keyart_3heroes.png";
            const string target = "Assets/Arena3Q/Art/UI/Startup/UI/Backgrounds/keyart_3heroes.png";
            if (!File.Exists(source)) throw new FileNotFoundException("Missing original startup keyart", source);
            if (Hash(source) != Hash(target))
            {
                File.Copy(target, Evidence + "/keyart-before.png", true);
                File.Copy(source, target, true);
                AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
            }
            var rows = Roots.SelectMany(root => Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                .Append("Assets/Arena3Q/Art/UI/LobbyRecruitDay.png").Select(path => path.Replace('\\', '/'))
                .Where(path => (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                    && !path.Contains("/Fonts/") && !path.Contains("/Video/")).Distinct().OrderBy(path => path).Select(ApplyTexture).ToArray();
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
            if (atlas == null) throw new InvalidOperationException("Missing integrated lobby atlas");
            var android = JsonUtility.ToJson(atlas.GetPlatformSettings("Android"));
            var ios = JsonUtility.ToJson(atlas.GetPlatformSettings("iPhone"));
            var web = atlas.GetPlatformSettings("WebGL");
            web.name = "WebGL"; web.overridden = true; web.maxTextureSize = 2048;
            web.format = TextureImporterFormat.RGBA32; web.textureCompression = TextureImporterCompression.Uncompressed;
            web.compressionQuality = 100; web.crunchedCompression = false;
            atlas.SetPlatformSettings(web); EditorUtility.SetDirty(atlas); AssetDatabase.SaveAssetIfDirty(atlas);
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, BuildTarget.WebGL);
            if (android != JsonUtility.ToJson(atlas.GetPlatformSettings("Android")) || ios != JsonUtility.ToJson(atlas.GetPlatformSettings("iPhone")))
                throw new InvalidOperationException("Web policy changed mobile atlas overrides");
            var report = new Report { textures = rows, mobileOverridesUnchanged = rows.All(row => row.androidBefore == row.androidAfter && row.iosBefore == row.iosAfter),
                atlasWebFormat = web.format.ToString() };
            File.WriteAllText(Evidence + "/web-import.json", JsonUtility.ToJson(report, true));
            if (!report.mobileOverridesUnchanged) throw new InvalidOperationException("Web policy changed mobile texture overrides");
            Debug.Log("[Web UI Clarity] Applied " + rows.Length + " scoped textures; mobile overrides unchanged");
        }
        private static Row ApplyTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing UI importer: " + path);
            var row = new Row { path = path, guid = AssetDatabase.AssetPathToGUID(path), sourceSha256 = Hash(path),
                androidBefore = JsonUtility.ToJson(importer.GetPlatformTextureSettings("Android")), iosBefore = JsonUtility.ToJson(importer.GetPlatformTextureSettings("iPhone")) };
            importer.GetSourceTextureWidthAndHeight(out row.sourceWidth, out row.sourceHeight);
            bool background = path.Contains("/Backgrounds/");
            var web = importer.GetPlatformTextureSettings("WebGL");
            var format = background ? (importer.DoesSourceTextureHaveAlpha() ? TextureImporterFormat.DXT5 : TextureImporterFormat.DXT1) : TextureImporterFormat.RGBA32;
            var compression = background ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            bool changed = !web.overridden || web.format != format || web.compressionQuality != 100 || web.maxTextureSize < 2048 || web.crunchedCompression || web.textureCompression != compression;
            web.name = "WebGL"; web.overridden = true; web.maxTextureSize = Math.Max(2048, web.maxTextureSize);
            web.format = format; web.textureCompression = compression;
            web.compressionQuality = 100; web.crunchedCompression = false;
            importer.SetPlatformTextureSettings(web);
            if (changed) importer.SaveAndReimport();
            row.androidAfter = JsonUtility.ToJson(importer.GetPlatformTextureSettings("Android")); row.iosAfter = JsonUtility.ToJson(importer.GetPlatformTextureSettings("iPhone"));
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            row.importedWidth = texture.width; row.importedHeight = texture.height; row.webFormat = web.format.ToString();
            return row;
        }
        private static string Hash(string path)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
    }
}
