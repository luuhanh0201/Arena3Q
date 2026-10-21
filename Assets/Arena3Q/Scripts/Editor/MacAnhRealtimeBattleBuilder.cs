using System;
using System.IO;
using System.Linq;
using Arena3Q.Data;
using Arena3Q.Graphics3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Arena3Q.Editor
{
    public static class MacAnhRealtimeBattleBuilder
    {
        private const string Root=MacAnhGraphicsBuilder.Root;
        private const string Source=Root+"/Source/LOD";
        private const string Output=Root+"/LOD";
        [Serializable] private class InnerMaterial {public string name;public float[] colour;public float roughness;}
        [Serializable] private class ExportInfo {public bool atlas,dual_uv;public string body_material,cap_material;public string[] lining_materials;public InnerMaterial[] inner_materials;}
        private static T Save<T>(T value,string path)where T:UnityEngine.Object
        {
            var previous=AssetDatabase.LoadAssetAtPath<T>(path);
            if(previous==null){AssetDatabase.CreateAsset(value,path);return value;}
            EditorUtility.CopySerialized(value,previous);UnityEngine.Object.DestroyImmediate(value);return previous;
        }
        private static int AddForwardRenderer()
        {
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Output+"/BattleForwardRenderer.asset");
            if(renderer==null)
            {renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,Output+"/BattleForwardRenderer.asset");}
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
            var serialized=new SerializedObject(pipeline);var list=serialized.FindProperty("m_RendererDataList");
            for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==renderer)return i;
            int index=list.arraySize;list.arraySize++;
            list.GetArrayElementAtIndex(index).objectReferenceValue=renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);return index;
        }
        private static Texture2D CopyMap(Texture texture,string label,string kind,int size)
        {
            if(texture==null)return null;
            string original=AssetDatabase.GetAssetPath(texture);
            string destination=Source+"/"+label+"_"+kind+Path.GetExtension(original);
            if(!File.Exists(destination))File.Copy(original,destination);
            AssetDatabase.ImportAsset(destination,ImportAssetOptions.ForceSynchronousImport);
            ConfigureMap(destination,size,kind=="Normal",kind=="BaseColor");
            return AssetDatabase.LoadAssetAtPath<Texture2D>(destination);
        }
        private static void ConfigureMap(string path,int size,bool normal,bool srgb)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            string before=EditorJsonUtility.ToJson(importer);
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.convertToNormalmap=false;importer.sRGBTexture=srgb;importer.maxTextureSize=size;
            importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Standalone",overridden=true,
                maxTextureSize=size,format=TextureImporterFormat.DXT5,compressionQuality=100});
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,
                maxTextureSize=size,format=TextureImporterFormat.ASTC_6x6,compressionQuality=100});
            if(before!=EditorJsonUtility.ToJson(importer))importer.SaveAndReimport();
        }
        private static GameObject BuildCharacter(string label)
        {
            string path=Source+"/MacAnh_"+label+"Rig.fbx";
            var importer=AssetImporter.GetAtPath(path)as ModelImporter;
            if(importer==null)throw new InvalidOperationException("Export the "+label+" LOD before building it.");
            importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation=false;importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=4;importer.minBoneWeight=0;
            var original=(ModelImporter)AssetImporter.GetAtPath(Root+"/Source/MacAnh_OriginalRig.fbx");
            var info=JsonUtility.FromJson<ExportInfo>(File.ReadAllText(Source+"/Export_"+label+".json"));
            foreach(var entry in original.GetExternalObjectMap())
            {
                if(entry.Key.type!=typeof(Material))continue;
                var material=entry.Value as Material;if(material==null)continue;
                if(material.HasProperty("_UseORM")&&material.GetFloat("_UseORM")>.5f)
                {
                    var copy=new Material(material){name=label+"Body"};int size=label=="Battle"?2048:4096;
                    if(info.atlas&&!info.dual_uv)
                    {
                        foreach(string kind in new[]{"BaseColor","Normal","ORM"})
                        {
                            string map=Source+"/MacAnh_"+label+"_Atlas"+kind+".png";
                            ConfigureMap(map,size,kind=="Normal",kind=="BaseColor");
                            copy.SetTexture(kind=="BaseColor"?"_BaseMap":kind=="Normal"?"_BumpMap":"_ORMMap",AssetDatabase.LoadAssetAtPath<Texture2D>(map));
                        }
                    }
                    else
                    {
                        copy.SetTexture("_BaseMap",CopyMap(material.GetTexture("_BaseMap"),label,"BaseColor",size));
                        copy.SetTexture("_ORMMap",CopyMap(material.GetTexture("_ORMMap"),label,"ORM",size));
                    }
                    string normal=Source+"/MacAnh_"+label+"_Normal_4096.png";
                    if(info.dual_uv)
                    {
                        normal=Source+"/MacAnh_"+label+"_AtlasNormal.png";copy.SetFloat("_NormalUV1",1);copy.SetFloat("_Cull",0);
                    }
                    if(!info.atlas||info.dual_uv)
                    {
                        ConfigureMap(normal,size,true,false);copy.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
                    }
                    material=Save(copy,Output+"/"+label+"Body.mat");
                    if(info.atlas)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),info.body_material),material);
                }
                importer.AddRemap(entry.Key,material);
            }
            if(info.lining_materials!=null && info.lining_materials.Length>0)
            {
                var lining=new Material(Shader.Find("Arena3Q/MacAnh/Original PBR"));
                lining.SetColor("_BaseColor",new Color(.025f,.012f,.045f));lining.SetFloat("_Smoothness",.1f);lining.SetFloat("_Cull",0);
                lining=Save(lining,Output+"/"+label+"Lining.mat");
                foreach(string name in info.lining_materials)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),lining);
            }
            if(!string.IsNullOrEmpty(info.cap_material))
            {
                var cap=new Material(Shader.Find("Arena3Q/MacAnh/Original PBR"));int size=label=="Battle"?2048:4096;
                cap.SetFloat("_Cull",0);cap.SetFloat("_BaseUV1",1);cap.SetFloat("_NormalUV1",1);cap.SetFloat("_Smoothness",.1f);
                foreach(string kind in new[]{"BaseColor","Normal"})
                {
                    string map=Source+"/MacAnh_"+label+"_Atlas"+kind+".png";ConfigureMap(map,size,kind=="Normal",kind=="BaseColor");
                    cap.SetTexture(kind=="BaseColor"?"_BaseMap":"_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(map));
                }
                cap=Save(cap,Output+"/"+label+"InnerFabric.mat");
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),info.cap_material),cap);
            }
            // Texture imports can refresh the model importer. Reacquire it and
            // set the final source-name remaps only after every texture import.
            importer=(ModelImporter)AssetImporter.GetAtPath(path);
            var expectedBody=AssetDatabase.LoadAssetAtPath<Material>(Output+"/"+label+"Body.mat");
            if(info.atlas)
            {
                if(expectedBody==null||string.IsNullOrEmpty(info.body_material))throw new InvalidOperationException("Missing explicit LOD body material.");
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),info.body_material),expectedBody);
            }
            if(!string.IsNullOrEmpty(info.cap_material))
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),info.cap_material),AssetDatabase.LoadAssetAtPath<Material>(Output+"/"+label+"InnerFabric.mat"));
            foreach(var inner in info.inner_materials??Array.Empty<InnerMaterial>())
            {
                var material=new Material(Shader.Find("Arena3Q/MacAnh/Original PBR"));material.SetFloat("_Cull",0);
                material.SetColor("_BaseColor",new Color(inner.colour[0],inner.colour[1],inner.colour[2],inner.colour[3]));
                material.SetFloat("_Smoothness",1-inner.roughness);material=Save(material,Output+"/"+inner.name+".mat");
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),inner.name),material);
            }
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            go.name="MacAnh_"+label+"3D";go.transform.localRotation=Quaternion.Euler(0,180,0);
            if(label=="Portrait")
            {
                var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Output+"/MacAnh_LODPreview.controller");
                if(controller!=null){go.GetComponent<Animator>().runtimeAnimatorController=controller;go.AddComponent<MacAnhAnimationEvents>();}
            }
            foreach(var renderer in go.GetComponentsInChildren<SkinnedMeshRenderer>())renderer.quality=SkinQuality.Bone4;
            var body=go.GetComponentInChildren<SkinnedMeshRenderer>();
            if(info.atlas&&body.sharedMaterials[0]!=expectedBody)throw new InvalidOperationException("LOD body material remap was not applied: "+label);
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())
                foreach(var material in renderer.sharedMaterials)
                    if(material==null||material.shader.name!="Arena3Q/MacAnh/Original PBR")throw new InvalidOperationException("Unmapped LOD material on "+renderer.name);
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,Output+"/"+go.name+".prefab");UnityEngine.Object.DestroyImmediate(go);return prefab;
        }
        private static Vector2 CalibrateFacing(GameObject prefab,AnimationClip idle,MacAnhPerformance profile)
        {
            var placement=new GameObject("Facing calibration");var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            model.transform.SetParent(placement.transform,false);
            var cameraGo=new GameObject("Projection calibration",typeof(Camera));var camera=cameraGo.GetComponent<Camera>();
            camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1.25f;
            var rotation=Quaternion.Euler(Mathf.Asin(.75f)*Mathf.Rad2Deg,0,0);
            camera.transform.SetPositionAndRotation(Vector3.up*.8f+rotation*new Vector3(0,0,-4),rotation);
            try
            {
                idle.SampleAnimation(model,0);
                var tip=model.GetComponentsInChildren<Transform>().First(t=>t.name=="Socket_Fan_Tip");
                var hand=model.GetComponentsInChildren<Transform>().First(t=>t.name=="Socket_Hand_L");
                var reference=profile.Find(MacAnhAction.Idle);float best=float.PositiveInfinity;Vector2 result=Vector2.zero;
                foreach(float sign in new[]{-1f,1f})for(int offset=0;offset<360;offset+=45)
                {
                    float error=0;
                    for(int direction=0;direction<8;direction++)
                    {
                        placement.transform.rotation=Quaternion.Euler(0,offset+sign*direction*45,0);
                        var origin=camera.WorldToViewportPoint(placement.transform.position);
                        var a=(Vector2)(camera.WorldToViewportPoint(tip.position)-origin)*2.5f;
                        var b=(Vector2)(camera.WorldToViewportPoint(hand.position)-origin)*2.5f;
                        var expected=reference.sockets[reference.Index(direction,0)];
                        var da=a-expected.fanTip;var db=b-expected.leftHand;
                        error+=da.x*da.x+db.x*db.x+.2f*(da.y*da.y+db.y*db.y);
                    }
                    if(error<best){best=error;result=new Vector2(offset,sign);}
                }
                Debug.Log("[MacAnh facing] calibrated offset="+result.x+" sign="+result.y+" residual="+best+" (old sprite uses a different elevation).");
                return result;
            }
            finally{UnityEngine.Object.DestroyImmediate(placement);UnityEngine.Object.DestroyImmediate(cameraGo);}
        }
        public static void BuildSpriteLibrary(MacAnhPerformance profile)
        {
            const string key="MacAnh/MacAnhSpriteLibrary";
            string folder=MacAnhContentBuilder.Art+"/Resources/MacAnh";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var values=profile.MetadataClips.Select(item=>item.frames!=null&&item.frames.Length>0?item:profile.FindSprite(item.action))
                .Select(item=>new MacAnhPerformance.Clip{action=item.action,duration=item.duration,loop=item.loop,
                    framesPerDirection=item.framesPerDirection,frames=(Sprite[])item.frames.Clone(),
                    sockets=(MacAnhPerformance.SocketFrame[])item.sockets.Clone(),events=item.events.Select(mark=>new MacAnhPerformance.EventMark
                        {key=mark.key,frame=mark.frame,comboIndex=mark.comboIndex}).ToArray()}).ToArray();
            if(values.Any(item=>item.frames.Length==0))throw new InvalidOperationException("Sprite library migration requires every action.");
            string path=folder+"/MacAnhSpriteLibrary.asset";
            var library=AssetDatabase.LoadAssetAtPath<MacAnhSpriteLibrary>(path);
            if(library==null){library=ScriptableObject.CreateInstance<MacAnhSpriteLibrary>();AssetDatabase.CreateAsset(library,path);}
            library.Configure(values);EditorUtility.SetDirty(library);
            var metadata=values.Select(item=>new MacAnhPerformance.Clip{action=item.action,duration=item.duration,loop=item.loop,
                framesPerDirection=item.framesPerDirection,frames=Array.Empty<Sprite>(),sockets=(MacAnhPerformance.SocketFrame[])item.sockets.Clone(),
                events=item.events.Select(mark=>new MacAnhPerformance.EventMark{key=mark.key,frame=mark.frame,comboIndex=mark.comboIndex}).ToArray()}).ToArray();
            profile.ConfigureSpriteStorage(key,metadata);EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
        }
        public static void ImportFixedSpriteCaptures()
        {
            foreach(MacAnhAction action in Enum.GetValues(typeof(MacAnhAction)))MacAnhContentBuilder.ImportAction(action.ToString());
            MacAnhContentBuilder.UpdatePresentationMaterial();MacAnhBattleFxBuilder.Bind();
            var profile=AssetDatabase.LoadAssetAtPath<MacAnhPerformance>(MacAnhContentBuilder.Art+"/MacAnhPerformance.asset");
            var idle=profile.FindSprite(MacAnhAction.Idle);var portrait=idle.frames[6*idle.framesPerDirection];
            foreach(string path in new[]{"Assets/Arena3Q/Content/Lords/Lord_Strategist.asset","Assets/Arena3Q/Content/Lords/Lord_MacAnhPreview.asset"})
            {
                var hero=AssetDatabase.LoadAssetAtPath<HeroDefinition>(path);
                if(hero==null||hero.MacAnhPerformance!=profile)continue;
                var data=new SerializedObject(hero);data.FindProperty("sprite").objectReferenceValue=portrait;data.ApplyModifiedPropertiesWithoutUndo();
            }
            string viewPath=MacAnhContentBuilder.Art+"/Resources/MacAnh/MacAnhRealtimeBattleView.prefab";
            var go=PrefabUtility.LoadPrefabContents(viewPath);
            try
            {
                var character=AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/MacAnh_Battle3D.prefab");
                var clip=AssetDatabase.LoadAllAssetsAtPath(Source+"/MacAnh_LODActions.fbx").OfType<AnimationClip>().First(c=>c.name=="Idle");
                var facing=CalibrateFacing(character,clip,profile);go.GetComponent<MacAnhRealtimeBattleView>().ConfigureFacing(facing.x,facing.y);
                PrefabUtility.SaveAsPrefabAsset(go,viewPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(go);}
            if(profile.EnableBattle3DTrial)throw new InvalidOperationException("Default route unexpectedly enabled.");
            AssetDatabase.SaveAssets();Debug.Log("MACANH_FIXED_SPRITES_IMPORTED: 11 clips, bindings retained, facing recalibrated.");
        }
        [MenuItem("Arena3Q/Mac Anh/Graphics/Build Realtime Battle Trial (default off)")]
        public static void Build()
        {
            Directory.CreateDirectory(Output);AssetDatabase.Refresh();
            var performance=AssetDatabase.LoadAssetAtPath<MacAnhPerformance>(MacAnhContentBuilder.Art+"/MacAnhPerformance.asset");
            BuildSpriteLibrary(performance);
            var character=BuildCharacter("Battle");
            if(File.Exists(Source+"/MacAnh_PortraitRig.fbx"))BuildCharacter("Portrait");
            string actionsPath=Source+"/MacAnh_LODActions.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(actionsPath);
            importer.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar=AssetDatabase.LoadAllAssetsAtPath(Source+"/MacAnh_BattleRig.fbx").OfType<Avatar>().First();
            importer.importAnimation=true;importer.animationCompression=ModelImporterAnimationCompression.Off;
            var settings=importer.defaultClipAnimations;
            foreach(var clip in settings)
            {
                int index=clip.name.LastIndexOf("MacAnh_",StringComparison.Ordinal);
                if(index>=0)clip.name=clip.name.Substring(index+7);
                clip.loopTime=clip.name=="Idle"||clip.name=="Walk"||clip.name=="Run";
                // Playback samples the same gameplay clock; events stay in the
                // performance data and must not fire again from this view.
                clip.events=Array.Empty<AnimationEvent>();
            }
            importer.clipAnimations=settings;importer.SaveAndReimport();
            var animations=AssetDatabase.LoadAllAssetsAtPath(actionsPath).OfType<AnimationClip>()
                .Where(c=>!c.name.StartsWith("__")).Select(c=>new MacAnhRealtimeBattleView.ActionClip
                {action=(MacAnhAction)Enum.Parse(typeof(MacAnhAction),c.name),clip=c}).ToArray();
            if(animations.Length!=11)throw new InvalidOperationException("Expected eleven shared action clips.");
            var shader=Shader.Find("Arena3Q/MacAnh/Realtime Composite");
            if(shader==null)throw new InvalidOperationException("Missing realtime composite shader.");
            var material=Save(new Material(shader),Output+"/RealtimeComposite.mat");
            int rendererIndex=AddForwardRenderer();
            var go=new GameObject("MacAnh_RealtimeBattleView");
            var view=go.AddComponent<MacAnhRealtimeBattleView>();view.Configure(character,animations,material,rendererIndex);
            var afterimage=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/AfterimageSurface.mat");
            if(afterimage==null)throw new InvalidOperationException("Missing model afterimage surface material (do not use the sprite atlas material).");
            view.ConfigureAfterimage(afterimage);
            var facing=CalibrateFacing(character,animations.First(c=>c.action==MacAnhAction.Idle).clip,
                AssetDatabase.LoadAssetAtPath<MacAnhPerformance>("Assets/Arena3Q/Art/Heroes/MacAnh/MacAnhPerformance.asset"));
            view.ConfigureFacing(facing.x,facing.y);
            const string viewResource="MacAnh/MacAnhRealtimeBattleView";
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,MacAnhContentBuilder.Art+"/Resources/"+viewResource+".prefab");UnityEngine.Object.DestroyImmediate(go);
            var profile=AssetDatabase.LoadAssetAtPath<MacAnhPerformance>("Assets/Arena3Q/Art/Heroes/MacAnh/MacAnhPerformance.asset");
            var serialized=new SerializedObject(profile);serialized.FindProperty("battle3DViewPrefab").objectReferenceValue=null;
            serialized.FindProperty("battle3DViewResource").stringValue=viewResource;
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
            MacAnhImportedSurfaceQa.Capture("Battle");
            if(File.Exists(Source+"/MacAnh_PortraitRig.fbx"))MacAnhImportedSurfaceQa.Capture("Portrait");
            Debug.Log("[MacAnh realtime trial] Built Forward capture + sprite-sort composite. Toggle preserved: "+profile.EnableBattle3DTrial+". Working mesh still requires visual/surface acceptance.");
        }
    }
}
