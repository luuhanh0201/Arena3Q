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

        public const string DecorationsAssetPath = PrototypeContentBuilder.ContentRoot + "/Maps/MapDecorations_BatTran.asset";

        private const string SlabProp = "BaguaWideStoneSlab";
        public const string CastleArtAssetPath = PrototypeContentBuilder.ContentRoot + "/Maps/CastleArt_Command.asset";

        // The drum platform (command post) is the castle of both sides; the battle-worn model (torn banners, charred
        // wood) is the same post after it has been destroyed.
        private const string CastleIntactProp = "ThreeKingdomsCommand_1";
        private const string CastleDestroyedProp = "ThreeKingdomsCommand_2";
        private const string TentProp = "ThreeKingdomsSquare";
        private const string TowerProp = "WoodenCampWatchtower";
        private const string FenceProp = "WoodenPalisadeFence";
        private const string FlagProp = "MilitaryFlagpoleBanner";

        /// <summary>
        /// First proposal for the camps (docs/BanDo/BatQuaiTran.md §5), for the player side (x &lt; 0); the enemy camp
        /// mirrors it. Kept clear of the summon zone (x −17.5…−11, y −9…9), the quick summon points, the outer lanes
        /// and the Defense spawn area (x 21–22.5, y −6…6). Pictures only, nothing blocks.
        /// </summary>
        private static readonly (string prop, Vector2 position)[] CampLayout =
        {
            (TentProp, new Vector2(-22f, 7.5f)), (TentProp, new Vector2(-22f, -7.5f)),
            (TowerProp, new Vector2(-17f, 10f)), (TowerProp, new Vector2(-17f, -10f)),
            (FenceProp, new Vector2(-22.3f, 11.8f)), (FenceProp, new Vector2(-19.3f, 11.8f)),
            (FenceProp, new Vector2(-22.3f, -11.8f)), (FenceProp, new Vector2(-19.3f, -11.8f)),
            (FlagProp, new Vector2(-17.8f, 2.6f)), (FlagProp, new Vector2(-17.8f, -2.6f)),
        };

        /// <summary>Flags marking the outer lanes (they run at y ≈ ±8 around x ±7.5).</summary>
        private static readonly (string prop, Vector2 position)[] FieldLayout =
        {
            (FlagProp, new Vector2(-7.5f, 10.5f)), (FlagProp, new Vector2(7.5f, 10.5f)),
            (FlagProp, new Vector2(-7.5f, -10.5f)), (FlagProp, new Vector2(7.5f, -10.5f)),
        };
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
            var decorations = EnsureDecorations();
            var castleArt = EnsureCastleArt();
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
                so.FindProperty("decorations").objectReferenceValue = decorations;
                SetIfEmpty(so.FindProperty("playerCastle.art"), castleArt);
                SetIfEmpty(so.FindProperty("enemyCastle.art"), castleArt);
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

        /// <summary>Creates the decorations asset with the proposed camps once; later runs keep any edits.</summary>
        private static MapDecorConfig EnsureDecorations()
        {
            var decorations = AssetDatabase.LoadAssetAtPath<MapDecorConfig>(DecorationsAssetPath);
            if (decorations != null)
                return decorations;
            Directory.CreateDirectory(Path.GetDirectoryName(DecorationsAssetPath));
            decorations = ScriptableObject.CreateInstance<MapDecorConfig>();
            AssetDatabase.CreateAsset(decorations, DecorationsAssetPath);

            var pieces = new List<(Sprite sprite, Vector2 position)>();
            foreach (var (prop, position) in CampLayout)
            {
                var sprite = FirstSprite(prop);
                if (sprite == null)
                    continue;
                pieces.Add((sprite, position));
                pieces.Add((sprite, new Vector2(-position.x, position.y)));
            }
            foreach (var (prop, position) in FieldLayout)
            {
                var sprite = FirstSprite(prop);
                if (sprite != null)
                    pieces.Add((sprite, position));
            }

            var so = new SerializedObject(decorations);
            var list = so.FindProperty("pieces");
            list.arraySize = pieces.Count;
            for (var i = 0; i < pieces.Count; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("sprite").objectReferenceValue = pieces[i].sprite;
                element.FindPropertyRelative("position").vector2Value = pieces[i].position;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(decorations);
            Debug.Log($"[Arena3Q] Created {DecorationsAssetPath} with {pieces.Count} pieces.");
            return decorations;
        }

        /// <summary>Creates the castle art (intact + destroyed command post) once; animation numbers stay tunable.</summary>
        private static CastleArtConfig EnsureCastleArt()
        {
            var art = AssetDatabase.LoadAssetAtPath<CastleArtConfig>(CastleArtAssetPath);
            if (art == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CastleArtAssetPath));
                art = ScriptableObject.CreateInstance<CastleArtConfig>();
                AssetDatabase.CreateAsset(art, CastleArtAssetPath);
            }
            var so = new SerializedObject(art);
            SetIfEmpty(so.FindProperty("intactSprite"), FirstSprite(CastleIntactProp));
            SetIfEmpty(so.FindProperty("destroyedSprite"), FirstSprite(CastleDestroyedProp));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            return art;
        }

        private static Sprite FirstSprite(string prop)
        {
            var sprites = LoadSprites(prop, out _);
            return sprites.Count > 0 ? sprites[0] : null;
        }

        private static void SetIfEmpty(SerializedProperty property, UnityEngine.Object value)
        {
            if (property != null && property.objectReferenceValue == null && value != null)
                property.objectReferenceValue = value;
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
