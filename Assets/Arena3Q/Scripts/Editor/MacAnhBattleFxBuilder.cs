using System;
using System.Collections.Generic;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using Arena3Q.Graphics3D;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    public static class MacAnhBattleFxBuilder
    {
        const string Root=MacAnhGraphicsBuilder.Root;
        [MenuItem("Arena3Q/Mac Anh/Graphics/Bind New VFX to Battle")]
        public static void Bind()
        {
            var map=new Dictionary<string,string>{{"WindSlash","Attack01"},{"TripleFeather","Attack02"},{"MagicArray","Skill01"},{"Cyclone","Cyclone"},{"Shield","Skill03"},{"Ultimate","Ultimate"},{"CounterSlash","DodgeAttack"},{"Impact","Impact"}};
            var proxies=new Dictionary<string,GameObject>();
            foreach(var pair in map)
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/FX_MacAnh_"+pair.Value+"3D.prefab");
                if(source==null)throw new InvalidOperationException("Build the 3D VFX first: "+pair.Value);
                var root=new GameObject("FX_MacAnh_"+pair.Key+"3D_Battle");
                var child=(GameObject)PrefabUtility.InstantiatePrefab(source);child.transform.SetParent(root.transform,false);
                root.AddComponent<MacAnhFx>().ConfigureSpatial((MacAnhFxKind)Enum.Parse(typeof(MacAnhFxKind),pair.Key),child.GetComponent<MacAnhArcaneEffect>());
                proxies[pair.Key]=PrefabUtility.SaveAsPrefabAsset(root,Root+"/"+root.name+".prefab");UnityEngine.Object.DestroyImmediate(root);
            }
            var profile=AssetDatabase.LoadAssetAtPath<MacAnhPerformance>("Assets/Arena3Q/Art/Heroes/MacAnh/MacAnhPerformance.asset");
            var data=new SerializedObject(profile);var effects=data.FindProperty("effects");
            for(int i=0;i<effects.arraySize;i++)
            {
                var item=effects.GetArrayElementAtIndex(i);if(proxies.TryGetValue(item.FindPropertyRelative("key").stringValue,out var prefab))item.FindPropertyRelative("prefab").objectReferenceValue=prefab;
            }
            var skills=data.FindProperty("skills");
            for(int i=0;i<skills.arraySize;i++)
            {
                var item=skills.GetArrayElementAtIndex(i);var action=(MacAnhAction)item.FindPropertyRelative("action").enumValueIndex;
                string key=action==MacAnhAction.Attack02?"TripleFeather":action==MacAnhAction.Skill01?"MagicArray":action==MacAnhAction.Skill02?"Cyclone":action==MacAnhAction.Skill03?"Shield":action==MacAnhAction.Ultimate?"Ultimate":action==MacAnhAction.DodgeAttack?"CounterSlash":"WindSlash";
                item.FindPropertyRelative("vfx").objectReferenceValue=proxies[key];
            }
            data.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
            Debug.Log("[MacAnh Graphics] Bound eight new pooled battle VFX. Original prefabs, gameplay definitions and pose-specific sprite afterimages were retained.");
        }
    }
}
