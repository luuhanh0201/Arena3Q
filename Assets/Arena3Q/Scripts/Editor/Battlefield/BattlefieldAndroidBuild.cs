using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Arena3Q.Editor
{
    public static class BattlefieldAndroidBuild
    {
        public const string Output="Builds/Android/Arena3Q-Battlefield-20261025.apk";
        [MenuItem("Arena3Q/Battlefield/Build Android Test APK")]
        public static void Build()
        {
            if(!File.Exists(BattlefieldBuilder.ScenePath))throw new InvalidOperationException("Generate Battlefield first.");
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            bool bundle=EditorUserBuildSettings.buildAppBundle;
            try
            {
                EditorUserBuildSettings.buildAppBundle=false;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes=new[]{BattlefieldBuilder.ScenePath,"Assets/Arena3Q/Scenes/Frontend.unity","Assets/Arena3Q/Scenes/Battle.unity"},
                    locationPathName=Output,target=BuildTarget.Android,targetGroup=BuildTargetGroup.Android,options=BuildOptions.Development
                });
                var s=report.summary;
                string result=$"result={s.result}\noutput={Output}\nbytes={s.totalSize}\nseconds={s.totalTime.TotalSeconds:F1}\nerrors={s.totalErrors}\nwarnings={s.totalWarnings}\nStartup scene={BattlefieldBuilder.ScenePath}\n";
                File.WriteAllText("NghiemThu/Battlefield/XemTruoc_Map_20261025/android-build.txt",result);Debug.Log("[Battlefield APK] "+result);
                if(s.result!=BuildResult.Succeeded)throw new InvalidOperationException("Battlefield APK build failed: "+s.result);
            }
            finally{EditorUserBuildSettings.buildAppBundle=bundle;}
        }
    }
}
