using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Common.Networking;
using JoksterCube.TempestRite.Domain;
using ServerSync;
using static JoksterCube.TempestRite.Settings.Constants.Groups;

namespace JoksterCube.TempestRite.Settings;

// Bound section by section, in the order Configuration Manager lists them.
internal static class PluginConfig
{
    // A boss's own loot for the Ragnarök Rite's chests (the resources of its lands): common, uncommon and rare; empty for
    // a boss without any.
    internal static string[] RagnarokBossLootOf(string boss) =>
        RagnarokBossLoot.TryGetValue(boss, out ConfigEntry<string>[] tiers) ? Array.ConvertAll(tiers, tier => tier.Value) : [];

    // 0 - General, and the group-wide settings of 1 - Atmospheric, 4 - Domain and 5 - Blessings
    internal static ConfigEntry<WeatherPermission> WhoCanChange = null!;
    internal static ConfigEntry<WeatherBiomes> AffectedBiomes = null!;
    internal static ConfigEntry<float> DomainRadius = null!;
    internal static ConfigEntry<float> BlessingRange = null!;
    internal static ConfigEntry<Toggle> RequireModOnClients = null!;
    internal static ConfigEntry<Toggle> AdminLocked = null!;

    // One section per rite, named by its group (1 - Atmospheric ... 7 - Reckoning).
    internal static ConfigEntry<Toggle> SovereignSanctuary = null!;
    internal static ConfigEntry<float> HearthHaulCarryBonus = null!;
    internal static ConfigEntry<Toggle> HearthHaulFreeStamina = null!;
    internal static ConfigEntry<float> PlumeWardFallDamage = null!;
    internal static ConfigEntry<float> TaprootSpeedMultiplier = null!;
    internal static ConfigEntry<float> TaprootRootMultiplier = null!;
    internal static ConfigEntry<float> PrimalHearthSpawnMultiplier = null!;
    internal static ConfigEntry<float> PrimalHearthCapacityMultiplier = null!;
    internal static ConfigEntry<float> KindleRestingMultiplier = null!;
    internal static ConfigEntry<float> KindleRestedMultiplier = null!;
    internal static ConfigEntry<float> ScaldDamageMultiplier = null!;
    internal static ConfigEntry<float> ScaldTickSlowdown = null!;
    internal static ConfigEntry<float> ScaldLavaDamageMultiplier = null!;
    internal static ConfigEntry<float> ScaldLavaTickSlowdown = null!;
    internal static ConfigEntry<float> TetherStrengthMultiplier = null!;
    internal static ConfigEntry<float> TetherStaminaMultiplier = null!;
    internal static ConfigEntry<float> TetherReelMultiplier = null!;
    internal static ConfigEntry<float> StriderRegenMultiplier = null!;
    internal static ConfigEntry<float> StriderAttackStamina = null!;
    internal static ConfigEntry<float> StriderSecondaryStamina = null!;
    internal static ConfigEntry<string> StriderSwappedMounts = null!;
    internal static ConfigEntry<float> KindleHealthShare = null!;
    internal static ConfigEntry<float> KindleKnockbackRadius = null!;
    internal static ConfigEntry<int> MorgenPouchCount = null!;
    internal static ConfigEntry<float> MorgenPouchRange = null!;
    internal static ConfigEntry<float> MorgenPouchReleaseCooldown = null!;
    internal static ConfigEntry<float> MorgenPouchCaptureCooldown = null!;
    internal static ConfigEntry<float> TendrilAmmoChance = null!;
    internal static ConfigEntry<float> TendrilReturnSpeed = null!;
    internal static ConfigEntry<Toggle> SentrySpareAnimals = null!;
    internal static ConfigEntry<float> UnmakingReturn = null!;
    internal static ConfigEntry<float> LeySurveyRefusedSeconds = null!;
    internal static ConfigEntry<Toggle> UnmakingUpgradeMaterials = null!;
    internal static ConfigEntry<float> LuminaryRange = null!;
    internal static ConfigEntry<float> DrawRange = null!;
    internal static ConfigEntry<float> SkalCooldown = null!;
    internal static ConfigEntry<float> EldhrimnirFreeUse = null!;
    internal static ConfigEntry<float> RallyCallSeconds = null!;
    internal static ConfigEntry<float> SerpentMultiplier = null!;
    internal static ConfigEntry<float> FishMultiplier = null!;
    internal static ConfigEntry<float> BigFishMultiplier = null!;
    internal static ConfigEntry<float> VatBoilSpeedMultiplier = null!;
    internal static ConfigEntry<float> PitchBellowsSpeedMultiplier = null!;
    internal static ConfigEntry<float> KindredGrowthMultiplier = null!;
    internal static ConfigEntry<float> KindredCrowdMultiplier = null!;
    internal static ConfigEntry<float> HarrowSpurSpeedMultiplier = null!;
    internal static ConfigEntry<float> HarrowSpurCapMultiplier = null!;
    internal static ConfigEntry<string> HarrowSpurExcluded = null!;
    internal static ConfigEntry<float> HushNoticeMultiplier = null!;
    internal static ConfigEntry<float> DraughtRecoveryMultiplier = null!;
    internal static ConfigEntry<float> TetherLeaveWithin = null!;
    internal static ConfigEntry<float> DraughtBuffMultiplier = null!;
    internal static ConfigEntry<float> ShroudDryingMultiplier = null!;
    internal static ConfigEntry<float> FoodFloor = null!;
    internal static ConfigEntry<float> FoodDurationMultiplier = null!;
    internal static ConfigEntry<float> LootMultiplier = null!;
    internal static ConfigEntry<float> OneExtraChance = null!;
    internal static ConfigEntry<float> TwoExtraChance = null!;
    internal static ConfigEntry<float> VeinSpread = null!;
    internal static ConfigEntry<float> VeinShatterAt = null!;
    internal static ConfigEntry<string> VeinExcluded = null!;
    internal static ConfigEntry<float> DurabilityMultiplier = null!;
    internal static ConfigEntry<float> LuckMultiplier = null!;
    internal static ConfigEntry<int> ForgeBreakPenalty = null!;
    private static readonly Dictionary<WeatherKind, WeatherConfig> Rites = [];
    internal static ConfigEntry<string> CarrionExcluded = null!;
    internal static ConfigEntry<float> CarrionSenses = null!;
    internal static ConfigEntry<string> PackwardExcluded = null!;
    internal static ConfigEntry<float> PackwardFightSeconds = null!;
    internal static ConfigEntry<float> VerdantGrowthMultiplier = null!;
    internal static ConfigEntry<float> CrimsonPause = null!;
    internal static ConfigEntry<float> CrimsonSpawnMultiplier = null!;
    internal static ConfigEntry<float> CrimsonCatchUp = null!;
    internal static ConfigEntry<string> CrimsonEnvironment = null!;
    internal static ConfigEntry<string> BloodmoonBase = null!;
    internal static ConfigEntry<string> CrimsonRewardItem = null!;
    internal static ConfigEntry<int> CrimsonRewardPerKill = null!;
    internal static ConfigEntry<float> CrimsonRewardPerPlayer = null!;
    internal static ConfigEntry<int> CrimsonMaxPlayers = null!;
    internal static ConfigEntry<string> RagnarokBosses = null!;
    internal static ConfigEntry<int> RagnarokBossCount = null!;
    internal static ConfigEntry<float> RagnarokPause = null!;
    internal static ConfigEntry<float> RagnarokHealthMultiplier = null!;
    internal static ConfigEntry<float> RagnarokDamageMultiplier = null!;
    internal static ConfigEntry<float> RagnarokRewardPerPlayer = null!;
    internal static ConfigEntry<int> RagnarokMaxPlayers = null!;
    internal static ConfigEntry<float> RagnarokLootPerStar = null!;
    // Each boss's own loot, by its prefab name ("gd_king").
    private static readonly Dictionary<string, ConfigEntry<string>[]> RagnarokBossLoot = new(StringComparer.OrdinalIgnoreCase);
    internal static ConfigEntry<string> RagnarokChests = null!;
    internal static ConfigEntry<string> RagnarokEnvironment = null!;
    internal static ConfigEntry<string> RagnarokSkyBase = null!;
    internal static ConfigEntry<float> HoardBaneEliteRadius = null!;
    internal static ConfigEntry<float> HoardBaneHealthShare = null!;
    internal static ConfigEntry<float> HoardBaneDamageShare = null!;
    // By the fight's place: the brass, silver and bronze chests.
    internal static ConfigEntry<string>[] HoardBaneCoins = [];
    internal static ConfigEntry<string>[] HoardBaneTreasure = [];
    internal static ConfigEntry<int>[] HoardBaneTreasureRolls = [];
    internal static ConfigEntry<float> HoardBaneRewardPerPlayer = null!;
    internal static ConfigEntry<int> HoardBaneMaxPlayers = null!;
    internal static ConfigEntry<string> HoardBaneIdols = null!;
    internal static ConfigEntry<string> HoardBaneEnvironment = null!;
    internal static ConfigEntry<string> HoardBaneSkyBase = null!;
    internal static ConfigEntry<Toggle> SanctuaryOnlyInside = null!;

