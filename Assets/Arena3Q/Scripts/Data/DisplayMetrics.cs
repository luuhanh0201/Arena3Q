using System;
using UnityEngine;

namespace Arena3Q.Data
{
    [Serializable]
    public sealed class DevicePreviewSettings
    {
        public int width, height, dpi, frameRate;
        public float left, right, top, bottom;
    }

    /// <summary>Shared screen metrics for UI and input; preview values are normalized to the render buffer.</summary>
    public static class DisplayMetrics
    {
        private static DevicePreviewSettings _preview;
        public static Rect SafeArea => _preview == null ? Screen.safeArea : new Rect(
            _preview.left * Screen.width, _preview.bottom * Screen.height,
            (1-_preview.left-_preview.right)*Screen.width, (1-_preview.top-_preview.bottom)*Screen.height);
        public static float Dpi => _preview != null && _preview.dpi > 0
            ? _preview.dpi * (float)Screen.width / _preview.width : Screen.dpi > 0 ? Screen.dpi : 160;
        public static int PreviewFrameRate => _preview?.frameRate ?? 0;

        public static bool TrySetPreview(DevicePreviewSettings value)
        {
            if (value == null) { Reset(); return true; }
            if (value.width <= 0 || value.height <= 0 || value.dpi < 0 || value.frameRate < 0
                || !Inset(value.left) || !Inset(value.right) || !Inset(value.top) || !Inset(value.bottom)
                || value.left + value.right >= 1 || value.top + value.bottom >= 1) return false;
            _preview = new DevicePreviewSettings { width=value.width,height=value.height,dpi=value.dpi,frameRate=value.frameRate,
                left=value.left,right=value.right,top=value.top,bottom=value.bottom };
            return true;
        }
        private static bool Inset(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value < 1;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() => _preview = null;
    }
}
