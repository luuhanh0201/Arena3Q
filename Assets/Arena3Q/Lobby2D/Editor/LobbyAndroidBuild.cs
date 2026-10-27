using System;
using System.IO;
using System.Linq;
using Arena3Q.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Arena3Q.Lobby2D.Editor
{
    public static class LobbyAndroidBuild
    {
        public const string Root="Assets/Arena3Q/Lobby2D";
        public const string ScenePath=Root+"/Unity/LobbyAndroidTest.unity";
        public const string Output="Builds/Android/Arena3Q-Lobby2D-20261027.apk";
        public const string Evidence="NghiemThu/UI/Lobby2D_Android_20261027";
        [Serializable] private sealed class Result {public string result,output,startupScene,gameScene,battleScene;public string[] scenes;public long apkBytes;public double seconds;public int errors,warnings;public bool lobbyIncluded,campaignIncluded,atlasIncluded,deviceMeasured;}

        [Obsolete("Legacy art review only. Build the integrated lobby with BattlefieldAndroidBuild.BuildMobile or WebGameBuild.Build.")]
        public static void Build()
        {
            throw new InvalidOperationException("Legacy LobbyAndroidTest APK builder is retired. Use Arena3Q.Editor.CurrentGameRelease.Apply, then Arena3Q.Editor.BattlefieldAndroidBuild.BuildMobile or Arena3Q.Editor.WebGameBuild.Build for the integrated Frontend lobby.");
        }
    }
}