    // 8 - Interface
    internal static ConfigEntry<Toggle> ShowStatus = null!;
    internal static ConfigEntry<Toggle> ShowLeverHint = null!;
    internal static ConfigEntry<Toggle> ShowHatchHint = null!;
    internal static ConfigEntry<Toggle> ShowItemHighlight = null!;
    internal static ConfigEntry<Toggle> ShowExplanations = null!;
    internal static ConfigEntry<Toggle> ShowMessages = null!;
    internal static ConfigEntry<Toggle> ShowRiteUnlocks = null!;
    internal static ConfigEntry<Toggle> GraveReequip = null!;
    internal static ConfigEntry<KeyboardShortcut> MorgenPouchShortcut = null!;
    internal static ConfigEntry<KeyboardShortcut> RallyCallShortcut = null!;
    internal static ConfigEntry<KeyboardShortcut> RallyRefuseShortcut = null!;
    internal static ConfigEntry<KeyboardShortcut> DomainBorderShortcut = null!;

    // 9 - Effects
    internal static ConfigEntry<Toggle> ShowBlessingLightning = null!;
    internal static ConfigEntry<Toggle> ShowCreatureIcons = null!;
    internal static ConfigEntry<Toggle> ShowDomainEffects = null!;
    internal static ConfigEntry<Toggle> AlwaysShowDomainBorder = null!;
    internal static ConfigEntry<Toggle> ShowVerdantSparkles = null!;
    internal static ConfigEntry<Toggle> ShowVatBoilMotes = null!;
    internal static ConfigEntry<Toggle> ShowPitchBellowsMotes = null!;
    internal static ConfigEntry<UnityEngine.Color> PitchBellowsFireTint = null!;
    internal static ConfigEntry<Toggle> ShowKindredHearts = null!;
    internal static ConfigEntry<Toggle> ShowHarrowSpurMotes = null!;
    internal static ConfigEntry<Toggle> ShowHearthHaulFeathers = null!;
    internal static ConfigEntry<Toggle> ShowSentrySightMotes = null!;
    internal static ConfigEntry<Toggle> ShowTaprootSurgeMotes = null!;
    internal static ConfigEntry<UnityEngine.Color> TaprootSurgeFireTint = null!;
    internal static ConfigEntry<Toggle> ShowMantleAnchorMotes = null!;
    internal static ConfigEntry<Toggle> ShowPrimalHearthMotes = null!;
    internal static ConfigEntry<float> PrimalHearthGlow = null!;
    internal static ConfigEntry<Toggle> ShowHearthKindleMotes = null!;
    internal static ConfigEntry<Toggle> ShowPlumeWardDown = null!;
    internal static ConfigEntry<Toggle> ShowMorgenPouchGlow = null!;
    internal static ConfigEntry<Toggle> GraveBeacon = null!;
    internal static ConfigEntry<float> LuminaryStrength = null!;
    internal static ConfigEntry<float> LuminaryBodyLight = null!;
    internal static ConfigEntry<Toggle> LuminaryShadows = null!;
    internal static ConfigEntry<Toggle> CrimsonShield = null!;
    internal static ConfigEntry<Toggle> RagnarokShield = null!;
    internal static ConfigEntry<Toggle> HoardBaneShield = null!;

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
        Add(WeatherKind.Reset, Reset);
        Add(WeatherKind.PolarDay, PolarDay);
        Add(WeatherKind.PolarNight, PolarNight);
        Add(WeatherKind.GaleBreath, GaleBreath);
        Add(WeatherKind.SunFall, SunFall);
        Add(WeatherKind.NoMist, NoMist);
        Add(WeatherKind.Sootfall, Sootfall);
        Add(WeatherKind.AbyssalSurge, AbyssalSurge);
        SerpentMultiplier = ConfigOptions.Config(Constants.Groups.SerpentMultiplier);
        FishMultiplier = ConfigOptions.Config(Constants.Groups.FishMultiplier);
        BigFishMultiplier = ConfigOptions.Config(Constants.Groups.BigFishMultiplier);
        Add(WeatherKind.NaturesRebound, NaturesRebound);
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
        Add(WeatherKind.Kindred, Kindred);
        KindredGrowthMultiplier = ConfigOptions.Config(Constants.Groups.KindredGrowthMultiplier);
        KindredCrowdMultiplier = ConfigOptions.Config(Constants.Groups.KindredCrowdMultiplier);
        Add(WeatherKind.HarrowSpur, HarrowSpur);
        HarrowSpurSpeedMultiplier = ConfigOptions.Config(Constants.Groups.HarrowSpurSpeedMultiplier);
        HarrowSpurCapMultiplier = ConfigOptions.Config(Constants.Groups.HarrowSpurCapMultiplier);
        HarrowSpurExcluded = ConfigOptions.Config(Constants.Groups.HarrowSpurExcluded);
        Add(WeatherKind.AmberMend, AmberMend);
        Add(WeatherKind.SovereignWard, SovereignWard);
        SovereignSanctuary = ConfigOptions.Config(Constants.Groups.SovereignSanctuary);
        Add(WeatherKind.HearthHaul, HearthHaul);
        HearthHaulCarryBonus = ConfigOptions.Config(Constants.Groups.HearthHaulCarryBonus);
        HearthHaulFreeStamina = ConfigOptions.Config(Constants.Groups.HearthHaulFreeStamina);
        Add(WeatherKind.AetherClear, AetherClear);
        Add(WeatherKind.ElderSprout, Constants.Groups.ElderSprout);
        Add(WeatherKind.SentrySight, SentrySight);
        SentrySpareAnimals = ConfigOptions.Config(Constants.Groups.SentrySpareAnimals);
        Add(WeatherKind.TaprootSurge, TaprootSurge);
        TaprootSpeedMultiplier = ConfigOptions.Config(Constants.Groups.TaprootSpeedMultiplier);
        TaprootRootMultiplier = ConfigOptions.Config(Constants.Groups.TaprootRootMultiplier);
        Add(WeatherKind.MantleAnchor, MantleAnchor);
        Add(WeatherKind.PrimalHearth, PrimalHearth);
        PrimalHearthSpawnMultiplier = ConfigOptions.Config(Constants.Groups.PrimalHearthSpawnMultiplier);
        PrimalHearthCapacityMultiplier = ConfigOptions.Config(Constants.Groups.PrimalHearthCapacityMultiplier);
        Add(WeatherKind.HearthKindle, HearthKindle);
        KindleRestingMultiplier = ConfigOptions.Config(Constants.Groups.KindleRestingMultiplier);
        KindleRestedMultiplier = ConfigOptions.Config(Constants.Groups.KindleRestedMultiplier);
        Add(WeatherKind.Unmaking, Unmaking);
        UnmakingReturn = ConfigOptions.Config(Constants.Groups.UnmakingReturn);
        UnmakingUpgradeMaterials = ConfigOptions.Config(Constants.Groups.UnmakingUpgradeMaterials);
        Add(WeatherKind.LeySurvey, Constants.Groups.LeySurvey);
        LeySurveyRefusedSeconds = ConfigOptions.Config(Constants.Groups.LeySurveyRefusedSeconds);
        Add(WeatherKind.Grounding, Grounding);
        Add(WeatherKind.CryptDelver, CryptDelver);
        LootMultiplier = ConfigOptions.Config(Constants.Groups.LootMultiplier);
        OneExtraChance = ConfigOptions.Config(Constants.Groups.OneExtraChance);
        TwoExtraChance = ConfigOptions.Config(Constants.Groups.TwoExtraChance);
        Add(WeatherKind.GlacialSatiety, GlacialSatiety);
        FoodFloor = ConfigOptions.Config(Constants.Groups.FoodFloor);
        FoodDurationMultiplier = ConfigOptions.Config(Constants.Groups.FoodDurationMultiplier);
        Add(WeatherKind.VeinShatter, VeinShatter);
        VeinSpread = ConfigOptions.Config(Constants.Groups.VeinSpread);
        VeinShatterAt = ConfigOptions.Config(Constants.Groups.VeinShatterAt);
        VeinExcluded = ConfigOptions.Config(Constants.Groups.VeinExcluded);
        Add(WeatherKind.CarapaceTemper, CarapaceTemper);
        DurabilityMultiplier = ConfigOptions.Config(Constants.Groups.DurabilityMultiplier);
        Add(WeatherKind.GraveBind, GraveBind);
        Add(WeatherKind.FortunesBoon, FortunesBoon);
        LuckMultiplier = ConfigOptions.Config(Constants.Groups.LuckMultiplier);
        ForgeBreakPenalty = ConfigOptions.Config(Constants.Groups.ForgeBreakPenalty);
        Add(WeatherKind.Luminary, Luminary);
        LuminaryRange = ConfigOptions.Config(Constants.Groups.LuminaryRange);
        Add(WeatherKind.AbyssalDraw, AbyssalDraw);
        DrawRange = ConfigOptions.Config(Constants.Groups.DrawRange);
        Add(WeatherKind.FairWind, FairWind);
        Add(WeatherKind.HushFelling, HushFelling);
        HushNoticeMultiplier = ConfigOptions.Config(Constants.Groups.HushNoticeMultiplier);
        Add(WeatherKind.DraughtFlush, DraughtFlush);
        DraughtRecoveryMultiplier = ConfigOptions.Config(Constants.Groups.DraughtRecoveryMultiplier);
        DraughtBuffMultiplier = ConfigOptions.Config(Constants.Groups.DraughtBuffMultiplier);
        Add(WeatherKind.SoulTether, Constants.Groups.SoulTether);
        TetherLeaveWithin = ConfigOptions.Config(Constants.Groups.TetherLeaveWithin);
        Add(WeatherKind.TundraShroud, Constants.Groups.TundraShroud);
        ShroudDryingMultiplier = ConfigOptions.Config(Constants.Groups.ShroudDryingMultiplier);
        Add(WeatherKind.ScaldDrift, ScaldDrift);
        ScaldDamageMultiplier = ConfigOptions.Config(Constants.Groups.ScaldDamageMultiplier);
        ScaldTickSlowdown = ConfigOptions.Config(Constants.Groups.ScaldTickSlowdown);
        ScaldLavaDamageMultiplier = ConfigOptions.Config(Constants.Groups.ScaldLavaDamageMultiplier);
        ScaldLavaTickSlowdown = ConfigOptions.Config(Constants.Groups.ScaldLavaTickSlowdown);
        Add(WeatherKind.TetherLock, TetherLock);
        TetherStrengthMultiplier = ConfigOptions.Config(Constants.Groups.TetherStrengthMultiplier);
        TetherStaminaMultiplier = ConfigOptions.Config(Constants.Groups.TetherStaminaMultiplier);
        TetherReelMultiplier = ConfigOptions.Config(Constants.Groups.TetherReelMultiplier);
        Add(WeatherKind.StriderGirth, StriderGirth);
        StriderRegenMultiplier = ConfigOptions.Config(Constants.Groups.StriderRegenMultiplier);
        StriderAttackStamina = ConfigOptions.Config(Constants.Groups.StriderAttackStamina);
        StriderSecondaryStamina = ConfigOptions.Config(Constants.Groups.StriderSecondaryStamina);
        StriderSwappedMounts = ConfigOptions.Config(Constants.Groups.StriderSwappedMounts);
        Add(WeatherKind.SecondKindle, SecondKindle);
        KindleHealthShare = ConfigOptions.Config(Constants.Groups.KindleHealthShare);
        KindleKnockbackRadius = ConfigOptions.Config(Constants.Groups.KindleKnockbackRadius);
        Add(WeatherKind.PlumeWard, PlumeWard);
        PlumeWardFallDamage = ConfigOptions.Config(Constants.Groups.PlumeWardFallDamage);
        Add(WeatherKind.MorgenPouch, MorgenPouch);
        MorgenPouchCount = ConfigOptions.Config(Constants.Groups.MorgenPouchCount);
        MorgenPouchRange = ConfigOptions.Config(Constants.Groups.MorgenPouchRange);
        MorgenPouchReleaseCooldown = ConfigOptions.Config(Constants.Groups.MorgenPouchReleaseCooldown);
        MorgenPouchCaptureCooldown = ConfigOptions.Config(Constants.Groups.MorgenPouchCaptureCooldown);
        Add(WeatherKind.TendrilDraw, TendrilDraw);
        TendrilAmmoChance = ConfigOptions.Config(Constants.Groups.TendrilAmmoChance);
        TendrilReturnSpeed = ConfigOptions.Config(Constants.Groups.TendrilReturnSpeed);
        Add(WeatherKind.Skal, Skal);
        SkalCooldown = ConfigOptions.Config(Constants.Groups.SkalCooldown);
        Add(WeatherKind.SkyCall, SkyCall);
        Add(WeatherKind.AnvilSpark, AnvilSpark);
        Add(WeatherKind.Eldhrimnir, Eldhrimnir);
        EldhrimnirFreeUse = ConfigOptions.Config(Constants.Groups.EldhrimnirFreeUse);
        Add(WeatherKind.RallyHorn, Constants.Groups.RallyHorn);
        RallyCallSeconds = ConfigOptions.Config(Constants.Groups.RallyCallSeconds);
        Add(WeatherKind.Crimson, Crimson);
        CrimsonPause = ConfigOptions.Config(Constants.Groups.CrimsonPause);
        CrimsonSpawnMultiplier = ConfigOptions.Config(Constants.Groups.CrimsonSpawnMultiplier);
        CrimsonCatchUp = ConfigOptions.Config(Constants.Groups.CrimsonCatchUp);
        CrimsonRewardItem = ConfigOptions.Config(Constants.Groups.CrimsonRewardItem);
        CrimsonRewardPerKill = ConfigOptions.Config(Constants.Groups.CrimsonRewardPerKill);
        CrimsonRewardPerPlayer = ConfigOptions.Config(Constants.Groups.CrimsonRewardPerPlayer);
        CrimsonMaxPlayers = ConfigOptions.Config(Constants.Groups.CrimsonMaxPlayers);
        CrimsonEnvironment = ConfigOptions.Config(Constants.Groups.CrimsonEnvironment);
        BloodmoonBase = ConfigOptions.Config(Constants.Groups.BloodmoonBase);
        Add(WeatherKind.Ragnarok, Ragnarok);
        RagnarokBosses = ConfigOptions.Config(Constants.Groups.RagnarokBosses);
        RagnarokBossCount = ConfigOptions.Config(Constants.Groups.RagnarokBossCount);
        RagnarokPause = ConfigOptions.Config(Constants.Groups.RagnarokPause);
        RagnarokHealthMultiplier = ConfigOptions.Config(Constants.Groups.RagnarokHealthMultiplier);
        RagnarokDamageMultiplier = ConfigOptions.Config(Constants.Groups.RagnarokDamageMultiplier);
        RagnarokRewardPerPlayer = ConfigOptions.Config(Constants.Groups.RagnarokRewardPerPlayer);
        RagnarokMaxPlayers = ConfigOptions.Config(Constants.Groups.RagnarokMaxPlayers);
        RagnarokLootPerStar = ConfigOptions.Config(Constants.Groups.RagnarokLootPerStar);
        RagnarokBossLoot.Clear();
        foreach ((string boss, ConfigInfo<string>[] tiers) in Constants.Groups.RagnarokBossLoot)
            RagnarokBossLoot[boss] = Array.ConvertAll(tiers, tier => ConfigOptions.Config(tier));
        RagnarokChests = ConfigOptions.Config(Constants.Groups.RagnarokChests);
        RagnarokEnvironment = ConfigOptions.Config(Constants.Groups.RagnarokEnvironment);
        RagnarokSkyBase = ConfigOptions.Config(Constants.Groups.RagnarokSkyBase);
        Add(WeatherKind.HoardBane, HoardBane);
        HoardBaneEliteRadius = ConfigOptions.Config(Constants.Groups.HoardBaneEliteRadius);
        HoardBaneHealthShare = ConfigOptions.Config(Constants.Groups.HoardBaneHealthShare);
        HoardBaneDamageShare = ConfigOptions.Config(Constants.Groups.HoardBaneDamageShare);
        HoardBaneCoins = Array.ConvertAll(Constants.Groups.HoardBaneCoins, ConfigOptions.Config);
        HoardBaneTreasure = Array.ConvertAll(Constants.Groups.HoardBaneTreasure, ConfigOptions.Config);
        HoardBaneTreasureRolls = Array.ConvertAll(Constants.Groups.HoardBaneTreasureRolls, ConfigOptions.Config);
        HoardBaneRewardPerPlayer = ConfigOptions.Config(Constants.Groups.HoardBaneRewardPerPlayer);
        HoardBaneMaxPlayers = ConfigOptions.Config(Constants.Groups.HoardBaneMaxPlayers);
        HoardBaneIdols = ConfigOptions.Config(Constants.Groups.HoardBaneIdols);
        HoardBaneEnvironment = ConfigOptions.Config(Constants.Groups.HoardBaneEnvironment);
        HoardBaneSkyBase = ConfigOptions.Config(Constants.Groups.HoardBaneSkyBase);
        Add(WeatherKind.Sanctuary, Sanctuary);
        SanctuaryOnlyInside = ConfigOptions.Config(Constants.Groups.SanctuaryOnlyInside);

