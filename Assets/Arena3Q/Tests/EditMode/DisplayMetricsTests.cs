using Arena3Q.Data;
using NUnit.Framework;
using UnityEngine;

namespace Arena3Q.Tests
{
    public sealed class DisplayMetricsTests
    {
        [TearDown] public void Reset() => DisplayMetrics.Reset();
        [Test] public void DeviceInsetsAndDpiFollowActualBufferWhenPreviewIsResized()
        {
            var settings = new DevicePreviewSettings {width=2340,height=1080,dpi=395,frameRate=30,left=80f/2340,right=48f/2340};
            Assert.True(DisplayMetrics.TrySetPreview(settings));
            Assert.AreEqual(80f/2340,DisplayMetrics.SafeArea.xMin/Screen.width,.0001f);
            Assert.AreEqual(1-48f/2340,DisplayMetrics.SafeArea.xMax/Screen.width,.0001f);
            Assert.AreEqual(395f/2340,DisplayMetrics.Dpi/Screen.width,.0001f);
            settings.left=.5f; // Caller cannot mutate the active preview after validation.
            Assert.AreEqual(80f/2340,DisplayMetrics.SafeArea.xMin/Screen.width,.0001f);
            Assert.AreEqual(30,DisplayMetrics.PreviewFrameRate);
            DisplayMetrics.Reset(); Assert.AreEqual(Screen.safeArea,DisplayMetrics.SafeArea);
        }
        [Test] public void MalformedOrInvertedSafeAreaCannotReplaceAValidPreview()
        {
            Assert.True(DisplayMetrics.TrySetPreview(new DevicePreviewSettings{width=1920,height=1080,dpi=320,frameRate=60}));
            Assert.False(DisplayMetrics.TrySetPreview(new DevicePreviewSettings{width=0,height=1080}));
            Assert.False(DisplayMetrics.TrySetPreview(new DevicePreviewSettings{width=1920,height=1080,left=.7f,right=.5f}));
            Assert.False(DisplayMetrics.TrySetPreview(new DevicePreviewSettings{width=1920,height=1080,left=float.NaN}));
            Assert.AreEqual(60,DisplayMetrics.PreviewFrameRate);
        }
    }
}
