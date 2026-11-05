using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// When StatusGroups is installed, the rites' statuses get places on its HUD, named in the player's language:
// blessings join its Buffs and kinship waits its Cooldowns; the obliterator's rites (Domain and Reckoning) and the
// world rites (Atmospheric, Celestial, Biome Mastery) get a group each. Called by reflection, so nothing changes without it.
internal static class StatusGroupsLink
{
    internal const string StatusGroupsGuid = "JoksterCube.StatusGroups";
    private const string ApiType = "JoksterCube.StatusGroups.StatusGroupsApi";

    // StatusGroups' own groups.
    private const string Buffs = "Buffs";
    private const string Cooldowns = "Cooldowns";
    // TempestRite's: also the names of their settings in StatusGroups ("TempestRite Domain Display"), so keep them.
    // Their places are claimed in StatusGroups' README (1000-1100).
    private const string Domain = "TempestRite Domain";
    private const string World = "TempestRite World";

    private static readonly Dictionary<RiteGroup, string> Groups = new()
    {
        [RiteGroup.Blessings] = Buffs,
        [RiteGroup.Kinship] = Cooldowns,
        [RiteGroup.Domain] = Domain,
        [RiteGroup.Reckoning] = Domain,
        [RiteGroup.Atmospheric] = World,
        [RiteGroup.Celestial] = World,
        [RiteGroup.BiomeMastery] = World
    };

    internal static void Register()
    {
        try
        {
            if (!Chainloader.PluginInfos.TryGetValue(StatusGroupsGuid, out PluginInfo info) || !info.Instance) return;
            Type? api = info.Instance.GetType().Assembly.GetType(ApiType);
            MethodInfo? addGroup = api?.GetMethod("AddGroup", [typeof(string), typeof(Func<string>), typeof(int)]);
            MethodInfo? assign = api?.GetMethod("Assign", [typeof(string), typeof(string)]);
            if (addGroup == null || assign == null)
            {
                Plugin.ModLogger.LogWarning("StatusGroups found, but not the calls TempestRite uses: rite statuses keep its default groups.");
                return;
            }

            addGroup.Invoke(null, [Domain, (Func<string>)(() =>
                $"{WeatherKinds.GroupName(RiteGroup.Domain)} & {WeatherKinds.GroupName(RiteGroup.Reckoning)}"), 1000]);
            addGroup.Invoke(null, [World, (Func<string>)(() => Messages.GroupWorldRites), 1100]);
            foreach (WeatherKind kind in WeatherKinds.All)
                assign.Invoke(null, [StatusCommands.Name(kind), Groups[WeatherKinds.Group(kind)]]);
            // The open rally call is the Rally-Horn Rite's, a kinship rite, but it's an offer to take, not a wait.
            assign.Invoke(null, [StatusCommands.RallyCallName, Buffs]);
            assign.Invoke(null, [StatusCommands.ArenaPreviewStatus, Domain]);
            Plugin.ModLogger.LogInfo("StatusGroups found: rite statuses are placed in its groups.");
        }
        catch (Exception exception)
        {
            Plugin.ModLogger.LogWarning($"Could not set up StatusGroups' rite groups: {exception.Message}");
        }
    }
}
