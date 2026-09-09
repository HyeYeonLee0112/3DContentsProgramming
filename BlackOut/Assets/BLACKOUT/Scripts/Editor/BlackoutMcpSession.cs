// Editor-only connection helper for the requested BLACKOUT scene-authoring session.
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Blackout.EditorTools
{
    [InitializeOnLoad]
    internal static class BlackoutMcpSession
    {
        static BlackoutMcpSession() { EditorApplication.delayCall += Connect; }

        [MenuItem("BLACKOUT/Intro/Connect Unity MCP")]
        private static async void Connect()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode) return;
            if (!Application.dataPath.Replace('\\', '/').EndsWith("3DContentsProgramming/BlackOut/Assets", StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var locator = assemblies.Select(a => a.GetType("MCPForUnity.Editor.Services.MCPServiceLocator")).FirstOrDefault(t => t != null);
                var modeType = assemblies.Select(a => a.GetType("MCPForUnity.Editor.Services.Transport.TransportMode")).FirstOrDefault(t => t != null);
                if (locator == null || modeType == null) return;
                EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
                EditorPrefs.SetString("MCPForUnity.HttpTransportScope", "local");
                EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8081");
                var manager = locator.GetProperty("TransportManager", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                var mode = Enum.Parse(modeType, "Http");
                if ((bool)manager.GetType().GetMethod("IsRunning").Invoke(manager, new[] { mode })) return;
                var task = (Task<bool>)manager.GetType().GetMethod("StartAsync").Invoke(manager, new[] { mode });
                Debug.Log(await task ? "[BLACKOUT Intro] Unity MCP connected." : "[BLACKOUT Intro] Start local MCP server, then use BLACKOUT/Intro/Connect Unity MCP.");
            }
            catch (Exception ex) { Debug.LogWarning("[BLACKOUT Intro] MCP connection: " + ex.GetBaseException().Message); }
        }
    }
}
