using BepInEx.Configuration;
using UnityEngine;
using JoksterCube.ChestKit.Data;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;

namespace JoksterCube.TempestRite.Settings;

internal static class Constants
{
    // Every text shown in game, in the selected language. English is Translations/English.json (embedded in the DLL);
    // other languages are files next to it, see ChestKit's Translations. Config descriptions stay in English.
    private static readonly Translations Text = new(typeof(Constants).Assembly, "TempestRite.English.json");

    // Colors are kept out of the translated texts, so translators only see words.
    private static string Colored(string color, string text) => $"<color={color}>{text}</color>";

    internal static class Plugin
    {
        internal const string ModName = "TempestRite";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "JoksterCube";
        internal const string ModGUID = $"{Author}.{ModName}";
        internal const string Description = "Perform rites at the obliterator: sacrifice an item to change the weather, hold the sun, call raids, grow crops faster, receive blessings and more.";
        internal const string Copyright = "Copyright ©  2026";
        internal const string Guid = "1e7b1458-bfca-49ce-aca8-9eef270f87e6";

        internal const string ConfigFileName = $"{ModGUID}.cfg";
        internal const string VersionCheckRpc = $"{ModName}_VersionCheck";
    }

    internal static class Rpcs
    {
        // Client -> server: please perform this rite.
        internal const string Request = $"{Plugin.ModName}_Request";
        // Client -> server: send me the rites going on.
        internal const string StateRequest = $"{Plugin.ModName}_StateRequest";
        // Server -> client: the rites going on, and what was just performed (blessings too, for the players near it).
        internal const string State = $"{Plugin.ModName}_State";
        // Server -> client: a request was refused.
        internal const string Refused = $"{Plugin.ModName}_Refused";
        // Vein owner -> striking player: a Vein-Shatter shatter broke this many more pieces (wear the pickaxe).
        internal const string VeinWear = $"{Plugin.ModName}_VeinWear";
        // Player -> everyone: show a kinship rite's effect (sparks and fire, or lightning) on me.
        internal const string KinshipShown = $"{Plugin.ModName}_KinshipShown";
        // Player -> everyone: I answered the Rally-Horn Rite's call and arrived here (lightning where I stand).
        internal const string RallyArrived = $"{Plugin.ModName}_RallyArrived";
        // Server -> client: whether a Rally-Horn call waits for answers (only one at a time).
        internal const string RallyPending = $"{Plugin.ModName}_RallyPending";
        // Client -> server: my player is done with the Rally-Horn call (passed by, answered, refused or let it fade).
        internal const string RallyDone = $"{Plugin.ModName}_RallyDone";
        // Creature owner -> server: a creature died within the Crimson Rite's bubble.
        internal const string CrimsonKill = $"{Plugin.ModName}_CrimsonKill";
        // Server -> everyone: the Crimson Rite's kill count is now this.
        internal const string CrimsonKills = $"{Plugin.ModName}_CrimsonKills";
        // Server -> everyone: a Domain rite ended, its obliterator destroyed (the rite, where).
        internal const string DomainBroken = $"{Plugin.ModName}_DomainBroken";
        // Server -> everyone: the Crimson Rite ended (how, kills, reward).
        internal const string CrimsonEnded = $"{Plugin.ModName}_CrimsonEnded";
        // Server -> everyone: the Crimson Rite's next raid comes (center).
        internal const string CrimsonWave = $"{Plugin.ModName}_CrimsonWave";
        // Server -> everyone: the Crimson Rite's raid (name) is caught up this many steps (center).
        internal const string CrimsonCatchUp = $"{Plugin.ModName}_CrimsonCatchUp";
        // Server -> the obliterator's owner (a ZNetView RPC): put the Crimson Rite's reward inside.
        internal const string CrimsonReward = $"{Plugin.ModName}_CrimsonReward";
        // Server -> each player within the Ragnarök Rite's bubble: which of these boss keys do you have?
        internal const string RagnarokAsk = $"{Plugin.ModName}_RagnarokAsk";
        // Player -> server: the boss keys I have, of those asked.
        internal const string RagnarokAnswer = $"{Plugin.ModName}_RagnarokAnswer";
        // Server -> one player inside: spawn this boss (prefab, center, ticket).
        internal const string RagnarokSpawn = $"{Plugin.ModName}_RagnarokSpawn";
        // That player -> server: the boss is up (its ZDO, the ticket).
        internal const string RagnarokSpawned = $"{Plugin.ModName}_RagnarokSpawned";
        // Boss owner -> server: the Ragnarök Rite's boss died.
        internal const string RagnarokDown = $"{Plugin.ModName}_RagnarokDown";
        // Server -> everyone: the Ragnarök Rite's bosses were picked (how many, center).
        internal const string RagnarokBegun = $"{Plugin.ModName}_RagnarokBegun";
        // Server -> everyone: a boss rises or falls (prefab, which one, of how many, fell, center).
        internal const string RagnarokBoss = $"{Plugin.ModName}_RagnarokBoss";
        // Server -> everyone: the Ragnarök Rite ended (how, whether treasure was won, center).
        internal const string RagnarokEnded = $"{Plugin.ModName}_RagnarokEnded";
        // Server -> the obliterator's owner (a ZNetView RPC): put the Ragnarök Rite's treasure inside.
        internal const string RagnarokReward = $"{Plugin.ModName}_RagnarokReward";
        // Boss owner -> server: a Ragnarök Rite boss fell out of the bubble (through the ground) and couldn't be put back.
        internal const string RagnarokLost = $"{Plugin.ModName}_RagnarokLost";
        // Player -> server, right before asking for the rite: the chest burnt for the Hoard-Bane Rite (prefab, where).
        internal const string HoardBaneChest = $"{Plugin.ModName}_HoardBaneChest";
        // Server -> one player inside: spawn the Hoard-Bane Rite's fight, or part of it (see HoardBaneArena).
        internal const string HoardBaneSpawn = $"{Plugin.ModName}_HoardBaneSpawn";
        // That player -> server: they are up (the ticket, their ZDOs).
        internal const string HoardBaneSpawned = $"{Plugin.ModName}_HoardBaneSpawned";
        // Server -> one player inside: spawn one more of the Hoard-Bane Rite's followers (ticket, encounter, center, where).
        internal const string HoardBaneReinforce = $"{Plugin.ModName}_HoardBaneReinforce";
        // That player -> server: it is up (the ticket, its ZDO).
        internal const string HoardBaneReinforced = $"{Plugin.ModName}_HoardBaneReinforced";
        // Creature owner -> server: one of the Hoard-Bane Rite's elites or minibosses died.
        internal const string HoardBaneDown = $"{Plugin.ModName}_HoardBaneDown";
        // Creature owner -> server: an elite or miniboss fell out of the bubble and couldn't be put back.
        internal const string HoardBaneLost = $"{Plugin.ModName}_HoardBaneLost";
        // Server -> everyone: the Hoard-Bane Rite's fight begins (the minibosses, how many elites, center).
        internal const string HoardBaneBegun = $"{Plugin.ModName}_HoardBaneBegun";
        // Server -> everyone: an elite or miniboss fell, the shield broke, a miniboss came back (see HoardBaneArena).
        internal const string HoardBaneNews = $"{Plugin.ModName}_HoardBaneNews";
        // Server -> everyone, again and again once every elite fell: the minibosses' shield is down (center).
        internal const string HoardBaneUnshield = $"{Plugin.ModName}_HoardBaneUnshield";
        // Server -> everyone: the Hoard-Bane Rite ended (how, whether treasure was won, center).
        internal const string HoardBaneEnded = $"{Plugin.ModName}_HoardBaneEnded";
        // Server -> the obliterator's owner (a ZNetView RPC): put the Hoard-Bane Rite's treasure inside.
        internal const string HoardBaneReward = $"{Plugin.ModName}_HoardBaneReward";
        // Server -> the player who burnt the chest: the Hoard-Bane Rite failed with no obliterator left to hold the
        // chest; it goes back to them (the chest's prefab).
        internal const string HoardBaneChestBack = $"{Plugin.ModName}_HoardBaneChestBack";
        // Server -> everyone, every few seconds: whether a boss fight goes on, and who was inside the Crimson, Ragnarök or
        // Hoard-Bane Rite going on as it began (see IncursionWatch).
        internal const string IncursionWatch = $"{Plugin.ModName}_IncursionWatch";
        // Player -> server, as the lever is pulled for a Crimson, Ragnarök or Hoard-Bane Rite: seal its bubble (kind,
        // where, its key items).
        internal const string IncursionPull = $"{Plugin.ModName}_IncursionPull";
        // Server -> everyone: that rite's bubble is sealed until it is performed (kind, where, seconds; None: no more).
        internal const string IncursionPending = $"{Plugin.ModName}_IncursionPending";
        // Server -> the obliterator's owner (a ZNetView RPC): a rite refused after its lever was pulled; put its key items back.
        internal const string ObliteratorRefund = $"{Plugin.ModName}_ObliteratorRefund";
        // Obliterator owner -> the player who pulled its lever (a ZNetView RPC), right as it burns: the rites whose key
        // items are burning now (only these are performed), the Hoard-Bane Rite's chest and the incursion key items burnt.
        internal const string ObliteratorBurnt = $"{Plugin.ModName}_ObliteratorBurnt";
        // Piece owner -> everyone: a warded piece was hit, glow it ward-blue with the ward's sound (the piece, where the hit was).
        internal const string WardFlash = $"{Plugin.ModName}_WardFlash";
        // Blessed player -> server: pull my tombstones to this obliterator, onto these free spots (the Soul-Tether Rite).
        internal const string SoulTether = $"{Plugin.ModName}_SoulTether";
        // Server -> that player: how many tombstones were pulled, how many already lay there, and where the pulled ones lay
        // (to cross out their death pins on the map).
        internal const string SoulTetherDone = $"{Plugin.ModName}_SoulTetherDone";
        // Server -> everyone: a stump the Elder-Sprout Rite takes away (where, its prefab, the sapling's prefab), for its puff.
        internal const string ElderSproutPuff = $"{Plugin.ModName}_ElderSproutPuff";
        // Player -> everyone: the Plume-Ward Rite softened my landing here, for its puff of down.
        internal const string PlumeLanding = $"{Plugin.ModName}_PlumeLanding";
        // Player -> everyone: the Second-Kindle Rite saved me from a killing blow, for its burst of fire.
        internal const string SecondKindleBurst = $"{Plugin.ModName}_SecondKindleBurst";
        // Player -> everyone: creatures went into or came out of my Morgen-Pouch here, for its puff.
        internal const string MorgenPouchPuff = $"{Plugin.ModName}_MorgenPouchPuff";
        internal const string MorgenPouchFlight = $"{Plugin.ModName}_MorgenPouchFlight";
    }

    internal static class Messages
    {
        internal static string ClearName => Text.Get("clear_name");
        internal static string ThunderstormName => Text.Get("thunderstorm_name");
        internal static string RainName => Text.Get("rain_name");
        internal static string MistName => Text.Get("mist_name");
        internal static string AuroraName => Text.Get("aurora_name");
        internal static string PolarDayName => Text.Get("polar_day_name");
        internal static string PolarNightName => Text.Get("polar_night_name");
        internal static string FairWindName => Text.Get("fair_wind_name");
        internal static string HushFellingName => Text.Get("hush_felling_name");
        internal static string DraughtFlushName => Text.Get("draught_flush_name");
        internal static string SoulTetherName => Text.Get("soul_tether_name");
        internal static string TundraShroudName => Text.Get("tundra_shroud_name");
        internal static string GaleBreathName => Text.Get("gale_breath_name");
        internal static string NoMistName => Text.Get("no_mist_name");
        internal static string SootfallName => Text.Get("sootfall_name");
        internal static string CryptDelverName => Text.Get("crypt_delver_name");
        internal static string PackwardName => Text.Get("packward_name");
        internal static string CarrionName => Text.Get("carrion_name");
        internal static string VerdantName => Text.Get("verdant_name");
        internal static string CrimsonName => Text.Get("crimson_name");
        internal static string SanctuaryName => Text.Get("sanctuary_name");
        internal static string RagnarokName => Text.Get("ragnarok_name");
        internal static string HoardBaneName => Text.Get("hoard_bane_name");
        internal static string ResetName => Text.Get("reset_name");
        internal static string SunFallName => Text.Get("sun_fall_name");
        internal static string NaturesReboundName => Text.Get("natures_rebound_name");
        internal static string GroundingName => Text.Get("grounding_name");
        internal static string VatBoilName => Text.Get("vat_boil_name");
        internal static string PitchBellowsName => Text.Get("pitch_bellows_name");
        internal static string KindredName => Text.Get("kindred_name");
        internal static string HarrowSpurName => Text.Get("harrow_spur_name");
        internal static string AmberMendName => Text.Get("amber_mend_name");
        internal static string AbyssalSurgeName => Text.Get("abyssal_surge_name");
        internal static string GlacialSatietyName => Text.Get("glacial_satiety_name");
        internal static string VeinShatterName => Text.Get("vein_shatter_name");
        internal static string CarapaceTemperName => Text.Get("carapace_temper_name");
        internal static string GraveBindName => Text.Get("grave_bind_name");
        internal static string FortunesBoonName => Text.Get("fortunes_boon_name");
        internal static string LuminaryName => Text.Get("luminary_name");
        internal static string SovereignWardName => Text.Get("sovereign_ward_name");
        internal static string HearthHaulName => Text.Get("hearth_haul_name");
        internal static string AetherClearName => Text.Get("aether_clear_name");
        internal static string AbyssalDrawName => Text.Get("abyssal_draw_name");
        internal static string UnmakingName => Text.Get("unmaking_name");
        internal static string ElderSproutName => Text.Get("elder_sprout_name");
        internal static string PlumeWardName => Text.Get("plume_ward_name");
        internal static string SentrySightName => Text.Get("sentry_sight_name");
        internal static string TaprootSurgeName => Text.Get("taproot_surge_name");
        internal static string MantleAnchorName => Text.Get("mantle_anchor_name");
        internal static string PrimalHearthName => Text.Get("primal_hearth_name");
        internal static string HearthKindleName => Text.Get("hearth_kindle_name");
        internal static string ScaldDriftName => Text.Get("scald_drift_name");
        internal static string TetherLockName => Text.Get("tether_lock_name");
        internal static string StriderGirthName => Text.Get("strider_girth_name");
        internal static string SecondKindleName => Text.Get("second_kindle_name");
        internal static string MorgenPouchName => Text.Get("morgen_pouch_name");
        internal static string TendrilDrawName => Text.Get("tendril_draw_name");
        internal static string SkalName => Text.Get("skal_name");
        internal static string SkyCallName => Text.Get("sky_call_name");
        internal static string AnvilSparkName => Text.Get("anvil_spark_name");
        internal static string EldhrimnirName => Text.Get("eldhrimnir_name");
        internal static string RallyHornName => Text.Get("rally_horn_name");

        // What each rite does, in a few words: "clear skies".
        internal static string ClearPlain => Text.Get("clear_plain");
        internal static string ThunderstormPlain => Text.Get("thunderstorm_plain");
        internal static string RainPlain => Text.Get("rain_plain");
        internal static string MistPlain => Text.Get("mist_plain");
        internal static string AuroraPlain => Text.Get("aurora_plain");
        internal static string PolarDayPlain => Text.Get("polar_day_plain");
        internal static string PolarNightPlain => Text.Get("polar_night_plain");
        internal static string GaleBreathPlain => Text.Get("gale_breath_plain");
        internal static string NoMistPlain => Text.Get("no_mist_plain");
        internal static string SootfallPlain => Text.Get("sootfall_plain");
        internal static string AbyssalSurgePlain => Text.Get("abyssal_surge_plain");
        internal static string PackwardPlain => Text.Get("packward_plain");
        internal static string CarrionPlain => Text.Get("carrion_plain");
        internal static string VerdantPlain => Text.Get("verdant_plain");
        internal static string VatBoilPlain => Text.Get("vat_boil_plain");
        internal static string PitchBellowsPlain => Text.Get("pitch_bellows_plain");
        internal static string KindredPlain => Text.Get("kindred_plain");
        internal static string HarrowSpurPlain => Text.Get("harrow_spur_plain");
        internal static string AmberMendPlain => Text.Get("amber_mend_plain");
        internal static string SovereignWardPlain => Text.Get("sovereign_ward_plain");
        internal static string HearthHaulPlain => Text.Get("hearth_haul_plain");
        internal static string AetherClearPlain => Text.Get("aether_clear_plain");
        internal static string GroundingPlain => Text.Get("grounding_plain");
        internal static string CryptDelverPlain => Text.Get("crypt_delver_plain");
        internal static string GlacialSatietyPlain => Text.Get("glacial_satiety_plain");
        internal static string VeinShatterPlain => Text.Get("vein_shatter_plain");
        internal static string CarapaceTemperPlain => Text.Get("carapace_temper_plain");
        internal static string GraveBindPlain => Text.Get("grave_bind_plain");
        internal static string FortunesBoonPlain => Text.Get("fortunes_boon_plain");
        internal static string LuminaryPlain => Text.Get("luminary_plain");
        internal static string AbyssalDrawPlain => Text.Get("abyssal_draw_plain");
        internal static string FairWindPlain => Text.Get("fair_wind_plain");
        internal static string HushFellingPlain => Text.Get("hush_felling_plain");
        internal static string DraughtFlushPlain => Text.Get("draught_flush_plain");
        internal static string SoulTetherPlain => Text.Get("soul_tether_plain");
        internal static string TundraShroudPlain => Text.Get("tundra_shroud_plain");
        internal static string SkalPlain => Text.Get("skal_plain");
        internal static string SkyCallPlain => Text.Get("sky_call_plain");
        internal static string AnvilSparkPlain => Text.Get("anvil_spark_plain");
        internal static string EldhrimnirPlain => Text.Get("eldhrimnir_plain");
        internal static string RallyHornPlain => Text.Get("rally_horn_plain");
        internal static string CrimsonPlain => Text.Get("crimson_plain");
        internal static string RagnarokPlain => Text.Get("ragnarok_plain");
        internal static string HoardBanePlain => Text.Get("hoard_bane_plain");
        internal static string SanctuaryPlain => Text.Get("sanctuary_plain");
        internal static string ResetPlain => Text.Get("reset_plain");
        internal static string SunFallPlain => Text.Get("sun_fall_plain");
        internal static string NaturesReboundPlain => Text.Get("natures_rebound_plain");
        internal static string UnmakingPlain => Text.Get("unmaking_plain");
        internal static string ElderSproutPlain => Text.Get("elder_sprout_plain");
        internal static string PlumeWardPlain => Text.Get("plume_ward_plain");
        internal static string SentrySightPlain => Text.Get("sentry_sight_plain");
        internal static string TaprootSurgePlain => Text.Get("taproot_surge_plain");
        internal static string MantleAnchorPlain => Text.Get("mantle_anchor_plain");
        internal static string PrimalHearthPlain => Text.Get("primal_hearth_plain");
        internal static string HearthKindlePlain => Text.Get("hearth_kindle_plain");
        internal static string ScaldDriftPlain => Text.Get("scald_drift_plain");
        internal static string TetherLockPlain => Text.Get("tether_lock_plain");
        internal static string StriderGirthPlain => Text.Get("strider_girth_plain");
        internal static string SecondKindlePlain => Text.Get("second_kindle_plain");
        internal static string MorgenPouchPlain => Text.Get("morgen_pouch_plain");
        internal static string TendrilDrawPlain => Text.Get("tendril_draw_plain");

