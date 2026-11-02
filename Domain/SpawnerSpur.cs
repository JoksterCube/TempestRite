using System.Collections.Generic;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Harrow-Spur Rite: creature spawners within its area spawn Spawn Speed Multiplier times as fast and keep Creature
// Cap Multiplier times as many creatures around before they stop. A spawner is anything with the game's SpawnArea part
// (Greydwarf nests, Draugr body piles, bone piles, charred spawners, Deep North ones, other mods' too), so new ones are
// found without a list; those named in Excluded Spawners are left alone.
// The game runs a spawner only in the game that owns it (see WeatherOwnership, which hands spawners in the area to a
// player with the mod).
internal static class SpawnerSpur
{
    // The game counts a spawner's timer up by this much every this many seconds.
    private const float Tick = 2f;

    // Prefab names already written to the log.
    private static readonly HashSet<string> Logged = [];

    internal static void ClearSession() => Logged.Clear();

    // Whether the rite reaches a spawner of this prefab at this position.
    internal static bool Reaches(int prefab, Vector3 position) =>
        WeatherState.Reaches(WeatherKind.HarrowSpur, position) && !PluginConfig.HarrowSpurExcludedPrefabs().Contains(prefab);

    // Whether the rite reaches this spawner. Names a spawner in the log the first time, for the Excluded Spawners setting.
    internal static bool Reaches(SpawnArea area)
    {
        if (!WeatherState.Reaches(WeatherKind.HarrowSpur, area.transform.position)) return false;
        ZNetView view = area.m_nview;
        if (!view || view.GetZDO() == null) return false;
        string name = view.GetPrefabName();
        if (Logged.Add(name)) Plugin.ModLogger.LogInfo($"Harrow-Spur Rite reaches spawner {name}.");
        return !PluginConfig.HarrowSpurExcludedPrefabs().Contains(view.GetZDO().GetPrefab());
    }

    // How much more the spawn timer goes up on each of the game's ticks: with 3x, two more ticks' worth.
    internal static float ExtraTime => Tick * (Mathf.Max(1f, PluginConfig.HarrowSpurSpeedMultiplier.Value) - 1f);

    // A spawner's creature limit under the rite.
    // Its puffs of motes on the spawners it reaches (see RiteSpeedup); the speed itself is the spawn timer's.
    internal static readonly SpedWork Work = new NestWork();

    private sealed class NestWork() : SpedWork(WeatherKind.HarrowSpur)
    {
        internal override float Speed => 1f;
        internal override bool ShowMotes => PluginConfig.ShowHarrowSpurMotes.IsOn();
        internal override float? Takes(GameObject prefab) => prefab.GetComponentInChildren<SpawnArea>(true) ? Scale(prefab) : null;
        internal override bool Busy(ZDO zdo, ZNetView view) => !PluginConfig.HarrowSpurExcludedPrefabs().Contains(zdo.GetPrefab());
        internal override void Hurry(ZDO zdo, ZNetView view, double seconds) { }
    }

    // How big a spawner's puffs of little ghosts are, by how wide it is: a bone pile (about 2 m) gets small ones, a big
    // nest or body pile (4 m and more) bigger ones.
    private const float NormalWidth = 2.5f;

    private static float Scale(GameObject prefab)
    {
        float width = 0f;
        foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!filter.sharedMesh) continue;
            Vector3 size = Vector3.Scale(filter.sharedMesh.bounds.size, filter.transform.lossyScale);
            width = Mathf.Max(width, Mathf.Max(size.x, size.z));
        }
        return width <= 0f ? 1f : Mathf.Clamp(width / NormalWidth, 0.5f, 1.6f);
    }

    // What a spawner it reaches does now, for its hover text: seconds between spawns and creatures it keeps close by.
    internal static (float Seconds, int Near) Pace(SpawnArea area) =>
        (area.m_spawnIntervalSec / Mathf.Max(1f, PluginConfig.HarrowSpurSpeedMultiplier.Value), Cap(area.m_maxNear));

    internal static int Cap(int limit) => Mathf.Max(limit, Mathf.RoundToInt(limit * Mathf.Max(1f, PluginConfig.HarrowSpurCapMultiplier.Value)));
}
