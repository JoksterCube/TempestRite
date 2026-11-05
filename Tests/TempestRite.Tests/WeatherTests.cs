using System.Linq;
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

    [Fact]
    public void Nothing_to_pick_all() =>
        Assert.Empty(KeyItemPicker.PickAll(new List<(WeatherKind, int, int)>()));

    [Fact]
    public void Rites_in_different_slots_all_go_in_order() =>
        // Rain (weather), Sun Zenith (daylight), Verdant (domain), two blessings and the Unmaking Rite: none clash.
        Assert.Equal(
            [WeatherKind.Rain, WeatherKind.PolarDay, WeatherKind.Verdant, WeatherKind.CryptDelver, WeatherKind.FortunesBoon, WeatherKind.Unmaking],
            KeyItemPicker.PickAll([
                (WeatherKind.FortunesBoon, 1, 0), (WeatherKind.Unmaking, 2, 0), (WeatherKind.Verdant, 2, 1),
                (WeatherKind.PolarDay, 1, 2), (WeatherKind.Rain, 0, 2), (WeatherKind.CryptDelver, 0, 0)
            ]));

    [Fact]
    public void Same_slot_keeps_the_first() =>
        // Thunderstorm is lower than Clear: Clear is left out, Sun Zenith still goes.
        Assert.Equal([WeatherKind.Thunderstorm, WeatherKind.PolarDay],
            KeyItemPicker.PickAll([(WeatherKind.Clear, 0, 1), (WeatherKind.Thunderstorm, 5, 2), (WeatherKind.PolarDay, 3, 1)]));

    [Fact]
    public void Packward_and_Carrion_share_a_slot() =>
        Assert.Equal([WeatherKind.Carrion, WeatherKind.Verdant],
            KeyItemPicker.PickAll([(WeatherKind.Packward, 3, 0), (WeatherKind.Carrion, 0, 1), (WeatherKind.Verdant, 1, 0)]));

    [Fact]
    public void A_rite_appears_once_however_many_stacks() =>
        Assert.Equal([WeatherKind.Rain], KeyItemPicker.PickAll([(WeatherKind.Rain, 3, 0), (WeatherKind.Rain, 0, 2), (WeatherKind.Rain, 1, 1)]));

    [Fact]
    public void Equilibrium_first_keeps_out_what_it_ends() =>
        // Equilibrium goes first: Rain is left out, the Domain and blessing rites still go.
        Assert.Equal([WeatherKind.Reset, WeatherKind.Verdant, WeatherKind.GraveBind],
            KeyItemPicker.PickAll([(WeatherKind.Rain, 1, 1), (WeatherKind.Reset, 0, 2), (WeatherKind.Verdant, 2, 1), (WeatherKind.GraveBind, 0, 0)]));

    [Fact]
    public void Rite_first_keeps_out_what_would_end_it() =>
        // Sanctuary would end the Crimson Rite; the Equilibrium Rite goes and keeps the Shrouding Rite out.
        Assert.Equal([WeatherKind.Crimson, WeatherKind.Reset],
            KeyItemPicker.PickAll([(WeatherKind.Crimson, 0, 2), (WeatherKind.Sanctuary, 1, 2), (WeatherKind.Reset, 2, 2), (WeatherKind.Mist, 0, 0)]));

    [Fact]
    public void Incursion_first_keeps_out_domain_rites() =>
        // Ragnarök goes first: Verdant and Packward are left out, the weather and the blessing still go.
        Assert.Equal([WeatherKind.Ragnarok, WeatherKind.Rain, WeatherKind.GraveBind],
            KeyItemPicker.PickAll([(WeatherKind.Verdant, 1, 1), (WeatherKind.Ragnarok, 0, 2), (WeatherKind.Rain, 1, 2),
                (WeatherKind.Packward, 2, 0), (WeatherKind.GraveBind, 0, 0)]));

    [Fact]
    public void Domain_rite_first_keeps_out_the_incursion() =>
        Assert.Equal([WeatherKind.Verdant, WeatherKind.Packward],
            KeyItemPicker.PickAll([(WeatherKind.Verdant, 0, 2), (WeatherKind.Crimson, 1, 2), (WeatherKind.Packward, 2, 2)]));

    [Fact]
    public void Grounding_clashes_only_with_domain_rites() =>
        Assert.Equal([WeatherKind.Grounding, WeatherKind.Rain],
            KeyItemPicker.PickAll([(WeatherKind.Grounding, 0, 2), (WeatherKind.Packward, 1, 2), (WeatherKind.Rain, 2, 2)]));
}

