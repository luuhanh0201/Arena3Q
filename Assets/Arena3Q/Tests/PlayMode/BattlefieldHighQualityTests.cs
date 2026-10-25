#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using NUnit.Framework;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Arena3Q.Tests.PlayMode
{
    public sealed class BattlefieldHighQualityTests
    {
        private const string Scene="Assets/Arena3Q/Scenes/Battlefield_HighQuality.unity";
        private const string Evidence="NghiemThu/Battlefield/XemTruoc_ChinhSua_20261025";
        private BattleController _battle;
        private BattlefieldEnvironment _environment;
        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;yield return null;
            _battle=Object.FindAnyObjectByType<BattleController>();_environment=Object.FindAnyObjectByType<BattlefieldEnvironment>();
            Assert.That(_battle.State,Is.Not.Null);_battle.AutoSimulate=false;
            Assert.That(_battle.Stage.StageId,Does.StartWith("battlefield_"));
            _environment.battleCamera.GetComponent<BattleCameraController>().enabled=false;
            Overview();
        }
        private void Overview()
        {
            var camera=_environment.battleCamera;camera.aspect=1600f/900f;
            camera.orthographicSize=28f;BattleView3D.PlaceCamera(camera.transform,_battle,Vector2.zero);
        }
        [UnityTest,Timeout(180000)]
        public IEnumerator FiveHeroes_MoveOnBothRoads_AndBattlePresentationRestoresCoordinates()
        {
            Assert.That(_battle.Cards.Count,Is.EqualTo(5));
            for(int card=0;card<5;card++)Assert.That(_battle.TrySummon(SummonRequest.Quick(card)).Accepted,Is.True,"card "+card);
            Assert.That(_battle.Registry.Count(u=>u is HeroUnit),Is.EqualTo(5));
            _battle.TrySelectCard(1);var hero=_battle.SelectedHero;
            Assert.That(_battle.MoveSelectedHero(new Vector2(-15f,17f)).Accepted,Is.True);
            for(int i=0;i<240;i++){_battle.Simulate(.05f);Assert.That(_battle.Layout.IsBlocked(hero.Position,.05f),Is.False,"crossed static obstacle at "+hero.Position);if(i%40==0)yield return null;}
            Assert.That(Vector2.Distance(hero.Position,new Vector2(-15f,17f)),Is.LessThan(1f));
            Assert.That(_battle.MoveSelectedHero(new Vector2(15f,17f)).Accepted,Is.True);
            for(int i=0;i<300;i++){_battle.Simulate(.05f);Assert.That(_battle.Layout.IsBlocked(hero.Position,.05f),Is.False);if(i%40==0)yield return null;}
            Assert.That(hero.Position.x,Is.GreaterThan(12f));
            var camera=_environment.battleCamera;Overview();var before=camera.transform.position;
            var controller=camera.GetComponent<BattleCameraController>();var point=new Vector2(-8,4);
            Assert.That(Vector2.Distance(controller.ScreenToWorld(controller.WorldToScreen(point)),point),Is.LessThan(.01f));
            Capture("play-five-heroes.png");
            Assert.That(Vector3.Distance(camera.transform.position,before),Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(_battle.transform.rotation,Quaternion.identity),Is.LessThan(.01f));
            _battle.StartBattle(1);yield return null;
            Assert.That(_battle.Stage.StageType,Is.EqualTo(StageType.Defense));
            for(int i=0;i<200;i++)_battle.Simulate(.05f);
            Assert.That(_battle.Registry.Count(u=>u is EnemyUnit),Is.GreaterThan(0),"defense waves spawn");
            Overview();Capture("play-defense.png");
        }
        [UnityTest,Timeout(180000)]
        public IEnumerator Layout_UsesOriginalAssetsAndHasClearRoadsAndRealObstacles()
        {
            var errors=new List<string>();foreach(var stage in _battle.Stages)stage.Validate(errors);Assert.That(errors,Is.Empty);
            var config=_environment.config;var centre=_environment.transform.Find("BattleCenter");
            Assert.That(centre.Find("ExistingBaguaAltar").GetComponentInChildren<MeshFilter>().sharedMesh.name,Is.Not.Empty);
            Assert.That(centre.Find("ExistingOctagonalPillarsAndRunes").childCount,Is.EqualTo(16));
            foreach(var stage in _battle.Stages)
            {
                var layout=stage.CreateLayout();
                foreach(var pillar in stage.Formation.CreateGeometry().Pillars)Assert.That(layout.IsBlocked(pillar.position,.2f),Is.True);
                foreach(float side in new[]{-1f,1f})
                {
                    var route=new[]{new Vector2(-24,0),new Vector2(-18,side*15),new Vector2(0,side*17),new Vector2(18,side*15),new Vector2(24,0)};
                    for(int i=1;i<route.Length;i++)Assert.That(layout.HasPath(route[i-1],route[i],.45f),Is.True,stage.name+" road "+side+" segment "+i);
                }
                foreach(var p in stage.QuickSummonPoints)Assert.That(layout.IsBlocked(p,.45f),Is.False);
                foreach(var rect in stage.ForbiddenZones)Assert.That(layout.IsBlocked(rect.center,.1f),Is.True);
            }
            var blue=_environment.transform.Find("Camps/BlueCamp");var red=_environment.transform.Find("Camps/RedCamp");Assert.That(blue.childCount,Is.EqualTo(red.childCount));
            for(int i=0;i<blue.childCount;i++){var a=blue.GetChild(i);var b=red.GetChild(i);Assert.That(Vector2.Distance(new Vector2(a.position.x,a.position.z),-new Vector2(b.position.x,b.position.z)),Is.LessThan(.01f),a.name);}
            Assert.That(_environment.transform.Find("Environment/Terrain").GetComponentsInChildren<MeshFilter>().Length,Is.EqualTo(4));
            foreach(var trunk in _environment.transform.Find("Environment/Vegetation/Trees").GetComponentsInChildren<CapsuleCollider>())
                Assert.That(_battle.Layout.IsBlocked(new Vector2(trunk.transform.position.x,trunk.transform.position.z),.05f),Is.True,"tree trunk belongs to navigation");
            // Runtime static batching replaces filters with a non-readable combined mesh; inspect the editable sources.
            var terrain=Enumerable.Range(0,4).Select(i=>AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Arena3Q/Art/Battlefield/Meshes/Terrain_"+i+".asset")).ToArray();
            Assert.That(terrain.Length,Is.EqualTo(4));
            foreach(var mesh in terrain)
                foreach(var vertex in mesh.vertices)
                { Assert.That(Mathf.Abs(vertex.x),Is.LessThanOrEqualTo(config.size.x*.5f+.001f)); Assert.That(Mathf.Abs(vertex.z),Is.LessThanOrEqualTo(config.size.y*.5f+.001f)); }
            Assert.That(terrain.SelectMany(m=>m.colors).Count(c=>c.r>.8f),Is.GreaterThan(100),"visible road mask");Assert.That(terrain.SelectMany(m=>m.colors).Count(c=>c.a>.6f),Is.GreaterThan(100),"grass blending");
            foreach(var mesh in terrain)Assert.That(new FileInfo(AssetDatabase.GetAssetPath(mesh)).Length,Is.LessThan(1000000),"small editable tile");
            foreach(var material in _environment.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct())
            {Assert.That(material,Is.Not.Null);Assert.That(ShaderUtil.GetShaderMessages(material.shader).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),Is.Empty,material.name);}
            yield return null;
        }
        [UnityTest,Timeout(180000)]
        public IEnumerator QualityAndWind_ChangeRenderingWithoutChangingWalkability()
        {
            var quality=_environment.GetComponent<BattlefieldQuality>();var obstacles=_battle.Stage.ForbiddenZones.ToArray();
            var instancing=_environment.GetComponent<BattlefieldInstancing>();Assert.That(instancing,Is.Not.Null);Assert.That(instancing.SubmittedDraws,Is.GreaterThan(0));Assert.That(instancing.SubmittedDraws,Is.LessThan(180));
            var groups=_environment.GetComponentsInChildren<LODGroup>();Assert.That(groups.Length,Is.GreaterThan(30));
            foreach(var group in groups)Assert.That(group.GetLODs()[1].renderers[0].GetComponent<MeshFilter>().sharedMesh.triangles.Length,Is.EqualTo(6));
            foreach(var preset in new[]{BattlefieldGraphics.Low,BattlefieldGraphics.Medium,BattlefieldGraphics.High})
            {
                _environment.quality=preset;quality.Apply();yield return null;yield return null;Overview();
                Assert.That(_battle.Stage.ForbiddenZones,Is.EqualTo(obstacles));
                Capture("quality-"+preset.ToString().ToLowerInvariant()+".png");
                for(int i=0;i<_environment.grassRoot.childCount;i+=2)Assert.That(_environment.grassRoot.GetChild(i).gameObject.activeSelf,Is.EqualTo(_environment.grassRoot.GetChild(i+1).gameObject.activeSelf),"paired grass density");
            }
            var before=Shader.GetGlobalFloat("_ArenaWindTime");yield return null;yield return null;
            Assert.That(Shader.GetGlobalFloat("_ArenaWindTime"),Is.GreaterThan(before));
            Shader.SetGlobalFloat("_ArenaWindTime",1f);var a=Capture("wind-a.png");
            Shader.SetGlobalFloat("_ArenaWindTime",4.3f);var b=Capture("wind-b.png");
            int changed=0;for(int i=0;i<a.Length;i++)if(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>12)changed++;
            File.WriteAllText(Evidence+"/wind-pixel-check.txt","Changed pixels between wind phases: "+changed);Assert.That(changed,Is.GreaterThan(100),"actual wind vertex motion in rendered frames");
            foreach(var particle in _environment.particles){Assert.That(particle.main.maxParticles,Is.LessThanOrEqualTo(30));Assert.That(particle.colorOverLifetime.enabled,Is.True);particle.Simulate(4f,true,true,true);particle.Play();}
            Assert.That(_environment.particles.Sum(p=>p.particleCount),Is.GreaterThan(0));
            Overview();Capture("play-environment-vfx.png");
            var camera=_environment.battleCamera;camera.orthographicSize=12f;BattleView3D.PlaceCamera(camera.transform,_battle,new Vector2(-27f,10f));
            for(int frame=0;frame<36;frame++)
            {
                Shader.SetGlobalFloat("_ArenaWindTime",frame/12f);
                foreach(var particle in _environment.particles)particle.Simulate(1f/12f,true,false,true);
                Capture("motion-"+frame.ToString("D3")+".png");
            }
        }
        [UnityTest,Timeout(180000)]
        public IEnumerator FullBattle_ReportsActualEditorRenderingCountersAndMemory()
        {
            for(int card=0;card<5;card++)_battle.TrySummon(SummonRequest.Quick(card));
            for(int i=0;i<40;i++)_battle.Simulate(.05f);
            using var calls=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");
            using var triangles=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count");
            using var batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count");
            using var passes=ProfilerRecorder.StartNew(ProfilerCategory.Render,"SetPass Calls Count");
            using var gpu=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Render Textures Bytes");
            var target=new RenderTexture(1600,900,24);var previous=_environment.battleCamera.targetTexture;_environment.battleCamera.targetTexture=target;
            try
            {
              foreach(var preset in new[]{BattlefieldGraphics.Low,BattlefieldGraphics.Medium,BattlefieldGraphics.High})
              {
                _environment.quality=preset;_environment.GetComponent<BattlefieldQuality>().Apply();
                for(int i=0;i<8;i++)yield return null;
                Capture("battle-"+preset.ToString().ToLowerInvariant()+".png");
                for(int i=0;i<3;i++)yield return null;
                long visibleTriangles=0;int visibleSubmeshes=0;var countedStatic=new HashSet<Mesh>();
                foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if(!renderer.enabled||!renderer.gameObject.activeInHierarchy||!renderer.isVisible)continue;
                    Mesh mesh=null;
                    if(renderer is SkinnedMeshRenderer skin)mesh=skin.sharedMesh;
                    else if(renderer is MeshRenderer){var filter=renderer.GetComponent<MeshFilter>();if(filter!=null)mesh=filter.sharedMesh;}
                    if(mesh!=null&&renderer.isPartOfStaticBatch&&!countedStatic.Add(mesh))continue;
                    if(mesh==null)continue;visibleSubmeshes+=mesh.subMeshCount;for(int sub=0;sub<mesh.subMeshCount;sub++)visibleTriangles+=mesh.GetIndexCount(sub)/3;
                }
                var usedTextures=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().SelectMany(m=>m.GetTexturePropertyNames().Select(p=>m.GetTexture(p))).Where(t=>t!=null).Distinct().ToArray();
                long textureBytes=usedTextures.Sum(t=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t));
                var instancing=_environment.GetComponent<BattlefieldInstancing>();
                string report=$"Editor only; not device FPS. Preset={preset}; profilerDrawCalls={Last(calls)}; profilerTriangles={Last(triangles)}; profilerBatches={Last(batches)}; profilerSetPass={Last(passes)}; UnityStatsDrawCalls={UnityStats.drawCalls}; UnityStatsTriangles={UnityStats.triangles}; renderTextureBytes={Last(gpu)}; totalAllocatedBytes={UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()}; materialTextureMemoryBytesEditor={textureBytes}; textureCount={usedTextures.Length}; visibleMeshTrianglesEstimate={visibleTriangles}; visibleSubmeshesEstimate={visibleSubmeshes}; instancedSubmissions={instancing.SubmittedDraws}; instancedTriangles={instancing.SubmittedTriangles}. Zero counters in headless Editor mean unavailable, never zero GPU workload.\n"+string.Join("\n",usedTextures.OrderByDescending(t=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)).Take(8).Select(t=>$"{t.name}: {t.width}x{t.height}, bytes={UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)}"));
                Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/render-"+preset+".txt",report);TestContext.WriteLine(report);
              }
            }
            finally{_environment.battleCamera.targetTexture=previous;target.Release();Object.DestroyImmediate(target);}
        }
        private static long Last(ProfilerRecorder r)=>r.Valid&&r.Count>0?r.LastValue:-1;
        [UnityTest]
        public IEnumerator PhoneTestButtons_SwitchPresetAndStageWithoutLosingTheMap()
        {
            var controls=Object.FindAnyObjectByType<Arena3Q.UI.BattlefieldTestControls>();Assert.That(controls,Is.Not.Null);Assert.That(controls.GraphicsButton,Is.Not.Null);
            for(int i=0;i<3;i++)
            {
                var previous=_environment.quality;controls.GraphicsButton.onClick.Invoke();Assert.That(_environment.quality,Is.EqualTo((BattlefieldGraphics)(((int)previous+1)%3)));Assert.That(Application.targetFrameRate,Is.GreaterThanOrEqualTo(RuntimePerformanceConfig.MinimumFrameRate));yield return null;
            }
            var stage=_battle.Stage;controls.StageButton.onClick.Invoke();yield return null;Assert.That(_battle.Stage,Is.Not.EqualTo(stage));Assert.That(_environment.gameObject.activeInHierarchy,Is.True);
            controls.StageButton.onClick.Invoke();yield return null;Assert.That(_battle.Stage,Is.EqualTo(stage));
        }
        [UnityTest,Timeout(180000)]
        public IEnumerator ForegroundTree_FadesGraduallyAndRestoresAfterHeroMoves()
        {
            Assert.That(_battle.TrySummon(SummonRequest.Quick(0)).Accepted,Is.True);var hero=_battle.SelectedHero;
            LODGroup selected=null;Vector2 spot=Vector2.zero;
            foreach(var group in _environment.transform.Find("Environment/Vegetation/Trees").GetComponentsInChildren<LODGroup>())
            {
                var members=group.GetLODs()[0].renderers;var bounds=members[0].bounds;foreach(var member in members)bounds.Encapsulate(member.bounds);
                if(bounds.center.z>-_environment.config.size.y*.5f+5f)continue;
                var p=new Vector2(bounds.center.x,bounds.center.z+(bounds.center.y-1f)/Mathf.Tan(_environment.config.cameraElevation*Mathf.Deg2Rad));
                if(_battle.Layout.IsWalkable(p)&&!_battle.Layout.IsBlocked(p,.45f)){selected=group;spot=p;break;}
            }
            Assert.That(selected,Is.Not.Null,"an actual foreground tree beside valid walkable ground");hero.SetPosition(spot);
            var camera=_environment.battleCamera;camera.orthographicSize=8f;BattleView3D.PlaceCamera(camera.transform,_battle,spot);
            var instancing=_environment.GetComponent<BattlefieldInstancing>();instancing.enabled=false;yield return null;Capture("occlusion-before.png");
            instancing.enabled=true;yield return new WaitForSecondsRealtime(.4f);
            Assert.That(instancing.TreeVisibility(selected),Is.LessThan(.4f));Capture("occlusion-faded.png");
            hero.SetPosition(Vector2.zero);yield return new WaitForSecondsRealtime(.4f);
            Assert.That(instancing.TreeVisibility(selected),Is.GreaterThan(.95f),"tree fades back when the line of sight clears");
        }
        [UnityTest]
        public IEnumerator Feedback_HeroesAreLarger_PlayerIsRed_CastlesChangeToRuins()
        {
            var source=AssetDatabase.LoadAssetAtPath<BattleViewConfig>("Assets/Arena3Q/Content/Resources/BattleView.asset");
            var originals=AssetDatabase.LoadAssetAtPath<MapProps3DLibrary>("Assets/Arena3Q/Content/Maps/MapProps3D.asset");
            var intact=originals.Props.First(p=>p.name=="ThreeKingdomsCommand_1").mesh;
            var ruined=originals.Props.First(p=>p.name=="ThreeKingdomsCommand_2").mesh;
            Assert.That(_environment.transform.Find("Camps/RedCamp").GetChild(0).position.x,Is.LessThan(0f));
            Assert.That(_environment.transform.Find("Camps/BlueCamp").GetChild(0).position.x,Is.GreaterThan(0f));
            Assert.That(_battle.View.PlayerColor.r,Is.GreaterThan(_battle.View.PlayerColor.b));
            for(int card=0;card<5;card++)Assert.That(_battle.TrySummon(SummonRequest.Quick(card)).Accepted,Is.True);
            foreach(var hero in _battle.Registry.Units.OfType<HeroUnit>())
            { Assert.That(hero.DrawnSize,Is.EqualTo(source.HeroModelScale*2f).Within(.001f)); Assert.That(hero.Radius,Is.EqualTo(HeroUnit.BodyRadius)); }
            yield return null;
            foreach(var preview in _battle.GetComponent<BattlefieldLegacyBridge>().editorCastlePreviews)Assert.That(preview.activeInHierarchy,Is.False);
            var block=new MaterialPropertyBlock();
            foreach(var castle in _battle.Registry.Units.OfType<CastleUnit>())
            {
                var body=castle.GetComponentsInChildren<MeshRenderer>().Single(r=>r.enabled&&r.GetComponent<MeshFilter>().sharedMesh==intact);
                body.GetPropertyBlock(block);
                Assert.That(block.GetFloat("_FactionMask"),Is.EqualTo(castle.Team==Team.Enemy?1f:0f));
                Assert.That(castle.transform.Find("HealthFill").GetComponent<SpriteRenderer>().color,Is.EqualTo(castle.Team==Team.Player?_battle.View.PlayerColor:_battle.View.EnemyColor));
                var original=originals.Props.First(p=>p.name=="ThreeKingdomsCommand_1");
                Assert.That(body.transform.parent.localScale.x/original.scale,Is.EqualTo(1.45f*2f).Within(.001f));
            }
            Overview();Capture("feedback-alive.png");
            foreach(var team in new[]{Team.Enemy,Team.Player})
            {
                if(team==Team.Player){_battle.StartBattle(1);yield return null;_battle.AutoSimulate=false;Overview();}
                var castle=_battle.Registry.Units.OfType<CastleUnit>().Single(u=>u.Team==team);
                castle.TakeDamage(castle.MaxHealth*100f,null);yield return null;
                Assert.That(_battle.CastleFalls.Count,Is.EqualTo(1));
                Capture(team==Team.Enemy?"feedback-enemy-shaking.png":"feedback-player-shaking.png");
                yield return new WaitForSeconds(1.25f);
                Assert.That(_battle.CastleFalls[0].HasCollapsed,Is.True);
                var ruin=_battle.CastleFalls[0].GetComponentsInChildren<MeshRenderer>().Single(r=>r.GetComponent<MeshFilter>().sharedMesh==ruined);
                ruin.GetPropertyBlock(block);Assert.That(block.GetFloat("_FactionMask"),Is.EqualTo(team==Team.Enemy?1f:0f));
                Assert.That(_battle.CastleFalls[0].GetComponentsInChildren<MeshRenderer>().Any(r=>r.GetComponent<MeshFilter>().sharedMesh==intact),Is.False);
                Capture(team==Team.Enemy?"feedback-enemy-ruined.png":"feedback-player-ruined.png");
            }
        }
        [UnityTest]
        public IEnumerator Altar_UsesRealSurfaceHeightOnlyDuringRendering()
        {
            Assert.That(_battle.TrySummon(SummonRequest.Quick(0)).Accepted,Is.True);var hero=_battle.SelectedHero;hero.SetPosition(Vector2.zero);
            var bridge=_battle.GetComponent<BattlefieldLegacyBridge>();Physics.SyncTransforms();
            Assert.That(bridge.altarCollider.Raycast(new Ray(Vector3.up*8f,Vector3.down),out var hit,12f),Is.True);Assert.That(hit.point.y,Is.GreaterThan(.1f));
            float renderedHeight=-1f;
            void DuringRender(UnityEngine.Rendering.ScriptableRenderContext context,Camera camera){if(camera==_environment.battleCamera)renderedHeight=hero.transform.position.y;}
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering+=DuringRender;
            try{Capture("play-altar.png");}finally{UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering-=DuringRender;}
            Assert.That(renderedHeight,Is.EqualTo(hit.point.y).Within(.03f));Assert.That(hero.Position,Is.EqualTo(Vector2.zero));Assert.That(hero.transform.localPosition.z,Is.EqualTo(0f).Within(.001f));
            yield return null;
        }
        private Color32[] Capture(string filename)
        {
            var camera=_environment.battleCamera;var previous=camera.targetTexture;var active=RenderTexture.active;
            var rt=new RenderTexture(1600,900,24);rt.antiAliasing=2;bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            try{camera.targetTexture=rt;camera.aspect=1600f/900f;camera.Render();camera.Render();RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();Directory.CreateDirectory(Evidence);File.WriteAllBytes(Evidence+"/"+filename,image.EncodeToPNG());var pixels=image.GetPixels32();Object.DestroyImmediate(image);return pixels;}
            finally{camera.targetTexture=previous;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);ShaderUtil.allowAsyncCompilation=async;}
        }
    }
}
#endif
