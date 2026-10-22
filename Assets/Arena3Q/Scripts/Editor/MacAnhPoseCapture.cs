using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>
    /// Renders Mặc Ảnh's in-battle model (the imported FBX with its Unity materials) in chosen clip poses, so animation
    /// fixes are judged on what the game shows, not on the Blender preview. Stills: the battle camera's tilt and close
    /// views of each arm. Sequences: every second frame around each release, front view of the upper body, to judge the
    /// motion frame by frame. Writes Logs/MacAnhPoses/. Arena3Q > Mac Anh > Capture Battle Poses / Capture Strike Sequences.
    /// </summary>
    public static class MacAnhPoseCapture
    {
        private const string ViewPrefab = "Assets/Arena3Q/Art/Heroes/MacAnh/Resources/MacAnh/MacAnhRealtimeBattleView.prefab";
        private const string Output = "Logs/MacAnhPoses";

        private static readonly (string clip, int frame)[] Poses =
        {
            ("Idle", 0), ("Attack01", 8), ("Attack01", 15), ("Attack02", 25), ("Skill02", 30), ("Ultimate", 60), ("Ultimate", 75)
        };

        private static readonly (string clip, int from, int to)[] Sequences =
        {
            ("Attack01", 2, 24), ("Attack02", 6, 40), ("Skill01", 6, 50), ("Skill02", 4, 52), ("Skill03", 4, 40), ("Ultimate", 10, 100)
        };

        private enum Focus { Body, Chest, LeftArm, RightArm }

        // The model faces -Z: a camera in front stands on the -Z side. Offsets point from the focus to the camera.
        private static readonly (string name, Focus focus, Vector3 toCamera, float size)[] StillViews =
        {
            ("Battle", Focus.Body, Quaternion.Euler(41.4f, 0f, 0f) * Vector3.back, 1.15f),
            ("LeftArm", Focus.LeftArm, new Vector3(.75f, .35f, -1f), .42f),
            ("LeftArmBack", Focus.LeftArm, new Vector3(.6f, .3f, 1f), .42f),
            ("RightArm", Focus.RightArm, new Vector3(-.75f, .35f, -1f), .42f),
        };

        private static readonly (string name, Focus focus, Vector3 toCamera, float size)[] SequenceViews =
        {
            ("Front", Focus.Body, new Vector3(0f, .25f, -1f), .72f),
            ("Side", Focus.Body, new Vector3(1f, .2f, -.35f), .72f),
            ("Battle", Focus.Body, Quaternion.Euler(41.4f, 0f, 0f) * Vector3.back, 1.15f),
        };

        [MenuItem("Arena3Q/Mac Anh/Capture Battle Poses")]
        public static void Run() => Capture(Poses, StillViews, "");

        [MenuItem("Arena3Q/Mac Anh/Capture Strike Sequences")]
        public static void RunSequences()
        {
            var poses = new List<(string, int)>();
            foreach (var (clip, from, to) in Sequences)
                for (var frame = from; frame <= to; frame += 2)
                    poses.Add((clip, frame));
            Capture(poses, SequenceViews, "Seq_");
        }

        private static void Capture(IEnumerable<(string clip, int frame)> poses,
            (string name, Focus focus, Vector3 toCamera, float size)[] views, string prefix)
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(ViewPrefab);
            var serialized = new SerializedObject(view.GetComponents<MonoBehaviour>().First(m => m != null && m.GetType().Name == "MacAnhRealtimeBattleView"));
            var character = (GameObject)serialized.FindProperty("characterPrefab").objectReferenceValue;
            var clips = serialized.FindProperty("clips");
            var model = UnityEngine.Object.Instantiate(character);
            var lightObject = new GameObject("CaptureSun");
            var cameraObject = new GameObject("CaptureCamera");
            var target = new RenderTexture(900, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            try
            {
                model.transform.position = Vector3.zero;
                var sun = lightObject.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.4f;
                lightObject.transform.rotation = Quaternion.Euler(50f, 150f, 0f);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.55f, .55f, .6f);
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.8f, .67f, .43f, 1f);
                camera.targetTexture = target;
                camera.nearClipPlane = .01f;
                camera.farClipPlane = 50f;
                // Battle draws 3D models with the Universal Forward renderer (index 1), not the 2D default.
                UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(camera).SetRenderer(1);
                var bones = model.GetComponentsInChildren<Transform>(true);
                Transform Bone(string name) => bones.First(t => t.name == name);
                var renderers = model.GetComponentsInChildren<Renderer>();
                Directory.CreateDirectory(Output);
                foreach (var (clipName, frame) in poses)
                {
                    AnimationClip clip = null;
                    for (var i = 0; i < clips.arraySize && clip == null; i++)
                    {
                        var candidate = (AnimationClip)clips.GetArrayElementAtIndex(i).FindPropertyRelative("clip").objectReferenceValue;
                        if (candidate != null && candidate.name == clipName)
                            clip = candidate;
                    }
                    if (clip == null)
                        continue;
                    clip.SampleAnimation(model, Mathf.Min(clip.length, frame / 30f));
                    // The size and body centre come from the rest-height so every frame of a sequence is framed alike.
                    var bounds = renderers.Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                    var height = Bone("Head").position.y - model.transform.position.y;
                    foreach (var (name, focus, toCamera, size) in views)
                    {
                        var point = focus switch
                        {
                            Focus.Chest => Bone("Chest").position + Vector3.up * height * .1f,
                            Focus.LeftArm => Bone("LeftLowerArm").position,
                            Focus.RightArm => Bone("RightLowerArm").position,
                            _ => new Vector3(model.transform.position.x, bounds.center.y, model.transform.position.z),
                        };
                        var look = -toCamera.normalized;
                        camera.transform.position = point - look * 10f;
                        camera.transform.rotation = Quaternion.LookRotation(look, Vector3.up);
                        camera.orthographicSize = height * size;
                        camera.Render();
                        RenderTexture.active = target;
                        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                        image.Apply();
                        RenderTexture.active = null;
                        File.WriteAllBytes($"{Output}/{prefix}{clipName}_{frame:000}_{name}.png", image.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(image);
                    }
                }
                Debug.Log("[Arena3Q] MACANH_POSES_CAPTURED " + Output);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(model);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                target.Release();
            }
        }
    }
}
