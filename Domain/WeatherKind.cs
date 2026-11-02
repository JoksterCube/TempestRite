using System;
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
    // Ends the atmospheric rites going on (the Equilibrium Rite). Has no slot and no time of its own.
    Reset = 8,
    Sootfall = 9,
    FairWind = 10,
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
    PitchBellows = 36,
    Kindred = 37,
    // A Hildir miniboss and its elites at the obliterator (see HoardBaneRush). Shares the Crimson Rite's slot.
    HoardBane = 38,
    HushFelling = 39,
    HarrowSpur = 40,
    DraughtFlush = 41,
    // Pulls the blessed players' tombstones to the obliterator (see SoulTether). A blessing with no time of its own.
    SoulTether = 42,
    HearthHaul = 43,
    Eldhrimnir = 44,
    // Calls everyone to the obliterator: each may accept within a while and is brought there (see RallyHorn).
    RallyHorn = 45,
    TundraShroud = 46,
    AetherClear = 47,
    // Turns the tree stumps around the obliterator into saplings of the same trees, one after another (see ElderSprout).
    ElderSprout = 48,
    // Ends the celestial rites going on. Has no slot and no time of its own.
    SunFall = 49,
    // Ends the biome mastery rites going on. Has no slot and no time of its own.
    NaturesRebound = 50,
    ScaldDrift = 51,
    TetherLock = 52,
    PlumeWard = 53,
    SentrySight = 54,
    StriderGirth = 55,
    TaprootSurge = 56,
    // Saves the blessed player from one killing blow, then ends (see SecondKindle).
    SecondKindle = 57,
    MantleAnchor = 58,
    PrimalHearth = 59,
    HearthKindle = 60,
    // Carries the blessed player's mount and following pets in a pocket, let out again by an ability (see PetPouch).
    MorgenPouch = 61,
    // Saves some arrows and bolts, and brings thrown spears back to the blessed player (see TendrilDraw).
    TendrilDraw = 62
}

// Rites in different slots go on side by side, each with its own time: rain during polar night is fine. A new one
// replaces only the one in its own slot. Blessings have no slot: each player keeps their own (see Blessings).
internal enum EffectSlot
{
    Weather = 0,
    Daylight = 1,
    Mistlands = 2,
    Ashlands = 3,
    // The Crimson, Ragnarök and Hoard-Bane Rites: one replaces another.
    Incursion = 4,
    Verdant = 5,
    // The Packward and Carrion Rites: one replaces the other.
    Creatures = 6,
    Fermenting = 7,
    Mending = 8,
    Ocean = 9,
    Ward = 10,
    Wind = 11,
    Smelting = 12,
    Breeding = 13,
    // The Harrow-Spur Rite: creature spawners.
    Spawners = 14,
    // The Hearth-Haul Rite: what players can carry.
    Carrying = 15,
    // The Aether-Clear Rite: the Mistlands mist around the obliterator.
    Clearing = 16,
    // The Elder-Sprout Rite: tree stumps around the obliterator.
    Sprouting = 17,
    // The Sentry-Sight Rite: ballistas around the obliterator.
    Sentries = 18,
    // The Taproot-Surge Rite: sap extractors, eitr refineries and ancient roots.
    Sap = 19,
    // The Pitch-Seal Rite: items lying on the ground.
    Anchoring = 20,
    // The Primal-Hearth Rite: wisp fountains and eternal pyres.
    Lures = 21,
    // The Hearth-Kindle Rite: players resting by a fire.
    Resting = 22
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
    // Names are looked up each time, in the selected language.
    private sealed class Rite(Func<string> name, Func<string> plain, RiteGroup group, EffectSlot? slot, bool located = false)
    {
        internal string Name => name();
        internal string Plain => plain();
        internal RiteGroup Group { get; } = group;
        // None: it happens at once, or it is a blessing, which each player keeps for themselves.
        internal EffectSlot? Slot { get; } = slot;
        // Works around the obliterator it was performed at, rather than everywhere.
        internal bool Located { get; } = located;
    }

