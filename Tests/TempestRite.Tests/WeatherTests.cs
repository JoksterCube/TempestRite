using System.Collections.Generic;
using JoksterCube.TempestRite.Domain;
using Xunit;

namespace JoksterCube.TempestRite.Tests;

public class KeyItemPickerTests
{
    [Fact]
    public void Nothing_to_pick() =>
        Assert.Equal(WeatherKind.None, KeyItemPicker.Pick(new List<(WeatherKind, int, int)>()));

    [Fact]
    public void Single_item_decides() =>
        Assert.Equal(WeatherKind.Rain, KeyItemPicker.Pick([(WeatherKind.Rain, 2, 3)]));

    [Fact]
    public void Bottom_row_wins_over_further_left() =>
        Assert.Equal(WeatherKind.Clear, KeyItemPicker.Pick([(WeatherKind.Thunderstorm, 0, 1), (WeatherKind.Clear, 7, 2)]));

    [Fact]
    public void Lowest_wins_in_any_row() =>
        // Copper ore in row 2, surtling core in row 1 (rows count from 0): the copper ore decides, even far right.
        Assert.Equal(WeatherKind.Thunderstorm, KeyItemPicker.Pick([(WeatherKind.Thunderstorm, 7, 2), (WeatherKind.Clear, 0, 1)]));

    [Fact]
    public void Leftmost_wins_within_a_row() =>
        Assert.Equal(WeatherKind.Clear, KeyItemPicker.Pick([(WeatherKind.Thunderstorm, 4, 0), (WeatherKind.Clear, 1, 0), (WeatherKind.Rain, 2, 0)]));

    [Fact]
    public void Order_in_the_list_does_not_matter()
    {
        var items = new List<(WeatherKind, int, int)> { (WeatherKind.Rain, 3, 0), (WeatherKind.Clear, 5, 1), (WeatherKind.Thunderstorm, 0, 1) };
        Assert.Equal(WeatherKind.Thunderstorm, KeyItemPicker.Pick(items));
        items.Reverse();
        Assert.Equal(WeatherKind.Thunderstorm, KeyItemPicker.Pick(items));
    }

    [Fact]
    public void First_ctrl_click_slot_wins() =>
        // Materials moved in go to the bottom row from the left: (0, 2) in a 3-row obliterator.
        Assert.Equal(WeatherKind.Rain, KeyItemPicker.Pick([(WeatherKind.Clear, 1, 2), (WeatherKind.Rain, 0, 2), (WeatherKind.Mist, 0, 0)]));

    [Fact]
    public void Ignores_items_that_call_nothing() =>
        Assert.Equal(1, KeyItemPicker.PickIndex([(WeatherKind.None, 9, 0), (WeatherKind.Rain, 0, 3)]));
}

public class EnvironmentTableTests
{
    private const int Meadows = 1, Swamp = 2, Mountain = 4, BlackForest = 8, Ocean = 256;

    [Fact]
    public void Reads_default_and_biomes()
    {
        EnvironmentTable table = EnvironmentTable.Parse("Default: Rain, Swamp: SwampRain, Mountain: Snow");
        Assert.Equal("Rain", table.Default);
        Assert.Equal("SwampRain", table.For(Swamp));
        Assert.Equal("Snow", table.For(Mountain));
        Assert.Equal("Rain", table.For(Meadows));
        Assert.Equal("Rain", table.For(Ocean));
        Assert.Empty(table.Problems);
    }

    [Fact]
    public void Keeps_spaces_inside_environment_names() =>
        Assert.Equal("DeepForest Mist", EnvironmentTable.Parse("Default: Misty, BlackForest: DeepForest Mist").For(BlackForest));

    [Fact]
    public void Biome_names_ignore_case_and_spaces() =>
        Assert.Equal("Misty", EnvironmentTable.Parse("black forest: Misty").For(BlackForest));

    [Fact]
    public void Reports_what_it_cannot_read()
    {
        EnvironmentTable table = EnvironmentTable.Parse("Default: Clear, Narnia: Snow, Swamp, Mountain:");
        Assert.Equal("Clear", table.Default);
        Assert.Equal(3, table.Problems.Count);
    }

    [Fact]
    public void Empty_setting_gives_no_environment() => Assert.Equal(string.Empty, EnvironmentTable.Parse("").For(Meadows));
}

