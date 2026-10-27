using System.Collections.Generic;
using JoksterCube.TempestRite.Settings;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The game environment called weather puts in place of the biome's own.
//
// Valheim works the weather out in each game on its own: the world time is cut into periods, and each period picks
// a weather from the biome's list with the period number as its random seed, so every game picks the same. The mod
// takes the place of that pick in each game with the mod; the server doesn't send weather. Raids, boss fights,
// dungeons and other places with weather of their own keep it.
//
// Each weather shows as the biome's own kind of it (snow in the mountains, Mistlands rain in the Mistlands...), from
// its Environments setting. Called weather never makes a place warmer: cold and freezing still come from the weather
// the biome would have had (see TemperaturePatch).
internal static class WeatherEnvironment
{
    private static readonly HashSet<string> Unknown = [];

    // Whether the weather was replaced in the last environment update.
    internal static bool Overriding { get; private set; }

    internal static void ClearSession()
    {
        Unknown.Clear();
        Overriding = false;
        _naturalKey = default;
        _natural = null;
    }

    // Called by the game's environment update when nothing else sets the weather.
    internal static string? Current(EnvMan envMan)
    {
        Overriding = false;
        WeatherKind kind = WeatherState.Active(EffectSlot.Weather);
        if (kind == WeatherKind.None) return null;
        CustomEnvironments.Ensure(envMan);

        int biome = (int)envMan.GetBiome().Biome;
        if (!PluginConfig.AffectedBiomes.Value.Includes(biome)) return null;

        EnvironmentTable? table = PluginConfig.For(kind)?.Table;
        if (table == null) return null;
        string name = table.For(biome);
        if (!Exists(envMan, name))
        {
            // A biome entry naming an environment the game doesn't have falls back to Default.
            name = table.Default;
            if (!Exists(envMan, name)) return null;
        }
        Overriding = true;
        return name;
    }

    // Called by the game's environment update when something else sets the weather, so it isn't ours.
    internal static void NotOverriding() => Overriding = false;

    // The Crimson Rite's blood moon, near its obliterator: over everything else, raids' own skies too.
    internal static string? Crimson(EnvMan envMan)
    {
        if (!WeatherState.IsActive(WeatherKind.Crimson)) return null;
        Camera? camera = Utils.GetMainCamera();
        if (!camera || !WeatherState.Reaches(WeatherKind.Crimson, camera!.transform.position)) return null;
        CustomEnvironments.Ensure(envMan);
        string name = PluginConfig.CrimsonEnvironment.Value;
        return Exists(envMan, name) ? name : null;
    }

    private static bool Exists(EnvMan envMan, string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (envMan.GetEnv(name) != null) return true;
        if (Unknown.Add(name))
            Plugin.ModLogger.LogWarning($"Environment \"{name}\" is not one the game knows. Check the Environments settings.");
        return false;
    }

    // ---- The weather the biome would have had ----

    private static (long period, int biome, bool ashlands, bool deepNorth) _naturalKey;
    private static EnvSetup? _natural;

    // The same pick the game makes (EnvMan.UpdateEnvironment), worked out again once per weather period or biome.
    internal static EnvSetup? Natural(EnvMan envMan)
    {
        Camera? camera = Utils.GetMainCamera();
        if (!camera || !ZNet.instance) return null;
        Vector3 position = camera!.transform.position;
        BiomeSector sector = envMan.GetBiome();
        long period = (long)ZNet.instance.GetTimeSeconds() / envMan.m_environmentDuration;
        bool ashlands = WorldGenerator.IsAshlands(position.x, position.z);
        // The game passes y here too.
        bool deepNorth = WorldGenerator.IsDeepnorth(position.x, position.y);
        var key = (period, (int)sector.Biome, ashlands, deepNorth);
        if (_natural != null && key == _naturalKey) return _natural;

        _naturalKey = key;
        _natural = null;
        Random.State state = Random.state;
        try
        {
            Random.InitState((int)period);
            List<EnvEntry>? environments = envMan.GetAvailableEnvironments(sector);
            if (environments == null || environments.Count == 0) return null;
            EnvSetup? picked = envMan.SelectWeightedEnvironment(environments);
            foreach (EnvEntry entry in environments)
            {
                if (entry.m_ashlandsOverride && ashlands) picked = entry.m_env;
                if (entry.m_deepnorthOverride && deepNorth) picked = entry.m_env;
            }
            _natural = picked;
            return picked;
        }
        finally
        {
            Random.state = state;
        }
    }
}
