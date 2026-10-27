using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Arena3Q.Editor
{
    public static class CurrentGameRelease
    {
        [Serializable] private sealed class PackedContent
        {
            public string platform, startupScene, battleScene;
            public string[] requiredAssets, scenes;
        }
        public const string Evidence = "NghiemThu/Release/Latest_20261025";
        public static GameReleaseSettings Settings => AssetDatabase.LoadAssetAtPath<GameReleaseSettings>(GameReleaseSettings.AssetPath)
            ?? throw new InvalidOperationException("Apply the current game release content first.");
        public static string[] ScenePaths => Settings.scenes.Select(AssetDatabase.GetAssetPath).ToArray();

        [MenuItem("Arena3Q/Build/Apply Latest Game to Mobile and Web")]
        public static void Apply()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GameReleaseSettings>(GameReleaseSettings.AssetPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<GameReleaseSettings>();
                T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path)
                    ?? throw new InvalidOperationException("Missing release source: " + path);
                const string content = "Assets/Arena3Q/Content/";
                settings.frontend = Load<FrontendConfig>(content + "Frontend/FrontendConfig.asset");
                settings.scenes = new[] { Load<SceneAsset>("Assets/Arena3Q/Scenes/Frontend.unity"),
                    Load<SceneAsset>(BattlefieldBuilder.ScenePath), Load<SceneAsset>("Assets/Arena3Q/Scenes/Battle.unity") };
                settings.tutorialSource = Load<StageConfig>(content + "Stages/Stage_Tutorial_01.asset");
                settings.attackSource = Load<StageConfig>(content + "Stages/Stage_Attack_01.asset");
                settings.defenseSource = Load<StageConfig>(content + "Stages/Stage_Defense_01.asset");
                settings.attackLayout = Load<StageConfig>(BattlefieldBuilder.Content + "/Stage_Attack.asset");
                settings.defenseLayout = Load<StageConfig>(BattlefieldBuilder.Content + "/Stage_Defense.asset");
                AssetDatabase.CreateAsset(settings, GameReleaseSettings.AssetPath);
            }
            EnsureFolder(settings.campaignFolder);
            var tutorial = Adapt(settings.tutorialSource, settings.defenseLayout, settings.campaignFolder);
            var attack = Adapt(settings.attackSource, settings.attackLayout, settings.campaignFolder);
            Adapt(settings.defenseSource, settings.defenseLayout, settings.campaignFolder);
            var frontend = new SerializedObject(settings.frontend);
            frontend.FindProperty("tutorialStage").objectReferenceValue = tutorial;
            frontend.FindProperty("defaultStage").objectReferenceValue = attack;
            frontend.FindProperty("battleSceneName").stringValue = settings.scenes[1].name;
            frontend.ApplyModifiedPropertiesWithoutUndo();
            LobbyContentBuilder.ApplyTo(settings.frontend);
            EditorUtility.SetDirty(settings.frontend);
            AssetDatabase.SaveAssets();
            EditorBuildSettings.scenes = ScenePaths.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
            Validate();
            Debug.Log("[CurrentGameRelease] Mobile and web now share Frontend -> " + settings.frontend.BattleSceneName);
        }

        public static void Validate()
        {
            if (Settings.frontend == null || Settings.frontend.Lobby == null || !Settings.frontend.Steps.Contains(FrontendStep.Lobby))
                throw new InvalidOperationException("Release requires the integrated frontend lobby.");
            var settings = Settings;
            var scenes = ScenePaths;
            LobbyCompressionQa.ValidateReleaseScenes(scenes);
            if (scenes.Length < 2 || scenes.Any(p => !File.Exists(p)) || settings.frontend == null ||
                Path.GetFileNameWithoutExtension(scenes[0]) != settings.frontend.FrontendSceneName ||
                Path.GetFileNameWithoutExtension(scenes[1]) != settings.frontend.BattleSceneName || scenes[1] != BattlefieldBuilder.ScenePath)
                throw new InvalidOperationException("Release scenes must start with Frontend and the current Battlefield.");
            foreach (var stage in new[] { settings.frontend.TutorialStage, settings.frontend.DefaultStage })
            {
                if (stage == null || !AssetDatabase.GetAssetPath(stage).StartsWith(settings.campaignFolder + "/", StringComparison.Ordinal) ||
                    stage.MapBounds != settings.attackLayout.MapBounds || stage.Formation != settings.attackLayout.Formation || stage.BackgroundArt != null)
                    throw new InvalidOperationException("Frontend still points to legacy map content.");
                var errors = new List<string>(); stage.Validate(errors);
                if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            }
        }

        public static void VerifyBuild(BuildReport report)
        {
            var config = Settings.frontend;
            var required = new[] { AssetDatabase.GetAssetPath(config), AssetDatabase.GetAssetPath(config.TutorialStage),
                AssetDatabase.GetAssetPath(config.DefaultStage), AssetDatabase.GetAssetPath(config.Lobby) };
            var packed = new HashSet<string>(report.packedAssets.SelectMany(p => p.contents).Select(c => c.sourceAssetPath));
            foreach (string asset in required)
                if (!packed.Contains(asset)) throw new InvalidOperationException("Built player does not contain the current campaign content: " + asset);
            LobbyCompressionQa.VerifyBuild(report, "Release");
            Directory.CreateDirectory(Evidence);
            var proof = new PackedContent { platform = report.summary.platform.ToString(), startupScene = config.FrontendSceneName,
                battleScene = config.BattleSceneName, requiredAssets = required, scenes = ScenePaths };
            File.WriteAllText(Evidence + "/" + proof.platform + "-content.json", JsonUtility.ToJson(proof, true));
        }

        private static StageConfig Adapt(StageConfig source, StageConfig layout, string folder)
        {
            if (source == null || layout == null || source.StageType != layout.StageType)
                throw new InvalidOperationException("Campaign stage and battlefield layout must have the same stage type.");
            string path = folder + "/" + source.name + ".asset";
            var stage = AssetDatabase.LoadAssetAtPath<StageConfig>(path);
            if (stage == null) { stage = ScriptableObject.CreateInstance<StageConfig>(); AssetDatabase.CreateAsset(stage, path); }
            EditorUtility.CopySerialized(source, stage);
            var target = new SerializedObject(stage); var map = new SerializedObject(layout);
            foreach (string field in new[] { "mapBounds", "forbiddenZones", "rivers", "backgroundArt", "backgroundArtArea", "formation", "decorations", "summonZone", "quickSummonPoints" })
                target.CopyFromSerializedProperty(map.FindProperty(field));
            foreach (string castle in new[] { "playerCastle", "enemyCastle" })
                foreach (string field in new[] { "position", "art" })
                    target.CopyFromSerializedProperty(map.FindProperty(castle).FindPropertyRelative(field));
            var defenders = target.FindProperty("defenders");
            for (int i = 0; i < defenders.arraySize; i++)
            {
                if (layout.Defenders.Count == 0) throw new InvalidOperationException("The current map has no defender placement.");
                defenders.GetArrayElementAtIndex(i).FindPropertyRelative("position").vector2Value = layout.Defenders[i % layout.Defenders.Count].Position;
            }
            var waveSlots = layout.Waves.SelectMany(w => w.Spawns).Select(s => s.SpawnPoint).ToArray();
            var waves = target.FindProperty("waves");
            for (int i = 0; i < waves.arraySize; i++)
            {
                var spawns = waves.GetArrayElementAtIndex(i).FindPropertyRelative("spawns");
                for (int j = 0; j < spawns.arraySize; j++)
                {
                    if (waveSlots.Length == 0) throw new InvalidOperationException("The current map has no wave spawn positions.");
                    spawns.GetArrayElementAtIndex(j).FindPropertyRelative("spawnPoint").vector2Value = waveSlots[j % waveSlots.Length];
                }
            }
            target.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(stage);
            var errors = new List<string>(); stage.Validate(errors);
            if (errors.Count != 0) throw new InvalidOperationException(string.Join("\n", errors));
            return stage;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
