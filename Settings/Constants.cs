using BepInEx.Configuration;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;

namespace JoksterCube.TempestRite.Settings;

internal static class Constants
{
    internal static class Plugin
    {
        internal const string ModName = "TempestRite";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "JoksterCube";
        internal const string ModGUID = $"{Author}.{ModName}";
        internal const string Description = "Perform rites at the obliterator: burn an item to change the weather, hold the sun, call raids, grow crops faster, receive blessings and more.";
        internal const string Copyright = "Copyright ©  2026";
        internal const string Guid = "1e7b1458-bfca-49ce-aca8-9eef270f87e6";

        internal const string ConfigFileName = $"{ModGUID}.cfg";
        internal const string VersionCheckRpc = $"{ModName}_VersionCheck";
        // This mod's place among ChestKit mods' flaps under the container panel (lower further left).
        internal const int FlapPriority = 10;
    }

    internal static class Rpcs
    {
        // Client -> server: please perform this rite.
        internal const string Request = $"{Plugin.ModName}_Request";
        // Client -> server: send me the rites going on.
        internal const string StateRequest = $"{Plugin.ModName}_StateRequest";
        // Server -> client: the rites going on.
        internal const string State = $"{Plugin.ModName}_State";
        // Server -> client: a request was refused.
        internal const string Refused = $"{Plugin.ModName}_Refused";
        // Server -> clients: a blessing was performed at an obliterator; players near it receive it.
        internal const string Blessing = $"{Plugin.ModName}_Blessing";
    }

    internal static class Messages
    {
        internal const string ClearName = "Clear Sky Rite";
        internal const string ThunderstormName = "Tempest Rite";
        internal const string RainName = "Downpour Rite";
        internal const string MistName = "Shrouding Rite";
        internal const string AuroraName = "Aurora Rite";
        internal const string PolarDayName = "Sun Zenith Rite";
        internal const string PolarNightName = "Midnight Rite";
        internal const string TailwindName = "Tailwind Rite";
        internal const string NoMistName = "True Sight Rite";
        internal const string SootfallName = "Sootfall Rite";
        internal const string CryptDelverName = "Crypt Delver Rite";
        internal const string PackwardName = "Packward Rite";
        internal const string CarrionName = "Carrion Rite";
        internal const string VerdantName = "Verdant Rite";
        internal const string CrimsonName = "Crimson Rite";
        internal const string SanctuaryName = "Sanctuary Rite";
        internal const string ResetName = "Equilibrium Rite";
        internal const string GroundingName = "Grounding Rite";
        internal const string VatBoilName = "Vat-Boil Rite";
        internal const string AmberMendName = "Amber-Mend Rite";
        internal const string AbyssalSurgeName = "Abyssal Surge Rite";
        internal const string GlacialSatietyName = "Glacial Satiety Rite";
        internal const string VeinShatterName = "Vein-Shatter Rite";
        internal const string CarapaceTemperName = "Carapace Temper Rite";
        internal const string GraveBindName = "Grave-Bind Rite";
        internal const string FortunesBoonName = "Fortune's Boon Rite";
        internal const string LuminaryName = "Luminary Rite";
        internal const string SovereignWardName = "Sovereign Ward Rite";
        internal const string AbyssalDrawName = "Abyssal Draw Rite";
        internal const string UnmakingName = "Unmaking Rite";

        internal const string GroupAtmospheric = "Atmospheric";
        internal const string GroupCelestial = "Celestial";
        internal const string GroupBiomeMastery = "Biome Mastery";
        internal const string GroupDomain = "Domain";
        internal const string GroupIncursions = "Incursions";
        internal const string GroupBlessings = "Blessings";

