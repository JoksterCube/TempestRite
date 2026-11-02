using System;
using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
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
    // Whether the object gives off motes now and then; busy: it is working (see Busy).
    internal virtual bool Glows(ZDO zdo, ZNetView view, bool busy) => busy;
    // Whether what the rite did while nobody was near is made up for once someone is (see RiteSpeedup).
    internal virtual bool CatchesUp => Speed > 1f;
    // When the object started its work, in world seconds, if it says: it is owed no rite time from before.
    internal virtual double? Since(ZDO zdo) => null;

    // Its puffs of motes: this many every this many seconds. Stations stand close together, so by default they puff one
    // at a time, each puff gone before the next.
    internal virtual int MotesEach => 1;
    internal virtual float MotesEvery => DomainVisuals.BurstSeconds;

    // Where its motes rise from: around the middle of what is shown.
    internal virtual Vector3 MotesAt(ZNetView view, Vector3 position) =>
        view.GetComponentInChildren<Renderer>() is { } renderer && renderer ? renderer.bounds.center : position + Vector3.up * 0.5f;
}

// The Verdant, Vat-Boil, Pitch-Bellows, Kindred and Taproot-Surge Rites: what they reach works Speed Multiplier times as
// fast; the Primal-Hearth Rite's wisp spawners try Draw Multiplier times as often. The Harrow-Spur, Sentry-Sight,
// Pitch-Seal and Hearth-Kindle Rites only puff here: what they do is done in their own patches.
//
// Every second, each object a rite reaches that this game owns, and that is working, is moved forward by
// (Speed Multiplier - 1) x the rite time it is owed: the world time the rite went on there since the object was last
// sped up (saved on it). Rite time passes while nobody is near too (see RiteHistory): an object loaded again is owed
// all of it, even after the rite ended, and makes it up at once. An object that isn't working is owed nothing for that
// time (once it has been loaded a few seconds, so the game has looked at its roof, fire...). Built pieces start owing
// nothing (see RiteHistoryPatches). What they did stays after the rite ends.
//
// Now and then, a few of the working objects near the player give off a small puff of motes in the rite's color (see
// DomainVisuals.Burst, which also caps how many show at once). Each kind of object (plants, fermenters, cooking stations,
// smelters, animals) takes its own turns, so a field of crops never crowds out the fermenters next to it.
internal static class RiteSpeedup
{
    private const string Feature = "Rite speedup";
    private const float Interval = 1f;
    // An object loaded this recently that isn't working may still be getting ready (its roof, fire...): it isn't
    // marked as sped up yet, so it can still make up the time once it works.
    private const float SettleSeconds = 5f;
    // An object not working is marked as sped up at most this often, so idle ones aren't changed every second.
    private const double IdleMarkSeconds = 30.0;
    // On each object: the world time (milliseconds) up to which it was sped up.
    internal static readonly int SpedKey = "TempestRite_SpedTo".GetStableHashCode();
    // Motes (and Pitch-Bellows and Taproot-Surge fire) show only within this far of the player.
    private const float ShowRange = 80f;

    private static readonly SpedWork[] Works =
    [
        VerdantGrowth.Work, StationWork.Fermenting, StationWork.Cooking, TaprootFlow.Refining, StationWork.Smelting, KindredBreeding.Work,
        SpawnerSpur.Work, SentryTargets.Work, TaprootFlow.Extracting, TaprootFlow.Refilling, DropAnchor.Work, LureSurge.Work, RestKindle.Work
    ];

    // Each prefab's work (the first that takes it) and its puffs' size.
    private static Dictionary<int, (int Work, float Scale)>? _prefabs;
    private static readonly List<KeyValuePair<ZDO, ZNetView>> Instances = [];
    private static float _next;
    private static readonly bool[] Active = new bool[Works.Length];
    private static readonly bool[] Show = new bool[Works.Length];
    // Whether some rite time may still be owed somewhere for this work.
    private static readonly bool[] Owing = new bool[Works.Length];
    private static readonly WeatherKind[] Kinds = KindsCaughtUp();
    // When each loaded object was first seen (see SettleSeconds).
    private static readonly Dictionary<ZDO, float> FirstSeen = [];
    private static float _nextPrune;
    private static readonly float[] NextMotes = new float[Works.Length];
    private static readonly List<(ZNetView View, Vector3 Position, float Scale)>[] Working = MakeWorking();
    // The last one of each kind that puffed: the next turn goes to another when there is one.
    private static readonly ZNetView?[] LastShown = new ZNetView?[Works.Length];

    private static List<(ZNetView View, Vector3 Position, float Scale)>[] MakeWorking()
    {
        var working = new List<(ZNetView View, Vector3 Position, float Scale)>[Works.Length];
        for (int i = 0; i < working.Length; i++) working[i] = [];
        return working;
    }

