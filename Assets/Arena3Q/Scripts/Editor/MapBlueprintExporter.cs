using System;
using System.Collections.Generic;
using System.IO;
using Arena3Q.Core;
using Arena3Q.Data;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>
    /// Exports map blueprints for artists / image generators, rasterised straight from the stage data with the same
    /// geometry the game uses, in the fixed 3/4 art view (docs/BanDo/MapSpecification.md §8.3):
    ///   &lt;stage&gt;_layout.png – grid, castles, river, bridges, summon zone, markers, HUD margins (to read);
    ///   &lt;stage&gt;_mask.png   – flat colour areas only (to paint over / use as an img2img or ControlNet guide).
    /// Batch mode: -executeMethod Arena3Q.Editor.MapBlueprintExporter.ExportAll
    /// </summary>
    public static class MapBlueprintExporter
    {
        public const string OutputFolder = "docs/art/blueprints";

        /// <summary>Provisional (map spec §8.3): on-screen vertical scale of the 3/4 art view.</summary>
        public const float ArtVerticalScale = 0.75f;

        /// <summary>Blueprint pixels per logic unit horizontally (final art is 128; blueprints are 1/4 scale).</summary>
        public const int PixelsPerUnit = 32;

        private const float BottomHudMargin = 4f;
        private const float TopHudMargin = 2.5f;

        // Mask palette (also listed in docs/BanDo/MapArtGuide.md).
        private static readonly Color32 Grass = new Color32(0x5C, 0x8C, 0x4A, 0xFF);
        private static readonly Color32 Water = new Color32(0x3A, 0x7B, 0xD5, 0xFF);
        private static readonly Color32 Bridge = new Color32(0x8B, 0x5A, 0x2B, 0xFF);
        private static readonly Color32 Blocked = new Color32(0x4A, 0x4A, 0x58, 0xFF);
        private static readonly Color32 PlayerCastle = new Color32(0x3D, 0x6F, 0xD9, 0xFF);
        private static readonly Color32 EnemyCastle = new Color32(0xC0, 0x39, 0x2B, 0xFF);

        [MenuItem("Arena3Q/Export Map Blueprints")]
        public static void ExportAll()
        {
            try
            {
                Directory.CreateDirectory(OutputFolder);
                foreach (var guid in AssetDatabase.FindAssets("t:StageConfig", new[] { PrototypeContentBuilder.ContentRoot }))
                {
                    var stage = AssetDatabase.LoadAssetAtPath<StageConfig>(AssetDatabase.GUIDToAssetPath(guid));
                    Export(stage, layout: true);
                    Export(stage, layout: false);
                }
                Debug.Log($"[Arena3Q] Map blueprints written to {Path.GetFullPath(OutputFolder)}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                throw;
            }
        }

        private static void Export(StageConfig stage, bool layout)
        {
            var bounds = stage.MapBounds;
            var width = Mathf.RoundToInt(bounds.width * PixelsPerUnit);
            var rowsPerUnit = PixelsPerUnit * ArtVerticalScale;
            var height = Mathf.RoundToInt(bounds.height * rowsPerUnit);

            // Water is drawn along the whole centre line (bridges go on top), so use rivers without crossings here.
            var rivers = stage.CreateRiverObstacles();
            var water = new List<RiverObstacle>();
            var bridges = new List<(RiverCrossing crossing, float riverWidth)>();
            foreach (var obstacle in rivers)
            {
                if (obstacle is not RiverObstacle river)
                    continue;
                water.Add(new RiverObstacle(river.Path, river.Width, Array.Empty<(Vector2, float)>()));
                foreach (var crossing in river.Crossings)
                    bridges.Add((crossing, river.Width));
            }

            var pixels = new Color32[width * height];
            for (var row = 0; row < height; row++)
            {
                var y = bounds.yMin + (row + 0.5f) / rowsPerUnit;
                for (var column = 0; column < width; column++)
                {
                    var x = bounds.xMin + (column + 0.5f) / PixelsPerUnit;
                    var p = new Vector2(x, y);
                    var colour = Ground(stage, water, bridges, p);
                    if (layout)
                        colour = Overlay(stage, p, colour, rowsPerUnit);
                    pixels[row * width + column] = colour;
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            var file = Path.Combine(OutputFolder, $"{stage.StageId}_{(layout ? "layout" : "mask")}.png");
            File.WriteAllBytes(file, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        // Flat areas: grass, rectangular forbidden zones, water, bridges, castle footprints.
        private static Color32 Ground(StageConfig stage, List<RiverObstacle> water,
            List<(RiverCrossing crossing, float riverWidth)> bridges, Vector2 p)
        {
            var colour = Grass;
            foreach (var zone in stage.ForbiddenZones)
            {
                if (zone.Contains(p))
                    colour = Blocked;
            }
            foreach (var river in water)
            {
                if (river.Contains(p, 0f))
                    colour = Water;
            }
            foreach (var (crossing, riverWidth) in bridges)
            {
                var local = p - crossing.Center;
                var along = Vector2.Dot(local, crossing.Along);
                var across = Vector2.Dot(local, new Vector2(-crossing.Along.y, crossing.Along.x));
                if (Mathf.Abs(along) <= crossing.Width * 0.5f && Mathf.Abs(across) <= (riverWidth + 1f) * 0.5f)
                    colour = Bridge;
            }
            if (Vector2.Distance(p, stage.PlayerCastle.Position) <= 1.4f)
                colour = PlayerCastle;
            if (stage.StageType == StageType.Attack && Vector2.Distance(p, stage.EnemyCastle.Position) <= 1.4f)
                colour = EnemyCastle;
            return colour;
        }

        // Reading aids: HUD margins, grid, summon zone, quick summon points, defenders, wave spawns, map border.
        private static Color32 Overlay(StageConfig stage, Vector2 p, Color32 colour, float rowsPerUnit)
        {
            var bounds = stage.MapBounds;
            if (p.y < bounds.yMin + BottomHudMargin || p.y > bounds.yMax - TopHudMargin)
                colour = Darken(colour, 0.65f);

            var gx = p.x - bounds.xMin;
            var gy = p.y - bounds.yMin;
            var onMajor = Near(gx, 4f, 1.5f / PixelsPerUnit) || Near(gy, 4f, 1.5f / rowsPerUnit);
            var onMinor = Near(gx, 1f, 0.5f / PixelsPerUnit) || Near(gy, 1f, 0.5f / rowsPerUnit);
            if (onMajor)
                colour = Blend(colour, new Color32(255, 255, 255, 255), 0.45f);
            else if (onMinor)
                colour = Blend(colour, new Color32(255, 255, 255, 255), 0.15f);

            var zone = stage.SummonZone;
            if (zone.Contains(p) && !InsideBy(zone, p, 2f / PixelsPerUnit))
                colour = new Color32(0x9F, 0xD8, 0xFF, 0xFF);

            foreach (var point in stage.QuickSummonPoints)
            {
                var d = Vector2.Distance(p, point);
                if (d >= 0.4f && d <= 0.55f)
                    colour = new Color32(255, 255, 255, 255);
            }

            if (stage.StageType == StageType.Attack)
            {
                foreach (var defender in stage.Defenders)
                {
                    if (defender != null && Vector2.Distance(p, defender.Position) <= 0.32f)
                        colour = new Color32(0x8E, 0x1B, 0x1B, 0xFF);
                }
            }
            else if (stage.Waves != null)
            {
                foreach (var wave in stage.Waves)
                {
                    foreach (var spawn in wave.Spawns)
                    {
                        if (spawn != null && Vector2.Distance(p, spawn.SpawnPoint) <= 0.5f)
                            colour = new Color32(0xF2, 0x9B, 0x1F, 0xFF);
                    }
                }
            }

            if (!InsideBy(bounds, p, 2f / PixelsPerUnit))
                colour = new Color32(0x10, 0x10, 0x10, 0xFF);
            return colour;
        }

        private static bool Near(float value, float step, float tolerance)
        {
            var remainder = Mathf.Repeat(value, step);
            return remainder < tolerance || step - remainder < tolerance;
        }

        private static bool InsideBy(Rect rect, Vector2 p, float inset) =>
            p.x >= rect.xMin + inset && p.x <= rect.xMax - inset && p.y >= rect.yMin + inset && p.y <= rect.yMax - inset;

        private static Color32 Darken(Color32 c, float factor) =>
            new Color32((byte)(c.r * factor), (byte)(c.g * factor), (byte)(c.b * factor), c.a);

        private static Color32 Blend(Color32 a, Color32 b, float t) =>
            new Color32((byte)Mathf.Lerp(a.r, b.r, t), (byte)Mathf.Lerp(a.g, b.g, t), (byte)Mathf.Lerp(a.b, b.b, t), 255);
    }
}
