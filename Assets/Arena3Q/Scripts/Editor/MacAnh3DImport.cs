using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    internal sealed class MacAnh3DImport : AssetPostprocessor
    {
        private const string Source="Assets/Arena3Q/Art/Heroes/MacAnh/Graphics3D/Source/";
        void OnPreprocessModel()
        {
            if(!assetPath.StartsWith(Source) || !assetImporter.importSettingsMissing) return;
            var m=(ModelImporter)assetImporter;
            m.globalScale=1; m.useFileScale=true; m.preserveHierarchy=true;
            m.animationType=ModelImporterAnimationType.Generic;
            m.optimizeGameObjects=false; m.meshCompression=ModelImporterMeshCompression.Off;
            m.isReadable=false; m.importBlendShapes=false; m.importCameras=false; m.importLights=false;
            m.importNormals=ModelImporterNormals.Import; m.importTangents=ModelImporterTangents.CalculateMikk;
            m.materialImportMode=ModelImporterMaterialImportMode.None;
            m.skinWeights=ModelImporterSkinWeights.Custom; m.maxBonesPerVertex=assetPath.Contains("/LOD/")?4:16; m.minBoneWeight=0;
            m.animationCompression=ModelImporterAnimationCompression.Off;
        }
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith(Source) || !assetImporter.importSettingsMissing) return;
            var t=(TextureImporter)assetImporter;
            t.textureType=TextureImporterType.Default;t.spriteImportMode=SpriteImportMode.None;
            t.mipmapEnabled=true;t.filterMode=FilterMode.Trilinear;t.anisoLevel=4;
            t.maxTextureSize=assetPath.EndsWith("Image_0.jpg")?8192:4096;
            t.sRGBTexture=!assetPath.EndsWith("Image_1.jpg")&&!assetPath.EndsWith("Image_2.jpg");
            if(assetPath.EndsWith("Image_2.jpg")) {t.textureType=TextureImporterType.NormalMap;t.convertToNormalmap=false;}
            t.textureCompression=TextureImporterCompression.CompressedHQ;
            // PC preview must not decompress unsupported mobile ETC2 into RGBA32.
            t.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name="Standalone",overridden=true,
                maxTextureSize=t.maxTextureSize,format=TextureImporterFormat.DXT5,compressionQuality=100 });
            t.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name="Android",overridden=true,
                maxTextureSize=assetPath.EndsWith("Image_0.jpg")?4096:2048,format=TextureImporterFormat.ASTC_6x6,compressionQuality=100 });
        }
    }
}
