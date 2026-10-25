using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arena3Q.Data;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>Reads the existing build report and dependency graph. Never rebuilds, saves or modifies game assets.</summary>
    public static class BattlefieldBuildSizeAudit
    {
        [Serializable] private sealed class Item { public string file, source, type; public ulong offset, bytes; }
        [Serializable] private sealed class Icon { public string path, format, compression; public int width, height; public bool overridden; }
        [Serializable] private sealed class Audit { public Item[] packed; public Icon[] icons; public string[] libraryOnlyAssets, resourceRoots; public ulong libraryOnlyBytes; }

        public static void Export()
        {
            const string library="Assets/Arena3Q/Content/Battlefield/BattlefieldAssetLibrary.asset";
            var report=BuildReport.GetLatestReport();
            if(report==null || !report.summary.outputPath.EndsWith("Arena3Q-Battlefield-Feedback-20261025.apk",StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Latest build report is not the delivered Feedback APK.");
            var packed=report.packedAssets.SelectMany(p=>p.contents.Select(c=>new Item{file=p.shortPath,source=c.sourceAssetPath,type=c.type.ToString(),offset=c.offset,bytes=c.packedSize})).ToArray();
            var resources=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)&&p.Contains("/Resources/")&&!AssetDatabase.IsValidFolder(p)).ToArray();
            var roots=new[]{BattlefieldBuilder.ScenePath,"Assets/Arena3Q/Scenes/Frontend.unity","Assets/Arena3Q/Scenes/Battle.unity"}.Concat(resources).ToArray();
            var all=new HashSet<string>(AssetDatabase.GetDependencies(roots,true));
            var needed=new HashSet<string>();var queue=new Queue<string>(roots);
            while(queue.Count>0)
            {
                var path=queue.Dequeue();if(path==library||!needed.Add(path))continue;
                foreach(var dependency in AssetDatabase.GetDependencies(path,false))if(dependency!=path)queue.Enqueue(dependency);
            }
            all.ExceptWith(needed);all.Remove(library);
            var libraryOnly=packed.Where(p=>all.Contains(p.source)).ToArray();
            var icons=packed.Where(p=>p.source.StartsWith("Assets/Arena3Q/Art/SkillIcons/",StringComparison.Ordinal)).Select(p=>p.source).Distinct().Select(path=>
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);var settings=importer.GetPlatformTextureSettings("Android");
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                return new Icon{path=path,width=texture.width,height=texture.height,overridden=settings.overridden,format=settings.format.ToString(),compression=settings.textureCompression.ToString()};
            }).ToArray();
            ulong bytes=0;foreach(var item in libraryOnly)bytes+=item.bytes;
            const string output="NghiemThu/Battlefield/AuditDungLuong_20261025";Directory.CreateDirectory(output);
            File.WriteAllText(output+"/unity-build-size.json",JsonUtility.ToJson(new Audit{packed=packed,icons=icons,libraryOnlyAssets=libraryOnly.Select(p=>p.source).Distinct().OrderBy(p=>p).ToArray(),resourceRoots=resources,libraryOnlyBytes=bytes},true));
            Debug.Log($"[BuildSizeAudit] {packed.Length} packed objects; {icons.Length} icons; {libraryOnly.Select(p=>p.source).Distinct().Count()} assets retained only by the editor library ({bytes} bytes before APK compression). No game asset modified.");
        }
    }
}