public class WeatherKindTests
{
    [Fact]
    public void Each_effect_has_its_slot()
    {
        Assert.Equal(EffectSlot.Weather, WeatherKinds.Slot(WeatherKind.Clear));
        Assert.Equal(EffectSlot.Weather, WeatherKinds.Slot(WeatherKind.Thunderstorm));
        Assert.Equal(EffectSlot.Weather, WeatherKinds.Slot(WeatherKind.Rain));
        Assert.Equal(EffectSlot.Weather, WeatherKinds.Slot(WeatherKind.Mist));
        Assert.Equal(EffectSlot.Daylight, WeatherKinds.Slot(WeatherKind.PolarDay));
        Assert.Equal(EffectSlot.Daylight, WeatherKinds.Slot(WeatherKind.PolarNight));
        Assert.Equal(EffectSlot.Mistlands, WeatherKinds.Slot(WeatherKind.NoMist));
        // Sootfall has its own slot, so it can go on together with True Sight.
        Assert.Equal(EffectSlot.Ashlands, WeatherKinds.Slot(WeatherKind.Sootfall));
        Assert.True(WeatherKinds.Lasts(WeatherKind.Sootfall));
    }

    [Fact]
    public void Network_numbers_stay_the_same()
    {
        Assert.Equal(1, (int)WeatherKind.Clear);
        Assert.Equal(4, (int)WeatherKind.Mist);
        Assert.Equal(7, (int)WeatherKind.NoMist);
        Assert.Equal(8, (int)WeatherKind.Reset);
        Assert.Equal(9, (int)WeatherKind.Sootfall);
        Assert.Equal(17, (int)WeatherKind.Carrion);
        Assert.Equal(18, (int)WeatherKind.Grounding);
        Assert.Equal(19, (int)WeatherKind.VatBoil);
        Assert.Equal(26, (int)WeatherKind.AbyssalSurge);
        Assert.Equal(30, (int)WeatherKind.Unmaking);
        Assert.False(WeatherKinds.IsKnown((WeatherKind)31));
        Assert.False(WeatherKinds.Lasts(WeatherKind.Unmaking));
        Assert.True(WeatherKinds.Personal(WeatherKind.Luminary));
        Assert.True(WeatherKinds.Personal(WeatherKind.AbyssalDraw));
        Assert.True(WeatherKinds.IsDomainSlot(WeatherKinds.Slot(WeatherKind.SovereignWard)));
        Assert.False(WeatherKinds.Lasts(WeatherKind.Grounding));
        Assert.False(WeatherKinds.Lasts(WeatherKind.Reset));
        Assert.False(WeatherKinds.Lasts(WeatherKind.Sanctuary));
    }
}

public class WeatherStoreTests
{
    [Fact]
    public void Saves_and_reads_back()
    {
        var entries = new List<WeatherState.Entry>
        {
            new(WeatherKind.Rain, 1234.5f, 1800f, "Testeroo"),
            new(WeatherKind.PolarNight, 60f, 600f, "Some; one")
        };
        List<WeatherState.Entry> read = WeatherStore.Parse(WeatherStore.Format(entries));
        Assert.Equal(2, read.Count);
        Assert.Equal(WeatherKind.Rain, read[0].Kind);
        Assert.Equal(1234.5f, read[0].Remaining);
        Assert.Equal(1800f, read[0].Duration);
        Assert.Equal("Testeroo", read[0].CalledBy);
        Assert.Equal("Some; one", read[1].CalledBy);
    }

    [Fact]
    public void Keeps_where_a_rite_was_performed()
    {
        var entries = new List<WeatherState.Entry> { new(WeatherKind.Verdant, 600f, 1800f, "Grower", new UnityEngine.Vector3(12.5f, 30f, -400.25f)) };
        WeatherState.Entry read = WeatherStore.Parse(WeatherStore.Format(entries))[0];
        Assert.Equal(12.5f, read.Position.x);
        Assert.Equal(30f, read.Position.y);
        Assert.Equal(-400.25f, read.Position.z);
        Assert.Equal("Grower", read.CalledBy);
    }

    [Fact]
    public void Reads_lines_saved_before_rites_had_a_place()
    {
        WeatherState.Entry read = WeatherStore.Parse("3;100;1800;Old Timer")[0];
        Assert.Equal(WeatherKind.Rain, read.Kind);
        Assert.Equal("Old Timer", read.CalledBy);
    }

