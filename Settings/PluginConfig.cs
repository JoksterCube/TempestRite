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
    internal static ConfigEntry<Toggle> AdminLocked = null!;

    // 02 - 34: one section per rite.
    internal static ConfigEntry<Toggle> SovereignSanctuary = null!;
    internal static ConfigEntry<float> UnmakingReturn = null!;
    internal static ConfigEntry<Toggle> UnmakingUpgradeMaterials = null!;
    internal static ConfigEntry<float> LuminaryRange = null!;
    internal static ConfigEntry<float> LuminaryStrength = null!;
    internal static ConfigEntry<float> LuminaryBodyLight = null!;
    internal static ConfigEntry<Toggle> LuminaryShadows = null!;
    internal static ConfigEntry<float> DrawRange = null!;
    internal static ConfigEntry<float> SkalCooldown = null!;
    internal static ConfigEntry<float> SerpentMultiplier = null!;
    internal static ConfigEntry<float> FishMultiplier = null!;
    internal static ConfigEntry<float> BigFishMultiplier = null!;
    internal static ConfigEntry<float> VatBoilSpeedMultiplier = null!;
    internal static ConfigEntry<float> PitchBellowsSpeedMultiplier = null!;
    internal static ConfigEntry<float> FoodFloor = null!;
    internal static ConfigEntry<float> LootMultiplier = null!;
    internal static ConfigEntry<float> OneExtraChance = null!;
    internal static ConfigEntry<float> TwoExtraChance = null!;
    internal static ConfigEntry<float> VeinSpread = null!;
    internal static ConfigEntry<float> VeinShatterAt = null!;
    internal static ConfigEntry<string> VeinExcluded = null!;
    internal static ConfigEntry<float> WearMultiplier = null!;
    internal static ConfigEntry<Toggle> GraveBeacon = null!;
    internal static ConfigEntry<Toggle> GraveReequip = null!;
    internal static ConfigEntry<float> LuckMultiplier = null!;
    internal static ConfigEntry<int> ForgeBreakPenalty = null!;
    private static readonly Dictionary<WeatherKind, WeatherConfig> Rites = [];
    internal static ConfigEntry<string> CarrionExcluded = null!;
    internal static ConfigEntry<float> CarrionSenses = null!;
    internal static ConfigEntry<string> PackwardExcluded = null!;
    internal static ConfigEntry<float> PackwardFightSeconds = null!;
    internal static ConfigEntry<float> VerdantGrowthMultiplier = null!;
    internal static ConfigEntry<float> CrimsonPause = null!;
    internal static ConfigEntry<string> CrimsonEnvironment = null!;
    internal static ConfigEntry<string> BloodmoonBase = null!;
    internal static ConfigEntry<string> CrimsonRewardItem = null!;
    internal static ConfigEntry<int> CrimsonRewardPerKill = null!;
    internal static ConfigEntry<Toggle> CrimsonBubble = null!;
    internal static ConfigEntry<string> RagnarokBosses = null!;
    internal static ConfigEntry<int> RagnarokBossCount = null!;
    internal static ConfigEntry<float> RagnarokPause = null!;
    internal static ConfigEntry<string> RagnarokTreasure = null!;
    internal static ConfigEntry<Toggle> RagnarokBubble = null!;
    internal static ConfigEntry<string> RagnarokEnvironment = null!;
    internal static ConfigEntry<string> RagnarokSkyBase = null!;

    // 35 - Display
    internal static ConfigEntry<Toggle> ShowStatus = null!;
    internal static ConfigEntry<Toggle> ShowLeverHint = null!;
    internal static ConfigEntry<Toggle> ShowHatchHint = null!;
    internal static ConfigEntry<Toggle> ShowItemHighlight = null!;
    internal static ConfigEntry<Toggle> ShowExplanations = null!;
    internal static ConfigEntry<Toggle> ShowVerdantSparkles = null!;
    internal static ConfigEntry<Toggle> ShowVatBoilMotes = null!;
    internal static ConfigEntry<Toggle> ShowPitchBellowsMotes = null!;
    internal static ConfigEntry<Toggle> ShowBlessingLightning = null!;
    internal static ConfigEntry<Toggle> ShowCreatureEffects = null!;
    internal static ConfigEntry<Toggle> ShowCreatureIcons = null!;
    internal static ConfigEntry<Toggle> ShowDomainEffects = null!;
    internal static ConfigEntry<Toggle> ShowMessages = null!;
    internal static ConfigEntry<Toggle> ShowRiteUnlocks = null!;

    internal static void Build(ConfigFile config, ConfigSync configSync)
    {
        ConfigOptions.Initialize(config, configSync);

        WhoCanChange = ConfigOptions.Config(General.WhoCanChange);
        AffectedBiomes = ConfigOptions.Config(General.AffectedBiomes);
        DomainRadius = ConfigOptions.Config(General.DomainRadius);
        BlessingRange = ConfigOptions.Config(General.BlessingRange);
        RequireModOnClients = ConfigOptions.Config(General.RequireModOnClients);
        AdminLocked = ConfigOptions.Config(General.AdminLocked);
        // As ServerSync's locking setting, only admins can change it, and changing it refreshes which settings are read-only.
        configSync.AddLockingConfigEntry(AdminLocked);

        Add(WeatherKind.Clear, Clear);
        Add(WeatherKind.Thunderstorm, Thunderstorm);
        Add(WeatherKind.Rain, Rain);
        Add(WeatherKind.Mist, Mist);
        Add(WeatherKind.Aurora, Aurora);
        Add(WeatherKind.PolarDay, PolarDay);
        Add(WeatherKind.PolarNight, PolarNight);
        Add(WeatherKind.GaleBreath, GaleBreath);
        Add(WeatherKind.NoMist, NoMist);
        Add(WeatherKind.Sootfall, Sootfall);
        Add(WeatherKind.AbyssalSurge, AbyssalSurge);
        SerpentMultiplier = ConfigOptions.Config(Constants.Groups.SerpentMultiplier);
        FishMultiplier = ConfigOptions.Config(Constants.Groups.FishMultiplier);
        BigFishMultiplier = ConfigOptions.Config(Constants.Groups.BigFishMultiplier);
        Add(WeatherKind.Packward, Packward);
        PackwardExcluded = ConfigOptions.Config(Constants.Groups.PackwardExcluded);
        PackwardFightSeconds = ConfigOptions.Config(Constants.Groups.PackwardFightSeconds);
        Add(WeatherKind.Carrion, Carrion);
        CarrionExcluded = ConfigOptions.Config(Constants.Groups.CarrionExcluded);
        CarrionSenses = ConfigOptions.Config(Constants.Groups.CarrionSenses);
        Add(WeatherKind.Verdant, Verdant);
        VerdantGrowthMultiplier = ConfigOptions.Config(Constants.Groups.VerdantGrowthMultiplier);
        Add(WeatherKind.VatBoil, VatBoil);
        VatBoilSpeedMultiplier = ConfigOptions.Config(Constants.Groups.VatBoilSpeedMultiplier);
        Add(WeatherKind.PitchBellows, PitchBellows);
        PitchBellowsSpeedMultiplier = ConfigOptions.Config(Constants.Groups.PitchBellowsSpeedMultiplier);
        Add(WeatherKind.AmberMend, AmberMend);
        Add(WeatherKind.SovereignWard, SovereignWard);
        SovereignSanctuary = ConfigOptions.Config(Constants.Groups.SovereignSanctuary);
        Add(WeatherKind.Unmaking, Unmaking);
        UnmakingReturn = ConfigOptions.Config(Constants.Groups.UnmakingReturn);
        UnmakingUpgradeMaterials = ConfigOptions.Config(Constants.Groups.UnmakingUpgradeMaterials);
        Add(WeatherKind.Grounding, Grounding);
        Add(WeatherKind.CryptDelver, CryptDelver);
        LootMultiplier = ConfigOptions.Config(Constants.Groups.LootMultiplier);
        OneExtraChance = ConfigOptions.Config(Constants.Groups.OneExtraChance);
        TwoExtraChance = ConfigOptions.Config(Constants.Groups.TwoExtraChance);
        Add(WeatherKind.GlacialSatiety, GlacialSatiety);
        FoodFloor = ConfigOptions.Config(Constants.Groups.FoodFloor);
        Add(WeatherKind.VeinShatter, VeinShatter);
        VeinSpread = ConfigOptions.Config(Constants.Groups.VeinSpread);
        VeinShatterAt = ConfigOptions.Config(Constants.Groups.VeinShatterAt);
        VeinExcluded = ConfigOptions.Config(Constants.Groups.VeinExcluded);
        Add(WeatherKind.CarapaceTemper, CarapaceTemper);
        WearMultiplier = ConfigOptions.Config(Constants.Groups.WearMultiplier);
        Add(WeatherKind.GraveBind, GraveBind);
        GraveBeacon = ConfigOptions.Config(Constants.Groups.GraveBeacon);
        GraveReequip = ConfigOptions.Config(Constants.Groups.GraveReequip);
        Add(WeatherKind.FortunesBoon, FortunesBoon);
        LuckMultiplier = ConfigOptions.Config(Constants.Groups.LuckMultiplier);
        ForgeBreakPenalty = ConfigOptions.Config(Constants.Groups.ForgeBreakPenalty);
        Add(WeatherKind.Luminary, Luminary);
        LuminaryRange = ConfigOptions.Config(Constants.Groups.LuminaryRange);
        LuminaryStrength = ConfigOptions.Config(Constants.Groups.LuminaryStrength);
        LuminaryBodyLight = ConfigOptions.Config(Constants.Groups.LuminaryBodyLight);
        LuminaryShadows = ConfigOptions.Config(Constants.Groups.LuminaryShadows);
        Add(WeatherKind.AbyssalDraw, AbyssalDraw);
        DrawRange = ConfigOptions.Config(Constants.Groups.DrawRange);
        Add(WeatherKind.Tailwind, Tailwind);
        Add(WeatherKind.Skal, Skal);
        SkalCooldown = ConfigOptions.Config(Constants.Groups.SkalCooldown);
        Add(WeatherKind.SkyCall, SkyCall);
        Add(WeatherKind.AnvilSpark, AnvilSpark);
        Add(WeatherKind.Crimson, Crimson);
        CrimsonPause = ConfigOptions.Config(Constants.Groups.CrimsonPause);
        CrimsonRewardItem = ConfigOptions.Config(Constants.Groups.CrimsonRewardItem);
        CrimsonRewardPerKill = ConfigOptions.Config(Constants.Groups.CrimsonRewardPerKill);
        CrimsonBubble = ConfigOptions.Config(Constants.Groups.CrimsonBubble);
        CrimsonEnvironment = ConfigOptions.Config(Constants.Groups.CrimsonEnvironment);
        BloodmoonBase = ConfigOptions.Config(Constants.Groups.BloodmoonBase);
        Add(WeatherKind.Ragnarok, Ragnarok);
        RagnarokBosses = ConfigOptions.Config(Constants.Groups.RagnarokBosses);
        RagnarokBossCount = ConfigOptions.Config(Constants.Groups.RagnarokBossCount);
        RagnarokPause = ConfigOptions.Config(Constants.Groups.RagnarokPause);
        RagnarokTreasure = ConfigOptions.Config(Constants.Groups.RagnarokTreasure);
        RagnarokBubble = ConfigOptions.Config(Constants.Groups.RagnarokBubble);
        RagnarokEnvironment = ConfigOptions.Config(Constants.Groups.RagnarokEnvironment);
        RagnarokSkyBase = ConfigOptions.Config(Constants.Groups.RagnarokSkyBase);
        Add(WeatherKind.Sanctuary, Sanctuary);
        Add(WeatherKind.Reset, Reset);

        ShowStatus = ConfigOptions.Config(Display.ShowStatus);
        ShowLeverHint = ConfigOptions.Config(Display.ShowLeverHint);
        ShowHatchHint = ConfigOptions.Config(Display.ShowHatchHint);
        ShowItemHighlight = ConfigOptions.Config(Display.ShowItemHighlight);
        ShowExplanations = ConfigOptions.Config(Display.ShowExplanations);
        ShowVerdantSparkles = ConfigOptions.Config(Display.ShowVerdantSparkles);
        ShowVatBoilMotes = ConfigOptions.Config(Display.ShowVatBoilMotes);
        ShowPitchBellowsMotes = ConfigOptions.Config(Display.ShowPitchBellowsMotes);
        ShowBlessingLightning = ConfigOptions.Config(Display.ShowBlessingLightning);
        ShowCreatureEffects = ConfigOptions.Config(Display.ShowCreatureEffects);
        ShowCreatureIcons = ConfigOptions.Config(Display.ShowCreatureIcons);
        ShowDomainEffects = ConfigOptions.Config(Display.ShowDomainEffects);
        ShowMessages = ConfigOptions.Config(Display.ShowMessages);
        ShowRiteUnlocks = ConfigOptions.Config(Display.ShowRiteUnlocks);
    }

    private static void Add(WeatherKind kind, RiteSettings group) => Rites[kind] = new WeatherConfig(group);

    internal static WeatherConfig? For(WeatherKind kind) => Rites.TryGetValue(kind, out WeatherConfig config) ? config : null;

    internal static bool IsEnabled(WeatherKind kind) => For(kind)?.Enabled.IsOn() == true;

    // A located rite's radius in meters (0 for rites that work everywhere): the Domain rites share one.
    internal static float Radius(WeatherKind kind) =>
        WeatherKinds.Group(kind) == RiteGroup.Domain ? DomainRadius.Value : For(kind)?.Radius?.Value ?? 0f;

    private static readonly PrefabList CarrionList = new();
    private static readonly PrefabList PackwardList = new();
    private static readonly PrefabList VeinList = new();

    // The prefab hashes of the creatures the Carrion Rite doesn't draw.
    internal static HashSet<int> CarrionExcludedPrefabs() => CarrionList.Read(CarrionExcluded.Value);

    // The prefab hashes of the creatures the Packward Rite leaves alone.
    internal static HashSet<int> PackwardExcludedPrefabs() => PackwardList.Read(PackwardExcluded.Value);

    // The prefab hashes of the mineables (or items they drop) the Vein-Shatter Rite leaves alone.
    internal static HashSet<int> VeinExcludedPrefabs() => VeinList.Read(VeinExcluded.Value);

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

    internal WeatherConfig(RiteSettings group)
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
