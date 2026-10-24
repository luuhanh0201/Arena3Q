using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Arena3Q.Editor
{
    public static class AppIconBuilder
    {
        private const string Root = "Assets/Arena3Q/Art/UI/AppIcon/";
        [MenuItem("Arena3Q/UI/Apply Supplied Logo as App Icon")]
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Texture2D Load(string name)
            {
                var path = Root + name + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Run docs/UI/Tools/prepare-app-icon.cjs first.");
                importer.textureType = TextureImporterType.Default; importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 1024; importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            var icon = Load("Arena3Q"); var background = Load("Background"); var foreground = Load("Foreground");
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
            {
                var slots = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                foreach (var slot in slots)
                    slot.SetTextures(slot.minLayerCount > 1 ? new[] { background, foreground } : new[] { icon });
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, slots);
                Debug.Log($"[Arena3Q] Applied logo: Android {kind}, {slots.Length} slots.");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