    [Fact]
    public void Skips_bad_and_finished_lines() =>
        Assert.Empty(WeatherStore.Parse("x;1;2;a\n99;100;100;b\n3;0;100;c\n\n3;abc;100;d"));
}

public class RiteColorTests
{
    [Fact]
    public void Haze_rites_share_a_color() => Assert.Equal(WeatherKinds.ColorHex(WeatherKind.NoMist), WeatherKinds.ColorHex(WeatherKind.Sootfall));

    [Fact]
    public void Groups_have_their_own_colors()
    {
        Assert.NotEqual(WeatherKinds.ColorHex(WeatherKind.Rain), WeatherKinds.ColorHex(WeatherKind.PolarDay));
        Assert.NotEqual(WeatherKinds.ColorHex(WeatherKind.Rain), WeatherKinds.ColorHex(WeatherKind.Sootfall));
        Assert.Equal(WeatherKinds.ColorHex(WeatherKind.PolarDay), WeatherKinds.ColorHex(WeatherKind.PolarNight));
    }
}

public class RiteGroupTests
{
    [Fact]
    public void Every_rite_is_on_a_tab()
    {
        foreach (WeatherKind kind in WeatherKinds.All)
            Assert.True(System.Array.IndexOf(WeatherKinds.Groups, WeatherKinds.Group(kind)) >= 0, kind.ToString());
    }

    [Fact]
    public void Groups_hold_their_rites()
    {
        Assert.Equal(RiteGroup.Atmospheric, WeatherKinds.Group(WeatherKind.Aurora));
        Assert.Equal(RiteGroup.Celestial, WeatherKinds.Group(WeatherKind.Tailwind));
        Assert.Equal(RiteGroup.BiomeMastery, WeatherKinds.Group(WeatherKind.AbyssalSurge));
        Assert.Equal(RiteGroup.Domain, WeatherKinds.Group(WeatherKind.Verdant));
        Assert.Equal(RiteGroup.Domain, WeatherKinds.Group(WeatherKind.VatBoil));
        Assert.Equal(RiteGroup.Domain, WeatherKinds.Group(WeatherKind.AmberMend));
        Assert.Equal(RiteGroup.Blessings, WeatherKinds.Group(WeatherKind.CryptDelver));
        Assert.Equal(RiteGroup.Blessings, WeatherKinds.Group(WeatherKind.FortunesBoon));
        Assert.Equal(RiteGroup.Incursions, WeatherKinds.Group(WeatherKind.Sanctuary));
        Assert.Equal(RiteGroup.Incursions, WeatherKinds.Group(WeatherKind.Reset));
    }

    [Fact]
    public void Located_rites()
    {
        Assert.True(WeatherKinds.Located(WeatherKind.Packward));
        Assert.True(WeatherKinds.Located(WeatherKind.Crimson));
        Assert.True(WeatherKinds.Located(WeatherKind.VatBoil));
        Assert.True(WeatherKinds.Located(WeatherKind.AmberMend));
        Assert.False(WeatherKinds.Located(WeatherKind.Rain));
        Assert.False(WeatherKinds.Located(WeatherKind.AbyssalSurge));
    }

    [Fact]
    public void Blessings_last_but_have_no_shared_slot()
    {
        foreach (WeatherKind kind in WeatherKinds.All)
        {
            bool personal = WeatherKinds.Group(kind) == RiteGroup.Blessings;
            Assert.Equal(personal, WeatherKinds.Personal(kind));
            if (!personal) continue;
            Assert.True(WeatherKinds.Lasts(kind), kind.ToString());
            Assert.False(WeatherKinds.HasSlot(kind), kind.ToString());
            Assert.False(WeatherKinds.Located(kind), kind.ToString());
        }
        Assert.True(WeatherKinds.HasSlot(WeatherKind.Rain));
        Assert.False(WeatherKinds.HasSlot(WeatherKind.Reset));
    }

    [Fact]
    public void Slots_are_listed_in_the_order_of_their_numbers()
    {
        for (int i = 0; i < WeatherKinds.Slots.Length; i++) Assert.Equal(i, (int)WeatherKinds.Slots[i]);
        foreach (WeatherKind kind in WeatherKinds.All)
            if (WeatherKinds.HasSlot(kind))
                Assert.True(System.Array.IndexOf(WeatherKinds.Slots, WeatherKinds.Slot(kind)) >= 0, kind.ToString());
    }

