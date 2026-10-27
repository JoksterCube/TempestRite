using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Verdant Rite: plants (crops, saplings) and bushes that regrow (berries...) near the obliterator grow faster.
//
// The game works out how far along they are from when they were planted or picked (saved on them, in world time). So
// every few seconds, for those this game owns within the radius, that time is moved back by the growth bonus for the
// time that passed: 50% faster moves it back by half the time that passed. What they grew stays after the rite ends,
// and whoever owns them next sees the same.
internal static class VerdantGrowth
{
    private const string Feature = "Verdant Rite";
    private const float Interval = 5f;
    // At most this much world time is counted at once (sleeping skips hours).
    private const double MaxStepSeconds = 3600.0;

    private static HashSet<int>? _plants;
    private static HashSet<int>? _bushes;
    private static float _nextUpdate;
    private static double _lastWorldTime = -1.0;

    internal static void ClearSession()
    {
        _nextUpdate = 0f;
        _lastWorldTime = -1.0;
    }

    // Plants and bushes: for handing them to players with the mod (see WeatherOwnership).
    internal static bool IsGrowing(int prefab) => Prefabs() && (_plants!.Contains(prefab) || _bushes!.Contains(prefab));

    // Every frame.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature) || Time.time < _nextUpdate) return;
        _nextUpdate = Time.time + Interval;
        if (!WeatherState.IsActive(WeatherKind.Verdant) || !ZNet.instance || !ZNetScene.instance)
        {
            _lastWorldTime = -1.0;
            return;
        }
        double now = ZNet.instance.GetTimeSeconds();
        double step = _lastWorldTime < 0.0 ? 0.0 : Math.Min(now - _lastWorldTime, MaxStepSeconds);
        _lastWorldTime = now;
        if (step <= 0.0 || !Prefabs()) return;

        try { Grow(TimeSpan.FromSeconds(step * PluginConfig.VerdantGrowthPercent.Value / 100.0).Ticks); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void Grow(long bonusTicks)
    {
        long me = ZDOMan.GetSessionID();
        foreach (KeyValuePair<ZDO, ZNetView> instance in ZNetScene.instance.m_instances)
        {
            ZDO zdo = instance.Key;
            if (zdo.GetOwner() != me) continue;
            int prefab = zdo.GetPrefab();
            bool plant = _plants!.Contains(prefab);
            if (!plant && !_bushes!.Contains(prefab)) continue;
            if (!WeatherState.Reaches(WeatherKind.Verdant, zdo.GetPosition())) continue;

            if (plant)
            {
                long planted = zdo.GetLong(ZDOVars.s_plantTime, 0L);
                if (planted > 0L) zdo.Set(ZDOVars.s_plantTime, Math.Max(1L, planted - bonusTicks));
            }
            else if (zdo.GetBool(ZDOVars.s_picked))
            {
                long picked = zdo.GetLong(ZDOVars.s_pickedTime, 0L);
                if (picked > 1L) zdo.Set(ZDOVars.s_pickedTime, Math.Max(2L, picked - bonusTicks));
            }
        }
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
