using System.Collections.Generic;
using JoksterCube.TempestRite.Domain;
using Xunit;

namespace JoksterCube.TempestRite.Tests;

public class BossRosterTests
{
    private static readonly List<string> Keys =
    [
        "defeated_eikthyr", "defeated_gdking", "defeated_bonemass", "defeated_dragon", "defeated_goblinking",
        "defeated_queen", "defeated_fader"
    ];

    [Fact]
    public void Bonemass_counts_eikthyr_and_the_elder_as_slain()
    {
        HashSet<string> slain = ["defeated_bonemass"];
        BossRoster.FillEarly(Keys, slain);
        Assert.Equal(["defeated_bonemass", "defeated_eikthyr", "defeated_gdking"], slain);
    }

    [Fact]
    public void A_stronger_boss_counts_the_early_ones_too()
    {
        HashSet<string> slain = ["defeated_dragon"];
        BossRoster.FillEarly(Keys, slain);
        Assert.True(BossRoster.PastEarly(Keys, slain));
        Assert.Contains("defeated_eikthyr", slain);
        Assert.Contains("defeated_gdking", slain);
        Assert.Contains("defeated_bonemass", slain);
        // Only the early bosses are filled in, not those between them and the one slain.
        Assert.Equal(4, slain.Count);
    }

    [Fact]
    public void Before_bonemass_nothing_is_filled_in()
    {
        HashSet<string> slain = ["defeated_eikthyr", "defeated_gdking"];
        BossRoster.FillEarly(Keys, slain);
        Assert.False(BossRoster.PastEarly(Keys, slain));
        Assert.Equal(2, slain.Count);
    }

    [Fact]
    public void With_fewer_bosses_the_strongest_is_the_last_early_one()
    {
        List<string> two = ["defeated_eikthyr", "defeated_gdking"];
        Assert.True(BossRoster.PastEarly(two, new HashSet<string> { "defeated_gdking" }));
        Assert.False(BossRoster.PastEarly(two, new HashSet<string> { "defeated_eikthyr" }));
        Assert.False(BossRoster.PastEarly([], new HashSet<string> { "defeated_eikthyr" }));
    }
}
