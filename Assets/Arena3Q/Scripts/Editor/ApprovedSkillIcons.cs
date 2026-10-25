using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Arena3Q.Data;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>Imports approved PNGs and verifies actual Unity Sprite references without saving skill data.</summary>
    public static class ApprovedSkillIcons
    {
        [Serializable] private sealed class Registry
        {
            public string status;
            public Entry[] skills;
        }
        [Serializable] private sealed class Entry
        {
            public string id;
            public string texturePath;
            public string sha256;
            public int width;
            public int height;
            public string[] skillPaths;
        }
        [Serializable] private sealed class Result
        {
            public string id;
            public string guid;
            public string fileId;
            public int width;
            public int height;
        }
        [Serializable] private sealed class Report
        {
            public Result[] skills;
        }

        [MenuItem("Arena3Q/Skills/Import Approved Icon Textures")]
        public static void Import() => Run(false);

        [MenuItem("Arena3Q/Skills/Verify Approved Icon Bindings")]
        public static void Verify() => Run(true);

        private static void Run(bool verify)
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            var registry = JsonUtility.FromJson<Registry>(File.ReadAllText(Path.Combine(root, "Admin/reviews/skill-icons-20261025.json")));
            if (registry.status != "APPROVED" || registry.skills == null || registry.skills.Length != 42)
                throw new InvalidOperationException("The complete user-approved icon registry is required.");
            var ids = new HashSet<string>();
            // Validate every source and target before importing textures.
            foreach (var entry in registry.skills)
            {
                if (!ids.Add(entry.id) || entry.texturePath != $"Assets/Arena3Q/Art/SkillIcons/{entry.id}.png" ||
                    entry.id.Contains("/") || entry.id.Contains("\\") || entry.id.Contains(".."))
                    throw new InvalidOperationException("Invalid approved icon identity.");
                using (var sha = SHA256.Create())
                {
                    var bytes = File.ReadAllBytes(Path.Combine(root, entry.texturePath));
                    var hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                    if (hash != entry.sha256) throw new InvalidOperationException("Artwork changed after approval: " + entry.id);
                }
                foreach (var source in entry.skillPaths)
                {
                    if (!source.StartsWith("Assets/Arena3Q/Content/Skills/", StringComparison.Ordinal) || source.Contains(".."))
                        throw new InvalidOperationException("Skill source outside allowed folder.");
                    var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(source);
                    if (skill == null || skill.SkillId != entry.id) throw new InvalidOperationException("Skill identity mismatch: " + source);
                }
            }
            var results = new List<Result>();
            foreach (var entry in registry.skills)
            {
                if (!verify)
                {
                    AssetDatabase.ImportAsset(entry.texturePath, ImportAssetOptions.ForceSynchronousImport);
                    var importer = AssetImporter.GetAtPath(entry.texturePath) as TextureImporter;
                    if (importer == null) throw new InvalidOperationException("No texture importer: " + entry.id);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.maxTextureSize = 2048;
                    importer.sRGBTexture = true;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.crunchedCompression = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    foreach (var platform in new[] { "Standalone", "Android", "iPhone" })
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                        {
                            name = platform, overridden = true, maxTextureSize = 2048,
                            format = TextureImporterFormat.RGBA32,
                            textureCompression = TextureImporterCompression.Uncompressed
                        });
                    importer.SaveAndReimport();
                }
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entry.texturePath);
                if (sprite == null || sprite.texture.width != entry.width || sprite.texture.height != entry.height)
                    throw new InvalidOperationException("Sprite missing or resized: " + entry.id);
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long fileId))
                    throw new InvalidOperationException("Cannot resolve Sprite identity: " + entry.id);
                foreach (var source in entry.skillPaths)
                {
                    var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(source);
                    if (verify && skill.Icon != sprite) throw new InvalidOperationException("Sprite binding failed: " + entry.id);
                }
                results.Add(new Result { id = entry.id, guid = guid, fileId = fileId.ToString(), width = sprite.texture.width, height = sprite.texture.height });
            }
            File.WriteAllText(Path.Combine(root, "Admin/.local/unity-approved-skill-icons.json"), JsonUtility.ToJson(new Report { skills = results.ToArray() }, true));
            Debug.Log($"[ApprovedSkillIcons] {(verify ? "Verified bindings" : "Imported textures")}: {results.Count} original-resolution icons; no skill data written by Unity tool.");
        }
    }
}
