using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;

namespace JoksterCube.TempestRite.Patches;

internal static class CompendiumPatches
{
    private const string Feature = "Compendium page";

    // Adds the Tempest Rite page right after Active effects.
    [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.UpdateTextsList))]
    private static class AddPage
    {
        private static void Postfix(TextsDialog __instance)
        {
            if (PatchGuard.IsDisabled(Feature) || !Player.m_localPlayer) return;
            try
            {
                int index = Math.Min(1, __instance.m_texts.Count);
                __instance.m_texts.Insert(index, new TextsDialog.TextInfo(RiteCompendium.Topic, RiteCompendium.BuildText()));
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Each rite's icon next to its name.
    [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.Update))]
    private static class RiteIcons
    {
        private static void Postfix(TextsDialog __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try { RiteCompendium.UpdateIcons(__instance); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }
}
