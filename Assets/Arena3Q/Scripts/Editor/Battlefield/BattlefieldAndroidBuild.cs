using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Arena3Q.Editor
{
    public static class BattlefieldAndroidBuild
    {
        public const string Output="Builds/Android/Arena3Q-Battlefield-Feedback-20261025.apk";
        [MenuItem("Arena3Q/Battlefield/Build Android Test APK")]
        public static void Build()=>BuildAt(Output,"NghiemThu/Battlefield/XemTruoc_ChinhSua_20261025");

        [MenuItem("Arena3Q/Build/Mobile Android (Optimized Icons)")]
        public static void BuildMobile()
        {
            CurrentGameRelease.Validate();
            ApprovedSkillIcons.Verify();
            string output=SkillIconPlatformSettings.Load().androidOutput;
            string builds=Path.GetFullPath("Builds/Android")+Path.DirectorySeparatorChar;
            if(!Path.GetFullPath(output).StartsWith(builds,StringComparison.OrdinalIgnoreCase)||!output.EndsWith(".apk",StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Mobile output must be an APK inside Builds/Android.");
            if(SkillIconPlatformSettings.Load().recreateAndroidPackage)
            {
                // Recreate only the generated Gradle APK. Incremental ZIP packaging otherwise retains
                // deleted texture payload as holes after a large compression change.
                string cacheRoot=Path.GetFullPath("Library/Bee/Android")+Path.DirectorySeparatorChar;
                string cached=Path.GetFullPath("Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build/outputs/apk/debug/launcher-debug.apk");
                if(!cached.StartsWith(cacheRoot,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Generated APK is outside the build cache.");
                if(File.Exists(cached)){File.Delete(cached);Debug.Log("[Mobile APK] Removed generated Gradle package for a fresh ZIP; source assets and previous delivered APKs are preserved.");}
            }
            BuildAt(output,CurrentGameRelease.Evidence,CurrentGameRelease.ScenePaths);
        }

        private static void BuildAt(string output,string evidence,string[] releaseScenes=null)
        {
            if(!File.Exists(BattlefieldBuilder.ScenePath))throw new InvalidOperationException("Generate Battlefield first.");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            bool bundle=EditorUserBuildSettings.buildAppBundle;
            try
            {
                EditorUserBuildSettings.buildAppBundle=false;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes=releaseScenes??new[]{BattlefieldBuilder.ScenePath,"Assets/Arena3Q/Scenes/Frontend.unity","Assets/Arena3Q/Scenes/Battle.unity"},
                    locationPathName=output,target=BuildTarget.Android,targetGroup=BuildTargetGroup.Android,options=BuildOptions.Development
                });
                var s=report.summary;
                long apkBytes=File.Exists(output)?new FileInfo(output).Length:0;
                string result=$"result={s.result}\noutput={output}\napkBytes={apkBytes}\nMB={apkBytes/1048576d:F1}\nallBuildOutputBytes={s.totalSize}\nseconds={s.totalTime.TotalSeconds:F1}\nerrors={s.totalErrors}\nwarnings={s.totalWarnings}\nStartup scene={releaseScenes?[0]??BattlefieldBuilder.ScenePath}\n";
                Directory.CreateDirectory(evidence);
                File.WriteAllText(evidence+"/android-build.txt",result);Debug.Log("[Battlefield APK] "+result);
                if(s.result!=BuildResult.Succeeded)throw new InvalidOperationException("Battlefield APK build failed: "+s.result);
                if(releaseScenes!=null){SkillIconMobileQa.VerifyBuild(report,evidence);CurrentGameRelease.VerifyBuild(report);}
            }
            finally{EditorUserBuildSettings.buildAppBundle=bundle;}
        }
    }
}
