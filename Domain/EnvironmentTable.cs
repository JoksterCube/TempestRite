using System;
using System.Collections.Generic;

namespace JoksterCube.TempestRite.Domain;

// Which game environment a weather shows in each biome, read from a setting like
// "Default: Rain, Swamp: SwampRain, Mountain: Snow". Biomes not listed use Default.
internal sealed class EnvironmentTable
{
    internal const string DefaultKey = "Default";

    private readonly Dictionary<int, string> _byBiome = [];

    internal string Default { get; private set; } = string.Empty;
    internal IReadOnlyDictionary<int, string> ByBiome => _byBiome;

    // Entries that couldn't be read, to warn about.
    internal List<string> Problems { get; } = [];

    internal static EnvironmentTable Parse(string text)
    {
        EnvironmentTable table = new();
        foreach (string part in (text ?? string.Empty).Split([',', ';'], StringSplitOptions.RemoveEmptyEntries))
        {
            int colon = part.IndexOf(':');
            if (colon <= 0)
            {
                if (part.Trim().Length > 0) table.Problems.Add(part.Trim());
                continue;
            }
            string key = part.Substring(0, colon).Trim();
            string environment = part.Substring(colon + 1).Trim();
            if (environment.Length == 0)
            {
                table.Problems.Add(part.Trim());
                continue;
            }
            if (key.Equals(DefaultKey, StringComparison.OrdinalIgnoreCase))
                table.Default = environment;
            else if (TryParseBiome(key, out WeatherBiomes biome))
                table._byBiome[(int)biome] = environment;
            else
                table.Problems.Add(part.Trim());
        }
        return table;
    }

    // biome: the game's Heightmap.Biome value.
    internal string For(int biome) => _byBiome.TryGetValue(biome, out string environment) ? environment : Default;

    private static bool TryParseBiome(string key, out WeatherBiomes biome)
    {
        string compact = key.Replace(" ", string.Empty);
        foreach (WeatherBiomes value in Enum.GetValues(typeof(WeatherBiomes)))
        {
            if (value == WeatherBiomes.None) continue;
            if (!value.ToString().Equals(compact, StringComparison.OrdinalIgnoreCase)) continue;
            biome = value;
            return true;
        }
        biome = WeatherBiomes.None;
        return false;
    }
}
