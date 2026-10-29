using BepInEx.Configuration;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;

namespace JoksterCube.TempestRite.Settings;

internal static class Constants
{
    internal static class Plugin
    {
        internal const string ModName = "TempestRite";
        internal const string ModVersion = "0.1.57";
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
        // Server -> client: the rites going on, and what was just performed (blessings too, for the players near it).
        internal const string State = $"{Plugin.ModName}_State";
        // Server -> client: a request was refused.
        internal const string Refused = $"{Plugin.ModName}_Refused";
        // Vein owner -> striking player: a Vein-Shatter shatter broke this many more pieces (wear the pickaxe).
        internal const string VeinWear = $"{Plugin.ModName}_VeinWear";
        // Player -> everyone: show a kinship rite's effect (sparks and fire, or lightning) on me.
        internal const string KinshipShown = $"{Plugin.ModName}_KinshipShown";
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
        internal const string GaleBreathName = "Gale-Breath Rite";
        internal const string NoMistName = "True Sight Rite";
        internal const string SootfallName = "Sootfall Rite";
        internal const string CryptDelverName = "Crypt Delver Rite";
        internal const string PackwardName = "Packward Rite";
        internal const string CarrionName = "Carrion Rite";
        internal const string VerdantName = "Verdant Rite";
        internal const string CrimsonName = "Crimson Rite";
        internal const string SanctuaryName = "Sanctuary Rite";
        internal const string RagnarokName = "Ragnarök Rite";
        internal const string ResetName = "Equilibrium Rite";
        internal const string GroundingName = "Grounding Rite";
        internal const string VatBoilName = "Vat-Boil Rite";
        internal const string PitchBellowsName = "Pitch-Bellows Rite";
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
        internal const string SkalName = "Skål Rite";
        internal const string SkyCallName = "Sky-Call Rite";
        internal const string AnvilSparkName = "Anvil-Spark Rite";

        internal const string GroupAtmospheric = "Atmospheric";
        internal const string GroupCelestial = "Celestial";
        internal const string GroupBiomeMastery = "Biome Mastery";
        internal const string GroupDomain = "Domain";
        internal const string GroupReckoning = "Reckoning";
        internal const string GroupBlessings = "Blessings";
        internal const string GroupKinship = "Kinship";

        // {0}: the rite's name, colored.
        internal const string StartedByYou = "You performed the {0}";
        internal const string StartedBy = "{0} performed the {1}";
        internal const string EndedWeather = "The weather returns to its own ways";
        internal const string EndedDaylight = "The sun returns to its own ways";
        internal const string EndedGaleBreath = "The wind returns to its own strength";
        internal const string EndedMistlands = "The mist returns to the Mistlands";
        internal const string EndedAshlands = "The smoke returns to the Ashlands";
        internal const string EndedVatBoil = "The fermenters and cooking fires slow down again";
        internal const string EndedPitchBellows = "The furnaces cool to their usual pace";
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
        internal const string GraveBindReequipped = "The Grave-Bind Rite puts your gear back on";
        // Kinship rites. {0}: who performed it ("You" for the local player), {1}: the rite's name, colored.
        internal const string YouName = "You";
        internal const string SkalReceived = "{0} performed the {1}: Skål! Drink and rest";
        internal const string SkyCallRenewed = "{0} performed the {1}: the sky renews your forsaken power";
        internal const string SkyCallReady = "{0} performed the {1}, but your forsaken power is ready already";
        internal const string SkyCallNoPower = "{0} performed the {1}, but you have no forsaken power to renew";
        // {2}: how long until the sky can renew it again.
        internal const string SkyCallWaiting = "{0} performed the {1}, but the sky renewed your power not long ago ({2} left)";
        internal const string SkyCallEnded = "The sky can renew your forsaken power again";
        internal const string SkalRestedAlready = "{0} performed the {1}, but you are rested longer already";
        // {2}: how long until it can rest the player again.
        internal const string SkalWaiting = "{0} performed the {1}, but you toasted not long ago ({2} left)";
        internal const string SkalEnded = "A Skål Rite can rest you again";
        // {2}: how many items.
        internal const string AnvilSparkRepaired = "{0} performed the {1}: the anvil rings, {2} of your items repaired";
        internal const string AnvilSparkNothing = "{0} performed the {1}, but none of your items needs repair";
        // {2}: how long until it can repair the player's gear again.
        internal const string AnvilSparkWaiting = "{0} performed the {1}, but your gear was repaired not long ago ({2} left)";
        internal const string AnvilSparkEnded = "The anvil can repair your gear again";
        // {0}: how many.
        internal const string CryptDelverFound = "The Crypt Delver Rite finds {0} more in the chest";
        internal const string EndedIncursion = "The raids stop coming";
        // {0}: how many creatures died within the bubble so far.
        internal const string CrimsonKillCount = "Kills: {0}";
        // {0}: kills.
        internal const string CrimsonSurvived = "You survived the Crimson Rite with {0} kills! Your reward waits in the obliterator";
        internal const string CrimsonSurvivedNoReward = "You survived the Crimson Rite, but nothing was slain";
        internal const string CrimsonFailedObliterator = "The obliterator is destroyed: the Crimson Rite is broken";
        internal const string CrimsonFailedFallen = "Everyone within the bubble has fallen or left: the Crimson Rite is broken";
        internal const string CrimsonFailedEnded = "The Crimson Rite was ended";
        // The bubble of the Crimson or Ragnarök Rite. {0}: the rite's name.
        internal const string IncursionNoPortal = "The {0}'s bubble holds you: no portals until it ends";
        internal const string IncursionNoPortalIn = "The {0}'s bubble seals the portal's far end until it ends";
        internal const string IncursionHeld = "The bubble holds you until the {0} ends";
        internal const string IncursionKeptOut = "The bubble keeps you out until the {0} ends";
        // {0}: the Domain rite, {1}: the Crimson or Ragnarök Rite.
        internal const string DomainCancelled = "The {0} is cancelled: its obliterator is within the {1}'s bubble";
        // {0}: the Domain rite.
        internal const string DomainBroken = "The obliterator is destroyed: the {0} has ended";
        internal const string IncursionNoBuild = "Nothing can be built, repaired or torn down within the {0}'s bubble";
        internal const string IncursionOnlySanctuary = "Within the {0}'s bubble, only the Sanctuary Rite can be performed";
        internal const string IncursionBusy = "The {0} is going on already: only one Crimson or Ragnarök Rite at a time";
        // On the map: the rite's name, ominous.
        internal const string CrimsonPin = "<color=#ff4a3a>Crimson Rite</color>";
        internal const string RagnarokPin = "<color=#ff9a2e>Ragnarök</color>";
        // {0}: how many bosses will rise.
        internal const string RagnarokBegins = "Ragnarök! {0} bosses will rise, one after another";
        internal const string RagnarokNoBosses = "The Ragnarök Rite calls no boss: none has fallen to everyone within the circle";
        internal const string RagnarokNoOne = "The Ragnarök Rite calls no boss: nobody stands within the circle";
        // {0}: the boss, {1}: which one, {2}: of how many.
        internal const string RagnarokRises = "{0} rises ({1} of {2})";
        internal const string RagnarokFalls = "{0} falls ({1} of {2})";
        internal const string RagnarokSurvived = "You survived Ragnarök! Treasure waits in the obliterator";
        internal const string RagnarokSurvivedNoReward = "You survived Ragnarök!";
        internal const string RagnarokFailedObliterator = "The obliterator is destroyed: Ragnarök is broken";
        internal const string RagnarokFailedFallen = "Everyone within the bubble has fallen or left: Ragnarök is lost";
        internal const string RagnarokFailedEnded = "The Ragnarök Rite was ended";
        internal const string RagnarokFailedTime = "Time ran out: the bosses withdraw and Ragnarök is lost";
        internal const string RagnarokFailedSpawn = "No boss could rise: the Ragnarök Rite is broken";
        internal const string EndedVerdant = "The growth slows again";
        internal const string EndedPackward = "The creatures are no longer afraid";
        internal const string EndedCarrion = "The creatures go back to the wild";
        internal const string RefusedAdminsOnly = "Only admins can perform rites";
        internal const string RefusedDisabled = "This rite is turned off on this server";
        // The connection error shown on joining. {0}: mod name and version.
        internal const string ServerLacksMod = "The server doesn't have {0}. Remove it to join, or ask the server to install it.";

