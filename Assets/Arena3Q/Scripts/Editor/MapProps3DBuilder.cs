using System.Collections.Generic;
using System.IO;
using Arena3Q.Data;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>
    /// Arena3Q > Build 3D Map Props (docs/KeHoach/ChuyenSang3D.md P2): turns the battle-budget copies of the Meshy props
    /// (<c>Props3D/&lt;prop&gt;.fbx</c> + 1024 textures, made by <c>Props3D/Tools~/decimate_prop.py</c> and
    /// <c>shrink_textures.ps1</c>) into Simple Lit materials and the <see cref="MapProps3DLibrary"/> the 3D battle uses.
    /// Size, yaw and ground pivot are computed exactly as <see cref="PropSpriteRenderer"/> did for the sprites, so a
    /// model stands where its picture stood.
    /// </summary>
    public static class MapProps3DBuilder
    {
        private const string ConfigPath = "Assets/Arena3Q/Art/Maps/batQuaiTran/PropSprites.asset";
        private const string Folder = "Assets/Arena3Q/Art/Maps/batQuaiTran/Props3D";
        private const string LibraryPath = "Assets/Arena3Q/Content/Maps/MapProps3D.asset";
        private const string ViewPath = "Assets/Arena3Q/Content/Resources/BattleView.asset";
        private const string SimpleLitShader = "Universal Render Pipeline/Simple Lit";

        [MenuItem("Arena3Q/Build 3D Map Props")]
        public static void BuildAll()
        {
            var config = AssetDatabase.LoadAssetAtPath<PropSpriteRenderConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogError($"[Arena3Q] Missing {ConfigPath}.");
                return;
            }
            var props = new List<MapProps3DLibrary.Prop>();
            foreach (var prop in config.props)
            {
                var entry = BuildProp(prop);
                if (entry != null)
                    props.Add(entry);
            }

            var library = AssetDatabase.LoadAssetAtPath<MapProps3DLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<MapProps3DLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            library.SetProps(props.ToArray());
            EditorUtility.SetDirty(library);

            var view = AssetDatabase.LoadAssetAtPath<BattleViewConfig>(ViewPath);
            if (view != null)
            {
                var serialized = new SerializedObject(view);
                serialized.FindProperty("mapProps").objectReferenceValue = library;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Arena3Q] 3D map props: {props.Count} of {config.props.Count} built into {LibraryPath}.");
        }

        private static MapProps3DLibrary.Prop BuildProp(PropSpriteRenderConfig.Prop prop)
        {
            var modelPath = $"{Folder}/{prop.name}.fbx";
            if (!File.Exists(modelPath))
            {
                Debug.LogWarning($"[Arena3Q] {prop.name}: no {modelPath} yet (run Props3D/Tools~/decimate_prop.py), skipped.");
                return null;
            }
            ConfigureModel(modelPath);
            var baseColor = ConfigureTexture($"{Folder}/{prop.name}_BaseColor.jpg", normalMap: false);
            var normal = ConfigureTexture($"{Folder}/{prop.name}_Normal.jpg", normalMap: true);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var filter = model.GetComponentInChildren<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                Debug.LogWarning($"[Arena3Q] {prop.name}: {modelPath} has no mesh, skipped.");
                return null;
            }
            // Same sizing as PropSpriteRenderer.RenderProp: bounds after the orientation fix, size along one axis,
            // pivot = centre of the footprint on the ground.
            var meshToModel = Matrix4x4.Rotate(Quaternion.Euler(prop.rotation)) * filter.transform.localToWorldMatrix;
            var bounds = TransformedBounds(filter.sharedMesh.bounds, meshToModel);
            var scale = prop.size / (prop.sizeAxis == PropSpriteRenderConfig.SizeAxis.Height
                ? bounds.size.y
                : Mathf.Max(bounds.size.x, bounds.size.z));

            var material = Material(prop.name, baseColor, normal);
            Debug.Log($"[Arena3Q] {prop.name}: {filter.sharedMesh.triangles.Length / 3} triangles, scale {scale:0.###}, " +
                      $"{bounds.size.x * scale:0.##}×{bounds.size.y * scale:0.##}×{bounds.size.z * scale:0.##} units");
            return new MapProps3DLibrary.Prop
            {
                name = prop.name,
                mesh = filter.sharedMesh,
                material = material,
                meshPosition = meshToModel.GetPosition(),
                meshRotation = meshToModel.rotation,
                meshScale = meshToModel.lossyScale,
                scale = scale,
                groundCentre = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z),
                footprint = Mathf.Max(bounds.size.x, bounds.size.z) * scale
            };
        }

        private static Bounds TransformedBounds(Bounds local, Matrix4x4 matrix)
        {
            var result = new Bounds(matrix.MultiplyPoint3x4(local.min), Vector3.zero);
            for (var i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? local.min.x : local.max.x, (i & 2) == 0 ? local.min.y : local.max.y,
                    (i & 4) == 0 ? local.min.z : local.max.z);
                result.Encapsulate(matrix.MultiplyPoint3x4(corner));
            }
            return result;
        }

        private static void ConfigureModel(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer))
                return;
            var changed = importer.materialImportMode != ModelImporterMaterialImportMode.None || importer.importAnimation ||
                          importer.animationType != ModelImporterAnimationType.None || importer.isReadable ||
                          importer.importNormals != ModelImporterNormals.Import;
            if (!changed)
                return;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.isReadable = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.SaveAndReimport();
        }

        private static Texture2D ConfigureTexture(string path, bool normalMap)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                return null;
            var type = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != type || importer.maxTextureSize != 1024 || importer.sRGBTexture == normalMap)
            {
                importer.textureType = type;
                importer.maxTextureSize = 1024;
                importer.sRGBTexture = !normalMap;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material Material(string name, Texture2D baseColor, Texture2D normal)
        {
            var path = $"{Folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(SimpleLitShader)) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = Shader.Find(SimpleLitShader);
            material.SetTexture("_BaseMap", baseColor);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.1f);
            material.SetFloat("_SpecularHighlights", 0f);
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.SetFloat("_ReceiveShadows", 0f);
            material.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
