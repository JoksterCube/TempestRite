using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// Raids live on the server (and in single player or when hosting); players' games only follow them.
// - The Crimson Rite: while it goes on, the server keeps a raid going at the obliterator: when one ends, the next starts
//   after a short pause, picked among the raids this world is ready for (as the game picks them) that fit the biome.
//   No other raid starts anywhere meanwhile (see CrimsonPatch), and one going on elsewhere is replaced.
// - The Sanctuary Rite: ends the raid going on at once, and with it the Crimson Rite.
internal static class CrimsonRaids
{
    private const string Feature = "Crimson Rite raids";
    // Close enough to count as a raid at the obliterator.
    private const float SamePlace = 2f;

    private static float _nextStart;
    private static bool _warned;

    // While the mod starts a raid itself (the only raid allowed to start during the Crimson Rite).
    internal static bool Starting { get; private set; }

    internal static bool Active => WeatherState.IsActive(WeatherKind.Crimson);

    internal static void ClearSession()
    {
        _nextStart = 0f;
        _warned = false;
        Starting = false;
    }

    // On the server, right after a rite was performed.
    internal static void Performed(WeatherKind kind)
    {
        if (!ZNet.instance || !ZNet.instance.IsServer() || !RandEventSystem.instance) return;
        switch (kind)
        {
            case WeatherKind.Sanctuary:
                if (RandEventSystem.instance.m_randomEvent != null)
                {
                    Plugin.ModLogger.LogInfo($"Sanctuary Rite: ended the raid {RandEventSystem.instance.m_randomEvent.m_name}.");
                    RandEventSystem.instance.ResetRandomEvent();
                }
                break;
            case WeatherKind.Crimson:
                // The first raid comes right away.
                _nextStart = 0f;
                break;
        }
    }

    // Every frame on the server.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature) || !Active || !ZNet.instance || !ZNet.instance.IsServer() || !RandEventSystem.instance) return;
        try { Keep(RandEventSystem.instance); }
        catch (System.Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void Keep(RandEventSystem raids)
    {
        Vector3 center = WeatherState.Position(WeatherKind.Crimson);
        RandomEvent? current = raids.m_randomEvent;
        bool oursGoingOn = current != null && Utils.DistanceXZ(current.m_pos, center) < SamePlace;
        if (oursGoingOn)
        {
            _nextStart = Time.time + PluginConfig.CrimsonPause.Value;
            return;
        }
        // One going on elsewhere is replaced at once; after ours ends, the next comes after the pause.
        if (current == null && Time.time < _nextStart) return;
        _nextStart = Time.time + PluginConfig.CrimsonPause.Value;

        RandomEvent? raid = Pick(raids, center);
        if (raid == null)
        {
            if (!_warned) Plugin.ModLogger.LogWarning("Crimson Rite: no raid this world is ready for fits the obliterator's place.");
            _warned = true;
            return;
        }
        Starting = true;
        try { raids.SetRandomEvent(raid, center); }
        finally { Starting = false; }
        Plugin.ModLogger.LogInfo($"Crimson Rite: started the raid {raid.m_name}.");
    }

    // The raids the game could start now (turned on, random, the world's keys allow it), that fit the obliterator's
    // biome; any of the first kind if none fit.
    private static RandomEvent? Pick(RandEventSystem raids, Vector3 center)
    {
        RandEventSystem.RefreshPlayerEventData();
        List<RandomEvent> ready = [];
        List<RandomEvent> fitting = [];
        foreach (RandomEvent raid in raids.m_events)
        {
            if (!raid.m_enabled || !raid.m_random || !raids.HaveGlobalKeys(raid, RandEventSystem.s_playerEventDatas)) continue;
            ready.Add(raid);
            if (raids.InValidBiome(raid, center)) fitting.Add(raid);
        }
        List<RandomEvent> from = fitting.Count > 0 ? fitting : ready;
        return from.Count > 0 ? from[Random.Range(0, from.Count)] : null;
    }
}