public class RiteClashTests
{
    [Theory]
    [InlineData((int)WeatherKind.Rain, (int)WeatherKind.Rain)]
    [InlineData((int)WeatherKind.Rain, (int)WeatherKind.Clear)]
    [InlineData((int)WeatherKind.PolarDay, (int)WeatherKind.PolarNight)]
    [InlineData((int)WeatherKind.Packward, (int)WeatherKind.Carrion)]
    [InlineData((int)WeatherKind.Reset, (int)WeatherKind.Rain)]
    [InlineData((int)WeatherKind.Reset, (int)WeatherKind.Aurora)]
    [InlineData((int)WeatherKind.SunFall, (int)WeatherKind.PolarDay)]
    [InlineData((int)WeatherKind.SunFall, (int)WeatherKind.GaleBreath)]
    [InlineData((int)WeatherKind.NaturesRebound, (int)WeatherKind.NoMist)]
    [InlineData((int)WeatherKind.NaturesRebound, (int)WeatherKind.AbyssalSurge)]
    [InlineData((int)WeatherKind.GaleBreath, (int)WeatherKind.GaleBreath)]
    [InlineData((int)WeatherKind.Grounding, (int)WeatherKind.SovereignWard)]
    [InlineData((int)WeatherKind.Sanctuary, (int)WeatherKind.Crimson)]
    [InlineData((int)WeatherKind.Sanctuary, (int)WeatherKind.Ragnarok)]
    [InlineData((int)WeatherKind.Crimson, (int)WeatherKind.Ragnarok)]
    [InlineData((int)WeatherKind.Crimson, (int)WeatherKind.Verdant)]
    [InlineData((int)WeatherKind.Crimson, (int)WeatherKind.SovereignWard)]
    [InlineData((int)WeatherKind.Ragnarok, (int)WeatherKind.Packward)]
    [InlineData((int)WeatherKind.Ragnarok, (int)WeatherKind.PitchBellows)]
    [InlineData((int)WeatherKind.CryptDelver, (int)WeatherKind.CryptDelver)]
    public void Clash(int a, int b)
    {
        Assert.True(WeatherKinds.Clashes((WeatherKind)a, (WeatherKind)b));
        Assert.True(WeatherKinds.Clashes((WeatherKind)b, (WeatherKind)a));
    }

    [Theory]
    [InlineData((int)WeatherKind.Rain, (int)WeatherKind.PolarNight)]
    [InlineData((int)WeatherKind.Aurora, (int)WeatherKind.GaleBreath)]
    [InlineData((int)WeatherKind.PolarDay, (int)WeatherKind.GaleBreath)]
    [InlineData((int)WeatherKind.FairWind, (int)WeatherKind.GaleBreath)]
    [InlineData((int)WeatherKind.Reset, (int)WeatherKind.Verdant)]
    [InlineData((int)WeatherKind.Reset, (int)WeatherKind.Crimson)]
    [InlineData((int)WeatherKind.Reset, (int)WeatherKind.Ragnarok)]
    [InlineData((int)WeatherKind.Reset, (int)WeatherKind.PolarDay)]
    [InlineData((int)WeatherKind.Reset, (int)WeatherKind.NoMist)]
    [InlineData((int)WeatherKind.SunFall, (int)WeatherKind.Rain)]
    [InlineData((int)WeatherKind.SunFall, (int)WeatherKind.Sootfall)]
    [InlineData((int)WeatherKind.SunFall, (int)WeatherKind.FairWind)]
    [InlineData((int)WeatherKind.NaturesRebound, (int)WeatherKind.Mist)]
    [InlineData((int)WeatherKind.NaturesRebound, (int)WeatherKind.AetherClear)]
    [InlineData((int)WeatherKind.NaturesRebound, (int)WeatherKind.SunFall)]
    [InlineData((int)WeatherKind.Reset, (int)WeatherKind.SunFall)]
    [InlineData((int)WeatherKind.Reset, (int)WeatherKind.CryptDelver)]
    [InlineData((int)WeatherKind.Reset, (int)WeatherKind.FairWind)]
    [InlineData((int)WeatherKind.Reset, (int)WeatherKind.Sanctuary)]
    [InlineData((int)WeatherKind.Reset, (int)WeatherKind.Grounding)]
    [InlineData((int)WeatherKind.Grounding, (int)WeatherKind.Crimson)]
    [InlineData((int)WeatherKind.Grounding, (int)WeatherKind.Ragnarok)]
    [InlineData((int)WeatherKind.Crimson, (int)WeatherKind.Rain)]
    [InlineData((int)WeatherKind.Crimson, (int)WeatherKind.CryptDelver)]
    [InlineData((int)WeatherKind.Ragnarok, (int)WeatherKind.Skal)]
    [InlineData((int)WeatherKind.Ragnarok, (int)WeatherKind.Grounding)]
    [InlineData((int)WeatherKind.Sanctuary, (int)WeatherKind.Rain)]
    [InlineData((int)WeatherKind.CryptDelver, (int)WeatherKind.FortunesBoon)]
    [InlineData((int)WeatherKind.Unmaking, (int)WeatherKind.Reset)]
    [InlineData((int)WeatherKind.Verdant, (int)WeatherKind.VatBoil)]
    public void No_clash(int a, int b)
    {
        Assert.False(WeatherKinds.Clashes((WeatherKind)a, (WeatherKind)b));
        Assert.False(WeatherKinds.Clashes((WeatherKind)b, (WeatherKind)a));
    }
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
    public void None_keeps_the_biomes_own_weather()
    {
        EnvironmentTable table = EnvironmentTable.Parse("Default: Clear, Swamp: none");
        Assert.True(EnvironmentTable.KeepsOwn(table.For(Swamp)));
        Assert.False(EnvironmentTable.KeepsOwn(table.For(Meadows)));
        Assert.Empty(table.Problems);
    }

    [Fact]
    public void Empty_setting_gives_no_environment() => Assert.Equal(string.Empty, EnvironmentTable.Parse("").For(Meadows));
}

public class OldDefaultsTests
{
    [Fact]
    public void Reads_the_version_that_saved_the_file()
    {
        Assert.Equal(new System.Version(1, 1, 3), JoksterCube.TempestRite.Settings.OldDefaults.ParseHeader("## Settings file was created by plugin TempestRite v1.1.3"));
        Assert.Null(JoksterCube.TempestRite.Settings.OldDefaults.ParseHeader("[0 - General]"));
        Assert.Null(JoksterCube.TempestRite.Settings.OldDefaults.ParseHeader(null));
    }

