using System;
using System.IO;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Frontend;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>
    /// Creates the game flow content (docs/KeHoach/VaoGame.md): lord roster, server list, name rules, opening story,
    /// tutorial stage, FrontendConfig and the Frontend scene. Like the rest of the prototype content, existing assets are
    /// kept (edit them freely); only missing ones are created. Called from <see cref="PrototypeContentBuilder.BuildAll"/>.
    /// </summary>
    internal static class FrontendContentBuilder
    {
        public const string FrontendScenePath = "Assets/Arena3Q/Scenes/Frontend.unity";
        private const string Root = PrototypeContentBuilder.ContentRoot;

        public static FrontendConfig Build(ProgressionConfig progression, StageConfig attackStage, StageConfig defenseStage)
        {
            PrototypeContentBuilder.EnsureFolder(Root + "/Frontend");

            var archer = Load<HeroDefinition>("Lords/Lord_Archer");
            var swordsman = Load<HeroDefinition>("Lords/Lord_Swordsman");
            var strategist = Load<HeroDefinition>("Lords/Lord_Strategist");
            var marshal = Load<HeroDefinition>("Lords/Lord_Marshal");
            var guardian = Load<HeroDefinition>("Heroes/Hero_Guardian");
            var hunter = Load<HeroDefinition>("Heroes/Hero_Archer");
            var starters = new[] { Load<HeroDefinition>("Heroes/Hero_Swordsman"), hunter, Load<HeroDefinition>("Heroes/Hero_Cavalry"), guardian };

            var roster = PrototypeContentBuilder.GetOrCreate<LordRoster>(Root + "/Frontend/LordRoster.asset", so =>
            {
                var lords = so.FindProperty("lords");
                lords.arraySize = 4;
                Lord(lords.GetArrayElementAtIndex(0), archer, 1, "Sát thương tầm xa, phá thành. Nữ cung thủ du hiệp có tầm đánh xa nhất.",
                    new[] { "Ưng Nhãn", "Liên Châu Tiễn", "Tiễn Vũ", "Phá Thành Hỏa Tiễn" },
                    new[] { "+15 % tầm; mỗi đòn thứ 4 bắn xuyên", "Bắn liên tiếp 3 mũi tên", "Mưa tên xuống một vùng trong 3 giây", "Mũi tên lửa xuyên quân, trúng thành ×3" });
                Lord(lords.GetArrayElementAtIndex(1), swordsman, 2, "Đột phá cận chiến, tự hồi máu. Nam kiếm khách song kiếm.",
                    new[] { "Kiếm Tâm", "Phi Ảnh Đột Kích", "Toàn Phong Trảm", "Vạn Kiếm Quy Tông" },
                    new[] { "Hút máu; dưới 30 % máu đánh nhanh hơn", "Lướt tới mục tiêu, choáng 1 giây", "Xoay song kiếm chém xung quanh", "Kiếm khí rơi xuống vùng hình quạt" });
                Lord(lords.GetArrayElementAtIndex(2), strategist, 3, "Khống chế vùng, tăng sức đội lính. Nữ mưu sĩ với quạt lông hạc.",
                    new[] { "Thao Lược", "Hỏa Kế", "Bát Quái Trận", "Đông Phong" },
                    new[] { "Lính quanh Mưu sĩ +10 % sát thương", "Trận lửa đốt quân địch 5 giây", "Trận đồ làm chậm và làm yếu quân địch", "Gió lớn đẩy lùi quân địch khắp chiến trường" });
                Lord(lords.GetArrayElementAtIndex(3), marshal, 1, "Chỉ huy: tăng sức toàn quân, thêm lính. Đại tướng cầm thương, cờ lệnh sau lưng.",
                    new[] { "Quân Lệnh Như Sơn", "Hiệu Triệu", "Trống Trận", "Thiết Kỵ Xung Phong" },
                    new[] { "Đội lính +2; toàn quân +5 % máu", "Bổ sung lính đã chết", "Quân ta đánh nhanh, chạy nhanh hơn", "Gọi hàng kỵ binh lao qua chiến trường" });
            });

            var servers = PrototypeContentBuilder.GetOrCreate<ServerList>(Root + "/Frontend/ServerList.asset", so =>
            {
                var list = so.FindProperty("servers");
                var entries = new[]
                {
                    ("s1", "S1 Hổ Lao", ServerStatus.Maintenance, false),
                    ("s2", "S2 Xích Bích", ServerStatus.Busy, false),
                    ("s3", "S3 Trường Bản", ServerStatus.New, true)
                };
                list.arraySize = entries.Length;
                for (var i = 0; i < entries.Length; i++)
                {
                    var element = list.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("serverId").stringValue = entries[i].Item1;
                    element.FindPropertyRelative("displayName").stringValue = entries[i].Item2;
                    element.FindPropertyRelative("status").enumValueIndex = (int)entries[i].Item3;
                    element.FindPropertyRelative("recommended").boolValue = entries[i].Item4;
                }
            });

            var names = PrototypeContentBuilder.GetOrCreate<LordNameConfig>(Root + "/Frontend/LordNameConfig.asset", so =>
            {
                so.FindProperty("minLength").intValue = 2;
                so.FindProperty("maxLength").intValue = 12;
                Strings(so.FindProperty("bannedWords"), "admin", "gm", "hệ thống", "quản trị", "moderator");
                Strings(so.FindProperty("familyNames"), "Lâm", "Tô", "Hạ", "Lý", "Trần", "Đường", "Vân", "Cố", "Diệp", "Tiêu", "Lạc",
                    "Bạch", "Mạc", "Tạ", "Hàn", "Phong");
                Strings(so.FindProperty("givenNames"), "Tử Yên", "Mạc Phong", "Thanh Vân", "Thiên Hành", "Nhược Thủy", "Vân Hi",
                    "Hàn Sơn", "Dạ Minh", "Cẩm Tú", "Kinh Hồng", "Lăng Vân", "Minh Nguyệt", "Thiếu Khanh", "Tịch Dao", "Lam Ngọc");
                Strings(so.FindProperty("reservedNames"), "Lữ Bố", "Quan Vũ", "Trương Phi", "Triệu Vân", "Tào Tháo", "Lưu Bị",
                    "Tôn Quyền", "Gia Cát Lượng", "Chu Du", "Tư Mã Ý", "Mã Siêu", "Hoàng Trung", "Điêu Thuyền", "Đổng Trác",
                    "Viên Thiệu", "Tôn Sách", "Hạ Hầu Đôn", "Điển Vi", "Cam Ninh", "Lục Tốn");
            });

            var story = PrototypeContentBuilder.GetOrCreate<StoryScript>(Root + "/Frontend/Story_Opening.asset", so =>
                FillOpeningStory(so, guardian, hunter, new[] { archer, swordsman, strategist, marshal }));

            var tutorial = PrototypeContentBuilder.GetOrCreate<StageConfig>(Root + "/Stages/Stage_Tutorial_01.asset", so =>
                FillTutorialStage(so, defenseStage));

            return PrototypeContentBuilder.GetOrCreate<FrontendConfig>(Root + "/Frontend/FrontendConfig.asset", so =>
            {
                so.FindProperty("lordRoster").objectReferenceValue = roster;
                so.FindProperty("serverList").objectReferenceValue = servers;
                so.FindProperty("names").objectReferenceValue = names;
                so.FindProperty("openingStory").objectReferenceValue = story;
                so.FindProperty("progression").objectReferenceValue = progression;
                so.FindProperty("tutorialStage").objectReferenceValue = tutorial;
                so.FindProperty("defaultStage").objectReferenceValue = attackStage;
                var catalog = so.FindProperty("heroCatalog");
                catalog.arraySize = starters.Length;
                for (var i = 0; i < starters.Length; i++)
                    catalog.GetArrayElementAtIndex(i).objectReferenceValue = starters[i];
                // Decision 2026-10-17: the Guardian comes at 3 stars so the tutorial can teach the ultimate.
                var gifts = so.FindProperty("starterGifts");
                gifts.arraySize = 2;
                Gift(gifts.GetArrayElementAtIndex(0), guardian, 3);
                Gift(gifts.GetArrayElementAtIndex(1), hunter, 1);
            });
        }

        /// <summary>The Frontend scene: a camera and the <see cref="FrontendController"/> (created once; references filled if empty).</summary>
        public static void BuildScene(FrontendConfig config)
        {
            // Opening a scene in Single mode unloads saved assets nothing references, which turns C# references taken
            // before into fake nulls: remember the path and load the asset again after opening (docs/QuyTac/NhatKySuaLoi.md).
            var configPath = AssetDatabase.GetAssetPath(config);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FrontendScenePath) == null)
            {
                PrototypeContentBuilder.EnsureFolder(Path.GetDirectoryName(FrontendScenePath).Replace('\\', '/'));
                if (!AssetDatabase.CopyAsset(PrototypeContentBuilder.SceneTemplatePath, FrontendScenePath))
                    throw new InvalidOperationException($"Could not copy the scene template to {FrontendScenePath}.");
                var created = EditorSceneManager.OpenScene(FrontendScenePath, OpenSceneMode.Single);
                if (Camera.main != null)
                    Camera.main.backgroundColor = Color.black;
                new GameObject("Frontend").AddComponent<FrontendController>();
                EditorSceneManager.MarkSceneDirty(created);
                if (!EditorSceneManager.SaveScene(created))
                    throw new InvalidOperationException($"Could not save {FrontendScenePath}.");
            }

            var scene = EditorSceneManager.OpenScene(FrontendScenePath, OpenSceneMode.Single);
            var flow = UnityEngine.Object.FindAnyObjectByType<FrontendController>()
                       ?? throw new InvalidOperationException($"{FrontendScenePath} has no FrontendController.");
            var so = new SerializedObject(flow);
            var property = so.FindProperty("config");
            if (property.objectReferenceValue != null)
                return;
            property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<FrontendConfig>(configPath)
                                            ?? throw new InvalidOperationException($"{configPath} not found.");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save {FrontendScenePath}.");
            Debug.Log($"[Arena3Q] {FrontendScenePath}: frontend config assigned.");
        }

        private static T Load<T>(string relativePath) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>($"{Root}/{relativePath}.asset")
            ?? throw new InvalidOperationException($"{Root}/{relativePath}.asset is missing (run the prototype content first).");

        private static void Lord(SerializedProperty entry, HeroDefinition lord, int difficulty, string role, string[] skills,
            string[] summaries)
        {
            entry.FindPropertyRelative("lord").objectReferenceValue = lord;
            entry.FindPropertyRelative("difficulty").intValue = difficulty;
            entry.FindPropertyRelative("role").stringValue = role;
            Strings(entry.FindPropertyRelative("skillNames"), skills);
            Strings(entry.FindPropertyRelative("skillSummaries"), summaries);
        }

        private static void Gift(SerializedProperty entry, HeroDefinition hero, int stars)
        {
            entry.FindPropertyRelative("hero").objectReferenceValue = hero;
            entry.FindPropertyRelative("level").intValue = 1;
            entry.FindPropertyRelative("stars").intValue = stars;
        }

        private static void Strings(SerializedProperty array, params string[] values)
        {
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        // Script of docs/KeHoach/VaoGame.md §10.1. Pictures stay empty until the story art exists (placeholders show).
        private static void FillOpeningStory(SerializedObject so, HeroDefinition guardian, HeroDefinition hunter, HeroDefinition[] lords)
        {
            var speakers = so.FindProperty("speakers");
            speakers.arraySize = 6;
            Speaker(speakers.GetArrayElementAtIndex(0), "narrator", "", narrator: true);
            Speaker(speakers.GetArrayElementAtIndex(1), "lord", "", playerLord: true);
            Speaker(speakers.GetArrayElementAtIndex(2), "elder", "Lão trưởng trấn", color: new Color(0.62f, 0.48f, 0.32f));
            Speaker(speakers.GetArrayElementAtIndex(3), "guardian", "", hero: guardian);
            Speaker(speakers.GetArrayElementAtIndex(4), "hunter", "", hero: hunter);
            Speaker(speakers.GetArrayElementAtIndex(5), "bandit_king", "Hắc Phong Trại Chủ", rightSide: true, color: new Color(0.3f, 0.25f, 0.25f));

            var panels = new (string id, string caption, Color color, bool perLord, (string speaker, string text)[] lines)[]
            {
                ("chaos", "Cuối thời Đông Hán – thiên hạ đại loạn", new Color(0.33f, 0.2f, 0.13f), false, new[]
                {
                    ("narrator", "Cuối thời Đông Hán, triều đình suy yếu. Giặc cướp nổi lên khắp nơi, dân lành không còn chốn nương thân.")
                }),
                ("raid", "Trấn Thanh Thạch bị sơn tặc cướp phá", new Color(0.12f, 0.1f, 0.18f), false, new[]
                {
                    ("narrator", "Dưới chân núi Hắc Phong, trấn Thanh Thạch đêm nào cũng bị sơn tặc cướp phá."),
                    ("elder", "Lũ Hắc Phong Trại lại xuống núi rồi… Trời cao ơi, ai cứu lấy trấn này!")
                }),
                ("arrival", "Bình minh ở cổng trấn Thanh Thạch", new Color(0.55f, 0.4f, 0.2f), true, new[]
                {
                    ("lord", "Ta là {Tên}, phụng mệnh dẹp loạn vùng này. Ai cần giúp, cứ nói!")
                }),
                ("allies", "Quảng trường trấn Thanh Thạch", new Color(0.3f, 0.4f, 0.25f), false, new[]
                {
                    ("elder", "Tướng quân! Xin cứu lấy dân trấn Thanh Thạch!"),
                    ("guardian", "Tại hạ là vệ binh của trấn. Tấm khiên này xin che chắn cho tướng quân!"),
                    ("hunter", "Rừng núi quanh đây ta thuộc như lòng bàn tay. Cho ta đi cùng.")
                }),
                ("bandit_king", "Hắc Phong Trại Chủ trên vách núi", new Color(0.3f, 0.08f, 0.08f), false, new[]
                {
                    ("bandit_king", "Ha ha ha! Kẻ nào dám cản đường Hắc Phong Trại? Các con, xông lên! Cướp sạch trấn Thanh Thạch!")
                }),
                ("charge", "Xuất trận!", new Color(0.45f, 0.3f, 0.15f), false, new[]
                {
                    ("lord", "Toàn quân, theo ta! Giữ vững trấn Thanh Thạch!")
                })
            };

            var list = so.FindProperty("panels");
            list.arraySize = panels.Length;
            for (var p = 0; p < panels.Length; p++)
            {
                var panel = list.GetArrayElementAtIndex(p);
                panel.FindPropertyRelative("panelId").stringValue = panels[p].id;
                panel.FindPropertyRelative("placeholderCaption").stringValue = panels[p].caption;
                panel.FindPropertyRelative("placeholderColor").colorValue = panels[p].color;
                // The arrival panel has one picture per lord (story_03_<lord>.png): one empty slot per lord to fill later.
                var images = panel.FindPropertyRelative("lordImages");
                images.arraySize = panels[p].perLord ? lords.Length : 0;
                for (var l = 0; l < images.arraySize; l++)
                    images.GetArrayElementAtIndex(l).FindPropertyRelative("lordHeroId").stringValue = lords[l].HeroId;
                var lines = panel.FindPropertyRelative("lines");
                lines.arraySize = panels[p].lines.Length;
                for (var l = 0; l < panels[p].lines.Length; l++)
                {
                    var line = lines.GetArrayElementAtIndex(l);
                    line.FindPropertyRelative("speakerId").stringValue = panels[p].lines[l].speaker;
                    line.FindPropertyRelative("text").stringValue = panels[p].lines[l].text;
                }
            }
        }

        private static void Speaker(SerializedProperty speaker, string id, string displayName, bool narrator = false,
            bool playerLord = false, bool rightSide = false, HeroDefinition hero = null, Color? color = null)
        {
            speaker.FindPropertyRelative("speakerId").stringValue = id;
            speaker.FindPropertyRelative("displayName").stringValue = displayName;
            speaker.FindPropertyRelative("isNarrator").boolValue = narrator;
            speaker.FindPropertyRelative("isPlayerLord").boolValue = playerLord;
            speaker.FindPropertyRelative("rightSide").boolValue = rightSide;
            speaker.FindPropertyRelative("hero").objectReferenceValue = hero;
            speaker.FindPropertyRelative("color").colorValue = color ?? Color.white;
        }

        // "Ải 1-1 Giải vây Thanh Thạch": a short, easy Defense stage on the standard map (docs/KeHoach/VaoGame.md §0).
        private static void FillTutorialStage(SerializedObject so, StageConfig defenseStage)
        {
            PrototypeContentBuilder.CommonStage(so, "tutorial_01", StageType.Defense, defenseStage.TestLoadout);
            PrototypeContentBuilder.SetCastle(so.FindProperty("playerCastle"), new Vector2(-19.4f, 0f), 3000f, true);
            PrototypeContentBuilder.SetCastle(so.FindProperty("enemyCastle"), new Vector2(19.4f, 0f), 4000f, false);
            so.FindProperty("timeLimit").floatValue = 60f;
            so.FindProperty("backgroundArt").objectReferenceValue = defenseStage.BackgroundArt;
            so.FindProperty("backgroundArtVerticalScale").floatValue = defenseStage.BackgroundArtVerticalScale;

            var swordsman = AssetDatabase.LoadAssetAtPath<TroopDefinition>(Root + "/Troops/Enemy_Swordsman.asset");
            var archer = AssetDatabase.LoadAssetAtPath<TroopDefinition>(Root + "/Troops/Enemy_Archer.asset");
            var waves = new[]
            {
                (3f, new[] { (swordsman, 4, new Vector2(22f, 2f)) }),
                (25f, new[] { (swordsman, 4, new Vector2(22f, -3f)), (archer, 2, new Vector2(22.5f, 3f)) })
            };
            var list = so.FindProperty("waves");
            list.arraySize = waves.Length;
            for (var w = 0; w < waves.Length; w++)
            {
                var wave = list.GetArrayElementAtIndex(w);
                wave.FindPropertyRelative("startTime").floatValue = waves[w].Item1;
                var spawns = wave.FindPropertyRelative("spawns");
                spawns.arraySize = waves[w].Item2.Length;
                for (var s = 0; s < waves[w].Item2.Length; s++)
                {
                    var spawn = spawns.GetArrayElementAtIndex(s);
                    spawn.FindPropertyRelative("unit").objectReferenceValue = waves[w].Item2[s].Item1;
                    spawn.FindPropertyRelative("count").intValue = waves[w].Item2[s].Item2;
                    spawn.FindPropertyRelative("spawnPoint").vector2Value = waves[w].Item2[s].Item3;
                }
            }
        }
    }
}
