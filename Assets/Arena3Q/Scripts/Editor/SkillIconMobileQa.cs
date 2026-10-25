using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Arena3Q.Editor
{
    public static class SkillIconMobileQa
    {
        public const string Evidence="NghiemThu/Icons/Mobile_20261025";
        [Serializable] private sealed class Registry { public Entry[] skills; }
        [Serializable] private sealed class Entry { public string id,texturePath; public int width,height; }
        [Serializable] private sealed class Binding { public string id,guid,fileId; public int width,height; }
        [Serializable] private sealed class Bindings { public Binding[] skills; }
        [Serializable] private sealed class BuiltIcon { public string source; public ulong bytes; }
        [Serializable] private sealed class BuildIcons { public int count; public ulong textureBytes; public BuiltIcon[] icons; }
        public static void Before()=>Capture(false);
        public static void ApplyAndCapture(){ApprovedSkillIcons.Import();ApprovedSkillIcons.Verify();Capture(true);}
        public static void VerifyLatestMobileBuild()
        {
            ApprovedSkillIcons.Verify();var report=BuildReport.GetLatestReport();
            if(report==null||Path.GetFullPath(report.summary.outputPath)!=Path.GetFullPath(SkillIconPlatformSettings.Load().androidOutput))
                throw new InvalidOperationException("Latest report does not match the mobile APK output.");
            VerifyBuild(report);
        }
        public static void VerifyBuild(BuildReport report,string evidence=Evidence)
        {
            var registry=JsonUtility.FromJson<Registry>(File.ReadAllText("Admin/reviews/skill-icons-20261025.json"));
            var sources=registry.skills.ToDictionary(e=>e.texturePath);
            var icons=report.packedAssets.SelectMany(p=>p.contents).Where(c=>c.type==typeof(Texture2D)&&sources.ContainsKey(c.sourceAssetPath)).Select(c=>
            {
                var entry=sources[c.sourceAssetPath];ulong expected=(ulong)((entry.width+3)/4)*(ulong)((entry.height+3)/4)*16;
                if(c.packedSize<expected||c.packedSize>expected+4096)throw new InvalidOperationException("Built icon is not the expected ASTC4x4 size: "+entry.id+" / "+c.packedSize);
                return new BuiltIcon{source=c.sourceAssetPath,bytes=c.packedSize};
            }).ToArray();
            if(icons.Length==0)throw new InvalidOperationException("No skill icons found in mobile APK report.");
            ulong bytes=0;foreach(var icon in icons)bytes+=icon.bytes;
            Directory.CreateDirectory(evidence);File.WriteAllText(evidence+"/build-icons.json",JsonUtility.ToJson(new BuildIcons{count=icons.Length,textureBytes=bytes,icons=icons},true));
            Debug.Log($"[IconMobileQA] Built {icons.Length} ASTC4x4 icon textures, {bytes} bytes before ZIP.");
        }
        private static void Capture(bool after)
        {
            Directory.CreateDirectory(Evidence+(after?"/after":"/before"));
            var registry=JsonUtility.FromJson<Registry>(File.ReadAllText("Admin/reviews/skill-icons-20261025.json"));
            var bindings=registry.skills.Select(entry=>
            {
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(entry.texturePath);
                if(sprite==null||sprite.texture.width!=entry.width||sprite.texture.height!=entry.height)throw new InvalidOperationException("Changed icon dimensions: "+entry.id);
                if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long fileId))throw new InvalidOperationException("Missing sprite identity.");
                Snapshot(sprite.texture,Evidence+(after?"/after/":"/before/")+entry.id+".png");
                return new Binding{id=entry.id,guid=guid,fileId=fileId.ToString(),width=sprite.texture.width,height=sprite.texture.height};
            }).ToArray();
            File.WriteAllText(Evidence+(after?"/after-bindings.json":"/before-bindings.json"),JsonUtility.ToJson(new Bindings{skills=bindings},true));
            if(after)
            {
                var old=JsonUtility.FromJson<Bindings>(File.ReadAllText(Evidence+"/before-bindings.json"));
                if(JsonUtility.ToJson(old)!=JsonUtility.ToJson(new Bindings{skills=bindings}))throw new InvalidOperationException("Sprite identity or size changed.");
            }
            Debug.Log("[IconMobileQA] "+(after?"After":"Before")+": "+bindings.Length+" rendered icon textures, dimensions and Sprite identities verified.");
        }
        private static void Snapshot(Texture source,string path)
        {
            var previous=RenderTexture.active;var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            Texture2D image=null;
            try{Graphics.Blit(source,rt);RenderTexture.active=rt;image=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,source.width,source.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);if(image!=null)UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