    private static readonly Dictionary<WeatherKind, Rite> Rites = new()
    {
        [WeatherKind.Clear] = new(() => Messages.ClearName, () => Messages.ClearPlain, RiteGroup.Atmospheric, EffectSlot.Weather),
        [WeatherKind.Thunderstorm] = new(() => Messages.ThunderstormName, () => Messages.ThunderstormPlain, RiteGroup.Atmospheric, EffectSlot.Weather),
        [WeatherKind.Rain] = new(() => Messages.RainName, () => Messages.RainPlain, RiteGroup.Atmospheric, EffectSlot.Weather),
        [WeatherKind.Mist] = new(() => Messages.MistName, () => Messages.MistPlain, RiteGroup.Atmospheric, EffectSlot.Weather),
        [WeatherKind.Aurora] = new(() => Messages.AuroraName, () => Messages.AuroraPlain, RiteGroup.Atmospheric, EffectSlot.Weather),
        [WeatherKind.Reset] = new(() => Messages.ResetName, () => Messages.ResetPlain, RiteGroup.Atmospheric, null),
        [WeatherKind.PolarDay] = new(() => Messages.PolarDayName, () => Messages.PolarDayPlain, RiteGroup.Celestial, EffectSlot.Daylight),
        [WeatherKind.PolarNight] = new(() => Messages.PolarNightName, () => Messages.PolarNightPlain, RiteGroup.Celestial, EffectSlot.Daylight),
        [WeatherKind.GaleBreath] = new(() => Messages.GaleBreathName, () => Messages.GaleBreathPlain, RiteGroup.Celestial, EffectSlot.Wind),
        [WeatherKind.SunFall] = new(() => Messages.SunFallName, () => Messages.SunFallPlain, RiteGroup.Celestial, null),
        [WeatherKind.NoMist] = new(() => Messages.NoMistName, () => Messages.NoMistPlain, RiteGroup.BiomeMastery, EffectSlot.Mistlands),
        [WeatherKind.Sootfall] = new(() => Messages.SootfallName, () => Messages.SootfallPlain, RiteGroup.BiomeMastery, EffectSlot.Ashlands),
        [WeatherKind.AbyssalSurge] = new(() => Messages.AbyssalSurgeName, () => Messages.AbyssalSurgePlain, RiteGroup.BiomeMastery, EffectSlot.Ocean),
        [WeatherKind.NaturesRebound] = new(() => Messages.NaturesReboundName, () => Messages.NaturesReboundPlain, RiteGroup.BiomeMastery, null),
        [WeatherKind.Packward] = new(() => Messages.PackwardName, () => Messages.PackwardPlain, RiteGroup.Domain, EffectSlot.Creatures, located: true),
        [WeatherKind.Carrion] = new(() => Messages.CarrionName, () => Messages.CarrionPlain, RiteGroup.Domain, EffectSlot.Creatures, located: true),
        [WeatherKind.Verdant] = new(() => Messages.VerdantName, () => Messages.VerdantPlain, RiteGroup.Domain, EffectSlot.Verdant, located: true),
        [WeatherKind.VatBoil] = new(() => Messages.VatBoilName, () => Messages.VatBoilPlain, RiteGroup.Domain, EffectSlot.Fermenting, located: true),
        [WeatherKind.PitchBellows] = new(() => Messages.PitchBellowsName, () => Messages.PitchBellowsPlain, RiteGroup.Domain, EffectSlot.Smelting, located: true),
        [WeatherKind.Kindred] = new(() => Messages.KindredName, () => Messages.KindredPlain, RiteGroup.Domain, EffectSlot.Breeding, located: true),
        [WeatherKind.HarrowSpur] = new(() => Messages.HarrowSpurName, () => Messages.HarrowSpurPlain, RiteGroup.Domain, EffectSlot.Spawners, located: true),
        [WeatherKind.AmberMend] = new(() => Messages.AmberMendName, () => Messages.AmberMendPlain, RiteGroup.Domain, EffectSlot.Mending, located: true),
        [WeatherKind.SovereignWard] = new(() => Messages.SovereignWardName, () => Messages.SovereignWardPlain, RiteGroup.Domain, EffectSlot.Ward, located: true),
        [WeatherKind.HearthHaul] = new(() => Messages.HearthHaulName, () => Messages.HearthHaulPlain, RiteGroup.Domain, EffectSlot.Carrying, located: true),
        [WeatherKind.AetherClear] = new(() => Messages.AetherClearName, () => Messages.AetherClearPlain, RiteGroup.Domain, EffectSlot.Clearing, located: true),
        [WeatherKind.ElderSprout] = new(() => Messages.ElderSproutName, () => Messages.ElderSproutPlain, RiteGroup.Domain, EffectSlot.Sprouting, located: true),
        [WeatherKind.SentrySight] = new(() => Messages.SentrySightName, () => Messages.SentrySightPlain, RiteGroup.Domain, EffectSlot.Sentries, located: true),
        [WeatherKind.TaprootSurge] = new(() => Messages.TaprootSurgeName, () => Messages.TaprootSurgePlain, RiteGroup.Domain, EffectSlot.Sap, located: true),
        [WeatherKind.MantleAnchor] = new(() => Messages.MantleAnchorName, () => Messages.MantleAnchorPlain, RiteGroup.Domain, EffectSlot.Anchoring, located: true),
        [WeatherKind.PrimalHearth] = new(() => Messages.PrimalHearthName, () => Messages.PrimalHearthPlain, RiteGroup.Domain, EffectSlot.Lures, located: true),
        [WeatherKind.HearthKindle] = new(() => Messages.HearthKindleName, () => Messages.HearthKindlePlain, RiteGroup.Domain, EffectSlot.Resting, located: true),
        [WeatherKind.Grounding] = new(() => Messages.GroundingName, () => Messages.GroundingPlain, RiteGroup.Domain, null),
        [WeatherKind.CryptDelver] = new(() => Messages.CryptDelverName, () => Messages.CryptDelverPlain, RiteGroup.Blessings, null),
        [WeatherKind.GlacialSatiety] = new(() => Messages.GlacialSatietyName, () => Messages.GlacialSatietyPlain, RiteGroup.Blessings, null),
        [WeatherKind.VeinShatter] = new(() => Messages.VeinShatterName, () => Messages.VeinShatterPlain, RiteGroup.Blessings, null),
        [WeatherKind.CarapaceTemper] = new(() => Messages.CarapaceTemperName, () => Messages.CarapaceTemperPlain, RiteGroup.Blessings, null),
        [WeatherKind.GraveBind] = new(() => Messages.GraveBindName, () => Messages.GraveBindPlain, RiteGroup.Blessings, null),
        [WeatherKind.FortunesBoon] = new(() => Messages.FortunesBoonName, () => Messages.FortunesBoonPlain, RiteGroup.Blessings, null),
        [WeatherKind.Luminary] = new(() => Messages.LuminaryName, () => Messages.LuminaryPlain, RiteGroup.Blessings, null),
        [WeatherKind.AbyssalDraw] = new(() => Messages.AbyssalDrawName, () => Messages.AbyssalDrawPlain, RiteGroup.Blessings, null),
        [WeatherKind.FairWind] = new(() => Messages.FairWindName, () => Messages.FairWindPlain, RiteGroup.Blessings, null),
        [WeatherKind.HushFelling] = new(() => Messages.HushFellingName, () => Messages.HushFellingPlain, RiteGroup.Blessings, null),
        [WeatherKind.DraughtFlush] = new(() => Messages.DraughtFlushName, () => Messages.DraughtFlushPlain, RiteGroup.Blessings, null),
        [WeatherKind.SoulTether] = new(() => Messages.SoulTetherName, () => Messages.SoulTetherPlain, RiteGroup.Blessings, null),
        [WeatherKind.TundraShroud] = new(() => Messages.TundraShroudName, () => Messages.TundraShroudPlain, RiteGroup.Blessings, null),
        [WeatherKind.ScaldDrift] = new(() => Messages.ScaldDriftName, () => Messages.ScaldDriftPlain, RiteGroup.Blessings, null),
        [WeatherKind.TetherLock] = new(() => Messages.TetherLockName, () => Messages.TetherLockPlain, RiteGroup.Blessings, null),
        [WeatherKind.StriderGirth] = new(() => Messages.StriderGirthName, () => Messages.StriderGirthPlain, RiteGroup.Blessings, null),
        [WeatherKind.SecondKindle] = new(() => Messages.SecondKindleName, () => Messages.SecondKindlePlain, RiteGroup.Blessings, null),
        [WeatherKind.PlumeWard] = new(() => Messages.PlumeWardName, () => Messages.PlumeWardPlain, RiteGroup.Blessings, null),
        [WeatherKind.MorgenPouch] = new(() => Messages.MorgenPouchName, () => Messages.MorgenPouchPlain, RiteGroup.Blessings, null),
        [WeatherKind.TendrilDraw] = new(() => Messages.TendrilDrawName, () => Messages.TendrilDrawPlain, RiteGroup.Blessings, null),
        [WeatherKind.Skal] = new(() => Messages.SkalName, () => Messages.SkalPlain, RiteGroup.Kinship, null),
        [WeatherKind.SkyCall] = new(() => Messages.SkyCallName, () => Messages.SkyCallPlain, RiteGroup.Kinship, null),
        [WeatherKind.AnvilSpark] = new(() => Messages.AnvilSparkName, () => Messages.AnvilSparkPlain, RiteGroup.Kinship, null),
        [WeatherKind.Eldhrimnir] = new(() => Messages.EldhrimnirName, () => Messages.EldhrimnirPlain, RiteGroup.Kinship, null),
        [WeatherKind.RallyHorn] = new(() => Messages.RallyHornName, () => Messages.RallyHornPlain, RiteGroup.Kinship, null),
        [WeatherKind.Crimson] = new(() => Messages.CrimsonName, () => Messages.CrimsonPlain, RiteGroup.Reckoning, EffectSlot.Incursion, located: true),
        [WeatherKind.Ragnarok] = new(() => Messages.RagnarokName, () => Messages.RagnarokPlain, RiteGroup.Reckoning, EffectSlot.Incursion, located: true),
        [WeatherKind.HoardBane] = new(() => Messages.HoardBaneName, () => Messages.HoardBanePlain, RiteGroup.Reckoning, EffectSlot.Incursion, located: true),
        [WeatherKind.Sanctuary] = new(() => Messages.SanctuaryName, () => Messages.SanctuaryPlain, RiteGroup.Reckoning, null),
        [WeatherKind.Unmaking] = new(() => Messages.UnmakingName, () => Messages.UnmakingPlain, RiteGroup.Reckoning, null),
    };

