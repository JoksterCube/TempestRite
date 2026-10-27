using HarmonyLib;
using ServerSync;

namespace JoksterCube.TempestRite.Patches;

// Synced settings come only from the server's config file. ServerSync normally lets admins change
// them in game; for this mod nobody can. These patch the mod's own bundled copy of ServerSync, so
// other mods are not affected.
internal static class EnforceServerConfigPatch
{
    // Client connected to a modded server: synced settings are locked, admins included. ServerSync then
    // marks them read-only in Configuration Manager and never sends local edits to the server.
    [HarmonyPatch(typeof(ConfigSync), nameof(ConfigSync.IsLocked), MethodType.Getter)]
    private static class LockForAdmins
    {
        private static void Postfix(ConfigSync __instance, ref bool __result)
        {
            if (__instance == Plugin.ServerConfigSync && !__instance.IsSourceOfTruth) __result = true;
        }
    }

    // Server: ignore setting changes sent by any client, admins included.
    [HarmonyPatch(typeof(ConfigSync), "RPC_FromOtherClientConfigSync")]
    private static class IgnoreClientChanges
    {
        private static bool Prefix(ConfigSync __instance, long sender)
        {
            if (__instance != Plugin.ServerConfigSync || !ZNet.instance || !ZNet.instance.IsServer()) return true;
            Plugin.ModLogger.LogWarning($"Ignored a settings change sent by a player ({sender}). Change {Settings.Constants.Plugin.ConfigFileName} on the server instead.");
            return false;
        }
    }
}