        // First line: what it does, capitalized.
        internal const string StatusTooltip = "{0}. Performed by {1}.\nEveryone with the mod has this weather until it ends, except where the world forces its own (dungeons, raids, {2}).";
        internal const string StatusTooltipNoBiomes = "{0}. Performed by {1}.\nEveryone with the mod has this weather until it ends, except where the world forces its own (dungeons, raids).";
        internal const string StatusTooltipPolarDay = "Day all the time. Performed by {0}.\nThe sun stays up and creatures spawn as by day, for everyone with the mod until it ends. Time goes on as usual.";
        internal const string StatusTooltipPolarNight = "Night all the time. Performed by {0}.\nThe sun stays down and night creatures spawn, for everyone with the mod until it ends. Time goes on as usual.";
        internal const string StatusTooltipTailwind = "Wind behind your ship. Performed by {0}.\nWhile you sail, the wind blows from behind, like Moder's power, until it ends or you die. Only its direction: its strength stays the weather's.";
        internal const string StatusTooltipGaleBreath = "The strongest wind. Performed by {0}.\nThe wind blows as hard as it can, for everyone with the mod until it ends. Only its strength: it still turns with the weather.";
        internal const string StatusTooltipSootfall = "No Ashlands smoke or heat haze. Performed by {0}.\nThe Ashlands ground smoke and the heat shimmer near lava are gone for everyone with the mod until it ends. The heat still burns.";
        internal const string StatusTooltipNoMist = "No Mistlands mist. Performed by {0}.\nThe Mistlands mist is gone for everyone with the mod until it ends.";
        internal const string StatusTooltipCryptDelver = "Never wet. Performed by {0}.\nRain and crypt water don't touch you until it ends or you die.";
        // {1}: the food floor in percent.
        internal const string StatusTooltipGlacialSatiety = "Food fades slower. Performed by {0}.\nEaten food never gives less than {1}% of its health, stamina and eitr, until it ends or you die.";
        // {1}: how much of a hit spreads, {2}: at how much health left the vein shatters, both in percent.
        internal const string StatusTooltipVeinShatter = "Rocks and ore shatter. Performed by {0}.\nHitting anything mined in pieces (rocks, ore, mud piles, ice...) deals {1}% of the hit to the pieces next to it, and once it is down to {2}% the rest breaks at once. Each hit wears your pickaxe as usual; the shatter wears it once more for each piece it breaks. Until it ends or you die.";
        // {1}: how much less wear, in percent (from the Wear Multiplier).
        internal const string StatusTooltipCarapaceTemper = "Gear wears slower. Performed by {0}.\nYour equipped weapons, tools and armor lose {1}% less durability until it ends or you die.";
        internal const string StatusTooltipGraveBind = "No skill loss on death. Performed by {0}.\nIf you die, you keep all your skills, and your tombstone shines a beam into the sky; looting it puts your gear back on. Ends with that death.";
        // {1}: the luck multiplier. {2}: what happens instead to an item the forge would destroy.
        internal const string StatusTooltipFortunesBoon = "More luck. Performed by {0}.\nLucky crafts, cooking and harvests come {1}x as often. At the Forge of Potential, an upgrade that would destroy your item {2} instead, once: that ends the blessing early, as does death.";
        internal const string FortuneForgeLoses = "costs it {0} level(s)";
        internal const string FortuneForgeKeeps = "leaves its level be";
        // {0}: the item's name. {1}: its level now.
        internal const string FortuneSavedItem = "Fortune's Boon saves your {0} from breaking: it is level {1}";
        // {1}: radius in meters, {2}: how much less time, in percent (from the Time Multiplier).
        internal const string StatusTooltipVatBoil = "Fermenting and cooking go faster. Performed by {0}.\nFermenters and cooking stations within {1} m of the obliterator work {2}x as fast until it ends.";
        internal const string StatusTooltipPitchBellows = "Smelters and kilns work faster. Performed by {0}.\nSmelters, blast furnaces, charcoal kilns and eitr refineries within {1} m of the obliterator work {2}x as fast until it ends.";
        internal const string StatusTooltipSovereignWard = "Buildings can't be harmed. Performed by {0}.\nBuilt pieces within {1} m of the obliterator take no damage and hostile projectiles crumble at its edge until it ends.";
        internal const string StatusTooltipSovereignSanctuary = "Buildings can't be harmed. Performed by {0}.\nBuilt pieces within {1} m of the obliterator take no damage, hostile projectiles crumble at its edge, and no player there can be hurt, until it ends.";
        // {1}: the light's range in meters.
        internal const string StatusTooltipLuminary = "You shine. Performed by {0}.\nYou shine with a warm light, lighting the ground and walls {1} m around you, for everyone to see, until it ends or you die.";
        // {1}: the pickup range in meters.
        internal const string StatusTooltipAbyssalDraw = "Items fly to you. Performed by {0}.\nItems within {1} m fly to you, unless a closer player with room can take them, until it ends or you die.";
        internal const string StatusTooltipAmberMend = "Buildings mend. Performed by {0}.\nDamaged building pieces within {1} m of the obliterator are repaired, one after another, until it ends.";
        // {1}: serpent multiplier, {2}: fish multiplier.
        internal const string StatusTooltipAbyssalSurge = "More serpents and fish. Performed by {0}.\nSerpents come {1}x as often at night and in storms, fish swim in schools {2}x as big and more of them are big ones, for everyone with the mod until it ends.";
        // {1}: the radius in meters.
        internal const string StatusTooltipPackward = "Creatures shun the area. Performed by {0}.\nCreatures (not bosses or tamed) keep out of {1} m around the obliterator until it ends. One that gets hurt fights back, and its pack nearby turns on whoever it notices. A raid reaching the area pauses it.";
        internal const string StatusTooltipCarrion = "Creatures come. Performed by {0}.\nCreatures (not bosses or tamed) within {1} m walk to the obliterator until it ends, their senses dulled. A raid reaching the area pauses it.";
        // {2}: how much faster, in percent.
        internal const string StatusTooltipVerdant = "Plants grow faster. Performed by {0}.\nPlants and bushes within {1} m of the obliterator grow {2}x as fast until it ends.";
        internal const string StatusTooltipSkal = "Toasted not long ago. Performed by {0}.\nThe Skål Rite rested you. No other Skål Rite can rest you until this ends; death doesn't end it.";
        internal const string StatusTooltipAnvilSpark = "Gear repaired not long ago. Performed by {0}.\nThe Anvil-Spark Rite repaired your gear. No other Anvil-Spark Rite can repair it until this ends; death doesn't end it.";
        internal const string StatusTooltipSkyCall = "Forsaken power renewed. Performed by {0}.\nThe sky renewed your forsaken power. No other Sky-Call Rite can renew it until this ends; death doesn't end it.";
        // {1}: the radius in meters.
        internal const string StatusTooltipCrimson = "Raid after raid. Performed by {0}.\nRaids keep coming within {1} m of the obliterator under a blood moon, and nowhere else. Nobody gets into or out of the bubble, and no portal there works, until it ends; no creature gets in or out. Nothing can be built or torn down inside, and only the Sanctuary Rite performed. Survive to the end for a reward in the obliterator for each kill.";
        // {1}: the radius in meters.
        internal const string StatusTooltipRagnarok = "Boss after boss. Performed by {0}.\nBosses everyone inside has slain rise within {1} m of the obliterator, one after another, under a burning sky. Nobody gets into or out of the bubble, and no portal there works, until it ends. Nothing can be built or torn down inside, and only the Sanctuary Rite performed. Slay them all before the time runs out for treasure in the obliterator.";
        internal const string SomeoneName = "someone";

