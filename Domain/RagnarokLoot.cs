using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Ragnarök Rite's treasure table, like a dungeon chest's: one item rolled for each boss slain. The table lists
// treasures from the least to the most precious ("Amber: 3-6, AmberPearl: 2-5, Ruby: 2-4, SilverNecklace: 1-3"), and
// each boss has a strength from 0 (the weakest in the Bosses setting) to 1 (the strongest). A boss most likely gives
// the treasure at its own place in the table, less likely those near it: Eikthyr mostly amber, Yagluth and the Queen
// mostly rubies and silver necklaces. Stronger bosses also roll toward the treasure's Max.
internal static class RagnarokLoot
{
    // How far from a boss's own place in the table its chances fall off (in the 0 to 1 span of the table).
    private const float Spread = 0.3f;

    internal readonly struct Treasure(string item, int min, int max)
    {
        internal string Item { get; } = item;
        internal int Min { get; } = min;
        internal int Max { get; } = max;
    }

    private static readonly char[] Separators = [',', ';'];

    // "Amber: 3-6, Ruby: 2" -> [Amber 3-6, Ruby 2-2]. Entries that can't be read go to problems.
    internal static List<Treasure> Parse(string? text, List<string>? problems = null)
    {
        List<Treasure> table = [];
        if (string.IsNullOrEmpty(text)) return table;
        foreach (string part in text!.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            string entry = part.Trim();
            if (entry.Length == 0) continue;
            int colon = entry.IndexOf(':');
            string item = (colon < 0 ? entry : entry.Substring(0, colon)).Trim();
            int min = 1, max = 1;
            bool read = item.Length > 0;
            if (read && colon >= 0)
            {
                string range = entry.Substring(colon + 1).Trim();
                int dash = range.IndexOf('-');
                read = dash < 0
                    ? int.TryParse(range, out min) && (max = min) >= 0
                    : int.TryParse(range.Substring(0, dash).Trim(), out min) && int.TryParse(range.Substring(dash + 1).Trim(), out max);
            }
            if (!read || min < 1 || max < 1)
            {
                problems?.Add(entry);
                continue;
            }
            table.Add(new Treasure(item, Math.Min(min, max), Math.Max(min, max)));
        }
        return table;
    }

    // A boss's strength from its place in the Bosses setting: 0 the first (weakest), 1 the last.
    internal static float Strength(int place, int bosses) =>
        bosses <= 1 ? 1f : Mathf.Clamp01(place / (float)(bosses - 1));

    // Each treasure's weight for a boss this strong: highest at the boss's own place in the table.
    internal static float[] Weights(int treasures, float strength)
    {
        float[] weights = new float[Math.Max(0, treasures)];
        for (int i = 0; i < weights.Length; i++)
        {
            float place = treasures <= 1 ? 1f : i / (float)(treasures - 1);
            float off = place - Mathf.Clamp01(strength);
            weights[i] = Mathf.Exp(-off * off / (2f * Spread * Spread));
        }
        return weights;
    }

    // Which treasure: roll from 0 to 1.
    internal static int Pick(int treasures, float strength, float roll)
    {
        if (treasures <= 0) return -1;
        float[] weights = Weights(treasures, strength);
        float total = 0f;
        foreach (float weight in weights) total += weight;
        float at = Mathf.Clamp01(roll) * total;
        for (int i = 0; i < weights.Length; i++)
        {
            if (at < weights[i]) return i;
            at -= weights[i];
        }
        return weights.Length - 1;
    }

    // How many: the weakest boss rolls in the lower half of Min-Max, the strongest in the upper half. Roll from 0 to 1.
    internal static int Amount(Treasure treasure, float strength, float roll)
    {
        int span = treasure.Max - treasure.Min;
        float at = Mathf.Clamp01(0.5f * Mathf.Clamp01(strength) + 0.5f * Mathf.Clamp01(roll));
        return Math.Min(treasure.Max, treasure.Min + Mathf.FloorToInt(at * (span + 1)));
    }

    // One item for each boss slain, by strength; the same items added together, in the table's order.
    internal static List<(string item, int amount)> Roll(List<Treasure> table, IEnumerable<float> strengths, Func<float> random)
    {
        int[] amounts = new int[table.Count];
        foreach (float strength in strengths)
        {
            int index = Pick(table.Count, strength, random());
            if (index < 0) break;
            amounts[index] += Amount(table[index], strength, random());
        }
        List<(string item, int amount)> rolled = [];
        for (int i = 0; i < table.Count; i++)
            if (amounts[i] > 0) rolled.Add((table[i].Item, amounts[i]));
        return rolled;
    }

    // Picks count bosses (by their place in the Bosses setting) among the eligible ones: different ones while there
    // are enough, then again from all of them; weakest first. random(n): 0 to n - 1.
    internal static List<int> PickBosses(IList<int> eligible, int count, Func<int, int> random)
    {
        List<int> picked = [];
        if (eligible.Count == 0 || count <= 0) return picked;
        List<int> left = [.. eligible];
        while (picked.Count < count)
        {
            if (left.Count == 0) left.AddRange(eligible);
            int at = random(left.Count);
            picked.Add(left[at]);
            left.RemoveAt(at);
        }
        picked.Sort();
        return picked;
    }
}
