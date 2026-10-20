using System;
using System.IO;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Graphics3D;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arena3Q.Editor
{
    public static class MacAnhLodDeviceBenchmarkBuilder
    {
        private const string Folder=MacAnhContentBuilder.Art+"/DeviceBenchmark";
        private const string Scene=Folder+"/MacAnhLodBenchmark.unity";
        [Serializable]private class ExportInfo{public int body_triangles,fan_triangles;}
        public static void BuildContent()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var template=AssetDatabase.LoadAssetAtPath<HeroDefinition>("Assets/Arena3Q/Content/Lords/Lord_MacAnhPreview.asset");
            var heroes=new HeroDefinition[5];
            for(int i=0;i<heroes.Length;i++)
            {
                string path=Folder+"/BenchmarkHero_"+i+".asset";var copy=UnityEngine.Object.Instantiate(template);
                var serialized=new SerializedObject(copy);serialized.FindProperty("heroId").stringValue="macanh_benchmark_"+i;
                if(i>0)serialized.FindProperty("kind").enumValueIndex=(int)HeroKind.Starter;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                heroes[i]=AssetDatabase.LoadAssetAtPath<HeroDefinition>(path);
                if(heroes[i]==null){AssetDatabase.CreateAsset(copy,path);heroes[i]=copy;}
                else{EditorUtility.CopySerialized(copy,heroes[i]);UnityEngine.Object.DestroyImmediate(copy);EditorUtility.SetDirty(heroes[i]);}
            }
            var profile=AssetDatabase.LoadAssetAtPath<MacAnhPerformance>(MacAnhContentBuilder.Art+"/MacAnhPerformance.asset");
            if(profile.EnableBattle3DTrial)throw new InvalidOperationException("Production 3D toggle must stay default off.");
            var portrait=AssetDatabase.LoadAssetAtPath<GameObject>(MacAnhGraphicsBuilder.Root+"/LOD/MacAnh_Portrait3D.prefab");
            if(portrait==null)throw new InvalidOperationException("Portrait LOD must be exported and built first.");
            var clips=AssetDatabase.LoadAllAssetsAtPath(MacAnhGraphicsBuilder.Root+"/Source/LOD/MacAnh_LODActions.fbx")
                .OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
            var geometry=JsonUtility.FromJson<ExportInfo>(File.ReadAllText(MacAnhGraphicsBuilder.Root+"/Source/LOD/Export_Portrait.json"));
            SaveBundle("BenchmarkBattle",bundle=>{bundle.performance=profile;bundle.heroes=heroes;});
            SaveBundle("BenchmarkPortrait",bundle=>{bundle.portraitPrefab=portrait;bundle.clips=clips;bundle.bodyTriangles=geometry.body_triangles;bundle.fanTriangles=geometry.fan_triangles;});
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var go=new GameObject("MacAnh LOD isolated device benchmark");
            go.AddComponent<MacAnhLodDeviceBenchmark>();
            EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();
            Debug.Log("MACANH_LOD_BENCHMARK_CONTENT_COMPLETE");
        }
        private static void SaveBundle(string name,Action<MacAnhLodBenchmarkContent> configure)
        {
            string path=Folder+"/"+name+".asset";
            var content=AssetDatabase.LoadAssetAtPath<MacAnhLodBenchmarkContent>(path);
            if(content==null){content=ScriptableObject.CreateInstance<MacAnhLodBenchmarkContent>();AssetDatabase.CreateAsset(content,path);}
            configure(content);EditorUtility.SetDirty(content);
        }
        public static void BuildAndroid()
        {
            BuildContent();
            var movedResources=new System.Collections.Generic.List<string>();
            string oldId=PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),oldName=PlayerSettings.productName;
            var oldDeformation=PlayerSettings.meshDeformation;bool oldBundle=EditorUserBuildSettings.buildAppBundle;
            int oldQuality=QualitySettings.GetQualityLevel();
            string oldDefault=AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline);
            var oldPipelines=Enumerable.Range(0,QualitySettings.names.Length).Select(i=>AssetDatabase.GetAssetPath(QualitySettings.GetRenderPipelineAssetAt(i))).ToArray();
            try
            {
                foreach(string name in new[]{"BenchmarkBattle","BenchmarkPortrait"})
                {
                    string error=AssetDatabase.MoveAsset(Folder+"/"+name+".asset",MacAnhContentBuilder.Art+"/Resources/MacAnh/"+name+".asset");
                    if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
                    movedResources.Add(name);
                }
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.arena3q.macanhtrial");PlayerSettings.productName="MacAnh LOD Trial";
                PlayerSettings.meshDeformation=MeshDeformation.GPU;EditorUserBuildSettings.buildAppBundle=false;
                var pipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
                GraphicsSettings.defaultRenderPipeline=pipeline;
                for(int i=0;i<oldPipelines.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=pipeline;}
                QualitySettings.SetQualityLevel(oldQuality,false);
                Directory.CreateDirectory("Builds/MacAnhTrial");
                var options=new BuildPlayerOptions{scenes=new[]{Scene,"Assets/Arena3Q/Scenes/Battle.unity"},
                    locationPathName="Builds/MacAnhTrial/MacAnhTrial.apk",target=BuildTarget.Android,options=BuildOptions.Development};
                var report=BuildPipeline.BuildPlayer(options);
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("LOD trial build failed: "+report.summary.result);
                long bytes=new FileInfo(options.locationPathName).Length;
                File.WriteAllText("Logs/MacAnh-lod-android-build.txt","APK="+options.locationPathName+"\nbytes="+bytes+"\nMB="+(bytes/1048576.0).ToString("F1")+"\nseconds="+report.summary.totalTime.TotalSeconds);
                Debug.Log("MACANH_LOD_ANDROID_BUILD_COMPLETE APK "+(bytes/1048576.0).ToString("F1")+" MB");
            }
            finally
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,oldId);PlayerSettings.productName=oldName;
                PlayerSettings.meshDeformation=oldDeformation;EditorUserBuildSettings.buildAppBundle=oldBundle;
                GraphicsSettings.defaultRenderPipeline=string.IsNullOrEmpty(oldDefault)?null:AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(oldDefault);
                for(int i=0;i<oldPipelines.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=string.IsNullOrEmpty(oldPipelines[i])?null:AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(oldPipelines[i]);}
                QualitySettings.SetQualityLevel(oldQuality,false);AssetDatabase.SaveAssets();
                foreach(string name in movedResources)
                {
                    string error=AssetDatabase.MoveAsset(MacAnhContentBuilder.Art+"/Resources/MacAnh/"+name+".asset",Folder+"/"+name+".asset");
                    if(!string.IsNullOrEmpty(error))Debug.LogError(error);
                }
            }
        }
        public static void ParkContent()
        {
            foreach(string name in new[]{"BenchmarkBattle","BenchmarkPortrait"})
            {
                string source=MacAnhContentBuilder.Art+"/Resources/MacAnh/"+name+".asset";
                if(!File.Exists(source))continue;
                string error=AssetDatabase.MoveAsset(source,Folder+"/"+name+".asset");
                if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
            }
            AssetDatabase.SaveAssets();Debug.Log("MACANH_DEVICE_CONTENT_PARKED: no benchmark-only bundle in production Resources.");
        }
    }
}
