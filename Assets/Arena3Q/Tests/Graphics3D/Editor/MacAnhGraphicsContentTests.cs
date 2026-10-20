using System;
using System.Linq;
using Arena3Q.Gameplay;
using Arena3Q.Data;
using Arena3Q.Graphics3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Tests.Graphics3D
{
    public sealed class MacAnhGraphicsContentTests
    {
        const string Root="Assets/Arena3Q/Art/Heroes/MacAnh/Graphics3D";
        [Test] public void PortraitPreviewEventsRemainInSecondsWhileBattleClipsStayEventless()
        {
            var portrait=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/LOD/MacAnh_Portrait3D.prefab");
            Assert.That(portrait.GetComponent<Animator>().runtimeAnimatorController,Is.Not.Null);
            Assert.That(portrait.GetComponent<MacAnhAnimationEvents>(),Is.Not.Null);
            var profile=AssetDatabase.LoadAssetAtPath<MacAnhPerformance>("Assets/Arena3Q/Art/Heroes/MacAnh/MacAnhPerformance.asset");
            var native=AssetDatabase.LoadAllAssetsAtPath(Root+"/Source/LOD/MacAnh_LODActions.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
            Assert.That(native.Length,Is.EqualTo(11));
            foreach(var source in native)
            {
                Assert.That(AnimationUtility.GetAnimationEvents(source),Is.Empty,"Battle clock owns gameplay events.");
                var preview=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/LOD/PreviewClips/"+source.name+".anim");
                Assert.That(preview,Is.Not.Null);Assert.That(preview.length,Is.EqualTo(source.length).Within(.00001f));
                var metadata=profile.MetadataClips.First(c=>c.action.ToString()==source.name);
                var events=AnimationUtility.GetAnimationEvents(preview).Where(e=>e.stringParameter!="Prepare"&&e.stringParameter!="Afterimage").ToArray();
                Assert.That(events.Length,Is.EqualTo(metadata.events.Length));
                foreach(var mark in metadata.events)
                {
                    var actual=events.Single(e=>e.stringParameter==mark.key&&e.intParameter==mark.comboIndex);
                    Assert.That(actual.time,Is.EqualTo(mark.Seconds).Within(.00001f),source.name+" "+mark.key);
                }
            }
        }
        [Test] public void BattleLodUsesFourInfluencesAndLeavesSpriteRouteDefault()
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(Root+"/Source/LOD/MacAnh_BattleRig.fbx");
            Assert.That(importer.maxBonesPerVertex,Is.EqualTo(4));Assert.That(importer.minBoneWeight,Is.LessThanOrEqualTo(.001001f));
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/LOD/MacAnh_Battle3D.prefab");
            foreach(var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Assert.That(renderer.quality,Is.EqualTo(SkinQuality.Bone4));
                Assert.That(renderer.sharedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1),Is.True,"Keep UV0 and add normal UV1.");
                Assert.That(renderer.sharedMaterials[0],Is.SameAs(AssetDatabase.LoadAssetAtPath<Material>(Root+"/LOD/BattleBody.mat")),"A renamed FBX material must retain its explicit body remap.");
            }
            var profile=AssetDatabase.LoadAssetAtPath<MacAnhPerformance>("Assets/Arena3Q/Art/Heroes/MacAnh/MacAnhPerformance.asset");
            Assert.That(profile.EnableBattle3DTrial,Is.False);Assert.That(profile.Battle3DViewPrefab,Is.Not.Null);
            Assert.That(profile.MetadataClips.Sum(c=>c.frames.Length),Is.Zero,"Realtime data must not directly retain all sprite atlases.");
            Assert.That(Enum.GetValues(typeof(MacAnhAction)).Cast<MacAnhAction>().Sum(action=>profile.FindSprite(action).frames.Length),Is.EqualTo(3960));
        }
        [Test] public void BothFinalLodsUseTheAuditedFourInfluenceRoute()
        {
            foreach(string label in new[]{"Battle","Portrait"})
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(Root+"/Source/LOD/MacAnh_"+label+"Rig.fbx");
                Assert.That(importer.skinWeights,Is.EqualTo(ModelImporterSkinWeights.Custom));
                Assert.That(importer.maxBonesPerVertex,Is.EqualTo(4));
                // Unity's API clamps this getter to .001 even when its saved
                // setting is zero. Actual imported surfaces are audited too.
                Assert.That(importer.minBoneWeight,Is.LessThanOrEqualTo(.001001f));
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/LOD/MacAnh_"+label+"3D.prefab");
                foreach(var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>())Assert.That(renderer.quality,Is.EqualTo(SkinQuality.Bone4));
            }
        }
        [Test] public void OriginalBodyKeepsTriangleCountAndMetreHeight()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/MacAnh_Character3D.prefab");
            var go=UnityEngine.Object.Instantiate(prefab);
            try
            {
                var body=go.GetComponentInChildren<SkinnedMeshRenderer>();ulong indices=0;
                for(int i=0;i<body.sharedMesh.subMeshCount;i++)indices+=body.sharedMesh.GetIndexCount(i);
                Assert.That(indices/3,Is.EqualTo(2953808));
                // Renderer.bounds is a padded animation culling envelope. Measure
                // the unchanged rest mesh through the FBX's unit/axis transform.
                var local=body.sharedMesh.bounds;var world=new Bounds(body.transform.TransformPoint(local.center),Vector3.zero);
                for(int i=0;i<8;i++) world.Encapsulate(body.transform.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
                Assert.That(world.size.y,Is.EqualTo(1.70f).Within(.001f));
                Assert.That(body.bones.Length,Is.GreaterThan(60));
            }
            finally {UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void EveryNewEffectHasFiniteGeometryAndThreeTiers()
        {
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{Root}))
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if(prefab.GetComponent<MacAnhArcaneEffect>()==null)continue;
                Assert.That(prefab.transform.childCount,Is.EqualTo(3),prefab.name);
                foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                    foreach(var p in filter.sharedMesh.vertices)
                        Assert.That(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z),Is.False,filter.name);
            }
        }
        [Test] public void CycloneHasDepthAndIncreasingBudgets()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/FX_MacAnh_Cyclone3D.prefab");int previous=0;
            for(int q=0;q<3;q++)
            {
                var root=prefab.transform.GetChild(q);int count=root.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.vertexCount);
                Assert.That(count,Is.GreaterThan(previous));previous=count;
                var body=root.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.name.Contains("Bounded"));
                Assert.That(body.sharedMesh.bounds.size.z,Is.GreaterThan(2));
                Assert.That(body.sharedMesh.bounds.max.y,Is.GreaterThan(2.85f),"The ragged crown must fit completely inside the integration cage");
                var vertices=body.sharedMesh.vertices;var triangles=body.sharedMesh.triangles;
                float bottom=vertices.Min(p=>p.y),top=vertices.Max(p=>p.y);bool bottomClosed=false,topClosed=false;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    float a=vertices[triangles[i]].y,b=vertices[triangles[i+1]].y,c=vertices[triangles[i+2]].y;
                    bottomClosed|=Mathf.Abs(a-bottom)<.00001f&&Mathf.Abs(b-bottom)<.00001f&&Mathf.Abs(c-bottom)<.00001f;
                    topClosed|=Mathf.Abs(a-top)<.00001f&&Mathf.Abs(b-top)<.00001f&&Mathf.Abs(c-top)<.00001f;
                }
                Assert.That(bottomClosed&&topClosed,Is.True,"Volume needs both caps for the battle's elevated camera");
                var mat=body.GetComponent<Renderer>().sharedMaterial;
                Assert.That(mat.GetTexture("_VolumeTex"),Is.TypeOf<Texture3D>());
                Assert.That(mat.FindPass("BoundedVolumeForward"),Is.GreaterThanOrEqualTo(0));
            }
            var particle=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/RisingMotesBattle.mat");
            Assert.That(particle.FindPass("Arcane2D"),Is.GreaterThanOrEqualTo(0),"2D battle cannot draw a UniversalForward-only graph");
        }
        [Test] public void NativeAnimationEventsHavePreparationAndExactCycloneRelease()
        {
            var clips=AssetDatabase.LoadAllAssetsAtPath(Root+"/Source/MacAnh_OriginalActions.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
            Assert.That(clips.Length,Is.EqualTo(11));
            var cyclone=clips.Single(c=>c.name=="Skill02");
            Assert.That(cyclone.events.Single(e=>e.stringParameter=="SpawnCyclone").time,Is.EqualTo(1).Within(.00001f));
            Assert.That(cyclone.events.Single(e=>e.stringParameter=="Prepare").time,Is.EqualTo(.5f).Within(.00001f));
            Assert.That(clips.Single(c=>c.name=="Attack02").events.Count(e=>e.stringParameter=="SpawnTripleFeather"),Is.EqualTo(3));
            Assert.That(clips.Single(c=>c.name=="DodgeAttack").events.Count(e=>e.stringParameter=="Afterimage"),Is.EqualTo(3));
        }
        [Test] public void CycloneUsesShortOpenWispsAndIndependentInteriorRunes()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/FX_MacAnh_Cyclone3D.prefab");
            foreach(Transform tier in prefab.transform)
            {
                var filters=tier.GetComponentsInChildren<MeshFilter>(true);
                Assert.That(filters.Length,Is.EqualTo(6),"Disconnected accents share one renderer");
                var wisps=filters.Single(f=>f.name.Contains("Short drifting")).sharedMesh;
                var data=new System.Collections.Generic.List<Vector4>();wisps.GetUVs(1,data);
                Assert.That(data.Count,Is.EqualTo(wisps.vertexCount));
                int bandCount=data.Select(v=>v.x).Distinct().Count();Assert.That(bandCount,Is.GreaterThanOrEqualTo(2));
                var points=wisps.vertices;int bandSize=points.Length/bandCount;
                for(int band=0;band<bandCount;band++)
                {
                    float total=0,previous=Mathf.Atan2(points[band*bandSize].z,points[band*bandSize].x)*Mathf.Rad2Deg;
                    for(int i=3;i<bandSize;i+=3)
                    {var p=points[band*bandSize+i];float angle=Mathf.Atan2(p.z,p.x)*Mathf.Rad2Deg;total+=Mathf.Abs(Mathf.DeltaAngle(previous,angle));previous=angle;}
                    Assert.That(total,Is.InRange(45f,180f),"An accent must not make a complete ring");
                }
                var runes=filters.Single(f=>f.name.Contains("Interior drifting")).sharedMesh;runes.GetUVs(1,data);
                Assert.That(data.Select(v=>v.x).Distinct().Count(),Is.GreaterThanOrEqualTo(8),"GPU orbit seeds must differ between runes");
                var volume=filters.Single(f=>f.name.Contains("Bounded")).GetComponent<Renderer>().sharedMaterial;
                Assert.That(volume.GetFloat("_Steps"),Is.InRange(10,20),"Ray budget must resolve the ragged crown inside its conservative cage");
            }
            foreach(var name in new[]{"CycloneVolume","CycloneWisps"})
                Assert.That(ShaderUtil.ShaderHasError(AssetDatabase.LoadAssetAtPath<Shader>(Root+"/Shaders/"+name+".shader")),Is.False,name);
        }
    }
}
