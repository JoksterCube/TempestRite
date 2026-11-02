using HarmonyLib;
using JoksterCube.TempestRite.Domain;

namespace JoksterCube.TempestRite.Patches;

// The Plume-Ward and Second-Kindle Rites: what keeps the local player alive (see PlumeFall, KindleSave).
internal static class DomainGuardPatches
{
    // Plume-Ward: a softer landing.
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyFallDamage))]
    private static class FallPatch
    {
        // After the game's own (and other mods') changes, so it is never fatal in the end.
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEMan __instance, ref float damage) => PlumeFall.Soften(__instance.m_character, ref damage);
    }

    // Second-Kindle: a killing blow is dropped and the player saved instead.
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    private static class KillingBlowPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(Character __instance, HitData hit) => hit == null || !KindleSave.Saves(__instance, hit);
    }
}
