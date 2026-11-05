using System;
using System.Collections.Generic;
using JoksterCube.ChestKit.Data;

namespace JoksterCube.TempestRite.Domain;

// The game's biomes, with the same values as Heightmap.Biome, but without its "All" and "Land" so Configuration
// Manager shows one checkbox per biome.
[Flags]
internal enum WeatherBiomes
{
    None = 0,
    Meadows = 1,
    Swamp = 2,
    Mountain = 4,
    BlackForest = 8,
    Plains = 16,
    AshLands = 32,
    DeepNorth = 64,
    Ocean = 256,
    Mistlands = 512
}

internal static class WeatherBiomesExtensions
{
    private static readonly WeatherBiomes[] Each =
    [
        WeatherBiomes.Meadows, WeatherBiomes.BlackForest, WeatherBiomes.Swamp, WeatherBiomes.Mountain, WeatherBiomes.Plains,
        WeatherBiomes.Mistlands, WeatherBiomes.AshLands, WeatherBiomes.DeepNorth, WeatherBiomes.Ocean
    ];

    // A biome the game hasn't worked out yet (None) counts as affected.
    internal static bool Includes(this WeatherBiomes affected, int biome) =>
        biome == 0 || ((int)affected & biome) != 0;

    // The biomes left, without those where keepsOwn (given the game's biome value) says the biome keeps its own weather.
    internal static WeatherBiomes Without(this WeatherBiomes affected, Func<int, bool> keepsOwn)
    {
        foreach (WeatherBiomes biome in Each)
            if (keepsOwn((int)biome)) affected &= ~biome;
        return affected;
    }

    // Names of the biomes left out, for the status tooltip: "Ashlands, Deep North".
    internal static string LeftOut(this WeatherBiomes affected)
    {
        List<string> names = [];
        foreach (WeatherBiomes biome in Each)
            if ((affected & biome) == 0) names.Add(Name(biome));
        return string.Join(", ", names);
    }

    // In the player's language, as the game names it on the map ("$biome_blackforest"); English outside the game.
    private static string Name(WeatherBiomes biome) =>
        Translations.Game("$biome_" + biome.ToString().ToLowerInvariant(), biome switch
        {
            WeatherBiomes.BlackForest => "Black Forest",
            WeatherBiomes.AshLands => "Ashlands",
            WeatherBiomes.DeepNorth => "Deep North",
            _ => biome.ToString()
        });
}
