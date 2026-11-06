using HarmonyLib;
using JoksterCube.TempestRite.Settings;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Common.Networking;

// Both sides send the version check as soon as they connect, before the game's own peer info, so by the time the peer
// info arrives each side knows whether the other has the mod.
// - Server: a player who didn't answer with the same version is turned away, unless Require Mod On Clients is off.
// - Player: a server that answered with another version is left, with a message on the menu. A server that didn't
//   answer at all doesn't have the mod: the player stays, with only the rites their own game can do.
[HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
internal static class RequireMod
{
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(ZRpc rpc, ZNet __instance)
    {
        if (__instance.IsServer())
        {
            if (RpcHandlers.ValidatedPeers.Contains(rpc) || !PluginConfig.RequireModOnClients.IsOn()) return true;
            Plugin.ModLogger.LogWarning($"Peer ({rpc.GetPeerHostName()}) doesn't have {Constants.Plugin.ModName} {Constants.Plugin.ModVersion}, disconnecting...");
            rpc.Invoke("Error", (int)ZNet.ConnectionStatus.ErrorVersion);
            return false;
        }

        if (RpcHandlers.ServerMatches) return true;
        if (!RpcHandlers.ServerAnswered)
        {
            RpcHandlers.JoinedServerWithoutMod();
            Plugin.ModLogger.LogInfo($"The server doesn't have {Constants.Plugin.ModName}: only blessings, kinship rites and the Unmaking Rite work.");
            return true;
        }
        Plugin.ModLogger.LogWarning($"The server doesn't have {Constants.Plugin.ModName} {Constants.Plugin.ModVersion}, disconnecting...");
        ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorVersion;
        return false;
    }
}