    // In the order they are listed to players: by group.
    internal static readonly WeatherKind[] All =
    [
        WeatherKind.Clear, WeatherKind.Thunderstorm, WeatherKind.Rain, WeatherKind.Mist, WeatherKind.Aurora, WeatherKind.Reset,
        WeatherKind.PolarDay, WeatherKind.PolarNight, WeatherKind.GaleBreath, WeatherKind.SunFall,
        WeatherKind.NoMist, WeatherKind.Sootfall, WeatherKind.AbyssalSurge, WeatherKind.NaturesRebound,
        WeatherKind.Packward, WeatherKind.Carrion, WeatherKind.Verdant, WeatherKind.VatBoil, WeatherKind.PitchBellows,
        WeatherKind.Kindred, WeatherKind.HarrowSpur, WeatherKind.AmberMend, WeatherKind.SovereignWard, WeatherKind.HearthHaul,
        WeatherKind.AetherClear, WeatherKind.ElderSprout, WeatherKind.SentrySight, WeatherKind.TaprootSurge,
        WeatherKind.MantleAnchor, WeatherKind.PrimalHearth, WeatherKind.HearthKindle, WeatherKind.Grounding,
        WeatherKind.CryptDelver, WeatherKind.GlacialSatiety, WeatherKind.VeinShatter, WeatherKind.CarapaceTemper, WeatherKind.GraveBind,
        WeatherKind.FortunesBoon, WeatherKind.Luminary, WeatherKind.AbyssalDraw, WeatherKind.FairWind, WeatherKind.HushFelling,
        WeatherKind.DraughtFlush, WeatherKind.SoulTether, WeatherKind.TundraShroud, WeatherKind.ScaldDrift, WeatherKind.TetherLock,
        WeatherKind.StriderGirth, WeatherKind.SecondKindle, WeatherKind.PlumeWard, WeatherKind.MorgenPouch, WeatherKind.TendrilDraw,
        WeatherKind.Skal, WeatherKind.SkyCall, WeatherKind.AnvilSpark, WeatherKind.Eldhrimnir, WeatherKind.RallyHorn,
        WeatherKind.Crimson, WeatherKind.Ragnarok, WeatherKind.HoardBane, WeatherKind.Sanctuary, WeatherKind.Unmaking
    ];

