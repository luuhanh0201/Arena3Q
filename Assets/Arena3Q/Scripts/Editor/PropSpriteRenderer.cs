using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Arena3Q.Editor
{
    /// <summary>
    /// Renders 3D prop models into sprites in the fixed 3/4 art view (map spec §8.3), with a soft ground shadow, so the
    /// 2D game can use 3D sources (e.g. Meshy FBX). The meshes are drawn through a CommandBuffer with
    /// Hidden/Arena3Q/PropCapture, so the project's 2D Renderer is not involved and no render pipeline setting changes.
    /// Each sprite's pivot is the model's ground centre, i.e. the point that sits on the unit's logic position.
    /// Batch mode: -executeMethod Arena3Q.Editor.PropSpriteRenderer.RenderAll
    /// </summary>
    public static class PropSpriteRenderer
    {
        public const string BatQuaiTranFolder = "Assets/Arena3Q/Art/Maps/batQuaiTran";
        public const string BatQuaiTranConfigPath = BatQuaiTranFolder + "/PropSprites.asset";
        private const string ShaderName = "Hidden/Arena3Q/PropCapture";
        private const string MeshyPrefix = "Meshy_AI_";
        private const int MaxRenderSize = 8192;

        /// <summary>The 8 pillars of the Bát Môn ring stand at rotY 22.5° + k·45° (BatTran_Layout.json).</summary>
        private static readonly float[] RingYaws = { 22.5f, 67.5f, 112.5f, 157.5f, 202.5f, 247.5f, 292.5f, 337.5f };

        /// <summary>The 8 gate slabs stand across their gaps at rotY 90° − k·45°, i.e. every 45°.</summary>
        private static readonly float[] GateYaws = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

        /// <summary>
        /// First-guess footprints (logic units) and yaws for the Bát Quái Trận props; tune them in the asset after
        /// looking at the sprites. Ring sizes come from BatTran_Layout.json, the rest from map spec §8.5.
        /// </summary>
        private static readonly Dictionary<string, (PropSpriteRenderConfig.SizeAxis axis, float size, float[] yaws)>
            BatQuaiTranDefaults = new()
            {
                ["BaguaRoundStoneAltar"] = (PropSpriteRenderConfig.SizeAxis.Footprint, 4f, new[] { 0f }),
                ["BaguaStonePillar"] = (PropSpriteRenderConfig.SizeAxis.Footprint, 2f, RingYaws),
                ["BaguaWideStoneSlab"] = (PropSpriteRenderConfig.SizeAxis.Footprint, 2.5f, GateYaws),
                ["MilitaryFlagpoleBanner"] = (PropSpriteRenderConfig.SizeAxis.Height, 3f, new[] { 0f }),
                ["ThreeKingdomsCommand_1"] = (PropSpriteRenderConfig.SizeAxis.Footprint, 3.5f, new[] { 0f }),
                ["ThreeKingdomsCommand_2"] = (PropSpriteRenderConfig.SizeAxis.Footprint, 3.5f, new[] { 0f }),
                ["ThreeKingdomsSquare"] = (PropSpriteRenderConfig.SizeAxis.Footprint, 3.5f, new[] { 0f }),
                ["WoodenCampWatchtower"] = (PropSpriteRenderConfig.SizeAxis.Height, 4f, new[] { 0f }),
                ["WoodenPalisadeFence"] = (PropSpriteRenderConfig.SizeAxis.Footprint, 3f, new[] { 0f }),
            };

        private static readonly int PropModelId = Shader.PropertyToID("_PropModel");
        private static readonly int PropViewProjId = Shader.PropertyToID("_PropViewProj");
        private static readonly int LightDirId = Shader.PropertyToID("_LightDir");
        private static readonly int AmbientId = Shader.PropertyToID("_Ambient");
        private static readonly int DiffuseId = Shader.PropertyToID("_Diffuse");
        private static readonly int LightWrapId = Shader.PropertyToID("_LightWrap");
        private static readonly int HasNormalMapId = Shader.PropertyToID("_HasNormalMap");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int NormalMapId = Shader.PropertyToID("_NormalMap");

        [MenuItem("Arena3Q/Render Prop Sprites")]
        public static void RenderAll()
        {
            try
            {
                EnsureBatQuaiTranConfig();
                foreach (var guid in AssetDatabase.FindAssets("t:PropSpriteRenderConfig"))
                    Render(AssetDatabase.LoadAssetAtPath<PropSpriteRenderConfig>(AssetDatabase.GUIDToAssetPath(guid)));
                AssetDatabase.SaveAssets();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                throw;
            }
        }

        /// <summary>Creates the Bát Quái Trận config from the model folders if it does not exist yet.</summary>
        public static PropSpriteRenderConfig EnsureBatQuaiTranConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<PropSpriteRenderConfig>(BatQuaiTranConfigPath);
            if (config != null)
                return config;

            config = ScriptableObject.CreateInstance<PropSpriteRenderConfig>();
            config.outputFolder = BatQuaiTranFolder + "/Sprites";
            foreach (var folder in AssetDatabase.GetSubFolders(BatQuaiTranFolder))
            {
                var name = Path.GetFileName(folder);
                var model = FindMeshyAsset<GameObject>(folder, "t:Model", "");
                if (model == null)
                    continue;
                var defaults = BatQuaiTranDefaults.TryGetValue(name, out var found)
                    ? found
                    : (axis: PropSpriteRenderConfig.SizeAxis.Footprint, size: 2f, yaws: new[] { 0f });
                config.props.Add(new PropSpriteRenderConfig.Prop
                {
                    name = name,
                    model = model,
                    baseColor = FindMeshyAsset<Texture2D>(folder, "t:Texture2D", ""),
                    normalMap = FindMeshyAsset<Texture2D>(folder, "t:Texture2D", "_normal"),
                    sizeAxis = defaults.axis,
                    size = defaults.size,
                    yaws = new List<float>(defaults.yaws),
                });
            }
            AssetDatabase.CreateAsset(config, BatQuaiTranConfigPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Arena3Q] Created {BatQuaiTranConfigPath} with {config.props.Count} props.");
            return config;
        }

        /// <summary>Finds the Meshy asset in a folder whose file name (without extension) ends with the suffix.</summary>
        private static T FindMeshyAsset<T>(string folder, string filter, string suffix) where T : Object
        {
            foreach (var guid in AssetDatabase.FindAssets(filter, new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var file = Path.GetFileNameWithoutExtension(path);
                if (!file.StartsWith(MeshyPrefix, StringComparison.Ordinal))
                    continue;
                var isData = file.EndsWith("_normal", StringComparison.Ordinal)
                             || file.EndsWith("_metallic", StringComparison.Ordinal)
                             || file.EndsWith("_roughness", StringComparison.Ordinal);
                if (suffix.Length > 0 ? file.EndsWith(suffix, StringComparison.Ordinal) : !isData)
                    return AssetDatabase.LoadAssetAtPath<T>(path);
            }
            return null;
        }

        public static void Render(PropSpriteRenderConfig config)
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
                foreach (var guid in AssetDatabase.FindAssets("PropCapture t:Shader"))
                    shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));
            if (shader == null)
                throw new InvalidOperationException($"{ShaderName} not found.");
            Directory.CreateDirectory(config.outputFolder);
            foreach (var prop in config.props)
            {
                if (prop.model == null || prop.baseColor == null)
                {
                    Debug.LogWarning($"[Arena3Q] {config.name}: prop '{prop.name}' has no model or base colour, skipped.");
                    continue;
                }
                var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    material.SetTexture(BaseMapId, prop.baseColor);
                    material.SetTexture(NormalMapId, prop.normalMap);
                    material.SetFloat(HasNormalMapId, prop.normalMap != null ? 1f : 0f);
                    RenderProp(config, prop, material);
                }
                finally
                {
                    Object.DestroyImmediate(material);
                }
            }
        }

        private static void RenderProp(PropSpriteRenderConfig config, PropSpriteRenderConfig.Prop prop, Material material)
        {
            // Keep the root's rotation: the FBX importer puts the Z-up → Y-up axis conversion there.
            var fix = Matrix4x4.Rotate(Quaternion.Euler(prop.rotation));
            var parts = new List<(Mesh mesh, Matrix4x4 matrix)>();
            foreach (var filter in prop.model.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh != null)
                    parts.Add((filter.sharedMesh, fix * filter.transform.localToWorldMatrix));
            foreach (var skinned in prop.model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (skinned.sharedMesh != null)
                    parts.Add((skinned.sharedMesh, fix * skinned.transform.localToWorldMatrix));
            if (parts.Count == 0)
            {
                Debug.LogWarning($"[Arena3Q] {prop.name}: the model has no mesh, skipped.");
                return;
            }

            var corners = new List<Vector3>();
            foreach (var (mesh, matrix) in parts)
                AddCorners(corners, mesh.bounds, matrix);
            var bounds = new Bounds(corners[0], Vector3.zero);
            foreach (var corner in corners)
                bounds.Encapsulate(corner);
            var scale = prop.size / (prop.sizeAxis == PropSpriteRenderConfig.SizeAxis.Height
                ? bounds.size.y
                : Mathf.Max(bounds.size.x, bounds.size.z));
            Debug.Log($"[Arena3Q] {prop.name}: model {bounds.size.x:0.###}×{bounds.size.y:0.###}×{bounds.size.z:0.###} "
                      + $"(x×y×z), root rotation {prop.model.transform.localEulerAngles}, scale {scale:0.###} → "
                      + $"{bounds.size.x * scale:0.##}×{bounds.size.y * scale:0.##}×{bounds.size.z * scale:0.##} units");
            var groundCentre = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

            foreach (var yaw in prop.yaws)
            {
                var model = Matrix4x4.Rotate(Quaternion.Euler(0f, yaw, 0f))
                            * Matrix4x4.Scale(Vector3.one * scale)
                            * Matrix4x4.Translate(-groundCentre);
                var file = prop.yaws.Count > 1
                    ? $"{prop.name}_{yaw.ToString("000.#", CultureInfo.InvariantCulture)}"
                    : prop.name;
                RenderSprite(config, parts, corners, model, material, $"{config.outputFolder}/{file}.png");
            }
        }

        private static void RenderSprite(PropSpriteRenderConfig config, List<(Mesh mesh, Matrix4x4 matrix)> parts,
            List<Vector3> modelCorners, Matrix4x4 model, Material material, string path)
        {
            // Orthographic camera tilted so the ground's depth axis shrinks to verticalScale on screen.
            var pitch = Mathf.Asin(config.verticalScale) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(pitch, 0f, 0f);
            var cameraToWorld = Matrix4x4.TRS(rotation * Vector3.back * 100f, rotation, Vector3.one);
            var view = Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) * cameraToWorld.inverse;

            var azimuth = config.lightAzimuth * Mathf.Deg2Rad;
            var elevation = config.lightElevation * Mathf.Deg2Rad;
            var lightDir = new Vector3(Mathf.Sin(azimuth) * Mathf.Cos(elevation), Mathf.Sin(elevation),
                Mathf.Cos(azimuth) * Mathf.Cos(elevation));

            // Conservative view-space box of the model (and its shadow); the result is cropped afterwards.
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = -min;
            foreach (var corner in modelCorners)
            {
                var world = model.MultiplyPoint3x4(corner);
                Include(view.MultiplyPoint3x4(world), ref min, ref max);
                if (config.castShadow)
                {
                    var onGround = world - lightDir * (Mathf.Max(world.y, 0f) / lightDir.y);
                    onGround.y = 0f;
                    Include(view.MultiplyPoint3x4(onGround), ref min, ref max);
                }
            }
            var ppu = config.pixelsPerUnit;
            var margin = config.padding + (config.castShadow ? 3f * config.shadowBlur : 0f);
            // Snap so the ground centre (view-space origin) falls on a pixel corner.
            var left = Mathf.Floor((min.x - margin) * ppu) / ppu;
            var bottom = Mathf.Floor((min.y - margin) * ppu) / ppu;
            var width = Mathf.CeilToInt((max.x + margin - left) * ppu);
            var height = Mathf.CeilToInt((max.y + margin - bottom) * ppu);
            var ss = Mathf.Max(1, Mathf.Min(config.supersampling, MaxRenderSize / Mathf.Max(width, height)));
            if (width * ss > MaxRenderSize || height * ss > MaxRenderSize)
                throw new InvalidOperationException($"{path}: {width}×{height} px is too large to render; lower the footprint or pixelsPerUnit.");

            var projection = Matrix4x4.Ortho(left, left + (float)width / ppu, bottom, bottom + (float)height / ppu,
                -max.z - 1f, -min.z + 1f);
            var viewProj = GL.GetGPUProjectionMatrix(projection, true) * view;
            material.SetMatrix(PropViewProjId, viewProj);
            material.SetVector(LightDirId, lightDir);
            material.SetFloat(AmbientId, config.ambient);
            material.SetFloat(DiffuseId, config.diffuse);
            material.SetFloat(LightWrapId, config.lightWrap);

            var renderWidth = width * ss;
            var renderHeight = height * ss;
            var colourTarget = RenderTexture.GetTemporary(new RenderTextureDescriptor(renderWidth, renderHeight,
                RenderTextureFormat.ARGB32, 24) { sRGB = true });
            var shadowTarget = RenderTexture.GetTemporary(new RenderTextureDescriptor(renderWidth, renderHeight,
                RenderTextureFormat.ARGB32, 0) { sRGB = false });
            Color32[] colour, shadow;
            try
            {
                using (var commands = new CommandBuffer { name = "Arena3Q Prop Capture" })
                {
                    commands.SetRenderTarget(colourTarget);
                    commands.ClearRenderTarget(true, true, Color.clear);
                    Draw(commands, parts, model, material, 0);
                    commands.SetRenderTarget(shadowTarget);
                    commands.ClearRenderTarget(false, true, Color.clear);
                    if (config.castShadow)
                        Draw(commands, parts, model, material, 1);
                    Graphics.ExecuteCommandBuffer(commands);
                }
                colour = ReadBack(colourTarget, linear: false);
                shadow = ReadBack(shadowTarget, linear: true);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(colourTarget);
                RenderTexture.ReleaseTemporary(shadowTarget);
            }

            var pixels = Compose(config, colour, shadow, width, height, ss);
            var originX = -left * ppu;
            var originY = -bottom * ppu;
            Crop(pixels, width, height, Mathf.CeilToInt(config.padding * ppu), out var crop);
            var cropped = new Color32[crop.width * crop.height];
            for (var y = 0; y < crop.height; y++)
                Array.Copy(pixels, (crop.y + y) * width + crop.x, cropped, y * crop.width, crop.width);
            var pivot = new Vector2((originX - crop.x) / crop.width, (originY - crop.y) / crop.height);
            WriteSprite(config, path, cropped, crop.width, crop.height, pivot);
            Debug.Log($"[Arena3Q] {path}: {crop.width}×{crop.height} px "
                      + $"({(float)crop.width / ppu:0.##}×{(float)crop.height / ppu:0.##} units), pivot {pivot.x:0.###}, {pivot.y:0.###}");
        }

        private static void Draw(CommandBuffer commands, List<(Mesh mesh, Matrix4x4 matrix)> parts, Matrix4x4 model,
            Material material, int pass)
        {
            foreach (var (mesh, matrix) in parts)
            {
                var block = new MaterialPropertyBlock();
                block.SetMatrix(PropModelId, model * matrix);
                for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
                    commands.DrawMesh(mesh, Matrix4x4.identity, material, submesh, pass, block);
            }
        }

        private static Color32[] ReadBack(RenderTexture target, bool linear)
        {
            var previous = RenderTexture.active;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false, linear);
            try
            {
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                return texture.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(texture);
            }
        }

        /// <summary>Averages each ss×ss block (premultiplied) and lays the blurred shadow under the model.</summary>
        private static Color32[] Compose(PropSpriteRenderConfig config, Color32[] colour, Color32[] shadow,
            int width, int height, int ss)
        {
            var rendered = width * ss;
            var r = new float[width * height];
            var g = new float[width * height];
            var b = new float[width * height];
            var a = new float[width * height];
            var mask = new float[width * height];
            var samples = 1f / (ss * ss * 255f);
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                float sr = 0, sg = 0, sb = 0, sa = 0, sm = 0;
                for (var dy = 0; dy < ss; dy++)
                for (var dx = 0; dx < ss; dx++)
                {
                    var c = colour[(y * ss + dy) * rendered + x * ss + dx];
                    var alpha = c.a / 255f;
                    sr += c.r * alpha;
                    sg += c.g * alpha;
                    sb += c.b * alpha;
                    sa += c.a;
                    sm += shadow[(y * ss + dy) * rendered + x * ss + dx].r;
                }
                var i = y * width + x;
                r[i] = sr * samples;
                g[i] = sg * samples;
                b[i] = sb * samples;
                a[i] = sa * samples;
                mask[i] = sm * samples;
            }

            if (config.castShadow)
                BoxBlur(mask, width, height, Mathf.RoundToInt(config.shadowBlur * config.pixelsPerUnit), passes: 3);

            var shade = config.shadowColor;
            var pixels = new Color32[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                var shadowAlpha = config.castShadow ? mask[i] * shade.a * (1f - a[i]) : 0f;
                var alpha = a[i] + shadowAlpha;
                if (alpha <= 0f)
                    continue;
                pixels[i] = new Color(
                    (r[i] + shade.r * shadowAlpha) / alpha,
                    (g[i] + shade.g * shadowAlpha) / alpha,
                    (b[i] + shade.b * shadowAlpha) / alpha,
                    alpha);
            }
            return pixels;
        }

        private static void BoxBlur(float[] values, int width, int height, int radius, int passes)
        {
            if (radius <= 0)
                return;
            var temp = new float[values.Length];
            for (var pass = 0; pass < passes; pass++)
            {
                BlurLine(values, temp, width, height, radius, horizontal: true);
                BlurLine(temp, values, width, height, radius, horizontal: false);
            }
        }

        private static void BlurLine(float[] source, float[] target, int width, int height, int radius, bool horizontal)
        {
            var lines = horizontal ? height : width;
            var length = horizontal ? width : height;
            var weight = 1f / (2 * radius + 1);
            for (var line = 0; line < lines; line++)
            {
                int Index(int k) => horizontal ? line * width + k : k * width + line;
                var sum = 0f;
                for (var k = -radius; k <= radius; k++)
                    sum += k >= 0 && k < length ? source[Index(k)] : 0f;
                for (var k = 0; k < length; k++)
                {
                    target[Index(k)] = sum * weight;
                    var outgoing = k - radius;
                    var incoming = k + radius + 1;
                    if (outgoing >= 0)
                        sum -= source[Index(outgoing)];
                    if (incoming < length)
                        sum += source[Index(incoming)];
                }
            }
        }

        /// <summary>Tight box around the visible pixels plus padding (clamped to the image).</summary>
        private static void Crop(Color32[] pixels, int width, int height, int padding, out RectInt crop)
        {
            int minX = width, minY = height, maxX = -1, maxY = -1;
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a == 0)
                    continue;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
            if (maxX < 0)
            {
                crop = new RectInt(0, 0, width, height);
                return;
            }
            minX = Mathf.Max(0, minX - padding);
            minY = Mathf.Max(0, minY - padding);
            maxX = Mathf.Min(width - 1, maxX + padding);
            maxY = Mathf.Min(height - 1, maxY + padding);
            crop = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static void WriteSprite(PropSpriteRenderConfig config, string path, Color32[] pixels, int width,
            int height, Vector2 pivot)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = config.pixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = config.maxTextureSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void AddCorners(List<Vector3> corners, Bounds bounds, Matrix4x4 matrix)
        {
            for (var i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (i & 4) == 0 ? bounds.min.z : bounds.max.z);
                corners.Add(matrix.MultiplyPoint3x4(corner));
            }
        }

        private static void Include(Vector3 point, ref Vector3 min, ref Vector3 max)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }
    }
}