    [Fact]
    public void Applies_only_to_files_saved_before_the_change()
    {
        Assert.True(JoksterCube.TempestRite.Settings.OldDefaults.Applies(new System.Version(1, 1, 3), "1.1.4"));
        Assert.False(JoksterCube.TempestRite.Settings.OldDefaults.Applies(new System.Version(1, 1, 4), "1.1.4"));
        Assert.False(JoksterCube.TempestRite.Settings.OldDefaults.Applies(null, "1.1.4"));
    }

    [Fact]
    public void Matches_lists_whatever_their_spacing()
    {
        Assert.True(JoksterCube.TempestRite.Settings.OldDefaults.Same("LeatherScraps,DeerHide", "LeatherScraps, DeerHide"));
        Assert.False(JoksterCube.TempestRite.Settings.OldDefaults.Same("DeerHide, LeatherScraps", "LeatherScraps, DeerHide"));
        Assert.True(JoksterCube.TempestRite.Settings.OldDefaults.Same(20f, 20f));
    }
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
        Assert.Equal(EffectSlot.Wind, WeatherKinds.Slot(WeatherKind.GaleBreath));
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
        Assert.Equal(31, (int)WeatherKind.Skal);
        Assert.Equal(32, (int)WeatherKind.SkyCall);
        Assert.Equal(33, (int)WeatherKind.Ragnarok);
        Assert.Equal(34, (int)WeatherKind.AnvilSpark);
        Assert.Equal(35, (int)WeatherKind.GaleBreath);
        Assert.Equal(11, (int)EffectSlot.Wind);
        Assert.Equal(36, (int)WeatherKind.PitchBellows);
        Assert.Equal(12, (int)EffectSlot.Smelting);
        Assert.Equal(37, (int)WeatherKind.Kindred);
        Assert.Equal(13, (int)EffectSlot.Breeding);
        Assert.Equal(38, (int)WeatherKind.HoardBane);
        Assert.Equal(EffectSlot.Incursion, WeatherKinds.Slot(WeatherKind.HoardBane));
        Assert.True(WeatherKinds.Untimed(WeatherKind.HoardBane));
        Assert.Equal(39, (int)WeatherKind.HushFelling);
        Assert.True(WeatherKinds.Personal(WeatherKind.HushFelling));
        Assert.Equal(40, (int)WeatherKind.HarrowSpur);
        Assert.Equal(14, (int)EffectSlot.Spawners);
        Assert.Equal(EffectSlot.Spawners, WeatherKinds.Slot(WeatherKind.HarrowSpur));
        Assert.Equal(41, (int)WeatherKind.DraughtFlush);
        Assert.True(WeatherKinds.Personal(WeatherKind.DraughtFlush));
        Assert.True(WeatherKinds.Lasts(WeatherKind.DraughtFlush));
        Assert.False(WeatherKinds.HasSlot(WeatherKind.DraughtFlush));
        Assert.Equal(42, (int)WeatherKind.SoulTether);
        Assert.True(WeatherKinds.Personal(WeatherKind.SoulTether));
        Assert.True(WeatherKinds.InstantBlessing(WeatherKind.SoulTether));
        Assert.False(WeatherKinds.Lasts(WeatherKind.SoulTether));
        Assert.False(WeatherKinds.HasSlot(WeatherKind.SoulTether));
        Assert.False(WeatherKinds.InstantBlessing(WeatherKind.DraughtFlush));
        Assert.Equal(43, (int)WeatherKind.HearthHaul);
        Assert.Equal(15, (int)EffectSlot.Carrying);
        Assert.Equal(EffectSlot.Carrying, WeatherKinds.Slot(WeatherKind.HearthHaul));
        Assert.Equal(RiteGroup.Domain, WeatherKinds.Group(WeatherKind.HearthHaul));
        Assert.True(WeatherKinds.Located(WeatherKind.HearthHaul));
        Assert.True(WeatherKinds.Lasts(WeatherKind.HearthHaul));
        Assert.True(WeatherKinds.IsDomainSlot(WeatherKinds.Slot(WeatherKind.HearthHaul)));
        Assert.True(WeatherKinds.Ends(WeatherKind.Grounding, WeatherKind.HearthHaul));
        Assert.False(WeatherKinds.Ends(WeatherKind.Reset, WeatherKind.HearthHaul));
        Assert.True(WeatherKinds.Ends(WeatherKind.Crimson, WeatherKind.HearthHaul));
        Assert.Equal(44, (int)WeatherKind.Eldhrimnir);
        Assert.Equal(RiteGroup.Kinship, WeatherKinds.Group(WeatherKind.Eldhrimnir));
        Assert.Equal(45, (int)WeatherKind.RallyHorn);
        Assert.Equal(RiteGroup.Kinship, WeatherKinds.Group(WeatherKind.RallyHorn));
        Assert.Equal(46, (int)WeatherKind.TundraShroud);
        Assert.Equal(RiteGroup.Blessings, WeatherKinds.Group(WeatherKind.TundraShroud));
        Assert.True(WeatherKinds.Personal(WeatherKind.TundraShroud));
        Assert.True(WeatherKinds.Lasts(WeatherKind.TundraShroud));
        Assert.False(WeatherKinds.HasSlot(WeatherKind.TundraShroud));
        Assert.False(WeatherKinds.Located(WeatherKind.TundraShroud));
        Assert.False(WeatherKinds.InstantBlessing(WeatherKind.TundraShroud));
        Assert.Equal(47, (int)WeatherKind.AetherClear);
        Assert.Equal(16, (int)EffectSlot.Clearing);
        Assert.Equal(EffectSlot.Clearing, WeatherKinds.Slot(WeatherKind.AetherClear));
        Assert.Equal(RiteGroup.Domain, WeatherKinds.Group(WeatherKind.AetherClear));
        Assert.True(WeatherKinds.Located(WeatherKind.AetherClear));
        Assert.True(WeatherKinds.Lasts(WeatherKind.AetherClear));
        Assert.True(WeatherKinds.Ends(WeatherKind.Grounding, WeatherKind.AetherClear));
        Assert.False(WeatherKinds.Ends(WeatherKind.Reset, WeatherKind.AetherClear));
        // Its own slot: it goes on together with the True Sight Rite.
        Assert.False(WeatherKinds.Clashes(WeatherKind.NoMist, WeatherKind.AetherClear));
        // The Elder-Sprout Rite: a Domain rite in its own slot, around its obliterator, ended by the Grounding Rite.
        Assert.Equal(48, (int)WeatherKind.ElderSprout);
        Assert.Equal(17, (int)EffectSlot.Sprouting);
        Assert.Equal(RiteGroup.Domain, WeatherKinds.Group(WeatherKind.ElderSprout));
        Assert.Equal(EffectSlot.Sprouting, WeatherKinds.Slot(WeatherKind.ElderSprout));
        Assert.True(WeatherKinds.Lasts(WeatherKind.ElderSprout));
        Assert.True(WeatherKinds.HasSlot(WeatherKind.ElderSprout));
        Assert.True(WeatherKinds.Located(WeatherKind.ElderSprout));
        Assert.True(WeatherKinds.IsDomainSlot(EffectSlot.Sprouting));
        Assert.Contains(EffectSlot.Sprouting, WeatherKinds.Slots);
        Assert.False(WeatherKinds.Personal(WeatherKind.ElderSprout));
        Assert.False(WeatherKinds.ForEveryone(WeatherKind.ElderSprout));
        Assert.True(WeatherKinds.Ends(WeatherKind.Grounding, WeatherKind.ElderSprout));
        Assert.True(WeatherKinds.Ends(WeatherKind.Crimson, WeatherKind.ElderSprout));
        Assert.False(WeatherKinds.Clashes(WeatherKind.ElderSprout, WeatherKind.Unmaking));
        Assert.False(WeatherKinds.Clashes(WeatherKind.ElderSprout, WeatherKind.Reset));
        Assert.False(WeatherKinds.Clashes(WeatherKind.ElderSprout, WeatherKind.Verdant));
        Assert.Contains(WeatherKind.ElderSprout, WeatherKinds.All);
        // The Sun-Fall and Nature's Rebound Rites: end the celestial and biome mastery rites, at once, with no slot.
        Assert.Equal(49, (int)WeatherKind.SunFall);
        Assert.Equal(50, (int)WeatherKind.NaturesRebound);
        Assert.Equal(RiteGroup.Celestial, WeatherKinds.Group(WeatherKind.SunFall));
        Assert.Equal(RiteGroup.BiomeMastery, WeatherKinds.Group(WeatherKind.NaturesRebound));
        foreach (WeatherKind ender in new[] { WeatherKind.SunFall, WeatherKind.NaturesRebound })
        {
            Assert.False(WeatherKinds.Lasts(ender));
            Assert.False(WeatherKinds.HasSlot(ender));
            Assert.False(WeatherKinds.Personal(ender));
            Assert.False(WeatherKinds.ForEveryone(ender));
            Assert.Contains(ender, WeatherKinds.All);
        }
        Assert.True(WeatherKinds.Ends(WeatherKind.SunFall, WeatherKind.PolarNight));
        Assert.True(WeatherKinds.Ends(WeatherKind.NaturesRebound, WeatherKind.Sootfall));
        Assert.False(WeatherKinds.IsKnown((WeatherKind)64));
        Assert.False(WeatherKinds.Lasts(WeatherKind.Unmaking));
        Assert.True(WeatherKinds.Personal(WeatherKind.Luminary));
        Assert.True(WeatherKinds.Personal(WeatherKind.AbyssalDraw));
        Assert.True(WeatherKinds.Personal(WeatherKind.FairWind));
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

public class RiteHistoryTests
{
    private static readonly UnityEngine.Vector3 Here = new(100f, 30f, 100f);