    // Every slot, in the order of their numbers.
    internal static readonly EffectSlot[] Slots =
    [
        EffectSlot.Weather, EffectSlot.Daylight, EffectSlot.Mistlands, EffectSlot.Ashlands, EffectSlot.Incursion,
        EffectSlot.Verdant, EffectSlot.Creatures, EffectSlot.Fermenting, EffectSlot.Mending, EffectSlot.Ocean,
        EffectSlot.Ward, EffectSlot.Wind, EffectSlot.Smelting, EffectSlot.Breeding, EffectSlot.Spawners,
        EffectSlot.Carrying, EffectSlot.Clearing, EffectSlot.Sprouting, EffectSlot.Sentries, EffectSlot.Sap,
        EffectSlot.Anchoring, EffectSlot.Lures, EffectSlot.Resting
    ];

    // The slots of the Domain rites: ended only by the Grounding Rite.
    internal static bool IsDomainSlot(EffectSlot slot) =>
        slot is EffectSlot.Verdant or EffectSlot.Creatures or EffectSlot.Fermenting or EffectSlot.Smelting or EffectSlot.Breeding
            or EffectSlot.Spawners or EffectSlot.Mending or EffectSlot.Ward or EffectSlot.Carrying
            or EffectSlot.Clearing or EffectSlot.Sprouting or EffectSlot.Sentries or EffectSlot.Sap
            or EffectSlot.Anchoring or EffectSlot.Lures or EffectSlot.Resting;

