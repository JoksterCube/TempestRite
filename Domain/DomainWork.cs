using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// Two Domain rites that work on what is built near the obliterator.
// - Vat-Boil: fermenters need less time. Like the Verdant Rite, every few seconds the time they started is moved back,
//   by the game that owns them, so what they fermented stays after it ends. They bubble now and then while they do.
// - Amber-Mend: damaged building pieces are repaired, a couple each second, by every game near them (a repair is
//   asked of the piece's owner, as the hammer does), each showing the hammer's repair effect on the piece.
internal static class DomainWork
{
    private const string Feature = "Vat-Boil and Amber-Mend Rites";
    private const float FermentInterval = 5f;
    private const double MaxStepSeconds = 3600.0;
    private const float BubbleInterval = 2f;
    private const float BubbleRange = 60f;
    private const float BubbleSeconds = 2.5f;
    private static readonly Color BubbleColor = new(0.8f, 0.95f, 0.3f, 1f);

    private const float MendInterval = 0.5f;
    private const int MendsPerTick = 1;
    // Pieces looked at per tick, so a large base is gone through over a few seconds without a hitch.
    private const int ScanPerTick = 400;
    private const float MendRange = 80f;

    private static HashSet<int>? _fermenters;
    private static float _nextFerment;
    private static float _nextBubble;
    private static double _lastWorldTime = -1.0;
    private static float _nextMend;
    private static int _scanIndex;

    internal static void ClearSession()
    {
        _nextFerment = 0f;
        _nextBubble = 0f;
        _lastWorldTime = -1.0;
        _nextMend = 0f;
        _scanIndex = 0;
    }

    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature) || !ZNet.instance || !ZNetScene.instance) return;
        try
        {
            Ferment();
            Bubble();
            Mend();
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // ---- Vat-Boil ----

    private static void Ferment()
    {
        if (Time.time < _nextFerment) return;
        _nextFerment = Time.time + FermentInterval;
        if (!WeatherState.IsActive(WeatherKind.VatBoil))
        {
            _lastWorldTime = -1.0;
            return;
        }
        double now = ZNet.instance.GetTimeSeconds();
        double step = _lastWorldTime < 0.0 ? 0.0 : Math.Min(now - _lastWorldTime, MaxStepSeconds);
        _lastWorldTime = now;
        if (step <= 0.0 || !Prefabs()) return;

        // Cutting the time by c means it goes 1 / (1 - c) times as fast: the extra is moved back.
        double cut = Mathf.Clamp(PluginConfig.VatBoilTimeCut.Value, 1f, 90f) / 100.0;
        long bonus = TimeSpan.FromSeconds(step * (1.0 / (1.0 - cut) - 1.0)).Ticks;
        long me = ZDOMan.GetSessionID();
        foreach (KeyValuePair<ZDO, ZNetView> instance in ZNetScene.instance.m_instances)
        {
            ZDO zdo = instance.Key;
            if (zdo.GetOwner() != me || !_fermenters!.Contains(zdo.GetPrefab()) || !Fermenting(zdo)) continue;
            if (!WeatherState.Reaches(WeatherKind.VatBoil, zdo.GetPosition())) continue;
            long started = zdo.GetLong(ZDOVars.s_startTime, 0L);
            zdo.Set(ZDOVars.s_startTime, Math.Max(1L, started - bonus));
        }
    }

    private static bool Fermenting(ZDO zdo) => zdo.GetInt(ZDOVars.s_content) != 0 && zdo.GetLong(ZDOVars.s_startTime, 0L) > 0L;

    // Now and then a fermenter that works faster bubbles, near this player.
    private static void Bubble()
    {
        if (Time.time < _nextBubble) return;
        _nextBubble = Time.time + BubbleInterval;
        Player? player = Player.m_localPlayer;
        if (!player || !WeatherState.IsActive(WeatherKind.VatBoil) || !PluginConfig.ShowFermenterBubbles.IsOn() || !Prefabs()) return;
        Vector3 here = player!.transform.position;
        foreach (KeyValuePair<ZDO, ZNetView> instance in ZNetScene.instance.m_instances)
        {
            ZDO zdo = instance.Key;
            if (!_fermenters!.Contains(zdo.GetPrefab()) || !Fermenting(zdo)) continue;
            Vector3 position = zdo.GetPosition();
            if (Utils.DistanceXZ(position, here) > BubbleRange || !WeatherState.Reaches(WeatherKind.VatBoil, position)) continue;
            if (UnityEngine.Random.value > 0.5f) continue;
            RiteEffects.Sparkle(position + Vector3.up * 1.2f, BubbleColor, BubbleSeconds);
        }
    }

    private static bool Prefabs()
    {
        if (_fermenters != null) return true;
        if (!ZNetScene.instance) return false;
        _fermenters = [];
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            if (prefab && prefab.GetComponent<Fermenter>()) _fermenters.Add(prefab.name.GetStableHashCode());
        return true;
    }

    // ---- Amber-Mend ----

    private static void Mend()
    {
        if (Time.time < _nextMend) return;
        _nextMend = Time.time + MendInterval;
        Player? player = Player.m_localPlayer;
        if (!player || !WeatherState.IsActive(WeatherKind.AmberMend)) return;

        List<WearNTear> pieces = WearNTear.GetAllInstances();
        if (pieces.Count == 0) return;
        Vector3 here = player!.transform.position;
        int mended = 0;
        for (int i = 0; i < ScanPerTick && i < pieces.Count && mended < MendsPerTick; i++)
        {
            _scanIndex = (_scanIndex + 1) % pieces.Count;
            WearNTear piece = pieces[_scanIndex];
            if (!piece || !piece.m_nview || !piece.m_nview.IsValid()) continue;
            Vector3 position = piece.transform.position;
            if (Utils.DistanceXZ(position, here) > MendRange || !WeatherState.Reaches(WeatherKind.AmberMend, position)) continue;
            if (piece.m_nview.GetZDO().GetFloat(ZDOVars.s_health, piece.m_health) >= piece.m_health) continue;
            if (!piece.Repair()) continue;
            mended++;
            // The hammer's repair effect: the piece's own placing effect.
            if (piece.GetComponent<Piece>() is { } built) built.m_placeEffect.Create(position, piece.transform.rotation);
        }
    }
}
