using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using ServerSync;
using static JoksterCube.TempestRite.Settings.Constants.Groups;

namespace JoksterCube.TempestRite.Settings;

// Bound section by section, in the order Configuration Manager lists them.
internal static class PluginConfig
{
    // 01 - General
    internal static ConfigEntry<WeatherPermission> WhoCanChange = null!;
    internal static ConfigEntry<WeatherBiomes> AffectedBiomes = null!;
    internal static ConfigEntry<float> DomainRadius = null!;
    internal static ConfigEntry<float> BlessingRange = null!;
    internal static ConfigEntry<Toggle> RequireModOnClients = null!;

    // 02 - 31: one section per rite.
    internal static ConfigEntry<Toggle> SovereignSanctuary = null!;
    internal static ConfigEntry<float> UnmakingPercent = null!;
    internal static ConfigEntry<float> LuminaryRange = null!;
    internal static ConfigEntry<float> DrawRange = null!;
    internal static ConfigEntry<float> SerpentMultiplier = null!;
    internal static ConfigEntry<float> FishMultiplier = null!;
    internal static ConfigEntry<float> BigFishMultiplier = null!;
    internal static ConfigEntry<float> VatBoilTimeCut = null!;
    internal static ConfigEntry<float> FoodFloorPercent = null!;
    internal static ConfigEntry<float> VeinSpreadPercent = null!;
    internal static ConfigEntry<float> VeinShatterAtPercent = null!;
    internal static ConfigEntry<string> VeinItems = null!;
    internal static ConfigEntry<float> WearReductionPercent = null!;
    internal static ConfigEntry<Toggle> GraveBeacon = null!;
    internal static ConfigEntry<float> TrophyMultiplier = null!;
    internal static ConfigEntry<string> TreeItems = null!;
    private static readonly Dictionary<WeatherKind, WeatherConfig> Rites = [];
    internal static ConfigEntry<string> CarrionCreatures = null!;
    internal static ConfigEntry<float> VerdantGrowthPercent = null!;
    internal static ConfigEntry<float> CrimsonPause = null!;
    internal static ConfigEntry<string> CrimsonEnvironment = null!;
    internal static ConfigEntry<string> BloodmoonBase = null!;

    // 32 - Display
    internal static ConfigEntry<Toggle> ShowStatus = null!;
    internal static ConfigEntry<Toggle> ShowLeverHint = null!;
    internal static ConfigEntry<Toggle> ShowHatchHint = null!;
    internal static ConfigEntry<Toggle> ShowItemHighlight = null!;
    internal static ConfigEntry<Toggle> ShowExplanations = null!;
    internal static ConfigEntry<Toggle> ShowRitesFlap = null!;
    internal static ConfigEntry<Toggle> ShowVerdantSparkles = null!;
    internal static ConfigEntry<Toggle> ShowFermenterBubbles = null!;
    internal static ConfigEntry<Toggle> ShowBlessingLightning = null!;
    internal static ConfigEntry<Toggle> ShowCreatureEffects = null!;
    internal static ConfigEntry<Toggle> ShowMessages = null!;