    // Whether a rite ends another going on: the Equilibrium Rite the atmospheric ones, the Sun-Fall Rite the celestial
    // ones, the Nature's Rebound Rite the biome mastery ones, the Grounding Rite the Domain ones, the Sanctuary Rite the
    // Crimson, Ragnarök and Hoard-Bane Rites, and those the Domain ones (see WeatherState.Start).
    internal static bool Ends(WeatherKind ender, WeatherKind kind) => ender switch
    {
        WeatherKind.Reset => HasSlot(kind) && Group(kind) == RiteGroup.Atmospheric,
        WeatherKind.SunFall => HasSlot(kind) && Group(kind) == RiteGroup.Celestial,
        WeatherKind.NaturesRebound => HasSlot(kind) && Group(kind) == RiteGroup.BiomeMastery,
        WeatherKind.Grounding => HasSlot(kind) && IsDomainSlot(Slot(kind)),
        WeatherKind.Sanctuary => IsIncursion(kind),
        _ => IsIncursion(ender) && HasSlot(kind) && IsDomainSlot(Slot(kind))
    };

    // The Crimson, Ragnarök and Hoard-Bane Rites: fights within a bubble around the obliterator.
    internal static bool IsIncursion(WeatherKind kind) => HasSlot(kind) && Slot(kind) == EffectSlot.Incursion;

    // Whether two rites can't both be performed by one burn: the same rite, the same slot, one ends the other, or both
    // are kinship rites (one emote at a time). Blessings and the Unmaking Rite clash with nothing.
    internal static bool Clashes(WeatherKind a, WeatherKind b) =>
        a == b || Ends(a, b) || Ends(b, a) || (HasSlot(a) && HasSlot(b) && Slot(a) == Slot(b)) || (ForEveryone(a) && ForEveryone(b));

    // The tabs of the rites page, in order.
    internal static readonly RiteGroup[] Groups =
        [RiteGroup.Atmospheric, RiteGroup.Celestial, RiteGroup.BiomeMastery, RiteGroup.Domain, RiteGroup.Blessings, RiteGroup.Kinship,
            RiteGroup.Reckoning];