    private static RiteHistory.Span Span(double start, double end, float duration = 3600f) =>
        new(WeatherKind.Verdant, Here, 40f, start, end, duration);

    [Fact]
    public void Owes_the_rite_time_after_last_sped_up()
    {
        var spans = new List<RiteHistory.Span> { Span(1000, 2800) };
        Assert.Equal(1800, RiteHistory.Owed(spans, WeatherKind.Verdant, Here, double.NegativeInfinity, 5000));
        Assert.Equal(800, RiteHistory.Owed(spans, WeatherKind.Verdant, Here, 2000, 5000));
        Assert.Equal(500, RiteHistory.Owed(spans, WeatherKind.Verdant, Here, 1500, 2000));
        Assert.Equal(0, RiteHistory.Owed(spans, WeatherKind.Verdant, Here, 3000, 5000));
    }

    [Fact]
    public void Owes_nothing_out_of_reach_or_for_another_rite()
    {
        var spans = new List<RiteHistory.Span> { Span(1000, 2800) };
        Assert.Equal(0, RiteHistory.Owed(spans, WeatherKind.Verdant, new UnityEngine.Vector3(200f, 0f, 100f), 0, 5000));
        Assert.Equal(0, RiteHistory.Owed(spans, WeatherKind.VatBoil, Here, 0, 5000));
    }

