using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>Machine-local connection settings for the optional MCP for Unity editor package.</summary>
    public static class UnityMcpSetup
    {
        [MenuItem("Arena3Q/Configure Unity MCP")]
        public static void Configure()
        {
            var uvx = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UnityMCP", "bin", "uvx.exe");
            if (File.Exists(uvx))
                EditorPrefs.SetString("MCPForUnity.UvxPath", uvx);
            EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
            EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8080");
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
            EditorPrefs.SetBool("MCPForUnity.TelemetryDisabled", true);
            Debug.Log("[Arena3Q] Unity MCP configured for localhost:8080. Open Window > MCP for Unity to start the session.");
        }
    }
}