    internal static void Build(ConfigFile config, ConfigSync configSync)
    {
        ConfigOptions.Initialize(config, configSync);

        WhoCanChange = ConfigOptions.Config(General.WhoCanChange);
        AffectedBiomes = ConfigOptions.Config(General.AffectedBiomes);
        DomainRadius = ConfigOptions.Config(General.DomainRadius);
        BlessingRange = ConfigOptions.Config(General.BlessingRange);
        RequireModOnClients = ConfigOptions.Config(General.RequireModOnClients);

        Add(WeatherKind.Clear, Clear);
        Add(WeatherKind.Thunderstorm, Thunderstorm);
        Add(WeatherKind.Rain, Rain);
        Add(WeatherKind.Mist, Mist);
        Add(WeatherKind.Aurora, Aurora);
        Add(WeatherKind.PolarDay, PolarDay);
        Add(WeatherKind.PolarNight, PolarNight);
        Add(WeatherKind.Tailwind, Tailwind);
        Add(WeatherKind.NoMist, NoMist);
        Add(WeatherKind.Sootfall, Sootfall);
        Add(WeatherKind.AbyssalSurge, AbyssalSurge);
        SerpentMultiplier = ConfigOptions.Config(Constants.Groups.SerpentMultiplier);
        FishMultiplier = ConfigOptions.Config(Constants.Groups.FishMultiplier);
        BigFishMultiplier = ConfigOptions.Config(Constants.Groups.BigFishMultiplier);
        Add(WeatherKind.Packward, Packward);
        Add(WeatherKind.Carrion, Carrion);
        CarrionCreatures = ConfigOptions.Config(Constants.Groups.CarrionCreatures);
        Add(WeatherKind.Verdant, Verdant);
        VerdantGrowthPercent = ConfigOptions.Config(Constants.Groups.VerdantGrowthPercent);
        Add(WeatherKind.VatBoil, VatBoil);
        VatBoilTimeCut = ConfigOptions.Config(Constants.Groups.VatBoilTimeCut);
        Add(WeatherKind.AmberMend, AmberMend);
        Add(WeatherKind.SovereignWard, SovereignWard);
        SovereignSanctuary = ConfigOptions.Config(Constants.Groups.SovereignSanctuary);
        Add(WeatherKind.Unmaking, Unmaking);
        UnmakingPercent = ConfigOptions.Config(Constants.Groups.UnmakingPercent);
        Add(WeatherKind.Grounding, Grounding);
        Add(WeatherKind.CryptDelver, CryptDelver);
        Add(WeatherKind.GlacialSatiety, GlacialSatiety);
        FoodFloorPercent = ConfigOptions.Config(Constants.Groups.FoodFloorPercent);
        Add(WeatherKind.VeinShatter, VeinShatter);
        VeinSpreadPercent = ConfigOptions.Config(Constants.Groups.VeinSpreadPercent);
        VeinShatterAtPercent = ConfigOptions.Config(Constants.Groups.VeinShatterAtPercent);
        VeinItems = ConfigOptions.Config(Constants.Groups.VeinItems);
        Add(WeatherKind.CarapaceTemper, CarapaceTemper);
        WearReductionPercent = ConfigOptions.Config(Constants.Groups.WearReductionPercent);
        Add(WeatherKind.GraveBind, GraveBind);
        GraveBeacon = ConfigOptions.Config(Constants.Groups.GraveBeacon);
        Add(WeatherKind.FortunesBoon, FortunesBoon);
        TrophyMultiplier = ConfigOptions.Config(Constants.Groups.TrophyMultiplier);
        TreeItems = ConfigOptions.Config(Constants.Groups.TreeItems);
        Add(WeatherKind.Luminary, Luminary);
        LuminaryRange = ConfigOptions.Config(Constants.Groups.LuminaryRange);
        Add(WeatherKind.AbyssalDraw, AbyssalDraw);
        DrawRange = ConfigOptions.Config(Constants.Groups.DrawRange);
        Add(WeatherKind.Crimson, Crimson);
        CrimsonPause = ConfigOptions.Config(Constants.Groups.CrimsonPause);
        CrimsonEnvironment = ConfigOptions.Config(Constants.Groups.CrimsonEnvironment);
        BloodmoonBase = ConfigOptions.Config(Constants.Groups.BloodmoonBase);
        Add(WeatherKind.Sanctuary, Sanctuary);
        Add(WeatherKind.Reset, Reset);

        ShowStatus = ConfigOptions.Config(Display.ShowStatus);
        ShowLeverHint = ConfigOptions.Config(Display.ShowLeverHint);
        ShowHatchHint = ConfigOptions.Config(Display.ShowHatchHint);
        ShowItemHighlight = ConfigOptions.Config(Display.ShowItemHighlight);
        ShowExplanations = ConfigOptions.Config(Display.ShowExplanations);
        ShowRitesFlap = ConfigOptions.Config(Display.ShowRitesFlap);
        ShowVerdantSparkles = ConfigOptions.Config(Display.ShowVerdantSparkles);
        ShowFermenterBubbles = ConfigOptions.Config(Display.ShowFermenterBubbles);
        ShowBlessingLightning = ConfigOptions.Config(Display.ShowBlessingLightning);
        ShowCreatureEffects = ConfigOptions.Config(Display.ShowCreatureEffects);
        ShowMessages = ConfigOptions.Config(Display.ShowMessages);
    }

