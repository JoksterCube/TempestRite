using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ServerSync;

namespace JoksterCube.TempestRite.Settings;

internal static class ConfigOptions
{
    private static ConfigFile? _configFile;
    private static ConfigSync? _configSync;
    // Configuration Manager lists higher Order first, so each group counts down in binding order.
    private static readonly Dictionary<string, int> NextOrder = [];

    // The mod version that last saved the config file (null for a new file), for moving old defaults to new ones.
    private static System.Version? _savedBy;

    internal static void Initialize(ConfigFile config, ConfigSync configSync)
    {
        _configFile = config;
        _configSync = configSync;
        _savedBy = OldDefaults.SavedBy(config.ConfigFilePath);
    }

    // After every setting is bound: saves once under this version, so old defaults move only once and a player who
    // sets an old default again keeps it.
    internal static void Finish(string modVersion)
    {
        if (_configFile == null || _savedBy == null || !System.Version.TryParse(modVersion, out System.Version current) || _savedBy >= current) return;
        _configFile.Save();
    }

    internal static ConfigEntry<T> Config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true, bool advanced = false)
    {
        int order = NextOrder.TryGetValue(group, out int next) ? next : 1000;
        NextOrder[group] = order - 1;
        ConfigurationManagerAttributes attributes = new() { Order = order, IsAdvanced = advanced ? true : null };
        ConfigDescription extendedDescription = new(
            description.Description + (synchronizedSetting
                ? " [Synced with Server]"
                : " [Not Synced with Server]"),
            description.AcceptableValues,
            (description.Tags ?? []).Append(attributes).ToArray());

        ConfigEntry<T> configEntry = (_configFile ?? throw new InvalidOperationException("ConfigOptions is not initialized.")).Bind(group, name, value, extendedDescription);
        SyncedConfigEntry<T> syncedConfigEntry = (_configSync ?? throw new InvalidOperationException("ConfigOptions is not initialized.")).AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;
        return configEntry;
    }

    internal static ConfigEntry<T> Config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) =>
        Config(group, name, value, new ConfigDescription(description), synchronizedSetting);

    internal static ConfigEntry<T> Config<T>(ConfigInfo<T> configInfo)
    {
        ConfigEntry<T> entry = Config(configInfo.Group, configInfo.Name, configInfo.DefaultValue, configInfo.Description, configInfo.Synchronized, configInfo.IsAdvanced);
        foreach ((string changedIn, T oldDefault) in configInfo.OldDefaultValues)
        {
            if (!OldDefaults.Applies(_savedBy, changedIn) || !OldDefaults.Same(entry.Value, oldDefault)) continue;
            entry.Value = configInfo.DefaultValue;
            Plugin.ModLogger.LogInfo($"{configInfo.Group} > {configInfo.Name}: was on the old default, now on the new one ({configInfo.DefaultValue}).");
            break;
        }
        return entry;
    }
}