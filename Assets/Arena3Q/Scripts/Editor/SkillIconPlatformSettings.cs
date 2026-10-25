using System;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>Editor-only settings. Platform overrides share the same approved sources and Sprite identities.</summary>
    public sealed class SkillIconPlatformSettings : ScriptableObject
    {
        public const string AssetPath="Assets/Arena3Q/Scripts/Editor/SkillIconPlatforms.asset";
        [Min(2048)] public int maximumSize=2048;
        [Range(0,100)] public int encoderQuality=100;
        public TextureImporterFormat androidFormat=TextureImporterFormat.ASTC_4x4;
        public TextureImporterFormat iphoneFormat=TextureImporterFormat.ASTC_4x4;
        public TextureImporterFormat webFormat=TextureImporterFormat.RGBA32;
        public string androidOutput="Builds/Android/Arena3Q-Mobile-20261025.apk";
        public bool recreateAndroidPackage=true;
        public string futureWebOutput="Builds/WebGL/Arena3Q";

        public static SkillIconPlatformSettings Load(bool create=false)
        {
            var settings=AssetDatabase.LoadAssetAtPath<SkillIconPlatformSettings>(AssetPath);
            if(settings==null&&create){settings=CreateInstance<SkillIconPlatformSettings>();AssetDatabase.CreateAsset(settings,AssetPath);AssetDatabase.SaveAssetIfDirty(settings);}
            if(settings==null)throw new InvalidOperationException("Import the approved icon platform settings first.");
            return settings;
        }
        public TextureImporterPlatformSettings For(string platform)
        {
            var format=platform switch {"Android"=>androidFormat,"iPhone"=>iphoneFormat,"WebGL"=>webFormat,"Standalone"=>TextureImporterFormat.RGBA32,_=>throw new ArgumentOutOfRangeException(nameof(platform))};
            return new TextureImporterPlatformSettings{name=platform,overridden=true,maxTextureSize=maximumSize,format=format,compressionQuality=encoderQuality,
                textureCompression=format==TextureImporterFormat.RGBA32?TextureImporterCompression.Uncompressed:TextureImporterCompression.CompressedHQ};
        }
    }
}
