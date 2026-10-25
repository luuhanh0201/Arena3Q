using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Arena3Q.Editor
{
    /// <summary>Bakes a distant representation of the approved model; leaves imported geometry untouched.</summary>
    internal static class BattlefieldVegetationLod
    {
        public static GameObject Ensure(GameObject prefab,string art)
        {
            if(prefab.GetComponent<LODGroup>()!=null)return prefab;
            string path=AssetDatabase.GetAssetPath(prefab);
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var renderers=root.GetComponentsInChildren<Renderer>();
                var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                float half=bounds.size.magnitude*.52f;
                string texturePath=art+"/Textures/LOD_"+prefab.name+".png";
                Bake(root,bounds,half,texturePath);
                var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=true;importer.maxTextureSize=512;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
                string matPath=art+"/Materials/LOD_"+prefab.name+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if(material==null){material=new Material(Shader.Find("Arena3Q/Battlefield/Vegetation Billboard")){enableInstancing=true};AssetDatabase.CreateAsset(material,matPath);}
                material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));EditorUtility.SetDirty(material);
                string meshPath=art+"/Meshes/LOD_"+prefab.name+".asset";
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(mesh==null)
                {
                    mesh=new Mesh{name="Billboard_"+prefab.name,vertices=new[]{new Vector3(-half,-half,0),new Vector3(half,-half,0),new Vector3(-half,half,0),new Vector3(half,half,0)},uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one},triangles=new[]{0,2,1,1,2,3}};
                    mesh.RecalculateBounds();mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,meshPath);
                }
                var card=new GameObject("Distant billboard",typeof(MeshFilter),typeof(MeshRenderer));card.transform.SetParent(root.transform,false);card.transform.localPosition=bounds.center;
                card.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=card.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                var group=root.AddComponent<LODGroup>();group.SetLODs(new[]{new LOD(.11f,renderers),new LOD(.012f,new Renderer[]{renderer})});group.RecalculateBounds();
                return PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        private static void Bake(GameObject root,Bounds bounds,float half,string path)
        {
            var scene=EditorSceneManager.NewPreviewScene();var previous=RenderTexture.active;
            bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            var rt=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32);
            try
            {
                var instance=Object.Instantiate(root);SceneManager.MoveGameObjectToScene(instance,scene);
                var cameraObject=new GameObject("LOD bake",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraObject,scene);
                var camera=cameraObject.GetComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=half;camera.aspect=1f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
                camera.transform.position=bounds.center+new Vector3(0f,.7660444f,-.6427876f)*40f;camera.transform.LookAt(bounds.center);camera.nearClipPlane=.1f;camera.farClipPlane=100f;camera.targetTexture=rt;camera.GetUniversalAdditionalCameraData().SetRenderer(1);camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
                var lightObject=new GameObject("LOD sun",typeof(Light));SceneManager.MoveGameObjectToScene(lightObject,scene);var light=lightObject.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.color=new Color(1f,.85f,.66f);light.transform.rotation=Quaternion.Euler(48,-32,0);
                camera.Render();camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(512,512,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,512,512),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            }
            finally{RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);ShaderUtil.allowAsyncCompilation=async;EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
