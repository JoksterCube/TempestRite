using System.Collections.Generic;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// Every rite the obliterator can perform. Sent over the network and saved as an int: keep the numbers.
internal enum WeatherKind
{
    None = 0,
    Clear = 1,
    Thunderstorm = 2,
    Rain = 3,
    Mist = 4,
    PolarDay = 5,
    PolarNight = 6,
    NoMist = 7,
    // Ends every rite going on. Has no slot and no time of its own.
    Reset = 8,
    Sootfall = 9,
    Tailwind = 10,
    // Ends the raid going on. Has no slot and no time of its own.
    Sanctuary = 11,
    Crimson = 12,
    Aurora = 13,
    Verdant = 14,
    CryptDelver = 15,
    Packward = 16,
    Carrion = 17,
    // Ends the Domain rites going on. Has no slot and no time of its own.
    Grounding = 18,
    VatBoil = 19,
    GlacialSatiety = 20,
    VeinShatter = 21,
    AmberMend = 22,
    CarapaceTemper = 23,
    GraveBind = 24,
    FortunesBoon = 25,
    AbyssalSurge = 26,
    Luminary = 27,
    SovereignWard = 28,
    AbyssalDraw = 29,
    // Turns gear burnt with it back into part of what it was made of. Has no slot and no time of its own.
    Unmaking = 30
}

// Rites in different slots go on side by side, each with its own time: rain during polar night is fine. A new one
// replaces only the one in its own slot. Blessings have no slot: each player keeps their own (see Blessings).
internal enum EffectSlot
{
    Weather = 0,
    Daylight = 1,
    Mistlands = 2,
    Ashlands = 3,
    Wind = 4,
    Incursion = 5,
    Verdant = 6,
    // The Packward and Carrion Rites: one replaces the other.
    Creatures = 7,
    Fermenting = 8,
    Mending = 9,
    Ocean = 10,
    Ward = 11
}

// How rites are grouped on the rites page, and colored.
internal enum RiteGroup
{
    Atmospheric = 0,
    Celestial = 1,
    BiomeMastery = 2,
    Domain = 3,
    Incursions = 4,
    // Given only to the players standing at the obliterator, each for themselves.
    Blessings = 5
}

internal static class WeatherKinds
{
    private sealed class Rite(string name, string plain, RiteGroup group, EffectSlot? slot, bool located = false)
    {
        internal string Name { get; } = name;
        internal string Plain { get; } = plain;
        internal RiteGroup Group { get; } = group;
        // None: it happens at once, or it is a blessing, which each player keeps for themselves.
        internal EffectSlot? Slot { get; } = slot;
        // Works around the obliterator it was performed at, rather than everywhere.
        internal bool Located { get; } = located;
    }

