using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// The Ragnarök Rite's treasure table, like a dungeon chest's: rolled a few times for each boss slain. The table lists
// treasures from the least to the most precious ("Amber: 3-6, AmberPearl: 2-5, Ruby: 2-4, SilverNecklace: 1-3"), and
// each boss has a strength from 0 (the weakest in the Bosses setting) to 1 (the strongest). A boss most likely gives
// the treasure at its own place in the table, less likely those near it: Eikthyr mostly amber, Yagluth and the Queen
// mostly rubies and silver necklaces. Stronger bosses also roll toward the treasure's Max.
// Each boss slain gives treasure containers (see TreasureChests): the resources of its own lands (its Loot setting), more for each
// star it had, and its rolls of the treasure table.
internal static class RagnarokLoot
{
    // How far from a boss's own place in the table its chances fall off (in the 0 to 1 span of the table).
    private const float Spread = 0.3f;

    // Half: a one-of-a-kind item (a queen bee, a dragon egg) that grows only half as much with a boss's stars and the
    // players ("QueenBee: 1-2 half").
    internal readonly struct Treasure(string item, int min, int max, bool half = false)
    {
        internal string Item { get; } = item;
        internal int Min { get; } = min;
        internal int Max { get; } = max;
        internal bool Half { get; } = half;
    }

