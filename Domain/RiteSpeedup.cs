using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// What a Domain rite speeds up near its obliterator (plants, fermenters, cooking stations, smelters...), and how.
internal abstract class SpedWork(WeatherKind kind)
{
    internal WeatherKind Kind { get; } = kind;
    // How many times as fast it works.
    internal abstract float Speed { get; }
    internal abstract bool ShowMotes { get; }
    // Whether it works on objects made from this prefab, and how big their puffs of motes are (1: normal).
    internal abstract float? Takes(GameObject prefab);
    // Whether the object is working now (growing, fermenting, cooking, smelting), so speeding it up does something.
    internal abstract bool Busy(ZDO zdo, ZNetView view);
    // Moves the object forward by this many seconds of its work. Only by the game that owns it.
    internal abstract void Hurry(ZDO zdo, ZNetView view, double seconds);

    // Where its motes rise from: around the middle of what is shown.
    internal virtual Vector3 MotesAt(ZNetView view, Vector3 position) =>
        view.GetComponentInChildren<Renderer>() is { } renderer && renderer ? renderer.bounds.center : position + Vector3.up * 0.5f;
}

// The Verdant, Vat-Boil and Pitch-Bellows Rites: what they reach works Speed Multiplier times as fast.
//
// Every second, each object a rite reaches that this game owns, and that is working, is moved forward by
// (Speed Multiplier - 1) x the world time that passed; a long step (sleeping) at most an hour. What they did stays after
// the rite ends.
//
// Every couple of seconds, a few of the working objects near the player give off a small puff of motes in the rite's
// color (see DomainVisuals.Burst, which also caps how many show at once).
internal static class RiteSpeedup
{
    private const string Feature = "Rite speedup";
    private const float Interval = 1f;
    // At most this much world time is counted at once (sleeping skips hours).
    private const double MaxStepSeconds = 3600.0;
    // Motes show only within this far of the player: this many puffs every this many seconds.
    private const float ShowRange = 80f;
    private const float MoteInterval = 2f;
    private const int MotesPerTick = 2;

    private static readonly SpedWork[] Works =
        [VerdantGrowth.Work, StationWork.Fermenting, StationWork.Cooking, StationWork.Smelting];

    private static Dictionary<int, (int Work, float Scale)>? _prefabs;
    private static float _next;
    private static float _nextMotes;
    private static double _lastWorldTime = -1.0;
    private static readonly bool[] Active = new bool[Works.Length];
    private static readonly bool[] Show = new bool[Works.Length];
    private static readonly double[] Seconds = new double[Works.Length];
    private static readonly List<(SpedWork Work, ZNetView View, Vector3 Position, float Scale)> Working = [];

    internal static void ClearSession()
    {
        _next = 0f;
        _nextMotes = 0f;
        _lastWorldTime = -1.0;
        Working.Clear();
    }

    // The rite that speeds up objects made from this prefab, or None (for handing them to players with the mod, see
    // WeatherOwnership).
    internal static WeatherKind KindOf(int prefab) =>
        Prefabs() && _prefabs!.TryGetValue(prefab, out (int Work, float Scale) entry) ? Works[entry.Work].Kind : WeatherKind.None;

    // Whether a rite speeds up this object now (for its hover text).
    internal static bool Speeds(WeatherKind kind, Component component) =>
        WeatherState.IsActive(kind) && WeatherState.Reaches(kind, component.transform.position);

    // About how many seconds until something that has `left` seconds of work to go at the usual speed is done: at the
    // rite's speed while it lasts, the usual speed after.
    internal static double ReadyIn(WeatherKind kind, float speed, double left)
    {
        if (left <= 0.0) return 0.0;
        double fast = Math.Max(1f, speed);
        double rite = Math.Max(0f, WeatherState.Remaining(kind));
        return left <= rite * fast ? left / fast : rite + (left - rite * fast);
    }

    // Every frame.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature) || Time.time < _next) return;
        _next = Time.time + Interval;
        bool any = false;
        for (int i = 0; i < Works.Length; i++) any |= Active[i] = WeatherState.IsActive(Works[i].Kind);
        if (!any || !ZNet.instance || !ZNetScene.instance)
        {
            _lastWorldTime = -1.0;
            return;
        }
        double now = ZNet.instance.GetTimeSeconds();
        double step = _lastWorldTime < 0.0 ? 0.0 : Math.Min(now - _lastWorldTime, MaxStepSeconds);
        _lastWorldTime = now;
        if (!Prefabs()) return;

        try { Tick(step); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void Tick(double step)
    {
        Player? player = Player.m_localPlayer;
        Vector3 here = player ? player!.transform.position : Vector3.zero;
        bool motes = Time.time >= _nextMotes;
        if (motes) _nextMotes = Time.time + MoteInterval;
        for (int i = 0; i < Works.Length; i++)
        {
            Seconds[i] = step <= 0.0 ? 0.0 : (Math.Max(1f, Works[i].Speed) - 1.0) * step;
            Show[i] = motes && player && Works[i].ShowMotes;
        }

        long me = ZDOMan.GetSessionID();
        Working.Clear();
        foreach (KeyValuePair<ZDO, ZNetView> instance in ZNetScene.instance.m_instances)
        {
            ZDO zdo = instance.Key;
            if (!_prefabs!.TryGetValue(zdo.GetPrefab(), out (int Work, float Scale) entry) || !Active[entry.Work]) continue;
            int i = entry.Work;
            SpedWork work = Works[i];
            Vector3 position = zdo.GetPosition();
            ZNetView view = instance.Value;
            if (!view || !WeatherState.Reaches(work.Kind, position)) continue;

            bool hurry = Seconds[i] > 0.0 && zdo.GetOwner() == me;
            bool show = Show[i] && Utils.DistanceXZ(position, here) <= ShowRange;
            if ((!hurry && !show) || !work.Busy(zdo, view)) continue;
            if (hurry) work.Hurry(zdo, view, Seconds[i]);
            if (show) Working.Add((work, view, position, entry.Scale));
        }

        for (int n = 0; n < MotesPerTick && Working.Count > 0; n++)
        {
            int pick = UnityEngine.Random.Range(0, Working.Count);
            (SpedWork work, ZNetView view, Vector3 position, float scale) = Working[pick];
            Working.RemoveAt(pick);
            if (view) DomainVisuals.Burst(work.MotesAt(view, position), DomainVisuals.RiteColor(work.Kind), scale);
        }
        Working.Clear();
    }

    // Found once from the game's prefabs.
    private static bool Prefabs()
    {
        if (_prefabs != null) return true;
        if (!ZNetScene.instance) return false;
        _prefabs = [];
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
        {
            if (!prefab) continue;
            for (int i = 0; i < Works.Length; i++)
                if (Works[i].Takes(prefab) is { } scale)
                {
                    _prefabs[prefab.name.GetStableHashCode()] = (i, scale);
                    break;
                }
        }
        return true;
    }
}
