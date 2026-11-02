using System;
using System.Collections.Generic;

namespace JoksterCube.TempestRite.Domain;

// The Crimson Rite's raids, picked and helped along (the game's parts are in CrimsonRaids and CrimsonSpawnPatches):
// - Turns: every raid the players are ready for comes as often as the others: those that came least so far go first,
//   never the same one twice in a row (when there is another), those fitting the biome first among equals.
// - Catch-up: a raid well below the players brings more, stronger creatures. A raid's tier is the strongest boss it
//   needs slain (its place in the Ragnarök Rite's Bosses setting, plus one; 0: it needs none). The party's tier is the
//   lowest among the players inside of the strongest raid each is ready for. Each step the party is past the raid
//   (times the Raid Catch-Up setting) brings more creatures in bigger groups, more often stars, and higher stars.
internal static class RaidCatchUp
{
    // Each step: this many more creatures (a share of the raid's own).
    private const float CountPerStep = 0.35f;
    // The chance (in percent) of each star up, at the first step and each further step; never above the most.
    private const float FirstChance = 20f;
    private const float ChancePerStep = 10f;
    private const float MostChance = 70f;
    // Steps for each higher star the creatures may get, up to two stars; from this many, at least one star.
    private const float StepsPerStar = 2f;
    private const int MostLevel = 3;
    private const float StarredFrom = 4f;

    internal readonly struct Boost(float count, float levelUpChance, int minLevel, int maxLevel)
    {
        // How many times as many creatures, and as big groups.
        internal float Count { get; } = count;
        // The chance of each star up, in percent (0: the game's own).
        internal float LevelUpChance { get; } = levelUpChance;
        // The level (1: no star) creatures spawn at least at, and may rise to.
        internal int MinLevel { get; } = minLevel;
        internal int MaxLevel { get; } = maxLevel;
        internal bool Any => Count > 1f || LevelUpChance > 0f || MinLevel > 1 || MaxLevel > 1;
    }

    internal static readonly Boost None = new(1f, 0f, 1, 1);

    // How many steps the party is past the raid; 0 when it isn't, or either is unknown (below 0).
    internal static int Steps(int raidTier, int partyTier) =>
        raidTier < 0 || partyTier < 0 ? 0 : Math.Max(0, partyTier - raidTier);

    internal static Boost For(int steps, float strength)
    {
        float s = Math.Max(0, steps) * Math.Max(0f, strength);
        if (s <= 0f) return None;
        float chance = Math.Min(MostChance, FirstChance + ChancePerStep * (s - 1f));
        int maxLevel = Math.Min(MostLevel, 1 + (int)Math.Ceiling(s / StepsPerStar));
        int minLevel = s >= StarredFrom ? 2 : 1;
        return new Boost(1f + CountPerStep * s, Math.Max(0f, chance), Math.Min(minLevel, maxLevel), maxLevel);
    }

    // A raid's tier from what it needs: keys (the world's, or players' own) and items known. bossKeys: each boss's key,
    // weakest first; bossItems: what each drops. 0: it needs nothing; -1: it needs something no boss gives.
    internal static int RaidTier(IEnumerable<string> keys, IEnumerable<string> items,
        IReadOnlyList<string> bossKeys, IReadOnlyList<ICollection<string>> bossItems)
    {
        int tier = 0;
        bool needs = false;
        foreach (string key in keys)
        {
            if (string.IsNullOrEmpty(key)) continue;
            needs = true;
            for (int place = 0; place < bossKeys.Count; place++)
                if (string.Equals(bossKeys[place], key, StringComparison.OrdinalIgnoreCase)) tier = Math.Max(tier, place + 1);
        }
        foreach (string item in items)
        {
            if (string.IsNullOrEmpty(item)) continue;
            needs = true;
            for (int place = 0; place < bossItems.Count; place++)
                if (bossItems[place].Contains(item)) tier = Math.Max(tier, place + 1);
        }
        return needs && tier == 0 ? -1 : tier;
    }

    // The party's tier: the lowest of each player's (the strongest raid they are ready for; -1: none known).
    internal static int PartyTier(IReadOnlyList<int> players)
    {
        if (players.Count == 0) return -1;
        int lowest = int.MaxValue;
        foreach (int tier in players) lowest = Math.Min(lowest, tier);
        return lowest;
    }

    // Which raid comes next (its place among the candidates): one that came least so far, not the last one when there is
    // another, one fitting the biome among equals. random(n): 0 to n - 1.
    internal static int Choose(IReadOnlyList<string> names, IReadOnlyList<bool> fitting, IReadOnlyDictionary<string, int> given,
        string? last, Func<int, int> random)
    {
        if (names.Count == 0) return -1;
        List<int> pool = [];
        for (int i = 0; i < names.Count; i++)
            if (names.Count == 1 || names[i] != last || AllLast(names, last)) pool.Add(i);
        int least = int.MaxValue;
        foreach (int i in pool) least = Math.Min(least, Given(given, names[i]));
        List<int> leastGiven = pool.FindAll(i => Given(given, names[i]) == least);
        List<int> fit = leastGiven.FindAll(i => i < fitting.Count && fitting[i]);
        List<int> from = fit.Count > 0 ? fit : leastGiven;
        return from[Math.Max(0, Math.Min(from.Count - 1, random(from.Count)))];
    }

    private static bool AllLast(IReadOnlyList<string> names, string? last)
    {
        foreach (string name in names)
            if (name != last) return false;
        return true;
    }

    private static int Given(IReadOnlyDictionary<string, int> given, string name) => given.TryGetValue(name, out int count) ? count : 0;
}