        // {0}: the rite's name, colored.
        internal const string StartedByYou = "You performed the {0}";
        internal const string StartedBy = "{0} performed the {1}";
        internal const string EndedWeather = "The weather returns to its own ways";
        internal const string EndedDaylight = "The sun returns to its own ways";
        internal const string EndedMistlands = "The mist returns to the Mistlands";
        internal const string EndedAshlands = "The smoke returns to the Ashlands";
        internal const string EndedWind = "The wind returns to its own ways";
        internal const string EndedVatBoil = "The fermenters slow down again";
        internal const string EndedAmberMend = "The mending stops";
        internal const string EndedAbyssalSurge = "The sea calms down";
        internal const string EndedSovereignWard = "The ward over your buildings fades";
        // {0}: the blessing's name, colored.
        internal const string BlessingEnded = "The {0} fades";
        internal const string BlessingsLostToDeath = "Your blessings fade with your death";
        internal const string BlessedByYou = "You performed the {0}: the lightning blesses you";
        internal const string BlessedBy = "{0} performed the {1}: the lightning blesses you";
        // {1}: the blessing range in meters.
        internal const string BlessingMissed = "You performed the {0}, but no one stood within {1} m of the obliterator";
        internal const string GraveBindKept = "The Grave-Bind Rite keeps your skills";
        internal const string EndedIncursion = "The raids stop coming";
        internal const string EndedVerdant = "The growth slows again";
        internal const string EndedPackward = "The creatures are no longer afraid";
        internal const string EndedCarrion = "The animals go back to the wild";
        internal const string RefusedAdminsOnly = "Only admins can perform rites";
        internal const string RefusedDisabled = "This rite is turned off on this server";
        // The connection error shown on joining. {0}: mod name and version.
        internal const string ServerLacksMod = "The server doesn't have {0}. Remove it to join, or ask the server to install it.";

        // First line: what it does, capitalized.
        internal const string StatusTooltip = "{0}. Performed by {1}.\nEveryone with the mod has this weather until it ends, except where the world forces its own (dungeons, raids, {2}).";
        internal const string StatusTooltipNoBiomes = "{0}. Performed by {1}.\nEveryone with the mod has this weather until it ends, except where the world forces its own (dungeons, raids).";
        internal const string StatusTooltipPolarDay = "Day all the time. Performed by {0}.\nThe sun stays up and creatures spawn as by day, for everyone with the mod until it ends. Time goes on as usual.";
        internal const string StatusTooltipPolarNight = "Night all the time. Performed by {0}.\nThe sun stays down and night creatures spawn, for everyone with the mod until it ends. Time goes on as usual.";
        internal const string StatusTooltipTailwind = "Wind behind your ship. Performed by {0}.\nWhile you sail, the wind blows from behind, like Moder's power, for everyone with the mod until it ends.";
        internal const string StatusTooltipSootfall = "No Ashlands smoke or heat haze. Performed by {0}.\nThe Ashlands ground smoke and the heat shimmer near lava are gone for everyone with the mod until it ends. The heat still burns.";
        internal const string StatusTooltipNoMist = "No Mistlands mist. Performed by {0}.\nThe Mistlands mist is gone for everyone with the mod until it ends.";
        internal const string StatusTooltipCryptDelver = "Never wet. Performed by {0}.\nRain and crypt water don't touch you until it ends or you die.";
        // {1}: the food floor in percent.
        internal const string StatusTooltipGlacialSatiety = "Food fades slower. Performed by {0}.\nEaten food never gives less than {1}% of its health, stamina and eitr, until it ends or you die.";
        // {1}: how much of a hit spreads, {2}: at how much health left the vein shatters, both in percent.
        internal const string StatusTooltipVeinShatter = "Ore veins shatter. Performed by {0}.\nHitting copper, silver or soft tissue deals {1}% of the hit to the pieces next to it, and once the vein is down to {2}% the rest breaks at once. Your pickaxe wears as for every piece. Until it ends or you die.";
        // {1}: how much less wear, in percent.
        internal const string StatusTooltipCarapaceTemper = "Gear wears slower. Performed by {0}.\nYour equipped weapons, tools and armor lose {1}% less durability until it ends or you die.";
        internal const string StatusTooltipGraveBind = "No skill loss on death. Performed by {0}.\nIf you die, you keep all your skills, and your tombstone shines a beam into the sky. Ends with that death.";
        // {1}: the trophy chance multiplier.
        internal const string StatusTooltipFortunesBoon = "More loot. Performed by {0}.\nCreatures you kill drop the most they can and trophies {1}x as often; trees you fell always give fine wood and seeds. Until it ends or you die.";
        // {1}: radius in meters, {2}: how much faster in percent.
        internal const string StatusTooltipVatBoil = "Fermenters work faster. Performed by {0}.\nFermenters within {1} m of the obliterator need {2}% less time until it ends.";
        internal const string StatusTooltipSovereignWard = "Buildings can't be harmed. Performed by {0}.\nBuilt pieces within {1} m of the obliterator take no damage and hostile projectiles crumble at its edge until it ends.";
        internal const string StatusTooltipSovereignSanctuary = "Buildings can't be harmed. Performed by {0}.\nBuilt pieces within {1} m of the obliterator take no damage, hostile projectiles crumble at its edge, and no player there can be hurt, until it ends.";
        // {1}: the light's range in meters.
        internal const string StatusTooltipLuminary = "You shine. Performed by {0}.\nA warm light around you lights {1} m in every direction, for everyone to see, until it ends or you die.";
        // {1}: the pickup range in meters.
        internal const string StatusTooltipAbyssalDraw = "Items fly to you. Performed by {0}.\nItems within {1} m fly to you, unless a closer player with room can take them, until it ends or you die.";
        internal const string StatusTooltipAmberMend = "Buildings mend. Performed by {0}.\nDamaged building pieces within {1} m of the obliterator are repaired, one after another, until it ends.";
        // {1}: serpent multiplier, {2}: fish multiplier.
        internal const string StatusTooltipAbyssalSurge = "More serpents and fish. Performed by {0}.\nSerpents come {1}x as often at night and in storms, fish swim in schools {2}x as big and more of them are big ones, for everyone with the mod until it ends.";
        // {1}: the radius in meters.
        internal const string StatusTooltipPackward = "Hostile creatures flee. Performed by {0}.\nHostile creatures (not bosses) that come within {1} m of the obliterator run away until it ends.";
        internal const string StatusTooltipCarrion = "Wild animals come. Performed by {0}.\nWild animals within {1} m are drawn to the obliterator until it ends.";
        // {2}: how much faster, in percent.
        internal const string StatusTooltipVerdant = "Plants grow faster. Performed by {0}.\nPlants and bushes within {1} m of the obliterator grow {2}% faster until it ends.";
        internal const string StatusTooltipCrimson = "Raid after raid. Performed by {0}.\nRaids keep coming to the obliterator under a blood moon, and nowhere else, until it ends.";
        internal const string SomeoneName = "someone";