    internal static bool IsKnown(WeatherKind kind) => Rites.ContainsKey(kind);

    // A blessing: given to the players at the obliterator, each keeps it for themselves until it ends or they die.
    internal static bool Personal(WeatherKind kind) => Rites.TryGetValue(kind, out Rite rite) && rite.Group == RiteGroup.Blessings;

    // A kinship rite: given to everyone on the server at once, each keeps what it gave for themselves.
    internal static bool ForEveryone(WeatherKind kind) => Rites.TryGetValue(kind, out Rite rite) && rite.Group == RiteGroup.Kinship;

    // Whether it lasts (has a time); the Equilibrium, Sun-Fall, Nature's Rebound, Grounding, Sanctuary and Unmaking
    // Rites happen at once. A kinship
    // rite's time is the wait after it gave a player something, until it can again (see KinshipRites).
    internal static bool Lasts(WeatherKind kind) =>
        Rites.TryGetValue(kind, out Rite rite) && !InstantBlessing(kind) && (rite.Slot != null || rite.Group is RiteGroup.Blessings or RiteGroup.Kinship);

    // A blessing that does what it does as it is received and isn't kept (the Soul-Tether Rite).
    internal static bool InstantBlessing(WeatherKind kind) => kind == WeatherKind.SoulTether;

    // Whether it goes on until it ends by itself, with no time limit (the Ragnarök Rite: until the last boss falls; the
    // Hoard-Bane Rite: until the miniboss falls).
    internal static bool Untimed(WeatherKind kind) => kind is WeatherKind.Ragnarok or WeatherKind.HoardBane;

    // How long an untimed rite is kept going at most, so a forgotten one doesn't last forever: a week of play.
    internal const float UntimedSeconds = 7f * 24f * 3600f;

    // Whether it lasts in a slot everyone shares (not a blessing).
    internal static bool HasSlot(WeatherKind kind) => Rites.TryGetValue(kind, out Rite rite) && rite.Slot != null;

    internal static EffectSlot Slot(WeatherKind kind) => Rites.TryGetValue(kind, out Rite rite) && rite.Slot is { } slot ? slot : EffectSlot.Weather;

    internal static RiteGroup Group(WeatherKind kind) => Rites.TryGetValue(kind, out Rite rite) ? rite.Group : RiteGroup.Atmospheric;

    internal static bool Located(WeatherKind kind) => Rites.TryGetValue(kind, out Rite rite) && rite.Located;

    // The rite's name: "Tempest Rite".
    internal static string Name(WeatherKind kind) => Rites.TryGetValue(kind, out Rite rite) ? rite.Name : string.Empty;

    // What it does, in a few words: "thunderstorm".
    internal static string Plain(WeatherKind kind) => Rites.TryGetValue(kind, out Rite rite) ? rite.Plain : string.Empty;