        // 8 - Interface
        ShowStatus = ConfigOptions.Config(Display.ShowStatus);
        ShowLeverHint = ConfigOptions.Config(Display.ShowLeverHint);
        ShowHatchHint = ConfigOptions.Config(Display.ShowHatchHint);
        ShowItemHighlight = ConfigOptions.Config(Display.ShowItemHighlight);
        ShowExplanations = ConfigOptions.Config(Display.ShowExplanations);
        ShowMessages = ConfigOptions.Config(Display.ShowMessages);
        ShowRiteUnlocks = ConfigOptions.Config(Display.ShowRiteUnlocks);
        GraveReequip = ConfigOptions.Config(Display.GraveReequip);
        MorgenPouchShortcut = ConfigOptions.Config(Display.MorgenPouchShortcut);
        RallyCallShortcut = ConfigOptions.Config(Display.RallyCallShortcut);
        RallyRefuseShortcut = ConfigOptions.Config(Display.RallyRefuseShortcut);
        DomainBorderShortcut = ConfigOptions.Config(Display.DomainBorderShortcut);

        // 9 - Effects
        ShowBlessingLightning = ConfigOptions.Config(Display.ShowBlessingLightning);
        ShowCreatureIcons = ConfigOptions.Config(Display.ShowCreatureIcons);
        ShowDomainEffects = ConfigOptions.Config(Display.ShowDomainEffects);
        AlwaysShowDomainBorder = ConfigOptions.Config(Display.AlwaysShowDomainBorder);
        ShowVerdantSparkles = ConfigOptions.Config(Display.ShowVerdantSparkles);
        ShowVatBoilMotes = ConfigOptions.Config(Display.ShowVatBoilMotes);
        ShowPitchBellowsMotes = ConfigOptions.Config(Display.ShowPitchBellowsMotes);
        PitchBellowsFireTint = ConfigOptions.Config(Display.PitchBellowsFireTint);
        ShowKindredHearts = ConfigOptions.Config(Display.ShowKindredHearts);
        ShowHarrowSpurMotes = ConfigOptions.Config(Display.ShowHarrowSpurMotes);
        ShowHearthHaulFeathers = ConfigOptions.Config(Display.ShowHearthHaulFeathers);
        ShowSentrySightMotes = ConfigOptions.Config(Display.ShowSentrySightMotes);
        ShowTaprootSurgeMotes = ConfigOptions.Config(Display.ShowTaprootSurgeMotes);
        TaprootSurgeFireTint = ConfigOptions.Config(Display.TaprootSurgeFireTint);
        ShowMantleAnchorMotes = ConfigOptions.Config(Display.ShowMantleAnchorMotes);
        ShowPrimalHearthMotes = ConfigOptions.Config(Display.ShowPrimalHearthMotes);
        PrimalHearthGlow = ConfigOptions.Config(Display.PrimalHearthGlow);
        ShowHearthKindleMotes = ConfigOptions.Config(Display.ShowHearthKindleMotes);
        ShowPlumeWardDown = ConfigOptions.Config(Display.ShowPlumeWardDown);
        ShowMorgenPouchGlow = ConfigOptions.Config(Display.ShowMorgenPouchGlow);
        GraveBeacon = ConfigOptions.Config(Display.GraveBeacon);
        LuminaryStrength = ConfigOptions.Config(Display.LuminaryStrength);
        LuminaryBodyLight = ConfigOptions.Config(Display.LuminaryBodyLight);
        LuminaryShadows = ConfigOptions.Config(Display.LuminaryShadows);
        CrimsonShield = ConfigOptions.Config(Display.CrimsonShield);
        RagnarokShield = ConfigOptions.Config(Display.RagnarokShield);
        HoardBaneShield = ConfigOptions.Config(Display.HoardBaneShield);

