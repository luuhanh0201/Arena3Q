#if UNITY_EDITOR
using System.Collections;
using Arena3Q.Gameplay;
using Arena3Q.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Arena3Q.Tests.PlayMode
{
    public sealed class BattlefieldViewportTests
    {
        private BattleController _battle;
        private BattleCameraController _rig;
        private BattlefieldEnvironment _environment;
        [UnitySetUp]
        public IEnumerator Load()
        {
            BattleLaunch.Clear(); UiSafeArea.OverrideArea = null;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Arena3Q/Scenes/Battlefield_HighQuality.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            _battle = Object.FindAnyObjectByType<BattleController>(); _battle.AutoSimulate = false;
            _rig = Object.FindAnyObjectByType<BattleCameraController>();
            _environment = Object.FindAnyObjectByType<BattlefieldEnvironment>();
        }
        [TearDown] public void Reset() { UiSafeArea.OverrideArea = null; }

        private void AssertViewportInsideMap()
        {
            var bounds = _battle.Stage.MapBounds;
            foreach (var corner in new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one })
            {
                var pixel = _rig.Camera.ViewportToScreenPoint(corner);
                var point = _rig.ScreenToWorld(pixel);
                Assert.That(point.x, Is.InRange(bounds.xMin - .02f, bounds.xMax + .02f), "horizontal edge at " + corner);
                Assert.That(point.y, Is.InRange(bounds.yMin - .02f, bounds.yMax + .02f), "vertical edge at " + corner);
            }
        }

        [UnityTest]
        public IEnumerator StartupZoomOutPanAndTilt_NeverRevealOutsideTheGround()
        {
            AssertViewportInsideMap();
            foreach (float aspect in new[] { 2340f / 1080f, 20f / 9f, 16f / 9f, 4f / 3f })
                foreach (float tilt in new[] { 0f, 40f, 45f })
                {
                    _rig.Camera.aspect = aspect; _battle.ViewTiltDegrees = tilt;
                    yield return null; AssertViewportInsideMap();
                    _rig.Zoom(1000f, new Vector2(Screen.width * .8f, Screen.height * .7f));
                    AssertViewportInsideMap();
                    foreach (var delta in new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down })
                    { _rig.Pan(delta * 100000f); AssertViewportInsideMap(); }
                }
        }

        [UnityTest]
        public IEnumerator ExternalFitAndAspectChanges_ClampBackBeforeRendering()
        {
            foreach (float aspect in new[] { 4f / 3f, 20f / 9f })
            {
                _rig.Camera.aspect = aspect; _environment.FitCamera();
                yield return null; AssertViewportInsideMap();
                _rig.Camera.orthographicSize = 100f;
                yield return null; AssertViewportInsideMap();
            }
        }

        [UnityTest]
        public IEnumerator Hud_ReflowsWhenTransientSystemBarsAppearAndDisappear()
        {
            var safe = Object.FindAnyObjectByType<UiSafeArea>(); Assert.That(safe, Is.Not.Null);
            var inset = new Rect(Screen.width * .04f, Screen.height * .02f, Screen.width * .90f, Screen.height * .96f);
            UiSafeArea.OverrideArea = inset; safe.Apply(); Canvas.ForceUpdateCanvases();
            var canvas = safe.GetComponentInParent<Canvas>();
            foreach (var name in new[] { "Stage", "Timer", "Info", "CardBar" })
            {
                var rect = (RectTransform)safe.transform.Find(name); Assert.That(rect, Is.Not.Null, name);
                var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                foreach (var point in corners)
                {
                    var screen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, point);
                    Assert.That(screen.x, Is.InRange(inset.xMin - 1f, inset.xMax + 1f), name);
                    Assert.That(screen.y, Is.InRange(inset.yMin - 1f, inset.yMax + 1f), name);
                }
            }
            UiSafeArea.OverrideArea = new Rect(0, 0, Screen.width, Screen.height); yield return null;
            var root = (RectTransform)safe.transform;
            Assert.That(root.anchorMin, Is.EqualTo(Vector2.zero)); Assert.That(root.anchorMax, Is.EqualTo(Vector2.one));
        }
    }
}
#endif
