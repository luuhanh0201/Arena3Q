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

        public static void BuildFullReview()
        {
            BuildAndCapture();
            MacAnhGaitDiagnostic.Run();
            MacAnhPoseCapture.RunFullReview();
        }

        public static void BuildReviewerFrames()
        {
            BuildAndCapture();
            MacAnhPoseCapture.RunReviewerFrames();
        }

        public static void BuildAnatomyReview()
        {
            BuildAndCapture();
            MacAnhPoseCapture.RunAnatomyAfter();
        }
    }
}