    private static readonly Dictionary<WeatherKind, Rite> Rites = new()
    {
        [WeatherKind.Clear] = new(Messages.ClearName, "clear skies", RiteGroup.Atmospheric, EffectSlot.Weather),
        [WeatherKind.Thunderstorm] = new(Messages.ThunderstormName, "thunderstorm", RiteGroup.Atmospheric, EffectSlot.Weather),
        [WeatherKind.Rain] = new(Messages.RainName, "rain", RiteGroup.Atmospheric, EffectSlot.Weather),
        [WeatherKind.Mist] = new(Messages.MistName, "mist", RiteGroup.Atmospheric, EffectSlot.Weather),
        [WeatherKind.Aurora] = new(Messages.AuroraName, "auroras and calm winds", RiteGroup.Atmospheric, EffectSlot.Weather),
        [WeatherKind.PolarDay] = new(Messages.PolarDayName, "the sun stays up", RiteGroup.Celestial, EffectSlot.Daylight),
        [WeatherKind.PolarNight] = new(Messages.PolarNightName, "the sun stays down", RiteGroup.Celestial, EffectSlot.Daylight),
        [WeatherKind.Tailwind] = new(Messages.TailwindName, "wind behind your ship", RiteGroup.Celestial, EffectSlot.Wind),
        [WeatherKind.NoMist] = new(Messages.NoMistName, "no Mistlands mist", RiteGroup.BiomeMastery, EffectSlot.Mistlands),
        [WeatherKind.Sootfall] = new(Messages.SootfallName, "no Ashlands smoke or heat haze", RiteGroup.BiomeMastery, EffectSlot.Ashlands),
        [WeatherKind.AbyssalSurge] = new(Messages.AbyssalSurgeName, "more serpents and fish", RiteGroup.BiomeMastery, EffectSlot.Ocean),
        [WeatherKind.Packward] = new(Messages.PackwardName, "hostile creatures flee", RiteGroup.Domain, EffectSlot.Creatures, located: true),
        [WeatherKind.Carrion] = new(Messages.CarrionName, "wild animals come", RiteGroup.Domain, EffectSlot.Creatures, located: true),
        [WeatherKind.Verdant] = new(Messages.VerdantName, "plants grow faster", RiteGroup.Domain, EffectSlot.Verdant, located: true),
        [WeatherKind.VatBoil] = new(Messages.VatBoilName, "fermenters work faster", RiteGroup.Domain, EffectSlot.Fermenting, located: true),
        [WeatherKind.AmberMend] = new(Messages.AmberMendName, "buildings mend", RiteGroup.Domain, EffectSlot.Mending, located: true),
        [WeatherKind.SovereignWard] = new(Messages.SovereignWardName, "buildings can't be harmed", RiteGroup.Domain, EffectSlot.Ward, located: true),
        [WeatherKind.Unmaking] = new(Messages.UnmakingName, "gear back into materials", RiteGroup.Domain, null),
        [WeatherKind.Grounding] = new(Messages.GroundingName, "ends the domain rites", RiteGroup.Domain, null),
        [WeatherKind.CryptDelver] = new(Messages.CryptDelverName, "never wet", RiteGroup.Blessings, null),
        [WeatherKind.GlacialSatiety] = new(Messages.GlacialSatietyName, "food fades slower", RiteGroup.Blessings, null),
        [WeatherKind.VeinShatter] = new(Messages.VeinShatterName, "ore veins shatter", RiteGroup.Blessings, null),
        [WeatherKind.CarapaceTemper] = new(Messages.CarapaceTemperName, "gear wears slower", RiteGroup.Blessings, null),
        [WeatherKind.GraveBind] = new(Messages.GraveBindName, "no skill loss on death", RiteGroup.Blessings, null),
        [WeatherKind.FortunesBoon] = new(Messages.FortunesBoonName, "more loot", RiteGroup.Blessings, null),
        [WeatherKind.Luminary] = new(Messages.LuminaryName, "you shine", RiteGroup.Blessings, null),
        [WeatherKind.AbyssalDraw] = new(Messages.AbyssalDrawName, "items fly to you", RiteGroup.Blessings, null),
        [WeatherKind.Crimson] = new(Messages.CrimsonName, "raid after raid", RiteGroup.Incursions, EffectSlot.Incursion, located: true),
        [WeatherKind.Sanctuary] = new(Messages.SanctuaryName, "ends the raid", RiteGroup.Incursions, null),
        [WeatherKind.Reset] = new(Messages.ResetName, "ends all rites", RiteGroup.Incursions, null),
    };

    // In the order they are listed to players: by group.
    internal static readonly WeatherKind[] All =
    [
        WeatherKind.Clear, WeatherKind.Thunderstorm, WeatherKind.Rain, WeatherKind.Mist, WeatherKind.Aurora,
        WeatherKind.PolarDay, WeatherKind.PolarNight, WeatherKind.Tailwind,
        WeatherKind.NoMist, WeatherKind.Sootfall, WeatherKind.AbyssalSurge,
        WeatherKind.Packward, WeatherKind.Carrion, WeatherKind.Verdant, WeatherKind.VatBoil, WeatherKind.AmberMend,
        WeatherKind.SovereignWard, WeatherKind.Unmaking, WeatherKind.Grounding,
        WeatherKind.CryptDelver, WeatherKind.GlacialSatiety, WeatherKind.VeinShatter, WeatherKind.CarapaceTemper, WeatherKind.GraveBind,
        WeatherKind.FortunesBoon, WeatherKind.Luminary, WeatherKind.AbyssalDraw,
        WeatherKind.Crimson, WeatherKind.Sanctuary, WeatherKind.Reset
    ];

    // Every slot, in the order of their numbers.
    internal static readonly EffectSlot[] Slots =
    [
        EffectSlot.Weather, EffectSlot.Daylight, EffectSlot.Mistlands, EffectSlot.Ashlands, EffectSlot.Wind,
        EffectSlot.Incursion, EffectSlot.Verdant, EffectSlot.Creatures, EffectSlot.Fermenting, EffectSlot.Mending, EffectSlot.Ocean,
        EffectSlot.Ward
    ];

