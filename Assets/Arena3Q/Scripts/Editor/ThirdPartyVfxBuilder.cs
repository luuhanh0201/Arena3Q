using System;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>
    /// Puts a store effect in place of one of Mặc Ảnh's (user 2026-10-21: Hovl Studio's free "Magic circle 2" for
    /// Skill01 Huyền Cơ Trận, used as it is). The store files live in Assets/ThirdParty, which is not in the public
    /// repository (Asset Store EULA): this tool upgrades their materials to URP with Unity's own particle upgrader,
    /// wraps the original prefab in a battle prefab (MacAnhFx + ParticleFxPresenter, turned from Y-up to the battle's
    /// up = -Z) and lists it in MacAnhPerformance.thirdPartyEffects; without the files the own effect keeps playing.
    /// </summary>
    public static class ThirdPartyVfxBuilder
    {
        private const string Hovl = "Assets/ThirdParty/HovlStudio/MagicEffectsFree";
        private const string Performance = "Assets/Arena3Q/Art/Heroes/MacAnh/MacAnhPerformance.asset";

        [MenuItem("Arena3Q/Mac Anh/Third Party/Use Hovl Magic Circle 2 for Skill01")]
        public static void UseHovlMagicCircle()
        {
            if (!AssetDatabase.IsValidFolder(Hovl))
                throw new InvalidOperationException(Hovl + " is missing: import Hovl Studio's Magic Effects FREE first.");
            UpgradeMaterials(Hovl);
            var prefab = Wrap(Hovl + "/Prefabs/Magic circle 2.prefab", Hovl + "/Battle/FX_MacAnh_MagicArray_Hovl_Battle.prefab",
                MacAnhFxKind.MagicArray, 1.4f);   // its rune ring at the skill radius
            Bind("MagicArray", prefab);
            AssetDatabase.SaveAssets();
            Debug.Log("[Arena3Q] Hovl Magic circle 2 now plays for Skill01 (MagicArray).");
        }

        [MenuItem("Arena3Q/Mac Anh/Third Party/Back to Own Skill01 Effect")]
        public static void BackToOwnEffect()
        {
            Bind("MagicArray", null);
            AssetDatabase.SaveAssets();
        }

        // Built-in particle materials (pink in URP) → URP particle shaders, with Unity's own upgrader.
        private static void UpgradeMaterials(string folder)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                var shader = material.shader != null ? material.shader.name : "";
                if (!shader.StartsWith("Particles/", StringComparison.Ordinal))
                    continue;
                MaterialUpgrader.Upgrade(material, new ParticleUpgrader(shader), MaterialUpgrader.UpgradeFlags.None);
                EditorUtility.SetDirty(material);
                Debug.Log($"[Arena3Q] {material.name}: {shader} → {material.shader.name}");
            }
        }

        private static GameObject Wrap(string sourcePath, string prefabPath, MacAnhFxKind kind, float authoredRadius)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath)
                         ?? throw new InvalidOperationException("Missing " + sourcePath);
            var root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefabPath));
            try
            {
                // Authored with Y up; the battle ground is XY with up = -Z (same turn as the map props).
                var turned = new GameObject("Y up to battle up").transform;
                turned.SetParent(root.transform, false);
                turned.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                var effect = (GameObject)PrefabUtility.InstantiatePrefab(source, turned);
                foreach (var system in effect.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = system.main;
                    main.playOnAwake = false;      // the battle clock drives it
                }
                var presenter = root.AddComponent<ParticleFxPresenter>();
                presenter.Configure(turned, authoredRadius);
                root.AddComponent<MacAnhFx>().ConfigureSpatial(kind, presenter);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(prefabPath));
                return PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void Bind(string key, GameObject prefab)
        {
            var profile = AssetDatabase.LoadAssetAtPath<MacAnhPerformance>(Performance);
            var data = new SerializedObject(profile);
            var list = data.FindProperty("thirdPartyEffects");
            var index = -1;
            for (var i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == key)
                    index = i;
            if (prefab == null)
            {
                if (index >= 0)
                    list.DeleteArrayElementAtIndex(index);
            }
            else
            {
                if (index < 0)
                {
                    index = list.arraySize;
                    list.InsertArrayElementAtIndex(index);
                }
                var item = list.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("key").stringValue = key;
                item.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