    private static readonly char[] Separators = [',', ';'];
    private const string HalfMark = "half";

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
            bool half = false;
            bool read = item.Length > 0;
            if (read && colon >= 0)
            {
                string range = entry.Substring(colon + 1).Trim();
                if (range.EndsWith(HalfMark, StringComparison.OrdinalIgnoreCase))
                {
                    half = true;
                    range = range.Substring(0, range.Length - HalfMark.Length).Trim();
                }
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
            table.Add(new Treasure(item, Math.Min(min, max), Math.Max(min, max), half));
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

    // ---- Each boss's chest ----

    // How many times a boss's own loot its chest holds: once, and once more times perStar for each star.
    internal static float StarMultiplier(int stars, float perStar) => 1f + Math.Max(0, stars) * Math.Max(0f, perStar);

    // What a "half" item is multiplied by: half as much, never less than once (x4: x2; x2.5: x1.25; x1.5: x1).
    internal static float HalfMultiplier(float multiplier) => Math.Max(1f, multiplier * 0.5f);

    // One tier of a boss's own loot (common, uncommon or rare): how many different items are picked from it, and its items.
    internal readonly struct Tier(int picks, List<Treasure> items)
    {
        internal int Picks { get; } = picks;
        internal List<Treasure> Items { get; } = items;
    }

    // "2 of DeerHide: 10-20, Flint: 20-30" -> 2 picked of [DeerHide 10-20, Flint 20-30]. Without "N of", one is picked.
    internal static Tier ParseTier(string? text, List<string>? problems = null)
    {
        string rest = (text ?? "").Trim();
        int picks = 1;
        int of = rest.IndexOf(" of ", StringComparison.OrdinalIgnoreCase);
        if (of > 0 && int.TryParse(rest.Substring(0, of).Trim(), out int count))
        {
            picks = Math.Max(0, count);
            rest = rest.Substring(of + 4);
        }
        return new Tier(picks, Parse(rest, problems));
    }

    // A boss's own loot (the resources of its lands), from its tiers (common, uncommon, rare): from each, its Picks
    // different items at random (all of them when it has fewer), Min-Max each, times the multiplier (its stars' and the
    // players'; half of it for "half" items), rounded at random so it is right on average. random: 0 to 1.
    internal static List<(string item, int amount)> BossLoot(IList<Tier> tiers, float multiplier, Func<float> random)
    {
        List<(string item, int amount)> loot = [];
        foreach (Tier tier in tiers)
        {
            List<Treasure> left = [.. tier.Items];
            int count = Math.Min(left.Count, Math.Max(0, tier.Picks));
            for (int i = 0; i < count; i++)
            {
                int at = Math.Min(left.Count - 1, (int)Math.Floor(Mathf.Clamp(random(), 0f, 0.9999f) * left.Count));
                Treasure treasure = left[at];
                left.RemoveAt(at);
                int span = treasure.Max - treasure.Min;
                int amount = Math.Min(treasure.Max, treasure.Min + (int)Math.Floor(Mathf.Clamp(random(), 0f, 0.9999f) * (span + 1)));
                Add(loot, treasure.Item, PartyReward.Scale(amount, treasure.Half ? HalfMultiplier(multiplier) : multiplier, random()));
            }
        }
        return loot;
    }

    // The same item added together, in the order they first came.
    internal static void Add(List<(string item, int amount)> loot, string item, int amount)
    {
        if (amount <= 0) return;
        int at = loot.FindIndex(entry => string.Equals(entry.item, item, StringComparison.OrdinalIgnoreCase));
        if (at < 0) loot.Add((item, amount));
        else loot[at] = (loot[at].item, loot[at].amount + amount);
    }

    // Splits a treasure into several containers on purpose, as stacks (item, count up to its stack size): each holds
    // between leastSplit and mostSplit stacks at random (never more than maxSlots), and a last one left with a single stack
    // goes in the one before when it fits. random(min, max): min to max - 1.
    internal static List<List<(string item, int count)>> Split(List<(string item, int amount)> loot, int maxSlots,
        Func<string, int> stackSize, Func<int, int, int> random, int leastSplit = 2, int mostSplit = 5)
    {
        List<(string item, int count)> stacks = [];
        foreach ((string item, int amount) in loot)
        {
            int stack = Math.Max(1, stackSize(item));
            for (int left = amount; left > 0; left -= stack) stacks.Add((item, Math.Min(left, stack)));
        }
        List<List<(string item, int count)>> containers = [];
        maxSlots = Math.Max(1, maxSlots);
        leastSplit = Math.Max(1, Math.Min(leastSplit, maxSlots));
        mostSplit = Math.Max(leastSplit, Math.Min(mostSplit, maxSlots));
        int taken = 0;
        while (taken < stacks.Count)
        {
            int left = stacks.Count - taken;
            int size = Math.Min(left, random(leastSplit, mostSplit + 1));
            // Not a container with a single stack: it goes in this one when it fits.
            if (left - size == 1 && size + 1 <= maxSlots) size++;
            containers.Add(stacks.GetRange(taken, size));
            taken += size;
        }
        return containers;
    }

    // Picks count bosses (by their place in the Bosses setting): different ones from the eligible while there are
    // enough, then different ones from the spare (bosses some but not all have slain), and only then again from all of
    // them, in a random order: each boss has the same chance to come, and to come at any turn. random(n): 0 to n - 1.
    internal static List<int> PickBosses(IList<int> eligible, int count, Func<int, int> random) =>
        PickBosses(eligible, [], count, random);

    internal static List<int> PickBosses(IList<int> eligible, IList<int> spare, int count, Func<int, int> random)
    {
        List<int> picked = [];
        if (eligible.Count == 0 || count <= 0) return picked;
        List<int> all = [.. eligible];
        foreach (int place in spare)
            if (!all.Contains(place)) all.Add(place);
        List<int> left = [.. eligible];
        List<int> spareLeft = all.GetRange(eligible.Count, all.Count - eligible.Count);
        while (picked.Count < count)
        {
            if (left.Count == 0)
            {
                if (spareLeft.Count > 0) (left, spareLeft) = (spareLeft, []);
                else left.AddRange(all);
            }
            int at = random(left.Count);
            picked.Add(left[at]);
            left.RemoveAt(at);
        }
        // Spare bosses were picked last: the order is shuffled so they can come at any turn too.
        for (int i = picked.Count - 1; i > 0; i--)
        {
            int j = random(i + 1);
            (picked[i], picked[j]) = (picked[j], picked[i]);
        }
        return picked;
    }
}