    private static WeatherKind[] KindsCaughtUp()
    {
        List<WeatherKind> kinds = [];
        foreach (SpedWork work in Works)
            if (work.CatchesUp && !kinds.Contains(work.Kind)) kinds.Add(work.Kind);
        return [.. kinds];
    }

    internal static void ClearSession()
    {
        _next = 0f;
        _nextPrune = 0f;
        FirstSeen.Clear();
        RiteHistory.ClearSession();
        for (int i = 0; i < Works.Length; i++)
        {
            NextMotes[i] = 0f;
            LastShown[i] = null;
            Working[i].Clear();
        }
        SmelterFire.ClearSession();
        DropAnchor.ClearSession();
        LureGlow.ClearSession();
        TetherGlow.ClearSession();
    }

    // The rite that speeds up objects made from this prefab, or None (for handing them to players with the mod, see
    // WeatherOwnership).
    internal static WeatherKind KindOf(int prefab) =>
        Prefabs() && _prefabs!.TryGetValue(prefab, out (int Work, float Scale) entry) ? Works[entry.Work].Kind : WeatherKind.None;

    // Whether this rite works on objects made from this prefab (going on or not).
    internal static bool Takes(WeatherKind kind, int prefab) =>
        Prefabs() && _prefabs!.TryGetValue(prefab, out (int Work, float Scale) entry) && Works[entry.Work].Kind == kind;

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

    // Marks an object as sped up to now: a piece just built is owed nothing from before (see RiteHistoryPatches).
    internal static void MarkNow(ZDO zdo)
    {
        if (ZNet.instance && Prefabs() && _prefabs!.TryGetValue(zdo.GetPrefab(), out (int Work, float Scale) entry) && Works[entry.Work].CatchesUp)
            Mark(zdo, ZNet.instance.GetTimeSeconds());
    }

    // When the object was last sped up (world seconds), or when it started working if later and known.
    internal static double SpedTo(ZDO zdo, SpedWork? work = null)
    {
        long marked = zdo.GetLong(SpedKey, 0L);
        double from = marked > 0L ? marked / 1000.0 : double.NegativeInfinity;
        return work?.Since(zdo) is { } since ? Math.Max(from, since) : from;
    }

    // Whether an object made from this prefab at this point still has rite time owed (for handing it to players with
    // the mod, see WeatherOwnership).
    internal static bool Owed(ZDO zdo, Vector3 position)
    {
        if (!Prefabs() || !_prefabs!.TryGetValue(zdo.GetPrefab(), out (int Work, float Scale) entry)) return false;
        SpedWork work = Works[entry.Work];
        return work.CatchesUp && RiteHistory.Pending(work.Kind, position, SpedTo(zdo, work));
    }

    private static void Mark(ZDO zdo, double now) => zdo.Set(SpedKey, Math.Max(1L, (long)(now * 1000.0)));

    // Ticks to world seconds (the planted, picked, started... times the game saves); null when not set.
    internal static double? FromTicks(long ticks) => ticks > 1L ? ticks / (double)TimeSpan.TicksPerSecond : null;