        ConfigOptions.Finish(Constants.Plugin.ModVersion);
    }

    private static void Add(WeatherKind kind, RiteSettings group) => Rites[kind] = new WeatherConfig(group);

    internal static WeatherConfig? For(WeatherKind kind) => Rites.TryGetValue(kind, out WeatherConfig config) ? config : null;

    // Turned on in the settings, and able to work here: on a server without the mod only some rites do (see
    // WeatherKinds.WorksWithoutServerMod); the rest are off as if turned off, hidden from the rites page and Compendium.
    internal static bool IsEnabled(WeatherKind kind) =>
        For(kind)?.Enabled.IsOn() == true && (!RpcHandlers.ServerLacksMod || WeatherKinds.WorksWithoutServerMod(kind));

    // A located rite's radius in meters (0 for rites that work everywhere): the Domain rites share one, and the
    // Ley-Survey Rite shows the widest arena.
    internal static float Radius(WeatherKind kind) =>
        kind == WeatherKind.LeySurvey ? ArenaRadius
        : WeatherKinds.Group(kind) == RiteGroup.Domain ? DomainRadius.Value : For(kind)?.Radius?.Value ?? 0f;

    // The widest of the Crimson, Ragnarök and Hoard-Bane Rites' arenas.
    internal static float ArenaRadius =>
        Math.Max(Radius(WeatherKind.Crimson), Math.Max(Radius(WeatherKind.Ragnarok), Radius(WeatherKind.HoardBane)));

    private static readonly PrefabList CarrionList = new();
    private static readonly PrefabList PackwardList = new();
    private static readonly PrefabList VeinList = new();
    private static readonly PrefabList HarrowSpurList = new();
    private static readonly PrefabList StriderSwappedList = new();

    // The prefab hashes of the mounts whose two attacks swap keys under the Strider-Girth Rite.
    internal static HashSet<int> StriderSwappedPrefabs() => StriderSwappedList.Read(StriderSwappedMounts.Value);

    // The prefab hashes of the spawners the Harrow-Spur Rite leaves alone.
    internal static HashSet<int> HarrowSpurExcludedPrefabs() => HarrowSpurList.Read(HarrowSpurExcluded.Value);

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
    internal bool DistinctKeys { get; }

    private string? _parsedText;
    private EnvironmentTable? _table;

    internal float Seconds => (Minutes?.Value ?? 0f) * 60f;

    internal WeatherConfig(RiteSettings group)
    {
        Enabled = ConfigOptions.Config(group.Enabled);
        DistinctKeys = group.DistinctKeys;
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