    [Fact]
    public void Each_lasting_rite_has_its_own_slot_but_weather_and_daylight()
    {
        Assert.Equal(EffectSlot.Weather, WeatherKinds.Slot(WeatherKind.Aurora));
        Assert.Equal(EffectSlot.Daylight, WeatherKinds.Slot(WeatherKind.PolarNight));
        Assert.Equal(EffectSlot.Wind, WeatherKinds.Slot(WeatherKind.Tailwind));
        // Packward and Carrion share a slot: one replaces the other.
        Assert.Equal(WeatherKinds.Slot(WeatherKind.Packward), WeatherKinds.Slot(WeatherKind.Carrion));
    }

    [Fact]
    public void Only_the_domain_slots_are_domain()
    {
        Assert.True(WeatherKinds.IsDomainSlot(WeatherKinds.Slot(WeatherKind.Verdant)));
        Assert.True(WeatherKinds.IsDomainSlot(WeatherKinds.Slot(WeatherKind.Carrion)));
        Assert.True(WeatherKinds.IsDomainSlot(WeatherKinds.Slot(WeatherKind.VatBoil)));
        Assert.True(WeatherKinds.IsDomainSlot(WeatherKinds.Slot(WeatherKind.AmberMend)));
        Assert.False(WeatherKinds.IsDomainSlot(WeatherKinds.Slot(WeatherKind.AbyssalSurge)));
        Assert.False(WeatherKinds.IsDomainSlot(WeatherKinds.Slot(WeatherKind.Rain)));
        Assert.False(WeatherKinds.IsDomainSlot(WeatherKinds.Slot(WeatherKind.Crimson)));
        Assert.Equal(RiteGroup.Domain, WeatherKinds.Group(WeatherKind.Grounding));
    }
}

public class TimeTextTests
{
    [Theory]
    [InlineData(0.2f, "1 s")]
    [InlineData(45f, "45 s")]
    [InlineData(59.5f, "60 s")]
    [InlineData(60f, "1 min")]
    [InlineData(61f, "2 min")]
    [InlineData(1800f, "30 min")]
    [InlineData(1770.5f, "30 min")]
    [InlineData(3600f, "1 h")]
    [InlineData(5400f, "1 h 30 min")]
    [InlineData(36000f, "10 h")]
    public void Formats_durations(float seconds, string expected) => Assert.Equal(expected, TimeText.Duration(seconds));
}

public class WeatherPermissionTests
{
    [Theory]
    // 0: Anyone, 1: AdminsOnly (the enum is internal, so theory data passes its value).
    [InlineData(0, false, true)]
    [InlineData(0, true, true)]
    [InlineData(1, false, false)]
    [InlineData(1, true, true)]
    public void Checks_who_may_change(int permission, bool isAdmin, bool allowed) =>
        Assert.Equal(allowed, WeatherPermissions.Allowed((WeatherPermission)permission, isAdmin));
}

public class WeatherBiomesTests
{
    private const WeatherBiomes Default = WeatherBiomes.Meadows | WeatherBiomes.BlackForest | WeatherBiomes.Swamp |
        WeatherBiomes.Mountain | WeatherBiomes.Plains | WeatherBiomes.Mistlands | WeatherBiomes.Ocean;

    [Fact]
    public void Includes_listed_biomes_by_game_value()
    {
        Assert.True(Default.Includes(256));  // Ocean
        Assert.True(Default.Includes(1));    // Meadows
        Assert.False(Default.Includes(32));  // Ashlands
        Assert.False(Default.Includes(64));  // Deep North
    }

    [Fact]
    public void Unknown_biome_counts_as_included() => Assert.True(WeatherBiomes.None.Includes(0));

    [Fact]
    public void Names_biomes_left_out() => Assert.Equal("Ashlands, Deep North", Default.LeftOut());

    [Fact]
    public void Nothing_left_out() => Assert.Equal(string.Empty, (Default | WeatherBiomes.AshLands | WeatherBiomes.DeepNorth).LeftOut());

    [Fact]
    public void Kinds_have_names()
    {
        foreach (WeatherKind kind in WeatherKinds.All)
        {
            Assert.NotEmpty(WeatherKinds.Name(kind));
            Assert.NotEmpty(WeatherKinds.Plain(kind));
        }
    }
}