        // What each rite does, told with a little lore, on the rites page.
        internal static string ClearFlavour => Text.Get("clear_flavour");
        internal static string ThunderstormFlavour => Text.Get("thunderstorm_flavour");
        internal static string RainFlavour => Text.Get("rain_flavour");
        internal static string MistFlavour => Text.Get("mist_flavour");
        internal static string AuroraFlavour => Text.Get("aurora_flavour");
        internal static string PolarDayFlavour => Text.Get("polar_day_flavour");
        internal static string PolarNightFlavour => Text.Get("polar_night_flavour");
        internal static string GaleBreathFlavour => Text.Get("gale_breath_flavour");
        internal static string NoMistFlavour => Text.Get("no_mist_flavour");
        internal static string SootfallFlavour => Text.Get("sootfall_flavour");
        internal static string AbyssalSurgeFlavour => Text.Get("abyssal_surge_flavour");
        internal static string PackwardFlavour => Text.Get("packward_flavour");
        internal static string CarrionFlavour => Text.Get("carrion_flavour");
        internal static string VerdantFlavour => Text.Get("verdant_flavour");
        internal static string VatBoilFlavour => Text.Get("vat_boil_flavour");
        internal static string PitchBellowsFlavour => Text.Get("pitch_bellows_flavour");
        internal static string KindredFlavour => Text.Get("kindred_flavour");
        internal static string HarrowSpurFlavour => Text.Get("harrow_spur_flavour");
        internal static string AmberMendFlavour => Text.Get("amber_mend_flavour");
        internal static string SovereignWardFlavour => Text.Get("sovereign_ward_flavour");
        internal static string HearthHaulFlavour => Text.Get("hearth_haul_flavour");
        internal static string AetherClearFlavour => Text.Get("aether_clear_flavour");
        internal static string GroundingFlavour => Text.Get("grounding_flavour");
        internal static string CryptDelverFlavour => Text.Get("crypt_delver_flavour");
        internal static string GlacialSatietyFlavour => Text.Get("glacial_satiety_flavour");
        internal static string VeinShatterFlavour => Text.Get("vein_shatter_flavour");
        internal static string CarapaceTemperFlavour => Text.Get("carapace_temper_flavour");
        internal static string GraveBindFlavour => Text.Get("grave_bind_flavour");
        internal static string FortunesBoonFlavour => Text.Get("fortunes_boon_flavour");
        internal static string LuminaryFlavour => Text.Get("luminary_flavour");
        internal static string AbyssalDrawFlavour => Text.Get("abyssal_draw_flavour");
        internal static string FairWindFlavour => Text.Get("fair_wind_flavour");
        internal static string HushFellingFlavour => Text.Get("hush_felling_flavour");
        internal static string DraughtFlushFlavour => Text.Get("draught_flush_flavour");
        internal static string SoulTetherFlavour => Text.Get("soul_tether_flavour");
        internal static string TundraShroudFlavour => Text.Get("tundra_shroud_flavour");
        internal static string SkalFlavour => Text.Get("skal_flavour");
        internal static string SkyCallFlavour => Text.Get("sky_call_flavour");
        internal static string AnvilSparkFlavour => Text.Get("anvil_spark_flavour");
        internal static string EldhrimnirFlavour => Text.Get("eldhrimnir_flavour");
        internal static string RallyHornFlavour => Text.Get("rally_horn_flavour");
        internal static string CrimsonFlavour => Text.Get("crimson_flavour");
        internal static string RagnarokFlavour => Text.Get("ragnarok_flavour");
        internal static string HoardBaneFlavour => Text.Get("hoard_bane_flavour");
        internal static string SanctuaryFlavour => Text.Get("sanctuary_flavour");
        internal static string ResetFlavour => Text.Get("reset_flavour");
        internal static string SunFallFlavour => Text.Get("sun_fall_flavour");
        internal static string NaturesReboundFlavour => Text.Get("natures_rebound_flavour");
        internal static string UnmakingFlavour => Text.Get("unmaking_flavour");
        internal static string ElderSproutFlavour => Text.Get("elder_sprout_flavour");
        internal static string PlumeWardFlavour => Text.Get("plume_ward_flavour");
        internal static string SentrySightFlavour => Text.Get("sentry_sight_flavour");
        internal static string TaprootSurgeFlavour => Text.Get("taproot_surge_flavour");
        internal static string MantleAnchorFlavour => Text.Get("mantle_anchor_flavour");
        internal static string PrimalHearthFlavour => Text.Get("primal_hearth_flavour");
        internal static string HearthKindleFlavour => Text.Get("hearth_kindle_flavour");
        internal static string ScaldDriftFlavour => Text.Get("scald_drift_flavour");
        internal static string TetherLockFlavour => Text.Get("tether_lock_flavour");
        internal static string StriderGirthFlavour => Text.Get("strider_girth_flavour");
        internal static string SecondKindleFlavour => Text.Get("second_kindle_flavour");
        internal static string MorgenPouchFlavour => Text.Get("morgen_pouch_flavour");
        internal static string TendrilDrawFlavour => Text.Get("tendril_draw_flavour");

        internal static string GroupAtmospheric => Text.Get("group_atmospheric");
        internal static string GroupCelestial => Text.Get("group_celestial");
        internal static string GroupBiomeMastery => Text.Get("group_biome_mastery");
        internal static string GroupDomain => Text.Get("group_domain");
        internal static string GroupReckoning => Text.Get("group_reckoning");
        internal static string GroupBlessings => Text.Get("group_blessings");
        internal static string GroupKinship => Text.Get("group_kinship");

        // A rite performed, as each player hears of it (see RiteNews): one message per burn.
        // {0}: the rite's name, colored; {1}: what happens (one of the ...Performed below, or a kinship rite's line).
        internal static string RiteByYou => Text.Get("rite_by_you");
        // {0}: who performed it, {1}: the rite's name, colored; {2}: what happens.
        internal static string RiteBy => Text.Get("rite_by");
        // Several rites in one burn.
        internal static string RitesByYou => Text.Get("rites_by_you");
        // {0}: who performed them.
        internal static string RitesBy => Text.Get("rites_by");
        // What happens, said after the rite's name.
        internal static string ClearPerformed => Text.Get("clear_performed");
        internal static string ThunderstormPerformed => Text.Get("thunderstorm_performed");
        internal static string RainPerformed => Text.Get("rain_performed");
        internal static string MistPerformed => Text.Get("mist_performed");
        internal static string AuroraPerformed => Text.Get("aurora_performed");
        internal static string ResetPerformed => Text.Get("reset_performed");
        internal static string PolarDayPerformed => Text.Get("polar_day_performed");
        internal static string PolarNightPerformed => Text.Get("polar_night_performed");
        internal static string GaleBreathPerformed => Text.Get("gale_breath_performed");
        internal static string SunFallPerformed => Text.Get("sun_fall_performed");
        internal static string NoMistPerformed => Text.Get("no_mist_performed");
        internal static string SootfallPerformed => Text.Get("sootfall_performed");
        internal static string AbyssalSurgePerformed => Text.Get("abyssal_surge_performed");
        internal static string NaturesReboundPerformed => Text.Get("natures_rebound_performed");
        internal static string PackwardPerformed => Text.Get("packward_performed");
        internal static string CarrionPerformed => Text.Get("carrion_performed");
        internal static string VerdantPerformed => Text.Get("verdant_performed");
        internal static string VatBoilPerformed => Text.Get("vat_boil_performed");
        internal static string PitchBellowsPerformed => Text.Get("pitch_bellows_performed");
        internal static string KindredPerformed => Text.Get("kindred_performed");
        internal static string HarrowSpurPerformed => Text.Get("harrow_spur_performed");
        internal static string AmberMendPerformed => Text.Get("amber_mend_performed");
        internal static string SovereignWardPerformed => Text.Get("sovereign_ward_performed");
        internal static string HearthHaulPerformed => Text.Get("hearth_haul_performed");
        internal static string AetherClearPerformed => Text.Get("aether_clear_performed");
        internal static string ElderSproutPerformed => Text.Get("elder_sprout_performed");
        internal static string PlumeWardPerformed => Text.Get("plume_ward_performed");
        internal static string SentrySightPerformed => Text.Get("sentry_sight_performed");
        internal static string TaprootSurgePerformed => Text.Get("taproot_surge_performed");
        internal static string MantleAnchorPerformed => Text.Get("mantle_anchor_performed");
        internal static string PrimalHearthPerformed => Text.Get("primal_hearth_performed");
        internal static string HearthKindlePerformed => Text.Get("hearth_kindle_performed");
        // In place of the game's "Rested (Comfort 3)" when rested by a fire under the rite. {0}: the game's comfort text.
        internal static string HearthKindleRested => Text.Get("hearth_kindle_rested");
        internal static string ScaldDriftPerformed => Text.Get("scald_drift_performed");
        internal static string TetherLockPerformed => Text.Get("tether_lock_performed");
        internal static string StriderGirthPerformed => Text.Get("strider_girth_performed");
        internal static string SecondKindlePerformed => Text.Get("second_kindle_performed");
        internal static string MorgenPouchPerformed => Text.Get("morgen_pouch_performed");
        internal static string TendrilDrawPerformed => Text.Get("tendril_draw_performed");
        internal static string GroundingPerformed => Text.Get("grounding_performed");
        internal static string CryptDelverPerformed => Text.Get("crypt_delver_performed");
        internal static string GlacialSatietyPerformed => Text.Get("glacial_satiety_performed");
        internal static string VeinShatterPerformed => Text.Get("vein_shatter_performed");
        internal static string CarapaceTemperPerformed => Text.Get("carapace_temper_performed");
        internal static string GraveBindPerformed => Text.Get("grave_bind_performed");
        internal static string FortunesBoonPerformed => Text.Get("fortunes_boon_performed");
        internal static string LuminaryPerformed => Text.Get("luminary_performed");
        internal static string AbyssalDrawPerformed => Text.Get("abyssal_draw_performed");
        internal static string FairWindPerformed => Text.Get("fair_wind_performed");
        internal static string HushFellingPerformed => Text.Get("hush_felling_performed");
        internal static string DraughtFlushPerformed => Text.Get("draught_flush_performed");
        internal static string SoulTetherPerformed => Text.Get("soul_tether_performed");
        internal static string TundraShroudPerformed => Text.Get("tundra_shroud_performed");
        // Told to the players within the bubble (the rest hear IncursionBegunFar).
        internal static string CrimsonPerformed => Text.Get("crimson_performed");
        internal static string RagnarokPerformed => Text.Get("ragnarok_performed");
        internal static string HoardBanePerformed => Text.Get("hoard_bane_performed");
        internal static string SanctuaryPerformed => Text.Get("sanctuary_performed");
        internal static string UnmakingPerformed => Text.Get("unmaking_performed");
        internal static string EndedWeather => Text.Get("ended_weather");
        internal static string EndedDaylight => Text.Get("ended_daylight");
        internal static string EndedGaleBreath => Text.Get("ended_gale_breath");
        internal static string EndedMistlands => Text.Get("ended_mistlands");
        internal static string EndedAshlands => Text.Get("ended_ashlands");
        internal static string EndedVatBoil => Text.Get("ended_vat_boil");
        internal static string EndedPitchBellows => Text.Get("ended_pitch_bellows");
        internal static string EndedKindred => Text.Get("ended_kindred");
        internal static string EndedHarrowSpur => Text.Get("ended_harrow_spur");
        internal static string EndedAmberMend => Text.Get("ended_amber_mend");
        internal static string EndedAbyssalSurge => Text.Get("ended_abyssal_surge");
        internal static string EndedSovereignWard => Text.Get("ended_sovereign_ward");
        internal static string EndedHearthHaul => Text.Get("ended_hearth_haul");
        internal static string EndedAetherClear => Text.Get("ended_aether_clear");
        internal static string EndedElderSprout => Text.Get("ended_elder_sprout");
        internal static string EndedSentrySight => Text.Get("ended_sentry_sight");
        internal static string EndedTaprootSurge => Text.Get("ended_taproot_surge");
        internal static string EndedMantleAnchor => Text.Get("ended_mantle_anchor");
        internal static string EndedPrimalHearth => Text.Get("ended_primal_hearth");
        internal static string EndedHearthKindle => Text.Get("ended_hearth_kindle");
        // {0}: the blessing's name, colored.
        internal static string BlessingEnded => Text.Get("blessing_ended");
        internal static string BlessingsLostToDeath => Text.Get("blessings_lost_to_death");
        // Said after the blessing's name. {0}: the blessing range in meters.
        internal static string BlessingMissed => Text.Get("blessing_missed");
        internal static string GraveBindKept => Text.Get("grave_bind_kept");
        internal static string GraveBindReequipped => Text.Get("grave_bind_reequipped");
        // Under the tombstone's name.
        internal static string GraveBindHover => Text.Get("grave_bind_hover");
        // {0}: the rite's name, colored.
        internal static string SoulTetherPulled => Text.Get("soul_tether_pulled");
        // {0}: the rite's name, colored.
        internal static string SoulTetherAllThere => Text.Get("soul_tether_all_there");
        internal static string SoulTetherNone => Text.Get("soul_tether_none");
        // Kinship rites: what the rite did to the player, said after its name.
        internal static string SkalReceived => Text.Get("skal_received");
        internal static string SkyCallRenewed => Text.Get("sky_call_renewed");
        internal static string SkyCallReady => Text.Get("sky_call_ready");
        internal static string SkyCallNoPower => Text.Get("sky_call_no_power");
        internal static string SkyCallWaiting => Text.Get("sky_call_waiting");
        internal static string SkyCallEnded => Text.Get("sky_call_ended");
        internal static string SkalRestedAlready => Text.Get("skal_rested_already");
        internal static string SkalWaiting => Text.Get("skal_waiting");
        internal static string SkalEnded => Text.Get("skal_ended");
        internal static string AnvilSparkRepaired => Text.Get("anvil_spark_repaired");
        internal static string AnvilSparkNothing => Text.Get("anvil_spark_nothing");
        internal static string AnvilSparkWaiting => Text.Get("anvil_spark_waiting");
        internal static string AnvilSparkEnded => Text.Get("anvil_spark_ended");
        internal static string EldhrimnirReceived => Text.Get("eldhrimnir_received");
        internal static string EldhrimnirFresh => Text.Get("eldhrimnir_fresh");
        internal static string EldhrimnirNoFood => Text.Get("eldhrimnir_no_food");
        internal static string EldhrimnirWaiting => Text.Get("eldhrimnir_waiting");
        internal static string EldhrimnirEnded => Text.Get("eldhrimnir_ended");
        internal static string RallyHornCalled => Text.Get("rally_horn_called");
        internal static string RallyHornByYou => Text.Get("rally_horn_by_you");
        internal static string RallyHornHere => Text.Get("rally_horn_here");
        internal static string RallyHornWaiting => Text.Get("rally_horn_waiting");
        internal static string RallyHornEnded => Text.Get("rally_horn_ended");
        // The Rally-Horn Rite's call: the ability's name (slot, status, Controls row), and what it says.
        internal static string RallyCallName => Text.Get("rally_call_name");
        internal static string RallyAcceptName => Text.Get("rally_accept_name");
        internal static string RallyRefuseName => Text.Get("rally_refuse_name");
        // The Bounds switch beside the Guardian Power: its name (slot, Controls row), and On or Off under it.
        internal static string DomainBorderName => Text.Get("domain_border_name");
        internal static string AbilityOn => Text.Get("ability_on");
        internal static string AbilityOff => Text.Get("ability_off");
        internal static string RallyCallRefused => Text.Get("rally_call_refused");
        internal static string RallyPendingRefused => Text.Get("rally_pending_refused");
        // {0}: the rite's name, colored.
        internal static string RallyCallAnswered => Text.Get("rally_call_answered");
        internal static string RallyCallFaded => Text.Get("rally_call_faded");
        internal static string RallyCallInCombat => Text.Get("rally_call_in_combat");
        internal static string RallyCallCannot => Text.Get("rally_call_cannot");
        internal static string CryptDelverFound => Text.Get("crypt_delver_found");
        // {0}: how many creatures died within the bubble so far.
        internal static string CrimsonKillCount => Text.Get("crimson_kill_count");
        internal static string CrimsonSurvived => Text.Get("crimson_survived");
        internal static string CrimsonSurvivedNoReward => Text.Get("crimson_survived_no_reward");
        internal static string CrimsonFailedObliterator => Text.Get("crimson_failed_obliterator");
        internal static string CrimsonFailedFallen => Text.Get("crimson_failed_fallen");
        internal static string CrimsonFailedEnded => Text.Get("crimson_failed_ended");
        // The bubble of the Crimson or Ragnarök Rite. {0}: the rite's name.
        internal static string IncursionNoPortal => Text.Get("incursion_no_portal");
        internal static string IncursionNoPortalIn => Text.Get("incursion_no_portal_in");
        internal static string IncursionHeld => Text.Get("incursion_held");
        internal static string IncursionKeptOut => Text.Get("incursion_kept_out");
        // {0}: the Domain rite, {1}: the Crimson or Ragnarök Rite.
        internal static string DomainCancelled => Text.Get("domain_cancelled");
        // {0}: the Domain rite.
        internal static string DomainBroken => Text.Get("domain_broken");
        internal static string IncursionNoBuild => Text.Get("incursion_no_build");
        internal static string IncursionOnlySanctuary => Text.Get("incursion_only_sanctuary");
        // {0}: the rite's name.
        internal static string IncursionOtherObliterator => Text.Get("incursion_other_obliterator");
        // {0}: the rite's name.
        internal static string SanctuaryOnlyInside => Text.Get("sanctuary_only_inside");
        internal static string IncursionBusy => Text.Get("incursion_busy");
        // {0}: the rite's name.
        internal static string IncursionNoTeleport => Text.Get("incursion_no_teleport");
        internal static string IncursionNoTeleportIn => Text.Get("incursion_no_teleport_in");
        internal static string IncursionNoDungeon => Text.Get("incursion_no_dungeon");
        internal static string IncursionNoSummon => Text.Get("incursion_no_summon");
        internal static string IncursionWonFar => Text.Get("incursion_won_far");
        internal static string IncursionLostFar => Text.Get("incursion_lost_far");
        // Told to the players outside the bubble, after the rite's name.
        internal static string IncursionBegunFar => Text.Get("incursion_begun_far");
        // {0}: the rite's name, {1}: what is in the way (one of the IncursionSite... below).
        internal static string IncursionSiteRefused => Text.Get("incursion_site_refused");
        internal static string IncursionSiteSpawn => Text.Get("incursion_site_spawn");
        internal static string IncursionSiteTrader => Text.Get("incursion_site_trader");
        internal static string IncursionSiteAltar => Text.Get("incursion_site_altar");
        internal static string IncursionSiteShield => Text.Get("incursion_site_shield");
        internal static string IncursionSiteRaid => Text.Get("incursion_site_raid");
        internal static string IncursionSiteBoss => Text.Get("incursion_site_boss");
        internal static string IncursionSiteDungeon => Text.Get("incursion_site_dungeon");
        internal static string IncursionSiteBed => Text.Get("incursion_site_bed");
        internal static string IncursionSitePlayerWard => Text.Get("incursion_site_player_ward");
        // {0}: the rite's name.
        internal static string IncursionRefusedRaid => Text.Get("incursion_refused_raid");
        internal static string IncursionRefusedBossFight => Text.Get("incursion_refused_boss_fight");
        // {0}: the rite's name.
        internal static string IncursionRefusedHostile => Text.Get("incursion_refused_hostile");
        internal static string IncursionMovedOut => Text.Get("incursion_moved_out");
        internal static string IncursionNoBed => Text.Get("incursion_no_bed");
        internal static string IncursionKeysKept => Text.Get("incursion_keys_kept");
        // {0}: the Sovereign Ward Rite's name.
        internal static string IncursionSiteWard => Text.Get("incursion_site_ward");
        // On the map: the rite's name, ominous.
        internal static string CrimsonPin => Colored("#ff4a3a", Text.Get("crimson_pin"));
        internal static string RallyHornPin => Colored("#ffd060", Text.Get("rally_horn_pin"));
        internal static string RagnarokPin => Colored("#ff9a2e", Text.Get("ragnarok_pin"));
        internal static string RagnarokBegins => Text.Get("ragnarok_begins");
        internal static string RagnarokNoBosses => Text.Get("ragnarok_no_bosses");
        internal static string RagnarokNoOne => Text.Get("ragnarok_no_one");
        // {0}: the boss.
        internal static string RagnarokRises => Text.Get("ragnarok_rises");
        internal static string RagnarokFalls => Text.Get("ragnarok_falls");
        internal static string RagnarokSurvived => Text.Get("ragnarok_survived");
        internal static string RagnarokSurvivedNoReward => Text.Get("ragnarok_survived_no_reward");
        internal static string RagnarokFailedObliterator => Text.Get("ragnarok_failed_obliterator");
        internal static string RagnarokFailedFallen => Text.Get("ragnarok_failed_fallen");
        internal static string RagnarokFailedEnded => Text.Get("ragnarok_failed_ended");
        internal static string RagnarokFailedTime => Text.Get("ragnarok_failed_time");
        internal static string RagnarokFailedSpawn => Text.Get("ragnarok_failed_spawn");
        internal static string HoardBanePin => Colored("#ffc24a", Text.Get("hoard_bane_pin"));
        // {0}: the miniboss (or both brothers).
        internal static string HoardBaneBegins => Text.Get("hoard_bane_begins");
        internal static string HoardBaneNoOne => Text.Get("hoard_bane_no_one");
        // {0}: the elite.
        internal static string HoardBaneEliteFalls => Text.Get("hoard_bane_elite_falls");
        // {0}: the miniboss (or both brothers).
        internal static string HoardBaneShieldBreaks => Text.Get("hoard_bane_shield_breaks");
        // {0}: the miniboss.
        internal static string HoardBaneBossFalls => Text.Get("hoard_bane_boss_falls");
        internal static string HoardBaneBossReturns => Text.Get("hoard_bane_boss_returns");
        internal static string HoardBaneEliteWakes => Text.Get("hoard_bane_elite_wakes");
        internal static string HoardBaneSurvived => Text.Get("hoard_bane_survived");
        internal static string HoardBaneSurvivedNoReward => Text.Get("hoard_bane_survived_no_reward");
        internal static string HoardBaneFailedObliterator => Text.Get("hoard_bane_failed_obliterator");
        internal static string HoardBaneFailedFallen => Text.Get("hoard_bane_failed_fallen");
        internal static string HoardBaneFailedEnded => Text.Get("hoard_bane_failed_ended");
        internal static string HoardBaneFailedTime => Text.Get("hoard_bane_failed_time");
        internal static string HoardBaneFailedSpawn => Text.Get("hoard_bane_failed_spawn");
        internal static string HoardBaneChestReturned => Text.Get("hoard_bane_chest_returned");
        internal static string HoardBaneChestBack => Text.Get("hoard_bane_chest_back");
        // Between the brothers' names: "Zil & Thungr".
        internal static string HoardBaneAnd => Text.Get("hoard_bane_and");
        internal static string EndedVerdant => Text.Get("ended_verdant");
        internal static string EndedPackward => Text.Get("ended_packward");
        internal static string EndedCarrion => Text.Get("ended_carrion");
        internal static string RefusedAdminsOnly => Text.Get("refused_admins_only");
        internal static string RefusedDisabled => Text.Get("refused_disabled");
        // The connection error shown on joining. {0}: mod name and version.
        internal static string ServerLacksMod => Text.Get("server_lacks_mod");

