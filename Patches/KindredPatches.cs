using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using JoksterCube.TempestRite.Settings;

namespace JoksterCube.TempestRite.Patches;

// The Kindred Rite, on a tamed animal it reaches: its crowd limit is raised while the game checks whether it can breed,
// and the check runs Breeding Speed Multiplier times as often (see KindredBreeding). The game checks only in the game
// that owns the animal.
[HarmonyPatch(typeof(Procreation), nameof(Procreation.Procreate))]
internal static class KindredProcreatePatch
{
    private const string Feature = "Kindred breeding";
    // Set while the extra checks run, so they don't add more of their own.
    private static bool _again;

    // __state: the animal's own crowd limit while it is raised, or -1.
    private static void Prefix(Procreation __instance, out int __state)
    {
        __state = -1;
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            if (!KindredBreeding.Reaches(__instance)) return;
            __state = __instance.m_maxCreatures;
            __instance.m_maxCreatures = KindredBreeding.Crowd(__state);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Puts the crowd limit back even when the game's check failed, then checks again.
    private static Exception? Finalizer(Procreation __instance, Exception? __exception, int __state)
    {
        if (__state < 0) return __exception;
        __instance.m_maxCreatures = __state;
        if (__exception != null || _again) return __exception;
        try
        {
            _again = true;
            for (int n = KindredBreeding.ExtraChecks(); n > 0; n--) __instance.Procreate();
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        finally { _again = false; }
        return null;
    }
}

// The Kindred Rite: a tamed young or pregnant animal it reaches says so when looked at, and about when it will be grown
// or give birth. A young one without a tame status of its own (a chick) showed nothing; now its name too.
[HarmonyPatch(typeof(Character), nameof(Character.GetHoverText))]
internal static class KindredHoverPatch
{
    private const string Feature = "Kindred animal hint";

    [HarmonyPriority(Priority.Last)]
    private static void Postfix(Character __instance, ref string __result)
    {
        if (PatchGuard.IsDisabled(Feature) || __instance is Player) return;
        try
        {
            if (!KindredBreeding.Reaches(__instance)) return;
            string? when;
            if (__instance.GetComponent<Growup>() is { } growup && growup)
                when = Time(KindredBreeding.GrownIn(growup), Constants.Messages.KindredGrownSoon, Constants.Messages.KindredGrownIn);
            else if (__instance.GetComponent<Procreation>() is { } procreation && procreation && KindredBreeding.DueIn(procreation) is var due && due >= 0.0)
                when = Time(due, Constants.Messages.KindredDueSoon, Constants.Messages.KindredDueIn);
            else return;

            if (string.IsNullOrEmpty(__result)) __result = Localization.instance.Localize(__instance.GetHoverName());
            __result += "\n" + WeatherKinds.Colored(WeatherKind.Kindred) + ObliteratorHints.Explain(WeatherKind.Kindred) + when;
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    internal static string Time(double seconds, string soon, string inMinutes) =>
        seconds < 0.0 ? string.Empty : seconds < 60.0 ? soon : string.Format(inMinutes, Math.Ceiling(seconds / 60.0));
}

// The Kindred Rite: a warm egg it reaches says so when looked at, and about when it will hatch.
[HarmonyPatch(typeof(EggGrow), nameof(EggGrow.GetHoverText))]
internal static class KindredEggHoverPatch
{
    private const string Feature = "Kindred egg hint";

    [HarmonyPriority(Priority.Last)]
    private static void Postfix(EggGrow __instance, ref string __result)
    {
        if (PatchGuard.IsDisabled(Feature) || string.IsNullOrEmpty(__result)) return;
        try
        {
            if (!RiteSpeedup.Speeds(WeatherKind.Kindred, __instance)) return;
            double hatches = KindredBreeding.HatchesIn(__instance);
            if (hatches < 0.0) return;
            __result += "\n" + WeatherKinds.Colored(WeatherKind.Kindred) + ObliteratorHints.Explain(WeatherKind.Kindred)
                + KindredHoverPatch.Time(hatches, Constants.Messages.KindredHatchesSoon, Constants.Messages.KindredHatchesIn);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}
