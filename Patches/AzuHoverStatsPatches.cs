using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Patches;

// AzuHoverStats (when installed) shows how long plants, picked bushes, fermenters, cooking food and sap extractors take,
// at the usual speed, and when food burns. Under the Verdant, Vat-Boil and Taproot-Surge Rites, its times count the rite instead (at the rite's
// speed while it lasts, the usual speed after), and food under the Vat-Boil Rite won't burn.
internal static class AzuHoverStatsPatches
{
    private const string Feature = "AzuHoverStats rite times";
    private const string Utilities = "AzuHoverStats.Utilities";

    internal static bool Installed => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Azumatt.AzuHoverStats");

    [HarmonyPatch]
    private static class PlantTime
    {
        private static bool Prepare() => Installed && AccessTools.Method(Utilities + ":GetPlantString") != null;
        private static System.Reflection.MethodBase TargetMethod() => AccessTools.Method(Utilities + ":GetPlantString");

        private static void Postfix(Plant plantComp, ref string __result)
        {
            if (PatchGuard.IsDisabled(Feature) || string.IsNullOrEmpty(__result)) return;
            try
            {
                if (plantComp && VerdantGrowth.Speeds(plantComp)) Replace(VerdantGrowth.ReadyIn(plantComp), ref __result);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    [HarmonyPatch]
    private static class BushTime
    {
        private static bool Prepare() => Installed && AccessTools.Method(Utilities + ":GetPickableString") != null;
        private static System.Reflection.MethodBase TargetMethod() => AccessTools.Method(Utilities + ":GetPickableString");

        private static void Postfix(Pickable pickableComp, ref string __result)
        {
            if (PatchGuard.IsDisabled(Feature) || string.IsNullOrEmpty(__result)) return;
            try
            {
                if (pickableComp) Replace(VerdantGrowth.RegrowsIn(pickableComp), ref __result);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    [HarmonyPatch]
    private static class FermenterTime
    {
        private static bool Prepare() => Installed && AccessTools.Method(Utilities + ":GetFermenterString") != null;
        private static System.Reflection.MethodBase TargetMethod() => AccessTools.Method(Utilities + ":GetFermenterString");

        private static void Postfix(Fermenter fermenter, ref string __result)
        {
            if (PatchGuard.IsDisabled(Feature) || string.IsNullOrEmpty(__result)) return;
            try
            {
                if (fermenter && RiteSpeedup.Speeds(WeatherKind.VatBoil, fermenter))
                    Replace(StationWork.FermentedIn(fermenter), ref __result);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Under the Vat-Boil Rite each slot says when it is done at the rite's speed, and that it won't burn.
    [HarmonyPatch]
    private static class CookingTimes
    {
        private static bool Prepare() => Installed && AccessTools.Method(Utilities + ":GetCookingStationString") != null;
        private static System.Reflection.MethodBase TargetMethod() => AccessTools.Method(Utilities + ":GetCookingStationString");

        private static void Postfix(CookingStation cookingStationComp, ref string __result)
        {
            if (PatchGuard.IsDisabled(Feature) || string.IsNullOrEmpty(__result)) return;
            try
            {
                CookingStation station = cookingStationComp;
                if (!station || !RiteSpeedup.Speeds(WeatherKind.VatBoil, station)) return;
                System.Text.StringBuilder text = new();
                for (int i = 0; i < station.m_slots.Length; i++)
                {
                    station.GetSlot(i, out string item, out float cooked, out _, out _);
                    if (string.IsNullOrEmpty(item) || station.GetItemConversion(item) is not { } conversion || !conversion.m_to) continue;
                    text.Append(Localization.instance.Localize(conversion.m_to.GetHoverName()));
                    if (cooked < conversion.m_cookTime)
                        text.Append(" (" + Format(RiteSpeedup.ReadyIn(WeatherKind.VatBoil, PluginConfig.VatBoilSpeedMultiplier.Value,
                            conversion.m_cookTime - cooked)) + ")");
                    text.Append("  " + Constants.Messages.WontBurn + "\n");
                }
                __result = text.ToString();
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Under the Taproot-Surge Rite the next sap and the time until full count the rite; the time elapsed stays.
    [HarmonyPatch]
    private static class SapTimes
    {
        private static bool Prepare() => Installed && AccessTools.Method(Utilities + ":GetSapCollectorString") != null;
        private static System.Reflection.MethodBase TargetMethod() => AccessTools.Method(Utilities + ":GetSapCollectorString");

        private static void Postfix(SapCollector sapCollector, ref string __result)
        {
            if (PatchGuard.IsDisabled(Feature) || string.IsNullOrEmpty(__result)) return;
            try
            {
                SapCollector collector = sapCollector;
                if (!collector || !collector.m_nview || !collector.m_nview.IsValid() || !RiteSpeedup.Speeds(WeatherKind.TaprootSurge, collector)
                    || collector.GetLevel() >= collector.m_maxLevel || !collector.m_root || !collector.m_root.CanDrain(1f)) return;
                ZDO zdo = collector.m_nview.GetZDO();
                DateTime last = new(zdo.GetLong(ZDOVars.s_lastTime, ZNet.instance.GetTime().Ticks));
                double stored = zdo.GetFloat(ZDOVars.s_product) + Math.Max(0.0, (ZNet.instance.GetTime() - last).TotalSeconds);
                // Same as AzuHoverStats: past one unit it says the sap is ready instead.
                if (stored >= collector.m_secPerUnit) return;
                float speed = PluginConfig.TaprootSpeedMultiplier.Value;
                double next = RiteSpeedup.ReadyIn(WeatherKind.TaprootSurge, speed, collector.m_secPerUnit - stored);
                double full = RiteSpeedup.ReadyIn(WeatherKind.TaprootSurge, speed,
                    collector.m_maxLevel * collector.m_secPerUnit - (Math.Round(stored) + collector.GetLevel() * collector.m_secPerUnit));
                __result = Localization.instance.Localize("$azuhoverstats_approximatenext $item_sap: " + SapFormat(next) + Environment.NewLine +
                    "$azuhoverstats_timeelapsed: " + SapFormat(stored) + Environment.NewLine + "$azuhoverstats_timeuntilfinished: " + SapFormat(full));
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        // In AzuHoverStats' sap format: zero parts left out ("07m 48s", "48s", "01h 05s").
        private static string SapFormat(double seconds)
        {
            TimeSpan left = TimeSpan.FromSeconds(Math.Round(Math.Max(0.0, seconds)));
            System.Collections.Generic.List<string> parts = [];
            if ((int)left.TotalHours > 0) parts.Add($"{(int)left.TotalHours:D2}h");
            if (left.Minutes > 0) parts.Add($"{left.Minutes:D2}m");
            if (left.Seconds > 0) parts.Add($"{left.Seconds:D2}s");
            return string.Join(" ", parts);
        }
    }

    private static void Replace(double seconds, ref string result)
    {
        if (seconds >= 0.0) result = "(" + Format(seconds) + ")";
    }

    // In AzuHoverStats' own format: "01h 02m 03s", "02m 03s" or "03s".
    private static string Format(double seconds)
    {
        TimeSpan left = TimeSpan.FromSeconds(Math.Ceiling(Math.Max(0.0, seconds)));
        return left.TotalHours >= 1.0 ? $"{(int)left.TotalHours:D2}h {left.Minutes:D2}m {left.Seconds:D2}s"
            : left.Minutes > 0 ? $"{left.Minutes:D2}m {left.Seconds:D2}s"
            : $"{left.Seconds:D2}s";
    }
}
