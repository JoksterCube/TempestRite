using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Patches;

// The Morgen-Pouch Rite (see PetPouch): its slot beside the Guardian Power, petting (which picks what goes in), the
// hover text of what would, when creatures start following, and summons over their limit.
internal static class PetPouchPatches
{
    private const string Feature = "Morgen-Pouch Rite";

    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateGuardianPower))]
    private static class Slot
    {
        private static void Postfix(Hud __instance) => PetPouch.UpdateHud(__instance);
    }

    [HarmonyPatch(typeof(Tameable), "RPC_Command")]
    private static class Follow
    {
        private static void Postfix(Tameable __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try { PetPouch.Commanded(__instance); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    [HarmonyPatch(typeof(Tameable), nameof(Tameable.Interact))]
    private static class Pet
    {
        // When it was last petted: the game lets it be petted once a second at most.
        private static void Prefix(Tameable __instance, out float __state) => __state = __instance.m_lastPetTime;

        private static void Postfix(Tameable __instance, Humanoid user, float __state)
        {
            if (PatchGuard.IsDisabled(Feature) || __instance.m_lastPetTime == __state) return;
            try { PetPouch.Petted(__instance, user); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    [HarmonyPatch(typeof(Tameable), nameof(Tameable.GetHoverText))]
    private static class Hover
    {
        private static void Postfix(Tameable __instance, ref string __result)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                if (__instance.m_character && PetPouch.Picked(__instance.m_character))
                    __result += "\n" + WeatherKinds.Colored(WeatherKind.MorgenPouch) + ": " + Messages.MorgenPouchHover;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    [HarmonyPatch(typeof(Tameable), "UnsummonMaxInstances")]
    private static class SummonLimit
    {
        private static bool Prefix(Tameable __instance, int maxInstances) => !PetPouch.Unsummons(__instance, maxInstances);
    }
}
