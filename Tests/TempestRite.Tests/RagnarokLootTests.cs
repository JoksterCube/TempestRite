using System.Collections.Generic;
using System.Linq;
using JoksterCube.TempestRite.Domain;
using Xunit;

namespace JoksterCube.TempestRite.Tests;

public class RagnarokLootTests
{
    private const string Table = "Amber: 3-6, AmberPearl: 2-5, Ruby: 2-4, SilverNecklace: 1-3";

    [Fact]
    public void Reads_the_table()
    {
        List<string> problems = [];
        List<RagnarokLoot.Treasure> table = RagnarokLoot.Parse("Amber: 3-6, Ruby: 2, Coins, Bad: x, Zero: 0-2", problems);
        Assert.Equal(["Amber", "Ruby", "Coins"], table.Select(t => t.Item));
        Assert.Equal((3, 6), (table[0].Min, table[0].Max));
        Assert.Equal((2, 2), (table[1].Min, table[1].Max));
        Assert.Equal((1, 1), (table[2].Min, table[2].Max));
        Assert.Equal(["Bad: x", "Zero: 0-2"], problems);
    }

    [Fact]
    public void Max_below_min_is_turned_around()
    {
        RagnarokLoot.Treasure treasure = RagnarokLoot.Parse("Ruby: 5-2")[0];
        Assert.Equal((2, 5), (treasure.Min, treasure.Max));
    }

    [Theory]
    [InlineData(0, 7, 0f)]
    [InlineData(6, 7, 1f)]
    [InlineData(3, 7, 0.5f)]
    [InlineData(0, 1, 1f)]
    public void Strength_by_place(int place, int bosses, float strength) =>
        Assert.Equal(strength, RagnarokLoot.Strength(place, bosses), 3);

    [Fact]
    public void The_weakest_boss_mostly_gives_amber()
    {
        float[] weights = RagnarokLoot.Weights(4, 0f);
        float total = weights.Sum();
        Assert.True(weights[0] / total > 0.5f);
        Assert.True(weights[3] / total < 0.01f);
    }

    [Fact]
    public void Strong_bosses_mostly_give_rubies_and_necklaces()
    {
        // Yagluth (5th of 7) and the Queen (6th of 7).
        foreach (float strength in new[] { RagnarokLoot.Strength(4, 7), RagnarokLoot.Strength(5, 7) })
        {
            float[] weights = RagnarokLoot.Weights(4, strength);
            float total = weights.Sum();
            Assert.True((weights[2] + weights[3]) / total > 0.6f, strength.ToString());
            Assert.True(weights[0] / total < 0.05f, strength.ToString());
        }
        float[] fader = RagnarokLoot.Weights(4, 1f);
        Assert.Equal(3, System.Array.IndexOf(fader, fader.Max()));
    }

    [Theory]
    [InlineData(0f, 0f, 0)]
    [InlineData(0f, 0.999f, 3)]
    [InlineData(1f, 0f, 0)]
    [InlineData(1f, 0.999f, 3)]
    public void Picks_by_roll(float strength, float roll, int expected) =>
        Assert.Equal(expected, RagnarokLoot.Pick(4, strength, roll));

    [Fact]
    public void Stronger_bosses_roll_more()
    {
        RagnarokLoot.Treasure amber = new("Amber", 3, 6);
        // The weakest stays in the lower half, the strongest in the upper half.
        Assert.Equal(3, RagnarokLoot.Amount(amber, 0f, 0f));
        Assert.Equal(4, RagnarokLoot.Amount(amber, 0f, 0.99f));
        Assert.Equal(5, RagnarokLoot.Amount(amber, 1f, 0f));
        Assert.Equal(6, RagnarokLoot.Amount(amber, 1f, 1f));
    }

    [Fact]
    public void One_roll_per_boss_added_together()
    {
        List<RagnarokLoot.Treasure> table = RagnarokLoot.Parse(Table);
        // Every roll 0: always the first treasure at its minimum.
        List<(string item, int amount)> rolled = RagnarokLoot.Roll(table, [0f, 0f, 0f], () => 0f);
        Assert.Equal([("Amber", 9)], rolled);
    }

    [Fact]
    public void Three_eikthyrs_give_less_than_three_strong_bosses()
    {
        List<RagnarokLoot.Treasure> table = RagnarokLoot.Parse(Table);
        int[] value = [5, 10, 20, 30];
        System.Random random = new(7);
        float Value(float strength)
        {
            float sum = 0f;
            for (int i = 0; i < 2000; i++)
                foreach ((string item, int amount) in RagnarokLoot.Roll(table, [strength, strength, strength], () => (float)random.NextDouble()))
                    sum += value[table.FindIndex(t => t.Item == item)] * amount;
            return sum / 2000f;
        }
        Assert.True(Value(0f) * 2f < Value(RagnarokLoot.Strength(4, 7)));
    }

