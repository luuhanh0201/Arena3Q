#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Frontend;
using Arena3Q.Gameplay;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Arena3Q.Tests.PlayMode
{
    public sealed class LobbyFlowTests
    {
        private FrontendController _flow;
        // Immutable run folders keep open image viewers from locking the next capture on Windows.
        private static readonly string Output = "NghiemThu/UI/XemTruoc_Lobby2D_SuaTheoMau_20261027/captures_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        [UnitySetUp]
        public IEnumerator Setup()
        {
            var profile = new PlayerProfile { accountId = "lobby-test", accountKind = AccountKind.Guest, lastServerId = "s3", introSeen = true };
            var character = profile.GetOrCreateCharacter("s3"); character.name = "Lâm Tử Yên";
            character.lordHeroId = "lord_strategist"; character.lordLevel = 15; character.storySeen = character.tutorialCleared = true;
            character.lordExperience = 65;
            character.lordStars = 5; character.vip = 12; character.combatPower = 1245680;
            character.wallet = new List<ResourceBalance> {
                new ResourceBalance { id = "Banknotes", amount = 12680 },
                new ResourceBalance { id = "Silver", amount = 5320000 },
                new ResourceBalance { id = "Stamina", amount = 180, capacity = 180 } };
            character.AddHero("guardian", 1, 3);
            FrontendServiceLocator.Reset(); FrontendServiceLocator.Override(profiles: new InMemoryProfileStore(profile));
            FrontendController.ResumeAt = FrontendStep.Lobby;
            yield return SceneManager.LoadSceneAsync("Frontend"); yield return null;
            _flow = Object.FindAnyObjectByType<FrontendController>();
            Assert.That(_flow.Step, Is.EqualTo(FrontendStep.Lobby));
            yield return new WaitForSecondsRealtime(.6f);
        }
        [TearDown]
        public void Cleanup()
        {
            FrontendServiceLocator.Reset(); FrontendController.ResumeAt = null; BattleLaunch.Clear(); SafeAreaFitter.OverrideArea = null;
            var transition = Object.FindAnyObjectByType<Arena3Q.UI.SceneTransition>(); if (transition != null) Object.Destroy(transition.gameObject);
        }
        [UnityTest]
        public IEnumerator LobbyUsesChosenModelAndSharedControlsWithoutPreviewButtons()
        {
            var lobby = (LobbyScreen)_flow.Screen;
            Assert.That(lobby.LordView.Model, Is.Not.Null); Assert.That(lobby.LordView.Idle.name, Does.Contain("Idle"));
            Assert.That(lobby.LordView.CaptureCamera.allowMSAA, Is.True);
            Assert.That(lobby.LordView.Texture.antiAliasing, Is.GreaterThan(1));
            Assert.That(lobby.LordView.GetComponent<RawImage>().material.shader.name, Is.EqualTo("Arena3Q/UI/Premultiplied Capture"));
            var renderer = lobby.LordView.Model.GetComponentInChildren<Renderer>();
            var properties = new MaterialPropertyBlock(); renderer.GetPropertyBlock(properties);
            var sentinel = Shader.PropertyToID("_LobbyProbeRegressionSentinel"); properties.SetFloat(sentinel, .25f);
            renderer.SetPropertyBlock(properties); lobby.LordView.SetTint(Color.white); renderer.GetPropertyBlock(properties);
            Assert.That(properties.GetFloat(sentinel), Is.EqualTo(.25f), "lighting preserves existing per-renderer properties");
            Assert.That(properties.GetTexture("unity_SpecCube0"), Is.SameAs(_flow.Config.Lobby.reflectionEnvironment), "capture PBR samples its own warm environment");
            Assert.That(lobby.Root.GetComponentsInChildren<MonoBehaviour>().Any(x => x != null && x.GetType().Name.StartsWith("LuBu")), Is.False);
            var labels = lobby.Root.GetComponentsInChildren<TMP_Text>().Select(x => x.text).ToArray();
            foreach (var text in new[] { "Sáng", "Trưa", "Chiều", "Tối", "Đêm", "Chạy", "Tấn công" }) Assert.That(labels, Does.Not.Contain(text));
            foreach (var button in lobby.Root.GetComponentsInChildren<Button>()) Assert.That(button.GetComponent<GameButton>(), Is.Not.Null, button.name);
            lobby.ShowUnavailable(); yield return new WaitForSecondsRealtime(.4f);
            Assert.That(lobby.Root.Find("SafeArea/Design1280/UnavailableModal"), Is.Not.Null);
        }
        [UnityTest]
        public IEnumerator LordWithoutModelShowsFallbackAndKeepsItsIdentity()
        {
            var missing = _flow.Config.LordRoster.Lords.First(entry => entry.lord.PortraitModelPrefab == null);
            _flow.Character.lordHeroId = missing.lord.HeroId;
            FrontendController.ResumeAt = FrontendStep.Lobby; _flow.Begin();
            yield return null;
            var lobby = (LobbyScreen)_flow.Screen;
            Assert.That(_flow.ChosenLord, Is.SameAs(missing));
            Assert.That(lobby.LordView.Model, Is.Null);
            Assert.That(lobby.Root.GetComponentsInChildren<TMP_Text>().Any(text => text.text == _flow.Config.Lobby.missingModel), Is.True);
        }
        [UnityTest]
        public IEnumerator DirectBattleFallbackReturnsToSameLordAndServerWithoutLogin()
        {
            // Keep coverage of the legacy direct-battle return and its model capture.
            // The release lobby -> chapter route is covered by FrontendFlowTests.
            var character = _flow.Character;
            var config = new UnityEditor.SerializedObject(_flow.Config);
            var chapterScene = config.FindProperty("chapterMapSceneName");
            var originalScene = chapterScene.stringValue;
            try
            {
                chapterScene.stringValue = ""; config.ApplyModifiedPropertiesWithoutUndo();
                _flow.EnterBattleFromLobby();
            }
            finally
            {
                chapterScene.stringValue = originalScene; config.ApplyModifiedPropertiesWithoutUndo();
            }
            BattleController battle = null;
            var deadline = Time.realtimeSinceStartup + 20;
            while (battle == null && Time.realtimeSinceStartup < deadline) { yield return null; battle = Object.FindAnyObjectByType<BattleController>(); }
            Assert.That(battle, Is.Not.Null); yield return null;
            battle.AutoSimulate = false;
            Assert.That(battle.TrySummon(SummonRequest.Quick(0)).Accepted, Is.True);
            yield return null;
            var hero = battle.GetHero(battle.State.Cards[0].Hero);
            hero.Performance.View.enabled = false;
            var realtime = hero.GetComponentsInChildren<MonoBehaviour>().First(x => x is IMacAnhRealtimeView);
            ((IMacAnhRealtimeView)realtime).Sample(MacAnhAction.Idle, .25f, 0);
            var model = (GameObject)realtime.GetType().GetField("_model", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(realtime);
            var cameraObject = new GameObject("ActualBattleSurfaceCamera", typeof(Camera), typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData));
            var camera = cameraObject.GetComponent<Camera>();
            camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().SetRenderer(1);
            camera.orthographic = true; camera.orthographicSize = .73f * model.transform.parent.lossyScale.x;
            camera.transform.rotation = model.transform.parent.rotation;
            camera.transform.position = model.transform.parent.TransformPoint(new Vector3(0, .99f, -4));
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
            camera.nearClipPlane = .1f; camera.farClipPlane = 12; camera.cullingMask = 1 << 31;
            var transforms = model.GetComponentsInChildren<Transform>(); var oldLayers = transforms.Select(x => x.gameObject.layer).ToArray();
            foreach (var child in transforms) child.gameObject.layer = 31;
            var target = new RenderTexture(1024, 1024, 24); camera.targetTexture = target;
            yield return null; camera.Render(); var previous = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(1024, 1024, TextureFormat.RGBA32, false); image.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0); image.Apply();
            Directory.CreateDirectory(Output); File.WriteAllBytes(Output + "/ActualBattleSurface.png", image.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null; target.Release();
            Object.Destroy(target); Object.Destroy(image); Object.Destroy(cameraObject);
            for (var i = 0; i < transforms.Length; i++) transforms[i].gameObject.layer = oldLayers[i];
            battle.ReturnToMenu();
            _flow = null; deadline = Time.realtimeSinceStartup + 20;
            while ((_flow == null || _flow.Step == FrontendStep.Done) && Time.realtimeSinceStartup < deadline)
            { yield return null; _flow = Object.FindAnyObjectByType<FrontendController>(); }
            Assert.That(_flow.Step, Is.EqualTo(FrontendStep.Lobby)); Assert.That(_flow.Character, Is.SameAs(character));
            Assert.That(_flow.Server.Id, Is.EqualTo("s3"));
        }
        [UnityTest]
        public IEnumerator CaptureLobbyAndIdleCycle()
        {
            Directory.CreateDirectory(Output);
            var lobby = (LobbyScreen)_flow.Screen;
            lobby.PreviewHour = 11; lobby.Tick(_flow.Config.Lobby.daylightFadeSeconds + 1);
            lobby.LordView.enabled = false; lobby.LordView.Sample(0);
            foreach (var width in new[] { 1920, 2160, 2340, 2400 }) yield return Capture("Lobby_" + width, width, 1080);
            lobby.LordView.enabled = false;
            for (var i = 0; i < 3; i++)
            {
                lobby.LordView.Sample(lobby.LordView.Idle.length * i / 2f);
                yield return Capture("Idle_" + i, 1920, 1080);
            }
            lobby.LordView.enabled = true;
            foreach (var time in _flow.Config.Lobby.daylight)
            {
                lobby.PreviewHour = time.startHour; lobby.Tick(_flow.Config.Lobby.daylightFadeSeconds + 1);
                yield return Capture("Daylight_" + time.startHour, 1920, 1080);
            }
            var heading = lobby.Root.GetComponentsInChildren<TMP_Text>().Single(x => x.name == "LordHeading");
            Assert.That(heading.text, Is.EqualTo("Chủ Tướng"));
            var plate = heading.transform.parent;
            Assert.That(plate.GetComponentsInChildren<TMP_Text>().Length, Is.EqualTo(1), "compact plaque only shows the lord heading");
            Assert.That(plate.GetComponentsInChildren<Image>().Length, Is.Zero, "no stars or player name in the plaque");
            foreach (var letter in "ỦủƯưỚớ") Assert.That(heading.font.HasCharacter(letter, true, true), Is.True, "Vietnamese glyph " + letter);
        }
        [UnityTest]
        public IEnumerator IdleSurfaceAndEditorFrameTimes()
        {
            var view = ((LobbyScreen)_flow.Screen).LordView; view.enabled = false; view.Sample(0);
            var skins = view.Model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var meshes = skins.Select(_ => new Mesh()).ToArray();
            var baselines = new List<Vector3[]>(); var triangles = new List<int[]>();
            for (var i = 0; i < skins.Length; i++)
            {
                skins[i].BakeMesh(meshes[i], true);
                baselines.Add(meshes[i].vertices.Select(skins[i].transform.TransformPoint).ToArray()); triangles.Add(meshes[i].triangles);
                Assert.That(skins[i].quality, Is.EqualTo(SkinQuality.Bone4));
            }
            var spikes = 0; var stretch = 0; var compression = 0; var edges = 0; var samples = 61; var nonFinite = 0;
            for (var sample = 0; sample < samples; sample++)
            {
                view.Sample(view.Idle.length * sample / (samples - 1));
                for (var m = 0; m < skins.Length; m++)
                {
                    skins[m].BakeMesh(meshes[m], true);
                    var vertices = meshes[m].vertices.Select(skins[m].transform.TransformPoint).ToArray();
                    for (var j = 0; j < triangles[m].Length; j++)
                    {
                        var a = triangles[m][j]; var b = triangles[m][j % 3 == 2 ? j - 2 : j + 1];
                        var rest = Vector3.Distance(baselines[m][a], baselines[m][b]); if (rest < 1e-6f) continue;
                        var length = Vector3.Distance(vertices[a], vertices[b]); var ratio = length / rest; edges++;
                        if (float.IsNaN(ratio) || float.IsInfinity(ratio)) nonFinite++;
                        if (ratio > 3 && length - rest > .002f) spikes++;
                        if (ratio > 1.35f && length - rest > .0068f) stretch++;
                        if (ratio < .65f && rest - length > .0068f) compression++;
                    }
                }
            }
            foreach (var mesh in meshes) Object.Destroy(mesh);
            view.enabled = true;
            // Exclude the synchronous surface audit and its deferred destruction from frame timing.
            yield return new WaitForSecondsRealtime(.6f);
            var durations = new List<float>();
            for (var i = 0; i < 120; i++) { yield return null; durations.Add(Time.unscaledDeltaTime); }
            durations.Sort(); Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "/IdleSurfaceAndEditorTiming.json", "{\"samples\":" + samples + ",\"edgesChecked\":" + edges +
                ",\"spikes\":" + spikes + ",\"stretched\":" + stretch + ",\"compressed\":" + compression +
                ",\"editorAverageFps\":" + (1f / durations.Average()).ToString(System.Globalization.CultureInfo.InvariantCulture) +
                ",\"editorP95Ms\":" + (durations[113] * 1000).ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"deviceMeasured\":false}");
            Assert.That(spikes + stretch + compression + nonFinite, Is.Zero, "mesh surface across the idle cycle");
        }
        private IEnumerator Capture(string name, int width, int height)
        {
            var camera = Camera.main; var canvas = _flow.GetComponentInChildren<Canvas>();
            var target = new RenderTexture(width, height, 24);
            var oldMode = canvas.renderMode; var oldTarget = camera.targetTexture;
            camera.targetTexture = target; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            var safe = _flow.Screen.Root.Find("SafeArea").GetComponent<RectTransform>();
            var fitter = safe.GetComponent<SafeAreaFitter>(); fitter.enabled = false;
            safe.anchorMin = new Vector2(width == 1920 ? 0 : 60f / width, 24f / height);
            safe.anchorMax = new Vector2(width == 1920 ? 1 : 1 - 60f / width, 1);
            yield return null; yield return null; Canvas.ForceUpdateCanvases();
            foreach (var frame in _flow.GetComponentsInChildren<DesignFrame>()) frame.SendMessage("LateUpdate");
            foreach (var frame in _flow.GetComponentsInChildren<LobbyEdgeGroup>()) frame.SendMessage("LateUpdate");
            ((LobbyScreen)_flow.Screen).Tick(0);
            ((LobbyScreen)_flow.Screen).LordView.CaptureCamera.Render(); camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG()); Object.Destroy(image);
            RenderTexture.active = previous; camera.targetTexture = oldTarget; canvas.renderMode = oldMode;
            target.Release(); Object.Destroy(target); fitter.enabled = true; fitter.Apply();
        }
    }
}
#endif
