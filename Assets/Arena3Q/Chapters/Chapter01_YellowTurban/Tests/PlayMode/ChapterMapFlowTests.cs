#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Frontend;
using Arena3Q.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arena3Q.Exploration.Tests
{
    public sealed partial class ChapterMapFlowTests
    {
        private const string Root="Assets/Arena3Q/Chapters/Chapter01_YellowTurban";
        private InMemoryProfileStore _store;
        private ChapterMapController _map;
        private float _previousVolume;
        [System.Serializable] private sealed class Reading { public Element[] markers, occluders; }
        [System.Serializable] private sealed class Element { public string name; public Rect rect, visualRect; public bool active; public float alpha; }
        private static readonly string CaptureDirectory = "NghiemThu/Chapter01/P1_Runtime_20261028/run_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        [UnitySetUp] public IEnumerator Open()
        {
            _previousVolume=AudioListener.volume; AudioListener.volume=0;
            var config=AssetDatabase.LoadAssetAtPath<ChapterMapConfig>(Root+"/Data/CH01_MapConfig.asset");
            Assert.NotNull(config);
            var profile=new PlayerProfile{accountId="chapter-test",accountKind=AccountKind.Guest,lastServerId="test"};
            var c=profile.GetOrCreateCharacter("test");c.lordHeroId=config.frontend.LordRoster.Lords.First(x=>x.lord!=null&&x.lord.PortraitModelPrefab!=null).lord.HeroId;c.name="Kiểm map";c.tutorialCleared=true;
            _store=new InMemoryProfileStore(profile);FrontendServiceLocator.Override(profiles:_store);
            yield return SceneManager.LoadSceneAsync(config.sceneName);
            yield return null;yield return null;
            _map=Object.FindFirstObjectByType<ChapterMapController>();Assert.NotNull(_map,"Builder must install systems prefab into the scene.");Assert.True(_map.Ready,_map.LastMessage);
        }
        [UnityTearDown] public IEnumerator Close()
        {
            var transition = Object.FindAnyObjectByType<Arena3Q.UI.SceneTransition>();
            if (transition != null) Object.Destroy(transition.gameObject);
            BattleLaunch.Clear(); FrontendController.ResumeAt = null;
            DisplayMetrics.Reset();
            yield return SceneManager.LoadSceneAsync("Frontend");
            yield return null;
            FrontendServiceLocator.Reset();
            AudioListener.volume=_previousVolume;
        }
        [UnityTest] public IEnumerator MarkersAvoidHudAtFourAspectRatiosAndAllQuestTextsFitBesideArrow()
        {
            var canvas=_map.Hud.GetComponentInChildren<Canvas>(); var camera=_map.View.Camera;
            var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;var previousTarget=camera.targetTexture;
            var quest=canvas.transform.Find("SafeArea/CurrentQuest/Text").GetComponent<TMPro.TMP_Text>();
            var arrow=(RectTransform)canvas.transform.Find("SafeArea/CurrentQuest/Direction");
            var focus=_map.View.Focus;int visible=0;
            try
            {
                for(int stage=0;stage<7;stage++)
                {
                    for(int i=0;i<stage;i++)_map.Session.Progress.Get(_map.Config.stages[i].id).stars=3;
                    _map.Hud.Refresh();quest.ForceMeshUpdate();
                    Assert.False(quest.isTextOverflowing,"Quest "+stage+": "+quest.text);
                    Assert.Less(quest.rectTransform.anchoredPosition.x+quest.rectTransform.rect.width,
                        arrow.anchoredPosition.x+arrow.rect.center.x-arrow.rect.size.magnitude*.5f,"Quest must leave space for the rotated direction arrow.");
                    var textCorners=new Vector3[4];var arrowCorners=new Vector3[4];quest.rectTransform.GetWorldCorners(textCorners);
                    for(int angle=0;angle<360;angle+=45)
                    {
                        arrow.localRotation=Quaternion.Euler(0,0,angle);arrow.GetWorldCorners(arrowCorners);
                        Assert.Less(textCorners.Max(c=>c.x),arrowCorners.Min(c=>c.x),"Rotated arrow intersects quest at "+angle+" degrees");
                    }
                }
                foreach(var size in new[]{new Vector2Int(1600,900),new Vector2Int(1800,900),new Vector2Int(2000,900),new Vector2Int(2340,1080)})
                {
                    var rt=new RenderTexture(size.x,size.y,24);rt.Create();camera.targetTexture=rt;
                    canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                    Assert.True(DisplayMetrics.TrySetPreview(new DevicePreviewSettings{width=size.x,height=size.y,dpi=395,frameRate=60,left=.034f,right=.02f}));
                    foreach(var stage in _map.Config.stages)
                    {
                        _map.View.FocusOn(stage.center);Canvas.ForceUpdateCanvases();
                        yield return new WaitForSecondsRealtime(.4f);
                        var reading=JsonUtility.FromJson<Reading>(_map.Hud.InspectUi());
                        Assert.AreEqual(_map.Config.ui.markerOccluders.Length,reading.occluders.Length);
                        foreach(var marker in reading.markers)
                        {
                            if(marker.alpha>.001f)visible++;
                            foreach(var hud in reading.occluders.Where(x=>x.active))
                                if(marker.visualRect.Overlaps(hud.rect))
                                {Assert.LessOrEqual(marker.alpha,.001f,size+" "+marker.name+" / "+hud.name);Assert.False(marker.active);}
                        }
                    }
                    camera.targetTexture=previousTarget;rt.Release();Object.Destroy(rt);
                }
                Assert.Greater(visible,0,"Occlusion must not hide every marker.");
            }
            finally{camera.targetTexture=previousTarget;canvas.renderMode=mode;canvas.worldCamera=previousCamera;_map.View.FocusOn(focus);DisplayMetrics.Reset();}
        }
        [UnityTest] public IEnumerator FogDissolveHasTenFramesPerPresetAndNeverCoversOpenRegion()
        {
            var field=typeof(ChapterMapWorld).GetField("_visibility",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var visibility=(float[])field.GetValue(_map.World);var saved=(float[])visibility.Clone();var focus=_map.View.Focus;
            var camera=_map.View.Camera;float cameraSize=camera.orthographicSize;
            bool batching=UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching;
            try
            {
                UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=false;
                _map.View.FocusOn((_map.Config.stages[0].center+_map.Config.stages[1].center)*.5f);
                foreach(int rate in new[]{30,60,120})
                {
                    Assert.True(DisplayMetrics.TrySetPreview(new DevicePreviewSettings{width=1672,height=941,dpi=160,frameRate=rate}));
                    for(int frame=0;frame<10;frame++)
                    {
                        // Isolated render fixture: sample exact reveal progress; no profile or gameplay mutation.
                        visibility[1]=frame/9f;yield return null;
                        Capture("fog-"+rate+"-"+frame.ToString("00"));
                    }
                    visibility[1]=0;_map.View.FocusOn(_map.Config.stages[1].center);camera.orthographicSize=cameraSize*.55f;
                    yield return null;Capture("fog-close-"+rate);
                    camera.orthographicSize=cameraSize;_map.View.FocusOn((_map.Config.stages[0].center+_map.Config.stages[1].center)*.5f);
                }
                for(int i=0;i<4;i++)visibility[i]=1;visibility[4]=.5f;
                _map.View.FocusOn((_map.Config.stages[3].center+_map.Config.stages[4].center)*.5f);yield return null;
                Capture("fog-granary-open-border");
            }
            finally{saved.CopyTo(visibility,0);_map.View.FocusOn(focus);camera.orthographicSize=cameraSize;DisplayMetrics.Reset();UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=batching;}
        }
        [UnityTest] public IEnumerator CaptureAuthoredBuildingStateAtHalfFade()
        {
            var visibility=(float[])typeof(ChapterMapWorld).GetField("_visibility",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(_map.World);
            var saved=(float[])visibility.Clone();var focus=_map.View.Focus;
            var groups=(System.Collections.IEnumerable)typeof(ChapterMapWorld).GetField("_groups",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(_map.World);
            bool batching=UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching;
            var restored=new System.Collections.Generic.List<(object group,float alpha)>();
            try
            {
                UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=false;
                for(int i=0;i<visibility.Length;i++)visibility[i]=1;
                foreach(int stage in new[]{0,3})
                {
                    _map.View.FocusOn(_map.Config.stages[stage].center);
                    var selected=new System.Collections.Generic.List<object>();
                    foreach(var group in groups)
                    {
                        var type=group.GetType();var root=(GameObject)type.GetField("Root").GetValue(group);
                        if((int)type.GetField("Stage").GetValue(group)!=stage || !root.name.StartsWith("State_"))continue;
                        restored.Add((group,(float)type.GetField("Alpha").GetValue(group)));selected.Add(group);
                    }
                    foreach(float alpha in new[]{0f,.5f,1f})
                    {
                        foreach(var group in selected)
                        {
                            var type=group.GetType();var root=(GameObject)type.GetField("Root").GetValue(group);
                            type.GetMethod("Set").Invoke(group,new object[]{root.name=="State_After"?alpha:1-alpha});
                        }
                        yield return null;Capture("building-state-"+(stage+1)+"-"+(int)(alpha*100));
                    }
                }
            }
            finally
            {
                foreach(var pair in restored)pair.group.GetType().GetMethod("Set").Invoke(pair.group,new object[]{pair.alpha});
                saved.CopyTo(visibility,0);_map.View.FocusOn(focus);UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=batching;
            }
        }
        [UnityTest] public IEnumerator SavedWorldRevealsEachRegionOnceAndKeepsCompletedStatesOnReplay()
        {
            Assert.NotNull(_map.Config.motion.fogMaterial);
            Assert.True(_map.Config.motion.fogMaterial.shader.isSupported);
            Assert.AreEqual(1,_map.World.VisibleCount);
            for(int stage=1;stage<7;stage++)
            {
                Assert.False(_map.World.MarkersVisible(stage));
                foreach(var anchor in _map.Anchors[stage])Assert.False(anchor.gameObject.activeInHierarchy,"Locked NPC must be hidden.");
            }
            for(int completed=0;completed<7;completed++)
            {
                // Only this isolated profile supplies verified results; release data remains unbound.
                _map.Session.AcceptQuest(completed);
                string attempt="world-test-"+completed;
                Assert.True(_map.Session.Rules.BeginBattle(_map.Session.Progress,completed,attempt));
                Assert.True(_map.Session.Rules.ApplyResult(_map.Session.Progress,_map.Config.stages[completed].id,attempt,true,3));
                _map.Session.Save();
                string scene=_map.Config.sceneName;
                yield return SceneManager.LoadSceneAsync(scene);yield return null;yield return null;
                _map=Object.FindFirstObjectByType<ChapterMapController>();
                if(completed<6)
                {
                    Assert.True(_map.World.Revealing);Assert.True(_map.Busy);
                    Assert.False(_map.World.MarkersVisible(completed+1));
                    Assert.AreEqual(0,_map.World.Visibility(completed+1));
                    var previousFocus=_map.View.Focus;
                    _map.View.FocusOn(_map.Config.stages[completed+1].center);
                    Capture("world-"+(completed+2)+"-before");
                    _map.View.FocusOn(previousFocus);
                    float deadline=Time.realtimeSinceStartup+12;
                    bool fadeCaptured=false;
                    while(_map.World.Revealing&&Time.realtimeSinceStartup<deadline)
                    {
                        if(!fadeCaptured && _map.World.Visibility(completed+1)>=1 && !_map.World.MarkersVisible(completed+1))
                        {yield return new WaitForSecondsRealtime(.25f);Capture("environment-"+(completed+2)+"-mid");fadeCaptured=true;}
                        yield return null;
                    }
                    Assert.False(_map.World.Revealing);Assert.False(_map.Busy);
                    Assert.AreEqual(completed+2,_map.Session.Progress.revealedStages);
                    Assert.AreEqual(1,_map.World.Visibility(completed+1));
                    Assert.True(_map.World.MarkersVisible(completed+1));
                    Assert.AreEqual(1,_map.Audio.Requested(MapSound.FogReveal),"Fog sound hook fires once for an actual reveal.");
                    Assert.AreEqual(_map.Config.stages[completed+1].boss?1:0,_map.Audio.Requested(MapSound.Boss));
                    Capture("world-"+(completed+2)+"-after");
                }
                var area=_map.Areas[completed];
                Assert.NotNull(area);
                Assert.False(area.Find("State_Before").gameObject.activeSelf);
                var after=area.Find("State_After");
                if(after!=null)Assert.True(after.gameObject.activeSelf);
                yield return SceneManager.LoadSceneAsync(scene);yield return null;yield return null;
                _map=Object.FindFirstObjectByType<ChapterMapController>();
                Assert.False(_map.World.Revealing,"Reload must not cover an opened region again.");
                Assert.AreEqual(0,_map.Audio.Requested(MapSound.FogReveal));Assert.AreEqual(0,_map.Audio.Requested(MapSound.Boss));
                Assert.AreEqual(Mathf.Min(7,completed+2),_map.World.VisibleCount);
            }
        }
        [UnityTest] public IEnumerator FirstVisitNavigationAndMissingBattleDoNotUnlock()
        {
            Assert.AreEqual(1,_map.Session.Rules.UnlockedCount(_map.Session.Progress));
            Assert.True(_map.Motor.Agent.isOnNavMesh);
            Assert.False(_map.Motor.PointUnlocked(_map.Config.stages[1].center));
            _map.LaunchBattle(0);Assert.False(_map.Busy);Assert.AreEqual(_map.Config.ui.unavailable,_map.LastMessage);
            Assert.AreEqual(1,_map.Session.Rules.UnlockedCount(_map.Session.Progress));
            yield return new WaitForSecondsRealtime(.75f);
            Capture("first-visit");
            bool batching = UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching;
            try { UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=false; Capture("first-visit-without-srp-batcher"); }
            finally { UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=batching; }
            yield return null;
        }
        [UnityTest] public IEnumerator RemoteQuestWalksAndOpensDialogueThenAccepts()
        {
            int target=System.Array.FindIndex(_map.Config.stages[0].targets,t=>t.kind==MapInteractionKind.Quest);
            Assert.GreaterOrEqual(target,0);_map.SelectTarget(0,target);
            float deadline=Time.realtimeSinceStartup+20;
            while(!_map.Hud.ModalVisible&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.True(_map.Hud.ModalVisible,_map.LastMessage);
            yield return new WaitForSecondsRealtime(.5f);Capture("quest-dialogue");
            _map.AcceptQuest(0);Assert.AreEqual(ChapterStageState.InProgress,_map.Session.Rules.State(_map.Session.Progress,0));
            Assert.True(_store.SaveCount>0);yield return null;
        }
        [UnityTest] public IEnumerator CancelAndReloadKeepPoseAndQuestProgress()
        {
            _map.AcceptQuest(0);_map.SavePose();var expected=_map.Motor.transform.position;
            _map.SelectTarget(0,0);_map.Motor.Cancel();Assert.False(_map.Motor.Moving);
            yield return SceneManager.LoadSceneAsync(_map.Config.sceneName);yield return null;yield return null;
            _map=Object.FindFirstObjectByType<ChapterMapController>();Assert.True(_map.Ready);
            Assert.Less(Vector3.Distance(expected,_map.Motor.transform.position),.5f);
            Assert.AreEqual(ChapterStageState.InProgress,_map.Session.Rules.State(_map.Session.Progress,0));
        }
        [UnityTest] public IEnumerator EveryUnlockedStageHasReachableTargetsAndLockedGroundIsRejected()
        {
            for(int stage=0;stage<7;stage++)
            {
                var data=_map.Config.stages[stage];
                if(stage<6) Assert.False(_map.Motor.MoveGround(_map.Config.stages[stage+1].center));
                for(int t=0;t<data.targets.Length;t++)
                {
                    if(data.targets[t].kind==MapInteractionKind.Battle) _map.AcceptQuest(stage);
                    else if(_map.Session.Progress.Find(data.id)?.questAccepted==true) continue;
                    bool reachable=_map.Motor.RequestTarget(stage,t,_map.Anchors[stage][t]);
                    Assert.True(reachable,data.id+" target "+t+": "+_map.Motor.PathFailure);
                    _map.Motor.Cancel();
                }
                // Test fixture only: emulate a verified completion to load the next baked navigation state.
                _map.Session.Progress.Get(data.id).stars=3;_map.Session.Progress.revealedStages=Mathf.Min(7,stage+2);_map.Session.Save();
                if(stage<6){yield return SceneManager.LoadSceneAsync(_map.Config.sceneName);yield return null;yield return null;_map=Object.FindFirstObjectByType<ChapterMapController>();Assert.True(_map.Ready);}
            }
        }
        [UnityTest] public IEnumerator BattleCallbacksValidateIdentitySupportReplayAndGrantRewardsOnce()
        {
            var entry=_map.Config.stages[0];var previous=entry.battle;
            var fixture=ScriptableObject.CreateInstance<StageConfig>();var wrong=ScriptableObject.CreateInstance<StageConfig>();
            var reward=_map.Config.rewards[0];var contents=reward.contents;int threshold=reward.stars;
            try
            {
                entry.battle=fixture;Assert.Null(_map.Session.CreateBattle(0));_map.AcceptQuest(0);
                var request=_map.Session.CreateBattle(0);Assert.NotNull(request);Assert.LessOrEqual(request.Lineup.Count,5);
                var win=new OutcomeDecision(BattleOutcome.Victory,OutcomeReason.EnemyCastleDestroyed);
                request.OnStarted(fixture);request.OnFinishedWithStars(wrong,win,3);Assert.AreEqual(0,_map.Session.Rules.TotalStars(_map.Session.Progress));
                request.OnFinishedWithStars(fixture,win,1);Assert.AreEqual(1,_map.Session.Rules.TotalStars(_map.Session.Progress));
                request.OnFinishedWithStars(fixture,win,3);Assert.AreEqual(1,_map.Session.Rules.TotalStars(_map.Session.Progress),"Duplicate delivery must be ignored.");
                request.OnStarted(fixture);request.OnFinishedWithStars(fixture,win,3);Assert.AreEqual(3,_map.Session.Rules.TotalStars(_map.Session.Progress));
                request.OnStarted(fixture);request.OnFinishedWithStars(fixture,new OutcomeDecision(BattleOutcome.Defeat,OutcomeReason.TimeUp),0);
                Assert.AreEqual(3,_map.Session.Rules.TotalStars(_map.Session.Progress));request.OnReturn();
                reward.stars=3;reward.contents=new[]{new ResourceBalance{id="Silver",amount=17}};
                long before=_map.Session.Character.wallet?.Find(x=>x.id=="Silver")?.amount??0;
                Assert.True(_map.Session.Claim(0));Assert.False(_map.Session.Claim(0));
                Assert.AreEqual(before+17,_map.Session.Character.wallet.Find(x=>x.id=="Silver").amount);
            }
            finally{entry.battle=previous;reward.contents=contents;reward.stars=threshold;Object.Destroy(fixture);Object.Destroy(wrong);}
            yield return null;
        }
        [UnityTest] public IEnumerator ClearedPathTimesOutAndChangedQuestCancelsPendingInteraction()
        {
            int target = System.Array.FindIndex(_map.Config.stages[0].targets, t => t.kind == MapInteractionKind.Quest);
            var anchor = _map.Anchors[0][target];
            Assert.Greater(Vector2.Distance(new Vector2(anchor.position.x, anchor.position.z),
                new Vector2(_map.Motor.transform.position.x, _map.Motor.transform.position.z)), _map.Config.interactionRadius);
            _map.SelectTarget(0, target);
            yield return null; // Let navigation process the request, then lose the route before arrival.
            _map.Motor.Agent.ResetPath();
            float deadline = Time.realtimeSinceStartup + _map.Config.stuckSeconds + 2;
            while (_map.LastMessage != _map.Config.ui.stuckMessage && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(_map.Config.ui.stuckMessage, _map.LastMessage);
            Assert.False(_map.Hud.ModalVisible);
            Assert.False(_map.Motor.Moving);

            _map.SelectTarget(0, target);
            Assert.True(_map.Session.AcceptQuest(0)); // State changes while walking.
            yield return null;
            Assert.False(_map.Motor.Moving);
            Assert.False(_map.Hud.ModalVisible);
        }
        [UnityTest] public IEnumerator InterruptedAttemptIsCancelledAndPersistedOnSessionRestore()
        {
            _map.AcceptQuest(0);
            Assert.True(_map.Session.Rules.BeginBattle(_map.Session.Progress, 0, "interrupted"));
            _map.Session.Save(); int saves = _store.SaveCount;
            var restored = new ChapterMapSession(_map.Config, _store);
            Assert.IsEmpty(restored.Progress.pendingBattleId);
            Assert.IsEmpty(restored.Progress.pendingStageId);
            Assert.AreEqual(saves + 1, _store.SaveCount, "cancellation must reach persistent storage");
            Assert.AreEqual(0, restored.Rules.TotalStars(restored.Progress));
            Assert.False(restored.Rules.ApplyResult(restored.Progress, _map.Config.stages[0].id, "interrupted", true, 3));
            yield return null;
        }
        [UnityTest] public IEnumerator DevicePreviewKeepsControlsInsideSafeAreaAndMarkerTouchLargeEnough()
        {
            Assert.True(DisplayMetrics.TrySetPreview(new DevicePreviewSettings { width=2340,height=1080,dpi=395,
                frameRate=60,left=80f/2340,right=48f/2340 }));
            yield return new WaitForSecondsRealtime(.4f);
            var canvas = _map.Hud.GetComponentInChildren<Canvas>();
            var safe = (RectTransform)canvas.transform.Find("SafeArea");
            Assert.AreEqual(80f/2340,safe.anchorMin.x,.0001f);
            Assert.AreEqual(1-48f/2340,safe.anchorMax.x,.0001f);
            foreach(var name in new[]{"Back","Resources/Resource_Stamina","Resources/Resource_Silver"})
            {
                var item = (RectTransform)safe.Find(name);
                var corners = new Vector3[4]; item.GetWorldCorners(corners);
                foreach(var corner in corners)
                {
                    var local = safe.InverseTransformPoint(corner);
                    Assert.That(local.x,Is.InRange(safe.rect.xMin-.1f,safe.rect.xMax+.1f),name);
                    Assert.That(local.y,Is.InRange(safe.rect.yMin-.1f,safe.rect.yMax+.1f),name);
                }
            }
            var hit = (RectTransform)canvas.transform.Find("Marker_0_0/TouchArea");
            var hitCorners = new Vector3[4]; hit.GetWorldCorners(hitCorners);
            Assert.GreaterOrEqual(Vector3.Distance(hitCorners[0],hitCorners[3]),_map.Config.ui.minimumTapDp*DisplayMetrics.Dpi/160f-.1f);
        }
        [UnityTest] public IEnumerator MarkerTouchRoutesThroughNearestVisibleTargetAndUsesSharedButtons()
        {
            yield return new WaitForSecondsRealtime(.4f);
            var canvas = _map.Hud.GetComponentInChildren<Canvas>();
            foreach(var button in canvas.GetComponentsInChildren<Button>())
                if(button.name!="CurrentQuest") Assert.NotNull(button.GetComponent<Arena3Q.Frontend.GameButton>(),button.name);
            int quest=System.Array.FindIndex(_map.Config.stages[0].targets,t=>t.kind==MapInteractionKind.Quest);
            var hit = (RectTransform)canvas.transform.Find("Marker_0_"+quest+"/TouchArea");
            var point=RectTransformUtility.WorldToScreenPoint(null,hit.position);
            ExecuteEvents.Execute(hit.gameObject,new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
            float deadline=Time.realtimeSinceStartup+20;
            while(!_map.Hud.ModalVisible&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.True(_map.Hud.ModalVisible,_map.LastMessage);
            Assert.NotNull(canvas.transform.Find("SafeArea/ChapterModal/Panel/AcceptQuest"));
        }
        [UnityTest] public IEnumerator RoadGuideHasUpwardSurfaceAndRewardsUseLibraryStars()
        {
            var route = _map.GetComponent<ChapterMapRoute>(); Assert.NotNull(route.RouteMesh);
            Assert.Greater(route.RouteMesh.vertexCount, 100, "The guide must follow the curved authored road.");
            foreach (var normal in route.RouteMesh.normals) Assert.Greater(normal.y, 0, "Ground mesh must face up.");
            Assert.True(_map.Config.ui.routeMaterial.shader.isSupported);
            var canvas = _map.Hud.GetComponentInChildren<Canvas>();
            foreach (var reward in _map.Config.rewards)
            {
                var root = canvas.transform.Find("SafeArea/ChapterRewards/Reward_" + reward.id);
                Assert.AreSame(UiIconSet.Load().Star, root.Find("Star").GetComponent<Image>().sprite);
                Assert.NotNull(root.Find("Chest").GetComponent<Image>().sprite);
            }
            yield return null;
        }
        [UnityTest] public IEnumerator RealBattleVictoryReplayDefeatAndReturnPreserveMapPose()
        {
            var entry = _map.Config.stages[0]; var previous = entry.battle;
            // Test-only binding to the existing campaign battle. Production chapter battles stay unbound.
            var fixture = AssetDatabase.LoadAssetAtPath<StageConfig>("Assets/Arena3Q/Content/Battlefield/Campaign/Stage_Attack_01.asset");
            Assert.NotNull(fixture);
            var expectedPosition = _map.Motor.transform.position;
            var expectedYaw = _map.Motor.transform.eulerAngles.y;
            var expectedFocus = _map.View.Focus;
            var expectedSize = _map.View.Camera.orthographicSize;
            var character = _map.Session.Character;
            string chapterId = _map.Config.chapterId, battleScene = _map.Config.frontend.BattleSceneName;
            try
            {
                entry.battle = fixture; _map.AcceptQuest(0);
                // Disable transition animation only for this integration fixture to avoid racing the cover.
                Arena3Q.UI.SceneTransition.Configure(0);
                _map.LaunchBattle(0);
                BattleController battle = null;
                float deadline = Time.realtimeSinceStartup + 30;
                while ((battle == null || battle.State == null) && Time.realtimeSinceStartup < deadline)
                { yield return null; battle = Object.FindAnyObjectByType<BattleController>(); }
                Assert.NotNull(battle); Assert.NotNull(battle.State);
                Assert.AreEqual(battleScene, battle.gameObject.scene.name);
                Assert.AreSame(fixture, battle.Stage);
                Assert.LessOrEqual(battle.Lineup.Count, SummonRules.MaxHeroesPerBattle);
                battle.AutoSimulate = false;
                var castle = battle.Registry.Get(battle.EnemyCastleId);
                Assert.NotNull(castle); castle.TakeDamage(float.MaxValue, null);
                battle.Simulate(.05f);
                Assert.AreEqual(BattleOutcome.Victory, battle.State.Outcome);
                int stars = battle.State.Stars;
                var progress = _store.Load().FindCharacter("test").GetChapter(chapterId);
                Assert.AreEqual(stars, progress.Find(entry.id).stars);

                battle.Restart(); battle.AutoSimulate = false;
                Assert.IsNotEmpty(progress.pendingBattleId, "replay starts a fresh attempt");
                // Attack stages lose on timeout, not on destruction of the player's castle.
                while (!battle.IsOver && battle.State.ElapsedTime <= fixture.TimeLimit) battle.Simulate(.25f);
                Assert.AreEqual(BattleOutcome.Defeat, battle.State.Outcome);
                Assert.AreEqual(stars, progress.Find(entry.id).stars);
                battle.ReturnToMenu();
                deadline = Time.realtimeSinceStartup + 30;
                while ((_map == null || !_map.Ready) && Time.realtimeSinceStartup < deadline)
                { yield return null; _map = Object.FindAnyObjectByType<ChapterMapController>(); }
                Assert.NotNull(_map); Assert.True(_map.Ready, _map.LastMessage);
                Assert.AreSame(character, _map.Session.Character);
                Assert.Less(Vector3.Distance(expectedPosition, _map.Motor.transform.position), .5f);
                Assert.Less(Mathf.Abs(Mathf.DeltaAngle(expectedYaw, _map.Motor.transform.eulerAngles.y)), 1);
                Assert.Less(Vector3.Distance(expectedFocus, _map.View.Focus), .05f);
                Assert.AreEqual(expectedSize, _map.View.Camera.orthographicSize, .01f);
                Assert.AreEqual(2, _map.Session.Rules.UnlockedCount(_map.Session.Progress));
                Assert.IsEmpty(_map.Session.Progress.pendingBattleId);
            }
            finally { entry.battle = previous; }
        }
        private void Capture(string label)
        {
            string dir=CaptureDirectory;Directory.CreateDirectory(dir);
            File.WriteAllText(dir+"/"+label+".json",_map.Hud.InspectUi());
            // Render camera + UI on a dedicated RT, leaving player overlay configuration intact afterwards.
            var camera=_map.View.Camera;var rt=new RenderTexture(1672,941,24);rt.Create();var previous=camera.targetTexture;
            var canvas=_map.Hud.GetComponentInChildren<Canvas>();var mode=canvas.renderMode;var world=canvas.worldCamera;
            try{
                camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();
                var active=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();RenderTexture.active=active;
                File.WriteAllBytes(dir+"/"+label+".png",texture.EncodeToPNG());Object.Destroy(texture);
            }finally{canvas.renderMode=mode;canvas.worldCamera=world;camera.targetTexture=previous;rt.Release();Object.Destroy(rt);}
        }
    }
}
#endif
