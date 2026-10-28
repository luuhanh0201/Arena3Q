#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Gameplay;
using Arena3Q.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Arena3Q.Tests.PlayMode
{
    /// <summary>
    /// The release battle as the player sees it (user 2026-10-28): fixed diagonal camera, outer ground and forest (never
    /// the empty background, at any zoom), the army stance button closed and open. Captures with the HUD go to
    /// NghiemThu/UI/GocNhinCheo_20261028/ingame_*.png at the Redmi's 2340×1080.
    /// </summary>
    public sealed class ReleaseBattleViewTests
    {
        private const string Scene = "Assets/Arena3Q/Scenes/Battlefield_HighQuality.unity";
        private const string Evidence = "NghiemThu/UI/GocNhinCheo_20261028";
        private const int Width = 2340, Height = 1080;
        private BattleController _battle;
        private BattleCameraController _camera;
        private BattleHud _hud;
        private RenderTexture _target;

        [UnitySetUp]
        public IEnumerator Load()
        {
            BattleLaunch.Clear();
            UiSafeArea.OverrideArea = null;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            _battle = Object.FindAnyObjectByType<BattleController>();
            _camera = Object.FindAnyObjectByType<BattleCameraController>();
            _hud = Object.FindAnyObjectByType<BattleHud>();
            _battle.AutoSimulate = false;
            // Render at the phone's size so the camera limits and the HUD use its aspect.
            _target = new RenderTexture(Width, Height, 24) { antiAliasing = 4 };
            _camera.Camera.targetTexture = _target;
            _camera.Camera.aspect = Width / (float)Height;
            _battle.Restart();
            yield return null;
        }

        [TearDown]
        public void Release()
        {
            if (_camera != null && _camera.Camera != null)
                _camera.Camera.targetTexture = null;
            if (_target != null)
            {
                _target.Release();
                Object.Destroy(_target);
            }
        }

        private void Run(float seconds)
        {
            for (var t = 0f; t < seconds; t += 0.05f)
                _battle.Simulate(0.05f);
        }

        private void AssertViewOnTheGround()
        {
            var cam = _camera.Camera;
            var ground = _camera.GroundBounds;
            foreach (var corner in new[] { Vector2.zero, new Vector2(cam.pixelWidth, 0f), new Vector2(0f, cam.pixelHeight),
                         new Vector2(cam.pixelWidth, cam.pixelHeight) })
            {
                var point = _camera.ScreenToWorld(corner);
                Assert.That(ground.Contains(point) || Vector2.Distance(point, ClampTo(ground, point)) < 0.05f, Is.True,
                    $"view corner {corner} at {point} lies outside the built ground {ground}");
            }
        }

        private static Vector2 ClampTo(Rect rect, Vector2 point) =>
            new Vector2(Mathf.Clamp(point.x, rect.xMin, rect.xMax), Mathf.Clamp(point.y, rect.yMin, rect.yMax));

        [UnityTest, Timeout(240000)]
        public IEnumerator DiagonalView_WithOuterGround_AndStanceButton_InTheRealBattle()
        {
            Assert.That(_battle.Stance, Is.EqualTo(ArmyStance.Hold));
            for (var i = 0; i < 3 && i < _battle.Lineup.Count; i++)
                _battle.TrySummon(SummonRequest.Quick(i));
            Run(2f);
            yield return null;
            yield return null;
            AssertViewOnTheGround();
            Capture("ingame_1_giu_macdinh.png");

            var stance = _hud.Stance;
            Assert.That(stance.MainLabel, Is.EqualTo("Giữ"));
            stance.Open();
            for (var t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
                yield return null;
            Assert.That(stance.OfferedStances, Is.EqualTo(new[] { ArmyStance.Attack, ArmyStance.Defend }));
            Capture("ingame_2_mo_lenh.png");

            stance.Choose(ArmyStance.Attack);
            Assert.That(_battle.Stance, Is.EqualTo(ArmyStance.Attack));
            Assert.That(stance.IsOpen, Is.False, "closes after a choice");
            Assert.That(stance.MainLabel, Is.EqualTo("Tấn công"));
            Run(8f);
            for (var t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
                yield return null;
            Assert.That(_battle.LivingHeroes.All(h => h.Brain.Stance == ArmyStance.Attack), Is.True);
            Capture("ingame_3_tancong.png");

            var cam = _camera.Camera;
            var centre = new Vector2(cam.pixelWidth, cam.pixelHeight) * 0.5f;
            _camera.Zoom(1000f, centre);
            yield return null;
            AssertViewOnTheGround();
            Capture("ingame_4_thunho_toida.png");

            _camera.Zoom(0.001f, centre);
            _camera.FocusOn(_battle.LivingHeroes.First().Position);
            for (var t = 0f; t < 2f; t += Time.unscaledDeltaTime)
                yield return null;
            AssertViewOnTheGround();
            Capture("ingame_5_phongto_toida.png");

            foreach (var delta in new[] { new Vector2(1f, 1f), new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f) })
            {
                _camera.Pan(delta * 100000f);
                yield return null;
                AssertViewOnTheGround();
            }
            Capture("ingame_6_keo_goc.png");
        }

        private void Capture(string fileName)
        {
            var cam = _camera.Camera;
            var canvas = _hud.GetComponentInChildren<Canvas>();
            var previousMode = canvas.renderMode;
            // The battle camera is turned into the scene's Y-up frame while it renders (BattlefieldLegacyBridge), so the
            // HUD is drawn separately by its own camera on a transparent target and laid over the battle picture, as the
            // overlay canvas is in game.
            var uiCamera = new GameObject("CaptureUiCamera").AddComponent<Camera>();
            var hudTarget = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            try
            {
                cam.Render();
                var scene = Read(_target, TextureFormat.RGB24);
                uiCamera.transform.position = new Vector3(0f, 0f, -5000f);
                uiCamera.orthographic = true;
                uiCamera.clearFlags = CameraClearFlags.SolidColor;
                uiCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                uiCamera.cullingMask = 1 << canvas.gameObject.layer;
                uiCamera.targetTexture = hudTarget;
                uiCamera.nearClipPlane = 0.1f;
                uiCamera.farClipPlane = 10f;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = uiCamera;
                canvas.planeDistance = 1f;
                Canvas.ForceUpdateCanvases();
                uiCamera.Render();
                var hud = Read(hudTarget, TextureFormat.RGBA32);
                var pixels = scene.GetPixels32();
                var over = hud.GetPixels32();
                for (var i = 0; i < pixels.Length; i++)
                {
                    // UI blending onto a clear target leaves colour premultiplied and alpha squared.
                    var a = Mathf.Sqrt(over[i].a / 255f);
                    pixels[i] = new Color32((byte)Mathf.Min(255f, over[i].r + pixels[i].r * (1f - a)),
                        (byte)Mathf.Min(255f, over[i].g + pixels[i].g * (1f - a)), (byte)Mathf.Min(255f, over[i].b + pixels[i].b * (1f - a)), 255);
                }
                scene.SetPixels32(pixels);
                scene.Apply();
                Directory.CreateDirectory(Evidence);
                File.WriteAllBytes(Path.Combine(Evidence, fileName), scene.EncodeToPNG());
                Object.Destroy(scene);
                Object.Destroy(hud);
            }
            finally
            {
                canvas.renderMode = previousMode;
                uiCamera.targetTexture = null;
                Object.Destroy(uiCamera.gameObject);
                hudTarget.Release();
                Object.Destroy(hudTarget);
            }
        }

        private static Texture2D Read(RenderTexture source, TextureFormat format)
        {
            var active = RenderTexture.active;
            RenderTexture.active = source;
            var texture = new Texture2D(Width, Height, format, false);
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = active;
            return texture;
        }
    }
}
#endif
