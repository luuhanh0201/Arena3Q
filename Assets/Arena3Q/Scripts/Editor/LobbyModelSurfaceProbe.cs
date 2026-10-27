using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arena3Q.Data;
using Arena3Q.Frontend;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace Arena3Q.Editor
{
    // Diagnostic clones only: none of these variants is written back to a source material.
    public static class LobbyModelSurfaceProbe
    {
        [Serializable] private sealed class Result
        {
            public string variant;
            public int opaque, partialAlpha, cyanOpaqueLower;
            public int msaa;
            public string[] probes;
        }
        public static void Run()
        {
            var config = AssetDatabase.LoadAssetAtPath<LobbyConfig>(LobbyContentBuilder.Path);
            if (config == null || config.models.Length == 0) throw new InvalidOperationException("Apply integrated lobby content first.");
            var output = "NghiemThu/UI/XemTruoc_Lobby2D_SuaTheoMau_20261027/surface-probe_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Directory.CreateDirectory(output);
            var results = new List<Result>();
            var asyncShaders = ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation = false;
            try
            {
                foreach (var variant in new[] { "Current", "NoProbe", "BoundCube", "DiffuseGI", "SpecularGI", "NoNormal", "NoORM", "BaseColorOnly" })
                {
                    var root = new GameObject("Lobby material diagnostic", typeof(RectTransform), typeof(RawImage), typeof(LobbyLordView));
                    var view = root.GetComponent<LobbyLordView>();
                    var copies = new Dictionary<Material, Material>(); var previous = RenderTexture.active;
                    try
                    {
                        view.Initialize(root.GetComponent<RawImage>(), config, config.models[0]);
                        using var isolated = new LobbyCaptureStage(view);
                        view.Sample(view.Idle.length * .25f);
                        if (variant == "NoProbe") foreach (var renderer in view.Model.GetComponentsInChildren<Renderer>())
                            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                        if (variant == "BoundCube") foreach (var renderer in view.Model.GetComponentsInChildren<Renderer>())
                        {
                            var properties = new MaterialPropertyBlock(); renderer.GetPropertyBlock(properties);
                            properties.SetTexture("unity_SpecCube0", config.reflectionEnvironment);
                            properties.SetVector("unity_SpecCube0_HDR", new Vector4(1, 1, 0, 0)); renderer.SetPropertyBlock(properties);
                        }
                        if (variant != "Current") foreach (var renderer in view.Model.GetComponentsInChildren<Renderer>())
                        {
                            renderer.sharedMaterials = renderer.sharedMaterials.Select(source =>
                            {
                                if (!copies.TryGetValue(source, out var copy))
                                {
                                    copy = new Material(source); copies[source] = copy;
                                    if (variant == "NoNormal" && copy.HasProperty("_BumpScale")) copy.SetFloat("_BumpScale", 0);
                                    if (variant == "NoORM" && copy.HasProperty("_UseORM")) copy.SetFloat("_UseORM", 0);
                                    if (variant == "BaseColorOnly") copy.shader = Shader.Find("Hidden/Arena3Q/Lobby Albedo Probe");
                                    if (variant == "DiffuseGI" || variant == "SpecularGI")
                                    {
                                        copy.shader = Shader.Find("Hidden/Arena3Q/Lobby GI Probe");
                                        copy.SetFloat("_GiMode", variant == "DiffuseGI" ? 0 : 1);
                                    }
                                }
                                return copy;
                            }).ToArray();
                        }
                        view.CaptureCamera.orthographicSize = .73f;
                        view.CaptureCamera.transform.localPosition = new Vector3(0, .99f, -4);
                        view.CaptureCamera.Render(); view.CaptureCamera.Render(); RenderTexture.active = view.Texture;
                        var image = new Texture2D(view.Texture.width, view.Texture.height, TextureFormat.RGBA32, false);
                        try
                        {
                            image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0); image.Apply();
                            var result = new Result { variant = variant, msaa = view.Texture.antiAliasing };
                            var probes = new List<string>();
                            foreach (var renderer in view.Model.GetComponentsInChildren<Renderer>())
                            {
                                var closest = new List<ReflectionProbeBlendInfo>(); renderer.GetClosestReflectionProbes(closest);
                                foreach (var probe in closest) probes.Add(renderer.name + ": " + probe.probe.name + " weight=" + probe.weight +
                                    " custom=" + (probe.probe.customBakedTexture == config.reflectionEnvironment) +
                                    " texture=" + (probe.probe.texture != null ? probe.probe.texture.name : "null"));
                            }
                            result.probes = probes.ToArray();
                            var pixels = image.GetPixels32();
                            for (var y = 0; y < image.height; y++) for (var x = 0; x < image.width; x++)
                            {
                                var color = pixels[y * image.width + x];
                                if (color.a == 255) result.opaque++; else if (color.a > 0) result.partialAlpha++;
                                // ReadPixels starts at bottom; this is the same lower-robe ROI as the crop report.
                                if (y < 174 && x > 340 && x < 970 && color.a > 250 && color.g > 100 && color.b > 120 &&
                                    color.g > color.r + 12 && color.b > color.r + 18) result.cyanOpaqueLower++;
                            }
                            results.Add(result); File.WriteAllBytes(output + "/" + variant + ".png", image.EncodeToPNG());
                        }
                        finally { UnityEngine.Object.DestroyImmediate(image); }
                    }
                    finally
                    {
                        RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(root);
                        foreach (var copy in copies.Values) UnityEngine.Object.DestroyImmediate(copy);
                    }
                }
            }
            finally { ShaderUtil.allowAsyncCompilation = asyncShaders; }
            File.WriteAllLines(output + "/metrics.jsonl", results.Select(result => JsonUtility.ToJson(result)));
            Debug.Log("LOBBY_SURFACE_PROBE_COMPLETE " + output);
        }
    }
}
