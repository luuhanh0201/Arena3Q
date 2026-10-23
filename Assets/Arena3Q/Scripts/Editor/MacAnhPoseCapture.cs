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
            ("FrontMarked", Focus.Body, new Vector3(0f, .25f, -1f), .72f),
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

        public static void RunFullReview()
        {
            var lengths = new (string name,int last)[] { ("Idle",40),("Walk",36),("Run",24),("Dodge",24),
                ("Attack01",24),("Attack02",45),("Skill01",54),("Skill02",60),("Skill03",42),("Ultimate",105),("DodgeAttack",33) };
            var poses = lengths.SelectMany(c => Enumerable.Range(0,c.last+1).Select(f => (c.name,f))).ToArray();
            var views = new (string,Focus,Vector3,float)[] {
                ("Front",Focus.Body,new Vector3(0,.15f,-1),1.03f),
                ("Back",Focus.Body,new Vector3(0,.15f,1),1.03f),
                ("Left",Focus.Body,new Vector3(1,.15f,0),1.03f),
                ("Right",Focus.Body,new Vector3(-1,.15f,0),1.03f),
                ("Battle",Focus.Body,Quaternion.Euler(41.4f,0,0)*Vector3.back,1.03f),
                ("RightArm",Focus.RightArm,new Vector3(-.75f,.35f,-1),.42f),
                ("LeftArm",Focus.LeftArm,new Vector3(.75f,.35f,-1),.42f) };
            const string output = "NghiemThu/MacAnh/XemTruoc_CuffGait_20261023/frames";
            // Baseline frames were captured before the cloth/action repair. Never
            // regenerate them by hiding submeshes on the changed geometry.
            Capture(poses,views,"after_",output,512,true);
        }

        private static void Capture(IEnumerable<(string clip, int frame)> poses,
            (string name, Focus focus, Vector3 toCamera, float size)[] views, string prefix,
            string output = Output,int resolution = 900,bool review = false)
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(ViewPrefab);
            var serialized = new SerializedObject(view.GetComponents<MonoBehaviour>().First(m => m != null && m.GetType().Name == "MacAnhRealtimeBattleView"));
            var character = (GameObject)serialized.FindProperty("characterPrefab").objectReferenceValue;
            var clips = serialized.FindProperty("clips");
            var model = UnityEngine.Object.Instantiate(character);
            var lightObject = new GameObject("CaptureSun");
            var cameraObject = new GameObject("CaptureCamera");
            var target = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            // Shaders compiled asynchronously draw nothing on their first use (the first frame lost its body).
            var asyncShaders = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
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
                var proxies = model.GetComponentsInChildren<SkinnedMeshRenderer>().Select(skin =>
                {
                    var copy = new GameObject(skin.name + "_Baked");
                    copy.transform.SetParent(skin.transform, false);
                    var mesh = new Mesh();
                    copy.AddComponent<MeshFilter>().sharedMesh = mesh;
                    copy.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                    skin.enabled = false;
                    return (skin, mesh);
                }).ToArray();
                // "Marked" views show where the hand bone (red) and the fan grip (cyan) really are.
                var markers = new[] { (Bone("RightHand"), Color.red), (Bone("Fan_Handle"), Color.cyan), (Bone("RightLowerArm"), Color.yellow) }
                    .Select(m => { var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); UnityEngine.Object.DestroyImmediate(s.GetComponent<Collider>()); s.transform.SetParent(m.Item1, false); s.transform.localScale = Vector3.one * .03f / m.Item1.lossyScale.x; s.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = m.Item2 }; return s; }).ToArray();
                Directory.CreateDirectory(output);
                // A newly made renderer is missing from the very first render: draw one throwaway frame.
                foreach (var (skin, proxy) in proxies)
                    skin.BakeMesh(proxy, true);
                camera.Render();
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
                        throw new InvalidOperationException("Missing review clip: " + clipName);
                    clip.SampleAnimation(model, Mathf.Min(clip.length, frame / 30f));
                    // Camera.Render in the Editor (batch) does not re-skin after SampleAnimation: the skinned body kept an
                    // old pose while the rigid fan moved (2026-10-22). Draw CPU-baked copies of the skin instead.
                    foreach (var (skin, proxy) in proxies)
                    {
                        skin.BakeMesh(proxy, true);
                    }
                    // The size and body centre come from the rest-height so every frame of a sequence is framed alike.
                    var bounds = renderers.Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                    var height = Bone("Head").position.y - model.transform.position.y;
                    foreach (var (name, focus, toCamera, size) in views)
                    {
                        foreach (var marker in markers)
                            marker.SetActive(name.EndsWith("Marked", StringComparison.Ordinal));
                        var point = focus switch
                        {
                            Focus.Chest => Bone("Chest").position + Vector3.up * height * .1f,
                            Focus.LeftArm => Bone("LeftLowerArm").position,
                            Focus.RightArm => Bone("RightLowerArm").position,
                            _ => new Vector3(model.transform.position.x, bounds.center.y, model.transform.position.z),
                        };
                        var look = -toCamera.normalized;
                        if(review && focus==Focus.Body)point=new Vector3(0,.85f,0);
                        camera.transform.position = point - look * 10f;
                        camera.transform.rotation = Quaternion.LookRotation(look, Vector3.up);
                        camera.orthographicSize = review ? size : height * size;
                        camera.Render();
                        RenderTexture.active = target;
                        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                        image.Apply();
                        RenderTexture.active = null;
                        File.WriteAllBytes($"{output}/{prefix}{clipName}_{frame:000}_{name}."+(review?"jpg":"png"),
                            review?image.EncodeToJPG(90):image.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(image);
                    }
                }
                Debug.Log("[Arena3Q] MACANH_POSES_CAPTURED " + output + " " + prefix);
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = asyncShaders;
                UnityEngine.Object.DestroyImmediate(model);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                target.Release();
            }
        }
    }
}
