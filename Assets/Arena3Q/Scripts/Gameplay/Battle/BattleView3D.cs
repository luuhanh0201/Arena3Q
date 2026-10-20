using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// Scene set-up of the 3D presentation (docs/KeHoach/ChuyenSang3D.md §12): the battle camera on the Universal
    /// renderer, tilted about X and orthographic; one directional light and a flat ambient, no realtime shadows.
    /// </summary>
    public static class BattleView3D
    {
        /// <summary>Puts the camera on the renderer of the chosen presentation (2D renderer = the URP default).</summary>
        public static void ConfigureCamera(Camera camera, BattleController battle)
        {
            camera.orthographic = true;
            var data = camera.GetUniversalAdditionalCameraData();
            if (battle.Presentation3D)
            {
                data.SetRenderer(battle.View.RendererIndex);
                data.renderShadows = false;
                camera.nearClipPlane = 0.3f;
                camera.farClipPlane = battle.View.CameraDistance * 2f + 50f;
            }
            else
            {
                data.SetRenderer(-1);
                camera.transform.rotation = Quaternion.identity;
            }
        }

        /// <summary>Camera transform that looks at a ground point (logic plane) with the battle's tilt.</summary>
        public static void PlaceCamera(Transform camera, BattleController battle, Vector2 ground)
        {
            camera.rotation = TiltedViewMath.CameraRotation(battle.ViewTiltDegrees);
            camera.position = TiltedViewMath.CameraPosition(ground, battle.ViewTiltDegrees, battle.View.CameraDistance);
        }

        public static Light CreateLight(Transform parent, BattleViewConfig view)
        {
            var go = new GameObject("Sun");
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(view.LightEulerAngles);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = view.LightColour;
            light.intensity = view.LightIntensity;
            light.shadows = LightShadows.None;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = view.AmbientColour;
            RenderSettings.sun = light;
            return light;
        }
    }
}