        // Item names in orange, as the game shows them in hover text.
        internal const string ItemColor = "orange";
        internal const string Faint = "#c9c9c9";

        // {0}: rite name, colored. {1}: the gray explanation (Explanation below), or nothing when Show Explanations is off.
        internal const string LeverBrings = "{0}{1}";
        internal const string Explanation = " <color=" + Faint + ">({0})</color>";
        // On a plant under the Verdant Rite (without AzuHoverStats, which shows the time itself): when it should be grown.
        internal const string VerdantReadyIn = "\n<color=" + Faint + ">Grown in about {0} min</color>";
        internal const string VerdantReadySoon = "\n<color=" + Faint + ">Grown in under a minute</color>";
        // On a fermenter under the Vat-Boil Rite (without AzuHoverStats): when it should be done.
        internal const string StationReadyIn = "\n<color=" + Faint + ">Ready in about {0} min</color>";
        internal const string StationReadySoon = "\n<color=" + Faint + ">Ready in under a minute</color>";
        // In AzuHoverStats' cooking times, under the Vat-Boil Rite.
        internal const string WontBurn = "<color=#9fe07a>[won't burn]</color>";
        internal const string LeverNotAllowed = "<color=#ff7a5c>Only admins can perform rites</color>";
        // On the lever of an obliterator with rites off, when rite items are in it.
        internal const string LeverSwitchedOff = "<color=#9a9a9a>Rites are off: these items just burn (turn them on from the flap under the open obliterator)</color>";

        // Topic of the popup when a rite becomes known (see RiteDiscovery).
        internal const string NewRite = "New rite";

        internal const string HatchTitle = "<color=#9fd8ff>Burn to perform a rite:</color>";
        internal const string HatchLine = "  <color=" + ItemColor + ">{0}</color>: {1}{2}";

        internal const string TooltipDecides = "Performs the {0}{1} when burnt.";
        // {0}: the rite that goes first and keeps this item's rite out.
        internal const string TooltipOverruled = "<color=#9a9a9a>Just burns: the {0}, from a lower item, goes first and keeps it out.</color>";
        internal const string TooltipOverruledShort = "<color=#9a9a9a>Just burns.</color>";
        // {0}: how many are needed, {1}: the rite, {2}: how many are in.
        internal const string TooltipNeedsMore = "<color=#9a9a9a>Needs {0} to perform the {1} ({2} in), until then just burns.</color>";
        internal const string TooltipNeedsMoreShort = "<color=#9a9a9a>Needs {0}.</color>";
        // {0}: the Unmaking Rite. The materials it gives back are shown under the line.
        internal const string TooltipUnmade = "The {0} unmakes it, giving back:";
        internal const string TooltipUnmadeNothing = "<color=#9a9a9a>The {0} unmakes it, but too little went into it for anything to come back.</color>";
        // An item with how many are needed: "2x Ooze", the number in white inside the item's orange.
        internal const string ItemAmount = "<color=#f2f2f2>{0}x</color> {1}";
        // Between the names of a rite's key items: "Surtling core / Ruby".
        internal const string ItemSeparator = " / ";

        // The flap under the obliterator: its switch, and the rites page it opens.
        internal const string SwitchTooltip = "Perform rites";
        internal const string PageTooltip = "Rites";
        internal const string SwitchedOn = "This obliterator performs rites";
        internal const string SwitchedOff = "This obliterator burns as usual";
        internal const string SwitchNotOwner = "Someone else is using this obliterator";
        internal const string PanelHint = "Burn a rite item to perform its rite. The lowest one decides. Click an item to put it in.";
        // Tooltip of an item on the rites page. {0}: how many it takes, {1}: how many you have.
        internal const string PanelItemMissing = "Missing items";
        internal const string PanelItemNeeds = "Needs {0}, you have {1}";
        internal const string PanelItemPut = "Click to put {0} in";
        internal const string PanelItemIn = "In the obliterator";
        // An item whose rite clashes with one already in. {0}: that rite.
        internal const string PanelItemClash = "Clashes with a prepared rite";
        internal const string PanelItemClashDetail = "The {0} is already prepared";
        // Clicking it. {0}: the clicked rite, {1}: the one already in.
        internal const string PanelClashWarning = "The {1} is already prepared: the {0} can't share its burn";
        // After the name of the rite keeping the clicked one out, for a moment.
        internal const string PanelPrepared = "<color=#e8735f>Already prepared</color>";
        // The "+N" button of a rite with more items than fit in its row. {0}: how many more.
        internal const string PanelMoreItems = "Show {0} more";
        internal const string PanelFewerItems = "Show fewer";
        // After the name of the rite pulling the lever performs.
        internal const string PanelReady = "<color=#8fe08f>Ready: pull the lever</color>";
        internal const string PanelRowDetail = "<color=" + ItemColor + ">{0}</color>  <color=" + Faint + ">{1}</color>";
        internal const string PanelRowItem = "<color=" + ItemColor + ">{0}</color>";
        internal const string PanelMoreToFind = "{0} more rites wait to be found here: hold their items to learn them.";
        internal const string PanelOneMoreToFind = "1 more rite waits to be found here: hold its item to learn it.";
        internal const string CloseTooltip = "Close";
    }

    // The "Tempest Rite" page of the Compendium (see RiteCompendium). Numbers in braces come from the settings.
    internal static class CompendiumTexts
    {
        internal const string Topic = "Tempest Rite";

        internal const string Intro =
            "Rites are performed at an <color=orange>obliterator</color>. Open it and click the fire on the flap under it to turn its rites on: " +
            "until then it burns everything as usual. Put a rite item in, along with whatever else you want to burn, and pull the lever. " +
            "Once it burns, the rite takes hold.";
        internal const string Rules =
            "One burn performs every rite with enough of its item in. When two can't go together (two weather rites, say), the one from the " +
            "lowest item goes first and the other's item just burns. Performing a rite again starts its time over. Each rite going on shows " +
            "as a status with the time left, and the book on the obliterator's flap opens the rites page, where clicking an item puts in as " +
            "many as its rite needs.";
        internal const string AdminsOnly = "<color=#ff7a5c>On this server, only admins can perform rites.</color>";
        internal const string Learning = "A rite shows here once you have held its item, the way recipes are learned.";
        internal const string NoneKnown = "You know no rites yet: hold a rite item to learn its rite.";
        internal const string MoreToFind = "{0} more rites wait to be found: hold their items to learn them.";
        internal const string OneMoreToFind = "1 more rite waits to be found: hold its item to learn it.";