        // First line: what it does, capitalized.
        internal static string StatusTooltip => Text.Get("status_tooltip");
        internal static string StatusTooltipNoBiomes => Text.Get("status_tooltip_no_biomes");
        internal static string StatusTooltipPolarDay => Text.Get("status_tooltip_polar_day");
        internal static string StatusTooltipPolarNight => Text.Get("status_tooltip_polar_night");
        internal static string StatusTooltipFairWind => Text.Get("status_tooltip_fair_wind");
        // {1}: how many times as far off wild creatures have to be to see you while you gather.
        internal static string StatusTooltipHushFelling => Text.Get("status_tooltip_hush_felling");
        // {1}: how many times as long a recovery mead's wait is, {2}: how many times as long other meads last.
        internal static string StatusTooltipDraughtFlush => Text.Get("status_tooltip_draught_flush");
        // {1}: how many times as fast Wet wears off.
        internal static string StatusTooltipTundraShroud => Text.Get("status_tooltip_tundra_shroud");
        internal static string StatusTooltipGaleBreath => Text.Get("status_tooltip_gale_breath");
        internal static string StatusTooltipSootfall => Text.Get("status_tooltip_sootfall");
        internal static string StatusTooltipNoMist => Text.Get("status_tooltip_no_mist");
        internal static string StatusTooltipCryptDelver => Text.Get("status_tooltip_crypt_delver");
        // {1}: the food floor in percent.
        internal static string StatusTooltipGlacialSatiety => Text.Get("status_tooltip_glacial_satiety");
        // {1}: how much of a hit spreads, {2}: at how much health left the vein shatters, both in percent.
        internal static string StatusTooltipVeinShatter => Text.Get("status_tooltip_vein_shatter");
        // {1}: how much less wear, in percent (from the Wear Multiplier).
        internal static string StatusTooltipCarapaceTemper => Text.Get("status_tooltip_carapace_temper");
        internal static string TemperedDurability => Text.Get("tempered_durability");
        internal static string StatusTooltipGraveBind => Text.Get("status_tooltip_grave_bind");
        // {1}: the luck multiplier. {2}: what happens instead to an item the forge would destroy.
        internal static string StatusTooltipFortunesBoon => Text.Get("status_tooltip_fortunes_boon");
        internal static string FortuneForgeLoses => Text.Get("fortune_forge_loses");
        internal static string FortuneForgeKeeps => Text.Get("fortune_forge_keeps");
        // {0}: the item's name.
        internal static string FortuneSavedItem => Text.Get("fortune_saved_item");
        // {1}: radius in meters, {2}: how much less time, in percent (from the Time Multiplier).
        internal static string StatusTooltipVatBoil => Text.Get("status_tooltip_vat_boil");
        internal static string StatusTooltipPitchBellows => Text.Get("status_tooltip_pitch_bellows");
        // {1}: radius in meters, {2}: breeding multiplier, {3}: crowd multiplier.
        internal static string StatusTooltipKindred => Text.Get("status_tooltip_kindred");
        // {1}: radius in meters, {2}: spawn speed multiplier, {3}: creature cap multiplier.
        internal static string StatusTooltipHarrowSpur => Text.Get("status_tooltip_harrow_spur");
        internal static string StatusTooltipSovereignWard => Text.Get("status_tooltip_sovereign_ward");
        internal static string StatusTooltipHearthHaul => Text.Get("status_tooltip_hearth_haul");
        internal static string StatusTooltipHearthHaulCarry => Text.Get("status_tooltip_hearth_haul_carry");
        internal static string StatusTooltipAetherClear => Text.Get("status_tooltip_aether_clear");
        internal static string StatusTooltipElderSprout => Text.Get("status_tooltip_elder_sprout");
        internal static string StatusTooltipPlumeWard => Text.Get("status_tooltip_plume_ward");
        internal static string StatusTooltipSentrySight => Text.Get("status_tooltip_sentry_sight");
        internal static string StatusTooltipTaprootSurge => Text.Get("status_tooltip_taproot_surge");
        internal static string StatusTooltipMantleAnchor => Text.Get("status_tooltip_mantle_anchor");
        internal static string StatusTooltipPrimalHearth => Text.Get("status_tooltip_primal_hearth");
        internal static string StatusTooltipHearthKindle => Text.Get("status_tooltip_hearth_kindle");
        internal static string StatusTooltipScaldDrift => Text.Get("status_tooltip_scald_drift");
        internal static string StatusTooltipTetherLock => Text.Get("status_tooltip_tether_lock");
        internal static string StatusTooltipStriderGirth => Text.Get("status_tooltip_strider_girth");
        internal static string StatusTooltipSecondKindle => Text.Get("status_tooltip_second_kindle");
        internal static string SecondKindleSaved => Text.Get("second_kindle_saved");
        internal static string PlumeWardSaved => Text.Get("plume_ward_saved");
        internal static string StatusTooltipMorgenPouch => Text.Get("status_tooltip_morgen_pouch");
        internal static string StatusTooltipTendrilDraw => Text.Get("status_tooltip_tendril_draw");
        // The Morgen-Pouch ability: its names (slot, Controls row) and what it says.
        internal static string MorgenPouchAbility => Text.Get("morgen_pouch_ability");
        internal static string MorgenPouchTake => Text.Get("morgen_pouch_take");
        internal static string MorgenPouchRelease => Text.Get("morgen_pouch_release");
        internal static string MorgenPouchTaken => Text.Get("morgen_pouch_taken");
        internal static string MorgenPouchNothing => Text.Get("morgen_pouch_nothing");
        internal static string MorgenPouchNothingRiding => Text.Get("morgen_pouch_nothing_riding");
        internal static string MorgenPouchReleased => Text.Get("morgen_pouch_released");
        internal static string MorgenPouchEjected => Text.Get("morgen_pouch_ejected");
        internal static string MorgenPouchOtherWorld => Text.Get("morgen_pouch_other_world");
        internal static string MorgenPouchSummonLost => Text.Get("morgen_pouch_summon_lost");
        internal static string MorgenPouchCarrying => Text.Get("morgen_pouch_carrying");
        internal static string MorgenPouchHover => Text.Get("morgen_pouch_hover");
        internal static string MorgenPouchWait => Text.Get("morgen_pouch_wait");
        internal static string StatusTooltipSovereignSanctuary => Text.Get("status_tooltip_sovereign_sanctuary");
        // {1}: the light's range in meters.
        internal static string StatusTooltipLuminary => Text.Get("status_tooltip_luminary");
        // {1}: the pickup range in meters.
        internal static string StatusTooltipAbyssalDraw => Text.Get("status_tooltip_abyssal_draw");
        internal static string StatusTooltipAmberMend => Text.Get("status_tooltip_amber_mend");
        // {1}: serpent multiplier, {2}: fish multiplier.
        internal static string StatusTooltipAbyssalSurge => Text.Get("status_tooltip_abyssal_surge");
        // {1}: the radius in meters.
        internal static string StatusTooltipPackward => Text.Get("status_tooltip_packward");
        internal static string StatusTooltipCarrion => Text.Get("status_tooltip_carrion");
        // {2}: how much faster, in percent.
        internal static string StatusTooltipVerdant => Text.Get("status_tooltip_verdant");
        internal static string StatusTooltipSkal => Text.Get("status_tooltip_skal");
        internal static string StatusTooltipAnvilSpark => Text.Get("status_tooltip_anvil_spark");
        internal static string StatusTooltipEldhrimnir => Text.Get("status_tooltip_eldhrimnir");
        internal static string StatusTooltipRallyHorn => Text.Get("status_tooltip_rally_horn");
        // {0}: who called, {1}: the Rally Call key, {2}: the wait after answering.
        internal static string StatusTooltipRallyCall => Text.Get("status_tooltip_rally_call");
        internal static string StatusTooltipSkyCall => Text.Get("status_tooltip_sky_call");
        // {1}: the radius in meters.
        internal static string StatusTooltipCrimson => Text.Get("status_tooltip_crimson");
        // {1}: the radius in meters.
        internal static string StatusTooltipRagnarok => Text.Get("status_tooltip_ragnarok");
        // {1}: the radius in meters.
        internal static string StatusTooltipHoardBane => Text.Get("status_tooltip_hoard_bane");
        internal static string SomeoneName => Text.Get("someone_name");
        // Times left: "45 s", "30 min", "1 h", "1 h 30 min" (see TimeText).
        internal static string TimeSeconds => Text.Get("time_seconds");
        internal static string TimeMinutes => Text.Get("time_minutes");
        internal static string TimeHours => Text.Get("time_hours");
        internal static string TimeHoursMinutes => Text.Get("time_hours_minutes");

        // Item names in orange, as the game shows them in hover text.
        internal const string ItemColor = "orange";
        internal const string Faint = "#c9c9c9";
        // Gray, for items that just burn.
        private const string Dim = "#9a9a9a";

        // {0}: rite name, colored. {1}: the gray explanation (Explanation below), or nothing when Show Explanations is off.
        internal const string LeverBrings = "{0}{1}";
        internal const string Explanation = " <color=" + Faint + ">({0})</color>";
        // On a plant under the Verdant Rite (without AzuHoverStats, which shows the time itself): when it should be grown.
        internal static string VerdantReadyIn => "\n" + Colored(Faint, Text.Get("verdant_ready_in"));
        internal static string VerdantReadySoon => "\n" + Colored(Faint, Text.Get("verdant_ready_soon"));
        internal static string AnchorDespawnIn => "\n" + Colored(Faint, Text.Get("anchor_despawn_in"));
        internal static string AnchorStaysBase => "\n" + Colored(Faint, Text.Get("anchor_stays_base"));
        internal static string AnchorStaysTar => "\n" + Colored(Faint, Text.Get("anchor_stays_tar"));
        // On a fermenter under the Vat-Boil Rite (without AzuHoverStats): when it should be done.
        internal static string StationReadyIn => "\n" + Colored(Faint, Text.Get("station_ready_in"));
        internal static string StationReadySoon => "\n" + Colored(Faint, Text.Get("station_ready_soon"));
        // On a tamed young animal under the Kindred Rite: when it should be grown; on a pregnant one: when it gives birth.
        internal static string KindredGrownIn => "\n" + Colored(Faint, Text.Get("kindred_grown_in"));
        internal static string KindredGrownSoon => "\n" + Colored(Faint, Text.Get("kindred_grown_soon"));
        internal static string KindredDueIn => "\n" + Colored(Faint, Text.Get("kindred_due_in"));
        internal static string KindredDueSoon => "\n" + Colored(Faint, Text.Get("kindred_due_soon"));
        // On a warm egg under the Kindred Rite: when it hatches.
        internal static string KindredHatchesIn => "\n" + Colored(Faint, Text.Get("kindred_hatches_in"));
        internal static string KindredHatchesSoon => "\n" + Colored(Faint, Text.Get("kindred_hatches_soon"));
        // On a spawner under the Harrow-Spur Rite. {0}: seconds between spawns, {1}: creatures it keeps close by.
        internal static string HarrowSpurSpawns => "\n" + Colored(Faint, Text.Get("harrow_spur_spawns"));
        // In AzuHoverStats' cooking times, under the Vat-Boil Rite.
        internal static string WontBurn => Colored("#9fe07a", Text.Get("wont_burn"));
        internal static string LeverNotAllowed => Colored("#ff7a5c", Text.Get("lever_not_allowed"));
        // On the lever of an obliterator with rites off, when rite items are in it.
        internal static string LeverSwitchedOff => Colored(Dim, Text.Get("lever_switched_off"));

        // Topic of the popup when a rite becomes known (see RiteDiscovery).
        internal static string NewRite => Text.Get("new_rite");

        internal static string HatchTitle => Colored("#9fd8ff", Text.Get("hatch_title"));
        internal const string HatchLine = "  <color=" + ItemColor + ">{0}</color>: {1}{2}";

        // On the obliterator's lever and hatch, and the flap's active rites button: the rites performed at this
        // obliterator that still go on, one per line: "  {rite}: {time left}". {0}: the time left.
        internal static string ActiveTitle => Colored("#9fe07a", Text.Get("active_title"));
        internal static string ActiveTooltip => Text.Get("active_tooltip");
        internal static string ActiveLeft => Colored(Faint, Text.Get("active_left"));
        internal static string ActivePaused => Colored(Dim, Text.Get("active_paused"));
        // In place of the time left, for a rite with no time limit (the Ragnarök Rite).
        internal static string ActiveUntimed => Colored(Faint, Text.Get("active_untimed"));
        internal const string ActiveLine = "  {0}: {1}";
        // On the active rites page, under a rite's name. {0}: what it does, {1}: who performed it.
        internal static string ActiveDetail => Text.Get("active_detail");

        internal static string TooltipDecides => Text.Get("tooltip_decides");
        // {0}: the rite that goes first and keeps this item's rite out.
        internal static string TooltipOverruled => Colored(Dim, Text.Get("tooltip_overruled"));
        internal static string TooltipOverruledShort => Colored(Dim, Text.Get("tooltip_overruled_short"));
        // {0}: how many are needed, {1}: the rite, {2}: how many are in.
        internal static string TooltipNeedsMore => Colored(Dim, Text.Get("tooltip_needs_more"));
        internal static string TooltipNeedsMoreShort => Colored(Dim, Text.Get("tooltip_needs_more_short"));
        // {0}: the Unmaking Rite. The materials it gives back are shown under the line.
        internal static string TooltipUnmade => Text.Get("tooltip_unmade");
        internal static string TooltipUnmadeNothing => Colored(Dim, Text.Get("tooltip_unmade_nothing"));
        // An item with how many are needed: "2x Ooze", the number in white inside the item's orange.
        internal const string ItemAmount = "<color=#f2f2f2>{0}x</color> {1}";
        // Different items, how many are needed: "3 different of Deer trophy / Elder trophy".
        internal static string ItemsDifferent => Text.Get("items_different");
        // Between the names of a rite's key items: "Surtling core / Ruby".
        internal const string ItemSeparator = " / ";

        // The flap under the obliterator: its switch, and the rites page it opens.
        internal static string SwitchTooltip => Text.Get("switch_tooltip");
        internal static string PageTooltip => Text.Get("page_tooltip");
        internal static string SwitchedOn => Text.Get("switched_on");
        internal static string SwitchedOff => Text.Get("switched_off");
        internal static string SwitchNotOwner => Text.Get("switch_not_owner");
        internal static string PanelHint => Text.Get("panel_hint");
        // Tooltip of an item on the rites page. {0}: how many it takes, {1}: how many you have.
        internal static string PanelItemMissing => Text.Get("panel_item_missing");
        internal static string PanelItemNeeds => Text.Get("panel_item_needs");
        internal static string PanelItemPut => Text.Get("panel_item_put");
        internal static string PanelItemIn => Text.Get("panel_item_in");
        internal static string PanelItemEnough => Text.Get("panel_item_enough");
        internal static string PanelAnyDifferent => Text.Get("panel_any_different");
        // An item whose rite clashes with one already in. {0}: that rite.
        internal static string PanelItemClash => Text.Get("panel_item_clash");
        internal static string PanelItemClashDetail => Text.Get("panel_item_clash_detail");
        // Clicking it. {0}: the clicked rite, {1}: the one already in.
        internal static string PanelClashWarning => Text.Get("panel_clash_warning");
        // After the name of the rite keeping the clicked one out, for a moment.
        internal static string PanelPrepared => Colored("#e8735f", Text.Get("panel_prepared"));
        // The "+N" button of a rite with more items than fit in its row. {0}: how many more.
        internal static string PanelMoreItems => Text.Get("panel_more_items");
        internal static string PanelFewerItems => Text.Get("panel_fewer_items");
        internal static string PanelOpenCompendium => Text.Get("panel_open_compendium");
        // After the name of the rite pulling the lever performs.
        internal static string PanelReady => Colored("#8fe08f", Text.Get("panel_ready"));
        internal static string PanelMoreToFind => Text.Get("panel_more_to_find");
        internal static string PanelOneMoreToFind => Text.Get("panel_one_more_to_find");
        internal static string CloseTooltip => Text.Get("close_tooltip");
    }

    // The "Tempest Rite" page of the Compendium (see RiteCompendium). Numbers in braces come from the settings.
    internal static class CompendiumTexts
    {
        internal static string Topic => Text.Get("compendium_topic");

        internal static string Intro => Text.Get("compendium_intro");
        internal static string Rules => Text.Get("compendium_rules");
        internal static string AdminsOnly => Colored("#ff7a5c", Text.Get("compendium_admins_only"));
        internal static string Learning => Text.Get("compendium_learning");
        internal static string NoneKnown => Text.Get("compendium_none_known");
        internal static string MoreToFind => Text.Get("compendium_more_to_find");
        internal static string OneMoreToFind => Text.Get("compendium_one_more_to_find");

        // Under each rite's name. {0}: the item(s) to burn.
        internal static string Burn => Text.Get("compendium_burn");
        internal const string Separator = "  ·  ";
        internal static string Lasts => Text.Get("compendium_lasts");
        internal static string AtOnce => Text.Get("compendium_at_once");
        // {0}: how long the players have.
        internal static string RagnarokTime => Text.Get("compendium_ragnarok_time");
        // {0}: how long until the same rite can give a player something again.
        internal static string KinshipWait => Text.Get("compendium_kinship_wait");
        // {0}: how long the player stays rested, {1}: the wait.
        internal static string SkalTimes => Text.Get("compendium_skal_times");

        // What each group shares. {0}: the Domain radius or blessing range in meters.
        internal static string GroupAtmospheric => Text.Get("compendium_group_atmospheric");
        internal static string GroupCelestial => Text.Get("compendium_group_celestial");
        internal static string GroupBiomeMastery => Text.Get("compendium_group_biome_mastery");
        internal static string GroupDomain => Text.Get("compendium_group_domain");
        internal static string GroupBlessings => Text.Get("compendium_group_blessings");
        internal static string GroupKinship => Text.Get("compendium_group_kinship");
        internal static string GroupReckoning => Text.Get("compendium_group_reckoning");

