using System;
using System.Runtime.CompilerServices;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Primal-Hearth Rite: wisp fountains and eternal pyres (both the game's wisp spawner) within its area draw more:
// - they try Draw Multiplier times as often (the game tries every 10 seconds, on the game that owns them; the rite adds
//   tries in between, see Hurry), and the game's shortest wait between two draws is that many times shorter;
// - they are full only with Capacity Multiplier times as many around them. Every game with the mod reads it the same
//   way, so wisps and embers keep coming to them there.
// - a lure it reaches takes only its own kind: wisps go to wisp fountains, embers to eternal pyres (see Seeking);
// - they glow brighter near the player (see LureGlow).
// They are handed to players with the mod (see WeatherOwnership). Ones drawing now and then give off sulfur-yellow
// flames (see RiteSpeedup).
internal static class LureSurge
{
    // The game's time between two tries.
    private const double TryEvery = 10.0;
    // Extra time saved up at most (after a long step, such as sleeping, it doesn't burst out many at once).
    private const double MaxSaved = TryEvery * 2.0;

    internal static readonly SpedWork Work = new LureWork();

    // Extra try time saved up by each spawner.
    private static readonly ConditionalWeakTable<WispSpawner, StrongBox<double>> Saved = new();

    internal static bool Reaches(Component spawner) => spawner && WeatherState.Reaches(WeatherKind.PrimalHearth, spawner.transform.position);

    internal static float Draw => Mathf.Max(1f, PluginConfig.PrimalHearthSpawnMultiplier.Value);

    internal static float Capacity => Mathf.Max(1f, PluginConfig.PrimalHearthCapacityMultiplier.Value);

    // ---- Each to its own kind ----
    // The game sends every lured wisp or ember to the nearest lure that isn't full, of either kind: a wisp fountain's
    // wisps (sent from 20 m away) end up at an eternal pyre nearby, and both count each other's toward being full.
    // Under the rite, a lure it reaches takes only its own kind and counts only its own kind. Elsewhere as usual.

    // The kind (prefab name) of the lured wisp or ember choosing where to go now, or null.
    internal static string? Seeking;
    // The kind the lure working out whether it is full counts, or null (all).
    internal static string? Counting;

    private static readonly ConditionalWeakTable<LuredWisp, string> Kinds = new();

    internal static string KindOf(LuredWisp wisp) => Kinds.GetValue(wisp, static lured => Utils.GetPrefabName(lured.gameObject));

    internal static string? Draws(WispSpawner spawner) => spawner.m_wispPrefab ? spawner.m_wispPrefab.name : null;

    // The game's choice (WispSpawner.GetBestSpawner), skipping lures the rite reaches that draw another kind.
    internal static WispSpawner? BestFor(string kind, Vector3 point, float maxRange)
    {
        WispSpawner? best = null;
        float bestDistance = 0f;
        foreach (WispSpawner spawner in WispSpawner.s_spawners)
        {
            if (!spawner || !spawner.m_spawnPoint) continue;
            float distance = Vector3.Distance(spawner.m_spawnPoint.position, point);
            if (distance > maxRange || (best && distance >= bestDistance)) continue;
            if (Reaches(spawner) && Draws(spawner) is { } draws && draws != kind) continue;
            WispSpawner.Status status = spawner.GetStatus();
            if (status == WispSpawner.Status.NoSpace || status == WispSpawner.Status.TooBright) continue;
            if (status == WispSpawner.Status.Full && distance > spawner.m_maxSpawnedArea) continue;
            best = spawner;
            bestDistance = distance;
        }
        return best;
    }

    // How many lured ones of this kind are within r of the point (LuredWisp.GetWispsInArea, one kind only).
    internal static int Around(string kind, Vector3 point, float r)
    {
        float r2 = r * r;
        int count = 0;
        foreach (LuredWisp wisp in LuredWisp.m_wisps)
            if (wisp && Utils.DistanceSqr(point, wisp.transform.position) < r2 && KindOf(wisp) == kind) count++;
        return count;
    }

    private sealed class LureWork() : SpedWork(WeatherKind.PrimalHearth)
    {
        // The rite loop hands over (Draw - 1) x the time passed: that time brings extra tries.
        internal override float Speed => Draw;
        // Wisps drawn while nobody was near aren't made up for.
        internal override bool CatchesUp => false;
        internal override bool ShowMotes => PluginConfig.ShowPrimalHearthMotes.IsOn();
        internal override float? Takes(GameObject prefab) => prefab.GetComponentInChildren<WispSpawner>(true) ? 0.9f : null;

        // Drawing: night, in the open, not full.
        internal override bool Busy(ZDO zdo, ZNetView view) =>
            view.GetComponentInChildren<WispSpawner>() is { } spawner && spawner && spawner.GetStatus() == WispSpawner.Status.Ok;

        // One extra try for each 10 seconds saved up, at most one per call (once a second).
        internal override void Hurry(ZDO zdo, ZNetView view, double seconds)
        {
            if (view.GetComponentInChildren<WispSpawner>() is not { } spawner || !spawner) return;
            StrongBox<double> saved = Saved.GetOrCreateValue(spawner);
            saved.Value = Math.Min(saved.Value + seconds, MaxSaved);
            if (saved.Value < TryEvery) return;
            saved.Value -= TryEvery;
            spawner.TrySpawn();
        }

        internal override Vector3 MotesAt(ZNetView view, Vector3 position) =>
            view.GetComponentInChildren<WispSpawner>() is { } spawner && spawner && spawner.m_spawnPoint
                ? spawner.m_spawnPoint.position
                : position + Vector3.up * 1.5f;
    }
}