    [Fact]
    public void Owes_at_most_the_rite_s_length_for_a_span_slept_through()
    {
        var spans = new List<RiteHistory.Span> { Span(1000, 30000, duration: 1800f) };
        Assert.Equal(1800, RiteHistory.Owed(spans, WeatherKind.Verdant, Here, 0, 40000));
    }

    [Fact]
    public void Adds_up_several_spans()
    {
        var spans = new List<RiteHistory.Span> { Span(1000, 2000), Span(3000, 3500) };
        Assert.Equal(1500, RiteHistory.Owed(spans, WeatherKind.Verdant, Here, 0, 5000));
    }

    [Fact]
    public void The_rite_going_on_carries_its_span_on()
    {
        var spans = new List<RiteHistory.Span>();
        RiteHistory.Track(spans, WeatherKind.Verdant, Here, 40f, 1800f, 1000);
        RiteHistory.Track(spans, WeatherKind.Verdant, Here, 40f, 1800f, 1001);
        RiteHistory.Track(spans, WeatherKind.Verdant, Here, 40f, 1800f, 1060);
        Assert.Single(spans);
        Assert.Equal(1000, spans[0].Start);
        Assert.Equal(1060, spans[0].End);
    }

    [Fact]
    public void A_new_rite_starts_a_new_span()
    {
        var spans = new List<RiteHistory.Span>();
        RiteHistory.Track(spans, WeatherKind.Verdant, Here, 40f, 1800f, 1000);
        RiteHistory.Track(spans, WeatherKind.Verdant, Here, 40f, 1800f, 5000);
        RiteHistory.Track(spans, WeatherKind.Verdant, new UnityEngine.Vector3(500f, 0f, 0f), 40f, 1800f, 5001);
        Assert.Equal(3, spans.Count);
    }

    [Fact]
    public void Keeps_only_the_latest_spans_of_a_rite()
    {
        var spans = new List<RiteHistory.Span>();
        for (int i = 0; i < 15; i++) RiteHistory.Track(spans, WeatherKind.Verdant, Here, 40f, 1800f, i * 1000.0);
        Assert.Equal(10, spans.Count);
        Assert.Equal(5000, spans.Min(s => s.Start));
    }

    [Fact]
    public void Saves_and_reads_back_beside_the_rites()
    {
        var spans = new List<RiteHistory.Span> { new(WeatherKind.PitchBellows, new UnityEngine.Vector3(1.5f, 2f, -3.25f), 50f, 1234.5, 2345.5, 1800f) };
        var entries = new List<WeatherState.Entry> { new(WeatherKind.Rain, 100f, 1800f, "Someone") };
        string text = WeatherStore.Format(entries) + "\n" + RiteHistory.Format(spans);
        RiteHistory.Span read = Assert.Single(RiteHistory.Parse(text));
        Assert.Equal(WeatherKind.PitchBellows, read.Kind);
        Assert.Equal(1234.5, read.Start);
        Assert.Equal(2345.5, read.End);
        Assert.Equal(1800f, read.Duration);
        Assert.Equal(50f, read.Radius);
        Assert.Equal(-3.25f, read.Position.z);
        // The rites are read as before, the span line skipped.
        Assert.Single(WeatherStore.Parse(text));
    }
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
        Assert.Equal(RiteGroup.Celestial, WeatherKinds.Group(WeatherKind.GaleBreath));
        Assert.Equal(RiteGroup.Blessings, WeatherKinds.Group(WeatherKind.FairWind));
        Assert.Equal(RiteGroup.BiomeMastery, WeatherKinds.Group(WeatherKind.AbyssalSurge));
        Assert.Equal(RiteGroup.Domain, WeatherKinds.Group(WeatherKind.Verdant));
        Assert.Equal(RiteGroup.Domain, WeatherKinds.Group(WeatherKind.VatBoil));
        Assert.Equal(RiteGroup.Domain, WeatherKinds.Group(WeatherKind.PitchBellows));
        Assert.Equal(RiteGroup.Domain, WeatherKinds.Group(WeatherKind.Kindred));
        Assert.Equal(RiteGroup.Domain, WeatherKinds.Group(WeatherKind.HarrowSpur));
        Assert.Equal(RiteGroup.Blessings, WeatherKinds.Group(WeatherKind.HushFelling));
        Assert.Equal(RiteGroup.Blessings, WeatherKinds.Group(WeatherKind.DraughtFlush));
        Assert.Equal(RiteGroup.Domain, WeatherKinds.Group(WeatherKind.AmberMend));
        Assert.Equal(RiteGroup.Blessings, WeatherKinds.Group(WeatherKind.CryptDelver));
        Assert.Equal(RiteGroup.Blessings, WeatherKinds.Group(WeatherKind.FortunesBoon));
        Assert.Equal(RiteGroup.Reckoning, WeatherKinds.Group(WeatherKind.Sanctuary));
        Assert.Equal(RiteGroup.Atmospheric, WeatherKinds.Group(WeatherKind.Reset));
        Assert.Equal(RiteGroup.Reckoning, WeatherKinds.Group(WeatherKind.Unmaking));
    }

