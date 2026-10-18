#if UNITY_EDITOR
// Edits stage copies through SerializedObject, so it exists in the Editor only (like the tests using it).
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Tests.PlayMode
{
    /// <summary>Stage edits shared by the PlayMode tests.</summary>
    internal static class TestStages
    {
        public static readonly Vector2 NorthBridge = new Vector2(0f, 6.7f);
        public static readonly Vector2 SouthBridge = new Vector2(0f, -4.85f);

        /// <summary>
        /// The shipped map is the Bát Quái Trận (no river); river behaviour is tested on a copy with a straight river
        /// down the middle (x = 0, width 2.3) and two 2.4-wide bridges, without the formation or the painted ground.
        /// </summary>
        public static void UseRiverInsteadOfFormation(SerializedObject so)
        {
            so.FindProperty("formation").objectReferenceValue = null;
            so.FindProperty("backgroundArt").objectReferenceValue = null;
            var rivers = so.FindProperty("rivers");
            rivers.arraySize = 1;
            var river = rivers.GetArrayElementAtIndex(0);
            river.FindPropertyRelative("name").stringValue = "Sông";
            river.FindPropertyRelative("width").floatValue = 2.3f;
            var path = river.FindPropertyRelative("path");
            path.arraySize = 2;
            path.GetArrayElementAtIndex(0).vector2Value = new Vector2(0f, 14f);
            path.GetArrayElementAtIndex(1).vector2Value = new Vector2(0f, -14f);
            var crossings = river.FindPropertyRelative("crossings");
            crossings.arraySize = 2;
            var bridges = new[] { NorthBridge, SouthBridge };
            for (var i = 0; i < bridges.Length; i++)
            {
                var crossing = crossings.GetArrayElementAtIndex(i);
                crossing.FindPropertyRelative("position").vector2Value = bridges[i];
                crossing.FindPropertyRelative("width").floatValue = 2.4f;
            }
        }
    }
}
#endif
