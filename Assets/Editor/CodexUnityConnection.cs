using System;
using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

// Editor-only connection helper; does not modify scenes or enter Play mode.
[InitializeOnLoad]
internal static class CodexUnityConnection
{
    static CodexUnityConnection()
    {
        EditorApplication.delayCall += Connect;
    }

    [MenuItem("Tools/Codex/Connect Unity MCP")]
    private static async void Connect()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        try
        {
            var configuration = EditorConfigurationCache.Instance;
            configuration.SetUseHttpTransport(true);
            configuration.SetHttpTransportScope("local");
            configuration.SetHttpBaseUrl("http://127.0.0.1:8080");
            if (MCPServiceLocator.Bridge.IsRunning)
                return;
            bool connected = await MCPServiceLocator.Bridge.StartAsync();
            Debug.Log(connected ? "[Codex] Unity MCP connected." : "[Codex] Unity MCP connection failed; check Window > MCP for Unity.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