    [Fact]
    public void Located_rites()
    {
        Assert.True(WeatherKinds.Located(WeatherKind.Packward));
        Assert.True(WeatherKinds.Located(WeatherKind.Crimson));
        Assert.True(WeatherKinds.Located(WeatherKind.Ragnarok));
        Assert.True(WeatherKinds.Located(WeatherKind.VatBoil));
        Assert.True(WeatherKinds.Located(WeatherKind.PitchBellows));
        Assert.True(WeatherKinds.Located(WeatherKind.Kindred));
        Assert.True(WeatherKinds.Located(WeatherKind.HarrowSpur));
        Assert.False(WeatherKinds.Located(WeatherKind.HushFelling));
        Assert.False(WeatherKinds.Located(WeatherKind.DraughtFlush));
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
            Assert.Equal(!WeatherKinds.InstantBlessing(kind), WeatherKinds.Lasts(kind));
            Assert.False(WeatherKinds.HasSlot(kind), kind.ToString());
            Assert.False(WeatherKinds.Located(kind), kind.ToString());
        }
        Assert.True(WeatherKinds.HasSlot(WeatherKind.Rain));
        Assert.False(WeatherKinds.HasSlot(WeatherKind.Reset));
    }

    [Fact]
    public void Kinship_rites_last_for_everyone_without_a_slot()
    {
        foreach (WeatherKind kind in WeatherKinds.All)
        {
            bool kinship = WeatherKinds.Group(kind) == RiteGroup.Kinship;
            Assert.Equal(kinship, WeatherKinds.ForEveryone(kind));
            if (!kinship) continue;
            Assert.True(WeatherKinds.Lasts(kind), kind.ToString());
            Assert.False(WeatherKinds.Personal(kind), kind.ToString());
            Assert.False(WeatherKinds.HasSlot(kind), kind.ToString());
            Assert.False(WeatherKinds.Located(kind), kind.ToString());
        }
        Assert.True(WeatherKinds.ForEveryone(WeatherKind.Skal));
        Assert.True(WeatherKinds.ForEveryone(WeatherKind.SkyCall));
        Assert.True(WeatherKinds.ForEveryone(WeatherKind.AnvilSpark));
        Assert.True(WeatherKinds.Clashes(WeatherKind.AnvilSpark, WeatherKind.SkyCall));
        Assert.True(WeatherKinds.Clashes(WeatherKind.Skal, WeatherKind.SkyCall));
        Assert.True(WeatherKinds.Clashes(WeatherKind.Skal, WeatherKind.AnvilSpark));
        Assert.True(WeatherKinds.ForEveryone(WeatherKind.Eldhrimnir));
        Assert.True(WeatherKinds.Clashes(WeatherKind.Eldhrimnir, WeatherKind.Skal));
        Assert.True(WeatherKinds.ForEveryone(WeatherKind.RallyHorn));
        Assert.True(WeatherKinds.Clashes(WeatherKind.RallyHorn, WeatherKind.Eldhrimnir));
        Assert.False(WeatherKinds.Ends(WeatherKind.Reset, WeatherKind.SkyCall));
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
        Assert.Equal(EffectSlot.Wind, WeatherKinds.Slot(WeatherKind.GaleBreath));
        // Vat-Boil and Pitch-Bellows each have their own, so both can go on at once.
        Assert.NotEqual(WeatherKinds.Slot(WeatherKind.VatBoil), WeatherKinds.Slot(WeatherKind.PitchBellows));
        Assert.True(WeatherKinds.Ends(WeatherKind.Grounding, WeatherKind.PitchBellows));
        Assert.False(WeatherKinds.Ends(WeatherKind.Reset, WeatherKind.PitchBellows));
        // Kindred has its own too, beside Verdant.
        Assert.NotEqual(WeatherKinds.Slot(WeatherKind.Verdant), WeatherKinds.Slot(WeatherKind.Kindred));
        Assert.True(WeatherKinds.Ends(WeatherKind.Grounding, WeatherKind.Kindred));
        Assert.False(WeatherKinds.Ends(WeatherKind.Reset, WeatherKind.Kindred));
        Assert.True(WeatherKinds.Ends(WeatherKind.Crimson, WeatherKind.Kindred));
        // Harrow-Spur too: a Domain rite, ended by Grounding, not Equilibrium.
        Assert.NotEqual(WeatherKinds.Slot(WeatherKind.Carrion), WeatherKinds.Slot(WeatherKind.HarrowSpur));
        Assert.True(WeatherKinds.Ends(WeatherKind.Grounding, WeatherKind.HarrowSpur));
        Assert.False(WeatherKinds.Ends(WeatherKind.Reset, WeatherKind.HarrowSpur));
        // Packward and Carrion share a slot: one replaces the other.
        Assert.Equal(WeatherKinds.Slot(WeatherKind.Packward), WeatherKinds.Slot(WeatherKind.Carrion));
    }

    [Fact]
    public void Only_the_domain_slots_are_domain()
    {
        Assert.True(WeatherKinds.IsDomainSlot(WeatherKinds.Slot(WeatherKind.Verdant)));
        Assert.True(WeatherKinds.IsDomainSlot(WeatherKinds.Slot(WeatherKind.Carrion)));
        Assert.True(WeatherKinds.IsDomainSlot(WeatherKinds.Slot(WeatherKind.VatBoil)));
        Assert.True(WeatherKinds.IsDomainSlot(WeatherKinds.Slot(WeatherKind.PitchBellows)));
        Assert.True(WeatherKinds.IsDomainSlot(WeatherKinds.Slot(WeatherKind.Kindred)));
        Assert.True(WeatherKinds.IsDomainSlot(WeatherKinds.Slot(WeatherKind.HarrowSpur)));
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
    public void Biomes_keeping_their_own_weather_are_left_out()
    {
        EnvironmentTable table = EnvironmentTable.Parse("Default: Clear, Swamp: None");
        Assert.Equal("Swamp, Ashlands, Deep North", Default.Without(biome => EnvironmentTable.KeepsOwn(table.For(biome))).LeftOut());
    }

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

public class ConfigNameTests
{
    // BepInEx refuses these in section and key names and the plugin then fails to load.
    private static readonly char[] Invalid = ['=', '\n', '\t', '\\', '"', '\'', '[', ']'];

    [Fact]
    public void Section_and_key_names_are_valid_for_BepInEx()
    {
        var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public;
        var types = new List<System.Type> { typeof(Settings.Constants.Groups) };
        types.AddRange(typeof(Settings.Constants.Groups).GetNestedTypes(flags));

        int checkedCount = 0;
        foreach (var type in types)
        foreach (var field in type.GetFields(flags))
        {
            var infos = new List<object?>();
            object? value = field.GetValue(null);
            if (value is Settings.RiteSettings rite)
                infos.AddRange([rite.Enabled, rite.Minutes, rite.KeyItem, rite.KeyAmount, rite.Environments, rite.Radius]);
            else if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Settings.ConfigInfo<>))
                infos.Add(value);

            foreach (var info in infos)
            {
                if (info == null) continue;
                foreach (var prop in new[] { "Group", "Name" })
                {
                    string text = (string)info.GetType().GetProperty(prop, flags | System.Reflection.BindingFlags.Instance)!.GetValue(info)!;
                    Assert.True(text.IndexOfAny(Invalid) < 0, $"{field.Name}.{prop} has an invalid character: {text}");
                }
                checkedCount++;
            }
        }
        Assert.True(checkedCount > 30);
    }

    // BepInEx hands back the same entry for a repeated section and key, so two settings would silently share one value.
    // Sections start with their group number, so Configuration Manager lists them in the rites page's order.
    [Fact]
    public void Section_and_key_pairs_are_unique_and_numbered()
    {
        var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
        var types = new List<System.Type> { typeof(Settings.Constants.Groups) };
        types.AddRange(typeof(Settings.Constants.Groups).GetNestedTypes(flags));

        var seen = new HashSet<string>();
        foreach (var type in types)
        foreach (var field in type.GetFields(flags & ~System.Reflection.BindingFlags.Instance))
        {
            var infos = new List<object?>();
            object? value = field.GetValue(null);
            if (value is Settings.RiteSettings rite)
                infos.AddRange([rite.Enabled, rite.Minutes, rite.KeyItem, rite.KeyAmount, rite.Environments, rite.Radius]);
            else if (value is System.Array array)
                foreach (object? item in array) infos.Add(item);
            else if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Settings.ConfigInfo<>))
                infos.Add(value);

            foreach (var info in infos)
            {
                if (info == null || !info.GetType().IsGenericType || info.GetType().GetGenericTypeDefinition() != typeof(Settings.ConfigInfo<>)) continue;
                string group = (string)info.GetType().GetProperty("Group", flags)!.GetValue(info)!;
                string name = (string)info.GetType().GetProperty("Name", flags)!.GetValue(info)!;
                Assert.Matches(@"^\d - ", group);
                Assert.True(seen.Add(group + " > " + name), $"Repeated setting: {group} > {name}");
            }
        }
        Assert.True(seen.Count > 200);
    }
}

