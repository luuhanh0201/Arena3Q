using Arena3Q.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    /// <summary>The shipped camera data (user 2026-10-28): fixed diagonal view that never shows the empty background.</summary>
    public class BattleCameraContentTests
    {
        private static BattleViewConfig View => Resources.Load<BattleViewConfig>(BattleViewConfig.ResourceName);

        private static BattlefieldConfig Battlefield =>
            AssetDatabase.LoadAssetAtPath<BattlefieldConfig>("Assets/Arena3Q/Content/Battlefield/BattlefieldConfig.asset");

        [Test]
        public void View_IsTheChosenDiagonal()
        {
            Assert.That(View, Is.Not.Null);
            Assert.That(View.DefaultTiltDegrees, Is.EqualTo(40f).Within(0.01f), "50° above the ground");
            Assert.That(View.DefaultYawDegrees, Is.EqualTo(45f).Within(0.01f));
            Assert.That(Battlefield.cameraElevation, Is.EqualTo(90f - View.DefaultTiltDegrees).Within(0.01f), "editor preview matches");
            Assert.That(Battlefield.cameraYaw, Is.EqualTo(View.DefaultYawDegrees).Within(0.01f));
            Assert.That(View.ZoomInLimit, Is.InRange(0.2f, 1f));
        }

        [Test]
        public void CameraMayShowOnlyGroundTheMapSceneBuilds()
        {
            // The outer ground mesh reaches outerGroundMargin + 4 and the forest outerGroundMargin + 2 beyond the map.
            Assert.That(View.CameraGroundMargin, Is.LessThanOrEqualTo(Battlefield.outerGroundMargin),
                "the camera limit stays inside the built outer ground");
            Assert.That(View.CameraGroundMargin, Is.GreaterThan(Battlefield.outerTreeEdgeGap), "the view reaches the forest");
        }
    }
}
