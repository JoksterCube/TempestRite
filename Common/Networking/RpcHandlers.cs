using System.Collections.Generic;
using static JoksterCube.TempestRite.Settings.Constants.Plugin;

namespace JoksterCube.TempestRite.Common.Networking;

// A server with the mod needs the same version on both sides: it turns away players without it (unless its Require Mod
// On Clients setting is off), and a player doesn't join one with another version (see RequireMod). A player can join a
// server without the mod: only the rites their own game can do work there (see WeatherKinds.WorksWithoutServerMod).
internal static class RpcHandlers
{
    internal static readonly HashSet<ZRpc> ValidatedPeers = [];
    private static bool _serverVersionReceived;
    private static bool _serverVersionMatches;
    private static bool _serverLacksMod;

    // On a client: the server answered the version check with the same version.
    internal static bool ServerMatches => _serverVersionReceived && _serverVersionMatches;

    // On a client: the server never answered the version check, so it doesn't have the mod.
    // Checked against being a client too, so it never lingers into single player or hosting.
    internal static bool ServerLacksMod => _serverLacksMod && ZNet.instance && !ZNet.instance.IsServer();
    // Whether the server answered the version check at all (with any version).
    internal static bool ServerAnswered => _serverVersionReceived;

    internal static void JoinedServerWithoutMod() => _serverLacksMod = true;

    internal static void BeginServerVersionCheck()
    {
        _serverVersionReceived = false;
        _serverVersionMatches = false;
        _serverLacksMod = false;
        Plugin.ConnectionError = string.Empty;
    }

    internal static void ResetServerVersionCheck()
    {
        _serverVersionReceived = false;
        _serverVersionMatches = false;
        _serverLacksMod = false;
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