        // Item names in orange, as the game shows them in hover text.
        internal const string ItemColor = "orange";
        internal const string Faint = "#c9c9c9";

        // {0}: rite name, colored. {1}: the gray explanation (Explanation below), or nothing when Show Explanations is off.
        internal const string LeverBrings = "{0}{1}";
        internal const string Explanation = " <color=" + Faint + ">({0})</color>";
        internal const string LeverNotAllowed = "<color=#ff7a5c>Only admins can perform rites</color>";

        internal const string HatchTitle = "<color=#9fd8ff>Burn to perform a rite:</color>";
        internal const string HatchLine = "  <color=" + ItemColor + ">{0}</color>: {1}{2}";

        internal const string TooltipDecides = "Performs the {0}{1} when burnt.";
        internal const string TooltipDecidesOfSeveral = "Performs the {0}{1} when burnt: it is the lowest rite item here, so it decides.";
        internal const string TooltipOverruled = "<color=#9a9a9a>Just burns: a lower rite item decides.</color>";
        internal const string TooltipOverruledShort = "<color=#9a9a9a>Just burns.</color>";
        // {0}: how many are needed, {1}: the rite, {2}: how many are in.
        internal const string TooltipNeedsMore = "<color=#9a9a9a>Needs {0} to perform the {1} ({2} in), until then just burns.</color>";
        internal const string TooltipNeedsMoreShort = "<color=#9a9a9a>Needs {0}.</color>";
        // An item with how many are needed: "2x Ooze", the number in white inside the item's orange.
        internal const string ItemAmount = "<color=#f2f2f2>{0}x</color> {1}";

        // The rites page, opened from the flap under the obliterator.
        internal const string FlapTooltip = "Rites";
        internal const string PanelHint = "Burn a rite item to perform its rite. The lowest one decides. Click an item to put it in.";
        // Tooltip of an item on the rites page. {0}: how many it takes, {1}: how many you have.
        internal const string PanelItemMissing = "Missing items";
        internal const string PanelItemNeeds = "Needs {0}, you have {1}";
        internal const string PanelItemPut = "Click to put {0} in";
        internal const string PanelItemIn = "In the obliterator";
        // After the name of the rite pulling the lever performs.
        internal const string PanelReady = "<color=#8fe08f>Ready: pull the lever</color>";
        internal const string PanelRowDetail = "<color=" + ItemColor + ">{0}</color>  <color=" + Faint + ">{1}</color>";
        internal const string PanelRowItem = "<color=" + ItemColor + ">{0}</color>";
        internal const string PanelMoreToFind = "{0} more rites wait to be found here: hold their items to learn them.";
        internal const string PanelOneMoreToFind = "1 more rite waits to be found here: hold its item to learn it.";
        internal const string CloseTooltip = "Close";
    }