    [Fact]
    public void Picks_different_bosses()
    {
        int next = 0;
        List<int> picked = RagnarokLoot.PickBosses([4, 0, 2, 6], 3, n => next++ % n);
        Assert.Equal(3, picked.Count);
        Assert.Equal(picked.Distinct().Count(), picked.Count);
    }

    // Every boss comes equally often, and at every turn equally often: not weakest first.
    [Fact]
    public void Every_boss_and_order_equally_likely()
    {
        System.Random rng = new(7);
        int[,] at = new int[3, 4];
        const int runs = 40000;
        for (int run = 0; run < runs; run++)
        {
            List<int> picked = RagnarokLoot.PickBosses([0, 1, 2, 3], 3, n => rng.Next(n));
            for (int turn = 0; turn < 3; turn++) at[turn, picked[turn]]++;
        }
        // Each boss at each turn: a quarter of the runs.
        for (int turn = 0; turn < 3; turn++)
            for (int boss = 0; boss < 4; boss++)
                Assert.InRange(at[turn, boss], runs / 4 * 0.95, runs / 4 * 1.05);
    }

    [Fact]
    public void Too_few_bosses_come_again()
    {
        Assert.Equal([0, 0, 0], RagnarokLoot.PickBosses([0], 3, _ => 0));
        Assert.Equal(3, RagnarokLoot.PickBosses([0, 1], 3, _ => 0).Count);
        Assert.Empty(RagnarokLoot.PickBosses([], 3, _ => 0));
    }

    [Fact]
    public void Spare_bosses_fill_in_before_any_comes_again()
    {
        Assert.Equal([0, 1, 3], RagnarokLoot.PickBosses([0], [3, 1], 3, _ => 0).OrderBy(p => p));
        Assert.Equal([0, 2, 5], RagnarokLoot.PickBosses([2, 0], [5, 0], 3, _ => 0).OrderBy(p => p));
        Assert.Equal([0, 0, 1], RagnarokLoot.PickBosses([0], [1], 3, _ => 0).OrderBy(p => p));
        Assert.Empty(RagnarokLoot.PickBosses([], [1, 2], 3, _ => 0));
    }

    // ---- Each boss's chest ----

    [Theory]
    [InlineData(0, 1f, 1f)]
    [InlineData(2, 1f, 3f)]
    [InlineData(6, 1f, 7f)]
    [InlineData(2, 0.5f, 2f)]
    [InlineData(3, 0f, 1f)]
    public void Each_star_adds_the_loot_again(int stars, float perStar, float multiplier) =>
        Assert.Equal(multiplier, RagnarokLoot.StarMultiplier(stars, perStar), 3);

    private static readonly List<RagnarokLoot.Tier> Tiers =
    [
        RagnarokLoot.ParseTier("2 of TinOre: 10-15, RoundLog: 20-30, Blueberries: 10-20"),
        RagnarokLoot.ParseTier("1 of CopperOre: 15-25, Thistle: 10-15"),
        RagnarokLoot.ParseTier("SurtlingCore: 4-8, AncientSeed: 3-5")
    ];

    [Fact]
    public void Reads_a_tier()
    {
        RagnarokLoot.Tier tier = RagnarokLoot.ParseTier(" 3 of Wood: 10-20, Stone: 5");
        Assert.Equal(3, tier.Picks);
        Assert.Equal(["Wood", "Stone"], tier.Items.Select(t => t.Item));
        // Without "N of": one picked.
        Assert.Equal(1, RagnarokLoot.ParseTier("Wood: 10-20").Picks);
        Assert.Empty(RagnarokLoot.ParseTier("").Items);
        Assert.Empty(RagnarokLoot.ParseTier(null).Items);
    }

    [Fact]
    public void Boss_loot_picks_from_each_tier_times_its_stars()
    {
        // Lowest rolls: the first left of each tier, Min each, times 3.
        List<(string item, int amount)> low = RagnarokLoot.BossLoot(Tiers, 3f, () => 0f);
        Assert.Equal([("TinOre", 30), ("RoundLog", 60), ("CopperOre", 45), ("SurtlingCore", 12)], low);
        // Highest rolls: the last left of each tier, Max each.
        List<(string item, int amount)> high = RagnarokLoot.BossLoot(Tiers, 1f, () => 1f);
        Assert.Equal([("Blueberries", 20), ("RoundLog", 30), ("Thistle", 15), ("AncientSeed", 5)], high);
    }

    [Fact]
    public void Picks_never_the_same_item_twice()
    {
        System.Random random = new(7);
        for (int run = 0; run < 200; run++)
            Assert.Equal(4, RagnarokLoot.BossLoot(Tiers, 1f, () => (float)random.NextDouble()).Count);
        // More picks than items: all of them; none: nothing from that tier.
        Assert.Equal(["Wood", "Stone"], RagnarokLoot.BossLoot([RagnarokLoot.ParseTier("9 of Wood: 1, Stone: 1")], 1f, () => 0f).Select(e => e.item));
        Assert.Empty(RagnarokLoot.BossLoot([RagnarokLoot.ParseTier("0 of Wood: 1, Stone: 1")], 1f, () => 0f));
    }

