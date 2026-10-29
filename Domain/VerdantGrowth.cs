using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Verdant Rite: plants (crops, saplings) and bushes that regrow (berries...) near the obliterator grow faster (see
// RiteSpeedup).
//
// The game works out how far along they are from when they were planted or picked (saved on them, in world time): that
// time is moved back.
internal static class VerdantGrowth
{
    // Crops and saplings get smaller puffs of motes than bushes.
    private const float PlantMotes = 0.45f;
    private const float BushMotes = 0.8f;

    private static HashSet<int>? _plants;
    private static HashSet<int>? _bushes;

    internal static readonly SpedWork Work = new GrowthWork();

    // Plants and bushes: for handing them to players with the mod (see WeatherOwnership).
    internal static bool IsGrowing(int prefab) => Prefabs() && (_plants!.Contains(prefab) || _bushes!.Contains(prefab));

    // A healthy plant within the rite's reach: it grows faster (for its hover text).
    internal static bool Speeds(Plant plant) =>
        WeatherState.IsActive(WeatherKind.Verdant) && plant.m_status == Plant.Status.Healthy
        && WeatherState.Reaches(WeatherKind.Verdant, plant.transform.position);

    // About how many seconds until a plant is grown: at the rite's speed while it lasts, the usual speed after. Other
    // mods' timers (AzuHoverStats...) count at the usual speed, so they show more. Negative: not known.
    internal static double ReadyIn(Plant plant) =>
        plant.m_nview && plant.m_nview.IsValid() ? ReadyIn(plant.m_nview.GetZDO().GetLong(ZDOVars.s_plantTime, 0L), plant.GetGrowTime()) : -1.0;

    // A picked bush under the rite: about how many seconds until it has regrown. Negative: not picked, or not under it.
    internal static double RegrowsIn(Pickable bush)
    {
        if (!bush.m_nview || !bush.m_nview.IsValid() || !WeatherState.IsActive(WeatherKind.Verdant)) return -1.0;
        ZDO zdo = bush.m_nview.GetZDO();
        if (!zdo.GetBool(ZDOVars.s_picked) || !IsGrowing(zdo.GetPrefab()) || !WeatherState.Reaches(WeatherKind.Verdant, bush.transform.position))
            return -1.0;
        return ReadyIn(zdo.GetLong(ZDOVars.s_pickedTime, 0L), bush.m_respawnTimeMinutes * 60.0);
    }

    // From the planted or picked time and the usual seconds it takes.
    private static double ReadyIn(long since, double takes)
    {
        if (since <= 1L || !ZNet.instance) return -1.0;
        double left = takes - (ZNet.instance.GetTime() - new DateTime(since)).TotalSeconds;
        return RiteSpeedup.ReadyIn(WeatherKind.Verdant, PluginConfig.VerdantGrowthMultiplier.Value, left);
    }

    private sealed class GrowthWork() : SpedWork(WeatherKind.Verdant)
    {
        internal override float Speed => PluginConfig.VerdantGrowthMultiplier.Value;
        internal override bool ShowMotes => PluginConfig.ShowVerdantSparkles.IsOn();

        internal override float? Takes(GameObject prefab) =>
            prefab.GetComponent<Plant>() ? PlantMotes
            : prefab.GetComponent<Pickable>() is { } pickable && pickable.m_respawnTimeMinutes > 0f ? BushMotes
            : null;

        // A healthy plant with its planted time, or a picked bush with its picked time; nothing to grow without one.
        internal override bool Busy(ZDO zdo, ZNetView view) =>
            IsPlant(zdo)
                ? zdo.GetLong(ZDOVars.s_plantTime, 0L) >= 1L && view.GetComponent<Plant>() is { } plant && plant && plant.m_status == Plant.Status.Healthy
                : zdo.GetBool(ZDOVars.s_picked) && zdo.GetLong(ZDOVars.s_pickedTime, 0L) >= 2L;

        internal override void Hurry(ZDO zdo, ZNetView view, double seconds)
        {
            bool plant = IsPlant(zdo);
            int key = plant ? ZDOVars.s_plantTime : ZDOVars.s_pickedTime;
            long min = plant ? 1L : 2L;
            zdo.Set(key, Math.Max(min, zdo.GetLong(key, 0L) - TimeSpan.FromSeconds(seconds).Ticks));
        }

        private static bool IsPlant(ZDO zdo) => Prefabs() && _plants!.Contains(zdo.GetPrefab());
    }

    // Found once from the game's prefabs.
    private static bool Prefabs()
    {
        if (_plants != null) return true;
        if (!ZNetScene.instance) return false;
        _plants = [];
        _bushes = [];
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
        {
            if (!prefab) continue;
            if (prefab.GetComponent<Plant>()) _plants.Add(prefab.name.GetStableHashCode());
            else if (prefab.GetComponent<Pickable>() is { } pickable && pickable.m_respawnTimeMinutes > 0f) _bushes.Add(prefab.name.GetStableHashCode());
        }
        return true;
    }
}
