using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>
    /// Command-line builds. Example:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod Arena3Q.Editor.BuildScripts.BuildAndroidDev -logFile -
    /// </summary>
    public static class BuildScripts
    {
        [MenuItem("Arena3Q/Build/Android Dev (APK)")]
        public static void BuildAndroidDev() => BattlefieldAndroidBuild.BuildMobile();

        [MenuItem("Arena3Q/Build/Windows Dev")]
        public static void BuildWindowsDev() =>
            Build(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, "Builds/Windows/Arena3Q.exe", BuildOptions.Development);

        private static void Build(BuildTarget target, BuildTargetGroup group, string outputPath, BuildOptions options)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes in Build Settings. Run Arena3Q > Build Prototype Content.");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                targetGroup = group,
                options = options
            });

            var summary = report.summary;
            Debug.Log($"[Arena3Q] Build {summary.result}: {summary.outputPath} ({summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors)");
            if (Application.isBatchMode && summary.result != BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