        internal static string Clear => Text.Get("compendium_clear");
        internal static string Thunderstorm => Text.Get("compendium_thunderstorm");
        internal static string Rain => Text.Get("compendium_rain");
        internal static string Mist => Text.Get("compendium_mist");
        internal static string Aurora => Text.Get("compendium_aurora");
        internal static string PolarDay => Text.Get("compendium_polar_day");
        internal static string PolarNight => Text.Get("compendium_polar_night");
        internal static string GaleBreath => Text.Get("compendium_gale_breath");
        internal static string NoMist => Text.Get("compendium_no_mist");
        internal static string Sootfall => Text.Get("compendium_sootfall");
        // {0}: serpent multiplier, {1}: fish school multiplier, {2}: big fish multiplier.
        internal static string AbyssalSurge => Text.Get("compendium_abyssal_surge");
        // {0}: how long a hurt creature fights back.
        internal static string Packward => Text.Get("compendium_packward");
        // {0}: how far a drawn creature sees and hears, in percent.
        internal static string Carrion => Text.Get("compendium_carrion");
        // {0}: growth multiplier.
        internal static string Verdant => Text.Get("compendium_verdant");
        // {0}: speed multiplier.
        internal static string VatBoil => Text.Get("compendium_vat_boil");
        // {0}: speed multiplier.
        internal static string PitchBellows => Text.Get("compendium_pitch_bellows");
        // {0}: breeding multiplier, {1}: crowd multiplier.
        internal static string Kindred => Text.Get("compendium_kindred");
        // {0}: spawn speed multiplier, {1}: creature cap multiplier.
        internal static string HarrowSpur => Text.Get("compendium_harrow_spur");
        internal static string AmberMend => Text.Get("compendium_amber_mend");
        internal static string SovereignWard => Text.Get("compendium_sovereign_ward");
        internal static string HearthHaul => Text.Get("compendium_hearth_haul");
        internal static string HearthHaulCarry => Text.Get("compendium_hearth_haul_carry");
        internal static string AetherClear => Text.Get("compendium_aether_clear");
        internal static string SovereignSanctuary => Text.Get("compendium_sovereign_sanctuary");
        internal static string Grounding => Text.Get("compendium_grounding");
        // {0}: loot multiplier, {1}: chance of one more item, {2}: of two more, both in percent.
        internal static string CryptDelver => Text.Get("compendium_crypt_delver");
        // {0}: the food floor, in percent.
        internal static string GlacialSatiety => Text.Get("compendium_glacial_satiety");
        // {0}: how much of a hit spreads, {1}: at how much health the rest breaks, both in percent.
        internal static string VeinShatter => Text.Get("compendium_vein_shatter");
        internal static string VeinShatterNoBreak => Text.Get("compendium_vein_shatter_no_break");
        // {0}: how much less wear, in percent.
        internal static string CarapaceTemper => Text.Get("compendium_carapace_temper");
        internal static string GraveBind => Text.Get("compendium_grave_bind");
        internal static string GraveBindBeam => Text.Get("compendium_grave_bind_beam");
        internal static string GraveBindReequip => Text.Get("compendium_grave_bind_reequip");
        // {0}: luck multiplier, {1}: what happens to an item the forge would destroy (Messages.FortuneForgeLoses or Keeps).
        internal static string FortunesBoon => Text.Get("compendium_fortunes_boon");
        // {0}: the light's range in meters.
        internal static string Luminary => Text.Get("compendium_luminary");
        internal static string LuminaryShadows => Text.Get("compendium_luminary_shadows");
        // {0}: pickup range in meters.
        internal static string AbyssalDraw => Text.Get("compendium_abyssal_draw");
        internal static string FairWind => Text.Get("compendium_fair_wind");
        // {0}: how many times as close wild creatures have to be to see you while you gather.
        internal static string HushFelling => Text.Get("compendium_hush_felling");
        // {0}: how many times as long a recovery mead's wait is, {1}: how many times as long other meads last.
        internal static string DraughtFlush => Text.Get("compendium_draught_flush");
        internal static string SoulTether => Text.Get("compendium_soul_tether");
        // {0}: how many times as fast Wet wears off.
        internal static string TundraShroud => Text.Get("compendium_tundra_shroud");
        internal static string Skal => Text.Get("compendium_skal");
        internal static string SkyCall => Text.Get("compendium_sky_call");
        internal static string AnvilSpark => Text.Get("compendium_anvil_spark");
        internal static string Eldhrimnir => Text.Get("compendium_eldhrimnir");
        // {0}: how long the call lasts, {1}: the Rally Call key.
        internal static string RallyHorn => Text.Get("compendium_rally_horn");
        // {0}: pause between raids, {1}: radius in meters.
        internal static string Crimson => Text.Get("compendium_crimson");
        // {0}: the reward for each kill.
        internal static string CrimsonReward => Text.Get("compendium_crimson_reward");
        internal static string CrimsonNoReward => Text.Get("compendium_crimson_no_reward");
        internal static string CrimsonEnd => Text.Get("compendium_crimson_end");
        // {0}: how many bosses, {1}: pause between bosses, {2}: radius in meters.
        internal static string Ragnarok => Text.Get("compendium_ragnarok");
        // {0}: radius in meters, {1}: the elites' bubbles' radius.
        internal static string HoardBane => Text.Get("compendium_hoard_bane");
        internal static string Sanctuary => Text.Get("compendium_sanctuary");
        internal static string Reset => Text.Get("compendium_reset");
        internal static string SunFall => Text.Get("compendium_sun_fall");
        internal static string NaturesRebound => Text.Get("compendium_natures_rebound");
        // {0}: share given back, in percent.
        internal static string Unmaking => Text.Get("compendium_unmaking");
        internal static string UnmakingUpgradeMaterials => Text.Get("compendium_unmaking_upgrade_materials");
        // {0}: the rite's radius in meters.
        internal static string ElderSprout => Text.Get("compendium_elder_sprout");
        internal static string PlumeWard => Text.Get("compendium_plume_ward");
        internal static string SentrySight => Text.Get("compendium_sentry_sight");
        internal static string TaprootSurge => Text.Get("compendium_taproot_surge");
        internal static string MantleAnchor => Text.Get("compendium_mantle_anchor");
        internal static string PrimalHearth => Text.Get("compendium_primal_hearth");
        internal static string HearthKindle => Text.Get("compendium_hearth_kindle");
        internal static string ScaldDrift => Text.Get("compendium_scald_drift");
        internal static string TetherLock => Text.Get("compendium_tether_lock");
        internal static string StriderGirth => Text.Get("compendium_strider_girth");
        internal static string SecondKindle => Text.Get("compendium_second_kindle");
        internal static string MorgenPouch => Text.Get("compendium_morgen_pouch");
        internal static string TendrilDraw => Text.Get("compendium_tendril_draw");
        internal static string SentrySparesAnimals => Text.Get("compendium_sentry_spares_animals");
    }

    internal static class DebugMessages
    {
        internal const string ReadConfigCalled = "ReadConfigValues called";
        internal const string ErrorLoadingConfig = $"There was an issue loading your {Plugin.ConfigFileName}";
        internal const string RequestCheckConfig = "Please check your config entries for spelling and format!";
    }

    // Sections are named "<group number> - <group> - <rite>", numbered as the rites page groups them (atmospheric, celestial,
    // biome mastery, domain, blessings, kinship, reckoning), so Configuration Manager lists them in that order and a new
    // rite never renumbers the others. Per-player settings come last (8 - Interface, 9 - Effects).
    internal static class Groups
    {
        internal static class General
        {
            internal const string Group = "0 - General";
            // Settings for a whole rite group sit in a section of the group's own name, listed before its rites.
            internal const string AtmosphericGroup = "1 - Atmospheric";
            internal const string DomainGroup = "4 - Domain";
            internal const string BlessingsGroup = "5 - Blessings";

            internal static readonly ConfigInfo<WeatherPermission> WhoCanChange = new(
                Group,
                "Who Can Change Weather",
                "Who may perform rites with the obliterator. Anyone: every player with the mod. " +
                "AdminsOnly: only server admins (and the host). Others' key items are just obliterated as usual.",
                WeatherPermission.Anyone,
                true);

            internal static readonly ConfigInfo<WeatherBiomes> AffectedBiomes = new(
                AtmosphericGroup,
                "Affected Biomes",
                "Biomes where weather rites (Clear Sky, Tempest, Downpour, Shrouding, Aurora) show. Elsewhere the biome keeps its own " +
                "weather while the rite goes on. Each weather rite shows as that biome's own kind of it (see its Environments setting). " +
                "The other rites don't depend on this. Deep North is left out by default until it is part of the game.",
                WeatherBiomes.Meadows | WeatherBiomes.BlackForest | WeatherBiomes.Swamp | WeatherBiomes.Mountain |
                WeatherBiomes.Plains | WeatherBiomes.Mistlands | WeatherBiomes.AshLands | WeatherBiomes.Ocean,
                true);

            internal static readonly ConfigInfo<float> DomainRadius = new(
                DomainGroup,
                "Domain Rites Radius",
                new ConfigDescription(
                    "How far from their obliterator the Domain rites (Packward, Carrion, Verdant, Vat-Boil, Pitch-Bellows, Kindred, Harrow-Spur, Amber-Mend, " +
                    "Sovereign Ward, Hearth-Haul, Aether-Clear, Elder-Sprout, Sentry-Sight, Taproot-Surge, Pitch-Seal, Primal-Hearth, " +
                    "Hearth-Kindle) work, in " +
                    "meters. Their status and message only show this near it.",
                    new AcceptableValueRange<float>(5f, 500f)),
                100f,
                true);

            internal static readonly ConfigInfo<float> BlessingRange = new(
                BlessingsGroup,
                "Blessings Range",
                new ConfigDescription(
                    "How close to the obliterator players must stand when its thunder strikes to receive a blessing (Crypt Delver, Glacial " +
                    "Satiety, Vein-Shatter, Carapace Temper, Grave-Bind, Fortune's Boon, Luminary, Abyssal Draw, Fair-Wind), in " +
                    "meters. 10 is the range Forsaken powers are shared in.",
                    new AcceptableValueRange<float>(2f, 100f)),
                10f,
                true);

            internal static readonly ConfigInfo<Toggle> RequireModOnClients = new(
                Group,
                "Require Mod On Clients",
                "Server: On turns away players who don't have the mod (or have another version). Off lets them join; they keep " +
                "the game's own weather and get none of the rites. Players with the mod always need it on the server.",
                Toggle.On,
                true);

            internal static readonly ConfigInfo<Toggle> AdminLocked = new(
                Group,
                "Admin Locked",
                "On lets server admins change the synced settings in game (Configuration Manager): each change is saved to the " +
                "server's config file and sent to every player. Off: synced settings come only from the server's config file, " +
                "and nobody can change them in game, admins included.",
                Toggle.On,
                true);
        }

        // ---- Atmospheric ----

        internal static readonly RiteSettings Clear = new(
            "1 - Atmospheric - Clear Sky",
            "the Clear Sky Rite (clear skies)",
            "SurtlingCore",
            "a surtling core",
            "No rain, snow or fog: for building, farming and travelling in peace. Cold biomes stay as cold as their own weather makes them.",
            true,
            amount: 2,
            minutes: 20f,
            environments: "Default: Clear, Mistlands: Mistlands_clear, AshLands: Ashlands_ashrain_clear");

        internal static readonly RiteSettings Thunderstorm = new(
            "1 - Atmospheric - Tempest",
            "the Tempest Rite (a thunderstorm)",
            "CopperOre",
            "copper ore",
            "Strong wind for sailing, and the weather serpents come out in. A snowstorm in the mountains.",
            true,
            minutes: 20f,
            environments: "Default: ThunderStorm, Mountain: SnowStorm, Mistlands: Mistlands_thunder, AshLands: Ashlands_storm");

        internal static readonly RiteSettings Rain = new(
            "1 - Atmospheric - Downpour",
            "the Downpour Rite (rain)",
            "Ooze",
            "ooze",
            "For the mood. Light rain in the meadows, snow in the mountains. Puts out spreading fire with no roof over it and keeps " +
            "cinders from lighting more (the Tempest Rite does too).",
            true,
            amount: 2,
            minutes: 20f,
            environments: "Default: Rain, Meadows: LightRain, Swamp: SwampRain, Mountain: Snow, Mistlands: Mistlands_rain, AshLands: Ashlands_ashrain");

        internal static readonly RiteSettings Mist = new(
            "1 - Atmospheric - Shrouding",
            "the Shrouding Rite (mist)",
            "Cloudberry",
            "cloudberries",
            "Fog that hides what is far away.",
            true,
            amount: 3,
            minutes: 20f,
            environments: "Default: Misty, BlackForest: DeepForest Mist, Mistlands: Mistlands_clear, AshLands: Ashlands_misty");

        internal static readonly RiteSettings Aurora = new(
            "1 - Atmospheric - Aurora",
            "the Aurora Rite (auroras and calm winds)",
            "Crystal",
            "crystals",
            "A clear sky with shimmering auroras at night, like the Deep North's, and calm, quiet winds. " + CustomEnvironmentHelp,
            true,
            amount: 3,
            minutes: 20f,
            environments: "Default: " + CustomEnvironmentNames.Aurora);

        internal static readonly RiteSettings Reset = new(
            "1 - Atmospheric - Equilibrium",
            "the Equilibrium Rite (ends the atmospheric rites)",
            "BoneFragments",
            "bone fragments",
            "Ends the atmospheric rites going on (Clear Sky, Tempest, Downpour, Shrouding, Aurora) at once, so the world's own " +
            "weather comes back. Other rites are left be.",
            true,
            lasts: false,
            amount: 9);

        // ---- Celestial ----

        internal static readonly RiteSettings PolarDay = new(
            "2 - Celestial - Sun Zenith",
            "the Sun Zenith Rite (polar day)",
            "Ruby",
            "a ruby",
            "It is day all the time: the sun stays up, creatures spawn as by day and night creatures leave. Time goes on: days are " +
            "counted, clocks show the real time, and beds go by the real clock.",
            true,
            amount: 2,
            minutes: 60f);

        internal static readonly RiteSettings PolarNight = new(
            "2 - Celestial - Midnight",
            "the Midnight Rite (polar night)",
            "SilverOre",
            "silver ore",
            "It is night all the time: the sun stays down, night creatures spawn (more and stronger ones) and nights are cold. " +
            "Time goes on: days are counted, clocks show the real time, and beds go by the real clock.",
            true);

        internal static readonly RiteSettings GaleBreath = new(
            "2 - Celestial - Gale-Breath",
            "the Gale-Breath Rite (the strongest wind)",
            "DragonTear",
            "a dragon tear",
            "The wind blows as hard as it can, everywhere, for everyone with the mod. Only its strength: it still turns with the " +
            "weather. With the Fair-Wind Rite it is never against your ship and always at full strength.",
            true);

        internal static readonly RiteSettings SunFall = new(
            "2 - Celestial - Sun-Fall",
            "the Sun-Fall Rite (ends the celestial rites)",
            "GreydwarfEye",
            "greydwarf eyes",
            "Ends the celestial rites going on (Sun Zenith, Midnight, Gale-Breath) at once, so day, night and wind go back to " +
            "their own course. Other rites are left be.",
            true,
            lasts: false,
            amount: 8);

        // ---- Biome mastery ----

        internal static readonly RiteSettings NoMist = new(
            "3 - Biome Mastery - True Sight",
            "the True Sight Rite (no Mistlands mist)",
            "DvergrKeyFragment",
            "a sealbreaker fragment",
            "The Mistlands mist is gone, for you and for what creatures can see. Off by default: it takes away much of the Mistlands' challenge.",
            true,
            minutes: 60f);

        internal static readonly RiteSettings Sootfall = new(
            "3 - Biome Mastery - Sootfall",
            "the Sootfall Rite (no Ashlands smoke or heat haze)",
            "MushroomSmokePuff",
            "smoke puffs",
            "The Ashlands ground smoke and the heat shimmer near lava are gone, so you can see. The heat itself still burns.",
            true,
            amount: 3);

        internal static readonly RiteSettings AbyssalSurge = new(
            "3 - Biome Mastery - Abyssal Surge",
            "the Abyssal Surge Rite (more serpents and fish)",
            "SerpentScale",
            "serpent scales",
            "The sea stirs: serpents come out far more often at night and in rain and storms, fish swim in bigger schools in the " +
            "sea, along the coasts and in rivers, and more of them are big ones (4 and 5 stars). Everywhere, for everyone with the mod.",
            true,
            amount: 2);

        internal static readonly ConfigInfo<float> SerpentMultiplier = new(
            AbyssalSurge.Enabled.Group,
            "Serpent Multiplier",
            new ConfigDescription("How many times as often serpents spawn (at night, and by day in rain and storms).", new AcceptableValueRange<float>(1f, 10f)),
            3f,
            true);

        internal static readonly ConfigInfo<float> FishMultiplier = new(
            AbyssalSurge.Enabled.Group,
            "Fish School Multiplier",
            new ConfigDescription("How many times as many fish spawn in each school.", new AcceptableValueRange<float>(1f, 10f)),
            2f,
            true);

        internal static readonly ConfigInfo<float> BigFishMultiplier = new(
            AbyssalSurge.Enabled.Group,
            "Big Fish Multiplier",
            new ConfigDescription("How many times as likely each star of a spawning fish is, so more reach 4 and 5 stars.", new AcceptableValueRange<float>(1f, 10f)),
            3f,
            true);

        internal static readonly RiteSettings NaturesRebound = new(
            "3 - Biome Mastery - Natures Rebound",
            "the Nature's Rebound Rite (ends the biome mastery rites)",
            "Guck",
            "guck",
            "Ends the biome mastery rites going on (True Sight, Sootfall, Abyssal Surge) at once, so mist, smoke and sea go back " +
            "to their own ways. Other rites are left be.",
            true,
            lasts: false,
            amount: 3);

        // ---- Domain ----

        internal static readonly RiteSettings Packward = new(
            "4 - Domain - Packward",
            "the Packward Rite (creatures shun the area)",
            "WolfFang",
            "wolf fangs",
            "Creatures (not bosses, not tamed) shun the Domain radius of the obliterator for as long as it lasts: those inside find " +
            "their way out, those outside don't wander or chase in. One that gets hurt fights back for a while, and so does its pack nearby. Raid creatures are left alone, and a raid reaching the area pauses the rite. Replaces a " +
            "Carrion Rite going on.",
            true,
            amount: 9,
            minutes: 60f);

        internal static readonly ConfigInfo<string> PackwardExcluded = new(
            Packward.Enabled.Group,
            "Excluded Creatures",
            "Prefab names of the creatures the Packward Rite leaves alone, separated by commas (Lox, Deer...). Bosses and tamed " +
            "creatures are always left alone. Empty: every other creature shuns the area.",
            "",
            true);

        internal static readonly ConfigInfo<float> PackwardFightSeconds = new(
            Packward.Enabled.Group,
            "Fight Back Seconds",
            new ConfigDescription("For how many seconds after it was last hurt a creature fights instead of shunning the area. Its pack nearby (same faction, within sight) fights whoever it notices for as long. Either way it runs once it hasn't sensed a foe for about 5 seconds.",
                new AcceptableValueRange<float>(0f, 300f)),
            30f,
            true);

        internal static readonly RiteSettings Carrion = new(
            "4 - Domain - Carrion",
            "the Carrion Rite (creatures come)",
            "Pukeberries",
            "bukeperries",
            "Creatures (not bosses, not tamed) within the Domain radius walk to the obliterator, out of the woods and into your " +
            "traps, pens or walls. Their senses dull while they do, but one that notices someone reacts as usual, then walks on " +
            "once it calms down. Raid creatures are left alone, and a raid reaching the area pauses the rite. Replaces a Packward Rite going on.",
            true,
            amount: 3,
            minutes: 15f);

        internal static readonly ConfigInfo<string> CarrionExcluded = new(
            Carrion.Enabled.Group,
            "Excluded Creatures",
            "Prefab names of the creatures the Carrion Rite doesn't draw, separated by commas (Troll, Fenring...). Bosses and " +
            "tamed creatures are never drawn. Empty: every other creature comes.",
            "",
            true);

        internal static readonly ConfigInfo<float> CarrionSenses = new(
            Carrion.Enabled.Group,
            "Senses While Drawn",
            new ConfigDescription("How far a drawn creature sees and hears, as a share of its usual range (0.4: 40%). Once it " +
                "notices someone, it has its full senses until it calms down.", new AcceptableValueRange<float>(0.05f, 1f)),
            0.6f,
            true);

