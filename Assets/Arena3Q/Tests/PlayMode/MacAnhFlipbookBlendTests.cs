#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Arena3Q.Tests.PlayMode
{
    public sealed class MacAnhFlipbookBlendTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private Camera _camera;
        private Material _material;
        private string Output => System.Environment.GetEnvironmentVariable("ARENA3Q_BLEND_EVIDENCE");

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;
            Object.FindAnyObjectByType<BattleController>().AutoSimulate = false;
            _material = Track(new Material(Shader.Find("Arena3Q/MacAnh Flipbook 2D")));
            var go = Track(new GameObject("Isolated flipbook camera", typeof(Camera)));
            _camera = go.GetComponent<Camera>();
            _camera.enabled = false;
            _camera.orthographic = true;
            _camera.orthographicSize = 4;
            _camera.transform.position = new Vector3(0, 0, -10);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.black;
            _camera.cullingMask = 1 << 31;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        private T Track<T>(T value) where T : Object { _objects.Add(value); return value; }

        private Sprite Frame(int width, int height, Rect rect, Vector2 pivot, Color color)
        {
            var texture = Track(new Texture2D(width, height, TextureFormat.RGBA32, false, true));
            // Magenta outside the crop makes atlas bleed observable.
            var pixels = Enumerable.Repeat(Color.magenta, width * height).ToArray();
            for (int y = (int)rect.yMin; y < rect.yMax; y++)
                for (int x = (int)rect.xMin; x < rect.xMax; x++)
                    pixels[y * width + x] = color;
            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            return Track(Sprite.Create(texture, rect, pivot, 16, 0, SpriteMeshType.FullRect));
        }

        private MacAnhVfxPlayer Synthetic(out MacAnhVfxRecipe recipe, bool loop = false)
        {
            recipe = Track(ScriptableObject.CreateInstance<MacAnhVfxRecipe>());
            recipe.layers = new[] { new MacAnhVfxRecipe.Layer
            {
                frames = new[] {
                    Frame(80, 48, new Rect(8, 8, 64, 32), new Vector2(.25f, .25f), new Color(.6f, 0, 0, 1)),
                    Frame(48, 80, new Rect(8, 8, 32, 64), new Vector2(.75f, .75f), new Color(0, .6f, 0, 1))
                },
                framesPerSecond = 2, loop = loop, fadeIn = 0, fadeOut = 0,
                dissolve = false, breath = 0, underlay = 0, glow = 1
            }};
            return Player(recipe);
        }

        private MacAnhVfxPlayer Player(MacAnhVfxRecipe recipe)
        {
            var go = Track(new GameObject("Flipbook under test"));
            var player = go.AddComponent<MacAnhVfxPlayer>();
            player.Configure(recipe, _material);
            Random.InitState(210021);
            player.Begin(MacAnhFxQuality.Medium, 10);
            foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.layer = 31;
            return player;
        }

        [Test]
        public void DifferentAtlasPagesCropsAndPivotsBlendWithoutClipping()
        {
            var player = Synthetic(out var recipe);
            var a = Pixels("Synthetic/A");
            player.Advance(.5f);
            var b = Pixels("Synthetic/B");
            player.Advance(-.25f);
            var middle = Pixels("Synthetic/Middle");
            int litA = a.Count(v => v.r > 20), litB = b.Count(v => v.g > 20);
            Assert.That(litA, Is.GreaterThan(1000));
            Assert.That(litB, Is.GreaterThan(1000));
            AssertMix(a, b, middle, .5f);
            // Compare both endpoints to independently rendered original sprites, not the new mesh.
            player.Stop();
            var reference = Track(new GameObject("Original sprite reference", typeof(SpriteRenderer)));
            reference.layer = 31;
            var renderer = reference.GetComponent<SpriteRenderer>();
            renderer.sharedMaterial = _material;
            var block = new MaterialPropertyBlock();
            block.SetFloat("_Underlay", 0);
            renderer.SetPropertyBlock(block);
            renderer.sprite = recipe.layers[0].frames[0];
            AssertPixels(a, Pixels("Synthetic/ReferenceA"), 1);
            renderer.sprite = recipe.layers[0].frames[1];
            AssertPixels(b, Pixels("Synthetic/ReferenceB"), 1);
        }

        [UnityTest]
        public IEnumerator PauseSeekSpeedLoopAndReuseKeepEffectClock()
        {
            var player = Synthetic(out var recipe, true);
            var a = Pixels();
            player.Advance(.5f);
            var b = Pixels();
            player.Advance(.25f); // Last frame -> first, halfway.
            var middle = Pixels();
            AssertMix(b, a, middle, .5f);
            yield return new WaitForSecondsRealtime(.2f);
            AssertPixels(middle, Pixels(), 0);
            player.Advance(-.5f); // First -> last, halfway.
            AssertPixels(middle, Pixels(), 1);
            player.Advance(.5f);
            AssertPixels(middle, Pixels(), 1);
            player.Stop();
            player.Begin(MacAnhFxQuality.Medium, 10);
            AssertPixels(a, Pixels(), 0);
            for (int i = 0; i < 15; i++) player.Advance(1f / 60);
            AssertPixels(middle, Pixels(), 1);
            recipe.layers[0].loop = false;
            player.Advance(3);
            AssertPixels(b, Pixels(), 0); // Non-loop holds final frame.
        }

        [Test]
        public void CaptureRealRecipesAtSixtyHz()
        {
            if (string.IsNullOrEmpty(Output)) Assert.Ignore("Set ARENA3Q_BLEND_EVIDENCE for the 60Hz evidence sequence.");
            foreach (var name in new[] { "WindSlash", "TripleFeather", "DodgeStreak", "MagicArray", "Cyclone", "Shield", "Ultimate" })
            {
                var recipe = AssetDatabase.LoadAssetAtPath<MacAnhVfxRecipe>(
                    $"Assets/Arena3Q/Art/Heroes/MacAnh/VFX/Flipbook/{name}/{name}.asset");
                Assert.That(recipe, Is.Not.Null, name);
                var player = Player(recipe);
                if (name == "MagicArray") player.SetCharge();
                player.SetBattlePresentation(recipe.referenceRadius, 100, .75f, MacAnhFxKind.WindSlash);
                _camera.orthographicSize = name == "Ultimate" || name == "MagicArray" ? 6 : 4;
                for (int frame = 0; frame <= 60; frame++)
                {
                    Pixels($"{name}/{frame:D3}");
                    player.Advance(1f / 60);
                }
                player.Stop();
            }
        }

        private Color32[] Pixels(string name = null)
        {
            var previous = _camera.targetTexture;
            var active = RenderTexture.active;
            var rt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var image = new Texture2D(256, 256, TextureFormat.RGBA32, false, true);
            try
            {
                rt.Create(); _camera.targetTexture = rt; _camera.Render(); RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply();
                if (name != null && !string.IsNullOrEmpty(Output))
                {
                    var path = Path.Combine(Output, name + ".png");
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllBytes(path, image.EncodeToPNG());
                }
                return image.GetPixels32();
            }
            finally
            {
                _camera.targetTexture = previous; RenderTexture.active = active;
                rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
            }
        }

        private static void AssertMix(Color32[] a, Color32[] b, Color32[] actual, float mix)
        {
            int maximum = 0, changed = 0;
            for (int i = 0; i < a.Length; i++)
            {
                int delta = Mathf.Max(Mathf.Abs(actual[i].r - Mathf.RoundToInt(Mathf.Lerp(a[i].r, b[i].r, mix))),
                    Mathf.Abs(actual[i].g - Mathf.RoundToInt(Mathf.Lerp(a[i].g, b[i].g, mix))),
                    Mathf.Abs(actual[i].b - Mathf.RoundToInt(Mathf.Lerp(a[i].b, b[i].b, mix))));
                maximum = Mathf.Max(maximum, delta);
                if (delta > 2) changed++;
            }
            Assert.That(changed, Is.Zero, $"Halfway image differs from linear frame mix; max byte delta {maximum}");
        }

        private static void AssertPixels(Color32[] expected, Color32[] actual, int tolerance)
        {
            int maximum = 0;
            for (int i = 0; i < expected.Length; i++)
                maximum = Mathf.Max(maximum, Mathf.Abs(expected[i].r - actual[i].r),
                    Mathf.Abs(expected[i].g - actual[i].g), Mathf.Abs(expected[i].b - actual[i].b));
            Assert.That(maximum, Is.LessThanOrEqualTo(tolerance));
        }
    }
}
#endif