    // The slots of the Domain rites: ended only by the Grounding Rite, not the Equilibrium Rite, so nobody's growth or
    // protection is ended by accident.
    internal static bool IsDomainSlot(EffectSlot slot) =>
        slot is EffectSlot.Verdant or EffectSlot.Creatures or EffectSlot.Fermenting or EffectSlot.Mending or EffectSlot.Ward;

    // The tabs of the rites page, in order.
    internal static readonly RiteGroup[] Groups =
        [RiteGroup.Atmospheric, RiteGroup.Celestial, RiteGroup.BiomeMastery, RiteGroup.Domain, RiteGroup.Blessings, RiteGroup.Incursions];

    internal static bool IsKnown(WeatherKind kind) => Rites.ContainsKey(kind);

    // A blessing: given to the players at the obliterator, each keeps it for themselves until it ends or they die.
    internal static bool Personal(WeatherKind kind) => Rites.TryGetValue(kind, out Rite rite) && rite.Group == RiteGroup.Blessings;

    // Whether it lasts (has a time); the Equilibrium, Grounding, Sanctuary and Unmaking Rites happen at once.
    internal static bool Lasts(WeatherKind kind) =>
        Rites.TryGetValue(kind, out Rite rite) && (rite.Slot != null || rite.Group == RiteGroup.Blessings);

    // Whether it lasts in a slot everyone shares (not a blessing).
    internal static bool HasSlot(WeatherKind kind) => Rites.TryGetValue(kind, out Rite rite) && rite.Slot != null;

    internal static EffectSlot Slot(WeatherKind kind) => Rites.TryGetValue(kind, out Rite rite) && rite.Slot is { } slot ? slot : EffectSlot.Weather;

    internal static RiteGroup Group(WeatherKind kind) => Rites.TryGetValue(kind, out Rite rite) ? rite.Group : RiteGroup.Atmospheric;

    internal static bool Located(WeatherKind kind) => Rites.TryGetValue(kind, out Rite rite) && rite.Located;

    // The rite's name: "Tempest Rite".
    internal static string Name(WeatherKind kind) => Rites.TryGetValue(kind, out Rite rite) ? rite.Name : string.Empty;

    // What it does, in a few words: "thunderstorm".
    internal static string Plain(WeatherKind kind) => Rites.TryGetValue(kind, out Rite rite) ? rite.Plain : string.Empty;

    internal static string GroupName(RiteGroup group) => group switch
    {
        RiteGroup.Atmospheric => Messages.GroupAtmospheric,
        RiteGroup.Celestial => Messages.GroupCelestial,
        RiteGroup.BiomeMastery => Messages.GroupBiomeMastery,
        RiteGroup.Domain => Messages.GroupDomain,
        RiteGroup.Incursions => Messages.GroupIncursions,
        RiteGroup.Blessings => Messages.GroupBlessings,
        _ => string.Empty
    };

    // Rich text color of the rite, by its group: atmospheric blue, celestial gold, biome mastery violet, domain green,
    // blessings pale silver, incursions red.
    internal static string ColorHex(WeatherKind kind) => Group(kind) switch
    {
        RiteGroup.Atmospheric => "#9fd8ff",
        RiteGroup.Celestial => "#ffd27a",
        RiteGroup.BiomeMastery => "#c9a6ff",
        RiteGroup.Domain => "#9fe07a",
        RiteGroup.Blessings => "#e6ecff",
        _ => "#ff7a6a"
    };

    // The same, for the frame on the deciding item in the obliterator.
    internal static Color Color(WeatherKind kind) => Group(kind) switch
    {
        RiteGroup.Atmospheric => new Color(0.45f, 0.8f, 1f),
        RiteGroup.Celestial => new Color(1f, 0.78f, 0.3f),
        RiteGroup.BiomeMastery => new Color(0.75f, 0.55f, 1f),
        RiteGroup.Domain => new Color(0.55f, 0.9f, 0.4f),
        RiteGroup.Blessings => new Color(0.88f, 0.92f, 1f),
        _ => new Color(1f, 0.4f, 0.32f)
    };

    // "<color=#9fd8ff>Tempest Rite</color>"
    internal static string Colored(WeatherKind kind) => $"<color={ColorHex(kind)}>{Name(kind)}</color>";
}
