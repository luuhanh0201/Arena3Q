using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    // Append only: prefabs store the kind as its number.
    public enum MacAnhFxKind { WindSlash, TripleFeather, MagicArray, Cyclone, Shield, Ultimate, Afterimage, CounterSlash, Impact, UltimateAura, DodgeStreak }
    public enum MacAnhFxQuality { Low, Medium, High }

    /// <summary>Small native 2D meshes. No combat rules, physics or full-screen alpha cards.</summary>
    public static class MacAnhFxGeometry
    {
        private sealed class Builder
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Color> Colors = new List<Color>();
            public readonly List<int> Triangles = new List<int>();
            public void Stroke(List<Vector2> points, float width, Color color, bool tapered = false)
            {
                var start = Vertices.Count;
                for (var i = 0; i < points.Count; i++)
                {
                    var direction = (points[Mathf.Min(i + 1, points.Count - 1)] - points[Mathf.Max(i - 1, 0)]).normalized;
                    var normal = new Vector2(-direction.y, direction.x);
                    var scale = tapered ? Mathf.Max(.03f, Mathf.Sin(Mathf.PI * i / (points.Count - 1))) : 1f;
                    Vertices.Add(points[i] - normal * width * scale);
                    Vertices.Add(points[i] + normal * width * scale);
                    Colors.Add(color); Colors.Add(color);
                    if (i == 0) continue;
                    var a = start + (i - 1) * 2;
                    Triangles.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
                }
            }
            public void Glow(List<Vector2> points, float width, Color color, bool tapered = false)
            {
                var glow = color; glow.a *= .14f;
                Stroke(points, width * 3.4f, glow, tapered);
                Stroke(points, width, color, tapered);
                var core = Color.Lerp(color, Color.white, .65f); core.a = color.a;
                Stroke(points, width * .23f, core, tapered);
            }
            public void Ring(Vector2 centre, float radius, int segments, Color color, float width)
            {
                var points = new List<Vector2>();
                for (var i = 0; i <= segments; i++)
                {
                    var a = i * Mathf.PI * 2f / segments;
                    points.Add(centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                }
                Glow(points, width, color);
            }
            public Mesh Finish(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(Vertices); mesh.SetColors(Colors); mesh.SetTriangles(Triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
        public static Mesh Create(MacAnhFxKind kind, MacAnhFxQuality quality, int layer)
        {
            var b = new Builder();
            var segments = quality == MacAnhFxQuality.High ? 56 : quality == MacAnhFxQuality.Medium ? 32 : 20;
            var particles = quality == MacAnhFxQuality.High ? 14 : quality == MacAnhFxQuality.Medium ? 9 : 5;
            var violet = new Color(.59f, .34f, .94f, .9f);
            var light = new Color(.84f, .73f, 1f, .95f);
            var gold = new Color(.90f, .77f, .49f, .85f);
            var color = layer == 1 ? gold : layer == 2 ? light : violet;
            if (kind == MacAnhFxKind.WindSlash || kind == MacAnhFxKind.CounterSlash)
            {
                var points = new List<Vector2>();
                for (var i = 0; i <= segments; i++)
                {
                    var a = Mathf.Lerp(-1.45f, 1.45f, (float)i / segments);
                    points.Add(new Vector2(Mathf.Cos(a) * .5f - .12f - layer * .06f, Mathf.Sin(a) * .56f));
                }
                b.Glow(points, layer == 0 ? .045f : .012f, color, true);
            }
            else if (kind == MacAnhFxKind.TripleFeather)
            {
                var points = new List<Vector2>();
                for (var i = 0; i <= segments; i++)
                {
                    var t = (float)i / segments;
                    points.Add(new Vector2(t * .78f - .39f, Mathf.Sin(t * Mathf.PI) * .06f + (layer - 1) * .018f));
                }
                b.Glow(points, layer == 0 ? .055f : .012f, color, true);
            }
            else if (kind == MacAnhFxKind.MagicArray || kind == MacAnhFxKind.Ultimate)
            {
                var radius = layer == 0 ? 1f : layer == 1 ? .81f : .57f;
                var centre = kind == MacAnhFxKind.Ultimate && layer == 2 ? Vector2.up * 1.6f : Vector2.zero;
                b.Ring(centre, radius, segments, color, .012f);
                for (var i = 0; i < particles; i++)
                {
                    var angle = i * Mathf.PI * 2f / particles;
                    var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    var tangent = new Vector2(-direction.y, direction.x);
                    var p = centre + direction * radius * .91f;
                    b.Glow(new List<Vector2> { p - tangent * .035f, p + direction * .07f, p + tangent * .035f, p - direction * .025f, p - tangent * .035f }, .004f, color);
                    if (layer == 2)
                    {
                        var bottom = direction * radius * .85f;
                        var top = kind == MacAnhFxKind.Ultimate ? centre + direction * radius * .6f :
                            bottom + Vector2.up * (.5f + .45f * Mathf.Sin(i * 2.2f));
                        b.Glow(new List<Vector2> { bottom, Vector2.Lerp(bottom, top, .5f), top }, .016f, light, true);
                    }
                }
                if (layer == 1)
                {
                    b.Ring(centre, radius * .72f, 8, color, .008f);
                    for (var i = 0; i < 8; i++)
                    {
                        var a = i * Mathf.PI / 4;
                        var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var tangent = new Vector2(-d.y, d.x);
                        for (var row = 0; row < 3; row++)
                        {
                            var p = centre + d * (radius * .43f + row * .045f);
                            if ((i & (1 << row)) != 0)
                                b.Glow(new List<Vector2> { p - tangent * .08f, p + tangent * .08f }, .006f, color);
                            else
                            {
                                b.Glow(new List<Vector2> { p - tangent * .08f, p - tangent * .022f }, .006f, color);
                                b.Glow(new List<Vector2> { p + tangent * .022f, p + tangent * .08f }, .006f, color);
                            }
                        }
                    }
                }
            }
            else if (kind == MacAnhFxKind.Cyclone)
            {
                var points = new List<Vector2>();
                for (var i = 0; i <= segments * 2; i++)
                {
                    var t = (float)i / (segments * 2);
                    var a = t * Mathf.PI * 7f + layer * 1.5f;
                    var radius = Mathf.Lerp(.30f, .63f, t);
                    points.Add(new Vector2(Mathf.Cos(a) * radius, t * 1.45f + Mathf.Sin(a) * radius * .24f));
                }
                b.Glow(points, layer == 0 ? .035f : .012f, color, true);
                if (layer == 1) b.Ring(Vector2.zero, .43f, segments, color, .012f);
            }
            else if (kind == MacAnhFxKind.Shield)
            {
                b.Ring(Vector2.up * .7f, .67f - layer * .035f, segments, color, .012f);
                if (layer == 1)
                    for (var i = 0; i < 6; i++)
                    {
                        var a = i * Mathf.PI / 3f;
                        var p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * .32f + Vector2.up * .7f;
                        b.Ring(p, .13f, 6, color * new Color(1, 1, 1, .65f), .006f);
                    }
            }
            else if (kind == MacAnhFxKind.Impact)
            {
                for (var i = 0; i < particles / 2; i++)
                {
                    var a = i * Mathf.PI * 2f / (particles / 2) + layer * .2f;
                    var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    b.Glow(new List<Vector2> { d * .08f, d * .3f + new Vector2(-d.y, d.x) * .04f, d * .5f }, .014f, color, true);
                }
            }
            else
            {
                b.Ring(Vector2.zero, .35f + layer * .04f, segments, color, .012f);
                b.Glow(new List<Vector2> { new Vector2(-.3f, .1f), new Vector2(-.1f, .5f), new Vector2(.2f, .9f) }, .02f, color, true);
            }
            return b.Finish($"MacAnh_{kind}_{quality}_{layer}");
        }
    }
}
