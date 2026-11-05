using System.Collections.Generic;
using JoksterCube.TempestRite.Domain;
using Xunit;

namespace JoksterCube.TempestRite.Tests;

public class PartyRewardTests
{
    [Fact]
    public void Each_player_beyond_the_first_adds_the_share()
    {
        Assert.Equal(1f, PartyReward.Multiplier(1, 0.5f));
        Assert.Equal(1.5f, PartyReward.Multiplier(2, 0.5f));
        Assert.Equal(2.5f, PartyReward.Multiplier(4, 0.5f));
        Assert.Equal(3f, PartyReward.Multiplier(3, 1f));
        // Nobody counted, or no share: as alone.
        Assert.Equal(1f, PartyReward.Multiplier(0, 0.5f));
        Assert.Equal(1f, PartyReward.Multiplier(5, 0f));
    }

    [Fact]
    public void Players_beyond_the_cap_add_nothing()
    {
        Assert.Equal(2.5f, PartyReward.Multiplier(4, 0.5f, 4));
        Assert.Equal(2.5f, PartyReward.Multiplier(6, 0.5f, 4));
        Assert.Equal(2f, PartyReward.Multiplier(3, 0.5f, 4));
        Assert.Equal(1f, PartyReward.Multiplier(5, 0.5f, 1));
    }

    [Fact]
    public void Crimson_defaults_give_one_more_coin_per_kill_for_each_player()
    {
        // 2 coins per kill, 10 kills: (1 + players) per kill.
        for (int players = 1; players <= 4; players++)
            Assert.Equal((1 + players) * 10, PartyReward.Scale(2 * 10, PartyReward.Multiplier(players, 0.5f), 0f));
    }

    [Fact]
    public void Fractions_round_at_random()
    {
        Assert.Equal(1, PartyReward.Scale(1, 1.5f, 0.49f));
        Assert.Equal(2, PartyReward.Scale(1, 1.5f, 0.5f));
        Assert.Equal(2, PartyReward.Scale(1, 1.5f, 1f));
        Assert.Equal(3, PartyReward.Scale(3, 1f, 1f));
        Assert.Equal(0, PartyReward.Scale(0, 2f, 0.9f));
    }

    [Fact]
    public void Every_item_scales()
    {
        List<(string item, int amount)> rewards = [("Coins", 1000), ("Ruby", 3)];
        PartyReward.ScaleAll(rewards, 2f, () => 0f);
        Assert.Equal([("Coins", 2000), ("Ruby", 6)], rewards);
    }
}
