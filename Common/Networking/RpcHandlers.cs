using System.Collections.Generic;
using static JoksterCube.TempestRite.Settings.Constants.Plugin;

namespace JoksterCube.TempestRite.Common.Networking;

// The mod is needed on both sides, in the same version. A player with the mod doesn't join a server without it (see
// RequireMod); a server turns away players without it, unless its Require Mod On Clients setting is off.
internal static class RpcHandlers
{
    internal static readonly HashSet<ZRpc> ValidatedPeers = [];
    private static bool _serverVersionReceived;
    private static bool _serverVersionMatches;

    // On a client: the server answered the version check with the same version.
    internal static bool ServerMatches => _serverVersionReceived && _serverVersionMatches;

    internal static void BeginServerVersionCheck()
    {
        _serverVersionReceived = false;
        _serverVersionMatches = false;
        Plugin.ConnectionError = string.Empty;
    }

    internal static void ResetServerVersionCheck()
    {
        _serverVersionReceived = false;
        _serverVersionMatches = false;
    }

    internal static void RPC_VersionCheck(ZRpc rpc, ZPackage pkg)
    {
        string version = pkg.ReadString();
        var instance = ZNet.instance;
        if (!instance) return;

        var isServer = instance.IsServer();
        if (!isServer)
        {
            _serverVersionReceived = true;
            _serverVersionMatches = version == ModVersion;
        }

        Plugin.ModLogger.LogInfo($"Version check, local: {ModVersion}, remote: {version}");
        if (version != ModVersion)
        {
            Plugin.ConnectionError = $"{ModName} Installed: {ModVersion}\n Needed: {version}";
            if (!isServer) return;
            Plugin.ModLogger.LogWarning($"Peer ({rpc.GetPeerHostName()}) has incompatible version, disconnecting...");
            rpc.Invoke("Error", 3);
        }
        else if (!isServer)
        {
            Plugin.ModLogger.LogInfo($"Server uses {ModName} {ModVersion}; rites are shared through the server.");
        }
        else
        {
            Plugin.ModLogger.LogInfo($"Adding peer ({rpc.GetPeerHostName()}) to validated list");
            ValidatedPeers.Add(rpc);
        }
    }
}
