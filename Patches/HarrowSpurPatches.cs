using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Patches;

// The Harrow-Spur Rite, on a spawner it reaches (see SpawnerSpur). The game runs both only in the game that owns the
// spawner.
[HarmonyPatch(typeof(SpawnArea), nameof(SpawnArea.UpdateSpawn))]
internal static class HarrowSpurTimerPatch
{
    private const string Feature = "Harrow-Spur spawn speed";

    // Before the game adds its own 2 seconds: the extra time, under the same conditions the game counts it.
    private static void Prefix(SpawnArea __instance)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            if (!__instance.m_nview || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner()) return;
            if (!SpawnerSpur.Reaches(__instance)) return;
            if (ZNetScene.instance.OutsideActiveArea(__instance.transform.position)
                || !Player.IsPlayerInRange(__instance.transform.position, __instance.m_triggerDistance)) return;
            __instance.m_spawnTimer += SpawnerSpur.ExtraTime;
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}

// Looking at a spawner the rite reaches (they show their name through the game's HoverText): it is under the rite, how
// often it spawns now and how many creatures it keeps close by.
[HarmonyPatch(typeof(HoverText), nameof(HoverText.GetHoverText))]
internal static class HarrowSpurHoverPatch
{
    private const string Feature = "Harrow-Spur spawner hint";

    [HarmonyPriority(Priority.Last)]
    private static void Postfix(HoverText __instance, ref string __result)
    {
        if (PatchGuard.IsDisabled(Feature) || !WeatherState.IsActive(WeatherKind.HarrowSpur)) return;
        try
        {
            SpawnArea area = __instance.GetComponentInChildren<SpawnArea>();
            if (!area || !SpawnerSpur.Reaches(area)) return;
            (float seconds, int near) = SpawnerSpur.Pace(area);
            if (string.IsNullOrEmpty(__result)) __result = Localization.instance.Localize(__instance.m_text);
            __result += "\n" + WeatherKinds.Colored(WeatherKind.HarrowSpur) + ObliteratorHints.Explain(WeatherKind.HarrowSpur)
                + string.Format(Constants.Messages.HarrowSpurSpawns, Mathf.RoundToInt(seconds), near);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}

[HarmonyPatch(typeof(SpawnArea), nameof(SpawnArea.SpawnOne))]
internal static class HarrowSpurCapPatch
{
    private const string Feature = "Harrow-Spur creature cap";

    // __state: the spawner's own limits (close by, in all) while they are raised, or null.
    private static void Prefix(SpawnArea __instance, out (int Near, int Total)? __state)
    {
        __state = null;
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            if (!SpawnerSpur.Reaches(__instance)) return;
            __state = (__instance.m_maxNear, __instance.m_maxTotal);
            __instance.m_maxNear = SpawnerSpur.Cap(__instance.m_maxNear);
            __instance.m_maxTotal = SpawnerSpur.Cap(__instance.m_maxTotal);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Puts the limits back, also when the game's spawn failed.
    private static Exception? Finalizer(SpawnArea __instance, Exception? __exception, (int Near, int Total)? __state)
    {
        if (__state is { } limits)
        {
            __instance.m_maxNear = limits.Near;
            __instance.m_maxTotal = limits.Total;
        }
        return __exception;
    }
}