        internal static readonly RiteSettings Verdant = new(
            "4 - Domain - Verdant",
            "the Verdant Rite (plants grow faster)",
            "Root",
            "roots",
            "Plants, crops, saplings and berry bushes within the Domain radius of the obliterator grow faster for as long as it " +
            "lasts, some now and then giving off green motes; what they grew stays after it ends.",
            true,
            amount: 3,
            minutes: 45f);

        internal static readonly ConfigInfo<float> VerdantGrowthMultiplier = new(
            Verdant.Enabled.Group,
            "Growth Speed Multiplier",
            new ConfigDescription(
                "How many times as fast things grow under the Verdant Rite: 2 grows twice as fast, 1.5 half again as fast.",
                new AcceptableValueRange<float>(1f, 10f)),
            2f,
            true);

        internal static readonly RiteSettings VatBoil = new(
            "4 - Domain - Vat-Boil",
            "the Vat-Boil Rite (fermenting and cooking go faster)",
            "Bilebag",
            "bile bags",
            "Fermenters and cooking stations (cooking station, iron cooking station, oven) within the Domain radius of the obliterator " +
            "work faster, some now and then giving off golden motes, and nothing cooked there burns while it lasts; what they did stays " +
            "after it ends. 20 minutes at 2x: about one fermentation.",
            true,
            amount: 2,
            minutes: 20f);

        internal static readonly ConfigInfo<float> VatBoilSpeedMultiplier = new(
            VatBoil.Enabled.Group,
            "Speed Multiplier",
            new ConfigDescription(
                "How many times as fast fermenters and cooking stations work under the Vat-Boil Rite: 2 is twice as fast.",
                new AcceptableValueRange<float>(1f, 10f)),
            2f,
            true);

        internal static readonly RiteSettings PitchBellows = new(
            "4 - Domain - Pitch-Bellows",
            "the Pitch-Bellows Rite (smelters and kilns work faster)",
            "CharredBone",
            "charred bones",
            "Smelters, blast furnaces and charcoal kilns (not eitr refineries) within the Domain radius of the obliterator work faster, some " +
            "now and then giving off ember motes, using up ore and fuel as usual; what they made stays after it ends.",
            true,
            amount: 8);

        internal static readonly ConfigInfo<float> PitchBellowsSpeedMultiplier = new(
            PitchBellows.Enabled.Group,
            "Speed Multiplier",
            new ConfigDescription(
                "How many times as fast smelters and kilns work under the Pitch-Bellows Rite: 2 is twice as fast. They use ore and fuel " +
                "as usual, so fuel runs out sooner too.",
                new AcceptableValueRange<float>(1f, 10f)),
            2f,
            true);

        internal static readonly RiteSettings Kindred = new(
            "4 - Domain - Kindred",
            "the Kindred Rite (tamed animals breed and grow faster)",
            "ChickenEgg",
            "eggs",
            "Tamed animals within the Domain radius of the obliterator breed faster: they fall pregnant and give birth (hens lay) " +
            "sooner, their young grow up faster, and more of them can live close together before they stop breeding. Now and " +
            "then a few of them give off hearts. What they did stays after it ends.",
            true,
            amount: 10,
            minutes: 60f);

        internal static readonly ConfigInfo<float> KindredGrowthMultiplier = new(
            Kindred.Enabled.Group,
            "Breeding Speed Multiplier",
            new ConfigDescription(
                "How many times as fast tamed animals under the Kindred Rite fall pregnant, carry their young (hens: lay eggs) and " +
                "grow up, and warm eggs hatch: 2 is twice as fast.",
                new AcceptableValueRange<float>(1f, 10f)),
            2f,
            true);

        internal static readonly ConfigInfo<float> KindredCrowdMultiplier = new(
            Kindred.Enabled.Group,
            "Crowd Limit Multiplier",
            new ConfigDescription(
                "Tamed animals stop breeding once too many of their kind are close by (4 boars within 10 m, say). Under the Kindred " +
                "Rite that limit is this many times as high: 2 lets twice as many live together.",
                new AcceptableValueRange<float>(1f, 10f)),
            2f,
            true);

        internal static readonly RiteSettings HarrowSpur = new(
            "4 - Domain - Harrow-Spur",
            "the Harrow-Spur Rite (creature spawners spawn faster)",
            "Ectoplasm",
            "ectoplasm",
            "Creature spawners within the Domain radius of the obliterator (Greydwarf nests, Draugr body piles, bone piles, " +
            "charred spawners, any object with the game's spawner part, from other mods too) spawn creatures faster and keep " +
            "more of them around before they stop. For farming what they drop.",
            true,
            amount: 3);

        internal static readonly ConfigInfo<float> HarrowSpurSpeedMultiplier = new(
            HarrowSpur.Enabled.Group,
            "Spawn Speed Multiplier",
            new ConfigDescription(
                "How many times as fast spawners under the Harrow-Spur Rite spawn: 3 is three times as fast (a Greydwarf nest " +
                "every 10 seconds instead of 30).",
                new AcceptableValueRange<float>(1f, 10f)),
            3f,
            true);

        internal static readonly ConfigInfo<float> HarrowSpurCapMultiplier = new(
            HarrowSpur.Enabled.Group,
            "Creature Cap Multiplier",
            new ConfigDescription(
                "A spawner stops once enough of its creatures are around it (a Greydwarf nest: 3 close by). Under the " +
                "Harrow-Spur Rite that limit, close by and in all, is this many times as high: 2 lets twice as many gather.",
                new AcceptableValueRange<float>(1f, 10f)),
            2f,
            true);

        internal static readonly ConfigInfo<string> HarrowSpurExcluded = new(
            HarrowSpur.Enabled.Group,
            "Excluded Spawners",
            "Prefab names of the spawners the Harrow-Spur Rite leaves alone, separated by commas (Spawner_GreydwarfNest, " +
            "BonePileSpawner...). The names of the spawners it reaches are written to the BepInEx log the first time. " +
            "Empty: every spawner in its area.",
            "",
            true);

        internal static readonly RiteSettings AmberMend = new(
            "4 - Domain - Amber-Mend",
            "the Amber-Mend Rite (buildings mend)",
            "Resin",
            "resin",
            "Damaged building pieces within the Domain radius of the obliterator are repaired one after another, a few each second, " +
            "with the hammer's repair effect.",
            true,
            amount: 20,
            minutes: 5f);

        internal static readonly RiteSettings SovereignWard = new(
            "4 - Domain - Sovereign Ward",
            "the Sovereign Ward Rite (buildings can't be harmed)",
            "QueenDrop",
            "a majestic carapace",
            "Built pieces within the Domain radius of the obliterator (walls, portals, chests, workbenches...) take no damage from " +
            "anything: they can only be taken down on purpose, with the hammer. Hostile projectiles (Gjall fireballs, catapult " +
            "rocks, drake frost) crumble harmlessly as they cross into it, or a tenth of a second after being shot within it.",
            true,
            minutes: 60f);

        internal static readonly ConfigInfo<Toggle> SovereignSanctuary = new(
            SovereignWard.Enabled.Group,
            "Sanctuary Mode",
            "On: players within the ward can't be hurt either, by anything, so the base is a true safe place.",
            Toggle.Off,
            true);

        internal static readonly RiteSettings HearthHaul = new(
            "4 - Domain - Hearth-Haul",
            "the Hearth-Haul Rite (players carry more)",
            "BlackMetalScrap",
            "black metal scrap",
            "Players within the Domain radius of the obliterator can carry more, and carrying too much only slows them, as " +
            "usual: walking with too much uses up no stamina, and stamina comes back as usual. For hauling ore and building " +
            "material home.",
            true,
            amount: 3,
            minutes: 30f);

        internal static readonly ConfigInfo<float> HearthHaulCarryBonus = new(
            HearthHaul.Enabled.Group,
            "Carry Weight Bonus",
            new ConfigDescription(
                "How much more players under the Hearth-Haul Rite can carry: 300 doubles a player's usual 300 (a Megingjord " +
                "belt adds 150 on top). The world's carry weight modifier applies to it too.",
                new AcceptableValueRange<float>(0f, 2000f)),
            300f,
            true);

        internal static readonly ConfigInfo<Toggle> HearthHaulFreeStamina = new(
            HearthHaul.Enabled.Group,
            "Free Stamina",
            "On: under the Hearth-Haul Rite, carrying too much only slows players (no running, jumping or dodging, as usual): " +
            "walking uses up no stamina and stamina comes back as usual. Off: carrying too much costs stamina as usual.",
            Toggle.On,
            true);

        internal static readonly RiteSettings AetherClear = new(
            "4 - Domain - Aether-Clear",
            "the Aether-Clear Rite (no Mistlands mist in the area)",
            "Wisp",
            "wisps",
            "The Domain radius of the obliterator is free of Mistlands mist, as around a Wisplight, only far bigger: no mist is " +
            "made there, what drifts in is pushed back out, and creatures inside see as if there were no mist.",
            true,
            amount: 20,
            minutes: 60f);

        internal static readonly RiteSettings ElderSprout = new(
            "4 - Domain - Elder-Sprout",
            "the Elder-Sprout Rite (tree stumps grow back as saplings)",
            "AncientSeed",
            "ancient seeds",
            "Tree stumps within the Domain radius of the obliterator are taken away one after another, about one each second, each " +
            "with a puff of splinters, and a sapling of the same tree is planted where it stood, turned a random way. Trees felled " +
            "while it goes on sprout too. Stumps of trees no sapling grows into (swamp trees, the old meadow stumps) stay.",
            true,
            amount: 15,
            minutes: 5f);

        internal static readonly RiteSettings SentrySight = new(
            "4 - Domain - Sentry-Sight",
            "the Sentry-Sight Rite (ballistas shoot every foe)",
            "MechanicalSpring",
            "a mechanical spring",
            "Ballistas within the Domain radius of the obliterator aim at every hostile creature they see, whatever trophies are " +
            "set in them, and never at players or tamed animals. They still need bolts.",
            true,
            minutes: 60f);

        internal static readonly ConfigInfo<Toggle> SentrySpareAnimals = new(
            SentrySight.Enabled.Group,
            "Spare Passive Animals",
            "On: ballistas under the Sentry-Sight Rite leave alone animals that only flee (deer, hares...). Off: they shoot " +
            "those too, like any other creature.",
            Toggle.On,
            true);

        internal static readonly RiteSettings TaprootSurge = new(
            "4 - Domain - Taproot-Surge",
            "the Taproot-Surge Rite (sap flows faster)",
            "ProustitePowder",
            "proustite powder",
            "Sap extractors and eitr refineries within the Domain radius of the obliterator work faster (refineries using soft " +
            "tissue and sap as usual), and ancient Yggdrasil roots there fill up with sap faster. Some now and then give off " +
            "glowing sap drops. What they made stays after it ends.",
            true,
            amount: 2,
            minutes: 60f);

        internal static readonly ConfigInfo<float> TaprootSpeedMultiplier = new(
            TaprootSurge.Enabled.Group,
            "Speed Multiplier",
            new ConfigDescription(
                "How many times as fast sap extractors and eitr refineries work under the Taproot-Surge Rite: 2 is twice as fast.",
                new AcceptableValueRange<float>(1f, 10f)),
            2f,
            true);

        internal static readonly ConfigInfo<float> TaprootRootMultiplier = new(
            TaprootSurge.Enabled.Group,
            "Root Refill Multiplier",
            new ConfigDescription(
                "How many times as fast ancient Yggdrasil roots fill up with sap again under the Taproot-Surge Rite: 2 is twice as fast.",
                new AcceptableValueRange<float>(1f, 10f)),
            2f,
            true);

        internal static readonly RiteSettings MantleAnchor = new(
            "4 - Domain - Pitch-Seal",
            "the Pitch-Seal Rite (dropped items stay)",
            "Tar",
            "tar",
            "Items lying on the ground within the Domain radius of the obliterator never despawn, even far from any workbench: " +
            "for farms where creatures spawn and die on their own. Creatures still spawn. Once it ends, each item's usual hour " +
            "starts from then.",
            true,
            amount: 3,
            minutes: 60f);

        internal static readonly RiteSettings PrimalHearth = new(
            "4 - Domain - Primal-Hearth",
            "the Primal-Hearth Rite (wisps and embers come faster)",
            "SulfurStone",
            "sulfur",
            "Wisp fountains and eternal pyres within the Domain radius of the obliterator draw wisps and embers more often and " +
            "hold more of them before they are full.",
            true,
            minutes: 15f);

        internal static readonly ConfigInfo<float> PrimalHearthSpawnMultiplier = new(
            PrimalHearth.Enabled.Group,
            "Draw Multiplier",
            new ConfigDescription(
                "How many times as often a wisp fountain or eternal pyre tries to draw a wisp or ember under the Primal-Hearth Rite: 2 is twice as often (the game tries every 10 seconds, so every 5 seconds).",
                new AcceptableValueRange<float>(1f, 10f)),
            2f,
            true);

        internal static readonly ConfigInfo<float> PrimalHearthCapacityMultiplier = new(
            PrimalHearth.Enabled.Group,
            "Capacity Multiplier",
            new ConfigDescription(
                "How many times as many wisps or embers a wisp fountain or eternal pyre holds before it is full under the Primal-Hearth Rite: 2 is twice as many.",
                new AcceptableValueRange<float>(1f, 10f)),
            2f,
            true);

        internal static readonly RiteSettings HearthKindle = new(
            "4 - Domain - Hearth-Kindle",
            "the Hearth-Kindle Rite (rest comes faster)",
            "FaderEmber",
            "embers",
            "Players resting by a fire within the Domain radius of the obliterator become Rested faster, and that Rested lasts " +
            "longer. Burning fires there now and then give off little rest signs.",
            true,
            amount: 3,
            minutes: 60f);

        internal static readonly ConfigInfo<float> KindleRestingMultiplier = new(
            HearthKindle.Enabled.Group,
            "Resting Speed Multiplier",
            new ConfigDescription(
                "How many times as fast resting by a fire makes players Rested under the Hearth-Kindle Rite: 2 is twice as fast.",
                new AcceptableValueRange<float>(1f, 10f)),
            2f,
            true);

        internal static readonly ConfigInfo<float> KindleRestedMultiplier = new(
            HearthKindle.Enabled.Group,
            "Rested Duration Multiplier",
            new ConfigDescription(
                "How many times as long the Rested players get by a fire under the Hearth-Kindle Rite lasts: 1.5 is half as long again.",
                new AcceptableValueRange<float>(1f, 10f)),
            1.5f,
            true);

        internal static readonly RiteSettings Unmaking = new(
            "7 - Reckoning - Unmaking",
            "the Unmaking Rite (gear back into materials)",
            "Thunderstone",
            "a thunderstone",
            "Every craftable weapon, tool, shield and piece of armor sacrificed along with it gives back part of what it took to make " +
            "and upgrade it (station materials up to level 4; the upgrade altar's materials, like battle idols, for levels past 4 " +
            "if Return Upgrade Materials is on). Everything else is obliterated as usual.",
            true,
            lasts: false);

        internal static readonly ConfigInfo<float> UnmakingReturn = new(
            Unmaking.Enabled.Group,
            "Return Multiplier",
            new ConfigDescription("How much of the full cost comes back (rounded down per material, but at least 1 of each): 0.8 gives back 80%.", new AcceptableValueRange<float>(0.01f, 1f)),
            0.8f,
            true);

        internal static readonly ConfigInfo<Toggle> UnmakingUpgradeMaterials = new(
            Unmaking.Enabled.Group,
            "Return Upgrade Materials",
            "On: gear upgraded past level 4 also gives back part of the upgrade altar's materials (battle idols) paid for levels 5, 6 " +
            "and up. Off: those never come back. Station materials (black metal, linen thread...) only count up to level 4 either way.",
            Toggle.On,
            true);

        internal static readonly RiteSettings Grounding = new(
            "4 - Domain - Grounding",
            "the Grounding Rite (ends the Domain rites)",
            "Flint",
            "flint",
            "Ends the Domain rites going on (Packward, Carrion, Verdant, Vat-Boil, Pitch-Bellows, Kindred, Harrow-Spur, Amber-Mend, Sovereign Ward, Hearth-Haul, " +
            "Aether-Clear, Elder-Sprout, Sentry-Sight, Taproot-Surge, Pitch-Seal, Primal-Hearth, Hearth-Kindle) at once.",
            true,
            lasts: false,
            amount: 2);

        // ---- Blessings: only for the players within the blessing range when the obliterator burns ----

        private const string BlessingNote = " A blessing: only players within the Blessings Range of the obliterator when its thunder strikes " +
            "receive it, struck by its lightning (no harm done). It ends at death.";

        internal static readonly RiteSettings CryptDelver = new(
            "5 - Blessings - Crypt Delver",
            "the Crypt Delver Rite (more dungeon loot)",
            "CryptKey",
            "a swamp key",
            "The first time you open a chest in a dungeon (crypts, caves, infested mines...), its items are multiplied by the " +
            "Loot Multiplier, rounded up, with a chance of one or two more; the extra items can be anything the chest can hold. " +
            "Each chest gives this once." + BlessingNote,
            true,
            minutes: 45f);

        internal static readonly ConfigInfo<float> LootMultiplier = new(
            CryptDelver.Enabled.Group,
            "Loot Multiplier",
            new ConfigDescription("How many times the items it rolled a dungeon chest gets, rounded up: 1.5 turns 1 item into 2 and 3 into 5, 2 doubles them. 1: no more.", new AcceptableValueRange<float>(1f, 4f)),
            1.5f,
            true);

        internal static readonly ConfigInfo<float> OneExtraChance = new(
            CryptDelver.Enabled.Group,
            "One Extra Item Probability",
            new ConfigDescription("The chance that a dungeon chest gets one more item on top, from 0 to 1: 0.2 is 20%.", new AcceptableValueRange<float>(0f, 1f)),
            0.2f,
            true);

        internal static readonly ConfigInfo<float> TwoExtraChance = new(
            CryptDelver.Enabled.Group,
            "Two Extra Items Probability",
            new ConfigDescription("The chance that a dungeon chest gets two more items on top (instead of one), from 0 to 1: 0.05 is 5%.", new AcceptableValueRange<float>(0f, 1f)),
            0.05f,
            true);

        internal static readonly RiteSettings GlacialSatiety = new(
            "5 - Blessings - Glacial Satiety",
            "the Glacial Satiety Rite (food fades slower)",
            "FreezeGland",
            "freeze glands",
            "Eaten food fades only down to the Food Floor Multiplier of what it gives, instead of half, until it runs out." + BlessingNote,
            true,
            amount: 3,
            minutes: 45f);

        internal static readonly ConfigInfo<float> FoodFloor = new(
            GlacialSatiety.Enabled.Group,
            "Food Floor Multiplier",
            new ConfigDescription("The least health, stamina and eitr a food gives while it lasts, times its full value: 0.75 is 75% (the game's own floor is about 0.5).", new AcceptableValueRange<float>(0.5f, 1f)),
            0.75f,
            true);

        internal static readonly RiteSettings VeinShatter = new(
            "5 - Blessings - Vein-Shatter",
            "the Vein-Shatter Rite (rocks and ore shatter)",
            "HardAntler",
            "a hard antler",
            "Hitting anything mined in pieces (rocks, ore veins, mud piles, ice, giant swords and skulls...) deals part of the hit " +
            "to the pieces next to it, and once it is worn down far enough, the rest " +
            "breaks at once. Each hit wears the pickaxe as usual; the shatter wears it once more for each piece it breaks." + BlessingNote,
            true);

        internal static readonly ConfigInfo<float> VeinSpread = new(
            VeinShatter.Enabled.Group,
            "Spread Multiplier",
            new ConfigDescription("How much of a hit the neighboring pieces take: 0.3 is 30% of it.", new AcceptableValueRange<float>(0f, 1f)),
            0.3f,
            true);

        internal static readonly ConfigInfo<float> VeinShatterAt = new(
            VeinShatter.Enabled.Group,
            "Shatter At Health Multiplier",
            new ConfigDescription("Once the mineable has this much of its full health left or less, the rest breaks: 0.5 is at half health. 0: never.", new AcceptableValueRange<float>(0f, 1f)),
            0.5f,
            true);

        internal static readonly ConfigInfo<string> VeinExcluded = new(
            VeinShatter.Enabled.Group,
            "Excluded",
            "Mineables that don't shatter, separated by commas: the prefab name of the mineable itself (rock4_copper_frac, " +
            "giant_sword1...) or of an item it drops (SilverOre leaves out every silver vein). Empty: everything shatters.",
            "",
            true);

