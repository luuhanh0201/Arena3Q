#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Arena3Q.Tests.PlayMode
{
    public sealed class CodexMacAnhDirectTests
    {
        private BattleController _battle;
        private HeroUnit _hero;
        private readonly List<Object> _owned = new List<Object>();
        private string Output => Environment.GetEnvironmentVariable("ARENA3Q_DIRECT_EVIDENCE") ?? "Logs/CodexFix/20261021/Direct";

        [UnitySetUp] public IEnumerator SetUp()
        {
            UnityEngine.Random.InitState(210021);
            yield return SceneManager.LoadSceneAsync("Battle"); yield return null;
            _battle = Object.FindAnyObjectByType<BattleController>(); _battle.AutoSimulate = false;
            Assert.That(_battle.Presentation3D, Is.True, "Exercise the shipped direct 3D route.");
            var definition = AssetDatabase.LoadAssetAtPath<HeroDefinition>("Assets/Arena3Q/Content/Lords/Lord_MacAnhPreview.asset");
            _battle.StartBattle(_battle.Stage, new[] { new LineupEntry(definition, 60) });
            Assert.That(_battle.TrySummon(SummonRequest.Quick(0)).Accepted, Is.True);
            _hero = _battle.GetHero(_battle.State.Cards[0].Hero);
            _hero.SetPosition(Vector2.zero);
            yield return null;
            _hero.Performance.View.enabled = false;
            Directory.CreateDirectory(Output);
        }

        [TearDown] public void TearDown()
        {
            foreach (var item in _owned) if (item != null) Object.DestroyImmediate(item);
            _owned.Clear();
        }

        [Serializable] private sealed class SnapshotReport
        {
            public int skinRenderers, staticParts, sourceIndices, capturedIndices;
            public Vector3 sourceSize, capturedSize;
            public float heightRatio;
            public float minimumSilhouetteCoverage=1;
            public string[] staticPartNames;
        }

        [Test] public void DirectSnapshotPreservesWorldScaleAndEveryMeshPart()
        {
            var realtime = _hero.GetComponentsInChildren<MonoBehaviour>().First(c => c is IMacAnhRealtimeView);
            var runtime = (IMacAnhRealtimeView)realtime;
            var setup = new SerializedObject(realtime);
            setup.FindProperty("transitionSeconds").floatValue = 0;
            setup.ApplyModifiedPropertiesWithoutUndo();
            runtime.Sample(MacAnhAction.DodgeAttack, .42f, -45);
            var model = (GameObject)realtime.GetType().GetField("_model", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(realtime);
            File.WriteAllText(Path.Combine(Output,"Hierarchy.txt"), "alive="+_hero.IsAlive+" modelActive="+model.activeSelf+"\n"+
                string.Join("\n",model.GetComponentsInChildren<Transform>(true).Select(t=>t.name+" active="+t.gameObject.activeSelf+" components="+string.Join(",",t.GetComponents<Component>().Select(c=>c.GetType().Name)))));
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var statics = model.GetComponentsInChildren<MeshFilter>(true).Where(f => f.GetComponent<MeshRenderer>() != null).ToArray();
            Assert.That(skins.Length, Is.GreaterThan(0));
            model.SetActive(true);
            var world = new List<Vector3>();
            int sourceIndices = 0;
            foreach (var skin in skins)
            {
                var mesh = skin.sharedMesh;
                // Imported meshes intentionally disable CPU reads. BakeMesh(true) is the convention
                // independently checked against bone matrices by MacAnhImportedFanQa.
                var reference = new Mesh(); _owned.Add(reference); skin.BakeMesh(reference, true);
                world.AddRange(reference.vertices.Select(v => skin.transform.TransformPoint(v)));
                for (int i = 0; i < mesh.subMeshCount; i++) sourceIndices += (int)mesh.GetIndexCount(i);
            }
            foreach (var part in statics)
            {
                var bounds = part.GetComponent<Renderer>().bounds;
                world.Add(bounds.min); world.Add(bounds.max);
                for (int i = 0; i < part.sharedMesh.subMeshCount; i++) sourceIndices += (int)part.sharedMesh.GetIndexCount(i);
            }
            var sourceBounds = BoundsOf(world);
            var fx = MacAnhFxPool.For(_battle).Spawn(_hero.Definition.MacAnhPerformance.Effect("Afterimage"), Vector3.zero,
                Vector3.one, Quaternion.identity, MacAnhFxQuality.Medium, 1, 100);
            Assert.That(runtime.CaptureAfterimage(fx, 100), Is.True);
            fx.Advance(.1f);
            var captured = fx.GetComponentsInChildren<MeshFilter>().Single(f => f.name == "Baked 3D pose");
            var capturedBounds = BoundsOf(captured.sharedMesh.vertices.Select(v => captured.transform.TransformPoint(v)));
            int capturedIndices = 0;
            var capturedParts = captured.GetComponentsInChildren<MeshFilter>();
            var capturedRenderers = captured.GetComponentsInChildren<Renderer>();
            foreach(var part in capturedParts)
            {
                if(part!=captured)capturedBounds.Encapsulate(part.GetComponent<Renderer>().bounds);
                for(int i=0;i<part.sharedMesh.subMeshCount;i++)capturedIndices+=(int)part.sharedMesh.GetIndexCount(i);
            }
            var report = new SnapshotReport { skinRenderers = skins.Length, staticParts = statics.Length,
                sourceIndices = sourceIndices, capturedIndices = capturedIndices, sourceSize = sourceBounds.size,
                capturedSize = capturedBounds.size, heightRatio = capturedBounds.size.z / sourceBounds.size.z,
                staticPartNames = statics.Select(s => s.name).ToArray() };
            File.WriteAllText(Path.Combine(Output, "Snapshot.json"), JsonUtility.ToJson(report, true));
            foreach (var t in model.GetComponentsInChildren<Transform>()) t.gameObject.layer = 31;
            foreach(var t in captured.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
            var camera = IsolatedCamera();
            camera.orthographicSize = Mathf.Max(sourceBounds.extents.magnitude * 1.1f, 1);
            var offsets = new[] { new Vector3(0,-4,-2), new Vector3(0,4,-2), new Vector3(-4,0,-2),
                new Vector3(4,0,-2), new Vector3(3,-4,-3), new Vector3(0,.01f,-5) };
            for (int i = 0; i < offsets.Length; i++)
            {
                camera.transform.position = sourceBounds.center + offsets[i];
                camera.transform.LookAt(sourceBounds.center, Vector3.back);
                foreach(var renderer in capturedRenderers)renderer.enabled=false; model.SetActive(true);
                Capture(camera, Path.Combine(Output, $"Source_{i}.png"));
                foreach(var renderer in capturedRenderers)renderer.enabled=true; model.SetActive(false);
                int visiblePixels=Capture(camera, Path.Combine(Output, $"Snapshot_{i}.png"));
                // An atlas intended for a 2D sprite must not punch holes through unrelated model UVs.
                var ghostMaterial=captured.GetComponent<Renderer>().sharedMaterial;
                var oldMask=ghostMaterial.GetTexture("_MainTex");
                ghostMaterial.SetTexture("_MainTex",Texture2D.whiteTexture);
                int fullPixels=Capture(camera,Path.Combine(Output,$"SilhouetteReference_{i}.png"));
                ghostMaterial.SetTexture("_MainTex",oldMask);
                Assert.That(fullPixels,Is.GreaterThan(100));
                report.minimumSilhouetteCoverage=Mathf.Min(report.minimumSilhouetteCoverage,visiblePixels/(float)fullPixels);
            }
            File.WriteAllText(Path.Combine(Output, "Snapshot.json"), JsonUtility.ToJson(report, true));
            model.SetActive(true);
            Assert.That(report.heightRatio, Is.EqualTo(1).Within(.01), "Snapshot must preserve world-space body and fan size.");
            Assert.That(capturedIndices, Is.EqualTo(sourceIndices), "Snapshot must retain every submesh and rigid fan part.");
            Assert.That(report.minimumSilhouetteCoverage,Is.GreaterThan(.95f),"Snapshot material must retain the complete silhouette, not sample the old sprite atlas through body UVs.");
            Assert.That(Vector3.Distance(sourceBounds.center,capturedBounds.center),Is.LessThan(.001f));
            foreach(var source in statics)
            {
                var copy=capturedParts.Single(p=>p.name==source.name);
                Assert.That(copy.sharedMesh,Is.SameAs(source.sharedMesh),"Borrow rigid geometry without duplicating CPU data.");
                Assert.That(Vector3.Distance(copy.transform.position,source.transform.position),Is.LessThan(.00001f));
            }
            fx.Advance(.8f);
            var properties=new MaterialPropertyBlock();float opacity=-1;
            foreach(var renderer in capturedRenderers)
            {
                renderer.GetPropertyBlock(properties);float value=properties.GetFloat("_Opacity");
                Assert.That(value,Is.GreaterThan(0).And.LessThan(1),"Every part must disperse.");
                if(opacity>=0)Assert.That(value,Is.EqualTo(opacity).Within(.000001f));
                opacity=value;
            }
            fx.Stop();
        }

        [Test] public void GroundAnchoredBillboardsUseCameraProjection()
        {
            // The own flipbook (a store effect may stand in for MagicArray on this machine).
            var prefab = _hero.Definition.MacAnhPerformance.OwnEffect("MagicArray");
            var fx = MacAnhFxPool.For(_battle).Spawn(prefab, Vector3.zero, new Vector3(2, 2 * _battle.ViewYScale, 1),
                Quaternion.identity, MacAnhFxQuality.Medium, 3, 100);
            fx.Advance(.5f);
            var player = fx.GetComponentInChildren<MacAnhVfxPlayer>();
            var layer = player.Recipe.layers.First(l => l.stretchToGround);
            var root = player.transform.Find(layer.name);
            var expected = _battle.GroundProjection / player.Recipe.renderProjection;
            foreach (var t in fx.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
            var camera = IsolatedCamera(); camera.orthographicSize = 3;
            BattleView3D.PlaceCamera(camera.transform, _battle, Vector2.zero);
            Capture(camera, Path.Combine(Output,"Footprint.png"));
            File.WriteAllText(Path.Combine(Output,"Footprint.json"), $"{{\"actual\":{Mathf.Abs(root.localScale.y)},\"expected\":{expected},\"cameraProjection\":{_battle.GroundProjection}}}");
            Assert.That(Mathf.Abs(root.localScale.y), Is.EqualTo(expected).Within(.001), "Billboard footprint must match the camera-projected ground ring.");
            foreach(float tilt in new[]{0f,41.40962f,60f})
            {
                player.SetUpright(Quaternion.Euler(-tilt,0,0));player.Advance(0);
                Assert.That(Mathf.Abs(root.localScale.y),Is.EqualTo(Mathf.Cos(tilt*Mathf.Deg2Rad)/player.Recipe.renderProjection).Within(.001),"Camera tilt "+tilt);
            }
            player.SetUpright(Quaternion.identity);
            player.SetBattlePresentation(2,100,.75f,MacAnhFxKind.MagicArray);
            Assert.That(Mathf.Abs(root.localScale.y),Is.EqualTo(.75f/player.Recipe.renderProjection).Within(.001),"Legacy flat camera retains its ground scale.");
            fx.Stop();
        }

        private static Bounds BoundsOf(IEnumerable<Vector3> points)
        {
            var list = points.ToArray(); var bounds = new Bounds(list[0], Vector3.zero);
            foreach (var p in list) bounds.Encapsulate(p); return bounds;
        }
        private Camera IsolatedCamera()
        {
            var go = new GameObject("Direct snapshot audit", typeof(Camera)); _owned.Add(go);
            var camera = go.GetComponent<Camera>(); camera.enabled = false; camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.1f,.11f,.14f);
            camera.cullingMask = 1 << 31; camera.farClipPlane = 1000;
            BattleView3D.ConfigureCamera(camera, _battle); return camera;
        }
        private static int Capture(Camera camera, string path)
        {
            var old = camera.targetTexture; var active = RenderTexture.active;
            var rt = new RenderTexture(768,768,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var image = new Texture2D(768,768,TextureFormat.RGB24,false);
            try { rt.Create(); camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,768,768),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG());
                var pixels=image.GetPixels32();var background=pixels[0];
                return pixels.Count(p=>Mathf.Abs(p.r-background.r)+Mathf.Abs(p.g-background.g)+Mathf.Abs(p.b-background.b)>30); }
            finally { camera.targetTexture=old; RenderTexture.active=active; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(image); }
        }
    }
}
#endif