        // Under each rite's name. {0}: the item(s) to burn.
        internal const string Burn = "Burn {0}";
        internal const string Separator = "  ·  ";
        internal const string Lasts = "lasts {0}";
        internal const string AtOnce = "happens at once";
        // {0}: how long the players have.
        internal const string RagnarokTime = "{0} to slay them all";
        // {0}: how long until the same rite can give a player something again.
        internal const string KinshipWait = "{0} wait before the next";
        // {0}: how long the player stays rested, {1}: the wait.
        internal const string SkalTimes = "Rested for {0}, {1} wait before the next";

        // What each group shares. {0}: the Domain radius or blessing range in meters.
        internal const string GroupAtmospheric =
            "The weather, for everyone, shown as each biome's own kind of it: snow in the mountains, Mistlands rain in the Mistlands, ash " +
            "rain in the Ashlands. One weather rite at a time: a new one replaces the last. Raids, boss fights and dungeons keep their own " +
            "weather, and a weather rite never makes a place warmer than its own weather would.";
        internal const string GroupCelestial =
            "The sun and the wind, for everyone. Time itself goes on as usual: days are counted, crops grow, beds go by the real clock and " +
            "clocks show the real time.";
        internal const string GroupBiomeMastery = "One part of the world bent to your will, for everyone.";
        internal const string GroupDomain =
            "Work within {0} m of the obliterator they were performed at, and their status shows only there. Each ends when its obliterator " +
            "is destroyed. The Equilibrium Rite leaves them be, so nobody's growth or protection is ended by accident: only the Grounding " +
            "Rite ends them.";
        internal const string GroupBlessings =
            "For the players standing within {0} m of the obliterator when it burns: its lightning strikes each of them, harmlessly. Each " +
            "keeps their own, through logging out, until it ends or they die. The Equilibrium Rite leaves them be.";
        internal const string GroupKinship =
            "For everyone on the server at once, wherever they are. Each player stops what they are doing to toast, roar or flex, and nobody " +
            "can be hurt meanwhile. Once a kinship rite gives a player something, no other one of the same rite can until the wait is over, " +
            "even through death; a player it passes by gets no wait.";
        internal const string GroupReckoning = "Fights for great rewards, and the rites that end others.";

        internal const string Clear =
            "Clear skies: no rain, snow or fog, for building, farming and travelling. Cold biomes stay as cold as their own weather would make them.";
        internal const string Thunderstorm =
            "A thunderstorm: strong wind for sailing, and the weather serpents come out in. A snowstorm in the mountains.";
        internal const string Rain =
            "Rain: light rain in the meadows, snow in the mountains. Puts out spreading fire that has no roof over it, and keeps cinders from " +
            "lighting more.";
        internal const string Mist = "Mist: fog that hides what is far away.";
        internal const string Aurora = "A clear sky with shimmering auroras at night, like the Deep North's, and calm, quiet winds.";
        internal const string PolarDay =
            "Polar day: the sun stays up and it counts as day, so creatures spawn as by day and night creatures leave.";
        internal const string PolarNight =
            "Polar night: the sun stays down and it counts as night, so night creatures spawn (more of them, and stronger) and nights are cold.";
        internal const string GaleBreath =
            "The wind blows as hard as it can, everywhere: the fastest sailing there is. Only its strength: it still turns with the weather. " +
            "Together with the Tailwind Rite, it is behind your ship at full strength.";
        internal const string NoMist = "No new Mistlands mist is made, and what is there fades. Creatures see as if there were no mist.";
        internal const string Sootfall =
            "In the Ashlands, the smoke and ash that come with its weather are turned off, and the heat shimmer near lava isn't shown. Only " +
            "the look: the heat still burns.";
        // {0}: serpent multiplier, {1}: fish school multiplier, {2}: big fish multiplier.
        internal const string AbyssalSurge =
            "The sea stirs: serpents come out {0}x as often at night and in rain and storms, and fish swim in schools {1}x as big in the sea, " +
            "along coasts and in rivers. Each star of a fish is {2}x as likely, so more of them are big ones.";
        // {0}: how long a hurt creature fights back.
        internal const string Packward =
            "Creatures (not bosses, not tamed) shun the area: those inside find their way out, those outside don't wander in or follow a " +
            "target in, but wait at the edge. One that gets hurt fights back for {0}, and so does its pack nearby against whoever it notices; the rest still " +
            "run. A raid reaching the area pauses it (raid creatures are left alone) until the raid ends. They drip with fear and show a " +
            "blue icon. Replaces a Carrion Rite going on.";
        // {0}: how far a drawn creature sees and hears, in percent.
        internal const string Carrion =
            "Creatures (not bosses, not tamed) walk to the obliterator, out of the woods and into your traps, pens or walls, trailing a green " +
            "smell and showing a green icon. On the way they see and hear only {0}% as far, but one that notices you reacts as usual, then " +
            "walks on once it calms down. A raid reaching the area pauses it (raid creatures are left alone) until the raid ends. " +
            "Replaces a Packward Rite going on.";
        // {0}: growth multiplier.
        internal const string Verdant =
            "Crops, saplings and berry bushes grow {0}x as fast, some now and then giving off green motes. Looking at a plant or a " +
            "picked bush shows it is under the rite. What they grew stays after the rite ends.";
        // {0}: speed multiplier.
        internal const string VatBoil =
            "Fermenters and cooking stations (the cooking station, the iron one and the oven) work {0}x as fast, some now and then " +
            "giving off golden motes, and nothing cooked there burns while the rite lasts. What they did stays after the rite ends.";
        // {0}: speed multiplier.
        internal const string PitchBellows =
            "Smelters, blast furnaces, charcoal kilns and eitr refineries work {0}x as fast, some now and then giving off ember motes. " +
            "They still use up ore and fuel as usual, and need their roof and clear smoke. What they made stays after the rite ends.";
        internal const string AmberMend =
            "Damaged building pieces are repaired one after another, a couple each second, each with the hammer's repair effect.";
        internal const string SovereignWard =
            "Built pieces (walls, portals, chests, workbenches...) take no damage from anything: only the hammer takes them down. Hostile " +
            "projectiles (Gjall fireballs, catapult rocks, drake frost) crumble as they cross in.";
        internal const string SovereignSanctuary = " Players inside can't be hurt either.";
        internal const string Grounding = "Ends the Domain rites going on.";
        // {0}: loot multiplier, {1}: chance of one more item, {2}: of two more, both in percent.
        internal const string CryptDelver =
            "The first time you open a chest in a dungeon (crypts, caves, infested mines...), it gets {0}x the items (rounded up, so 1 " +
            "becomes 2), with a {1}% chance of one more and {2}% of two more. The extra items can be anything the chest can hold. Each chest " +
            "gives this once.";
        // {0}: the food floor, in percent.
        internal const string GlacialSatiety =
            "Food fades only down to {0}% of what it gives (the game's own floor is about half), until it runs out.";
        // {0}: how much of a hit spreads, {1}: at how much health the rest breaks, both in percent.
        internal const string VeinShatter =
            "Hitting anything mined in pieces (rocks, ore veins, mud piles, ice, giant swords and skulls...) deals {0}% of the hit to the " +
            "pieces next to it, and once it is down to {1}% of its health, the rest breaks at once. Each hit wears your pickaxe as usual; the " +
            "shatter wears it once more for each piece it breaks.";
        internal const string VeinShatterNoBreak =
            "Hitting anything mined in pieces (rocks, ore veins, mud piles, ice, giant swords and skulls...) deals {0}% of the hit to the " +
            "pieces next to it. Each hit wears your pickaxe as usual; the shatter wears it once more for each piece it breaks.";
        // {0}: how much less wear, in percent.
        internal const string CarapaceTemper = "Your equipped weapons, tools, bows, shields and armor lose {0}% less durability.";
        internal const string GraveBind = "Dying doesn't lower your skills.";
        internal const string GraveBindBeam =
            " Your tombstone shines a beam into the sky that you can see from far away until you empty it.";
        internal const string GraveBindReequip = " Looting your tombstone puts the gear you wore back on.";
        // {0}: luck multiplier, {1}: what happens to an item the forge would destroy (Messages.FortuneForgeLoses or Keeps).
        internal const string FortunesBoon =
            "Your lucky rolls for extra items come {0}x as often: crafting and cauldron cooking, the cooking station, harvesting crops and " +
            "other pickables. At the Forge of Potential, an upgrade that would destroy your item {1} instead, once, which ends the blessing.";
        // {0}: the light's range in meters.
        internal const string Luminary =
            "You shine with a warm light like a lantern, lighting the ground and walls {0} m around you, in caves and dungeons too, and " +
            "everyone sees it.";
        internal const string LuminaryShadows = " Floors, ceilings and walls block it.";
        // {0}: pickup range in meters.
        internal const string AbyssalDraw =
            "Items fly to you from {0} m instead of 2 m. An item a closer player can pick up goes to them, unless their inventory is full.";
        internal const string Tailwind =
            "While you sail, the wind always blows from behind, like Moder's power, also for everyone on the same ship. Only its direction: " +
            "its strength stays the weather's (the Gale-Breath Rite makes it the strongest).";
        internal const string Skal =
            "Everyone raises a toast, aglow like a stamina mead amid the crackle of a fire, and is Rested as if at comfort 3. Anyone rested " +
            "longer already is passed by.";
        internal const string SkyCall =
            "Everyone whose forsaken power is cooling down is struck by lightning (no harm done), roars, and has it ready again. Anyone whose " +
            "power is ready already is passed by.";
        internal const string AnvilSpark =
            "An anvil rings and sparks burst around everyone, who flexes and has every worn item in their inventory, equipped gear included, " +
            "repaired, no workbench or forge needed. Anyone with nothing to repair is passed by.";
        // {0}: pause between raids, {1}: radius in meters.
        internal const string Crimson =
            "Raids keep coming to the obliterator, one after another, {0} apart, picked among those the players within {1} m are ready for, " +
            "with no wait and no base needed; lightning strikes the obliterator as each new raid comes. No other raid starts anywhere, and " +
            "raid creatures only spawn inside. The sky turns to a blood moon and the area is marked on everyone's map.\n" +
            "A shield bubble covers it: nobody and no creature gets in or out, no portal into or out of it works, nothing can be built, " +
            "repaired or torn down inside, and no rite but the Sanctuary Rite can be performed there. No Domain rite works within it while " +
            "it goes on.\n";
        // {0}: the reward for each kill.
        internal const string CrimsonReward =
            "Each kill inside shows in the top left corner: survive to the end and the obliterator holds {0} for each kill. ";
        internal const string CrimsonNoReward = "Each kill inside shows in the top left corner. ";
        internal const string CrimsonEnd =
            "However it ends, the hostile creatures still inside die and drop nothing. It breaks off with no reward if the obliterator is " +
            "destroyed, everyone inside falls or leaves, or the Sanctuary or Equilibrium Rite ends it. Only one Crimson or Ragnarök Rite " +
            "can go on at a time.";
        // {0}: how many bosses, {1}: pause between bosses, {2}: radius in meters.
        internal const string Ragnarok =
            "A boss rush: {0} bosses rise at the obliterator one after another, weakest first, {1} apart, picked among those every player " +
            "within {2} m has slain (with too few, some come again). If nobody inside has slain any boss, nothing rises. Which bosses come " +
            "is only seen as each rises. The sky burns ember and gold and the area is marked on everyone's map.\n" +
            "A shield bubble covers it like the Crimson Rite's: nobody gets in or out, bosses can't leave, no portal into or out of it " +
            "works, nothing can be built, repaired or torn down inside, and no rite but the Sanctuary Rite can be performed there.\n" +
            "Slay them all in time and lightning strikes the obliterator, which holds treasure: one roll for each boss, richer for stronger " +
            "bosses. However it ends, the bosses and creatures still inside vanish and drop nothing. It breaks off with no reward if the " +
            "time runs out, the obliterator is destroyed, everyone inside falls or leaves, or the Sanctuary or Equilibrium Rite ends it. " +
            "Only one Crimson or Ragnarök Rite can go on at a time.";
        internal const string Sanctuary =
            "Ends the raid going on in the world at once, and any Crimson or Ragnarök Rite, without its reward. The only rite that can be " +
            "performed within their bubble: to bail out, bring its item, have someone outside perform it, or let the enemies destroy the " +
            "obliterator.";
        internal const string Reset = "Ends every rite going on at once, except the Domain rites and blessings.";
        // {0}: share given back, in percent.
        internal const string Unmaking =
            "Every craftable weapon, tool, shield and piece of armor burnt with it gives back {0}% of what it took to make and upgrade, at least 1 of each material " +
            "(levels above 4 give back what level 4 does). Everything else burns as usual.";
        internal const string UnmakingUpgradeMaterials = " Gear upgraded past level 4 also gives back that share of the battle idols paid at the upgrade altar.";
    }