    // Every frame.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature) || Time.time < _next) return;
        _next = Time.time + Interval;
        try
        {
            if (!ZNet.instance || !ZNetScene.instance || !Prefabs())
            {
                SmelterFire.Sweep();
                DropAnchor.Sweep();
                LureGlow.Sweep();
                return;
            }
            double now = ZNet.instance.GetTimeSeconds();
            RiteHistory.Track(Kinds, now);
            bool any = false;
            for (int i = 0; i < Works.Length; i++)
            {
                any |= Active[i] = WeatherState.IsActive(Works[i].Kind);
                any |= Owing[i] = Works[i].CatchesUp && HasSpan(Works[i].Kind);
            }
            if (!any)
            {
                FirstSeen.Clear();
                SmelterFire.Sweep();
                DropAnchor.Sweep();
                LureGlow.Sweep();
                return;
            }
            Tick(now);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static bool HasSpan(WeatherKind kind)
    {
        foreach (RiteHistory.Span span in RiteHistory.All)
            if (span.Kind == kind) return true;
        return false;
    }

    private static void Tick(double now)
    {
        Player? player = Player.m_localPlayer;
        Vector3 here = player ? player!.transform.position : Vector3.zero;
        for (int i = 0; i < Works.Length; i++)
        {
            Show[i] = player && Works[i].ShowMotes && Time.time >= NextMotes[i];
            Working[i].Clear();
        }

        long me = ZDOMan.GetSessionID();
        // A copy: speeding things up can spawn or remove objects (a plant growing, a wisp drawn), which changes the
        // game's list while it is gone through.
        Instances.Clear();
        Instances.AddRange(ZNetScene.instance.m_instances);
        foreach (KeyValuePair<ZDO, ZNetView> instance in Instances)
        {
            ZDO zdo = instance.Key;
            if (!_prefabs!.TryGetValue(zdo.GetPrefab(), out (int Work, float Scale) entry)) continue;
            int i = entry.Work;
            if (!Active[i] && !Owing[i]) continue;
            SpedWork work = Works[i];
            Vector3 position = zdo.GetPosition();
            ZNetView view = instance.Value;
            if (!view) continue;
            bool mine = zdo.GetOwner() == me;
            bool? busy = Owing[i] && mine ? CatchUp(zdo, view, work, position, now) : null;
            if (!Active[i] || !WeatherState.Reaches(work.Kind, position)) continue;

            bool near = player && Utils.DistanceXZ(position, here) <= ShowRange;
            if (near && work == StationWork.Smelting) SmelterFire.Keep(view, PluginConfig.PitchBellowsFireTint.Value, false, 1f);
            else if (near && work == TaprootFlow.Refining) SmelterFire.Keep(view, PluginConfig.TaprootSurgeFireTint.Value, true, Math.Max(1f, work.Speed));
            else if (near && work == DropAnchor.Work) DropAnchor.Keep(view);
            else if (near && work == LureSurge.Work) LureGlow.Keep(view);
            // What doesn't catch up (the Primal-Hearth Rite's extra draws) is handed its share of each second as it passes.
            if (!work.CatchesUp && work.Speed > 1f && mine && (busy ??= work.Busy(zdo, view)) == true)
                work.Hurry(zdo, view, (work.Speed - 1.0) * Interval);
            if (!Show[i] || !near) continue;
            if (work.Glows(zdo, view, busy ??= work.Busy(zdo, view))) Working[i].Add((view, position, entry.Scale));
        }
        Instances.Clear();
        SmelterFire.Sweep();
        DropAnchor.Sweep();
        LureGlow.Sweep();
        PruneSeen();

        for (int i = 0; i < Works.Length; i++)
            if (Show[i] && Working[i].Count > 0) Puff(i);
    }

    // Speeds an object up by the rite time it is owed, and marks it as sped up to now. Returns whether it is working,
    // or null when it wasn't looked at (owed nothing).
    private static bool? CatchUp(ZDO zdo, ZNetView view, SpedWork work, Vector3 position, double now)
    {
        if (!RiteHistory.Reaches(work.Kind, position)) return null;
        double from = SpedTo(zdo, work);
        // Within a Crimson, Ragnarök or Hoard-Bane Rite's bubble the rite does nothing: that time isn't owed.
        if (WeatherState.Suspended(work.Kind, position))
        {
            if (now - from >= IdleMarkSeconds) Mark(zdo, now);
            return null;
        }
        double owed = RiteHistory.Owed(work.Kind, position, from, now);
        if (owed <= 0.0) return null;
        bool busy = work.Busy(zdo, view);
        if (busy)
        {
            work.Hurry(zdo, view, (Math.Max(1f, work.Speed) - 1.0) * owed);
            Mark(zdo, now);
        }
        else if (now - from >= IdleMarkSeconds && Settled(zdo)) Mark(zdo, now);
        return busy;
    }

    // Whether a loaded object has been loaded long enough for the game to have looked at it (roof, fire...).
    private static bool Settled(ZDO zdo)
    {
        if (!FirstSeen.TryGetValue(zdo, out float seen))
        {
            FirstSeen[zdo] = Time.time;
            return false;
        }
        return Time.time - seen >= SettleSeconds;
    }

    // Now and then, forgets objects no longer loaded.
    private static void PruneSeen()
    {
        if (Time.time < _nextPrune) return;
        _nextPrune = Time.time + 30f;
        List<ZDO>? gone = null;
        foreach (ZDO zdo in FirstSeen.Keys)
            if (!ZNetScene.instance.m_instances.ContainsKey(zdo)) (gone ??= []).Add(zdo);
        if (gone != null) foreach (ZDO zdo in gone) FirstSeen.Remove(zdo);
    }

    // A few of one kind's working objects puff; its next turn waits only once one did.
    private static void Puff(int i)
    {
        SpedWork work = Works[i];
        List<(ZNetView View, Vector3 Position, float Scale)> working = Working[i];
        if (working.Count > 1 && LastShown[i] is { } last && last)
            working.RemoveAll(entry => entry.View == last);
        for (int n = 0; n < work.MotesEach && working.Count > 0; n++)
        {
            int pick = UnityEngine.Random.Range(0, working.Count);
            (ZNetView view, Vector3 position, float scale) = working[pick];
            working.RemoveAt(pick);
            if (!view || !DomainVisuals.Burst(work.Kind, work.MotesAt(view, position), DomainVisuals.RiteColor(work.Kind), scale)) continue;
            LastShown[i] = view;
            NextMotes[i] = Time.time + work.MotesEvery;
        }
        working.Clear();
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
            {
                if (Works[i].Takes(prefab) is not { } takes) continue;
                _prefabs[prefab.name.GetStableHashCode()] = (i, takes);
                break;
            }
        }
        return true;
    }
}