        internal static readonly RiteSettings CarapaceTemper = new(
            "5 - Blessings - Carapace Temper",
            "the Carapace Temper Rite (gear wears slower)",
            "Mandible",
            "mandibles",
            "Equipped weapons, tools, bows, shields and armor last longer before they break." + BlessingNote,
            true,
            amount: 2);

        internal static readonly ConfigInfo<float> DurabilityMultiplier = new(
            CarapaceTemper.Enabled.Group,
            "Durability Multiplier",
            new ConfigDescription("How many times longer equipped gear lasts: 1.75 is 75% longer (it loses 1/1.75 of the usual durability).", new AcceptableValueRange<float>(1f, 10f)),
            1.75f,
            true);

        internal static readonly RiteSettings GraveBind = new(
            "5 - Blessings - Grave-Bind",
            "the Grave-Bind Rite (no skill loss on death)",
            "YagluthDrop",
            "a torn spirit",
            "Dying doesn't lower your skills (with or without the No Skill Drain status), and your tombstone shines a beam into the " +
            "sky that can be seen from far away until it is emptied; looting it puts your gear back on. The death ends it." + BlessingNote,
            true,
            minutes: 60f);

        internal static readonly RiteSettings FortunesBoon = new(
            "5 - Blessings - Fortunes Boon",
            "the Fortune's Boon Rite (more luck)",
            "Wishbone",
            "a wishbone",
            "Your skills' lucky rolls for extra items come more often: crafting and cauldron cooking, the cooking station, and " +
            "harvesting crops and other pickables. At the Forge of Potential, the first upgrade that would destroy your item " +
            "doesn't: the item loses the Forge Break Penalty in levels instead, and that ends the blessing." + BlessingNote,
            true,
            minutes: 15f);

        internal static readonly ConfigInfo<float> LuckMultiplier = new(
            FortunesBoon.Enabled.Group,
            "Luck Multiplier",
            new ConfigDescription("How many times as likely a skill's lucky roll for extra items is (up to always): 2 turns a 25% chance into 50%.", new AcceptableValueRange<float>(1f, 20f)),
            2f,
            true);

        internal static readonly ConfigInfo<int> ForgeBreakPenalty = new(
            FortunesBoon.Enabled.Group,
            "Forge Break Penalty",
            new ConfigDescription("Levels an item loses at the Forge of Potential instead of being destroyed (never below level 1). " +
                "1: as when an upgrade fails. 0: it keeps its level.", new AcceptableValueRange<int>(0, 10)),
            1,
            true);

        internal static readonly RiteSettings Luminary = new(
            "5 - Blessings - Luminary",
            "the Luminary Rite (you shine)",
            "Lantern",
            "a Dvergr Lantern",
            "You shine with a warm light like a lantern, lighting the ground and walls around you, in caves and dungeons too, " +
            "and everyone with the mod sees it." + BlessingNote,
            true,
            minutes: 60f);

        internal static readonly ConfigInfo<float> LuminaryRange = new(
            Luminary.Enabled.Group,
            "Light Range",
            new ConfigDescription("How far around you the light reaches, in meters.", new AcceptableValueRange<float>(3f, 50f)),
            20f,
            true);

        internal static readonly RiteSettings AbyssalDraw = new(
            "5 - Blessings - Abyssal Draw",
            "the Abyssal Draw Rite (items fly to you)",
            "Chitin",
            "chitin",
            "Items on the ground fly to you from much farther than the game's 2 m. An item a closer player can pick up goes to " +
            "them instead, unless their inventory is full." + BlessingNote,
            true,
            amount: 4,
            minutes: 60f);

        internal static readonly ConfigInfo<float> DrawRange = new(
            AbyssalDraw.Enabled.Group,
            "Pickup Range",
            new ConfigDescription("How far away items fly to you from, in meters.", new AcceptableValueRange<float>(2f, 30f)),
            10f,
            true);

        internal static readonly RiteSettings FairWind = new(
            "5 - Blessings - Fair-Wind",
            "the Fair-Wind Rite (never a headwind)",
            "DragonEgg",
            "a dragon egg",
            "While you sail, the wind never blows against your ship, also for everyone on the same ship: steer into it and it " +
            "turns aside, just enough for the sail to catch it. Any other wind stays as the weather made it. Only its direction: " +
            "how strong it is stays the weather's (the Gale-Breath Rite makes it the strongest)." + BlessingNote,
            true);

        internal static readonly RiteSettings HushFelling = new(
            "5 - Blessings - Hush-Felling",
            "the Hush-Felling Rite (the wild ignores your toil)",
            "GoblinTotem",
            "a fuling totem",
            "The wild ignores your toil: chopping, pickaxing and harvesting make no sound, falling logs alert no one, and while " +
            "you gather, wild creatures have to come closer to see you. Fighting is as loud as ever." + BlessingNote,
            true,
            minutes: 60f);

        internal static readonly ConfigInfo<float> HushNoticeMultiplier = new(
            HushFelling.Enabled.Group,
            "Notice Multiplier",
            new ConfigDescription(
                "While you gather under the Hush-Felling Rite, wild creatures (not bosses or tamed) see you only from 1/this " +
                "as far as usual: 2 means they have to come twice as close (from 50 m down to 25 m). Gathering: " +
                "a few seconds after you last hit a tree, log, rock or bush, unless you fought since. 1: as usual.",
                new AcceptableValueRange<float>(1f, 5f)),
            2f,
            true);

        internal static readonly RiteSettings DraughtFlush = new(
            "5 - Blessings - Draught-Flush",
            "the Draught-Flush Rite (meads work longer, recover sooner)",
            "RoyalJelly",
            "royal jelly",
            "Recovery meads (health, stamina, eitr) can be drunk again sooner, and every other mead (lingering meads, resistance " +
            "meads, barley wine and the like) lasts longer. Only meads drunk while blessed." + BlessingNote,
            true,
            amount: 5,
            minutes: 45f);

        internal static readonly ConfigInfo<float> DraughtRecoveryMultiplier = new(
            DraughtFlush.Enabled.Group,
            "Recovery Wait Multiplier",
            new ConfigDescription(
                "Under the Draught-Flush Rite, a recovery mead (one that restores health, stamina or eitr within a short time) " +
                "waits this times as long before another of its kind can be drunk: 0.5 halves the wait. What it restores, and " +
                "how fast, stays the same; the wait is never shorter than the restoring. 1: as usual.",
                new AcceptableValueRange<float>(0.1f, 1f)),
            0.5f,
            true);

        internal static readonly ConfigInfo<float> DraughtBuffMultiplier = new(
            DraughtFlush.Enabled.Group,
            "Mead Duration Multiplier",
            new ConfigDescription(
                "Under the Draught-Flush Rite, every other mead (lingering meads, resistance meads, barley wine, tasty mead and " +
                "the like) lasts this times as long: 1.5 makes a 10 minute mead last 15. Anything it restores over its whole " +
                "time is restored at the same pace, for longer. 1: as usual.",
                new AcceptableValueRange<float>(1f, 5f)),
            1.5f,
            true);

        internal static readonly RiteSettings SoulTether = new(
            "5 - Blessings - Soul-Tether",
            "the Soul-Tether Rite (your tombstones come to the obliterator)",
            "UndeadBjornRibcage",
            "a vile ribcage",
            "Every tombstone of each blessed player, wherever in the world it lies, is pulled to the obliterator: set down " +
            "around it on the ground it stands on, spread out so none sits in another or in anything there. It happens at " +
            "once. A blessing: only players within the Blessings Range of the obliterator when its thunder strikes receive it, struck by " +
            "its lightning (no harm done).",
            true,
            lasts: false);

        internal static readonly ConfigInfo<float> TetherLeaveWithin = new(
            SoulTether.Enabled.Group,
            "Leave Tombstones Within",
            new ConfigDescription(
                "Under the Soul-Tether Rite, a tombstone of yours already lying this many meters or closer to the obliterator " +
                "stays where it is; any farther away is pulled in next to it. Lower it to pull in tombstones close by too: 0 " +
                "pulls every one (those already by the obliterator may be set down again in another free spot).",
                new AcceptableValueRange<float>(0f, 16f)),
            16f,
            true);

        internal static readonly RiteSettings TundraShroud = new(
            "5 - Blessings - Tundra Shroud",
            "the Tundra Shroud Rite (rain and night don't chill you)",
            "LeatherScraps, DeerHide, TrollHide, BjornHide, WolfPelt, LoxPelt, ScaleHide, AskHide, MooseHide, SealHide",
            "different hides and pelts",
            "Rain no longer makes you Wet and nightfall (or any merely cold weather) no longer makes you Cold. Freezing (the " +
            "mountains, blizzards, the Deep North's frost) is untouched: that still needs frost resistance or a fire. Water " +
            "still makes you Wet when you swim or wade in it, but you dry off as fast as by a fire." + BlessingNote,
            true,
            amount: 3,
            minutes: 30f,
            distinctKeys: true);

        internal static readonly ConfigInfo<float> ShroudDryingMultiplier = new(
            TundraShroud.Enabled.Group,
            "Drying Speed Multiplier",
            new ConfigDescription(
                "Under the Tundra Shroud Rite, Wet (from swimming or wading) wears off this times as fast: 11 is as fast as " +
                "drying by a fire. By a fire it dries as fast as it does there. 1: as usual.",
                new AcceptableValueRange<float>(1f, 50f)),
            11f,
            true);

        internal static readonly RiteSettings ScaldDrift = new(
            "5 - Blessings - Scald-Drift",
            "the Scald-Drift Rite (boiling seas and lava burn less)",
            "BonemawSerpentTooth",
            "a bonemaw tooth",
            "The Ashlands' boiling sea burns you far less, and lava no longer kills you at once." + BlessingNote,
            true,
            minutes: 45f);

        internal static readonly ConfigInfo<float> ScaldDamageMultiplier = new(
            ScaldDrift.Enabled.Group,
            "Boiling Water Damage Multiplier",
            new ConfigDescription(
                "How much damage the Ashlands' boiling sea does under the Scald-Drift Rite: 0.25 is a quarter of it.",
                new AcceptableValueRange<float>(0f, 1f)),
            0.25f,
            true);

        internal static readonly ConfigInfo<float> ScaldTickSlowdown = new(
            ScaldDrift.Enabled.Group,
            "Boiling Water Burn Interval Multiplier",
            new ConfigDescription(
                "How many times as long the boiling sea waits between burns under the Scald-Drift Rite: 10 burns ten times less often.",
                new AcceptableValueRange<float>(1f, 50f)),
            10f,
            true);

        internal static readonly ConfigInfo<float> ScaldLavaDamageMultiplier = new(
            ScaldDrift.Enabled.Group,
            "Lava Damage Multiplier",
            new ConfigDescription(
                "How much damage lava does under the Scald-Drift Rite: 0.5 is half of it. Lava stays deadly, just not at once.",
                new AcceptableValueRange<float>(0f, 1f)),
            0.5f,
            true);

        internal static readonly ConfigInfo<float> ScaldLavaTickSlowdown = new(
            ScaldDrift.Enabled.Group,
            "Lava Burn Interval Multiplier",
            new ConfigDescription(
                "How many times as long lava waits between burns under the Scald-Drift Rite: 5 burns five times less often.",
                new AcceptableValueRange<float>(1f, 50f)),
            5f,
            true);

        internal static readonly RiteSettings TetherLock = new(
            "5 - Blessings - Tether-Lock",
            "the Tether-Lock Rite (your harpoon holds)",
            "MorgenSinew",
            "morgen sinew",
            "Your harpoon line is harder to snap, pulling on it uses less stamina, and it reels what it holds in faster. It works " +
            "fully when the harpooned creature is handled by a game with the mod (usually yours)." + BlessingNote,
            true);

        internal static readonly ConfigInfo<float> TetherStrengthMultiplier = new(
            TetherLock.Enabled.Group,
            "Line Strength Multiplier",
            new ConfigDescription(
                "How many times as far your harpoon line stretches before it snaps under the Tether-Lock Rite: 2 is twice as far.",
                new AcceptableValueRange<float>(1f, 10f)),
            2f,
            true);

        internal static readonly ConfigInfo<float> TetherStaminaMultiplier = new(
            TetherLock.Enabled.Group,
            "Pull Stamina Multiplier",
            new ConfigDescription(
                "How much stamina pulling on your harpoon line uses under the Tether-Lock Rite: 0.5 is half.",
                new AcceptableValueRange<float>(0f, 1f)),
            0.5f,
            true);

        internal static readonly ConfigInfo<float> TetherReelMultiplier = new(
            TetherLock.Enabled.Group,
            "Reel Speed Multiplier",
            new ConfigDescription(
                "How many times as fast your harpoon line reels in what it holds under the Tether-Lock Rite: 1.5 is half as fast again.",
                new AcceptableValueRange<float>(1f, 5f)),
            1.5f,
            true);

        internal static readonly RiteSettings StriderGirth = new(
            "5 - Blessings - Strider-Girth",
            "the Strider-Girth Rite (your mount gets its breath back fast and fights for you)",
            "AskBladder",
            "an asksvin bladder",
            "The mount you ride gets its stamina back faster and attacks on your attack keys." + BlessingNote,
            true,
            minutes: 45f);

        internal static readonly ConfigInfo<float> StriderRegenMultiplier = new(
            StriderGirth.Enabled.Group,
            "Stamina Regen Multiplier",
            new ConfigDescription(
                "How many times as fast the mount you ride gets its stamina back under the Strider-Girth Rite: 10 is ten times as fast.",
                new AcceptableValueRange<float>(1f, 30f)),
            10f,
            true);

        internal static readonly ConfigInfo<float> StriderAttackStamina = new(
            StriderGirth.Enabled.Group,
            "Attack Stamina",
            new ConfigDescription(
                "How much stamina the mount uses for its attack (your attack key) under the Strider-Girth Rite. A mount has about 100. Without enough, it doesn't attack.",
                new AcceptableValueRange<float>(0f, 100f)),
            10f,
            true);

        internal static readonly ConfigInfo<float> StriderSecondaryStamina = new(
            StriderGirth.Enabled.Group,
            "Secondary Attack Stamina",
            new ConfigDescription(
                "How much stamina the mount uses for its second attack (your secondary attack key) under the Strider-Girth Rite.",
                new AcceptableValueRange<float>(0f, 100f)),
            20f,
            true);

        internal static readonly ConfigInfo<string> StriderSwappedMounts = new(
            StriderGirth.Enabled.Group,
            "Swapped Attack Mounts",
            "Prefab names of the mounts whose two attacks swap keys under the Strider-Girth Rite, separated by commas. By " +
            "default your attack key uses a mount's bite or swipe and the secondary attack key its stomp or slam.",
            "Asksvin,Moose",
            true);

        internal static readonly RiteSettings SecondKindle = new(
            "5 - Blessings - Second-Kindle",
            "the Second-Kindle Rite (cheat death once)",
            "FaderDrop",
            "kindled ribs",
            "The next blow that would kill you doesn't: you are left with part of your health, flames burst from you and foes " +
            "nearby are thrown back and staggered (no damage done). Then the blessing ends." + BlessingNote,
            true,
            minutes: 60f);

        internal static readonly ConfigInfo<float> KindleHealthShare = new(
            SecondKindle.Enabled.Group,
            "Health Restored Multiplier",
            new ConfigDescription(
                "How much of your health you are left with when the Second-Kindle Rite saves you: 0.4 is 40%.",
                new AcceptableValueRange<float>(0.05f, 1f)),
            0.4f,
            true);

        internal static readonly ConfigInfo<float> KindleKnockbackRadius = new(
            SecondKindle.Enabled.Group,
            "Knockback Radius",
            new ConfigDescription(
                "How far around you foes are thrown back when the Second-Kindle Rite saves you, in meters.",
                new AcceptableValueRange<float>(0f, 30f)),
            6f,
            true);

        internal static readonly RiteSettings PlumeWard = new(
            "5 - Blessings - Plume-Ward",
            "the Plume-Ward Rite (soft falls)",
            "CelestialFeather",
            "celestial feathers",
            "You take less fall damage, and the first fall that would still kill you does you no harm at all, which uses the " +
            "blessing up. Each softened landing gives off a puff of pale down." + BlessingNote,
            true,
            amount: 2,
            minutes: 60f);

        internal static readonly ConfigInfo<float> PlumeWardFallDamage = new(
            PlumeWard.Enabled.Group,
            "Fall Damage Multiplier",
            new ConfigDescription(
                "How much fall damage you take under the Plume-Ward Rite: 0.5 is half of it. A fall that would still kill you does no harm, and ends the blessing.",
                new AcceptableValueRange<float>(0f, 1f)),
            0.5f,
            true);

        internal static readonly RiteSettings MorgenPouch = new(
            "5 - Blessings - Morgen-Pouch",
            "the Morgen-Pouch Rite (carry your mount and pets)",
            "MorgenHeart",
            "morgen hearts",
            "While it lasts, the Morgen-Pouch ability beside the Guardian Power pulls the tamed creatures you petted last, near " +
            "you, into a pocket (a mount, a wolf, a boar, a chicken...), just as they are: health, stars, name and saddle. Use it " +
            "again to let them out around you. They go through portals with you. When it runs out, or you die, they come " +
            "out where you are. Summoned creatures can be pocketed too; a new summon over your limit takes the one that started " +
            "following you first, even from the pocket." + BlessingNote,
            true,
            amount: 3,
            minutes: 30f);

        internal static readonly ConfigInfo<int> MorgenPouchCount = new(
            MorgenPouch.Enabled.Group,
            "Creatures",
            new ConfigDescription(
                "How many tamed creatures the Morgen-Pouch Rite holds: those you petted last, within Capture Range.",
                new AcceptableValueRange<int>(1, 10)),
            3,
            true);

        internal static readonly ConfigInfo<float> MorgenPouchRange = new(
            MorgenPouch.Enabled.Group,
            "Capture Range",
            new ConfigDescription(
                "How far from you, at most, the tamed creatures you petted can be for the Morgen-Pouch Rite to take them, in meters.",
                new AcceptableValueRange<float>(2f, 30f)),
            10f,
            true);

        internal static readonly ConfigInfo<float> MorgenPouchReleaseCooldown = new(
            MorgenPouch.Enabled.Group,
            "Release Cooldown",
            new ConfigDescription(
                "How long after the Morgen-Pouch takes creatures before you can let them out, in seconds.",
                new AcceptableValueRange<float>(0f, 600f)),
            30f,
            true);

        internal static readonly ConfigInfo<float> MorgenPouchCaptureCooldown = new(
            MorgenPouch.Enabled.Group,
            "Capture Cooldown",
            new ConfigDescription(
                "How long after you let creatures out of the Morgen-Pouch before it can take any again, in seconds.",
                new AcceptableValueRange<float>(0f, 600f)),
            60f,
            true);

        internal static readonly RiteSettings TendrilDraw = new(
            "5 - Blessings - Tendril-Draw",
            "the Tendril-Draw Rite (arrows saved, spears return)",
            "WrithanRoots",
            "a writhan root",
            "Arrows and crossbow bolts you shoot sometimes aren't used up, and a spear you throw comes back to your hands " +
            "after it strikes a foe, a shield or the ground, from however far: the farther, the longer it flies." + BlessingNote,
            true,
            minutes: 45f);

        internal static readonly ConfigInfo<float> TendrilAmmoChance = new(
            TendrilDraw.Enabled.Group,
            "Ammo Saved Chance",
            new ConfigDescription(
                "The chance an arrow or crossbow bolt you shoot isn't used up under the Tendril-Draw Rite: 0.5 is one in two.",
                new AcceptableValueRange<float>(0f, 1f)),
            0.5f,
            true);

        internal static readonly ConfigInfo<float> TendrilReturnSpeed = new(
            TendrilDraw.Enabled.Group,
            "Spear Return Speed",
            new ConfigDescription(
                "How fast a thrown spear flies back to you under the Tendril-Draw Rite, in meters a second. One that strikes " +
                "close by is back in your hands at once.",
                new AcceptableValueRange<float>(5f, 200f)),
            20f,
            true);

        // ---- Kinship: for everyone on the server at once, wherever they are ----

        private const string KinshipNote = " A kinship rite: everyone on the server receives it at once, wherever they are.";

