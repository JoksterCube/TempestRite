using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;

namespace JoksterCube.TempestRite.Patches;

// The Vat-Boil Rite: nothing cooked on a cooking station it reaches burns (the cooking station, the iron one, the oven).
// The game that owns the station cooks: each second, food that is done is kept at just done, so it never gets to burning;
// and should the game burn it anyway (a long time passed at once), it stays done instead. After the rite ends, food left
// on the station burns as usual.
internal static class VatBoilPatches
{
    private const string Feature = "Vat-Boil Rite (no burning)";

    private static bool Guards(CookingStation station) =>
        !PatchGuard.IsDisabled(Feature) && station && station.m_nview && station.m_nview.IsValid()
        && RiteSpeedup.Speeds(WeatherKind.VatBoil, station);

    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.UpdateCooking))]
    private static class KeepDone
    {
        private static void Prefix(CookingStation __instance)
        {
            try
            {
                if (!Guards(__instance) || !__instance.m_nview.IsOwner()) return;
                for (int i = 0; i < __instance.m_slots.Length; i++)
                {
                    __instance.GetSlot(i, out string item, out float cooked, out CookingStation.Status status, out bool cheated);
                    if (status != CookingStation.Status.Done || __instance.GetItemConversion(item) is not { } conversion) continue;
                    if (cooked > conversion.m_cookTime) __instance.SetSlot(i, item, conversion.m_cookTime, status, cheated);
                }
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.SetSlot))]
    private static class NoBurnt
    {
        private static void Prefix(CookingStation __instance, int slot, ref string itemName, ref float cookedTime, ref CookingStation.Status status)
        {
            if (status != CookingStation.Status.Burnt) return;
            try
            {
                if (!Guards(__instance)) return;
                __instance.GetSlot(slot, out string before, out _, out _, out _);
                if (__instance.GetItemConversion(before) is not { } conversion || !conversion.m_to) return;
                itemName = conversion.m_to.name;
                cookedTime = conversion.m_cookTime;
                status = CookingStation.Status.Done;
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }
}
