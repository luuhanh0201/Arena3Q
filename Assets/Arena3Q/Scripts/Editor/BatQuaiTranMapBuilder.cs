using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Arena3Q.Data;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>
    /// Puts the Bát Quái Trận map (docs/BanDo/BatQuaiTran.md) on every shipped stage: imports the ring geometry from
    /// BatTran_Layout.json into the formation asset (created once, so tuned rule values survive; geometry and sprites
    /// are refreshed every run), renders the missing slab sprites, imports the painted ground and sets each stage's
    /// formation, background, castles and removes the old river. Batch mode:
    /// -executeMethod Arena3Q.Editor.BatQuaiTranMapBuilder.ApplyToAllStages
    /// </summary>
    public static class BatQuaiTranMapBuilder
    {
        public const string LayoutJsonPath = PropSpriteRenderer.BatQuaiTranFolder + "/BatTran_Terrain/BatTran_Layout.json";
        public const string PaintedGroundPath = PropSpriteRenderer.BatQuaiTranFolder + "/BatTran_Terrain/BatTran_Painted.png";
        public const string FormationAssetPath = PrototypeContentBuilder.ContentRoot + "/Maps/BaguaFormation_BatTran.asset";
        /// <summary>Same vertical scale as the prop sprites (map spec §8.3); the painted ground is flat, so it fits any.</summary>
        public const float ArtVerticalScale = 0.75f;

        private const string SlabProp = "BaguaWideStoneSlab";
        private const string PillarProp = "BaguaStonePillar";
        private const string AltarProp = "BaguaRoundStoneAltar";

        // BatTran_Layout.json, as written by the terrain kit's build script.
        [Serializable] private sealed class TerrainInfo { public float sizeX, sizeZ, posX, posZ; }
        [Serializable] private sealed class RingInfo { public float radius, gateWidth, pillarWidth, altarDiameter; }
        [Serializable] private sealed class Marker { public string name, kind; public float x, y, rotY; }
        [Serializable] private sealed class Layout { public TerrainInfo terrain; public RingInfo ring; public Marker[] markers; }

        [MenuItem("Arena3Q/Apply Bat Quai Tran Map")]
        public static void ApplyToAllStages()
        {
            try
            {
                var stages = new List<StageConfig>();
                foreach (var guid in AssetDatabase.FindAssets("t:StageConfig", new[] { PrototypeContentBuilder.ContentRoot }))
                    stages.Add(AssetDatabase.LoadAssetAtPath<StageConfig>(AssetDatabase.GUIDToAssetPath(guid)));
                Apply(stages);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                throw;
            }
        }

        public static void Apply(IEnumerable<StageConfig> stages)
        {
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(LayoutJsonPath))
                         ?? throw new InvalidOperationException($"{LayoutJsonPath} could not be read.");
            EnsureSlabSprites();
            var formation = BuildFormation(layout);
            var ground = ImportPaintedGround();
            var area = new Rect(layout.terrain.posX, layout.terrain.posZ, layout.terrain.sizeX, layout.terrain.sizeZ);
            var playerBase = Find(layout, "Base_A");
            var enemyBase = Find(layout, "Base_B");

            foreach (var stage in stages)
            {
                if (stage == null)
                    continue;
                var so = new SerializedObject(stage);
                so.FindProperty("forbiddenZones").arraySize = 0;
                so.FindProperty("rivers").arraySize = 0;
                so.FindProperty("formation").objectReferenceValue = formation;
                so.FindProperty("backgroundArt").objectReferenceValue = ground;
                so.FindProperty("backgroundArtVerticalScale").floatValue = ArtVerticalScale;
                so.FindProperty("backgroundArtArea").rectValue = area;
                if (playerBase != null)
                    so.FindProperty("playerCastle").FindPropertyRelative("position").vector2Value = new Vector2(playerBase.x, playerBase.y);
                if (enemyBase != null)
                    so.FindProperty("enemyCastle").FindPropertyRelative("position").vector2Value = new Vector2(enemyBase.x, enemyBase.y);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(stage);
                Debug.Log($"[Arena3Q] Bát Quái Trận applied to {stage.name}.");
            }
            AssetDatabase.SaveAssets();
        }

        private static BaguaFormationConfig BuildFormation(Layout layout)
        {
            var formation = AssetDatabase.LoadAssetAtPath<BaguaFormationConfig>(FormationAssetPath);
            if (formation == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FormationAssetPath));
                formation = ScriptableObject.CreateInstance<BaguaFormationConfig>();
                AssetDatabase.CreateAsset(formation, FormationAssetPath);
            }

            var so = new SerializedObject(formation);
            var altar = Find(layout, "Formation_Center");
            so.FindProperty("altarCenter").vector2Value = altar != null ? new Vector2(altar.x, altar.y) : Vector2.zero;
            so.FindProperty("altarRadius").floatValue = layout.ring.altarDiameter * 0.5f;
            so.FindProperty("pillarRadius").floatValue = layout.ring.pillarWidth * 0.5f;
            so.FindProperty("slabLength").floatValue = layout.ring.gateWidth;

            var pillars = so.FindProperty("pillars");
            var gates = so.FindProperty("gates");
            pillars.arraySize = BaguaFormationConfig.GateCount;
            gates.arraySize = BaguaFormationConfig.GateCount;
            for (var k = 0; k < BaguaFormationConfig.GateCount; k++)
            {
                var pillar = Find(layout, $"Pillar_{k}") ?? throw new InvalidOperationException($"Pillar_{k} missing in {LayoutJsonPath}.");
                pillars.GetArrayElementAtIndex(k).vector2Value = new Vector2(pillar.x, pillar.y);
                var gate = Find(layout, $"Gate_{k}") ?? throw new InvalidOperationException($"Gate_{k} missing in {LayoutJsonPath}.");
                var element = gates.GetArrayElementAtIndex(k);
                element.FindPropertyRelative("position").vector2Value = new Vector2(gate.x, gate.y);
                element.FindPropertyRelative("rotY").floatValue = gate.rotY;
            }

            var pillarSprites = LoadSprites(PillarProp, out _);
            var pillarList = so.FindProperty("pillarSprites");
            pillarList.arraySize = pillarSprites.Count;
            for (var i = 0; i < pillarSprites.Count; i++)
                pillarList.GetArrayElementAtIndex(i).objectReferenceValue = pillarSprites[i];

            var slabSprites = LoadSprites(SlabProp, out var slabYaws);
            var slabList = so.FindProperty("slabSprites");
            slabList.arraySize = slabSprites.Count;
            for (var i = 0; i < slabSprites.Count; i++)
            {
                var element = slabList.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("yaw").floatValue = slabYaws[i];
                element.FindPropertyRelative("sprite").objectReferenceValue = slabSprites[i];
            }

            var altarSprites = LoadSprites(AltarProp, out _);
            so.FindProperty("altarSprite").objectReferenceValue = altarSprites.Count > 0 ? altarSprites[0] : null;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(formation);
            return formation;
        }

        /// <summary>The slab stands across each gate, so it needs one sprite per gate direction (every 45°).</summary>
        private static void EnsureSlabSprites()
        {
            var config = PropSpriteRenderer.EnsureBatQuaiTranConfig();
            var slab = config.props.Find(p => p.name == SlabProp);
            if (slab == null)
                return;
            var wanted = new List<float>();
            for (var yaw = 0; yaw < 360; yaw += 45)
                wanted.Add(yaw);
            var changed = slab.yaws.Count != wanted.Count;
            if (changed)
            {
                slab.yaws = wanted;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
            LoadSprites(SlabProp, out var have);
            if (!changed && have.Count >= wanted.Count)
                return;
            if (slab.model == null)
            {
                Debug.LogWarning($"[Arena3Q] {SlabProp}: the source model is missing (gitignored Meshy file), slab sprites not rendered.");
                return;
            }
            PropSpriteRenderer.Render(config);
            // The single-yaw sprite from before is replaced by the per-direction ones.
            var single = $"{config.outputFolder}/{SlabProp}.png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(single) != null)
                AssetDatabase.DeleteAsset(single);
        }

        /// <summary>Sprites named &lt;prop&gt;.png or &lt;prop&gt;_&lt;yaw&gt;.png in the prop sprite folder, sorted by yaw.</summary>
        private static List<Sprite> LoadSprites(string prop, out List<float> yaws)
        {
            var found = new List<(float yaw, Sprite sprite)>();
            var folder = PropSpriteRenderer.BatQuaiTranFolder + "/Sprites";
            if (AssetDatabase.IsValidFolder(folder))
            {
                foreach (var guid in AssetDatabase.FindAssets($"{prop} t:Sprite", new[] { folder }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var file = Path.GetFileNameWithoutExtension(path);
                    float yaw;
                    if (file == prop)
                        yaw = 0f;
                    else if (!file.StartsWith(prop + "_", StringComparison.Ordinal)
                             || !float.TryParse(file.Substring(prop.Length + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out yaw))
                        continue;
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null)
                        found.Add((yaw, sprite));
                }
            }
            found.Sort((a, b) => a.yaw.CompareTo(b.yaw));
            yaws = found.ConvertAll(f => f.yaw);
            return found.ConvertAll(f => f.sprite);
        }

        private static Sprite ImportPaintedGround()
        {
            AssetDatabase.ImportAsset(PaintedGroundPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(PaintedGroundPath)
                           ?? throw new InvalidOperationException($"{PaintedGroundPath} not found.");
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(PaintedGroundPath);
        }

        private static Marker Find(Layout layout, string name)
        {
            foreach (var marker in layout.markers)
            {
                if (marker.name == name)
                    return marker;
            }
            return null;
        }
    }
}