public class KinshipSaveTests
{
    [Fact]
    public void Sky_call_wait_reads_back()
    {
        string text = KinshipRites.Format(321.5f, 600f, "Ale, Wright");
        Assert.True(KinshipRites.TryParse(text, out float remaining, out float duration, out string calledBy));
        Assert.Equal(321.5f, remaining);
        Assert.Equal(600f, duration);
        Assert.Equal("Ale  Wright", calledBy);
    }

    [Fact]
    public void Skips_bad_or_finished_waits()
    {
        Assert.False(KinshipRites.TryParse("", out _, out _, out _));
        Assert.False(KinshipRites.TryParse("abc,600,x", out _, out _, out _));
        Assert.False(KinshipRites.TryParse("0,600,x", out _, out _, out _));
    }
}

public class RiteOrderTests
{
    // Each rite that ends its group's rites is listed last in that group.
    [Theory]
    [InlineData((int)WeatherKind.Reset)]
    [InlineData((int)WeatherKind.SunFall)]
    [InlineData((int)WeatherKind.NaturesRebound)]
    public void Ender_is_last_of_its_group(int kind)
    {
        RiteGroup group = WeatherKinds.Group((WeatherKind)kind);
        Assert.Equal((WeatherKind)kind, System.Linq.Enumerable.Last(WeatherKinds.All, k => WeatherKinds.Group(k) == group));
    }

