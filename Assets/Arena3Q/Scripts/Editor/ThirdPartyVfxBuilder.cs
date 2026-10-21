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

        // Mặc Ảnh's palette (spec §14): violet band #9657EF, gold trim #E6C47C.
        private static readonly float Violet = Hue(new Color(.588f, .341f, .937f));
        private static readonly float Gold = Hue(new Color(.902f, .769f, .486f));

        /// <summary>
        /// The redesigned Ultimate (user 2026-10-22): the ground circle of Hovl's "Laser AOE" without its falling lasers,
        /// in Mặc Ảnh's colours, spreading from her feet; "Star aura" (violet streaks) on every hero it buffs.
        /// </summary>
        [MenuItem("Arena3Q/Mac Anh/Third Party/Use Hovl Rune Circle and Star Aura for the Ultimate")]
        public static void UseHovlUltimate()
        {
            if (!AssetDatabase.IsValidFolder(Hovl))
                throw new InvalidOperationException(Hovl + " is missing: import Hovl Studio's Magic Effects FREE first.");
            UpgradeMaterials(Hovl);
            var circle = Wrap(Hovl + "/Prefabs/Laser AOE.prefab", Hovl + "/Battle/FX_MacAnh_Ultimate_Hovl_Battle.prefab",
                MacAnhFxKind.Ultimate, 4.2f, effect =>
                {
                    // The lasers: the root system's stretched beams, the "Shoot" streaks and their hits on the ground.
                    effect.GetComponent<ParticleSystemRenderer>().enabled = false;
                    foreach (var part in new[] { "Shoot", "Sparks", "Flash" })
                        Find(effect, part).gameObject.SetActive(false);
                    Recolour(effect, Violet, ("TechCircle2", Gold));
                }, "TechCircleSmall", "Glow");
            var aura = Wrap(Hovl + "/Prefabs/Star aura.prefab", Hovl + "/Battle/FX_MacAnh_UltimateBuff_Hovl_Battle.prefab",
                MacAnhFxKind.UltimateBuff, 1f, effect => Recolour(effect, Violet));
            Bind("Ultimate", circle);
            Bind("UltimateBuff", aura);
            AssetDatabase.SaveAssets();
            Debug.Log("[Arena3Q] Hovl rune circle + star aura now play for the Ultimate.");
        }

        private static Transform Find(GameObject root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name)
                    return t;
            throw new InvalidOperationException($"{root.name} has no part {name}");
        }

        private static float Hue(Color c)
        {
            Color.RGBToHSV(c, out var h, out _, out _);
            return h;
        }

        // Every colour of the effect turned to one hue (saturation, brightness and alpha kept: the textures are grey, the
        // colours come from the particles and materials); materials are tinted copies, the store originals stay as they are.
        private static void Recolour(GameObject effect, float hue, params (string part, float hue)[] exceptions)
        {
            foreach (var system in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var h = hue;
                foreach (var exception in exceptions)
                    if (system.name == exception.part)
                        h = exception.hue;
                var main = system.main;
                main.startColor = Shift(main.startColor, h);
                var overLife = system.colorOverLifetime;
                if (overLife.enabled)
                    overLife.color = Shift(overLife.color, h);
                var bySpeed = system.colorBySpeed;
                if (bySpeed.enabled)
                    bySpeed.color = Shift(bySpeed.color, h);
                var trails = system.trails;
                if (trails.enabled)
                {
                    trails.colorOverLifetime = Shift(trails.colorOverLifetime, h);
                    trails.colorOverTrail = Shift(trails.colorOverTrail, h);
                }
                var renderer = system.GetComponent<ParticleSystemRenderer>();
                if (renderer == null)
                    continue;
                renderer.sharedMaterial = Tinted(renderer.sharedMaterial, h);
                if (renderer.trailMaterial != null)
                    renderer.trailMaterial = Tinted(renderer.trailMaterial, h);
            }
        }

        // Near-white store colours keep a little of the hue only: lift them to at least MinSaturation so they read violet / gold.
        private const float MinSaturation = .6f;

        private static Color Shift(Color c, float hue)
        {
            Color.RGBToHSV(c, out _, out var s, out var v);
            var shifted = Color.HSVToRGB(hue, Mathf.Max(s, MinSaturation), v, true);
            shifted.a = c.a;
            return shifted;
        }

        private static Gradient Shift(Gradient g, float hue)
        {
            if (g == null)
                return null;
            var keys = g.colorKeys;
            for (var i = 0; i < keys.Length; i++)
                keys[i].color = Shift(keys[i].color, hue);
            return new Gradient { colorKeys = keys, alphaKeys = g.alphaKeys, mode = g.mode };
        }

        private static ParticleSystem.MinMaxGradient Shift(ParticleSystem.MinMaxGradient g, float hue)
        {
            switch (g.mode)
            {
                case ParticleSystemGradientMode.Color: return new ParticleSystem.MinMaxGradient(Shift(g.color, hue));
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(Shift(g.colorMin, hue), Shift(g.colorMax, hue));
                case ParticleSystemGradientMode.Gradient: return new ParticleSystem.MinMaxGradient(Shift(g.gradient, hue));
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(Shift(g.gradientMin, hue), Shift(g.gradientMax, hue));
                default:
                    var random = new ParticleSystem.MinMaxGradient(Shift(g.gradient, hue)) { mode = ParticleSystemGradientMode.RandomColor };
                    return random;
            }
        }

        private static Material Tinted(Material source, float hue)
        {
            if (source == null)
                return null;
            var folder = Hovl + "/MacAnhColours";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(Hovl, "MacAnhColours");
            var path = $"{folder}/{source.name}_h{Mathf.RoundToInt(hue * 360f)}_s{Mathf.RoundToInt(MinSaturation * 100f)}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;
            var copy = new Material(source);
            foreach (var property in new[] { "_BaseColor", "_EmissionColor" })
                if (copy.HasProperty(property))
                    copy.SetColor(property, Shift(copy.GetColor(property), hue));
            AssetDatabase.CreateAsset(copy, path);
            return copy;
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

        private static GameObject Wrap(string sourcePath, string prefabPath, MacAnhFxKind kind, float authoredRadius,
            Action<GameObject> adapt = null, params string[] windUpParts)
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
                adapt?.Invoke(effect);
                presenter.Configure(turned, authoredRadius, windUpParts);
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
