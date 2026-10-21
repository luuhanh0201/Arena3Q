#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Arena3Q.Core;
using Arena3Q.Gameplay;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Arena3Q.Tests.PlayMode
{
    /// <summary>
    /// How much a full 3D battle draws (docs/KeHoach/ChuyenSang3D.md §2): all five heroes with their soldiers against
    /// the defenders, seen by the battle camera and by the whole-map view. Triangles are counted from the visible
    /// renderers (meshes, skinned meshes, sprites), draw calls / batches / set-pass calls from the render profiler.
    /// Writes Logs/RenderStats.json. Only the triangle budget is enforced here: draw calls and frame time must be
    /// measured on the device (spec §1: 60 FPS), Editor numbers are an indication.
    /// </summary>
    public class RenderBudgetTests
    {
        private const int TriangleBudget = 800000;   // §2: 600–800 k per frame
        private const int DrawCallBudget = 150;      // §2, device target

        private BattleController _battle;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
            yield return null;
            _battle = Object.FindAnyObjectByType<BattleController>();
            _battle.AutoSimulate = false;
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator FullBattle_StaysInsideTheTriangleBudget_AndReportsDrawCalls()
        {
            Assume.That(_battle.Presentation3D, Is.True, "3D presentation is the shipped default");
            _battle.StartBattle(0);
            yield return null;
            for (var card = 0; card < _battle.Cards.Count; card++)
                _battle.TrySummon(SummonRequest.Quick(card));
            for (var i = 0; i < 60; i++)
                _battle.Simulate(0.05f);
            yield return null;

            var camera = Camera.main;
            var report = new StringBuilder("{\n");
            yield return Counters();
            var battleView = Measure(camera, "battle_camera", report);
            report.Append(",\n");
            // The whole map at once: the worst case for triangles.
            var controller = camera.GetComponent<BattleCameraController>();
            controller.enabled = false;
            var size = camera.orthographicSize;
            try
            {
                var view = _battle.ViewBounds;
                camera.orthographicSize = Mathf.Max(view.height * 0.5f, view.width * 0.5f / camera.aspect) + 0.2f;
                BattleView3D.PlaceCamera(camera.transform, _battle, TiltedViewMath.PlaneToGround(view.center, _battle.GroundProjection));
                yield return Counters();
                var whole = Measure(camera, "whole_map", report);
                report.Append("\n}\n");
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/RenderStats.json", report.ToString());
                TestContext.WriteLine(report.ToString());

                Assert.That(whole.triangles, Is.LessThanOrEqualTo(TriangleBudget), "§2 triangle budget (whole map)");
                Assert.That(battleView.triangles, Is.LessThanOrEqualTo(TriangleBudget), "§2 triangle budget (battle camera)");
                if (battleView.drawCalls > DrawCallBudget)
                    Debug.LogWarning($"[Arena3Q] Editor counts {battleView.drawCalls} draw calls (device target {DrawCallBudget}); measure on the device.");
            }
            finally
            {
                camera.orthographicSize = size;
                controller.enabled = true;
            }
        }

        private long _drawCalls = -1, _batches = -1, _setPass = -1;

        // The render counters of real frames with the camera where it is now (they report the last finished frame).
        private IEnumerator Counters()
        {
            using var drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            using var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            using var setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            for (var i = 0; i < 3; i++)
                yield return null;
            _drawCalls = Last(drawCalls);
            _batches = Last(batches);
            _setPass = Last(setPass);
        }

        private (int triangles, long drawCalls) Measure(Camera camera, string name, StringBuilder report)
        {

            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var groups = new Dictionary<string, (int renderers, int triangles)>();
            var total = 0;
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                    (camera.cullingMask & (1 << renderer.gameObject.layer)) == 0 ||
                    !GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                    continue;
                var triangles = Triangles(renderer);
                total += triangles;
                var group = Group(renderer);
                groups.TryGetValue(group, out var sum);
                groups[group] = (sum.renderers + 1, sum.triangles + triangles);
            }
            report.Append($"  \"{name}\": {{ \"triangles\": {total}, \"draw_calls\": {_drawCalls}, " +
                          $"\"batches\": {_batches}, \"set_pass_calls\": {_setPass}, \"groups\": {{");
            report.Append(string.Join(",", groups.OrderByDescending(g => g.Value.triangles)
                .Select(g => $"\n    \"{g.Key}\": {{ \"renderers\": {g.Value.renderers}, \"triangles\": {g.Value.triangles} }}")));
            report.Append(" } }");
            return (total, _drawCalls);
        }

        private static long Last(ProfilerRecorder recorder) => recorder.Valid && recorder.Count > 0 ? recorder.LastValue : -1;

        private static int Triangles(Renderer renderer)
        {
            Mesh mesh = null;
            if (renderer is SkinnedMeshRenderer skinned) mesh = skinned.sharedMesh;
            else if (renderer is MeshRenderer) mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
            else if (renderer is SpriteRenderer sprite) return sprite.sprite != null ? sprite.sprite.triangles.Length / 3 : 0;
            if (mesh == null)
                return 0;
            var count = 0;
            for (var i = 0; i < mesh.subMeshCount; i++)
                count += (int)(mesh.GetIndexCount(i) / 3);
            return count;
        }

        // Which part of the battle a renderer belongs to, from its place in the hierarchy.
        private static string Group(Renderer renderer)
        {
            if (renderer is SpriteRenderer) return "sprites";
            for (var t = renderer.transform; t != null; t = t.parent)
            {
                if (t.GetComponent<HeroUnit>() != null) return "heroes";
                if (t.GetComponent<TroopUnit>() != null) return "troops";
                if (t.GetComponent<EnemyUnit>() != null) return "enemies";
                if (t.GetComponent<CastleUnit>() != null || t.name == "Castle Fall") return "castles";
                if (t.name == "Ground3D") return "ground";
                if (t.name.Contains("effect pool") || t.GetComponent<MacAnhFx>() != null) return "vfx";
            }
            return "map_props";
        }
    }
}
#endif
