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
    Unmaking = 30,
    Skal = 31,
    SkyCall = 32,
    // Boss after boss at the obliterator (see RagnarokRush). Shares the Crimson Rite's slot: one replaces the other.
    Ragnarok = 33,
    AnvilSpark = 34,
    GaleBreath = 35,
    PitchBellows = 36
}

// Rites in different slots go on side by side, each with its own time: rain during polar night is fine. A new one
// replaces only the one in its own slot. Blessings have no slot: each player keeps their own (see Blessings).
internal enum EffectSlot
{
    Weather = 0,
    Daylight = 1,
    Mistlands = 2,
    Ashlands = 3,
    // The Crimson and Ragnarök Rites: one replaces the other.
    Incursion = 4,
    Verdant = 5,
    // The Packward and Carrion Rites: one replaces the other.
    Creatures = 6,
    Fermenting = 7,
    Mending = 8,
    Ocean = 9,
    Ward = 10,
    Wind = 11,
    Smelting = 12
}

// How rites are grouped on the rites page, and colored.
internal enum RiteGroup
{
    Atmospheric = 0,
    Celestial = 1,
    BiomeMastery = 2,
    Domain = 3,
    Reckoning = 4,
    // Given only to the players standing at the obliterator, each for themselves.
    Blessings = 5,
    // Given to everyone on the server at once, wherever they are (see KinshipRites).
    Kinship = 6
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
        [WeatherKind.GaleBreath] = new(Messages.GaleBreathName, "the strongest wind", RiteGroup.Celestial, EffectSlot.Wind),
        [WeatherKind.NoMist] = new(Messages.NoMistName, "no Mistlands mist", RiteGroup.BiomeMastery, EffectSlot.Mistlands),
        [WeatherKind.Sootfall] = new(Messages.SootfallName, "no Ashlands smoke or heat haze", RiteGroup.BiomeMastery, EffectSlot.Ashlands),
        [WeatherKind.AbyssalSurge] = new(Messages.AbyssalSurgeName, "more serpents and fish", RiteGroup.BiomeMastery, EffectSlot.Ocean),
        [WeatherKind.Packward] = new(Messages.PackwardName, "creatures shun the area", RiteGroup.Domain, EffectSlot.Creatures, located: true),
        [WeatherKind.Carrion] = new(Messages.CarrionName, "creatures come", RiteGroup.Domain, EffectSlot.Creatures, located: true),
        [WeatherKind.Verdant] = new(Messages.VerdantName, "plants grow faster", RiteGroup.Domain, EffectSlot.Verdant, located: true),
        [WeatherKind.VatBoil] = new(Messages.VatBoilName, "fermenting and cooking go faster", RiteGroup.Domain, EffectSlot.Fermenting, located: true),
        [WeatherKind.PitchBellows] = new(Messages.PitchBellowsName, "smelters and kilns work faster", RiteGroup.Domain, EffectSlot.Smelting, located: true),
        [WeatherKind.AmberMend] = new(Messages.AmberMendName, "buildings mend", RiteGroup.Domain, EffectSlot.Mending, located: true),
        [WeatherKind.SovereignWard] = new(Messages.SovereignWardName, "buildings can't be harmed", RiteGroup.Domain, EffectSlot.Ward, located: true),
        [WeatherKind.Grounding] = new(Messages.GroundingName, "ends the domain rites", RiteGroup.Domain, null),
        [WeatherKind.CryptDelver] = new(Messages.CryptDelverName, "more dungeon loot", RiteGroup.Blessings, null),
        [WeatherKind.GlacialSatiety] = new(Messages.GlacialSatietyName, "food fades slower", RiteGroup.Blessings, null),
        [WeatherKind.VeinShatter] = new(Messages.VeinShatterName, "rocks and ore shatter", RiteGroup.Blessings, null),
        [WeatherKind.CarapaceTemper] = new(Messages.CarapaceTemperName, "gear wears slower", RiteGroup.Blessings, null),
        [WeatherKind.GraveBind] = new(Messages.GraveBindName, "no skill loss on death", RiteGroup.Blessings, null),
        [WeatherKind.FortunesBoon] = new(Messages.FortunesBoonName, "more luck", RiteGroup.Blessings, null),
        [WeatherKind.Luminary] = new(Messages.LuminaryName, "you shine", RiteGroup.Blessings, null),
        [WeatherKind.AbyssalDraw] = new(Messages.AbyssalDrawName, "items fly to you", RiteGroup.Blessings, null),
        [WeatherKind.Tailwind] = new(Messages.TailwindName, "wind behind your ship", RiteGroup.Blessings, null),
        [WeatherKind.Skal] = new(Messages.SkalName, "everyone drinks and rests", RiteGroup.Kinship, null),
        [WeatherKind.SkyCall] = new(Messages.SkyCallName, "forsaken powers renewed", RiteGroup.Kinship, null),
        [WeatherKind.AnvilSpark] = new(Messages.AnvilSparkName, "everyone's gear repaired", RiteGroup.Kinship, null),
        [WeatherKind.Crimson] = new(Messages.CrimsonName, "raid after raid", RiteGroup.Reckoning, EffectSlot.Incursion, located: true),
        [WeatherKind.Ragnarok] = new(Messages.RagnarokName, "boss after boss", RiteGroup.Reckoning, EffectSlot.Incursion, located: true),
        [WeatherKind.Sanctuary] = new(Messages.SanctuaryName, "ends the raid", RiteGroup.Reckoning, null),
        [WeatherKind.Reset] = new(Messages.ResetName, "ends all rites", RiteGroup.Reckoning, null),
        [WeatherKind.Unmaking] = new(Messages.UnmakingName, "gear back into materials", RiteGroup.Reckoning, null),
    };

    // In the order they are listed to players: by group.
    internal static readonly WeatherKind[] All =
    [
        WeatherKind.Clear, WeatherKind.Thunderstorm, WeatherKind.Rain, WeatherKind.Mist, WeatherKind.Aurora,
        WeatherKind.PolarDay, WeatherKind.PolarNight, WeatherKind.GaleBreath,
        WeatherKind.NoMist, WeatherKind.Sootfall, WeatherKind.AbyssalSurge,
        WeatherKind.Packward, WeatherKind.Carrion, WeatherKind.Verdant, WeatherKind.VatBoil, WeatherKind.PitchBellows,
        WeatherKind.AmberMend, WeatherKind.SovereignWard, WeatherKind.Grounding,
        WeatherKind.CryptDelver, WeatherKind.GlacialSatiety, WeatherKind.VeinShatter, WeatherKind.CarapaceTemper, WeatherKind.GraveBind,
        WeatherKind.FortunesBoon, WeatherKind.Luminary, WeatherKind.AbyssalDraw, WeatherKind.Tailwind,
        WeatherKind.Skal, WeatherKind.SkyCall, WeatherKind.AnvilSpark,
        WeatherKind.Crimson, WeatherKind.Ragnarok, WeatherKind.Sanctuary, WeatherKind.Reset, WeatherKind.Unmaking
    ];

    // Every slot, in the order of their numbers.
    internal static readonly EffectSlot[] Slots =
    [
        EffectSlot.Weather, EffectSlot.Daylight, EffectSlot.Mistlands, EffectSlot.Ashlands, EffectSlot.Incursion,
        EffectSlot.Verdant, EffectSlot.Creatures, EffectSlot.Fermenting, EffectSlot.Mending, EffectSlot.Ocean,
        EffectSlot.Ward, EffectSlot.Wind, EffectSlot.Smelting
    ];

    // The slots of the Domain rites: ended only by the Grounding Rite, not the Equilibrium Rite, so nobody's growth or
    // protection is ended by accident.
    internal static bool IsDomainSlot(EffectSlot slot) =>
        slot is EffectSlot.Verdant or EffectSlot.Creatures or EffectSlot.Fermenting or EffectSlot.Smelting or EffectSlot.Mending
            or EffectSlot.Ward;

    // Whether a rite ends another going on: the Equilibrium Rite every slot but the Domain ones, the Grounding Rite the
    // Domain ones, the Sanctuary Rite the Crimson and Ragnarök Rites, and those two the Domain ones (see WeatherState.Start).
    internal static bool Ends(WeatherKind ender, WeatherKind kind) => ender switch
    {
        WeatherKind.Reset => HasSlot(kind) && !IsDomainSlot(Slot(kind)),
        WeatherKind.Grounding => HasSlot(kind) && IsDomainSlot(Slot(kind)),
        WeatherKind.Sanctuary => IsIncursion(kind),
        _ => IsIncursion(ender) && HasSlot(kind) && IsDomainSlot(Slot(kind))
    };

    // The Crimson and Ragnarök Rites: fights within a bubble around the obliterator.
    internal static bool IsIncursion(WeatherKind kind) => HasSlot(kind) && Slot(kind) == EffectSlot.Incursion;

    // Whether two rites can't both be performed by one burn: the same rite, the same slot, or one ends the other.
    // Blessings, kinship rites and the Unmaking Rite clash with nothing.
    internal static bool Clashes(WeatherKind a, WeatherKind b) =>
        a == b || Ends(a, b) || Ends(b, a) || (HasSlot(a) && HasSlot(b) && Slot(a) == Slot(b));

    // The tabs of the rites page, in order.
    internal static readonly RiteGroup[] Groups =
        [RiteGroup.Atmospheric, RiteGroup.Celestial, RiteGroup.BiomeMastery, RiteGroup.Domain, RiteGroup.Blessings, RiteGroup.Kinship,
            RiteGroup.Reckoning];

    internal static bool IsKnown(WeatherKind kind) => Rites.ContainsKey(kind);

    // A blessing: given to the players at the obliterator, each keeps it for themselves until it ends or they die.
    internal static bool Personal(WeatherKind kind) => Rites.TryGetValue(kind, out Rite rite) && rite.Group == RiteGroup.Blessings;

    // A kinship rite: given to everyone on the server at once, each keeps what it gave for themselves.
    internal static bool ForEveryone(WeatherKind kind) => Rites.TryGetValue(kind, out Rite rite) && rite.Group == RiteGroup.Kinship;

    // Whether it lasts (has a time); the Equilibrium, Grounding, Sanctuary and Unmaking Rites happen at once. A kinship
    // rite's time is the wait after it gave a player something, until it can again (see KinshipRites).
    internal static bool Lasts(WeatherKind kind) =>
        Rites.TryGetValue(kind, out Rite rite) && (rite.Slot != null || rite.Group is RiteGroup.Blessings or RiteGroup.Kinship);

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
        RiteGroup.Reckoning => Messages.GroupReckoning,
        RiteGroup.Blessings => Messages.GroupBlessings,
        RiteGroup.Kinship => Messages.GroupKinship,
        _ => string.Empty
    };

    // Rich text color of the rite, by its group: atmospheric blue, celestial gold, biome mastery violet, domain green,
    // blessings pale silver, kinship mead amber, reckoning red.
    internal static string ColorHex(WeatherKind kind) => GroupColorHex(Group(kind));

    internal static string GroupColorHex(RiteGroup group) => group switch
    {
        RiteGroup.Atmospheric => "#9fd8ff",
        RiteGroup.Celestial => "#ffd27a",
        RiteGroup.BiomeMastery => "#c9a6ff",
        RiteGroup.Domain => "#9fe07a",
        RiteGroup.Blessings => "#e6ecff",
        RiteGroup.Kinship => "#f2b35a",
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
        RiteGroup.Kinship => new Color(0.95f, 0.66f, 0.28f),
        _ => new Color(1f, 0.4f, 0.32f)
    };

    // "<color=#9fd8ff>Tempest Rite</color>"
    internal static string Colored(WeatherKind kind) => $"<color={ColorHex(kind)}>{Name(kind)}</color>";
}
