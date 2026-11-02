using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;

namespace JoksterCube.TempestRite.Patches;

// The Draught-Flush Rite (see DraughtTimes).
internal static class DraughtFlushPatches
{
    private const string Feature = "Draught-Flush Rite";

    // The blessed local player drinking a mead: its effect, added while drinking, gets the blessed time.
    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeItem))]
    private static class DrinkPatch
    {
        private static void Prefix(Player __instance, ItemDrop.ItemData item)
        {
            DraughtTimes.Drinking = 0;
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (DraughtTimes.Applies(__instance, item)) DraughtTimes.Drinking = item.m_shared.m_consumeStatusEffect.NameHash();
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static Exception? Finalizer(Exception? __exception)
        {
            DraughtTimes.Drinking = 0;
            return __exception;
        }
    }

    // The mead's own copy of its effect, as it starts: before SE_Stats works out its restoring from the time (SE_Stats
    // calls this first).
    [HarmonyPatch(typeof(StatusEffect), nameof(StatusEffect.Setup))]
    private static class SetupPatch
    {
        private static void Prefix(StatusEffect __instance, Character character)
        {
            if (DraughtTimes.Drinking == 0 || PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (character != Player.m_localPlayer || __instance.NameHash() != DraughtTimes.Drinking) return;
                DraughtTimes.Drinking = 0;
                DraughtTimes.Adjust(__instance);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }
}
