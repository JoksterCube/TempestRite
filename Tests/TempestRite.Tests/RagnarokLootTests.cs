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
    public void Picks_different_bosses_weakest_first()
    {
        int next = 0;
        List<int> picked = RagnarokLoot.PickBosses([4, 0, 2, 6], 3, n => next++ % n);
        Assert.Equal(3, picked.Count);
        Assert.Equal(picked.Distinct().Count(), picked.Count);
        Assert.Equal(picked.OrderBy(p => p), picked);
    }

    [Fact]
    public void Too_few_bosses_come_again()
    {
        Assert.Equal([0, 0, 0], RagnarokLoot.PickBosses([0], 3, _ => 0));
        Assert.Equal(3, RagnarokLoot.PickBosses([0, 1], 3, _ => 0).Count);
        Assert.Empty(RagnarokLoot.PickBosses([], 3, _ => 0));
    }
}