    // The Sentry-Sight, Taproot-Surge, Pitch-Seal, Primal-Hearth and Hearth-Kindle Rites: Domain rites, each
    // in its own slot, around their obliterator, ended by the Grounding Rite and by a Crimson Rite over them.
    [Theory]
    [InlineData((int)WeatherKind.SentrySight, 54, (int)EffectSlot.Sentries, 18)]
    [InlineData((int)WeatherKind.TaprootSurge, 56, (int)EffectSlot.Sap, 19)]
    [InlineData((int)WeatherKind.MantleAnchor, 58, (int)EffectSlot.Anchoring, 20)]
    [InlineData((int)WeatherKind.PrimalHearth, 59, (int)EffectSlot.Lures, 21)]
    [InlineData((int)WeatherKind.HearthKindle, 60, (int)EffectSlot.Resting, 22)]
    public void New_domain_rites_have_their_own_slots(int rite, int number, int slot, int slotNumber)
    {
        WeatherKind kind = (WeatherKind)rite;
        Assert.Equal(number, rite);
        Assert.Equal(slotNumber, slot);
        Assert.Equal((EffectSlot)slot, WeatherKinds.Slot(kind));
        Assert.Contains((EffectSlot)slot, WeatherKinds.Slots);
        Assert.Equal(slotNumber, System.Array.IndexOf(WeatherKinds.Slots, (EffectSlot)slot));
        Assert.Equal(RiteGroup.Domain, WeatherKinds.Group(kind));
        Assert.True(WeatherKinds.HasSlot(kind));
        Assert.True(WeatherKinds.Located(kind));
        Assert.True(WeatherKinds.Lasts(kind));
        Assert.True(WeatherKinds.IsDomainSlot((EffectSlot)slot));
        Assert.False(WeatherKinds.Personal(kind));
        Assert.False(WeatherKinds.ForEveryone(kind));
        Assert.True(WeatherKinds.Ends(WeatherKind.Grounding, kind));
        Assert.True(WeatherKinds.Ends(WeatherKind.Crimson, kind));
        Assert.False(WeatherKinds.Ends(WeatherKind.Reset, kind));
        Assert.False(WeatherKinds.Clashes(kind, WeatherKind.HearthHaul));
        Assert.Contains(kind, WeatherKinds.All);
    }

    // The Scald-Drift, Tether-Lock, Strider-Girth, Second-Kindle, Plume-Ward, Morgen-Pouch and Tendril-Draw Rites: blessings, kept by
    // each player, with no slot.
    [Theory]
    [InlineData((int)WeatherKind.ScaldDrift, 51)]
    [InlineData((int)WeatherKind.TetherLock, 52)]
    [InlineData((int)WeatherKind.StriderGirth, 55)]
    [InlineData((int)WeatherKind.SecondKindle, 57)]
    [InlineData((int)WeatherKind.PlumeWard, 53)]
    [InlineData((int)WeatherKind.MorgenPouch, 61)]
    [InlineData((int)WeatherKind.TendrilDraw, 62)]
    public void New_blessings_are_personal(int rite, int number)
    {
        WeatherKind kind = (WeatherKind)rite;
        Assert.Equal(number, rite);
        Assert.Equal(RiteGroup.Blessings, WeatherKinds.Group(kind));
        Assert.True(WeatherKinds.Personal(kind));
        Assert.True(WeatherKinds.Lasts(kind));
        Assert.False(WeatherKinds.HasSlot(kind));
        Assert.False(WeatherKinds.Located(kind));
        Assert.False(WeatherKinds.ForEveryone(kind));
        Assert.False(WeatherKinds.InstantBlessing(kind));
        Assert.False(WeatherKinds.Clashes(kind, WeatherKind.TundraShroud));
        Assert.Contains(kind, WeatherKinds.All);
    }

    // The Ley-Survey Rite: a Reckoning rite in its own slot around its obliterator, ended by any other Reckoning rite but
    // by nothing else, and never ending anything itself.
    [Fact]
    public void Ley_survey_is_a_located_reckoning_rite_ended_by_other_reckonings()
    {
        WeatherKind kind = WeatherKind.LeySurvey;
        Assert.Equal(63, (int)kind);
        Assert.Equal(23, (int)EffectSlot.Survey);
        Assert.Equal(EffectSlot.Survey, WeatherKinds.Slot(kind));
        Assert.Equal(23, System.Array.IndexOf(WeatherKinds.Slots, EffectSlot.Survey));
        Assert.Equal(RiteGroup.Reckoning, WeatherKinds.Group(kind));
        Assert.True(WeatherKinds.HasSlot(kind));
        Assert.True(WeatherKinds.Located(kind));
        Assert.True(WeatherKinds.Lasts(kind));
        Assert.False(WeatherKinds.Untimed(kind));
        Assert.False(WeatherKinds.IsIncursion(kind));
        Assert.False(WeatherKinds.IsDomainSlot(EffectSlot.Survey));
        Assert.True(WeatherKinds.EndsWithObliterator(EffectSlot.Survey));
        Assert.False(WeatherKinds.Personal(kind));
        Assert.False(WeatherKinds.ForEveryone(kind));
        foreach (WeatherKind ender in new[] { WeatherKind.Crimson, WeatherKind.Ragnarok, WeatherKind.HoardBane, WeatherKind.Sanctuary, WeatherKind.Unmaking })
        {
            Assert.True(WeatherKinds.EndsSurvey(ender), ender.ToString());
            Assert.True(WeatherKinds.Ends(ender, kind), ender.ToString());
            Assert.True(WeatherKinds.Clashes(ender, kind), ender.ToString());
        }
        Assert.False(WeatherKinds.EndsSurvey(kind));
        Assert.False(WeatherKinds.Ends(WeatherKind.Grounding, kind));
        Assert.False(WeatherKinds.Ends(WeatherKind.Reset, kind));
        Assert.False(WeatherKinds.Clashes(kind, WeatherKind.Verdant));
        foreach (WeatherKind other in WeatherKinds.All) Assert.False(WeatherKinds.Ends(kind, other), other.ToString());
        Assert.Contains(kind, WeatherKinds.All);
    }

    // Each blessing gets its own bit on the character: fewer than 31 of them.
    [Fact]
    public void Blessings_fit_in_their_bits() =>
        Assert.True(System.Array.FindAll(WeatherKinds.All, WeatherKinds.Personal).Length < 31);
}
