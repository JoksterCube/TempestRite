using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;

namespace JoksterCube.TempestRite.Patches;

// The Tundra Shroud Rite (see TundraShroud).
internal static class TundraShroudPatches
{
    private const string Feature = "Tundra Shroud Rite";

    // The blessed local player's Wet, Cold and Freezing: worked out as if the weather were neither wet nor cold.
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateEnvStatusEffects))]
    private static class EnvironmentPatch
    {
        private static void Prefix(Player __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (__instance == Player.m_localPlayer && TundraShroud.Active) TundraShroud.Hide();
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static Exception? Finalizer(Exception? __exception)
        {
            TundraShroud.Restore();
            return __exception;
        }
    }

    // Wet (from water) on the blessed local player wears off as by a fire.
    [HarmonyPatch(typeof(SE_Wet), nameof(SE_Wet.UpdateStatusEffect))]
    private static class DryingPatch
    {
        private static void Postfix(SE_Wet __instance, float dt)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                __instance.m_time += TundraShroud.ExtraDrying(__instance.m_character, dt);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }
}
