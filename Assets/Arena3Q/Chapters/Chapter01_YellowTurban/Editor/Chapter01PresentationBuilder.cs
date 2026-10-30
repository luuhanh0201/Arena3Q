using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Arena3Q.Exploration.Editor
{
    public static class Chapter01PresentationBuilder
    {
        [MenuItem("Arena3Q/Chapter 01/Configure Environment Motion")]
        public static void ConfigureEnvironment()
        {
            const string root="Assets/Arena3Q/Chapters/Chapter01_YellowTurban";
            var config=AssetDatabase.LoadAssetAtPath<Arena3Q.Data.ChapterMapConfig>(Chapter01SystemsBuilder.ConfigPath).motion;
            config.battlefieldWind=AssetDatabase.LoadAssetAtPath<Arena3Q.Data.BattlefieldConfig>("Assets/Arena3Q/Content/Battlefield/BattlefieldConfig.asset");
            config.waterSource=AssetDatabase.LoadAssetAtPath<Material>("Assets/ThirdParty/Chapter01/_Map/Materials/CH01_Water.mat");
            config.bambooSource=AssetDatabase.LoadAssetAtPath<Material>("Assets/ThirdParty/Chapter01/_Kit/Materials/M_Bamboo.mat");
            Material Copy(Material source,string shaderName,string name)
            {
                if(source==null)throw new InvalidDataException("Missing source "+name);
                var shader=Shader.Find(shaderName);if(shader==null)throw new InvalidDataException("Missing shader "+shaderName);
                string path=root+"/Data/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
                material.shader=shader;material.CopyPropertiesFromMaterial(source);material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
            }
            config.waterMotionMaterial=Copy(config.waterSource,"Arena3Q/Chapter Water","CH01_WaterMotion");
            config.bambooMotionMaterial=Copy(config.bambooSource,"Arena3Q/Chapter Bamboo","CH01_BambooMotion");
            config.environmentMotion=true;
            EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
            Debug.Log("[Chapter01 P4] wind, water and bamboo configured; source materials/scene unchanged.");
        }
        [Serializable] private sealed class AuditData { public RendererData[] renderers; }
        [Serializable] private sealed class RendererData
        {
            public string path; public string[] materials, shaders, materialPaths;
            public Vector3 center, size; public int vertices;
        }
        public static void Audit()
        {
            EditorSceneManager.OpenScene(Chapter01SystemsBuilder.ScenePath);
            var rows=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Select(r=>new RendererData{path=ChapterMapController.Path(r.transform),center=r.bounds.center,size=r.bounds.size,
                    vertices=r.GetComponent<MeshFilter>()?.sharedMesh?.vertexCount??0,
                    materials=r.sharedMaterials.Select(m=>m==null?"":m.name).ToArray(),
                    shaders=r.sharedMaterials.Select(m=>m==null?"":m.shader.name).ToArray(),
                    materialPaths=r.sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray()}).ToArray();
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Chapter01-P4-renderers.json",JsonUtility.ToJson(new AuditData{renderers=rows},true));
            Debug.Log("[Chapter01 P4 audit] renderers="+rows.Length);
        }
    }
}