    // What it does, told with a little lore, for the rites page: "Thor rides out: thunder rolls...".
    internal static string Flavour(WeatherKind kind) => kind switch
    {
        WeatherKind.Clear => Messages.ClearFlavour,
        WeatherKind.Thunderstorm => Messages.ThunderstormFlavour,
        WeatherKind.Rain => Messages.RainFlavour,
        WeatherKind.Mist => Messages.MistFlavour,
        WeatherKind.Aurora => Messages.AuroraFlavour,
        WeatherKind.Reset => Messages.ResetFlavour,
        WeatherKind.PolarDay => Messages.PolarDayFlavour,
        WeatherKind.PolarNight => Messages.PolarNightFlavour,
        WeatherKind.GaleBreath => Messages.GaleBreathFlavour,
        WeatherKind.SunFall => Messages.SunFallFlavour,
        WeatherKind.NoMist => Messages.NoMistFlavour,
        WeatherKind.Sootfall => Messages.SootfallFlavour,
        WeatherKind.AbyssalSurge => Messages.AbyssalSurgeFlavour,
        WeatherKind.NaturesRebound => Messages.NaturesReboundFlavour,
        WeatherKind.Packward => Messages.PackwardFlavour,
        WeatherKind.Carrion => Messages.CarrionFlavour,
        WeatherKind.Verdant => Messages.VerdantFlavour,
        WeatherKind.VatBoil => Messages.VatBoilFlavour,
        WeatherKind.PitchBellows => Messages.PitchBellowsFlavour,
        WeatherKind.Kindred => Messages.KindredFlavour,
        WeatherKind.HarrowSpur => Messages.HarrowSpurFlavour,
        WeatherKind.AmberMend => Messages.AmberMendFlavour,
        WeatherKind.SovereignWard => Messages.SovereignWardFlavour,
        WeatherKind.HearthHaul => Messages.HearthHaulFlavour,
        WeatherKind.AetherClear => Messages.AetherClearFlavour,
        WeatherKind.Grounding => Messages.GroundingFlavour,
        WeatherKind.CryptDelver => Messages.CryptDelverFlavour,
        WeatherKind.GlacialSatiety => Messages.GlacialSatietyFlavour,
        WeatherKind.VeinShatter => Messages.VeinShatterFlavour,
        WeatherKind.CarapaceTemper => Messages.CarapaceTemperFlavour,
        WeatherKind.GraveBind => Messages.GraveBindFlavour,
        WeatherKind.FortunesBoon => Messages.FortunesBoonFlavour,
        WeatherKind.Luminary => Messages.LuminaryFlavour,
        WeatherKind.AbyssalDraw => Messages.AbyssalDrawFlavour,
        WeatherKind.FairWind => Messages.FairWindFlavour,
        WeatherKind.HushFelling => Messages.HushFellingFlavour,
        WeatherKind.DraughtFlush => Messages.DraughtFlushFlavour,
        WeatherKind.SoulTether => Messages.SoulTetherFlavour,
        WeatherKind.TundraShroud => Messages.TundraShroudFlavour,
        WeatherKind.Skal => Messages.SkalFlavour,
        WeatherKind.SkyCall => Messages.SkyCallFlavour,
        WeatherKind.AnvilSpark => Messages.AnvilSparkFlavour,
        WeatherKind.Eldhrimnir => Messages.EldhrimnirFlavour,
        WeatherKind.RallyHorn => Messages.RallyHornFlavour,
        WeatherKind.Crimson => Messages.CrimsonFlavour,
        WeatherKind.Ragnarok => Messages.RagnarokFlavour,
        WeatherKind.HoardBane => Messages.HoardBaneFlavour,
        WeatherKind.Sanctuary => Messages.SanctuaryFlavour,
        WeatherKind.Unmaking => Messages.UnmakingFlavour,
        WeatherKind.ElderSprout => Messages.ElderSproutFlavour,
        WeatherKind.PlumeWard => Messages.PlumeWardFlavour,
        WeatherKind.SentrySight => Messages.SentrySightFlavour,
        WeatherKind.TaprootSurge => Messages.TaprootSurgeFlavour,
        WeatherKind.MantleAnchor => Messages.MantleAnchorFlavour,
        WeatherKind.PrimalHearth => Messages.PrimalHearthFlavour,
        WeatherKind.HearthKindle => Messages.HearthKindleFlavour,
        WeatherKind.ScaldDrift => Messages.ScaldDriftFlavour,
        WeatherKind.TetherLock => Messages.TetherLockFlavour,
        WeatherKind.StriderGirth => Messages.StriderGirthFlavour,
        WeatherKind.SecondKindle => Messages.SecondKindleFlavour,
        WeatherKind.MorgenPouch => Messages.MorgenPouchFlavour,
        WeatherKind.TendrilDraw => Messages.TendrilDrawFlavour,
        _ => Plain(kind)
    };

