using System;
using System.Collections.Generic;
using System.Linq;
using JoksterCube.TempestRite.Domain;
using Xunit;

namespace JoksterCube.TempestRite.Tests;

public class RaidCatchUpTests
{
    private static readonly string[] BossKeys = ["defeated_eikthyr", "defeated_gdking", "defeated_bonemass", "defeated_dragon"];
    private static readonly ICollection<string>[] BossItems =
        [new HashSet<string> { "TrophyEikthyr" }, new HashSet<string> { "TrophyTheElder" }, new HashSet<string>(), new HashSet<string>()];

    private static List<string> Run(string[] names, int raids, int seed, bool[]? fitting = null)
    {
        Random random = new(seed);
        Dictionary<string, int> given = [];
        List<string> order = [];
        string? last = null;
        for (int i = 0; i < raids; i++)
        {
            int chosen = RaidCatchUp.Choose(names, fitting ?? new bool[names.Length], given, last, random.Next);
            last = names[chosen];
            given[last] = (given.TryGetValue(last, out int count) ? count : 0) + 1;
            order.Add(last);
        }
        return order;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Every_raid_comes_before_any_repeats(int seed)
    {
        string[] names = ["a", "b", "c", "d", "e"];
        List<string> order = Run(names, 5, seed);
        Assert.Equal(names.OrderBy(n => n), order.OrderBy(n => n));
    }

    [Theory]
    [InlineData(3, 7)]
    [InlineData(10, 4)]
    [InlineData(2, 9)]
    public void Raids_come_equally_often(int count, int raids)
    {
        string[] names = Enumerable.Range(0, count).Select(i => $"r{i}").ToArray();
        for (int seed = 0; seed < 20; seed++)
        {
            List<int> times = names.Select(n => Run(names, raids, seed).Count(o => o == n)).ToList();
            Assert.True(times.Max() - times.Min() <= 1);
        }
    }

    [Fact]
    public void Never_twice_in_a_row()
    {
        string[] names = ["a", "b", "c"];
        for (int seed = 0; seed < 20; seed++)
        {
            List<string> order = Run(names, 30, seed);
            for (int i = 1; i < order.Count; i++) Assert.NotEqual(order[i - 1], order[i]);
        }
    }

    [Fact]
    public void A_single_raid_repeats() => Assert.Equal(["a", "a", "a"], Run(["a"], 3, 0));

    [Fact]
    public void Fitting_biome_goes_first_among_equals()
    {
        List<string> order = Run(["a", "b", "c"], 3, 5, [false, true, false]);
        Assert.Equal("b", order[0]);
    }

    [Fact]
    public void Raid_tier_from_keys_and_trophies()
    {
        Assert.Equal(0, RaidCatchUp.RaidTier([], [], BossKeys, BossItems));
        Assert.Equal(1, RaidCatchUp.RaidTier(["Defeated_Eikthyr"], [], BossKeys, BossItems));
        Assert.Equal(4, RaidCatchUp.RaidTier(["defeated_gdking", "defeated_dragon"], [], BossKeys, BossItems));
        Assert.Equal(2, RaidCatchUp.RaidTier([], ["TrophyTheElder"], BossKeys, BossItems));
        Assert.Equal(-1, RaidCatchUp.RaidTier(["some_modded_key"], [], BossKeys, BossItems));
    }

    [Fact]
    public void Party_tier_is_the_lowest()
    {
        Assert.Equal(2, RaidCatchUp.PartyTier([5, 2, 7]));
        Assert.Equal(-1, RaidCatchUp.PartyTier([]));
    }

    [Theory]
    [InlineData(1, 7, 6)]
    [InlineData(5, 3, 0)]
    [InlineData(-1, 7, 0)]
    [InlineData(1, -1, 0)]
    public void Steps_between_raid_and_party(int raid, int party, int steps) => Assert.Equal(steps, RaidCatchUp.Steps(raid, party));

    [Fact]
    public void Boost_grows_with_steps()
    {
        Assert.False(RaidCatchUp.For(0, 1f).Any);
        Assert.False(RaidCatchUp.For(6, 0f).Any);
        RaidCatchUp.Boost one = RaidCatchUp.For(1, 1f);
        RaidCatchUp.Boost six = RaidCatchUp.For(6, 1f);
        Assert.Equal(2, one.MaxLevel);
        Assert.Equal(1, one.MinLevel);
        Assert.Equal(3, six.MaxLevel);
        Assert.Equal(2, six.MinLevel);
        Assert.True(six.Count > one.Count && six.LevelUpChance > one.LevelUpChance);
        Assert.Equal(70f, RaidCatchUp.For(20, 1f).LevelUpChance);
    }
}