    internal static class DebugMessages
    {
        internal const string ReadConfigCalled = "ReadConfigValues called";
        internal const string ErrorLoadingConfig = $"There was an issue loading your {Plugin.ConfigFileName}";
        internal const string RequestCheckConfig = "Please check your config entries for spelling and format!";
    }

    // Sections are numbered as the rites page groups them: atmospheric, celestial, biome mastery, domain, blessings,
    // incursions.
    internal static class Groups
    {
        internal static class General
        {
            internal const string Group = "01 - General";

            internal static readonly ConfigInfo<WeatherPermission> WhoCanChange = new(
                Group,
                "Who Can Change Weather",
                "Who may perform rites with the obliterator. Anyone: every player with the mod. " +
                "AdminsOnly: only server admins (and the host). Others' key items just burn as usual.",
                WeatherPermission.Anyone,
                true);

            internal static readonly ConfigInfo<WeatherBiomes> AffectedBiomes = new(
                Group,
                "Affected Biomes",
                "Biomes where weather rites (Clear Sky, Tempest, Downpour, Shrouding, Aurora) show. Elsewhere the biome keeps its own " +
                "weather while the rite goes on. Each weather rite shows as that biome's own kind of it (see its Environments setting). " +
                "The other rites don't depend on this. Deep North is left out by default until it is part of the game.",
                WeatherBiomes.Meadows | WeatherBiomes.BlackForest | WeatherBiomes.Swamp | WeatherBiomes.Mountain |
                WeatherBiomes.Plains | WeatherBiomes.Mistlands | WeatherBiomes.AshLands | WeatherBiomes.Ocean,
                true);

            internal static readonly ConfigInfo<float> DomainRadius = new(
                Group,
                "Domain Rites Radius",
                new ConfigDescription(
                    "How far from their obliterator the Domain rites (Packward, Carrion, Verdant, Vat-Boil, Amber-Mend) work, in " +
                    "meters. Their status and message only show this near it.",
                    new AcceptableValueRange<float>(5f, 500f)),
                100f,
                true);