    // What players hear when it is performed, after its name: "Thor rides out, thunder rolls". Kinship rites say what
    // they did to the player instead (see KinshipRites).
    internal static string Performed(WeatherKind kind) => kind switch
    {
        WeatherKind.Clear => Messages.ClearPerformed,
        WeatherKind.Thunderstorm => Messages.ThunderstormPerformed,
        WeatherKind.Rain => Messages.RainPerformed,
        WeatherKind.Mist => Messages.MistPerformed,
        WeatherKind.Aurora => Messages.AuroraPerformed,
        WeatherKind.Reset => Messages.ResetPerformed,
        WeatherKind.PolarDay => Messages.PolarDayPerformed,
        WeatherKind.PolarNight => Messages.PolarNightPerformed,
        WeatherKind.GaleBreath => Messages.GaleBreathPerformed,
        WeatherKind.SunFall => Messages.SunFallPerformed,
        WeatherKind.NoMist => Messages.NoMistPerformed,
        WeatherKind.Sootfall => Messages.SootfallPerformed,
        WeatherKind.AbyssalSurge => Messages.AbyssalSurgePerformed,
        WeatherKind.NaturesRebound => Messages.NaturesReboundPerformed,
        WeatherKind.Packward => Messages.PackwardPerformed,
        WeatherKind.Carrion => Messages.CarrionPerformed,
        WeatherKind.Verdant => Messages.VerdantPerformed,
        WeatherKind.VatBoil => Messages.VatBoilPerformed,
        WeatherKind.PitchBellows => Messages.PitchBellowsPerformed,
        WeatherKind.Kindred => Messages.KindredPerformed,
        WeatherKind.HarrowSpur => Messages.HarrowSpurPerformed,
        WeatherKind.AmberMend => Messages.AmberMendPerformed,
        WeatherKind.SovereignWard => Messages.SovereignWardPerformed,
        WeatherKind.HearthHaul => Messages.HearthHaulPerformed,
        WeatherKind.AetherClear => Messages.AetherClearPerformed,
        WeatherKind.ElderSprout => Messages.ElderSproutPerformed,
        WeatherKind.PlumeWard => Messages.PlumeWardPerformed,
        WeatherKind.SentrySight => Messages.SentrySightPerformed,
        WeatherKind.TaprootSurge => Messages.TaprootSurgePerformed,
        WeatherKind.MantleAnchor => Messages.MantleAnchorPerformed,
        WeatherKind.PrimalHearth => Messages.PrimalHearthPerformed,
        WeatherKind.HearthKindle => Messages.HearthKindlePerformed,
        WeatherKind.Grounding => Messages.GroundingPerformed,
        WeatherKind.CryptDelver => Messages.CryptDelverPerformed,
        WeatherKind.GlacialSatiety => Messages.GlacialSatietyPerformed,
        WeatherKind.VeinShatter => Messages.VeinShatterPerformed,
        WeatherKind.CarapaceTemper => Messages.CarapaceTemperPerformed,
        WeatherKind.GraveBind => Messages.GraveBindPerformed,
        WeatherKind.FortunesBoon => Messages.FortunesBoonPerformed,
        WeatherKind.Luminary => Messages.LuminaryPerformed,
        WeatherKind.AbyssalDraw => Messages.AbyssalDrawPerformed,
        WeatherKind.FairWind => Messages.FairWindPerformed,
        WeatherKind.HushFelling => Messages.HushFellingPerformed,
        WeatherKind.DraughtFlush => Messages.DraughtFlushPerformed,
        WeatherKind.SoulTether => Messages.SoulTetherPerformed,
        WeatherKind.TundraShroud => Messages.TundraShroudPerformed,
        WeatherKind.ScaldDrift => Messages.ScaldDriftPerformed,
        WeatherKind.TetherLock => Messages.TetherLockPerformed,
        WeatherKind.StriderGirth => Messages.StriderGirthPerformed,
        WeatherKind.SecondKindle => Messages.SecondKindlePerformed,
        WeatherKind.MorgenPouch => Messages.MorgenPouchPerformed,
        WeatherKind.TendrilDraw => Messages.TendrilDrawPerformed,
        WeatherKind.Crimson => Messages.CrimsonPerformed,
        WeatherKind.Ragnarok => Messages.RagnarokPerformed,
        WeatherKind.HoardBane => Messages.HoardBanePerformed,
        WeatherKind.Sanctuary => Messages.SanctuaryPerformed,
        WeatherKind.Unmaking => Messages.UnmakingPerformed,
        _ => Plain(kind)
    };

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