    private static void Add(WeatherKind kind, WeatherGroup group) => Rites[kind] = new WeatherConfig(group);

    internal static WeatherConfig? For(WeatherKind kind) => Rites.TryGetValue(kind, out WeatherConfig config) ? config : null;

    internal static bool IsEnabled(WeatherKind kind) => For(kind)?.Enabled.IsOn() == true;

    // A located rite's radius in meters (0 for rites that work everywhere): the Domain rites share one.
    internal static float Radius(WeatherKind kind) =>
        WeatherKinds.Group(kind) == RiteGroup.Domain ? DomainRadius.Value : For(kind)?.Radius?.Value ?? 0f;

    private static readonly PrefabList CarrionList = new();
    private static readonly PrefabList VeinList = new();
    private static readonly PrefabList TreeList = new();

    // The prefab hashes of the animals the Carrion Rite draws.
    internal static HashSet<int> CarrionPrefabs() => CarrionList.Read(CarrionCreatures.Value);

    // The prefab hashes of the items whose veins the Vein-Shatter Rite shatters.
    internal static HashSet<int> VeinPrefabs() => VeinList.Read(VeinItems.Value);

    // The prefab hashes of the items felled trees always drop under the Fortune's Boon Rite.
    internal static HashSet<int> TreePrefabs() => TreeList.Read(TreeItems.Value);

    // A comma separated list of prefab names, read again whenever the setting changes.
    private sealed class PrefabList
    {
        private string? _text;
        private readonly HashSet<int> _hashes = [];

        internal HashSet<int> Read(string text)
        {
            if (text == _text) return _hashes;
            _text = text;
            _hashes.Clear();
            foreach (string name in text.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries))
                if (name.Trim().Length > 0) _hashes.Add(name.Trim().GetStableHashCode());
            return _hashes;
        }
    }
}

internal sealed class WeatherConfig
{
    internal ConfigEntry<Toggle> Enabled { get; }
    internal ConfigEntry<float>? Minutes { get; }
    internal ConfigEntry<string> KeyItem { get; }
    internal ConfigEntry<int> KeyAmount { get; }
    internal ConfigEntry<string>? Environments { get; }
    internal ConfigEntry<float>? Radius { get; }

    private string? _parsedText;
    private EnvironmentTable? _table;

    internal float Seconds => (Minutes?.Value ?? 0f) * 60f;

    internal WeatherConfig(WeatherGroup group)
    {
        Enabled = ConfigOptions.Config(group.Enabled);
        if (group.Minutes != null) Minutes = ConfigOptions.Config(group.Minutes);
        KeyItem = ConfigOptions.Config(group.KeyItem);
        KeyAmount = ConfigOptions.Config(group.KeyAmount);
        if (group.Radius != null) Radius = ConfigOptions.Config(group.Radius);
        if (group.Environments != null) Environments = ConfigOptions.Config(group.Environments);
    }

    // The Environments setting, read again whenever it changes (also when the server sends its own).
    internal EnvironmentTable? Table
    {
        get
        {
            if (Environments == null) return null;
            string text = Environments.Value;
            if (_table != null && text == _parsedText) return _table;
            _parsedText = text;
            _table = EnvironmentTable.Parse(text);
            foreach (string problem in _table.Problems)
                Plugin.ModLogger.LogWarning($"Couldn't read \"{problem}\" in {Environments.Definition.Section} > {Environments.Definition.Key}: use Biome: Environment.");
            return _table;
        }
    }
}
