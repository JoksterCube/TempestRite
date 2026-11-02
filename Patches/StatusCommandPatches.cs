using HarmonyLib;
using JoksterCube.TempestRite.Domain;

namespace JoksterCube.TempestRite.Patches;

// The game's addstatus and clearstatus console commands on the mod's statuses (see StatusCommands).
internal static class StatusCommandPatches
{
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    private static class Listed
    {
        private static void Postfix(ObjectDB __instance) => StatusCommands.Register(__instance);
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
    private static class ListedCopy
    {
        private static void Postfix(ObjectDB __instance) => StatusCommands.Register(__instance);
    }

    // By name: addstatus, and the same asked of the player's game by another.
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.Internal_AddStatusEffect))]
    private static class Added
    {
        private static bool Prefix(SEMan __instance, int nameHash) => StatusCommands.Adding(__instance, nameHash);
    }

    [HarmonyPatch(typeof(SEMan), nameof(SEMan.RemoveStatusEffect), typeof(int), typeof(bool))]
    private static class Removed
    {
        private static void Postfix(SEMan __instance, int nameHash, bool __result)
        {
            if (__result) StatusCommands.Removed(__instance, nameHash);
        }
    }

    [HarmonyPatch(typeof(SEMan), nameof(SEMan.RemoveAllStatusEffects))]
    private static class Cleared
    {
        private static void Prefix(SEMan __instance) => StatusCommands.Clearing(__instance);
    }
}
