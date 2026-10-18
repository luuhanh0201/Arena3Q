using System;
using System.IO;
using UnityEditor;

namespace Arena3Q.Editor
{
    /// <summary>
    /// First-import settings for Meshy downloads (files named Meshy_AI_*): they are only sources for
    /// <see cref="PropSpriteRenderer"/>, so skip materials / animation and keep the textures uncompressed (fast import,
    /// sharp renders). Normal, metallic and roughness maps are linear data; the last two are not used and stay tiny.
    /// Later manual changes in the Inspector are kept (only applied while the import settings are missing).
    /// </summary>
    internal sealed class MeshyAssetPostprocessor : AssetPostprocessor
    {
        private const string Prefix = "Meshy_AI_";

        private void OnPreprocessModel()
        {
            if (!IsNewMeshyAsset())
                return;
            var importer = (ModelImporter)assetImporter;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importConstraints = false;
            importer.isReadable = false;
        }

        private void OnPreprocessTexture()
        {
            if (!IsNewMeshyAsset())
                return;
            var importer = (TextureImporter)assetImporter;
            var file = Path.GetFileNameWithoutExtension(assetPath);
            var isNormal = file.EndsWith("_normal", StringComparison.Ordinal);
            var isUnused = file.EndsWith("_metallic", StringComparison.Ordinal)
                           || file.EndsWith("_roughness", StringComparison.Ordinal);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = !isNormal && !isUnused;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = isUnused ? 32 : 2048;
        }

        private bool IsNewMeshyAsset() =>
            assetImporter.importSettingsMissing
            && Path.GetFileName(assetPath).StartsWith(Prefix, StringComparison.Ordinal);
    }
}