        internal static readonly RiteSettings Skal = new(
            "6 - Kinship - Skal",
            "the Skål Rite (everyone drinks and rests)",
            "MeadTasty",
            "tasty mead",
            "Everyone raises a toast, aglow like a stamina mead amid the crackle of a fire, and is Rested as if at comfort 3. " +
            "Nobody can be hurt while toasting. A player rested this way can't be rested by another Skål Rite for a while; a " +
            "player rested longer already is passed by." + KinshipNote,
            true,
            amount: 3,
            minutes: 10f,
            minutesDescription: "How long the Rested status the Skål Rite gives lasts, in minutes. A longer Rested a player " +
            "already has is kept.");

        internal static readonly ConfigInfo<float> SkalCooldown = new(
            Skal.Enabled.Group,
            "Cooldown Minutes",
            new ConfigDescription("How long after the Skål Rite rests a player no other one can rest them, in minutes of play. " +
                "Death doesn't end it. A player it didn't rest (rested longer already) gets no wait.",
                new AcceptableValueRange<float>(0.5f, 600f)),
            30f,
            true);

        internal static readonly RiteSettings SkyCall = new(
            "6 - Kinship - Sky-Call",
            "the Sky-Call Rite (forsaken powers renewed)",
            "YmirRemains",
            "Ymir flesh",
            "Everyone whose forsaken power is cooling down is struck by lightning (no harm done), roars, and has it ready again. " +
            "Nobody can be hurt while roaring. A player renewed this way can't be renewed by another Sky-Call Rite for a while; " +
            "a player whose power is ready already is passed by, and the next one can still renew them." + KinshipNote,
            true,
            minutes: 30f,
            minutesDescription: "How long after the Sky-Call Rite renews a player's forsaken power no other one can renew it, " +
            "in minutes of play. Death doesn't end it.");

        internal static readonly RiteSettings AnvilSpark = new(
            "6 - Kinship - Anvil-Spark",
            "the Anvil-Spark Rite (everyone's gear repaired)",
            "SharpeningStone",
            "a sharpening stone",
            "An anvil rings and sparks burst around everyone, who flexes and has every worn item in their inventory (equipped " +
            "gear included) repaired, as at a workbench or forge but needing none. Nobody can be hurt while flexing. A player " +
            "repaired this way can't be repaired by another Anvil-Spark Rite for a while; a player with nothing to repair is " +
            "passed by, and the next one can still repair them." + KinshipNote,
            true,
            minutes: 30f,
            minutesDescription: "How long after the Anvil-Spark Rite repairs a player's gear no other one can repair it, " +
            "in minutes of play. Death doesn't end it.");

        // Any of the feasts, the Meadows' to the Deep North's.
        internal static readonly RiteSettings Eldhrimnir = new(
            "6 - Kinship - Eldhrimnir",
            "the Eldhrímnir Rite (everyone's food fresh again)",
            "FeastMeadows_Material, FeastBlackforest_Material, FeastSwamps_Material, FeastOceans_Material, FeastMountains_Material, " +
            "FeastPlains_Material, FeastMistlands_Material, FeastAshlands_Material, FeastDeepNorth_Material",
            "a feast",
            "Everyone cheers, aglow like a health mead, and every food they have eaten is fresh again, as if just eaten: its " +
            "full time, health, stamina and eitr back. Nobody can be hurt while cheering. A player whose food had more than a " +
            "little of its time used up (Free Use Multiplier) can't be fed by another Eldhrímnir Rite for a while; one whose " +
            "food is all still fresh, or who has eaten nothing, is passed by." + KinshipNote,
            true,
            minutes: 60f,
            minutesDescription: "How long after the Eldhrímnir Rite makes a player's used food fresh again no other one can, " +
            "in minutes of play. Death doesn't end it.");

        internal static readonly ConfigInfo<float> EldhrimnirFreeUse = new(
            Eldhrimnir.Enabled.Group,
            "Free Use Multiplier",
            new ConfigDescription("How much of each food's time a player can have used up and still be passed by the " +
                "Eldhrímnir Rite, times its full time: 0.1 is 10%. Only a player any of whose foods had more used up is fed (and gets the wait).",
                new AcceptableValueRange<float>(0f, 1f)),
            0.1f,
            true);

        internal static readonly RiteSettings RallyHorn = new(
            "6 - Kinship - Rally-Horn",
            "the Rally-Horn Rite (everyone called to the obliterator)",
            "TankardAnniversary",
            "an anniversary tankard",
            "Everyone not at the obliterator (within the Blessings Range) hears the call: for Call Seconds a Rally Call " +
            "status and a Rally Call slot beside the Guardian Power show, and pressing the Rally Call Shortcut brings them " +
            "to the obliterator: they kneel, lightning strikes where they kneel (no harm done) and they vanish, then arrive " +
            "kneeling around the obliterator on the same level in another strike, and soon stand up, free to move; meanwhile " +
            "nobody can hurt them. As with a portal, a player carrying anything that can't go through one can't answer " +
            "until it is put away, and nobody can answer in the middle of a fight. A " +
            "player who answers can't answer another Rally-Horn Rite for a while; letting the call fade costs nothing." + KinshipNote,
            true,
            minutes: 60f,
            minutesDescription: "How long after answering the Rally-Horn Rite's call a player can't answer another, in minutes " +
            "of play. Death doesn't end it. Letting a call fade starts no wait.");

        internal static readonly ConfigInfo<float> RallyCallSeconds = new(
            RallyHorn.Enabled.Group,
            "Call Seconds",
            new ConfigDescription("How long each player has to answer the Rally-Horn Rite's call, in seconds.",
                new AcceptableValueRange<float>(10f, 600f)),
            120f,
            true);

        // ---- Reckoning ----

        internal static readonly RiteSettings Crimson = new(
            "7 - Reckoning - Crimson",
            "the Crimson Rite (raid after raid)",
            "WitheredBone",
            "withered bones",
            "Raids keep coming to the obliterator, one after another, picked among those the players within the radius are ready " +
            "for (player based raids and World Advancement Progression's private raids too), with no wait and no base needed; " +
            "lightning strikes the obliterator as each new raid comes. No other raid starts anywhere meanwhile, and raid " +
            "creatures only spawn within the radius. A shield bubble covers " +
            "the radius: from when it starts, players inside can't get out and players outside can't get in, and no portal " +
            "into or out of it works, until it ends. No creature inside can get out, and none outside can get in. Nothing can " +
            "be built, repaired or torn down inside, and no rite but the Sanctuary Rite performed there. " +
            "The next raid comes almost at once; the creatures of the last " +
            "raid still inside die as it comes. Each creature slain inside counts; survive to the end and the obliterator, struck by lightning, holds a reward for each kill. " +
            "However it ends, the hostile creatures still inside die and drop nothing (not counted; spawners and nests are left " +
            "alone). The obliterator can't be destroyed while it goes on and no creature goes after it; a beam of light rises from it. " +
            "It breaks off early if everyone inside falls or leaves, or the Sanctuary " +
            "Rite ends it. Only one Crimson or Ragnarök Rite can go on at a time, and no Domain rite works " +
            "within its bubble: one at an obliterator inside is cancelled. Its status and messages only " +
            "show near the obliterator; its area is marked on everyone's map.",
            true,
            amount: 5,
            minutes: 10f,
            radius: 50f);

        internal static readonly ConfigInfo<string> CrimsonRewardItem = new(
            Crimson.Enabled.Group,
            "Reward Item",
            "Prefab name of the item put into the obliterator for each kill when the Crimson Rite is survived (default Coins).",
            "Coins",
            true);

        internal static readonly ConfigInfo<int> CrimsonRewardPerKill = new(
            Crimson.Enabled.Group,
            "Reward Per Kill",
            new ConfigDescription("How many of the reward item each creature slain within the bubble gives, when the Crimson Rite is survived.",
                new AcceptableValueRange<int>(0, 100)),
            1,
            true);

        internal static readonly ConfigInfo<float> CrimsonRewardPerPlayer = new(
            Crimson.Enabled.Group,
            "Reward Per Extra Player",
            new ConfigDescription("How much more reward each player beyond the first brings, as a share of one player's " +
                "(the players alive inside as it is won count). 0.5: two players get 1.5 times as much, three twice, four " +
                "2.5 times. 0: the same however many fight.", new AcceptableValueRange<float>(0f, 5f)),
            0.5f,
            true);

        internal static readonly ConfigInfo<float> CrimsonPause = new(
            Crimson.Enabled.Group,
            "Pause Between Raids Seconds",
            new ConfigDescription("How long after a raid ends the next one starts. The creatures of the last raid still inside die " +
                "as the next one comes (dropping nothing, not counted).", new AcceptableValueRange<float>(0f, 600f)),
            2f,
            true);

        internal static readonly ConfigInfo<float> CrimsonSpawnMultiplier = new(
            Crimson.Enabled.Group,
            "Raid Spawn Multiplier",
            new ConfigDescription("How many times as many creatures the Crimson Rite's raids bring as the game's own raids, and how " +
                "many times as often they spawn.", new AcceptableValueRange<float>(1f, 5f)),
            1f,
            true);

        internal static readonly ConfigInfo<float> CrimsonCatchUp = new(
            Crimson.Enabled.Group,
            "Raid Catch-Up",
            new ConfigDescription("How much raids below the players inside are brought up to them. A raid's tier is the strongest " +
                "boss it needs slain (Ragnarök Rite's Bosses setting); the players' is the lowest, among them, of the strongest raid " +
                "each is ready for. For each step between them, times this: about a third more creatures in bigger groups, more " +
                "often stars, up to two stars from three steps (from four, every creature has one). 0: raids as the game " +
                "makes them.", new AcceptableValueRange<float>(0f, 3f)),
            1f,
            true);

        internal static readonly ConfigInfo<string> CrimsonEnvironment = new(
            Crimson.Enabled.Group,
            "Environment",
            "The sky shown near the obliterator while raids come. " + CustomEnvironmentHelp,
            CustomEnvironmentNames.Bloodmoon,
            true)
        { IsAdvanced = true };

        internal static readonly ConfigInfo<string> BloodmoonBase = new(
            Crimson.Enabled.Group,
            "Blood Moon Base Environment",
            "The game environment the blood moon sky is made from, tinted red.",
            "Eikthyr",
            true)
        { IsAdvanced = true };

        internal static readonly RiteSettings Ragnarok = new(
            "7 - Reckoning - Ragnarok",
            "the Ragnarök Rite (boss after boss)",
            "TrophyEikthyr, TrophyTheElder, TrophyBonemass, TrophyDragonQueen, TrophyGoblinKing, TrophySeekerQueen, TrophyFader",
            "different boss trophies",
            "Bosses rise at the obliterator one after another, picked among those every player within the radius has " +
            "slain (each player's own boss kills; with player based raids off, the world's count for everyone), weaker ones " +
            "first. A boss gets a star for every two bosses between it and the strongest they have all slain, and each star " +
            "raises it to the boss two places up (after Bonemass, Eikthyr is 1 star and as strong as Bonemass): its health " +
            "(its resistances counted) and damage are raised to match, and so are those of what it summons. With none slain, " +
            "nothing rises and everyone there is told why. The sky burns, and a shield bubble covers the radius: " +
            "from when it starts, players inside can't get out and players outside can't get in, and no portal into or out of " +
            "it works; no creature gets in or out. Nothing can be built, repaired or torn down inside, and no rite but the " +
            "Sanctuary Rite performed there. Lightning strikes the obliterator as each boss falls. The bosses drop what they " +
            "usually do but their trophies. There is no time limit: slay them all and the obliterator, struck by lightning, " +
            "holds treasure: rolls of the treasure table (valuables) for each boss, the stronger the strongest boss slain the " +
            "more precious and plentiful. However it ends, the bosses and hostile creatures still inside vanish and drop nothing. " +
            "The obliterator can't be destroyed while it goes on and no creature goes after it; a beam of light rises from it. " +
            "It breaks off with no reward if everyone inside falls or leaves, or the " +
            "Sanctuary Rite ends it. Only one Crimson or Ragnarök Rite can go on at a time, and no Domain rite " +
            "works within its bubble: one at an obliterator inside is cancelled. Its status and " +
            "messages only show near the obliterator; its area is marked on everyone's map.",
            true,
            lasts: false,
            amount: 3,
            radius: 50f,
            distinctKeys: true);

        internal static readonly ConfigInfo<string> RagnarokBosses = new(
            Ragnarok.Enabled.Group,
            "Bosses",
            "Prefab names of the bosses the Ragnarök Rite can call, weakest first: their place in this list is how strong they " +
            "count for the treasure. A player has slain a boss when they have " +
            "the key it sets as it dies (Eikthyr: defeated_eikthyr...). Modded bosses work too when they set such a key.",
            "Eikthyr, gd_king, Bonemass, Dragon, GoblinKing, SeekerQueen, Fader",
            true);

        internal static readonly ConfigInfo<int> RagnarokBossCount = new(
            Ragnarok.Enabled.Group,
            "Boss Count",
            new ConfigDescription("How many bosses rise, one after another, a different one each while there are enough: with " +
                "fewer that everyone has slain, ones only some have slain fill in, and only then do some come again.",
                new AcceptableValueRange<int>(1, 10)),
            3,
            true);

        internal static readonly ConfigInfo<float> RagnarokPause = new(
            Ragnarok.Enabled.Group,
            "Pause Between Bosses Seconds",
            new ConfigDescription("How long after a boss falls the next one rises.", new AcceptableValueRange<float>(1f, 120f)),
            5f,
            true);

        internal static readonly ConfigInfo<float> RagnarokHealthMultiplier = new(
            Ragnarok.Enabled.Group,
            "Health Multiplier",
            new ConfigDescription("Multiplies every boss's health (and its summons') after it is raised by its stars " +
                "(1: as hard to kill as the boss it is raised to).", new AcceptableValueRange<float>(0.1f, 10f)),
            1f,
            true);

        internal static readonly ConfigInfo<float> RagnarokDamageMultiplier = new(
            Ragnarok.Enabled.Group,
            "Damage Multiplier",
            new ConfigDescription("Multiplies every boss's damage (and its summons') after it is raised by its stars " +
                "(1: hits as hard as the boss it is raised to).", new AcceptableValueRange<float>(0.1f, 10f)),
            1f,
            true);

        internal static readonly ConfigInfo<string> RagnarokTreasure = new(
            Ragnarok.Enabled.Group,
            "Treasure",
            "The treasure table, as Item: Min-Max pairs, from the least to the most precious. It is rolled Treasure Rolls Per " +
            "Boss times for each boss, whichever bosses came: the weaker the strongest boss everyone inside has slain, the " +
            "more likely the first; the stronger, the more likely the last, rolling toward Max.",
            "Amber: 6-10, AmberPearl: 4-6, Ruby: 2-5, SilverNecklace: 2-4",
            true);

        internal static readonly ConfigInfo<int> RagnarokTreasureRolls = new(
            Ragnarok.Enabled.Group,
            "Treasure Rolls Per Boss",
            new ConfigDescription("How many times the Treasure table is rolled for each boss.", new AcceptableValueRange<int>(1, 10)),
            4,
            true);

        internal static readonly ConfigInfo<float> RagnarokRewardPerPlayer = new(
            Ragnarok.Enabled.Group,
            "Reward Per Extra Player",
            new ConfigDescription("How much more treasure each player beyond the first brings, as a share of one player's " +
                "(the players alive inside as it is won count). 0.5: two players get 1.5 times as much, three twice, four " +
                "2.5 times. 0: the same however many fight.", new AcceptableValueRange<float>(0f, 5f)),
            0.5f,
            true);

        internal static readonly ConfigInfo<string> RagnarokEnvironment = new(
            Ragnarok.Enabled.Group,
            "Environment",
            "The sky shown near the obliterator while the bosses rise. " + CustomEnvironmentHelp,
            CustomEnvironmentNames.Ragnarok,
            true)
        { IsAdvanced = true };

        internal static readonly ConfigInfo<string> RagnarokSkyBase = new(
            Ragnarok.Enabled.Group,
            "Burning Sky Base Environment",
            "The game environment the burning sky is made from, tinted ember and gold.",
            "ThunderStorm",
            true)
        { IsAdvanced = true };

        internal static readonly RiteSettings HoardBane = new(
            "7 - Reckoning - Hoard-Bane",
            "the Hoard-Bane Rite (Hildir's minibosses)",
            "chest_hildir1, chest_hildir2, chest_hildir3",
            "one of Hildir's chests",
            "One of Hildir's minibosses comes for its chest: the brass chest calls Brenna with skeletons, the " +
            "silver chest Geirrhafa with ulvs, the bronze chest Zil and Thungr with fulings, each harder than the last. It rises on the " +
            "edge of the arena with four followers (every 30 seconds, while fewer than two stand, one more comes in furthest from the players), under a shield that takes no damage. Three elites, each with a name of its own " +
            "and a pack (Brenna: a 3 star troll with a log, a 1 star abomination, a 1 star wraith; Geirrhafa: a 1 star stone golem, " +
            "a 2 star fenring, a 1 star cultist; Zil and Thungr: a 3 star fuling berserker, a 1 star vile and lox), never fleeing, each " +
            "wait with their pack in a small bubble of their own on the edge: they don't move and take no damage until a player " +
            "steps into their bubble or hits one of them (from afar too, which only wakes them: no sneak attack), and then the " +
            "whole pack wakes. Lightning strikes the miniboss as each elite falls; with the last its shield breaks, and until " +
            "then the miniboss only walks, slowly. Nothing the rite brings attacks the " +
            "rest of it. The miniboss is stronger than usual, the stronger the strongest boss " +
            "everyone inside has slain (the Ragnarök Rite's Bosses setting), never as strong as that boss, and drops no chest " +
            "nor trophy. Slay it and the obliterator, struck by lightning, holds treasure: coins and valuables (each chest its own, the bronze " +
            "richest, then the silver, then the brass), and the battle and protection idols of the age everyone inside has reached. A shield bubble covers the radius as for the Ragnarök Rite: " +
            "nobody gets in or out, no portal works, nothing can be built inside, and only the Sanctuary Rite performed there. " +
            "There is no time limit. The obliterator can't be destroyed while it goes on and no creature goes after it; a beam " +
            "of light rises from it. It breaks off with no reward if everyone inside falls or " +
            "leaves, or the Sanctuary Rite ends it, but the chest isn't lost: it waits in the obliterator again. " +
            "However it ends, what it brought vanishes. Only one Crimson, " +
            "Ragnarök or Hoard-Bane Rite can go on at a time.",
            true,
            lasts: false,
            amount: 1,
            radius: 50f);

        internal static readonly ConfigInfo<float> HoardBaneEliteRadius = new(
            HoardBane.Enabled.Group,
            "Elite Bubble Radius",
            new ConfigDescription("How big each elite's bubble is, in meters: a player stepping into it wakes the elite. The " +
                "bubbles sit evenly around the edge of the arena.", new AcceptableValueRange<float>(3f, 30f)),
            10f,
            true);

        internal static readonly ConfigInfo<float> HoardBaneHealthShare = new(
            HoardBane.Enabled.Group,
            "Miniboss Health Share",
            new ConfigDescription("How hard to kill the miniboss is, as a share of the strongest boss everyone inside has slain " +
                "(its resistances counted; the brothers split it). Never less than 1.5 times its own health.",
                new AcceptableValueRange<float>(0.05f, 2f)),
            0.4f,
            true);

        internal static readonly ConfigInfo<float> HoardBaneDamageShare = new(
            HoardBane.Enabled.Group,
            "Miniboss Damage Share",
            new ConfigDescription("How hard the miniboss hits, as a share of the strongest boss everyone inside has slain (its " +
                "average damage per attack). Never less than 1.2 times its own.", new AcceptableValueRange<float>(0.05f, 2f)),
            0.6f,
            true);

        // Each chest's treasure: the brass chest (Brenna), the silver (Geirrhafa), the bronze (Zil and Thungr), by the fight's
        // place (see HoardBaneLoot.Encounters). The harder the fight, the richer.
        private static readonly string[] HoardBaneChestNames = ["Brass Chest", "Silver Chest", "Bronze Chest"];

        internal static readonly ConfigInfo<string>[] HoardBaneCoins =
        [
            HoardBaneCoinsFor(0, "Coins: 300-800"),
            HoardBaneCoinsFor(1, "Coins: 600-1500"),
            HoardBaneCoinsFor(2, "Coins: 1200-2500")
        ];