    // The defaults: every tier has at least one item more than is picked (nothing certain), and every item reads.
    [Fact]
    public void Default_boss_loot_is_never_certain()
    {
        foreach ((string boss, var tiers) in Settings.Constants.Groups.RagnarokBossLoot)
        {
            Assert.Equal(4, tiers.Length);
            foreach (var setting in tiers)
            {
                List<string> problems = [];
                RagnarokLoot.Tier tier = RagnarokLoot.ParseTier(setting.DefaultValue, problems);
                Assert.Empty(problems);
                if (tier.Items.Count == 0) continue;
                Assert.True(tier.Items.Count > tier.Picks, $"{boss} {setting.Name}: {tier.Picks} of {tier.Items.Count}");
                Assert.Equal(tier.Items.Count, tier.Items.Select(t => t.Item).Distinct().Count());
                // No trophies: they come only from slaying the boss or creature yourself.
                Assert.DoesNotContain(tier.Items, t => t.Item.StartsWith("Trophy"));
            }
        }
    }

    [Fact]
    public void Reads_half_items()
    {
        List<string> problems = [];
        List<RagnarokLoot.Treasure> table = RagnarokLoot.Parse("QueenBee: 1-2 half, DragonEgg: 1 HALF, Honey: 10-20", problems);
        Assert.Empty(problems);
        Assert.Equal([true, true, false], table.Select(t => t.Half));
        Assert.Equal((1, 2), (table[0].Min, table[0].Max));
        Assert.Equal((1, 1), (table[1].Min, table[1].Max));
    }

    [Theory]
    [InlineData(4f, 2f)]
    [InlineData(2.5f, 1.25f)]
    [InlineData(1.5f, 1f)]
    [InlineData(1f, 1f)]
    public void Half_items_grow_half_as_much_never_less(float multiplier, float half) =>
        Assert.Equal(half, RagnarokLoot.HalfMultiplier(multiplier), 3);

    [Fact]
    public void Half_items_in_boss_loot()
    {
        // 6 star Eikthyr (x4): 1-2 queen bees become 2-4.
        List<RagnarokLoot.Tier> bees = [RagnarokLoot.ParseTier("1 of QueenBee: 1-2 half")];
        Assert.Equal([("QueenBee", 2)], RagnarokLoot.BossLoot(bees, 4f, () => 0f));
        Assert.Equal([("QueenBee", 4)], RagnarokLoot.BossLoot(bees, 4f, () => 1f));
        // 3 star Moder (x2.5): one dragon egg becomes 1 or 2.
        List<RagnarokLoot.Tier> egg = [RagnarokLoot.ParseTier("1 of DragonEgg: 1 half")];
        Assert.Equal([("DragonEgg", 1)], RagnarokLoot.BossLoot(egg, 2.5f, () => 0f));
        Assert.Equal([("DragonEgg", 2)], RagnarokLoot.BossLoot(egg, 2.5f, () => 1f));
    }

    [Fact]
    public void The_same_item_adds_up()
    {
        List<(string item, int amount)> loot = [("Amber", 2)];
        RagnarokLoot.Add(loot, "Ruby", 1);
        RagnarokLoot.Add(loot, "amber", 3);
        RagnarokLoot.Add(loot, "Coins", 0);
        Assert.Equal([("Amber", 5), ("Ruby", 1)], loot);
    }

    [Fact]
    public void Treasure_is_split_into_several_containers()
    {
        // 20 stacks of wood: containers of 2 to 5 stacks each (6 with a last single one), nothing lost.
        System.Random random = new(3);
        for (int run = 0; run < 200; run++)
        {
            var split = RagnarokLoot.Split([("Wood", 1000)], 32, _ => 50, (min, max) => random.Next(min, max));
            Assert.Equal(1000, split.Sum(c => c.Sum(s => s.count)));
            Assert.All(split, c => Assert.InRange(c.Count, 2, 6));
            Assert.True(split.Count >= 4);
        }
        // A typical boss treasure of 8 stacks: always at least two containers.
        for (int run = 0; run < 200; run++)
            Assert.True(RagnarokLoot.Split([("Wood", 400)], 32, _ => 50, (min, max) => random.Next(min, max)).Count >= 2);
        // A small treasure: one container.
        Assert.Single(RagnarokLoot.Split([("Wood", 100), ("Stone", 30)], 32, _ => 50, (min, _) => min));
        // Always the most: 5, 5, 5, 5.
        Assert.Equal([5, 5, 5, 5], RagnarokLoot.Split([("Wood", 1000)], 32, _ => 50, (_, max) => max - 1).Select(c => c.Count));
        // A last single stack goes in the one before: 2, 2, 3 for 7 stacks.
        Assert.Equal([2, 2, 3], RagnarokLoot.Split([("Wood", 350)], 32, _ => 50, (min, _) => min).Select(c => c.Count));
    }
}
