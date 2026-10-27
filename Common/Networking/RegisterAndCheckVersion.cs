using System;
using HarmonyLib;
using static JoksterCube.TempestRite.Settings.Constants.Plugin;

namespace JoksterCube.TempestRite.Common.Networking;

[HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
internal static class RegisterAndCheckVersion
{
    private static void Prefix(ZNetPeer peer, ZNet __instance)
    {
        if (!__instance.IsServer())
            RpcHandlers.BeginServerVersionCheck();

        Plugin.ModLogger.LogDebug("Registering version RPC handler");
        peer.m_rpc.Register(VersionCheckRpc, new Action<ZRpc, ZPackage>(RpcHandlers.RPC_VersionCheck));

        Plugin.ModLogger.LogInfo("Invoking version check");
        ZPackage zpackage = new();
        zpackage.Write(ModVersion);
        peer.m_rpc.Invoke(VersionCheckRpc, zpackage);
    }
}