        internal static readonly ConfigInfo<string>[] HoardBaneTreasure =
        [
            HoardBaneTreasureFor(0, "Amber: 3-6, AmberPearl: 2-4, Ruby: 1-3, SilverNecklace: 1-2"),
            HoardBaneTreasureFor(1, "Amber: 4-8, AmberPearl: 3-6, Ruby: 2-5, SilverNecklace: 2-4"),
            HoardBaneTreasureFor(2, "Amber: 6-10, AmberPearl: 4-8, Ruby: 3-6, SilverNecklace: 3-5")
        ];

        internal static readonly ConfigInfo<int>[] HoardBaneTreasureRolls =
        [
            HoardBaneRollsFor(0, 3),
            HoardBaneRollsFor(1, 4),
            HoardBaneRollsFor(2, 6)
        ];

        private static ConfigInfo<string> HoardBaneCoinsFor(int chest, string value) => new(
            HoardBane.Enabled.Group,
            $"{HoardBaneChestNames[chest]} Coins",
            $"The coins in the {HoardBaneChestNames[chest].ToLowerInvariant()}'s treasure, as Item: Min-Max (any amount in " +
            "between, evenly). Empty: none.",
            value,
            true);

        private static ConfigInfo<string> HoardBaneTreasureFor(int chest, string value) => new(
            HoardBane.Enabled.Group,
            $"{HoardBaneChestNames[chest]} Treasure",
            $"The valuables in the {HoardBaneChestNames[chest].ToLowerInvariant()}'s treasure, as Item: Min-Max pairs, from " +
            "the least to the most precious, rolled as the Ragnarök Rite's: the stronger the strongest boss everyone inside " +
            "has slain, the more likely the last, rolling toward Max.",
            value,
            true);

        private static ConfigInfo<int> HoardBaneRollsFor(int chest, int value) => new(
            HoardBane.Enabled.Group,
            $"{HoardBaneChestNames[chest]} Treasure Rolls",
            new ConfigDescription($"How many times the {HoardBaneChestNames[chest]} Treasure table is rolled.",
                new AcceptableValueRange<int>(0, 20)),
            value,
            true);

        internal static readonly ConfigInfo<float> HoardBaneRewardPerPlayer = new(
            HoardBane.Enabled.Group,
            "Reward Per Extra Player",
            new ConfigDescription("How much more treasure (coins, valuables and idols) each player beyond the first brings, as a share of one player's " +
                "(the players alive inside as it is won count). 0.5: two players get 1.5 times as much, three twice, four " +
                "2.5 times. 0: the same however many fight.", new AcceptableValueRange<float>(0f, 5f)),
            0.5f,
            true);

        internal static readonly ConfigInfo<string> HoardBaneIdols = new(
            HoardBane.Enabled.Group,
            "Idols",
            "The idols in the treasure, as Boss: Item: Min-Max, by the strongest boss everyone inside has slain (prefab names " +
            "of the Ragnarök Rite's Bosses setting; None: no boss slain). List a boss more than once for several idols. A boss " +
            "not listed takes the entries of the nearest weaker one listed. The defaults are the battle (UpgraderNWeapon) and " +
            "protection (UpgraderNArmor) idols of the age the players are in: wooden with no boss slain, black metal once " +
            "everyone has slain Moder (however far some of them went), bloodgold after the Fader.",
            "None: Upgrader0Weapon: 1-2, None: Upgrader0Armor: 1-2, " +
            "Eikthyr: Upgrader1Weapon: 1-2, Eikthyr: Upgrader1Armor: 1-2, " +
            "gd_king: Upgrader2Weapon: 1-2, gd_king: Upgrader2Armor: 1-2, " +
            "Bonemass: Upgrader3Weapon: 1-2, Bonemass: Upgrader3Armor: 1-2, " +
            "Dragon: Upgrader4Weapon: 1-2, Dragon: Upgrader4Armor: 1-2, " +
            "GoblinKing: Upgrader5Weapon: 1-2, GoblinKing: Upgrader5Armor: 1-2, " +
            "SeekerQueen: Upgrader6Weapon: 1-2, SeekerQueen: Upgrader6Armor: 1-2, " +
            "Fader: Upgrader7Weapon: 1-2, Fader: Upgrader7Armor: 1-2",
            true);

        internal static readonly ConfigInfo<string> HoardBaneEnvironment = new(
            HoardBane.Enabled.Group,
            "Environment",
            "The sky shown near the obliterator while the fight goes on. " + CustomEnvironmentHelp,
            CustomEnvironmentNames.HoardBane,
            true)
        { IsAdvanced = true };

        internal static readonly ConfigInfo<string> HoardBaneSkyBase = new(
            HoardBane.Enabled.Group,
            "Cursed Sky Base Environment",
            "The game environment the cursed sky is made from, dimmed and tinted violet.",
            "Misty",
            true)
        { IsAdvanced = true };

        internal static readonly RiteSettings Sanctuary = new(
            "7 - Reckoning - Sanctuary",
            "the Sanctuary Rite (ends the raid)",
            "IronScrap",
            "scrap iron",
            "Ends the raid going on in the world at once, and any Crimson, Ragnarök or Hoard-Bane Rite (without its reward). The only rite " +
            "that can be performed within their bubble.",
            true,
            lasts: false,
            amount: 3);

        internal static readonly ConfigInfo<Toggle> SanctuaryOnlyInside = new(
            Sanctuary.Enabled.Group,
            "Ends Fights Only From Inside",
            "On: a Crimson, Ragnarök or Hoard-Bane Rite can only be ended by the Sanctuary Rite performed at its own " +
            "obliterator, inside the bubble. Performed anywhere else while one goes on, it is refused and keeps its key " +
            "items, so nobody outside can end someone else's fight. Off: performed anywhere, it ends the fight.",
            Toggle.Off);

        private const string CustomEnvironmentHelp = "Its sky is made by the mod from the game's own (see Environments).";

        internal static class Display
        {
            // Per player, never synced: what you see and press (8), and how the rites look in the world (9).
            internal const string Group = "8 - Interface";
            internal const string EffectsGroup = "9 - Effects";

            internal static readonly ConfigInfo<Toggle> ShowStatus = new(
                Group,
                "Show Weather Status",
                "Show a status with the time left for each rite going on and each blessing you carry. Rite statuses stay through death; blessings end with it.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowLeverHint = new(
                Group,
                "Show Lever Hint",
                "Looking at the obliterator's lever shows which rite pulling it performs.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowHatchHint = new(
                Group,
                "Show Hatch Hint",
                "Looking at the obliterator's hatch lists the items that perform rites you know. Off by default: the rites page " +
                "(the flap under the open obliterator) lists them too.",
                Toggle.Off,
                false);

            internal static readonly ConfigInfo<Toggle> ShowItemHighlight = new(
                Group,
                "Show Obliterator Highlights",
                "With the obliterator open, the rite item that decides gets a frame in its rite's color and any others a dim gray one, " +
                "and their tooltips say which rite they perform or that they are sacrificed for nothing.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowExplanations = new(
                Group,
                "Show Explanations",
                "Show the gray explanations next to rite names (what each rite does, why an item decides) in hover texts, item tooltips " +
                "and the rites page. Off: only the item and the rite's name, for less clutter once you know them.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowMessages = new(
                Group,
                "Show Messages",
                "Show a message when someone performs a rite, and when it ends.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowRiteUnlocks = new(
                Group,
                "Show New Rites",
                "Show a \"New rite\" popup, like the game's \"New recipe\" one, when you learn a rite. The first time you build, open " +
                "or pull an obliterator, every rite you already know shows; after that, each rite whose item you find for the first time.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> GraveReequip = new(
                Group,
                "Grave-Bind Re-Equip Gear",
                "After a death under the Grave-Bind Rite, put the gear you wore back on once you loot your tombstone.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<KeyboardShortcut> MorgenPouchShortcut = new(
                Group,
                "Morgen-Pouch Shortcut",
                "Your own key for the Morgen-Pouch ability while the Morgen-Pouch Rite lasts: pocket the creatures you petted, or let " +
                "them out (also in the game's Settings > Controls). Works while moving.",
                new KeyboardShortcut(KeyCode.P, KeyCode.LeftAlt),
                false);

            internal static readonly ConfigInfo<KeyboardShortcut> RallyCallShortcut = new(
                Group,
                "Rally Call Shortcut",
                "Your own key for answering the Rally-Horn Rite's call while it lasts (also in the game's Settings > Controls). " +
                "Works while moving.",
                new KeyboardShortcut(KeyCode.Y),
                false);

            internal static readonly ConfigInfo<KeyboardShortcut> RallyRefuseShortcut = new(
                Group,
                "Rally Refuse Shortcut",
                "Your own key for refusing the Rally-Horn Rite's call while it lasts, letting it go at once (also in the game's " +
                "Settings > Controls). Works while moving.",
                new KeyboardShortcut(KeyCode.N),
                false);

            internal static readonly ConfigInfo<KeyboardShortcut> DomainBorderShortcut = new(
                Group,
                "Domain Bounds Shortcut",
                "While you are in a Domain rite's area, show or hide its edge (also in the game's Settings > Controls). Your " +
                "choice is kept for the next area. Works while moving. Leave empty to hide the Bounds slot.",
                new KeyboardShortcut(KeyCode.B, KeyCode.LeftAlt),
                false);

            internal static readonly ConfigInfo<Toggle> ShowBlessingLightning = new(
                EffectsGroup,
                "Show Blessing Lightning",
                "Show the lightning striking each player who receives a blessing.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowCreatureIcons = new(
                EffectsGroup,
                "Show Creature Icons",
                "Where the game shows its yellow and red alert icons over a creature: a blue drop while it shuns a Packward Rite, " +
                "a green heart while a Carrion Rite draws it.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowDomainEffects = new(
                EffectsGroup,
                "Show Domain Effects",
                "While a Domain rite goes on, rings in its color spread out from the obliterator over its area, one at a time, " +
                "and the obliterator glows and gives off rising motes in the colors of its rites, each rite in its own shape " +
                "(Verdant leaves, Vat-Boil bubbles, Pitch-Bellows embers and forge sparks, Kindred hearts, Harrow-Spur little ghosts, Sovereign Ward stars, Hearth-Haul feathers, " +
                "Sentry-Sight bolts, Taproot-Surge sap drops, Pitch-Seal blobs of pitch, Primal-Hearth violet flames, Hearth-Kindle crescent moons).",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> AlwaysShowDomainBorder = new(
                EffectsGroup,
                "Always Show Domain Border",
                "Show the edge of every Domain rite's area near you all the time. Off: only the edge of the area you are in " +
                "shows (and for 5 seconds after you leave it), and a Bounds slot beside the Guardian Power switches it on or off.",
                Toggle.Off,
                false);

            internal static readonly ConfigInfo<Toggle> ShowVerdantSparkles = new(
                EffectsGroup,
                "Show Verdant Sparkles",
                "Under the Verdant Rite, now and then a few of the plants and bushes growing faster give off green leaves.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowVatBoilMotes = new(
                EffectsGroup,
                "Show Vat-Boil Motes",
                "Under the Vat-Boil Rite, now and then a few of the fermenters and cooking stations working faster give off golden bubbles.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowPitchBellowsMotes = new(
                EffectsGroup,
                "Show Pitch-Bellows Motes",
                "Under the Pitch-Bellows Rite, now and then a few of the smelters and kilns working faster give off embers.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<UnityEngine.Color> PitchBellowsFireTint = new(
                EffectsGroup,
                "Pitch-Bellows Fire Tint",
                "Under the Pitch-Bellows Rite, the fire of smelters, kilns and furnaces near you leans toward this color (in your game only). " +
                "Its alpha is how far: 0 leaves the fire as usual, 1 turns it fully that color.",
                new UnityEngine.Color(0.8f, 0.12f, 0.06f, 0.4f),
                false);

            internal static readonly ConfigInfo<Toggle> ShowKindredHearts = new(
                EffectsGroup,
                "Show Kindred Hearts",
                "Under the Kindred Rite, now and then a few of the tamed young and pregnant animals and warm eggs it reaches give off pink hearts.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowHarrowSpurMotes = new(
                EffectsGroup,
                "Show Harrow-Spur Motes",
                "Under the Harrow-Spur Rite, now and then a few of the spawners it reaches give off little ghosts, bigger for bigger spawners.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowHearthHaulFeathers = new(
                EffectsGroup,
                "Show Hearth-Haul Feathers",
                "Under the Hearth-Haul Rite, players carrying too much give off drifting feathers, showing why they can still walk " +
                "without tiring (seen by everyone with the mod).",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowSentrySightMotes = new(
                EffectsGroup,
                "Show Sentry-Sight Motes",
                "Under the Sentry-Sight Rite, now and then a few of the ballistas it reaches give off steel-blue bolts.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowTaprootSurgeMotes = new(
                EffectsGroup,
                "Show Taproot-Surge Motes",
                "Under the Taproot-Surge Rite, now and then a few of the sap extractors, eitr refineries and ancient roots it reaches give off glowing sap drops.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<UnityEngine.Color> TaprootSurgeFireTint = new(
                EffectsGroup,
                "Taproot-Surge Fire Tint",
                "Under the Taproot-Surge Rite, the fire and glow of eitr refineries near you turn this color, and their machinery moves as " +
                "fast as they work (in your game only). Its alpha is how far: 0 leaves the fire as usual, 1 turns it fully that color.",
                new UnityEngine.Color(0.1f, 0.45f, 1f, 1f),
                false);

            internal static readonly ConfigInfo<Toggle> ShowMantleAnchorMotes = new(
                EffectsGroup,
                "Show Pitch-Seal Motes",
                "Under the Pitch-Seal Rite, the items it holds on the ground give off little blobs of pitch with a coin stuck in them instead of their usual yellow sparks.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowPrimalHearthMotes = new(
                EffectsGroup,
                "Show Primal-Hearth Motes",
                "Under the Primal-Hearth Rite, now and then a few of the wisp fountains and eternal pyres it reaches give off violet flames.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<float> PrimalHearthGlow = new(
                EffectsGroup,
                "Primal-Hearth Glow",
                new ConfigDescription(
                    "How many times as bright the wisp fountains and eternal pyres the Primal-Hearth Rite reaches glow near you (their light, glowing parts and flames). 1 is as usual.",
                    new AcceptableValueRange<float>(1f, 4f)),
                2f,
                false);

            internal static readonly ConfigInfo<Toggle> ShowHearthKindleMotes = new(
                EffectsGroup,
                "Show Hearth-Kindle Motes",
                "Under the Hearth-Kindle Rite, now and then a few of the burning fires it reaches give off little rest signs, " +
                "and while you rest by a fire there its embers arc into you, with a puff of Rested symbols when you become Rested.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowPlumeWardDown = new(
                EffectsGroup,
                "Show Plume-Ward Down",
                "Under the Plume-Ward Rite, each landing it softens gives off a puff of pale down (seen by everyone with the mod).",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowMorgenPouchGlow = new(
                EffectsGroup,
                "Show Morgen-Pouch Marks",
                "Under the Morgen-Pouch Rite, the creatures your pouch would take wear a violet glow, like a Guardian Power's (seen only by you).",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> GraveBeacon = new(
                EffectsGroup,
                "Show Grave-Bind Beam",
                "Show a beam into the sky over your tombstone after a death under the Grave-Bind Rite.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<float> LuminaryStrength = new(
                EffectsGroup,
                "Luminary Light Strength",
                new ConfigDescription("How bright the light around you is: 0.5 is half as bright, 2 twice as bright. The range stays the same.", new AcceptableValueRange<float>(0.1f, 2f)),
                0.6f,
                false);

            internal static readonly ConfigInfo<float> LuminaryBodyLight = new(
                EffectsGroup,
                "Luminary Body Light",
                new ConfigDescription("How bright the soft light that keeps the shining player's body lit is: 1 as usual, lower it if the body looks too bright. 0: no body light.", new AcceptableValueRange<float>(0f, 2f)),
                1f,
                false);

            internal static readonly ConfigInfo<Toggle> LuminaryShadows = new(
                EffectsGroup,
                "Luminary Shadows",
                "On: floors, ceilings and walls block the light, so it doesn't shine through to the floor above or below or the next " +
                "room. Off: a little lighter on the graphics card, but the light goes through everything within its range. Also " +
                "needs shadows on in the game's graphics settings.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> CrimsonBubble = new(
                EffectsGroup,
                "Show Crimson Bubble",
                "Show the shield bubble over the Crimson Rite's radius. Players are held inside (or kept out) either way.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> RagnarokBubble = new(
                EffectsGroup,
                "Show Ragnarok Bubble",
                "Show the shield bubble over the Ragnarök Rite's radius. Players are held inside (or kept out) either way.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> HoardBaneBubble = new(
                EffectsGroup,
                "Show Hoard-Bane Bubble",
                "Show the shield bubble over the Hoard-Bane Rite's radius. Players are held inside (or kept out) either way; the " +
                "elites' bubbles and the miniboss's shield always show.",
                Toggle.On,
                false);
        }
    }
}

// Environments the mod makes from the game's own, for rites with a sky the game doesn't have.
internal static class CustomEnvironmentNames
{
    internal const string Aurora = "TempestRite_Aurora";
    internal const string Bloodmoon = "TempestRite_Bloodmoon";
    internal const string Ragnarok = "TempestRite_Ragnarok";
    internal const string HoardBane = "TempestRite_HoardBane";
}

// The settings of one rite, in its own section.
internal sealed class RiteSettings
{
    internal ConfigInfo<Toggle> Enabled { get; }
    // Not for rites that happen at once (Equilibrium, Sanctuary).
    internal ConfigInfo<float>? Minutes { get; }
    internal ConfigInfo<string> KeyItem { get; }
    internal ConfigInfo<int> KeyAmount { get; }
    // Only for weather: the game environment shown in each biome.
    internal ConfigInfo<string>? Environments { get; }
    // Only for rites that work around the obliterator.
    internal ConfigInfo<float>? Radius { get; }
    // Whether only different key items count toward the Key Item Amount (two of the same count once).
    internal bool DistinctKeys { get; }

    // minutesDescription: what the Duration Minutes setting means, when it isn't how long the rite goes on.
    internal RiteSettings(string group, string effect, string keyItem, string keyItemName, string why, bool enabled, bool lasts = true,
        string? environments = null, string namePrefix = "", int amount = 1, float minutes = 30f, float radius = 0f,
        string? minutesDescription = null, bool distinctKeys = false)
    {
        DistinctKeys = distinctKeys;
        Enabled = new(
            group,
            namePrefix + "Enabled",
            $"Sacrificing {keyItemName} in the obliterator performs {effect}. {why}",
            enabled ? Toggle.On : Toggle.Off,
            true);

        if (lasts) Minutes = new(
            group,
            "Duration Minutes",
            new ConfigDescription(
                minutesDescription ?? $"How long {effect} lasts, in minutes of play. Sleeping doesn't shorten it; pausing a " +
                "single player game pauses it. Performing it again starts the time over.",
                new AcceptableValueRange<float>(0.5f, 600f)),
            minutes,
            true);

        KeyItem = new(
            group,
            namePrefix + "Key Item",
            $"Prefab names of the items that perform {effect} when sacrificed, separated by commas (default {keyItem}, {keyItemName}). " +
            (distinctKeys ? "Add others to let any of them work: each different one counts once toward the Key Item Amount. "
                : "Add others to let any of them work: any mix of them counts toward the Key Item Amount. ") + "An item performs one rite " +
            "only: when two rites list it, the one listed first on the rites page keeps it.",
            keyItem,
            true);

        KeyAmount = new(
            group,
            namePrefix + "Key Item Amount",
            new ConfigDescription(
                (distinctKeys ? $"How many different key items have to be in the obliterator to perform {effect} (two of the same count once). "
                    : $"How many key items (any mix of them) have to be in the obliterator to perform {effect}. ") + "Fewer are sacrificed for nothing.",
                new AcceptableValueRange<int>(1, 100)),
            amount,
            true);

        if (radius > 0f) Radius = new(
            group,
            "Radius",
            new ConfigDescription($"How far from the obliterator {effect} works, in meters.", new AcceptableValueRange<float>(5f, 500f)),
            radius,
            true);

        if (environments == null) return;
        Environments = new(
            group,
            "Environments",
            $"The game environment shown during {effect} in each biome, as Biome: Environment pairs. " +
            "Biomes: Meadows, BlackForest, Swamp, Mountain, Plains, Mistlands, AshLands, DeepNorth, Ocean; Default is used for any " +
            "biome not listed. Any name the env console command takes works.",
            environments,
            true)
        { IsAdvanced = true };
    }
}