            internal static readonly ConfigInfo<float> BlessingRange = new(
                Group,
                "Blessings Range",
                new ConfigDescription(
                    "How close to the obliterator players must stand when it burns to receive a blessing (Crypt Delver, Glacial " +
                    "Satiety, Vein-Shatter, Carapace Temper, Grave-Bind, Fortune's Boon), in meters. 10 is the range Forsaken powers " +
                    "are shared in.",
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
        }

        // ---- Atmospheric ----

        internal static readonly WeatherGroup Clear = new(
            "02 - Clear Sky Rite",
            "the Clear Sky Rite (clear skies)",
            "SurtlingCore",
            "a surtling core",
            "No rain, snow or fog: for building, farming and travelling in peace. Cold biomes stay as cold as their own weather makes them.",
            true,
            environments: "Default: Clear, Mistlands: Mistlands_clear, AshLands: Ashlands_ashrain_clear");

        internal static readonly WeatherGroup Thunderstorm = new(
            "03 - Tempest Rite",
            "the Tempest Rite (a thunderstorm)",
            "CopperOre",
            "copper ore",
            "Strong wind for sailing, and the weather serpents come out in. A snowstorm in the mountains.",
            true,
            environments: "Default: ThunderStorm, Mountain: SnowStorm, Mistlands: Mistlands_thunder, AshLands: Ashlands_storm");

        internal static readonly WeatherGroup Rain = new(
            "04 - Downpour Rite",
            "the Downpour Rite (rain)",
            "Ooze",
            "ooze",
            "For the mood. Light rain in the meadows, snow in the mountains. Puts out spreading fire with no roof over it and keeps " +
            "cinders from lighting more (the Tempest Rite does too).",
            true,
            amount: 2,
            environments: "Default: Rain, Meadows: LightRain, Swamp: SwampRain, Mountain: Snow, Mistlands: Mistlands_rain, AshLands: Ashlands_ashrain");

        internal static readonly WeatherGroup Mist = new(
            "05 - Shrouding Rite",
            "the Shrouding Rite (mist)",
            "GreydwarfEye",
            "greydwarf eyes",
            "Fog that hides what is far away.",
            true,
            amount: 3,
            environments: "Default: Misty, BlackForest: DeepForest Mist, Mistlands: Mistlands_clear, AshLands: Ashlands_misty");

        internal static readonly WeatherGroup Aurora = new(
            "06 - Aurora Rite",
            "the Aurora Rite (auroras and calm winds)",
            "SilverNecklace",
            "a silver necklace",
            "A clear sky with shimmering auroras at night, like the Deep North's, and calm, quiet winds. " + CustomEnvironmentHelp,
            true,
            environments: "Default: " + CustomEnvironmentNames.Aurora);

        // ---- Celestial ----

        internal static readonly WeatherGroup PolarDay = new(
            "07 - Sun Zenith Rite",
            "the Sun Zenith Rite (polar day)",
            "Ruby",
            "a ruby",
            "It is day all the time: the sun stays up, creatures spawn as by day and night creatures leave. Time goes on: days are " +
            "counted, clocks show the real time, and beds go by the real clock.",
            true);

        internal static readonly WeatherGroup PolarNight = new(
            "08 - Midnight Rite",
            "the Midnight Rite (polar night)",
            "SilverOre",
            "silver ore",
            "It is night all the time: the sun stays down, night creatures spawn (more and stronger ones) and nights are cold. " +
            "Time goes on: days are counted, clocks show the real time, and beds go by the real clock.",
            true);

        internal static readonly WeatherGroup Tailwind = new(
            "09 - Tailwind Rite",
            "the Tailwind Rite (wind behind your ship)",
            "DragonEgg",
            "a dragon egg",
            "While sailing, the wind always blows from behind, like Moder's power. Only its direction: how strong it is stays the weather's.",
            true);

        // ---- Biome mastery ----

        internal static readonly WeatherGroup NoMist = new(
            "10 - True Sight Rite",
            "the True Sight Rite (no Mistlands mist)",
            "Eitr",
            "refined eitr",
            "The Mistlands mist is gone, for you and for what creatures can see. Off by default: it takes away much of the Mistlands' challenge.",
            false,
            amount: 3);

        internal static readonly WeatherGroup Sootfall = new(
            "11 - Sootfall Rite",
            "the Sootfall Rite (no Ashlands smoke or heat haze)",
            "MushroomSmokePuff",
            "smoke puffs",
            "The Ashlands ground smoke and the heat shimmer near lava are gone, so you can see. The heat itself still burns.",
            true,
            amount: 3);


        internal static readonly WeatherGroup AbyssalSurge = new(
            "12 - Abyssal Surge Rite",
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

        // ---- Domain ----

        internal static readonly WeatherGroup Packward = new(
            "13 - Packward Rite",
            "the Packward Rite (hostile creatures flee)",
            "WolfFang",
            "wolf fangs",
            "Hostile creatures (not bosses, not tamed) that come within the Domain radius of the obliterator run away for as long as " +
            "it lasts. Replaces a Carrion Rite going on.",
            true,
            amount: 9,
            minutes: 60f);

        internal static readonly WeatherGroup Carrion = new(
            "14 - Carrion Rite",
            "the Carrion Rite (wild animals come)",
            "Pukeberries",
            "bukeperries",
            "Wild animals within the Domain radius walk to the obliterator, out of the woods and into your traps, pens or walls, " +
            "unless they are already fighting or scared. Replaces a Packward Rite going on.",
            true,
            amount: 3);

        internal static readonly ConfigInfo<string> CarrionCreatures = new(
            Carrion.Enabled.Group,
            "Creatures",
            "Prefab names of the animals the Carrion Rite draws, separated by commas.",
            "Boar,Deer,Wolf",
            true);

        internal static readonly WeatherGroup Verdant = new(
            "15 - Verdant Rite",
            "the Verdant Rite (plants grow faster)",
            "AncientSeed",
            "ancient seeds",
            "Plants, crops, saplings and berry bushes within the Domain radius of the obliterator grow faster for as long as it " +
            "lasts, with a green sparkle now and then; what they grew stays after it ends.",
            true,
            amount: 9);

        internal static readonly ConfigInfo<float> VerdantGrowthPercent = new(
            Verdant.Enabled.Group,
            "Growth Speed Percent",
            new ConfigDescription(
                "How much faster things grow under the Verdant Rite: 50 grows half again as fast, 100 twice as fast.",
                new AcceptableValueRange<float>(1f, 1000f)),
            50f,
            true);

        internal static readonly WeatherGroup VatBoil = new(
            "16 - Vat-Boil Rite",
            "the Vat-Boil Rite (fermenters work faster)",
            "Bilebag",
            "bile bags",
            "Fermenters within the Domain radius of the obliterator need less time, bubbling now and then while they do; what they " +
            "fermented stays after it ends.",
            true,
            amount: 2,
            minutes: 60f);

        internal static readonly ConfigInfo<float> VatBoilTimeCut = new(
            VatBoil.Enabled.Group,
            "Time Cut Percent",
            new ConfigDescription("How much less time fermenting takes under the Vat-Boil Rite: 50 halves it.", new AcceptableValueRange<float>(1f, 90f)),
            50f,
            true);

        internal static readonly WeatherGroup AmberMend = new(
            "17 - Amber-Mend Rite",
            "the Amber-Mend Rite (buildings mend)",
            "Resin",
            "resin",
            "Damaged building pieces within the Domain radius of the obliterator are repaired one after another, a few each second, " +
            "with the hammer's repair effect.",
            true,
            amount: 50);

        internal static readonly WeatherGroup SovereignWard = new(
            "18 - Sovereign Ward Rite",
            "the Sovereign Ward Rite (buildings can't be harmed)",
            "QueenDrop",
            "a majestic carapace",
            "Built pieces within the Domain radius of the obliterator (walls, portals, chests, workbenches...) take no damage from " +
            "anything: they can only be taken down on purpose, with the hammer. Hostile projectiles (Gjall fireballs, catapult " +
            "rocks, drake frost) crumble harmlessly as they cross into it.",
            true);

        internal static readonly ConfigInfo<Toggle> SovereignSanctuary = new(
            SovereignWard.Enabled.Group,
            "Sanctuary Mode",
            "On: players within the ward can't be hurt either, by anything, so the base is a true safe place.",
            Toggle.Off,
            true);

        internal static readonly WeatherGroup Unmaking = new(
            "19 - Unmaking Rite",
            "the Unmaking Rite (gear back into materials)",
            "Thunderstone",
            "a thunderstone",
            "Every craftable weapon, tool, shield and piece of armor burnt along with it gives back part of what it took to make " +
            "and upgrade it (up to level 4: higher levels from other mods give back what level 4 does). Everything else burns as usual.",
            true,
            lasts: false);

        internal static readonly ConfigInfo<float> UnmakingPercent = new(
            Unmaking.Enabled.Group,
            "Return Percent",
            new ConfigDescription("How much of the full cost comes back, in percent (rounded down per material).", new AcceptableValueRange<float>(1f, 100f)),
            80f,
            true);

        internal static readonly WeatherGroup Grounding = new(
            "20 - Grounding Rite",
            "the Grounding Rite (ends the Domain rites)",
            "Flint",
            "flint",
            "Ends the Domain rites going on (Packward, Carrion, Verdant, Vat-Boil, Amber-Mend, Sovereign Ward) at once. The Equilibrium Rite leaves them be.",
            true,
            lasts: false);

        // ---- Blessings: only for the players within the blessing range when the obliterator burns ----

        private const string BlessingNote = " A blessing: only players within the Blessings Range of the obliterator when it burns " +
            "receive it, struck by its lightning (no harm done). It ends at death; the Equilibrium Rite leaves it be.";

        internal static readonly WeatherGroup CryptDelver = new(
            "21 - Crypt Delver Rite",
            "the Crypt Delver Rite (never wet)",
            "CryptKey",
            "a swamp key",
            "Rain and crypt water don't make you wet, so crypts are easier to explore." + BlessingNote,
            true,
            minutes: 45f);

        internal static readonly WeatherGroup GlacialSatiety = new(
            "22 - Glacial Satiety Rite",
            "the Glacial Satiety Rite (food fades slower)",
            "DragonTear",
            "a dragon tear",
            "Eaten food fades only down to the Food Floor Percent of what it gives, instead of half, until it runs out." + BlessingNote,
            true,
            minutes: 90f);

        internal static readonly ConfigInfo<float> FoodFloorPercent = new(
            GlacialSatiety.Enabled.Group,
            "Food Floor Percent",
            new ConfigDescription("The least health, stamina and eitr a food gives while it lasts, in percent of its full value (the game's own floor is about 50).", new AcceptableValueRange<float>(50f, 100f)),
            75f,
            true);

        internal static readonly WeatherGroup VeinShatter = new(
            "23 - Vein-Shatter Rite",
            "the Vein-Shatter Rite (ore veins shatter)",
            "HardAntler",
            "a hard antler",
            "Hitting an ore vein deals part of the hit to the pieces next to it, and once the vein is worn down far enough, the rest " +
            "breaks at once. The pickaxe wears as if it had hit every piece." + BlessingNote,
            true);

        internal static readonly ConfigInfo<float> VeinSpreadPercent = new(
            VeinShatter.Enabled.Group,
            "Spread Percent",
            new ConfigDescription("How much of a hit the neighboring pieces of the vein take, in percent.", new AcceptableValueRange<float>(0f, 100f)),
            30f,
            true);

        internal static readonly ConfigInfo<float> VeinShatterAtPercent = new(
            VeinShatter.Enabled.Group,
            "Shatter At Percent",
            new ConfigDescription("Once the vein has this much of its health left or less, in percent, the next hit breaks the rest. 0: never.", new AcceptableValueRange<float>(0f, 100f)),
            50f,
            true);

        internal static readonly ConfigInfo<string> VeinItems = new(
            VeinShatter.Enabled.Group,
            "Vein Drops",
            "Prefab names of the items whose veins shatter (a vein counts when it drops one of them), separated by commas.",
            "CopperOre,SilverOre,Softtissue",
            true)
        { IsAdvanced = true };

        internal static readonly WeatherGroup CarapaceTemper = new(
            "24 - Carapace Temper Rite",
            "the Carapace Temper Rite (gear wears slower)",
            "Mandible",
            "mandibles",
            "Equipped weapons, tools, bows, shields and armor lose less durability." + BlessingNote,
            true,
            amount: 2);

        internal static readonly ConfigInfo<float> WearReductionPercent = new(
            CarapaceTemper.Enabled.Group,
            "Wear Reduction Percent",
            new ConfigDescription("How much less durability equipped gear loses, in percent.", new AcceptableValueRange<float>(0f, 100f)),
            60f,
            true);

        internal static readonly WeatherGroup GraveBind = new(
            "25 - Grave-Bind Rite",
            "the Grave-Bind Rite (no skill loss on death)",
            "YagluthDrop",
            "a torn spirit",
            "Dying doesn't lower your skills (with or without the No Skill Drain status), and your tombstone shines a beam into the " +
            "sky that can be seen from far away until it is emptied. The death ends it." + BlessingNote,
            true,
            minutes: 60f);

        internal static readonly ConfigInfo<Toggle> GraveBeacon = new(
            GraveBind.Enabled.Group,
            "Tombstone Beam",
            "Show a beam into the sky over your tombstone after a death under the Grave-Bind Rite.",
            Toggle.On,
            false);

        internal static readonly WeatherGroup FortunesBoon = new(
            "26 - Fortune's Boon Rite",
            "the Fortune's Boon Rite (more loot)",
            "Wishbone",
            "a wishbone",
            "Creatures you kill drop the most of each item they can and trophies more often; trees you fell always give fine wood " +
            "and seeds." + BlessingNote,
            true);

        internal static readonly ConfigInfo<float> TrophyMultiplier = new(
            FortunesBoon.Enabled.Group,
            "Trophy Chance Multiplier",
            new ConfigDescription("How many times as likely trophies drop (up to always): 2 turns 5% into 10% and 60% into 100%.", new AcceptableValueRange<float>(1f, 20f)),
            2f,
            true);

        internal static readonly ConfigInfo<string> TreeItems = new(
            FortunesBoon.Enabled.Group,
            "Guaranteed Tree Drops",
            "Prefab names of the items a felled tree always drops (when it can drop them at all), separated by commas.",
            "FineWood,BirchSeeds,Acorn",
            true)
        { IsAdvanced = true };

        internal static readonly WeatherGroup Luminary = new(
            "27 - Luminary Rite",
            "the Luminary Rite (you shine)",
            "Wisp",
            "wisps",
            "A bright, warm light around you lights every direction at once (unlike the Dvergr circlet's beam), and everyone with " +
            "the mod sees it." + BlessingNote,
            true,
            amount: 5);

        internal static readonly ConfigInfo<float> LuminaryRange = new(
            Luminary.Enabled.Group,
            "Light Range",
            new ConfigDescription("How far the light reaches, in meters.", new AcceptableValueRange<float>(3f, 50f)),
            16f,
            true);

        internal static readonly WeatherGroup AbyssalDraw = new(
            "28 - Abyssal Draw Rite",
            "the Abyssal Draw Rite (items fly to you)",
            "Chitin",
            "chitin",
            "Items on the ground fly to you from much farther than the game's 2 m. An item a closer player can pick up goes to " +
            "them instead, unless their inventory is full." + BlessingNote,
            true,
            amount: 4);

        internal static readonly ConfigInfo<float> DrawRange = new(
            AbyssalDraw.Enabled.Group,
            "Pickup Range",
            new ConfigDescription("How far away items fly to you from, in meters.", new AcceptableValueRange<float>(2f, 30f)),
            12f,
            true);

        // ---- Incursions ----

        internal static readonly WeatherGroup Crimson = new(
            "29 - Crimson Rite",
            "the Crimson Rite (raid after raid)",
            "WitheredBone",
            "withered bones",
            "Raids keep coming to the obliterator, one after another, and no other raid starts anywhere meanwhile. Within the radius " +
            "the sky turns to a blood moon. Its status and message only show near the obliterator.",
            true,
            amount: 3,
            radius: 100f);

        internal static readonly ConfigInfo<float> CrimsonPause = new(
            Crimson.Enabled.Group,
            "Pause Between Raids Seconds",
            new ConfigDescription("How long after a raid ends the next one starts.", new AcceptableValueRange<float>(5f, 600f)),
            20f,
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

        internal static readonly WeatherGroup Sanctuary = new(
            "30 - Sanctuary Rite",
            "the Sanctuary Rite (ends the raid)",
            "IronScrap",
            "scrap iron",
            "Ends the raid going on in the world at once, and any Crimson Rite.",
            true,
            lasts: false);

        internal static readonly WeatherGroup Reset = new(
            "31 - Equilibrium Rite",
            "the Equilibrium Rite (ends all rites)",
            "BoneFragments",
            "bone fragments",
            "Ends every rite going on at once, except the Domain rites, so nobody's growth or protection is ended by accident " +
            "(the Grounding Rite ends those), and the blessings players carry.",
            true,
            lasts: false,
            amount: 3);

        private const string CustomEnvironmentHelp = "Its sky is made by the mod from the game's own (see Environments).";

        internal static class Display
        {
            internal const string Group = "32 - Display";

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
                "and their tooltips say which rite they perform or that they just burn.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowExplanations = new(
                Group,
                "Show Explanations",
                "Show the gray explanations next to rite names (what each rite does, why an item decides) in hover texts, item tooltips " +
                "and the rites page. Off: only the item and the rite's name, for less clutter once you know them.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowRitesFlap = new(
                Group,
                "Show Rites Flap",
                "With the obliterator open, a flap under it opens a page listing the rites you know, in tabs: the item to burn and the rite it performs.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowVerdantSparkles = new(
                Group,
                "Show Verdant Sparkles",
                "Under the Verdant Rite, plants and bushes that grow faster sparkle green now and then.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowFermenterBubbles = new(
                Group,
                "Show Vat-Boil Bubbles",
                "Under the Vat-Boil Rite, fermenters that work faster bubble now and then.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowBlessingLightning = new(
                Group,
                "Show Blessing Lightning",
                "Show the lightning striking each player who receives a blessing.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowCreatureEffects = new(
                Group,
                "Show Creature Effects",
                "Creatures fleeing a Packward Rite drip with fear, and animals drawn by a Carrion Rite trail a green smell.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowMessages = new(
                Group,
                "Show Messages",
                "Show a message when someone performs a rite, and when it ends.",
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
}

// The settings of one rite, in its own section.
internal sealed class WeatherGroup
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

    internal WeatherGroup(string group, string effect, string keyItem, string keyItemName, string why, bool enabled, bool lasts = true,
        string? environments = null, string namePrefix = "", int amount = 1, float minutes = 30f, float radius = 0f)
    {
        Enabled = new(
            group,
            namePrefix + "Enabled",
            $"Burning {keyItemName} in the obliterator performs {effect}. {why}",
            enabled ? Toggle.On : Toggle.Off,
            true);

        if (lasts) Minutes = new(
            group,
            "Duration Minutes",
            new ConfigDescription(
                $"How long {effect} lasts, in minutes of play. Sleeping doesn't shorten it; pausing a single player game pauses it. " +
                "Performing it again starts the time over.",
                new AcceptableValueRange<float>(0.5f, 600f)),
            minutes,
            true);

        KeyItem = new(
            group,
            namePrefix + "Key Item",
            $"Prefab name of the item that performs {effect} when burnt (default {keyItem}, {keyItemName}).",
            keyItem,
            true);

        KeyAmount = new(
            group,
            namePrefix + "Key Item Amount",
            new ConfigDescription(
                $"How many of the key item have to be in the obliterator to perform {effect}. Fewer just burn.",
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
