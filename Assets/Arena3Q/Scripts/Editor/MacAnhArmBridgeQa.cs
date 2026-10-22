using System.Reflection;
using UnityEditor;

namespace Arena3Q.Editor
{
    public static class MacAnhArmBridgeQa
    {
        public static void BuildAndCapture()
        {
            AssetDatabase.Refresh();
            typeof(MacAnhRealtimeBattleBuilder).GetMethod("BuildCharacter", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { "Battle" });
            AssetDatabase.SaveAssets();
            MacAnhPoseCapture.Run();
            MacAnhImportedSurfaceQa.Capture("Battle");
        }
    }
}
