using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Arena3Q.Editor.Chapter01
{
    /// <summary>Rebuilds local review prefabs from sf_normalize.py; never changes the chapter map.</summary>
    public static class Chapter01SketchfabImport
    {
        private const string Root = "Assets/ThirdParty/Chapter01/Sketchfab";
        private const string Output = "ArtSource/Chapter01/3D/Renders/Sketchfab/Unity";
        [Serializable] private sealed class Manifest { public string code; public Piece[] pieces; public Surface[] materials; }
        [Serializable] private sealed class Piece
        {
            public string name, path, pivot; public int triangles, degenerate, uvLayers;
            public float[] size; public string[] materials;
        }
        [Serializable] private sealed class Surface
        {
            public string name, baseMap, normalMap, metalMap, emissionMap; public float[] color, emission;
            public float metallic, smoothness, cutoff; public bool cutout, doubleSided;
        }

        public static void Build()
        {
            AudioListener.volume = 0f;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory(Output);
            var report = new StringBuilder("source\tpiece\ttriangles\tmaterials\theight_m\tpivot\tdegenerate_source\n");
            int count = 0;
            foreach (string file in Directory.GetFiles(Root, "manifest.json", SearchOption.AllDirectories).OrderBy(p => p))
            {
                var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(file));
                string folder = Root + "/" + manifest.code;
                string matFolder = folder + "/Materials";
                Directory.CreateDirectory(matFolder); AssetDatabase.Refresh();
                // Import textures first: reimports can invalidate native importer/material references.
                foreach (var s in manifest.materials)
                {
                    Texture(s.baseMap, false, true); Texture(s.normalMap, true, false); Texture(s.metalMap, false, false); Texture(s.emissionMap, false, true);
                }
                var surfaces = manifest.materials.ToDictionary(s => s.name, s => MakeMaterial(s, matFolder));
                foreach (var p in manifest.pieces)
                {
                    var importer = (ModelImporter)AssetImporter.GetAtPath(p.path);
                    if (importer == null) throw new InvalidOperationException("Missing FBX: " + p.path);
                    importer.isReadable = true; importer.importAnimation = false; importer.importCameras = false;
                    importer.importLights = false; importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                    importer.importNormals = ModelImporterNormals.Import; importer.addCollider = false;
                    importer.SaveAndReimport();
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(p.path);
                    var instance = Object.Instantiate(model); instance.name = p.name;
                    try
                    {
                        int triangles = 0;
                        foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>())
                        {
                            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                            triangles += mesh.triangles.Length / 3;
                            if (mesh.uv.Length == 0 && p.uvLayers > 0) throw new InvalidOperationException("Lost UV: " + p.name);
                            if (mesh.vertices.Any(v => !float.IsFinite(v.x) || !float.IsFinite(v.y) || !float.IsFinite(v.z))) throw new InvalidOperationException("Nonfinite: " + p.name);
                            var mats = renderer.sharedMaterials;
                            for (int i = 0; i < mats.Length; i++)
                            {
                                if (mats[i] == null || !surfaces.TryGetValue(mats[i].name, out var material))
                                    throw new InvalidOperationException("Unmapped material: " + p.name + " / " + mats[i]?.name);
                                mats[i] = material;
                            }
                            renderer.sharedMaterials = mats;
                        }
                        var b = Chapter01ColorReference.Bounds(instance.GetComponentsInChildren<Renderer>());
                        var expected = new Vector3(p.size[0], p.size[2], p.size[1]);
                        if ((b.size - expected).magnitude > .005f) throw new InvalidOperationException("FBX size mismatch: " + p.name + " " + b.size + " expected " + expected);
                        if (Mathf.Abs(p.pivot == "top" ? b.max.y : b.min.y) > .005f || Mathf.Abs(b.center.x) > .005f || Mathf.Abs(b.center.z) > .005f)
                            throw new InvalidOperationException("FBX pivot mismatch: " + p.name);
                        if (triangles != p.triangles) throw new InvalidOperationException("Triangle mismatch: " + p.name);
                        PrefabUtility.SaveAsPrefabAsset(instance, Path.ChangeExtension(p.path, ".prefab"));
                        report.AppendLine($"{manifest.code}\t{p.name}\t{triangles}\t{p.materials.Length}\t{b.size.y:F4}\t{p.pivot}\t{p.degenerate}");
                        count++;
                    }
                    finally { Object.DestroyImmediate(instance); }
                }
            }
            AssetDatabase.SaveAssets(); File.WriteAllText(Output + "/import.tsv", report.ToString());
            Debug.Log("SKETCHFAB_IMPORT_PASS pieces=" + count);
        }

        private static void Texture(string path, bool normal, bool srgb)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) throw new FileNotFoundException(path);
            ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = srgb; ti.mipmapEnabled = true; ti.maxTextureSize = 1024;
            ti.wrapMode = TextureWrapMode.Repeat; ti.alphaIsTransparency = false; ti.SaveAndReimport();
        }

        private static Material MakeMaterial(Surface s, string folder)
        {
            string path = folder + "/" + Chapter01ColorReference.Slug(s.name) + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.name = s.name;
            // Blender's glTF base colour factors are linear, Unity Color properties are authored in sRGB.
            mat.SetColor("_BaseColor", new Color(s.color[0], s.color[1], s.color[2], s.color[3]).gamma);
            mat.SetTexture("_BaseMap", string.IsNullOrEmpty(s.baseMap) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(s.baseMap));
            mat.SetFloat("_Metallic", s.metallic); mat.SetFloat("_Smoothness", s.smoothness);
            if (!string.IsNullOrEmpty(s.normalMap)) { mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(s.normalMap)); mat.EnableKeyword("_NORMALMAP"); }
            if (!string.IsNullOrEmpty(s.metalMap))
            {
                mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(s.metalMap));
                mat.SetFloat("_Smoothness", 1f); mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            if (s.emission != null && s.emission.Length == 3)
            {
                var emission = new Color(s.emission[0], s.emission[1], s.emission[2]);
                mat.SetColor("_EmissionColor", emission.gamma);
                if (!string.IsNullOrEmpty(s.emissionMap)) mat.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(s.emissionMap));
                if (emission.maxColorComponent > 0f) mat.EnableKeyword("_EMISSION"); else mat.DisableKeyword("_EMISSION");
            }
            mat.SetFloat("_AlphaClip", s.cutout ? 1f : 0f); mat.SetFloat("_Cutoff", s.cutoff);
            if (s.cutout) mat.EnableKeyword("_ALPHATEST_ON"); else mat.DisableKeyword("_ALPHATEST_ON");
            mat.SetOverrideTag("RenderType", s.cutout ? "TransparentCutout" : "Opaque");
            mat.renderQueue = s.cutout ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry;
            mat.SetFloat("_Cull", s.doubleSided || s.cutout ? (float)CullMode.Off : (float)CullMode.Back);
            EditorUtility.SetDirty(mat); return mat;
        }

        public static void ValidateRenders()
        {
            var lines = File.ReadAllLines(Output + "/report.tsv");
            if (lines.Length < 23 || lines.Any(l => l.Contains("\tmissing") || l.Contains("empty bounds")))
                throw new InvalidOperationException("Incomplete Sketchfab renders");
            Debug.Log("SKETCHFAB_REVIEW_PASS candidates=" + (lines.Length-1));
        }
    }
}
