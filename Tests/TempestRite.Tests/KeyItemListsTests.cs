using System.Collections.Generic;
using JoksterCube.TempestRite.Domain;
using Xunit;

namespace JoksterCube.TempestRite.Tests;

public class KeyItemListsTests
{
    [Fact]
    public void One_item_stays_one() => Assert.Equal(["SurtlingCore"], KeyItemLists.Parse("SurtlingCore"));

    [Fact]
    public void Lists_are_trimmed_and_repeats_left_out() =>
        Assert.Equal(["Ruby", "Amber", "Coins"], KeyItemLists.Parse(" Ruby , Amber;;Ruby,  ,Coins "));

    [Fact]
    public void Empty_lists_nothing()
    {
        Assert.Empty(KeyItemLists.Parse(""));
        Assert.Empty(KeyItemLists.Parse(null));
        Assert.Empty(KeyItemLists.Parse(" , ; "));
    }

    [Fact]
    public void An_item_two_rites_list_goes_to_the_first()
    {
        Dictionary<string, WeatherKind> owners = [];
        List<KeyItemLists.Conflict> conflicts = [];
        Dictionary<WeatherKind, List<string>> lists = KeyItemLists.Resolve(
            [(WeatherKind.Clear, "SurtlingCore, Ruby"), (WeatherKind.Rain, "Ooze, Ruby"), (WeatherKind.Mist, "Ruby")], owners, conflicts);

        Assert.Equal(["SurtlingCore", "Ruby"], lists[WeatherKind.Clear]);
        Assert.Equal(["Ooze"], lists[WeatherKind.Rain]);
        Assert.Empty(lists[WeatherKind.Mist]);
        Assert.Equal(WeatherKind.Clear, owners["Ruby"]);
        Assert.Equal(WeatherKind.Rain, owners["Ooze"]);
        Assert.Equal(2, conflicts.Count);
        Assert.All(conflicts, c => Assert.Equal(WeatherKind.Clear, c.Kept));
        Assert.Equal(WeatherKind.Rain, conflicts[0].Dropped);
        Assert.Equal(WeatherKind.Mist, conflicts[1].Dropped);
    }
}
