using System;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// Procedurally generated placeholder sprites (spec §11: no real art in the prototype). One world unit = one sprite.
    /// </summary>
    public static class PlaceholderArt
    {
        private const int Size = 64;

        private static Sprite _circle;
        private static Sprite _square;
        private static Sprite _ring;
        private static Sprite _star;

        public static Sprite Circle => _circle ??= Create("Circle", (x, y) => Disc(x, y, 1f));
        public static Sprite Square => _square ??= Create("Square", (x, y) => 1f);
        public static Sprite Ring => _ring ??= Create("Ring", (x, y) => Mathf.Min(Disc(x, y, 1f), 1f - Disc(x, y, 0.78f)));
        public static Sprite Star => _star ??= Create("Star", StarAlpha);

        /// <summary>Material for every SpriteRenderer we create. Set by BattleController from its serialized field.</summary>
        public static Material SpriteMaterial { get; set; }

        public static SpriteRenderer CreateRenderer(string name, Transform parent, Sprite sprite, Color color, Vector2 scale,
            int orderOffset = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = orderOffset;
            if (SpriteMaterial != null)
                renderer.sharedMaterial = SpriteMaterial;
            return renderer;
        }

        private static Sprite Create(string name, Func<float, float, float> alpha)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "Placeholder" + name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    // Sample at pixel centre in [-1, 1].
                    var u = (x + 0.5f) / Size * 2f - 1f;
                    var v = (y + 0.5f) / Size * 2f - 1f;
                    var a = Mathf.Clamp01(alpha(u, v));
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            // FullRect: a tight mesh would turn the anti-aliased circles into octagons.
            var sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size, 0,
                SpriteMeshType.FullRect);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        // Anti-aliased disc of the given radius (in [-1, 1] space).
        private static float Disc(float x, float y, float radius)
        {
            var d = Mathf.Sqrt(x * x + y * y);
            var edge = 2f / Size;
            return Mathf.Clamp01((radius - d) / edge);
        }

        private static float StarAlpha(float x, float y)
        {
            // Five-pointed star: compare the radius with the star outline at this angle.
            var angle = Mathf.Atan2(x, y);
            var r = Mathf.Sqrt(x * x + y * y);
            const float outer = 0.95f;
            const float inner = 0.42f;
            var sector = Mathf.PI * 2f / 5f;
            var t = Mathf.Abs(Mathf.Repeat(angle, sector) - sector * 0.5f) / (sector * 0.5f);
            var outline = Mathf.Lerp(inner, outer, t);
            return Mathf.Clamp01((outline - r) / (2f / Size));
        }
    }
}