    internal static class DebugMessages
    {
        internal const string ReadConfigCalled = "ReadConfigValues called";
        internal const string ErrorLoadingConfig = $"There was an issue loading your {Plugin.ConfigFileName}";
        internal const string RequestCheckConfig = "Please check your config entries for spelling and format!";
    }

    // Sections are numbered as the rites page groups them: atmospheric, celestial, biome mastery, domain, blessings,
    // kinship, reckoning.
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
                    "How far from their obliterator the Domain rites (Packward, Carrion, Verdant, Vat-Boil, Pitch-Bellows, Amber-Mend) work, in " +
                    "meters. Their status and message only show this near it.",
                    new AcceptableValueRange<float>(5f, 500f)),
                100f,
                true);

            internal static readonly ConfigInfo<float> BlessingRange = new(
                Group,
                "Blessings Range",
                new ConfigDescription(
                    "How close to the obliterator players must stand when it burns to receive a blessing (Crypt Delver, Glacial " +
                    "Satiety, Vein-Shatter, Carapace Temper, Grave-Bind, Fortune's Boon, Luminary, Abyssal Draw, Tailwind), in " +
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
            "02 - Clear Sky Rite",
            "the Clear Sky Rite (clear skies)",
            "SurtlingCore",
            "a surtling core",
            "No rain, snow or fog: for building, farming and travelling in peace. Cold biomes stay as cold as their own weather makes them.",
            true,
            environments: "Default: Clear, Mistlands: Mistlands_clear, AshLands: Ashlands_ashrain_clear");

        internal static readonly RiteSettings Thunderstorm = new(
            "03 - Tempest Rite",
            "the Tempest Rite (a thunderstorm)",
            "CopperOre",
            "copper ore",
            "Strong wind for sailing, and the weather serpents come out in. A snowstorm in the mountains.",
            true,
            environments: "Default: ThunderStorm, Mountain: SnowStorm, Mistlands: Mistlands_thunder, AshLands: Ashlands_storm");

        internal static readonly RiteSettings Rain = new(
            "04 - Downpour Rite",
            "the Downpour Rite (rain)",
            "Ooze",
            "ooze",
            "For the mood. Light rain in the meadows, snow in the mountains. Puts out spreading fire with no roof over it and keeps " +
            "cinders from lighting more (the Tempest Rite does too).",
            true,
            amount: 2,
            environments: "Default: Rain, Meadows: LightRain, Swamp: SwampRain, Mountain: Snow, Mistlands: Mistlands_rain, AshLands: Ashlands_ashrain");

        internal static readonly RiteSettings Mist = new(
            "05 - Shrouding Rite",
            "the Shrouding Rite (mist)",
            "Cloudberry",
            "cloudberries",
            "Fog that hides what is far away.",
            true,
            amount: 3,
            environments: "Default: Misty, BlackForest: DeepForest Mist, Mistlands: Mistlands_clear, AshLands: Ashlands_misty");

        internal static readonly RiteSettings Aurora = new(
            "06 - Aurora Rite",
            "the Aurora Rite (auroras and calm winds)",
            "SilverNecklace",
            "a silver necklace",
            "A clear sky with shimmering auroras at night, like the Deep North's, and calm, quiet winds. " + CustomEnvironmentHelp,
            true,
            environments: "Default: " + CustomEnvironmentNames.Aurora);

        // ---- Celestial ----

        internal static readonly RiteSettings PolarDay = new(
            "07 - Sun Zenith Rite",
            "the Sun Zenith Rite (polar day)",
            "Ruby",
            "a ruby",
            "It is day all the time: the sun stays up, creatures spawn as by day and night creatures leave. Time goes on: days are " +
            "counted, clocks show the real time, and beds go by the real clock.",
            true);

        internal static readonly RiteSettings PolarNight = new(
            "08 - Midnight Rite",
            "the Midnight Rite (polar night)",
            "SilverOre",
            "silver ore",
            "It is night all the time: the sun stays down, night creatures spawn (more and stronger ones) and nights are cold. " +
            "Time goes on: days are counted, clocks show the real time, and beds go by the real clock.",
            true);

        internal static readonly RiteSettings GaleBreath = new(
            "09 - Gale-Breath Rite",
            "the Gale-Breath Rite (the strongest wind)",
            "FreezeGland",
            "freeze glands",
            "The wind blows as hard as it can, everywhere, for everyone with the mod. Only its strength: it still turns with the " +
            "weather. With the Tailwind Rite it is behind your ship at full strength.",
            true,
            amount: 3);

        // ---- Biome mastery ----

        internal static readonly RiteSettings NoMist = new(
            "10 - True Sight Rite",
            "the True Sight Rite (no Mistlands mist)",
            "Eitr",
            "refined eitr",
            "The Mistlands mist is gone, for you and for what creatures can see. Off by default: it takes away much of the Mistlands' challenge.",
            true,
            amount: 3);

        internal static readonly RiteSettings Sootfall = new(
            "11 - Sootfall Rite",
            "the Sootfall Rite (no Ashlands smoke or heat haze)",
            "MushroomSmokePuff",
            "smoke puffs",
            "The Ashlands ground smoke and the heat shimmer near lava are gone, so you can see. The heat itself still burns.",
            true,
            amount: 3);


        internal static readonly RiteSettings AbyssalSurge = new(
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

        internal static readonly RiteSettings Packward = new(
            "13 - Packward Rite",
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
            new ConfigDescription("For how many seconds after it was last hurt a creature fights instead of shunning the area. Its pack nearby (same faction, within sight) fights whoever it notices for as long.",
                new AcceptableValueRange<float>(0f, 300f)),
            30f,
            true);

        internal static readonly RiteSettings Carrion = new(
            "14 - Carrion Rite",
            "the Carrion Rite (creatures come)",
            "Pukeberries",
            "bukeperries",
            "Creatures (not bosses, not tamed) within the Domain radius walk to the obliterator, out of the woods and into your " +
            "traps, pens or walls. Their senses dull while they do, but one that notices someone reacts as usual, then walks on " +
            "once it calms down. Raid creatures are left alone, and a raid reaching the area pauses the rite. Replaces a Packward Rite going on.",
            true,
            amount: 3);

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
            "15 - Verdant Rite",
            "the Verdant Rite (plants grow faster)",
            "AncientSeed",
            "ancient seeds",
            "Plants, crops, saplings and berry bushes within the Domain radius of the obliterator grow faster for as long as it " +
            "lasts, some now and then giving off green motes; what they grew stays after it ends.",
            true,
            amount: 9);

        internal static readonly ConfigInfo<float> VerdantGrowthMultiplier = new(
            Verdant.Enabled.Group,
            "Growth Speed Multiplier",
            new ConfigDescription(
                "How many times as fast things grow under the Verdant Rite: 2 grows twice as fast, 1.5 half again as fast.",
                new AcceptableValueRange<float>(1f, 10f)),
            2f,
            true);

        internal static readonly RiteSettings VatBoil = new(
            "16 - Vat-Boil Rite",
            "the Vat-Boil Rite (fermenting and cooking go faster)",
            "Bilebag",
            "bile bags",
            "Fermenters and cooking stations (cooking station, iron cooking station, oven) within the Domain radius of the obliterator " +
            "work faster, some now and then giving off golden motes, and nothing cooked there burns while it lasts; what they did stays " +
            "after it ends. 30 minutes at 2x: about one fermentation.",
            true,
            amount: 2);

        internal static readonly ConfigInfo<float> VatBoilSpeedMultiplier = new(
            VatBoil.Enabled.Group,
            "Speed Multiplier",
            new ConfigDescription(
                "How many times as fast fermenters and cooking stations work under the Vat-Boil Rite: 2 is twice as fast.",
                new AcceptableValueRange<float>(1f, 10f)),
            2f,
            true);

        internal static readonly RiteSettings PitchBellows = new(
            "17 - Pitch-Bellows Rite",
            "the Pitch-Bellows Rite (smelters and kilns work faster)",
            "Tar",
            "tar",
            "Smelters, blast furnaces, charcoal kilns and eitr refineries within the Domain radius of the obliterator work faster, some " +
            "now and then giving off ember motes, using up ore and fuel as usual; what they made stays after it ends.",
            true,
            amount: 5);

        internal static readonly ConfigInfo<float> PitchBellowsSpeedMultiplier = new(
            PitchBellows.Enabled.Group,
            "Speed Multiplier",
            new ConfigDescription(
                "How many times as fast smelters and kilns work under the Pitch-Bellows Rite: 2 is twice as fast. They use ore and fuel " +
                "as usual, so fuel runs out sooner too.",
                new AcceptableValueRange<float>(1f, 10f)),
            2f,
            true);

        internal static readonly RiteSettings AmberMend = new(
            "18 - Amber-Mend Rite",
            "the Amber-Mend Rite (buildings mend)",
            "Resin",
            "resin",
            "Damaged building pieces within the Domain radius of the obliterator are repaired one after another, a few each second, " +
            "with the hammer's repair effect.",
            true,
            amount: 20,
            minutes: 5f);

        internal static readonly RiteSettings SovereignWard = new(
            "19 - Sovereign Ward Rite",
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

        internal static readonly RiteSettings Unmaking = new(
            "20 - Unmaking Rite",
            "the Unmaking Rite (gear back into materials)",
            "Thunderstone",
            "a thunderstone",
            "Every craftable weapon, tool, shield and piece of armor burnt along with it gives back part of what it took to make " +
            "and upgrade it (station materials up to level 4; the upgrade altar's materials, like battle idols, for levels past 4 " +
            "if Return Upgrade Materials is on). Everything else burns as usual.",
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
            "21 - Grounding Rite",
            "the Grounding Rite (ends the Domain rites)",
            "Flint",
            "flint",
            "Ends the Domain rites going on (Packward, Carrion, Verdant, Vat-Boil, Pitch-Bellows, Amber-Mend, Sovereign Ward) at once. The Equilibrium Rite leaves them be.",
            true,
            lasts: false);

        // ---- Blessings: only for the players within the blessing range when the obliterator burns ----

        private const string BlessingNote = " A blessing: only players within the Blessings Range of the obliterator when it burns " +
            "receive it, struck by its lightning (no harm done). It ends at death; the Equilibrium Rite leaves it be.";

        internal static readonly RiteSettings CryptDelver = new(
            "22 - Crypt Delver Rite",
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
            "23 - Glacial Satiety Rite",
            "the Glacial Satiety Rite (food fades slower)",
            "DragonTear",
            "a dragon tear",
            "Eaten food fades only down to the Food Floor Multiplier of what it gives, instead of half, until it runs out." + BlessingNote,
            true,
            minutes: 90f);

        internal static readonly ConfigInfo<float> FoodFloor = new(
            GlacialSatiety.Enabled.Group,
            "Food Floor Multiplier",
            new ConfigDescription("The least health, stamina and eitr a food gives while it lasts, times its full value: 0.75 is 75% (the game's own floor is about 0.5).", new AcceptableValueRange<float>(0.5f, 1f)),
            0.75f,
            true);

        internal static readonly RiteSettings VeinShatter = new(
            "24 - Vein-Shatter Rite",
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
            "25 - Carapace Temper Rite",
            "the Carapace Temper Rite (gear wears slower)",
            "Mandible",
            "mandibles",
            "Equipped weapons, tools, bows, shields and armor lose less durability." + BlessingNote,
            true,
            amount: 2);

        internal static readonly ConfigInfo<float> WearMultiplier = new(
            CarapaceTemper.Enabled.Group,
            "Wear Multiplier",
            new ConfigDescription("How much of the usual durability equipped gear loses: 0.4 is 40% (60% less). 0: no wear.", new AcceptableValueRange<float>(0f, 1f)),
            0.4f,
            true);

        internal static readonly RiteSettings GraveBind = new(
            "26 - Grave-Bind Rite",
            "the Grave-Bind Rite (no skill loss on death)",
            "YagluthDrop",
            "a torn spirit",
            "Dying doesn't lower your skills (with or without the No Skill Drain status), and your tombstone shines a beam into the " +
            "sky that can be seen from far away until it is emptied; looting it puts your gear back on. The death ends it." + BlessingNote,
            true,
            minutes: 60f);

        internal static readonly ConfigInfo<Toggle> GraveBeacon = new(
            GraveBind.Enabled.Group,
            "Tombstone Beam",
            "Show a beam into the sky over your tombstone after a death under the Grave-Bind Rite.",
            Toggle.On,
            false);

        internal static readonly ConfigInfo<Toggle> GraveReequip = new(
            GraveBind.Enabled.Group,
            "Re-Equip Gear",
            "After a death under the Grave-Bind Rite, put the gear you wore back on once you loot your tombstone.",
            Toggle.On,
            false);

        internal static readonly RiteSettings FortunesBoon = new(
            "27 - Fortunes Boon Rite",
            "the Fortune's Boon Rite (more luck)",
            "Wishbone",
            "a wishbone",
            "Your skills' lucky rolls for extra items come more often: crafting and cauldron cooking, the cooking station, and " +
            "harvesting crops and other pickables. At the Forge of Potential, the first upgrade that would destroy your item " +
            "doesn't: the item loses the Forge Break Penalty in levels instead, and that ends the blessing." + BlessingNote,
            true,
            minutes: 5f);

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
            "28 - Luminary Rite",
            "the Luminary Rite (you shine)",
            "Wisp",
            "wisps",
            "You shine with a warm light like a lantern, lighting the ground and walls around you, in caves and dungeons too, " +
            "and everyone with the mod sees it." + BlessingNote,
            true,
            amount: 5);

        internal static readonly ConfigInfo<float> LuminaryRange = new(
            Luminary.Enabled.Group,
            "Light Range",
            new ConfigDescription("How far around you the light reaches, in meters.", new AcceptableValueRange<float>(3f, 50f)),
            16f,
            true);

        internal static readonly ConfigInfo<float> LuminaryStrength = new(
            Luminary.Enabled.Group,
            "Light Strength Multiplier",
            new ConfigDescription("How bright the light around you is: 0.5 is half as bright, 2 twice as bright. The range stays the same.", new AcceptableValueRange<float>(0.1f, 2f)),
            1f,
            false);

        internal static readonly ConfigInfo<float> LuminaryBodyLight = new(
            Luminary.Enabled.Group,
            "Body Light Multiplier",
            new ConfigDescription("How bright the soft light that keeps the shining player's body lit is: 1 as usual, lower it if the body looks too bright. 0: no body light.", new AcceptableValueRange<float>(0f, 2f)),
            1f,
            false);

        internal static readonly ConfigInfo<Toggle> LuminaryShadows = new(
            Luminary.Enabled.Group,
            "Light Shadows",
            "On: floors, ceilings and walls block the light, so it doesn't shine through to the floor above or below or the next " +
            "room. Off: a little lighter on the graphics card, but the light goes through everything within its range. Also " +
            "needs shadows on in the game's graphics settings.",
            Toggle.On,
            false);

        internal static readonly RiteSettings AbyssalDraw = new(
            "29 - Abyssal Draw Rite",
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

        internal static readonly RiteSettings Tailwind = new(
            "30 - Tailwind Rite",
            "the Tailwind Rite (wind behind your ship)",
            "DragonEgg",
            "a dragon egg",
            "While you sail, the wind always blows from behind, like Moder's power, also for everyone on the same ship. Only its " +
            "direction: how strong it is stays the weather's (the Gale-Breath Rite makes it the strongest)." + BlessingNote,
            true);

        // ---- Kinship: for everyone on the server at once, wherever they are ----

        private const string KinshipNote = " A kinship rite: everyone on the server receives it at once, wherever they are.";

        internal static readonly RiteSettings Skal = new(
            "31 - Skal Rite",
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
            "32 - Sky-Call Rite",
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
            "33 - Anvil-Spark Rite",
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

        // ---- Reckoning ----

        internal static readonly RiteSettings Crimson = new(
            "34 - Crimson Rite",
            "the Crimson Rite (raid after raid)",
            "WitheredBone",
            "withered bones",
            "Raids keep coming to the obliterator, one after another, picked among those the players within the radius are ready " +
            "for (player based raids and World Advancement Progression's private raids too), with no wait and no base needed; " +
            "lightning strikes the obliterator as each new raid comes. No other raid starts anywhere meanwhile, and raid " +
            "creatures only spawn within the radius. A shield bubble covers " +
            "the radius: from when it starts, players inside can't get out and players outside can't get in, and no portal " +
            "into or out of it works, until it ends. No creature inside can get out, and none outside can get in. Nothing can " +
            "be built, repaired or torn down inside, and no rite but the Sanctuary Rite performed there. Each creature " +
            "slain inside counts; survive to the end and the obliterator, struck by lightning, holds a reward for each kill. " +
            "However it ends, the hostile creatures still inside die and drop nothing (not counted; spawners and nests are left " +
            "alone). It breaks off early if the obliterator is destroyed, everyone inside falls or leaves, or the Sanctuary " +
            "or Equilibrium Rite ends it. Only one Crimson or Ragnarök Rite can go on at a time, and no Domain rite works " +
            "within its bubble: one at an obliterator inside is cancelled. Its status and messages only " +
            "show near the obliterator; its area is marked on everyone's map.",
            true,
            amount: 3,
            minutes: 10f,
            radius: 75f);

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
            2,
            true);

        internal static readonly ConfigInfo<Toggle> CrimsonBubble = new(
            Crimson.Enabled.Group,
            "Show Bubble",
            "Show the shield bubble over the Crimson Rite's radius. Players are held inside (or kept out) either way.",
            Toggle.On,
            false);

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

        internal static readonly RiteSettings Ragnarok = new(
            "35 - Ragnarok Rite",
            "the Ragnarök Rite (boss after boss)",
            "TrophyEikthyr, TrophyTheElder, TrophyBonemass, TrophyDragonQueen, TrophyGoblinKing, TrophySeekerQueen, TrophyFader",
            "any boss trophies",
            "Bosses rise at the obliterator one after another, picked among those every player within the radius has slain " +
            "(each player's own boss kills; with player based raids off, the world's count for everyone), weaker ones first. " +
            "With none, nothing rises and everyone there is told why. The sky burns, and a shield bubble covers the radius: " +
            "from when it starts, players inside can't get out and players outside can't get in, and no portal into or out of " +
            "it works; no creature gets in or out. Nothing can be built, repaired or torn down inside, and no rite but the " +
            "Sanctuary Rite performed there. Lightning strikes the obliterator as each boss falls. Slay them all before the " +
            "time runs out and the obliterator, struck by " +
            "lightning, holds treasure: one roll of the treasure table for each boss, the stronger the boss the more precious " +
            "and plentiful. However it ends, the bosses and hostile creatures still inside vanish and drop nothing. It breaks " +
            "off with no reward if the time runs out, the obliterator is destroyed, everyone inside falls or leaves, or the " +
            "Sanctuary or Equilibrium Rite ends it. Only one Crimson or Ragnarök Rite can go on at a time, and no Domain rite " +
            "works within its bubble: one at an obliterator inside is cancelled. Its status and " +
            "messages only show near the obliterator; its area is marked on everyone's map.",
            true,
            amount: 3,
            minutes: 20f,
            radius: 75f,
            minutesDescription: "How long the players have to slay every boss of the Ragnarök Rite, in minutes of play. When it " +
            "runs out, the bosses withdraw and there is no reward.");

        internal static readonly ConfigInfo<string> RagnarokBosses = new(
            Ragnarok.Enabled.Group,
            "Bosses",
            "Prefab names of the bosses the Ragnarök Rite can call, weakest first: their place in this list is how strong they " +
            "count for the treasure. A player has slain a boss when they have the key it sets as it dies (Eikthyr: " +
            "defeated_eikthyr...). Modded bosses work too when they set such a key.",
            "Eikthyr, gd_king, Bonemass, Dragon, GoblinKing, SeekerQueen, Fader",
            true);

        internal static readonly ConfigInfo<int> RagnarokBossCount = new(
            Ragnarok.Enabled.Group,
            "Boss Count",
            new ConfigDescription("How many bosses rise, one after another. Different ones are picked while there are enough; " +
                "with fewer eligible, some come again.", new AcceptableValueRange<int>(1, 10)),
            3,
            true);

        internal static readonly ConfigInfo<float> RagnarokPause = new(
            Ragnarok.Enabled.Group,
            "Pause Between Bosses Seconds",
            new ConfigDescription("How long after a boss falls the next one rises.", new AcceptableValueRange<float>(3f, 120f)),
            10f,
            true);

        internal static readonly ConfigInfo<string> RagnarokTreasure = new(
            Ragnarok.Enabled.Group,
            "Treasure",
            "The treasure table, as Item: Min-Max pairs, from the least to the most precious. One item is rolled for each boss " +
            "slain: the weakest boss most likely gives the first, the strongest the last, and stronger bosses roll toward Max.",
            "Amber: 3-6, AmberPearl: 2-5, Ruby: 2-4, SilverNecklace: 1-3",
            true);

        internal static readonly ConfigInfo<Toggle> RagnarokBubble = new(
            Ragnarok.Enabled.Group,
            "Show Bubble",
            "Show the shield bubble over the Ragnarök Rite's radius. Players are held inside (or kept out) either way.",
            Toggle.On,
            false);

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

        internal static readonly RiteSettings Sanctuary = new(
            "36 - Sanctuary Rite",
            "the Sanctuary Rite (ends the raid)",
            "IronScrap",
            "scrap iron",
            "Ends the raid going on in the world at once, and any Crimson or Ragnarök Rite (without its reward). The only rite " +
            "that can be performed within their bubble.",
            true,
            lasts: false,
            amount: 3);

        internal static readonly RiteSettings Reset = new(
            "37 - Equilibrium Rite",
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
            internal const string Group = "38 - Display";

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

            internal static readonly ConfigInfo<Toggle> ShowVerdantSparkles = new(
                Group,
                "Show Verdant Sparkles",
                "Under the Verdant Rite, now and then a few of the plants and bushes growing faster give off green motes.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowVatBoilMotes = new(
                Group,
                "Show Vat-Boil Motes",
                "Under the Vat-Boil Rite, now and then a few of the fermenters and cooking stations working faster give off golden motes.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowPitchBellowsMotes = new(
                Group,
                "Show Pitch-Bellows Motes",
                "Under the Pitch-Bellows Rite, now and then a few of the smelters and kilns working faster give off ember motes.",
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
                "Creatures shunning a Packward Rite drip with fear, and creatures drawn by a Carrion Rite trail a green smell.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowCreatureIcons = new(
                Group,
                "Show Creature Icons",
                "Where the game shows its yellow and red alert icons over a creature: a blue drop while it shuns a Packward Rite, " +
                "a green heart while a Carrion Rite draws it.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowDomainEffects = new(
                Group,
                "Show Domain Effects",
                "While a Domain rite goes on, rings in its color spread out from the obliterator over its area, one at a time, " +
                "and the obliterator glows and gives off rising motes in the colors of its rites.",
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
        }
    }
}

// Environments the mod makes from the game's own, for rites with a sky the game doesn't have.
internal static class CustomEnvironmentNames
{
    internal const string Aurora = "TempestRite_Aurora";
    internal const string Bloodmoon = "TempestRite_Bloodmoon";
    internal const string Ragnarok = "TempestRite_Ragnarok";
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

    // minutesDescription: what the Duration Minutes setting means, when it isn't how long the rite goes on.
    internal RiteSettings(string group, string effect, string keyItem, string keyItemName, string why, bool enabled, bool lasts = true,
        string? environments = null, string namePrefix = "", int amount = 1, float minutes = 30f, float radius = 0f,
        string? minutesDescription = null)
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
                minutesDescription ?? $"How long {effect} lasts, in minutes of play. Sleeping doesn't shorten it; pausing a " +
                "single player game pauses it. Performing it again starts the time over.",
                new AcceptableValueRange<float>(0.5f, 600f)),
            minutes,
            true);

        KeyItem = new(
            group,
            namePrefix + "Key Item",
            $"Prefab names of the items that perform {effect} when burnt, separated by commas (default {keyItem}, {keyItemName}). " +
            "Add others to let any of them work: any mix of them counts toward the Key Item Amount. An item performs one rite " +
            "only: when two rites list it, the one in the lower numbered section keeps it.",
            keyItem,
            true);

        KeyAmount = new(
            group,
            namePrefix + "Key Item Amount",
            new ConfigDescription(
                $"How many key items (any mix of them) have to be in the obliterator to perform {effect}. Fewer just burn.",
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
