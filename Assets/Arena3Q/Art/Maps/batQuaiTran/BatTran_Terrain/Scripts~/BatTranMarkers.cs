// Arena3Q — draws Bat Tran markers and lanes in the Scene view (no runtime cost).
// Put this file in a NON-Editor folder, e.g. Assets/Scripts/Maps/BatTranMarkers.cs
using UnityEngine;

public class BatTranMarkers : MonoBehaviour
{
    public float gateWidth = 2.5f, gateHeight = 1.8f, gateThickness = 0.4f;
    public float pillarRadius = 1.0f, baseRadius = 3.0f, altarRadius = 2.0f;

    void OnDrawGizmos()
    {
        foreach (Transform t in GetComponentsInChildren<Transform>())
        {
            if (t == transform) continue;
            string n = t.name;
            if (n.StartsWith("Base_")) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(t.position, baseRadius); }
            else if (n.StartsWith("Formation_Center")) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(t.position, altarRadius); }
            else if (n.StartsWith("Pillar_")) { Gizmos.color = Color.gray; Gizmos.DrawWireSphere(t.position + Vector3.up, pillarRadius); }
            else if (n.StartsWith("Spawn_")) { Gizmos.color = Color.white; Gizmos.DrawSphere(t.position, 0.25f); }
            else if (n.StartsWith("Gate_"))
            {
                Gizmos.color = Color.cyan;
                Gizmos.matrix = t.localToWorldMatrix;   // local X = gate width (tangent), local Z = outward
                Gizmos.DrawWireCube(new Vector3(0, gateHeight / 2, 0), new Vector3(gateWidth, gateHeight, gateThickness));
                Gizmos.matrix = Matrix4x4.identity;
            }
            else if (n.StartsWith("Lane_") && t.childCount > 1)
            {
                Gizmos.color = n.StartsWith("Lane_Center") ? Color.yellow : Color.green;
                for (int i = 1; i < t.childCount; i++)
                    Gizmos.DrawLine(t.GetChild(i - 1).position, t.GetChild(i).position);
            }
        }
    }
}
