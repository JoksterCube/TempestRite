using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using ServerSync;

namespace JoksterCube.TempestRite.Patches;

// Synced settings come only from the server's config file, unless the server's Admin Locked setting is on: then
// ServerSync works as it normally does, letting admins change them in game (sent to the server, saved to its config
// file and passed on to every player). These patch the mod's own bundled copy of ServerSync, so other mods are not affected.
internal static class EnforceServerConfigPatch
{
    // The server's value once connected (it is synced), this computer's own before.
    private static bool AdminsMayChange => PluginConfig.AdminLocked?.Value.IsOn() == true;

    // Client connected to a modded server: synced settings are locked, admins included, unless Admin Locked is on.
    // ServerSync then marks them read-only in Configuration Manager and never sends local edits to the server.
    [HarmonyPatch(typeof(ConfigSync), nameof(ConfigSync.IsLocked), MethodType.Getter)]
    private static class LockForAdmins
    {
        private static void Postfix(ConfigSync __instance, ref bool __result)
        {
            if (__instance == Plugin.ServerConfigSync && !__instance.IsSourceOfTruth && !AdminsMayChange) __result = true;
        }
    }

    // Server: ignore setting changes sent by any client, admins included, unless Admin Locked is on (ServerSync then
    // still ignores those sent by players who aren't admins).
    [HarmonyPatch(typeof(ConfigSync), "RPC_FromOtherClientConfigSync")]
    private static class IgnoreClientChanges
    {
        private static bool Prefix(ConfigSync __instance, long sender)
        {
            if (__instance != Plugin.ServerConfigSync || !ZNet.instance || !ZNet.instance.IsServer() || AdminsMayChange) return true;
            Plugin.ModLogger.LogWarning($"Ignored a settings change sent by a player ({sender}). Change {Settings.Constants.Plugin.ConfigFileName} on the server instead, or turn on Admin Locked.");
            return false;
        }
    }
}
